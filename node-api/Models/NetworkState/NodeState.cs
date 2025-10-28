namespace node_api.Models.NetworkState;

/// <summary>
/// Represents the current state of a node in the network
/// </summary>
public class NodeState
{
    public required string Callsign { get; init; }
    public string? Alias { get; set; }
    public string? Locator { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Software { get; set; }
    public string? Version { get; set; }
    public int? UptimeSecs { get; set; }
    public int? LinksIn { get; set; }
    public int? LinksOut { get; set; }
    public int? CircuitsIn { get; set; }
    public int? CircuitsOut { get; set; }
    public int? L3Relayed { get; set; }
    public NodeStatus Status { get; set; } = NodeStatus.Unknown;
    public DateTime? LastSeen { get; set; }
    public DateTime? FirstSeen { get; set; }
    public DateTime? LastStatusUpdate { get; set; }
    public DateTime? LastUpEvent { get; set; }
    public DateTime? LastDownEvent { get; set; }
    public int L2TraceCount { get; set; }
    public DateTime? LastL2Trace { get; set; }
    
    /// <summary>
    /// Last known IP address (last two octets only for IPv4, or last half for IPv6)
    /// </summary>
    public string? IpAddressObfuscated { get; set; }
    
    /// <summary>
    /// GeoIP country code (e.g., "GB", "US")
    /// </summary>
    public string? GeoIpCountryCode { get; set; }
    
    /// <summary>
    /// GeoIP country name (e.g., "United Kingdom", "United States")
    /// </summary>
    public string? GeoIpCountryName { get; set; }
    
    /// <summary>
    /// GeoIP city (e.g., "London", "New York")
    /// </summary>
    public string? GeoIpCity { get; set; }
    
    /// <summary>
    /// Last time the IP address was updated
    /// </summary>
    public DateTime? LastIpUpdate { get; set; }
}

public enum NodeStatus
{
    Unknown,
    Online,
    Offline
}
