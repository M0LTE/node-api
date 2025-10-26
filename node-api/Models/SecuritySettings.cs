namespace node_api.Models;

public class SecuritySettings
{
    public List<string> UdpAllowedSourceNetworks { get; set; } = new() { "0.0.0.0/0" };
    public ApiRateLimitingSettings ApiRateLimiting { get; set; } = new();
    public UdpRateLimitingSettings UdpRateLimiting { get; set; } = new();
    public List<string> CorsAllowedOrigins { get; set; } = new() { "*" };
}

public class ApiRateLimitingSettings
{
    public bool Enabled { get; set; } = true;
    public int PermitLimit { get; set; } = 100;
    public int Window { get; set; } = 60;
}

public class UdpRateLimitingSettings
{
    public bool Enabled { get; set; } = true;
    public int MaxPacketsPerSecondPerIp { get; set; } = 10;
    public int MaxTotalPacketsPerSecond { get; set; } = 1000;
}
