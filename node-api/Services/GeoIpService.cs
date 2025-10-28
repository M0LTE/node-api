using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Responses;
using System.Net;

namespace node_api.Services;

/// <summary>
/// Service for GeoIP lookups using MaxMind GeoLite2 database
/// </summary>
public interface IGeoIpService
{
    /// <summary>
    /// Get GeoIP information for an IP address
    /// </summary>
    GeoIpInfo? GetGeoIpInfo(IPAddress ipAddress);
    
    /// <summary>
    /// Obfuscate an IP address (show only last two octets for IPv4)
    /// </summary>
    string ObfuscateIpAddress(IPAddress ipAddress);
}

/// <summary>
/// GeoIP information for an IP address
/// </summary>
public record GeoIpInfo
{
    public string? CountryCode { get; init; }
    public string? CountryName { get; init; }
    public string? City { get; init; }
}

/// <summary>
/// Implementation of GeoIP service using MaxMind GeoLite2
/// </summary>
public class GeoIpService : IGeoIpService, IDisposable
{
    private readonly DatabaseReader? _reader;
    private readonly ILogger<GeoIpService> _logger;

    public GeoIpService(ILogger<GeoIpService> logger)
    {
        _logger = logger;
        
        // Try to load GeoLite2 database from standard locations
        var possiblePaths = new[]
        {
            "/usr/share/GeoIP/GeoLite2-City.mmdb",           // Linux standard location
            "/var/lib/GeoIP/GeoLite2-City.mmdb",             // Alternative Linux location
            "GeoLite2-City.mmdb",                            // Current directory
            Path.Combine(AppContext.BaseDirectory, "GeoLite2-City.mmdb"), // App directory
            Environment.GetEnvironmentVariable("GEOIP_DB_PATH") ?? ""     // Environment variable
        };

        foreach (var path in possiblePaths.Where(p => !string.IsNullOrEmpty(p)))
        {
            if (File.Exists(path))
            {
                try
                {
                    _reader = new DatabaseReader(path);
                    _logger.LogInformation("Loaded GeoIP database from {Path}", path);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load GeoIP database from {Path}", path);
                }
            }
        }

        _logger.LogWarning("GeoIP database not found. GeoIP lookups will be disabled. " +
            "To enable, download GeoLite2-City.mmdb from MaxMind and place it in a standard location.");
    }

    public GeoIpInfo? GetGeoIpInfo(IPAddress ipAddress)
    {
        if (_reader == null)
            return null;

        try
        {
            if (_reader.TryCity(ipAddress, out CityResponse? response) && response != null)
            {
                return new GeoIpInfo
                {
                    CountryCode = response.Country?.IsoCode,
                    CountryName = response.Country?.Name,
                    City = response.City?.Name
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GeoIP lookup failed for {IpAddress}", ipAddress);
        }

        return null;
    }

    public string ObfuscateIpAddress(IPAddress ipAddress)
    {
        var ip = ipAddress.ToString();

        // IPv4: Show only last two octets (e.g., 192.168.1.100 -> ***.***. 1.100)
        if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var parts = ip.Split('.');
            if (parts.Length == 4)
            {
                return $"***.***. {parts[2]}.{parts[3]}";
            }
        }
        // IPv6: Show only last half (e.g., 2001:db8::1 -> ****:****::1)
        else if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            var parts = ip.Split(':');
            if (parts.Length >= 2)
            {
                var redacted = new string('*', 4) + ":" + new string('*', 4);
                return redacted + ":" + string.Join(":", parts.Skip(2));
            }
        }

        return ip; // Fallback
    }

    public void Dispose()
    {
        _reader?.Dispose();
    }
}
