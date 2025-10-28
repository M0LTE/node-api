using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;

namespace node_api.Services;

/// <summary>
/// Provides a singleton MQTT client for the application
/// </summary>
public class MqttClientProvider : IMqttClientProvider, IAsyncDisposable
{
    private readonly ILogger<MqttClientProvider> _logger;
    private IManagedMqttClient? _mqttClient;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public MqttClientProvider(ILogger<MqttClientProvider> logger)
    {
        _logger = logger;
    }

    public IManagedMqttClient GetClient()
    {
        if (_mqttClient == null)
        {
            throw new InvalidOperationException("MQTT client has not been initialized. Call InitializeAsync first.");
        }
        return _mqttClient;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized) // Double-check after acquiring lock
                return;

            var factory = new MqttFactory();
            _mqttClient = factory.CreateManagedMqttClient();
            
            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithTcpServer("node-api.packet.oarc.uk", 1883)
                    .WithCredentials("writer", Environment.GetEnvironmentVariable("MQTT_WRITER_PASSWORD") 
                        ?? throw new InvalidOperationException("MQTT_WRITER_PASSWORD environment variable is not set"))
                    .WithCleanSession(true)
                    .WithProtocolVersion(MqttProtocolVersion.V500)
                    .Build())
                .Build();

            await _mqttClient.StartAsync(options);
            _isInitialized = true;
            
            _logger.LogInformation("MQTT client initialized and connected");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_mqttClient != null)
        {
            await _mqttClient.StopAsync();
            _mqttClient.Dispose();
        }
        _initLock.Dispose();
    }
}
