namespace node_api.Models.NetworkState;

/// <summary>
/// Represents the current state of a node in the network
/// </summary>
public class NodeState
{
    public required string Callsign { get; init; }
    
    private string? _alias;
    public string? Alias
    {
        get => _alias;
        set
        {
            if (_alias != value)
            {
                _alias = value;
                MarkDirty();
            }
        }
    }
    
    private string? _locator;
    public string? Locator
    {
        get => _locator;
        set
        {
            if (_locator != value)
            {
                _locator = value;
                MarkDirty();
            }
        }
    }
    
    private decimal? _latitude;
    public decimal? Latitude
    {
        get => _latitude;
        set
        {
            if (_latitude != value)
            {
                _latitude = value;
                MarkDirty();
            }
        }
    }
    
    private decimal? _longitude;
    public decimal? Longitude
    {
        get => _longitude;
        set
        {
            if (_longitude != value)
            {
                _longitude = value;
                MarkDirty();
            }
        }
    }
    
    private string? _software;
    public string? Software
    {
        get => _software;
        set
        {
            if (_software != value)
            {
                _software = value;
                MarkDirty();
            }
        }
    }
    
    private string? _version;
    public string? Version
    {
        get => _version;
        set
        {
            if (_version != value)
            {
                _version = value;
                MarkDirty();
            }
        }
    }
    
    private int? _uptimeSecs;
    public int? UptimeSecs
    {
        get => _uptimeSecs;
        set
        {
            if (_uptimeSecs != value)
            {
                _uptimeSecs = value;
                MarkDirty();
            }
        }
    }
    
    private int? _linksIn;
    public int? LinksIn
    {
        get => _linksIn;
        set
        {
            if (_linksIn != value)
            {
                _linksIn = value;
                MarkDirty();
            }
        }
    }
    
    private int? _linksOut;
    public int? LinksOut
    {
        get => _linksOut;
        set
        {
            if (_linksOut != value)
            {
                _linksOut = value;
                MarkDirty();
            }
        }
    }
    
    private int? _circuitsIn;
    public int? CircuitsIn
    {
        get => _circuitsIn;
        set
        {
            if (_circuitsIn != value)
            {
                _circuitsIn = value;
                MarkDirty();
            }
        }
    }
    
    private int? _circuitsOut;
    public int? CircuitsOut
    {
        get => _circuitsOut;
        set
        {
            if (_circuitsOut != value)
            {
                _circuitsOut = value;
                MarkDirty();
            }
        }
    }
    
    private int? _l3Relayed;
    public int? L3Relayed
    {
        get => _l3Relayed;
        set
        {
            if (_l3Relayed != value)
            {
                _l3Relayed = value;
                MarkDirty();
            }
        }
    }
    
    private NodeStatus _status = NodeStatus.Unknown;
    public NodeStatus Status
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
    
    private DateTime? _lastSeen;
    public DateTime? LastSeen
    {
        get => _lastSeen;
        set
        {
            if (_lastSeen != value)
            {
                _lastSeen = value;
                MarkDirty();
            }
        }
    }
    
    private DateTime? _firstSeen;
    public DateTime? FirstSeen
    {
        get => _firstSeen;
        set
        {
            if (_firstSeen != value)
            {
                _firstSeen = value;
                MarkDirty();
            }
        }
    }
    
    private DateTime? _lastStatusUpdate;
    public DateTime? LastStatusUpdate
    {
        get => _lastStatusUpdate;
        set
        {
            if (_lastStatusUpdate != value)
            {
                _lastStatusUpdate = value;
                MarkDirty();
            }
        }
    }
    
    private DateTime? _lastUpEvent;
    public DateTime? LastUpEvent
    {
        get => _lastUpEvent;
        set
        {
            if (_lastUpEvent != value)
            {
                _lastUpEvent = value;
                MarkDirty();
            }
        }
    }
    
    private DateTime? _lastDownEvent;
    public DateTime? LastDownEvent
    {
        get => _lastDownEvent;
        set
        {
            if (_lastDownEvent != value)
            {
                _lastDownEvent = value;
                MarkDirty();
            }
        }
    }
    
    private int _l2TraceCount;
    public int L2TraceCount
    {
        get => _l2TraceCount;
        set
        {
            if (_l2TraceCount != value)
            {
                _l2TraceCount = value;
                MarkDirty();
            }
        }
    }
    
    private DateTime? _lastL2Trace;
    public DateTime? LastL2Trace
    {
        get => _lastL2Trace;
        set
        {
            if (_lastL2Trace != value)
            {
                _lastL2Trace = value;
                MarkDirty();
            }
        }
    }
    
    /// <summary>
    /// Last known IP address (last two octets only for IPv4, or last half for IPv6)
    /// </summary>
    private string? _ipAddressObfuscated;
    public string? IpAddressObfuscated
    {
        get => _ipAddressObfuscated;
        set
        {
            if (_ipAddressObfuscated != value)
            {
                _ipAddressObfuscated = value;
                MarkDirty();
            }
        }
    }
    
    /// <summary>
    /// GeoIP country code (e.g., "GB", "US")
    /// </summary>
    private string? _geoIpCountryCode;
    public string? GeoIpCountryCode
    {
        get => _geoIpCountryCode;
        set
        {
            if (_geoIpCountryCode != value)
            {
                _geoIpCountryCode = value;
                MarkDirty();
            }
        }
    }
    
    /// <summary>
    /// GeoIP country name (e.g., "United Kingdom", "United States")
    /// </summary>
    private string? _geoIpCountryName;
    public string? GeoIpCountryName
    {
        get => _geoIpCountryName;
        set
        {
            if (_geoIpCountryName != value)
            {
                _geoIpCountryName = value;
                MarkDirty();
            }
        }
    }
    
    /// <summary>
    /// GeoIP city (e.g., "London", "New York")
    /// </summary>
    private string? _geoIpCity;
    public string? GeoIpCity
    {
        get => _geoIpCity;
        set
        {
            if (_geoIpCity != value)
            {
                _geoIpCity = value;
                MarkDirty();
            }
        }
    }
    
    /// <summary>
    /// Last time the IP address was updated
    /// </summary>
    private DateTime? _lastIpUpdate;
    public DateTime? LastIpUpdate
    {
        get => _lastIpUpdate;
        set
        {
            if (_lastIpUpdate != value)
            {
                _lastIpUpdate = value;
                MarkDirty();
            }
        }
    }
    
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

public enum NodeStatus
{
    Unknown,
    Online,
    Offline
}
