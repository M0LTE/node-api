namespace node_api.Models;

/// <summary>
/// Configuration settings for UDP rate limiting
/// </summary>
public class UdpRateLimitSettings
{
    /// <summary>
    /// Maximum requests per second allowed per IP address. Default is 10.
    /// </summary>
    public int RequestsPerSecondPerIp { get; set; } = 10;

    /// <summary>
    /// List of IP addresses or CIDR ranges to blacklist (e.g., "192.168.1.0/24", "10.0.0.1")
    /// </summary>
    public string[] Blacklist { get; set; } = Array.Empty<string>();
}
