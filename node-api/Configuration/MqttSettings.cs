namespace node_api.Configuration;

/// <summary>
/// Configuration settings for MQTT connection
/// </summary>
public class MqttSettings
{
    /// <summary>
    /// MQTT broker hostname
    /// </summary>
    public string Host { get; set; } = "node-api.packet.oarc.uk";

    /// <summary>
    /// MQTT broker port
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// MQTT username for write access
    /// </summary>
    public string Username { get; set; } = "writer";

    /// <summary>
    /// MQTT password for write access (should be set via User Secrets or environment variable)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Client ID prefix for MQTT connections
    /// </summary>
    public string ClientIdPrefix { get; set; } = "node-api";

    /// <summary>
    /// Auto-reconnect delay in seconds
    /// </summary>
    public int AutoReconnectDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Whether to use clean session (true) or persistent session (false)
    /// </summary>
    public bool CleanSession { get; set; } = true;
}
