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
}

public enum NodeStatus
{
    Unknown,
    Online,
    Offline
}
