using MQTTnet.Extensions.ManagedClient;

namespace node_api.Services;

/// <summary>
/// Interface for providing access to the shared MQTT client
/// </summary>
public interface IMqttClientProvider
{
    /// <summary>
    /// Initialize the MQTT client
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the managed MQTT client instance
    /// </summary>
    IManagedMqttClient GetClient();
    
    /// <summary>
    /// Indicates whether the MQTT client is initialized
    /// </summary>
    bool IsInitialized { get; }
}
