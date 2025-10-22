using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using node_api.Models;
using node_api.Validators;
using node_api.Utilities;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace node_api.Services;

public sealed class UdpNodeInfoListener : BackgroundService, IAsyncDisposable
{
    private readonly ILogger<UdpNodeInfoListener> _logger;
    private readonly DatagramValidationService _validationService;
    private readonly Channel<(UdpNodeInfoJsonDatagram Frame, IPEndPoint RemoteEndPoint)> _processingChannel;
    private readonly ChannelWriter<(UdpNodeInfoJsonDatagram Frame, IPEndPoint RemoteEndPoint)> _channelWriter;
    private readonly SemaphoreSlim _processingSemaphore;
    private IManagedMqttClient? mqttClient;
    private UdpClient? _udpClient;
    private Task? _processingTask;

    public int Port { get; set; } = 13579;
    public int MaxConcurrentProcessing { get; set; } = 100;
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

    public UdpNodeInfoListener(
        ILogger<UdpNodeInfoListener> logger,
        DatagramValidationService validationService)
    {
        _logger = logger;
        _validationService = validationService;
        var channelOptions = new BoundedChannelOptions(MaxConcurrentProcessing * 2)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        };
        
        _processingChannel = Channel.CreateBounded<(UdpNodeInfoJsonDatagram, IPEndPoint)>(channelOptions);
        _channelWriter = _processingChannel.Writer;
        _processingSemaphore = new SemaphoreSlim(MaxConcurrentProcessing, MaxConcurrentProcessing);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        mqttClient = factory.CreateManagedMqttClient();
        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithTcpServer("node-api.packet.oarc.uk", 1883)
                .WithCredentials("writer", Environment.GetEnvironmentVariable("MQTT_WRITER_PASSWORD") ?? throw new InvalidOperationException("MQTT_WRITER_PASSWORD environment variable is not set"))
                .WithCleanSession(true)
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .Build())
            .Build();
        await mqttClient.StartAsync(options);

        // Start the frame processing task
        _processingTask = ProcessFramesAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await StartUdpListenerAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "UDP listener failed, restarting in {Delay}", ReconnectDelay);
                await Task.Delay(ReconnectDelay, stoppingToken).ConfigureAwait(false);
            }
        }

        // Signal that we're done writing to the channel
        _channelWriter.Complete();
        
        // Wait for processing to complete
        if (_processingTask is not null)
        {
            await _processingTask.ConfigureAwait(false);
        }
    }

    private async Task StartUdpListenerAsync(CancellationToken stoppingToken)
    {
        _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, Port));
        _logger.LogInformation("UDP service started listening on port {Port}", Port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await _udpClient.ReceiveAsync(stoppingToken).ConfigureAwait(false);
                _logger.LogDebug("Received datagram from {ip}", result.RemoteEndPoint);
                await ProcessDatagramAsync(result, stoppingToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
        }
    }

    const string udpTopic = "in/udp";
    const string badJsonTopic = "in/udp/errored/badjson";
    const string badTypeTopic = "in/udp/errored/badtype";
    const string validationErrorTopic = "in/udp/errored/validation";
    const string outTopicPrefix = "out";

    private async Task ProcessDatagramAsync(UdpReceiveResult result, CancellationToken stoppingToken)
    {
        string? json;
        try
        {
            json = Encoding.UTF8.GetString(result.Buffer);
            _logger.LogDebug("Received UDP datagram from {Endpoint}: {Json}", result.RemoteEndPoint, Convert.ToBase64String(result.Buffer));

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(udpTopic)
                .WithPayload(json)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            await mqttClient!.EnqueueAsync(message);

            _logger.LogDebug("Sent UDP datagram from {Endpoint} to {Topic}", result.RemoteEndPoint, udpTopic);

            if (UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var frame, out var jsonException) && frame != null)
            {
                _logger.LogDebug("Validating from {Endpoint}...", result.RemoteEndPoint);
                // Validate the deserialized datagram
                var validationResult = _validationService.Validate(frame!);

                _logger.LogDebug("Validated from {Endpoint}, isvalid:{IsValid}", result.RemoteEndPoint, validationResult.IsValid);

                if (validationResult.IsValid)
                {
                    // Only process valid datagrams
                    _logger.LogDebug("Deserialized valid datagram from {Endpoint} as type {Type}", result.RemoteEndPoint, frame.DatagramType);
                    await _channelWriter.WriteAsync((frame, result.RemoteEndPoint), stoppingToken).ConfigureAwait(false);
                }
                else
                {
                    // Map C# property names to JSON property names for validation errors
                    var datagramType = frame.GetType();
                    var mappedErrors = validationResult.Errors.Select(e => new
                    {
                        property = JsonPropertyNameMapper.GetJsonPropertyName(datagramType, e.PropertyName),
                        error = e.ErrorMessage
                    }).ToList();

                    _logger.LogWarning(
                        "Received invalid datagram from {Endpoint}. Type: {Type}. Errors: {Errors}, Datagram: {Datagram}",
                        result.RemoteEndPoint,
                        frame!.DatagramType,
                        string.Join("; ", mappedErrors.Select(e => $"{e.property}: {e.error}")),
                        Convert.ToBase64String(result.Buffer)
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
                        .Build();
                    await mqttClient!.EnqueueAsync(validationErrorMessage);

                    _logger.LogDebug("Published invalid UDP datagram from {Endpoint} to {Topic}", result.RemoteEndPoint, validationErrorTopic);
                }
            }
            else
            {
                if (jsonException is not null)
                {
                    _logger.LogWarning("Failed to deserialize JSON from {Endpoint}: {Json}  {message}. Published to {topic}", result.RemoteEndPoint, Convert.ToBase64String(result.Buffer), jsonException.Message, badJsonTopic);

                    var badJsonMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(badJsonTopic)
                        .WithPayload(JsonSerializer.SerializeToUtf8Bytes(new { error = jsonException.Message, json }))
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();

                    await mqttClient!.EnqueueAsync(badJsonMessage);
                }
                else
                {
                    _logger.LogWarning("Received unknown datagram type from {Endpoint}: {Json}", result.RemoteEndPoint, json);

                    var unknownTypeMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(badTypeTopic)
                        .WithPayload(json)
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();

                    await mqttClient!.EnqueueAsync(unknownTypeMessage);
                }
            }
        }
        catch (InvalidOperationException) when (stoppingToken.IsCancellationRequested)
        {
            // Channel is closed, ignore
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing datagram from {Endpoint}", result.RemoteEndPoint);
        }
    }

    private async Task ProcessFramesAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var (frame, remoteEndPoint) in _processingChannel.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                var wrappedFrame = new UdpNodeInfoJsonDatagramWrapper
                {
                    Datagram = frame,
                    FromIp = remoteEndPoint.Address.ToString()
                };

                // Process frames with controlled concurrency
                _ = Task.Run(async () =>
                {
                    await _processingSemaphore.WaitAsync(stoppingToken).ConfigureAwait(false);
                    try
                    {
                        await HandleFrame(wrappedFrame, remoteEndPoint, stoppingToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        _processingSemaphore.Release();
                    }
                }, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in frame processing loop");
        }
    }
    
    private static readonly JsonSerializerOptions options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    private async Task HandleFrame(UdpNodeInfoJsonDatagramWrapper wrappedFrame, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling frame of type {Type} from {RemoteEndPoint}", wrappedFrame.Datagram.DatagramType, remoteEndPoint);

        try
        {
            var frame = wrappedFrame.Datagram;

            var payload = JsonSerializer.SerializeToUtf8Bytes(frame, frame.GetType(), options);

            var topic = outTopicPrefix + "/" + wrappedFrame.Datagram.DatagramType;

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithUserProperty("type", wrappedFrame.Datagram.DatagramType)
                .Build();

            await mqttClient!.EnqueueAsync(message);

            _logger.LogDebug("EnqueueAsync frame of type {Type} from {RemoteEndPoint} to topic {topic}", wrappedFrame.Datagram.DatagramType, remoteEndPoint, topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling frame from {RemoteEndPoint}", remoteEndPoint);
        }
    }

    public override void Dispose()
    {
        _udpClient?.Close();
        _udpClient?.Dispose();
        _processingSemaphore?.Dispose();
        base.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        _channelWriter?.Complete();
        
        if (_processingTask is not null)
        {
            await _processingTask.ConfigureAwait(false);
        }
        
        _udpClient?.Close();
        _udpClient?.Dispose();
        _processingSemaphore?.Dispose();
        
        Dispose();
        GC.SuppressFinalize(this);
    }
}

class UdpNodeInfoJsonDatagramWrapper
{
    public required UdpNodeInfoJsonDatagram Datagram { get; init; }
    public required string FromIp { get; init; }
}