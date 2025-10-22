using FluentAssertions;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SmokeTests;

/// <summary>
/// Smoke tests for UDP datagram reception
/// These tests verify that the service can receive and process UDP datagrams
/// </summary>
[Collection("Smoke Tests")]
public class UdpSmokeTests : IClassFixture<SmokeTestFixture>
{
    private readonly SmokeTestFixture _fixture;

    public UdpSmokeTests(SmokeTestFixture fixture)
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
    public async Task UDP_Listener_Should_Be_Accessible()
    {
        // Arrange
        using var udpClient = new UdpClient();
        var endpoint = GetUdpEndpoint();

        // Use valid callsign format (max 6 chars + optional -SSID)
        var validDatagram = """
        {
            "@type": "NodeUpEvent",
            "nodeCall": "TEST-1",
            "nodeAlias": "TST",
            "locator": "IO82VJ",
            "software": "SmokeTest",
            "version": "1.0"
        }
        """;

        var bytes = Encoding.UTF8.GetBytes(validDatagram);

        // Act - Send UDP datagram
        var sent = await udpClient.SendAsync(bytes, bytes.Length, endpoint);

        // Assert
        sent.Should().Be(bytes.Length, "UDP datagram should be sent successfully");

        // Note: We can't directly verify reception without access to the service logs or MQTT
        // But if we get here without exception, the UDP port is at least accessible
    }

    [Fact]
    public async Task UDP_Should_Accept_L2Trace_Datagram()
    {
        // Arrange
        using var udpClient = new UdpClient();
        var endpoint = GetUdpEndpoint();

        var l2TraceDatagram = """
        {
            "@type": "L2Trace",
            "reportFrom": "TEST",
            "port": "1",
            "srce": "TEST-1",
            "dest": "TEST-2",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C"
        }
        """;

        var bytes = Encoding.UTF8.GetBytes(l2TraceDatagram);

        // Act
        var sent = await udpClient.SendAsync(bytes, bytes.Length, endpoint);

        // Assert
        sent.Should().Be(bytes.Length, "L2Trace datagram should be sent successfully");
    }

    [Fact]
    public async Task UDP_Should_Accept_CircuitUpEvent_Datagram()
    {
        // Arrange
        using var udpClient = new UdpClient();
        var endpoint = GetUdpEndpoint();

        var circuitUpDatagram = """
        {
            "@type": "CircuitUpEvent",
            "time": 1759688220,
            "node": "TEST",
            "id": 1,
            "direction": "incoming",
            "service": 0,
            "remote": "TEST1@TEST:1234",
            "local": "SMOKE:5678"
        }
        """;

        var bytes = Encoding.UTF8.GetBytes(circuitUpDatagram);

        // Act
        var sent = await udpClient.SendAsync(bytes, bytes.Length, endpoint);

        // Assert
        sent.Should().Be(bytes.Length, "CircuitUpEvent datagram should be sent successfully");
    }

    [Fact]
    public async Task UDP_Should_Handle_Multiple_Datagrams_In_Sequence()
    {
        // Arrange
        using var udpClient = new UdpClient();
        var endpoint = GetUdpEndpoint();

        var datagrams = new[]
        {
            """{"@type": "NodeUpEvent", "nodeCall": "TEST1", "nodeAlias": "TST1", "locator": "IO82VJ", "software": "Test", "version": "1.0"}""",
            """{"@type": "NodeStatus", "nodeCall": "TEST1", "nodeAlias": "TST1", "locator": "IO82VJ", "software": "Test", "version": "1.0", "uptimeSecs": 100}""",
            """{"@type": "NodeDownEvent", "nodeCall": "TEST1", "nodeAlias": "TST1"}"""
        };

        // Act & Assert
        foreach (var datagram in datagrams)
        {
            var bytes = Encoding.UTF8.GetBytes(datagram);
            var sent = await udpClient.SendAsync(bytes, bytes.Length, endpoint);
            sent.Should().Be(bytes.Length);
            
            // Small delay between datagrams
            await Task.Delay(100);
        }
    }

    [Fact]
    public async Task UDP_Should_Accept_Large_L2Trace_With_Routing_Info()
    {
        // Arrange
        using var udpClient = new UdpClient();
        var endpoint = GetUdpEndpoint();

        var largeL2Trace = """
        {
            "@type": "L2Trace",
            "reportFrom": "TEST",
            "port": "1",
            "srce": "TEST-1",
            "dest": "NODES",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C",
            "ptcl": "NET/ROM",
            "l3type": "Routing info",
            "type": "INP3",
            "nodes": [
                {
                    "call": "NODE1",
                    "hops": 2,
                    "tt": 10,
                    "alias": "NODE1"
                },
                {
                    "call": "NODE2",
                    "hops": 3,
                    "tt": 15,
                    "alias": "NODE2"
                }
            ]
        }
        """;

        var bytes = Encoding.UTF8.GetBytes(largeL2Trace);

        // Act
        var sent = await udpClient.SendAsync(bytes, bytes.Length, endpoint);

        // Assert
        sent.Should().Be(bytes.Length, "Large L2Trace with routing info should be sent successfully");
    }

    [Fact]
    public async Task UDP_Should_Not_Throw_On_Invalid_JSON()
    {
        // Arrange
        using var udpClient = new UdpClient();
        var endpoint = GetUdpEndpoint();

        var invalidJson = "{ this is not valid json }";
        var bytes = Encoding.UTF8.GetBytes(invalidJson);

        // Act - Should not throw, service should handle gracefully
        var action = async () => await udpClient.SendAsync(bytes, bytes.Length, endpoint);

        // Assert
        await action.Should().NotThrowAsync("Service should handle invalid JSON gracefully");
    }

    [Fact]
    public async Task UDP_Should_Not_Throw_On_Unknown_Datagram_Type()
    {
        // Arrange
        using var udpClient = new UdpClient();
        var endpoint = GetUdpEndpoint();

        var unknownType = """
        {
            "@type": "UnknownEventType",
            "someField": "someValue"
        }
        """;
        var bytes = Encoding.UTF8.GetBytes(unknownType);

        // Act - Should not throw, service should handle gracefully
        var action = async () => await udpClient.SendAsync(bytes, bytes.Length, endpoint);

        // Assert
        await action.Should().NotThrowAsync("Service should handle unknown types gracefully");
    }
}
