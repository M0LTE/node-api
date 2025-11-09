using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using node_api.Configuration;

namespace node_api.Services;

/// <summary>
/// Provides a singleton MQTT client for the application
/// </summary>
public class MqttClientProvider : IMqttClientProvider, IAsyncDisposable
{
    private readonly ILogger<MqttClientProvider> _logger;
    private readonly MqttSettings _mqttSettings;
    private IManagedMqttClient? _mqttClient;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public MqttClientProvider(
        ILogger<MqttClientProvider> logger,
        IOptions<MqttSettings> mqttSettings)
    {
        _logger = logger;
        _mqttSettings = mqttSettings.Value;
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

            if (string.IsNullOrWhiteSpace(_mqttSettings.Password))
            {
                throw new InvalidOperationException(
                    "MQTT password is not configured. Set MqttSettings:Password in appsettings.json, " +
                    "User Secrets, or MQTT_WRITER_PASSWORD environment variable.");
            }

            var factory = new MqttFactory();
            _mqttClient = factory.CreateManagedMqttClient();
            
            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(_mqttSettings.AutoReconnectDelaySeconds))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithTcpServer(_mqttSettings.Host, _mqttSettings.Port)
                    .WithCredentials(_mqttSettings.Username, _mqttSettings.Password)
                    .WithCleanSession(_mqttSettings.CleanSession)
                    .WithProtocolVersion(MqttProtocolVersion.V500)
                    .Build())
                .Build();

            await _mqttClient.StartAsync(options);
            _isInitialized = true;
            
            _logger.LogInformation("MQTT client initialized and connected to {Host}:{Port}", 
                _mqttSettings.Host, _mqttSettings.Port);
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
