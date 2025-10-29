using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using node_api.Models;
using System.Text.Json;

namespace node_api.Services;

/// <summary>
/// Subscribes to MQTT topics and updates the network state based on published events.
/// This keeps the server-side state synchronized with the network events.
/// </summary>
public class MqttStateSubscriber : BackgroundService
{
    private readonly ILogger<MqttStateSubscriber> _logger;
    private readonly NetworkStateUpdater _networkStateUpdater;
    private IManagedMqttClient? _mqttClient;

    public MqttStateSubscriber(
        ILogger<MqttStateSubscriber> logger,
        NetworkStateUpdater networkStateUpdater)
    {
        _logger = logger;
        _networkStateUpdater = networkStateUpdater;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MQTTnet.MqttFactory();
        _mqttClient = factory.CreateManagedMqttClient();

        // Set up MQTT client options
        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithTcpServer("node-api.packet.oarc.uk", 1883)
                .WithClientId($"node-api-state-subscriber-{Environment.MachineName}-{Guid.NewGuid()}")
                .WithCleanSession(false) // Persist session to avoid missing messages during reconnect
                .Build())
            .Build();

        // Set up message handler
        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

        // Connect and subscribe
        await _mqttClient.StartAsync(options);
        
        _logger.LogInformation("MQTT state subscriber connecting to broker...");

        // Subscribe to all output topics that we care about
        await _mqttClient.SubscribeAsync(new[]
        {
            new MqttTopicFilterBuilder().WithTopic("out/NodeUpEvent").Build(),
            new MqttTopicFilterBuilder().WithTopic("out/NodeStatus").Build(),
            new MqttTopicFilterBuilder().WithTopic("out/NodeDownEvent").Build(),
            new MqttTopicFilterBuilder().WithTopic("out/L2Trace").Build(),
            new MqttTopicFilterBuilder().WithTopic("out/LinkUpEvent").Build(),
            new MqttTopicFilterBuilder().WithTopic("out/LinkStatus").Build(),
            new MqttTopicFilterBuilder().WithTopic("out/LinkDownEvent").Build(),
            new MqttTopicFilterBuilder().WithTopic("out/CircuitUpEvent").Build(),
            new MqttTopicFilterBuilder().WithTopic("out/CircuitStatus").Build(),
            new MqttTopicFilterBuilder().WithTopic("out/CircuitDownEvent").Build(),
        });

        _logger.LogInformation("MQTT state subscriber subscribed to out/# topics");

        // Keep the service running
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MQTT state subscriber stopping...");
        }
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            var topic = args.ApplicationMessage.Topic;
            var payload = args.ApplicationMessage.ConvertPayloadToString();

            _logger.LogDebug("Received message on topic {Topic}", topic);

            // Extract IP address information from user properties if available
            var userProps = args.ApplicationMessage.UserProperties;
            var ipObfuscated = userProps.FirstOrDefault(p => p.Name == "ipObfuscated")?.Value;
            var geoCountryCode = userProps.FirstOrDefault(p => p.Name == "geoCountryCode")?.Value;
            var geoCountryName = userProps.FirstOrDefault(p => p.Name == "geoCountryName")?.Value;
            var geoCity = userProps.FirstOrDefault(p => p.Name == "geoCity")?.Value;

            // Parse and route to appropriate handler based on topic
            switch (topic)
            {
                case "out/NodeUpEvent":
                    var nodeUp = JsonSerializer.Deserialize<NodeUpEvent>(payload);
                    if (nodeUp != null)
                    {
                        _networkStateUpdater.UpdateFromNodeUpEvent(nodeUp);
                        UpdateNodeIpInfo(nodeUp.NodeCall, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                    }
                    break;

                case "out/NodeStatus":
                    var nodeStatus = JsonSerializer.Deserialize<NodeStatusReportEvent>(payload);
                    if (nodeStatus != null)
                    {
                        _networkStateUpdater.UpdateFromNodeStatus(nodeStatus);
                        UpdateNodeIpInfo(nodeStatus.NodeCall, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                    }
                    break;

                case "out/NodeDownEvent":
                    var nodeDown = JsonSerializer.Deserialize<NodeDownEvent>(payload);
                    if (nodeDown != null)
                    {
                        _networkStateUpdater.UpdateFromNodeDownEvent(nodeDown);
                        UpdateNodeIpInfo(nodeDown.NodeCall, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                    }
                    break;

                case "out/L2Trace":
                    var l2Trace = JsonSerializer.Deserialize<L2Trace>(payload);
                    if (l2Trace != null)
                    {
                        _networkStateUpdater.UpdateFromL2Trace(l2Trace);
                        if (l2Trace.ReportFrom != null)
                        {
                            UpdateNodeIpInfo(l2Trace.ReportFrom, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                        }
                    }
                    break;

                case "out/LinkUpEvent":
                    var linkUp = JsonSerializer.Deserialize<LinkUpEvent>(payload);
                    if (linkUp != null)
                    {
                        _networkStateUpdater.UpdateFromLinkUpEvent(linkUp);
                        UpdateNodeIpInfo(linkUp.Node, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                    }
                    break;

                case "out/LinkStatus":
                    var linkStatus = JsonSerializer.Deserialize<Models.LinkStatus>(payload);
                    if (linkStatus != null)
                    {
                        _networkStateUpdater.UpdateFromLinkStatus(linkStatus);
                        UpdateNodeIpInfo(linkStatus.Node, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                    }
                    break;

                case "out/LinkDownEvent":
                    var linkDown = JsonSerializer.Deserialize<LinkDisconnectionEvent>(payload);
                    if (linkDown != null)
                    {
                        _networkStateUpdater.UpdateFromLinkDownEvent(linkDown);
                        UpdateNodeIpInfo(linkDown.Node, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                    }
                    break;

                case "out/CircuitUpEvent":
                    var circuitUp = JsonSerializer.Deserialize<CircuitUpEvent>(payload);
                    if (circuitUp != null)
                    {
                        _networkStateUpdater.UpdateFromCircuitUpEvent(circuitUp);
                        UpdateNodeIpInfo(circuitUp.Node, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                    }
                    break;

                case "out/CircuitStatus":
                    var circuitStatus = JsonSerializer.Deserialize<CircuitStatus>(payload);
                    if (circuitStatus != null)
                    {
                        _networkStateUpdater.UpdateFromCircuitStatus(circuitStatus);
                        UpdateNodeIpInfo(circuitStatus.Node, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                    }
                    break;

                case "out/CircuitDownEvent":
                    var circuitDown = JsonSerializer.Deserialize<CircuitDisconnectionEvent>(payload);
                    if (circuitDown != null)
                    {
                        _networkStateUpdater.UpdateFromCircuitDownEvent(circuitDown);
                        UpdateNodeIpInfo(circuitDown.Node, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                    }
                    break;

                default:
                    _logger.LogDebug("Ignoring message on topic {Topic}", topic);
                    break;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize message from topic {Topic}", args.ApplicationMessage.Topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from topic {Topic}", args.ApplicationMessage.Topic);
        }

        return Task.CompletedTask;
    }

    private void UpdateNodeIpInfo(string callsign, string? ipObfuscated, string? geoCountryCode, string? geoCountryName, string? geoCity)
    {
        if (string.IsNullOrWhiteSpace(callsign))
            return;

        // Only update if we have IP information to set
        if (string.IsNullOrWhiteSpace(ipObfuscated))
            return;

        _networkStateUpdater.UpdateNodeIpInfo(callsign, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MQTT state subscriber stopping...");
        
        if (_mqttClient != null)
        {
            await _mqttClient.StopAsync();
            _mqttClient.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}
