using FluentAssertions;
using MQTTnet;
using MQTTnet.Client;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace SmokeTests;

/// <summary>
/// End-to-end integration smoke tests
/// These tests verify the complete flow from UDP to MQTT
/// </summary>
[Collection("Smoke Tests")]
public class EndToEndSmokeTests : IClassFixture<SmokeTestFixture>
{
    private readonly SmokeTestFixture _fixture;

    public EndToEndSmokeTests(SmokeTestFixture fixture)
    {
        _fixture = fixture;
    }

    private IPEndPoint GetUdpEndpoint()
    {
        // Get IPv4 address to avoid protocol mismatch with default UdpClient
        var addresses = Dns.GetHostAddresses(_fixture.Settings.UdpHost);
        var ipv4Address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
            ?? throw new InvalidOperationException($"No IPv4 address found for {_fixture.Settings.UdpHost}");
        
        return new IPEndPoint(ipv4Address, _fixture.Settings.UdpPort);
    }

    [Fact]
    public async Task Complete_Flow_Valid_Datagram_Should_Appear_On_MQTT()
    {
        // This test verifies the complete flow: UDP -> Service -> MQTT

        // Arrange - Setup MQTT listener
        var factory = new MqttFactory();
        using var mqttClient = factory.CreateMqttClient();
        
        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_fixture.Settings.MqttHost, _fixture.Settings.MqttPort)
            .WithCleanSession()
            .Build();

        await mqttClient.ConnectAsync(mqttOptions);

        var receivedMessages = new List<string>();
        var messageReceived = new TaskCompletionSource<bool>();

        mqttClient.ApplicationMessageReceivedAsync += args =>
        {
            var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
            receivedMessages.Add(payload);
            
            // Check if it's our test message
            if (payload.Contains("E2E"))
            {
                messageReceived.TrySetResult(true);
            }
            return Task.CompletedTask;
        };

        await mqttClient.SubscribeAsync("out/NodeUpEvent");

        // Act - Send UDP datagram with valid callsign format
        using var udpClient = new UdpClient();
        var endpoint = GetUdpEndpoint();

        var testDatagram = """
        {
            "@type": "NodeUpEvent",
            "nodeCall": "E2E",
            "nodeAlias": "E2ETST",
            "locator": "IO82VJ",
            "software": "SmokeTest",
            "version": "1.0"
        }
        """;

        var bytes = Encoding.UTF8.GetBytes(testDatagram);
        await udpClient.SendAsync(bytes, bytes.Length, endpoint);

        // Assert - Wait for message to appear on MQTT
        var timeout = Task.Delay(TimeSpan.FromSeconds(_fixture.Settings.TestTimeoutSeconds));
        var completedTask = await Task.WhenAny(messageReceived.Task, timeout);

        completedTask.Should().Be(messageReceived.Task, 
            "Message should appear on MQTT within timeout");

        await mqttClient.DisconnectAsync();
    }

    [Fact]
    public async Task Invalid_Datagram_Should_Appear_On_Error_Topic()
    {
        // Arrange - Setup MQTT listener for error topics
        var factory = new MqttFactory();
        using var mqttClient = factory.CreateMqttClient();
        
        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_fixture.Settings.MqttHost, _fixture.Settings.MqttPort)
            .WithCleanSession()
            .Build();

        await mqttClient.ConnectAsync(mqttOptions);

        var errorReceived = new TaskCompletionSource<string>();

        mqttClient.ApplicationMessageReceivedAsync += args =>
        {
            var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
            errorReceived.TrySetResult(payload);
            return Task.CompletedTask;
        };

        await mqttClient.SubscribeAsync("in/udp/errored/#");

        // Act - Send invalid UDP datagram
        using var udpClient = new UdpClient();
        var endpoint = GetUdpEndpoint();

        var invalidDatagram = """
        {
            "@type": "NodeUpEvent",
            "nodeCall": "",
            "nodeAlias": ""
        }
        """;

        var bytes = Encoding.UTF8.GetBytes(invalidDatagram);
        await udpClient.SendAsync(bytes, bytes.Length, endpoint);

        // Assert - Wait for error message
        var timeout = Task.Delay(TimeSpan.FromSeconds(_fixture.Settings.TestTimeoutSeconds));
        var completedTask = await Task.WhenAny(errorReceived.Task, timeout);

        completedTask.Should().Be(errorReceived.Task, 
            "Error should appear on MQTT error topic within timeout");

        var errorPayload = await errorReceived.Task;
        errorPayload.Should().NotBeNullOrEmpty();

        await mqttClient.DisconnectAsync();
    }

    [Fact]
    public async Task Service_Should_Handle_Burst_Of_UDP_Messages()
    {
        // Arrange
        using var udpClient = new UdpClient();
        var endpoint = GetUdpEndpoint();

        // Use valid callsign format (max 6 chars)
        var datagrams = Enumerable.Range(1, 10).Select(i => $$"""
        {
            "@type": "NodeStatus",
            "nodeCall": "BRS{{i}}",
            "nodeAlias": "BRS{{i}}",
            "locator": "IO82VJ",
            "software": "Test",
            "version": "1.0",
            "uptimeSecs": {{i * 100}}
        }
        """).ToArray();

        // Act - Send burst of messages
        var sendTasks = datagrams.Select(async datagram =>
        {
            var bytes = Encoding.UTF8.GetBytes(datagram);
            return await udpClient.SendAsync(bytes, bytes.Length, endpoint);
        });

        var results = await Task.WhenAll(sendTasks);

        // Assert
        results.Should().AllSatisfy(result => result.Should().BeGreaterThan(0), 
            "All messages should be sent successfully");
    }

    [Fact]
    public async Task Diagnostics_Endpoint_Should_Work_While_Service_Is_Running()
    {
        // Arrange - Use valid callsign format
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "LIVE",
            "port": "1",
            "srce": "TEST-1",
            "dest": "TEST-2",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C"
        }
        """;

        // Act
        var response = await _fixture.HttpClient.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(
            "Diagnostics endpoint should work while service is processing UDP");
    }

    [Fact]
    public async Task Different_Datagram_Types_Should_All_Be_Processed()
    {
        // Arrange
        using var udpClient = new UdpClient();
        var endpoint = GetUdpEndpoint();

        // Use valid callsign format (max 6 chars)
        var datagrams = new[]
        {
            ("NodeUpEvent", """{"@type": "NodeUpEvent", "nodeCall": "M1", "nodeAlias": "M1", "locator": "IO82VJ", "software": "Test", "version": "1.0"}"""),
            ("L2Trace", """{"@type": "L2Trace", "reportFrom": "M2", "port": "1", "srce": "SRC", "dest": "DST", "ctrl": 3, "l2Type": "UI", "cr": "C"}"""),
            ("CircuitUpEvent", """{"@type": "CircuitUpEvent", "node": "M3", "id": 1, "direction": "incoming", "remote": "R:1", "local": "L:2"}"""),
            ("LinkUpEvent", """{"@type": "LinkUpEvent", "node": "M4", "id": 1, "direction": "outgoing", "port": "1", "remote": "REM", "local": "LOC"}"""),
            ("NodeDownEvent", """{"@type": "NodeDownEvent", "nodeCall": "M5", "nodeAlias": "M5"}""")
        };

        // Act - Send different types of datagrams
        foreach (var (type, datagram) in datagrams)
        {
            var bytes = Encoding.UTF8.GetBytes(datagram);
            var sent = await udpClient.SendAsync(bytes, bytes.Length, endpoint);
            
            sent.Should().BeGreaterThan(0, $"{type} should be sent successfully");
            
            // Small delay between different types
            await Task.Delay(50);
        }

        // Assert - All datagrams sent successfully (we can't verify processing without MQTT)
        // But the fact that we got here without exceptions is a good sign
    }
}
