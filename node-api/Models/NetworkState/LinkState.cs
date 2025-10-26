namespace node_api.Models.NetworkState;

/// <summary>
/// Represents the current state of a link in the network
/// </summary>
public class LinkState
{
    public required string CanonicalKey { get; init; }
    public required string Endpoint1 { get; init; } // Always the lexically smaller callsign
    public required string Endpoint2 { get; init; } // Always the lexically larger callsign
    public LinkStatus Status { get; set; } = LinkStatus.Active;
    public DateTime ConnectedAt { get; set; }
    public DateTime? DisconnectedAt { get; set; }
    public DateTime LastUpdate { get; set; }
    public string? Initiator { get; set; }
    public Dictionary<string, LinkEndpointState> Endpoints { get; init; } = new();
}

public class LinkEndpointState
{
    public required string Node { get; init; }
    public required int Id { get; init; }
    public required string Direction { get; init; }
    public required string Port { get; init; }
    public required string Local { get; init; }
    public required string Remote { get; init; }
    public DateTime LastUpdate { get; set; }
    public int? UpForSecs { get; set; }
    public int? FramesSent { get; set; }
    public int? FramesReceived { get; set; }
    public int? FramesResent { get; set; }
    public int? FramesQueued { get; set; }
    public int? FramesQueuedPeak { get; set; }
    public int? BytesSent { get; set; }
    public int? BytesReceived { get; set; }
    public int? BpsTxMean { get; set; }
    public int? BpsRxMean { get; set; }
    public int? FrameQueueMax { get; set; }
    public int? L2RttMs { get; set; }
    public string? Reason { get; set; }
}

public enum LinkStatus
{
    Active,
    Disconnected
}
