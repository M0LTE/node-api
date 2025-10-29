namespace node_api.Models.NetworkState;

/// <summary>
/// Represents the current state of a link in the network
/// </summary>
public class LinkState
{
    public required string CanonicalKey { get; init; }
    public required string Endpoint1 { get; init; } // Always the lexically smaller callsign
    public required string Endpoint2 { get; init; } // Always the lexically larger callsign
    
    private LinkStatus _status = LinkStatus.Active;
    public LinkStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                MarkDirty();
            }
        }
    }
    
    private DateTime _connectedAt;
    public DateTime ConnectedAt
    {
        get => _connectedAt;
        set
        {
            if (_connectedAt != value)
            {
                _connectedAt = value;
                MarkDirty();
            }
        }
    }
    
    private DateTime? _disconnectedAt;
    public DateTime? DisconnectedAt
    {
        get => _disconnectedAt;
        set
        {
            if (_disconnectedAt != value)
            {
                _disconnectedAt = value;
                MarkDirty();
            }
        }
    }
    
    private DateTime _lastUpdate;
    public DateTime LastUpdate
    {
        get => _lastUpdate;
        set
        {
            if (_lastUpdate != value)
            {
                _lastUpdate = value;
                MarkDirty();
            }
        }
    }
    
    private string? _initiator;
    public string? Initiator
    {
        get => _initiator;
        set
        {
            if (_initiator != value)
            {
                _initiator = value;
                MarkDirty();
            }
        }
    }
    
    public Dictionary<string, LinkEndpointState> Endpoints { get; init; } = new();
    
    // Dirty tracking for persistence optimization
    public bool IsDirty { get; private set; }
    
    public void MarkDirty()
    {
        IsDirty = true;
    }
    
    public void MarkClean()
    {
        IsDirty = false;
    }
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
