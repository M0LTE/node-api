using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using node_api.Models;
using System.Text.Json;

namespace node_api.Services;

/// <summary>
/// Subscribes to MQTT topics and:
/// 1. Updates the network state based on published events
/// 2. Persists traces, events, and errors to the database
/// This keeps the server-side state synchronized with the network events.
/// </summary>
public class MqttStateSubscriber : BackgroundService
{
    private readonly ILogger<MqttStateSubscriber> _logger;
    private readonly NetworkStateUpdater _networkStateUpdater;
    private readonly MySqlTraceRepository _traceRepository;
    private readonly MySqlEventRepository _eventRepository;
    private readonly MySqlErroredMessageRepository _erroredMessageRepository;
    private IManagedMqttClient? _mqttClient;

    public MqttStateSubscriber(
        ILogger<MqttStateSubscriber> logger,
        NetworkStateUpdater networkStateUpdater,
        MySqlTraceRepository traceRepository,
        MySqlEventRepository eventRepository,
        MySqlErroredMessageRepository erroredMessageRepository)
    {
        _logger = logger;
        _networkStateUpdater = networkStateUpdater;
        _traceRepository = traceRepository;
        _eventRepository = eventRepository;
        _erroredMessageRepository = erroredMessageRepository;
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

        // Subscribe to all output topics for state updates
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

        // Subscribe to error topics for database persistence
        await _mqttClient.SubscribeAsync("in/udp/errored/#");

        _logger.LogInformation("MQTT state subscriber subscribed to out/# and in/udp/errored/# topics");

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

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            var topic = args.ApplicationMessage.Topic;
            var payload = args.ApplicationMessage.ConvertPayloadToString();

            _logger.LogDebug("Received message on topic {Topic}", topic);

            // Handle error messages (database persistence only)
            if (topic.StartsWith("in/udp/errored/"))
            {
                await HandleErrorMessageAsync(topic, payload);
                return;
            }

            // Extract metadata from user properties
            var userProps = args.ApplicationMessage.UserProperties;
            var ipObfuscated = userProps.FirstOrDefault(p => p.Name == "ipObfuscated")?.Value;
            var geoCountryCode = userProps.FirstOrDefault(p => p.Name == "geoCountryCode")?.Value;
            var geoCountryName = userProps.FirstOrDefault(p => p.Name == "geoCountryName")?.Value;
            var geoCity = userProps.FirstOrDefault(p => p.Name == "geoCity")?.Value;
            var arrivalTimeStr = userProps.FirstOrDefault(p => p.Name == "arrivalTime")?.Value;

            // Parse arrival time
            DateTime? arrivalTime = null;
            if (!string.IsNullOrWhiteSpace(arrivalTimeStr) && DateTime.TryParse(arrivalTimeStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            {
                arrivalTime = parsed.Kind == DateTimeKind.Utc ? parsed : DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }

            // Parse and route to appropriate handler based on topic
            switch (topic)
            {
                case "out/NodeUpEvent":
                    var nodeUp = JsonSerializer.Deserialize<NodeUpEvent>(payload);
                    if (nodeUp != null)
                    {
                        _networkStateUpdater.UpdateFromNodeUpEvent(nodeUp);
                        UpdateNodeIpInfo(nodeUp.NodeCall, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                        await _eventRepository.InsertEventAsync(payload, arrivalTime);
                    }
                    break;

                case "out/NodeStatus":
                    var nodeStatus = JsonSerializer.Deserialize<NodeStatusReportEvent>(payload);
                    if (nodeStatus != null)
                    {
                        _networkStateUpdater.UpdateFromNodeStatus(nodeStatus);
                        UpdateNodeIpInfo(nodeStatus.NodeCall, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                        await _eventRepository.InsertEventAsync(payload, arrivalTime);
                    }
                    break;

                case "out/NodeDownEvent":
                    var nodeDown = JsonSerializer.Deserialize<NodeDownEvent>(payload);
                    if (nodeDown != null)
                    {
                        _networkStateUpdater.UpdateFromNodeDownEvent(nodeDown);
                        UpdateNodeIpInfo(nodeDown.NodeCall, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                        await _eventRepository.InsertEventAsync(payload, arrivalTime);
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
                        await _traceRepository.InsertTraceAsync(payload, arrivalTime);
                    }
                    break;

                case "out/LinkUpEvent":
                    var linkUp = JsonSerializer.Deserialize<LinkUpEvent>(payload);
                    if (linkUp != null)
                    {
                        _networkStateUpdater.UpdateFromLinkUpEvent(linkUp);
                        UpdateNodeIpInfo(linkUp.Node, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                        await _eventRepository.InsertEventAsync(payload, arrivalTime);
                    }
                    break;

                case "out/LinkStatus":
                    var linkStatus = JsonSerializer.Deserialize<Models.LinkStatus>(payload);
                    if (linkStatus != null)
                    {
                        _networkStateUpdater.UpdateFromLinkStatus(linkStatus);
                        UpdateNodeIpInfo(linkStatus.Node, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                        await _eventRepository.InsertEventAsync(payload, arrivalTime);
                    }
                    break;

                case "out/LinkDownEvent":
                    var linkDown = JsonSerializer.Deserialize<LinkDisconnectionEvent>(payload);
                    if (linkDown != null)
                    {
                        _networkStateUpdater.UpdateFromLinkDownEvent(linkDown);
                        UpdateNodeIpInfo(linkDown.Node, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                        await _eventRepository.InsertEventAsync(payload, arrivalTime);
                    }
                    break;

                case "out/CircuitUpEvent":
                    var circuitUp = JsonSerializer.Deserialize<CircuitUpEvent>(payload);
                    if (circuitUp != null)
                    {
                        _networkStateUpdater.UpdateFromCircuitUpEvent(circuitUp);
                        UpdateNodeIpInfo(circuitUp.Node, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                        await _eventRepository.InsertEventAsync(payload, arrivalTime);
                    }
                    break;

                case "out/CircuitStatus":
                    var circuitStatus = JsonSerializer.Deserialize<CircuitStatus>(payload);
                    if (circuitStatus != null)
                    {
                        _networkStateUpdater.UpdateFromCircuitStatus(circuitStatus);
                        UpdateNodeIpInfo(circuitStatus.Node, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                        await _eventRepository.InsertEventAsync(payload, arrivalTime);
                    }
                    break;

                case "out/CircuitDownEvent":
                    var circuitDown = JsonSerializer.Deserialize<CircuitDisconnectionEvent>(payload);
                    if (circuitDown != null)
                    {
                        _networkStateUpdater.UpdateFromCircuitDownEvent(circuitDown);
                        UpdateNodeIpInfo(circuitDown.Node, ipObfuscated, geoCountryCode, geoCountryName, geoCity);
                        await _eventRepository.InsertEventAsync(payload, arrivalTime);
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
    }

    private async Task HandleErrorMessageAsync(string topic, string payload)
    {
        try
        {
            var reason = topic.Split('/').Last();

            if (reason == "validation")
            {
                _logger.LogDebug("Processing validation error");
                
                var validationError = JsonSerializer.Deserialize<ValidationError>(payload);
                if (validationError != null)
                {
                    await _erroredMessageRepository.InsertErroredMessageAsync(
                        reason: reason,
                        datagram: validationError.Datagram,
                        type: validationError.Type,
                        errors: string.Join("; ", validationError.Errors.Select(e => $"{e.Property}: {e.Error}")));
                }
            }
            else
            {
                _logger.LogDebug("Processing generic errored message");
                await _erroredMessageRepository.InsertErroredMessageAsync(
                    reason: reason,
                    json: payload);
            }

            _logger.LogDebug("Saved errored message to database: {Reason}", reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save errored message to database");
        }
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

    private class ValidationError
    {
        public required string Datagram { get; init; }
        public required string Type { get; init; }
        public required List<ValidationErrorDetail> Errors { get; init; }

        public record ValidationErrorDetail
        {
            public required string Property { get; init; }
            public required string Error { get; init; }
        }
    }
}
