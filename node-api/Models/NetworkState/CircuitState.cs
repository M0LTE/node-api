namespace node_api.Models.NetworkState;

/// <summary>
/// Represents the current state of a NetROM Layer 4 circuit in the network
/// </summary>
public class CircuitState
{
    public required string CanonicalKey { get; init; }
    public required string Endpoint1 { get; init; } // Always the lexically smaller address
    public required string Endpoint2 { get; init; } // Always the lexically larger address
    public CircuitStatus Status { get; set; } = CircuitStatus.Active;
    public DateTime ConnectedAt { get; set; }
    public DateTime? DisconnectedAt { get; set; }
    public DateTime LastUpdate { get; set; }
    public string? Initiator { get; set; }
    public Dictionary<string, CircuitEndpointState> Endpoints { get; init; } = new();
}

public class CircuitEndpointState
{
    public required string Node { get; init; }
    public required int Id { get; init; }
    public required string Direction { get; init; }
    public int? Service { get; set; }
    public required string Remote { get; init; }
    public required string Local { get; init; }
    public DateTime LastUpdate { get; set; }
    public int? SegmentsSent { get; set; }
    public int? SegmentsReceived { get; set; }
    public int? SegmentsResent { get; set; }
    public int? SegmentsQueued { get; set; }
    public int? BytesSent { get; set; }
    public int? BytesReceived { get; set; }
    public string? Reason { get; set; }
}

public enum CircuitStatus
{
    Active,
    Disconnected
}
