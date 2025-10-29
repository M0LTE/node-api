namespace node_api.Models.NetworkState;

/// <summary>
/// Represents the current state of a NetROM Layer 4 circuit in the network
/// </summary>
public class CircuitState
{
    public required string CanonicalKey { get; init; }
    public required string Endpoint1 { get; init; } // Always the lexically smaller address
    public required string Endpoint2 { get; init; } // Always the lexically larger address
    
    private CircuitStatus _status = CircuitStatus.Active;
    public CircuitStatus Status
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
    
    public Dictionary<string, CircuitEndpointState> Endpoints { get; init; } = new();
    
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
