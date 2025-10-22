using FluentAssertions;
using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;

namespace SmokeTests;

/// <summary>
/// Smoke tests for MQTT message publishing
/// These tests verify that UDP datagrams are being published to MQTT topics
/// </summary>
[Collection("Smoke Tests")]
public class MqttSmokeTests : IClassFixture<SmokeTestFixture>
{
    private readonly SmokeTestFixture _fixture;

    public MqttSmokeTests(SmokeTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MQTT_Broker_Should_Be_Accessible()
    {
        // Arrange
        var factory = new MqttFactory();
        using var mqttClient = factory.CreateMqttClient();
        
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_fixture.Settings.MqttHost, _fixture.Settings.MqttPort)
            .WithCleanSession()
            .WithTimeout(TimeSpan.FromSeconds(10))
            .Build();

        // Act
        var connectResult = await mqttClient.ConnectAsync(options);

        // Assert
        connectResult.ResultCode.Should().Be(MqttClientConnectResultCode.Success,
            $"Should be able to connect to MQTT broker at {_fixture.Settings.MqttHost}:{_fixture.Settings.MqttPort}");

        await mqttClient.DisconnectAsync();
    }

    [Fact]
    public async Task MQTT_Should_Receive_Message_After_UDP_Send()
    {
        // This test demonstrates the end-to-end flow but requires MQTT write credentials

        // Arrange - Connect to MQTT
        var factory = new MqttFactory();
        using var mqttClient = factory.CreateMqttClient();
        
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_fixture.Settings.MqttHost, _fixture.Settings.MqttPort)
            .WithCleanSession()
            .Build();

        await mqttClient.ConnectAsync(options);

        // Subscribe to output topics
        var messageReceived = new TaskCompletionSource<MqttApplicationMessage>();
        
        mqttClient.ApplicationMessageReceivedAsync += args =>
        {
            messageReceived.TrySetResult(args.ApplicationMessage);
            return Task.CompletedTask;
        };

        await mqttClient.SubscribeAsync("out/#");

        // Act - Send UDP datagram
        using var udpClient = new System.Net.Sockets.UdpClient();
        var endpoint = new System.Net.IPEndPoint(
            System.Net.Dns.GetHostAddresses(_fixture.Settings.UdpHost).First(),
            _fixture.Settings.UdpPort);

        var datagram = """
        {
            "@type": "NodeUpEvent",
            "nodeCall": "MQTTTEST",
            "nodeAlias": "MQTTST",
            "locator": "IO82VJ",
            "software": "SmokeTest",
            "version": "1.0"
        }
        """;

        var bytes = Encoding.UTF8.GetBytes(datagram);
        await udpClient.SendAsync(bytes, bytes.Length, endpoint);

        // Assert - Wait for MQTT message (with timeout)
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_fixture.Settings.TestTimeoutSeconds));
        var completedTask = await Task.WhenAny(messageReceived.Task, timeoutTask);

        completedTask.Should().Be(messageReceived.Task, 
            "Should receive MQTT message within timeout period");

        var message = await messageReceived.Task;
        message.Should().NotBeNull();
        message.Topic.Should().StartWith("out/");

        await mqttClient.DisconnectAsync();
    }

    [Fact]
    public async Task MQTT_Should_Allow_Subscription_To_Input_Topics()
    {
        // Arrange
        var factory = new MqttFactory();
        using var mqttClient = factory.CreateMqttClient();
        
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_fixture.Settings.MqttHost, _fixture.Settings.MqttPort)
            .WithCleanSession()
            .Build();

        await mqttClient.ConnectAsync(options);

        // Act - Subscribe to input topics to verify they exist
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter("in/udp")
            .Build();

        var subscribeResult = await mqttClient.SubscribeAsync(subscribeOptions);

        // Assert
        subscribeResult.Items.Should().HaveCount(1);
        var items = subscribeResult.Items.ToList();
        items[0].ResultCode.Should().Be(MqttClientSubscribeResultCode.GrantedQoS0);

        await mqttClient.DisconnectAsync();
    }

    [Fact]
    public async Task MQTT_Should_Allow_Subscription_To_Error_Topics()
    {
        // Arrange
        var factory = new MqttFactory();
        using var mqttClient = factory.CreateMqttClient();
        
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_fixture.Settings.MqttHost, _fixture.Settings.MqttPort)
            .WithCleanSession()
            .Build();

        await mqttClient.ConnectAsync(options);

        // Act - Subscribe to error topics
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter("in/udp/errored/#")
            .Build();

        var subscribeResult = await mqttClient.SubscribeAsync(subscribeOptions);

        // Assert
        subscribeResult.Items.Should().HaveCount(1);
        var items = subscribeResult.Items.ToList();
        items[0].ResultCode.Should().Be(MqttClientSubscribeResultCode.GrantedQoS0);

        await mqttClient.DisconnectAsync();
    }

    [Fact]
    public async Task MQTT_Should_Allow_Subscription_To_Output_Topics()
    {
        // Arrange
        var factory = new MqttFactory();
        using var mqttClient = factory.CreateMqttClient();
        
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_fixture.Settings.MqttHost, _fixture.Settings.MqttPort)
            .WithCleanSession()
            .Build();

        await mqttClient.ConnectAsync(options);

        // Act - Subscribe to all output topics
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter("out/#")
            .Build();

        var subscribeResult = await mqttClient.SubscribeAsync(subscribeOptions);

        // Assert
        subscribeResult.Items.Should().HaveCount(1);
        var items = subscribeResult.Items.ToList();
        items[0].ResultCode.Should().Be(MqttClientSubscribeResultCode.GrantedQoS0);

        await mqttClient.DisconnectAsync();
    }

    [Fact]
    public async Task MQTT_Connection_Should_Support_Clean_Session()
    {
        // Arrange
        var factory = new MqttFactory();
        using var mqttClient = factory.CreateMqttClient();
        
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_fixture.Settings.MqttHost, _fixture.Settings.MqttPort)
            .WithCleanSession(true)
            .Build();

        // Act
        var connectResult = await mqttClient.ConnectAsync(options);

        // Assert
        connectResult.ResultCode.Should().Be(MqttClientConnectResultCode.Success);
        connectResult.IsSessionPresent.Should().BeFalse("Clean session should not have existing session");

        await mqttClient.DisconnectAsync();
    }
}
