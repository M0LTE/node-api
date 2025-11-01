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
    
    private bool? _isRF;
    /// <summary>
    /// Indicates whether this link uses RF (true), internet/ethernet (false), or is unknown (null).
    /// Updated from L2Trace events that include the isRF property.
    /// </summary>
    public bool? IsRF
    {
        get => _isRF;
        set
        {
            if (_isRF != value)
            {
                _isRF = value;
                MarkDirty();
            }
        }
    }
    
    private int _flapCount;
    /// <summary>
    /// Number of connection/disconnection transitions within the current flap detection window
    /// </summary>
    public int FlapCount
    {
        get => _flapCount;
        set
        {
            if (_flapCount != value)
            {
                _flapCount = value;
                MarkDirty();
            }
        }
    }
    
    private DateTime? _flapWindowStart;
    /// <summary>
    /// Start time of the current flap detection window
    /// </summary>
    public DateTime? FlapWindowStart
    {
        get => _flapWindowStart;
        set
        {
            if (_flapWindowStart != value)
            {
                _flapWindowStart = value;
                MarkDirty();
            }
        }
    }
    
    private DateTime? _lastFlapTime;
    /// <summary>
    /// Timestamp of the most recent flap (up/down transition)
    /// </summary>
    public DateTime? LastFlapTime
    {
        get => _lastFlapTime;
        set
        {
            if (_lastFlapTime != value)
            {
                _lastFlapTime = value;
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
    
    /// <summary>
    /// Checks if this link is currently flapping based on flap count and timing
    /// </summary>
    /// <param name="flapThreshold">Minimum number of flaps to be considered flapping (default: 3)</param>
    /// <param name="windowMinutes">Time window in minutes for flap detection (default: 15)</param>
    /// <returns>True if the link is flapping, false otherwise</returns>
    public bool IsFlapping(int flapThreshold = 3, int windowMinutes = 15)
    {
        if (FlapCount < flapThreshold)
            return false;
            
        if (!FlapWindowStart.HasValue)
            return false;
            
        var windowEnd = FlapWindowStart.Value.AddMinutes(windowMinutes);
        var now = DateTime.UtcNow;
        
        // Link is flapping if we're still within the window and have enough flaps
        return now <= windowEnd && FlapCount >= flapThreshold;
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
