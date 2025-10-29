using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using node_api.Models;
using node_api.Utilities;
using node_api.Validators;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace node_api.Services;

/// <summary>
/// Shared service for processing UDP datagrams from either UDP directly or RabbitMQ
/// </summary>
public class DatagramProcessor : IDatagramProcessor
{
    private readonly ILogger<DatagramProcessor> _logger;
    private readonly DatagramValidationService _validationService;
    private readonly IUdpRateLimitService _rateLimitService;
    private readonly IGeoIpService _geoIpService;
    private readonly IManagedMqttClient _mqttClient;
    private readonly Channel<(UdpNodeInfoJsonDatagram Frame, IPEndPoint RemoteEndPoint, DateTime ArrivalTime)> _processingChannel;
    private readonly SemaphoreSlim _processingSemaphore;

    private const string udpTopic = "in/udp";
    private const string badJsonTopic = "in/udp/errored/badjson";
    private const string badTypeTopic = "in/udp/errored/badtype";
    private const string validationErrorTopic = "in/udp/errored/validation";
    private const string outTopicPrefix = "out";

    public int MaxConcurrentProcessing { get; set; } = 100;

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public DatagramProcessor(
        ILogger<DatagramProcessor> logger,
        DatagramValidationService validationService,
        IUdpRateLimitService rateLimitService,
        IGeoIpService geoIpService,
        IManagedMqttClient mqttClient)
    {
        _logger = logger;
        _validationService = validationService;
        _rateLimitService = rateLimitService;
        _geoIpService = geoIpService;
        _mqttClient = mqttClient;

        var channelOptions = new BoundedChannelOptions(MaxConcurrentProcessing * 2)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        };

        _processingChannel = Channel.CreateBounded<(UdpNodeInfoJsonDatagram, IPEndPoint, DateTime)>(channelOptions);
        _processingSemaphore = new SemaphoreSlim(MaxConcurrentProcessing, MaxConcurrentProcessing);

        // Start the frame processing task
        _ = ProcessFramesAsync();
    }

    public async Task ProcessDatagramAsync(byte[] datagram, IPAddress sourceIpAddress, DateTime arrivalTime, CancellationToken cancellationToken = default)
    {
        var remoteEndPoint = new IPEndPoint(sourceIpAddress, 0);
        string? json = null;
        string? reportingCallsign = null;

        try
        {
            json = Encoding.UTF8.GetString(datagram);

            // Try to extract reportFrom for rate limiting before full processing
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("reportFrom", out var reportFromProp))
                {
                    reportingCallsign = reportFromProp.GetString();
                }
            }
            catch
            {
                // Ignore errors in extraction, will still rate limit without callsign
            }
        }
        catch
        {
            // Can't even decode as UTF-8, still need to rate limit
        }

        // Check rate limit and blacklist first (with callsign if available)
        if (!await _rateLimitService.ShouldAllowRequestAsync(sourceIpAddress, reportingCallsign))
        {
            _logger.LogDebug("Blocked datagram from {Endpoint} (Callsign: {Callsign}) due to rate limiting or blacklist",
                remoteEndPoint, reportingCallsign ?? "Unknown");
            return;
        }

        // Continue with normal processing
        try
        {
            _logger.LogDebug("Received UDP datagram from {Endpoint}: {Json}", remoteEndPoint, Convert.ToBase64String(datagram));

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(udpTopic)
                .WithPayload(json!)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithUserProperty("arrivalTime", arrivalTime.ToString("O")) // ISO 8601 format
                .Build();
            await _mqttClient.EnqueueAsync(message);

            _logger.LogDebug("Sent UDP datagram from {Endpoint} to {Topic}", remoteEndPoint, udpTopic);

            if (UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json!, out var frame, out var jsonException) && frame != null)
            {
                _logger.LogDebug("Validating from {Endpoint}...", remoteEndPoint);
                // Validate the deserialized datagram
                var validationResult = _validationService.Validate(frame!);

                _logger.LogDebug("Validated from {Endpoint}, isvalid:{IsValid}", remoteEndPoint, validationResult.IsValid);

                if (validationResult.IsValid)
                {
                    // Only process valid datagrams
                    _logger.LogDebug("Deserialized valid datagram from {Endpoint} as type {Type}", remoteEndPoint, frame.DatagramType);
                    await _processingChannel.Writer.WriteAsync((frame, remoteEndPoint, arrivalTime), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Map C# property names to JSON property names for validation errors
                    var datagramType = frame.GetType();
                    var mappedErrors = validationResult.Errors.Select(e => new
                    {
                        property = JsonPropertyNameMapper.GetJsonPropertyName(datagramType, e.PropertyName),
                        error = JsonPropertyNameMapper.TransformErrorMessage(datagramType, e.ErrorMessage)
                    }).ToList();

                    _logger.LogWarning(
                        "Received invalid datagram from {Endpoint}. Type: {Type}. Errors: {Errors}, Datagram: {Datagram}",
                        remoteEndPoint,
                        frame!.DatagramType,
                        string.Join("; ", mappedErrors.Select(e => $"{e.property}: {e.error}")),
                        Convert.ToBase64String(datagram)
                    );

                    // Publish validation errors to MQTT for monitoring
                    var validationErrorMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(validationErrorTopic)
                        .WithPayload(JsonSerializer.SerializeToUtf8Bytes(new
                        {
                            datagram = json,
                            type = frame.DatagramType,
                            errors = mappedErrors
                        }))
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                        .WithUserProperty("arrivalTime", arrivalTime.ToString("O"))
                        .Build();
                    await _mqttClient.EnqueueAsync(validationErrorMessage);

                    _logger.LogDebug("Published invalid UDP datagram from {Endpoint} to {Topic}", remoteEndPoint, validationErrorTopic);
                }
            }
            else
            {
                if (jsonException is not null)
                {
                    _logger.LogWarning("Failed to deserialize JSON from {Endpoint}: {Json}  {message}. Published to {topic}", remoteEndPoint, Convert.ToBase64String(datagram), jsonException.Message, badJsonTopic);

                    var badJsonMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(badJsonTopic)
                        .WithPayload(JsonSerializer.SerializeToUtf8Bytes(new { error = jsonException.Message, json }))
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                        .WithUserProperty("arrivalTime", arrivalTime.ToString("O"))
                        .Build();

                    await _mqttClient.EnqueueAsync(badJsonMessage);
                }
                else
                {
                    _logger.LogWarning("Received unknown datagram type from {Endpoint}: {Json}", remoteEndPoint, json);

                    var unknownTypeMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(badTypeTopic)
                        .WithPayload(json)
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                        .WithUserProperty("arrivalTime", arrivalTime.ToString("O"))
                        .Build();

                    await _mqttClient.EnqueueAsync(unknownTypeMessage);
                }
            }
        }
        catch (InvalidOperationException) when (cancellationToken.IsCancellationRequested)
        {
            // Channel is closed, ignore
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing datagram from {Endpoint}", remoteEndPoint);
        }
    }

    private async Task ProcessFramesAsync()
    {
        try
        {
            await foreach (var (frame, remoteEndPoint, arrivalTime) in _processingChannel.Reader.ReadAllAsync())
            {
                // Process frames with controlled concurrency
                _ = Task.Run(async () =>
                {
                    await _processingSemaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        await HandleFrame(frame, remoteEndPoint, arrivalTime).ConfigureAwait(false);
                    }
                    finally
                    {
                        _processingSemaphore.Release();
                    }
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in frame processing loop");
        }
    }

    private async Task HandleFrame(UdpNodeInfoJsonDatagram frame, IPEndPoint remoteEndPoint, DateTime arrivalTime)
    {
        _logger.LogDebug("Handling frame of type {Type} from {RemoteEndPoint}", frame.DatagramType, remoteEndPoint);

        try
        {
            // IP address information will be added as user properties to MQTT message
            // This allows downstream subscribers (like MqttStateSubscriber) to update state if needed
            // without modifying in-memory state multiple times for the same event

            // Network state is now updated by MqttStateSubscriber listening to out/# topics
            // This handler just publishes to MQTT

            var payload = JsonSerializer.SerializeToUtf8Bytes(frame, frame.GetType(), jsonOptions);

            var topic = outTopicPrefix + "/" + frame.DatagramType;

            var messageBuilder = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithUserProperty("type", frame.DatagramType)
                .WithUserProperty("arrivalTime", arrivalTime.ToString("O")); // ISO 8601 format

            // Add IP address information as user properties for downstream processing
            var reportingCallsign = ExtractReportingCallsign(frame);
            if (!string.IsNullOrWhiteSpace(reportingCallsign))
            {
                messageBuilder.WithUserProperty("sourceIp", remoteEndPoint.Address.ToString());

                // Add obfuscated IP
                var obfuscatedIp = _geoIpService.ObfuscateIpAddress(remoteEndPoint.Address);
                messageBuilder.WithUserProperty("ipObfuscated", obfuscatedIp);

                // Add GeoIP info if available
                var geoInfo = _geoIpService.GetGeoIpInfo(remoteEndPoint.Address);
                if (geoInfo != null)
                {
                    if (!string.IsNullOrWhiteSpace(geoInfo.CountryCode))
                        messageBuilder.WithUserProperty("geoCountryCode", geoInfo.CountryCode);
                    if (!string.IsNullOrWhiteSpace(geoInfo.CountryName))
                        messageBuilder.WithUserProperty("geoCountryName", geoInfo.CountryName);
                    if (!string.IsNullOrWhiteSpace(geoInfo.City))
                        messageBuilder.WithUserProperty("geoCity", geoInfo.City);
                }
            }

            var message = messageBuilder.Build();

            await _mqttClient.EnqueueAsync(message);

            _logger.LogDebug("EnqueueAsync frame of type {Type} from {RemoteEndPoint} to topic {topic}", frame.DatagramType, remoteEndPoint, topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling frame from {RemoteEndPoint}", remoteEndPoint);
        }
    }

    private static string? ExtractReportingCallsign(UdpNodeInfoJsonDatagram frame)
    {
        return frame switch
        {
            L2Trace trace => trace.ReportFrom,
            NodeUpEvent nodeUp => nodeUp.NodeCall,
            NodeStatusReportEvent nodeStatus => nodeStatus.NodeCall,
            NodeDownEvent nodeDown => nodeDown.NodeCall,
            LinkUpEvent linkUp => linkUp.Node,
            Models.LinkStatus linkStatus => linkStatus.Node,
            LinkDisconnectionEvent linkDown => linkDown.Node,
            CircuitUpEvent circuitUp => circuitUp.Node,
            Models.CircuitStatus circuitStatus => circuitStatus.Node,
            CircuitDisconnectionEvent circuitDown => circuitDown.Node,
            _ => null
        };
    }
}
