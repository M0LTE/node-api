using node_api.Models;
using node_api.Models.NetworkState;
using System.Text.RegularExpressions;

namespace node_api.Services;

/// <summary>
/// Updates the network state based on incoming events from MQTT/UDP
/// </summary>
public partial class NetworkStateUpdater : IHostedService
{
    private readonly INetworkStateService _networkState;
    private readonly ILogger<NetworkStateUpdater> _logger;

    public NetworkStateUpdater(
        INetworkStateService networkState,
        ILogger<NetworkStateUpdater> logger)
    {
        _networkState = networkState;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Network state updater started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Network state updater stopped");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Converts a decimal Unix timestamp (seconds since 1970-01-01, with optional millisecond fraction)
    /// to a DateTime with millisecond precision.
    /// </summary>
    private static DateTime UnixSecondsToDateTime(decimal unixSeconds)
    {
        // Split into whole seconds and fractional milliseconds
        var wholeSeconds = (long)unixSeconds;
        var fractionalPart = unixSeconds - wholeSeconds;
        var milliseconds = (int)(fractionalPart * 1000m);
        
        // Create DateTimeOffset from whole seconds, then add milliseconds
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(wholeSeconds).UtcDateTime;
        return dateTime.AddMilliseconds(milliseconds);
    }

    public void UpdateFromNodeUpEvent(NodeUpEvent evt)
    {
        var node = _networkState.GetOrCreateNode(evt.NodeCall);
        
        // Mark as reporting node (sends UDP telemetry)
        node.IsReportingNode = true;
        
        node.Alias = evt.NodeAlias;
        node.Locator = evt.Locator;
        node.Latitude = evt.Latitude;
        node.Longitude = evt.Longitude;
        node.Software = evt.Software;
        node.Version = evt.Version;
        node.Status = NodeStatus.Online;
        node.LastSeen = DateTime.UtcNow;
        node.LastUpEvent = DateTime.UtcNow;
        
        // NOTE: IsCb and IsTest are NOT updated from events - they are administrative fields
        // set manually via database or admin API, and must be preserved across telemetry updates

        _logger.LogDebug("Updated node state from NodeUpEvent: {Callsign}", evt.NodeCall);
    }

    public void UpdateFromNodeStatus(NodeStatusReportEvent evt)
    {
        var node = _networkState.GetOrCreateNode(evt.NodeCall);
        
        // DEBUG: Log current values before update
        _logger.LogInformation(
            "UpdateFromNodeStatus for {Callsign}: IsCb={IsCb}, IsTest={IsTest} BEFORE update", 
            evt.NodeCall, node.IsCb, node.IsTest);
        
        // Mark as reporting node (sends UDP telemetry)
        node.IsReportingNode = true;
        
        node.Alias = evt.NodeAlias;
        node.Locator = evt.Locator;
        node.Latitude = evt.Latitude;
        node.Longitude = evt.Longitude;
        node.Software = evt.Software;
        node.Version = evt.Version;
        node.UptimeSecs = evt.UptimeSecs;
        node.LinksIn = evt.LinksIn;
        node.LinksOut = evt.LinksOut;
        node.CircuitsIn = evt.CircuitsIn;
        node.CircuitsOut = evt.CircuitsOut;
        node.L3Relayed = evt.L3Relayed;
        node.Status = NodeStatus.Online;
        node.LastSeen = DateTime.UtcNow;
        node.LastStatusUpdate = DateTime.UtcNow;
        
        // DEBUG: Log values after update
        _logger.LogInformation(
            "UpdateFromNodeStatus for {Callsign}: IsCb={IsCb}, IsTest={IsTest} AFTER update", 
            evt.NodeCall, node.IsCb, node.IsTest);
        
        // NOTE: IsCb and IsTest are NOT updated from events - they are administrative fields
        // set manually via database or admin API, and must be preserved across telemetry updates

        _logger.LogDebug("Updated node state from NodeStatus: {Callsign}", evt.NodeCall);
    }

    public void UpdateFromNodeDownEvent(NodeDownEvent evt)
    {
        var node = _networkState.GetOrCreateNode(evt.NodeCall);
        
        node.Alias = evt.NodeAlias;
        node.Status = NodeStatus.Offline;
        node.LastSeen = DateTime.UtcNow;
        node.LastDownEvent = DateTime.UtcNow;
        
        // NOTE: IsCb and IsTest are NOT updated from events - they are administrative fields
        // set manually via database or admin API, and must be preserved across telemetry updates

        _logger.LogDebug("Updated node state from NodeDownEvent: {Callsign}", evt.NodeCall);
    }

    public void UpdateFromLinkUpEvent(LinkUpEvent evt)
    {
        var link = _networkState.GetOrCreateLink(evt.Local, evt.Remote);
        
        // Track flapping: if link was previously disconnected, this is a flap
        var wasDisconnected = link.Status == Models.NetworkState.LinkStatus.Disconnected;
        
        link.Status = Models.NetworkState.LinkStatus.Active;
        link.LastUpdate = DateTime.UtcNow;
        
        if (evt.TimeUnixSeconds.HasValue)
        {
            link.ConnectedAt = UnixSecondsToDateTime(evt.TimeUnixSeconds.Value);
        }

        var endpoint = new LinkEndpointState
        {
            Node = evt.Node,
            Id = evt.Id,
            Direction = evt.Direction,
            Port = evt.Port,
            Local = evt.Local,
            Remote = evt.Remote,
            LastUpdate = DateTime.UtcNow
        };
        
        link.Endpoints[evt.Node] = endpoint;
        link.MarkDirty(); // Explicitly mark dirty when modifying Endpoints

        if (link.Initiator == null)
        {
            link.Initiator = evt.Direction.Equals("outgoing", StringComparison.OrdinalIgnoreCase) 
                ? evt.Local 
                : evt.Remote;
        }
        
        // Update flap tracking
        if (wasDisconnected)
        {
            TrackLinkFlap(link);
        }

        _logger.LogDebug("Updated link state from LinkUpEvent: {Key} ({Local} <-> {Remote})", link.CanonicalKey, evt.Local, evt.Remote);
    }

    public void UpdateFromLinkStatus(Models.LinkStatus evt)
    {
        var link = _networkState.GetOrCreateLink(evt.Local, evt.Remote);
        
        link.Status = Models.NetworkState.LinkStatus.Active;
        link.LastUpdate = DateTime.UtcNow;

        if (evt.TimeUnixSeconds.HasValue && evt.UpForSecs.HasValue)
        {
            link.ConnectedAt = UnixSecondsToDateTime(evt.TimeUnixSeconds.Value - evt.UpForSecs.Value);
        }

        var endpoint = new LinkEndpointState
        {
            Node = evt.Node,
            Id = evt.Id,
            Direction = evt.Direction,
            Port = evt.Port,
            Local = evt.Local,
            Remote = evt.Remote,
            LastUpdate = DateTime.UtcNow,
            UpForSecs = evt.UpForSecs,
            FramesSent = evt.FramesSent,
            FramesReceived = evt.FramesReceived,
            FramesResent = evt.FramesResent,
            FramesQueued = evt.FramesQueued,
            FramesQueuedPeak = evt.FramesQueuedPeak,
            BytesSent = evt.BytesSent,
            BytesReceived = evt.BytesReceived,
            BpsTxMean = evt.BpsTxMean,
            BpsRxMean = evt.BpsRxMean,
            FrameQueueMax = evt.FrameQueueMax,
            L2RttMs = evt.L2RttMs
        };
        
        link.Endpoints[evt.Node] = endpoint;
        link.MarkDirty(); // Explicitly mark dirty when modifying Endpoints

        if (link.Initiator == null)
        {
            link.Initiator = evt.Direction.Equals("outgoing", StringComparison.OrdinalIgnoreCase) 
                ? evt.Local 
                : evt.Remote;
        }

        _logger.LogDebug("Updated link state from LinkStatus: {Key} ({Local} <-> {Remote})", link.CanonicalKey, evt.Local, evt.Remote);
    }

    public void UpdateFromLinkDownEvent(LinkDisconnectionEvent evt)
    {
        var canonicalKey = _networkState.GetCanonicalLinkKey(evt.Local, evt.Remote);
        var link = _networkState.GetLink(canonicalKey);
        
        if (link != null)
        {
            link.Status = Models.NetworkState.LinkStatus.Disconnected;
            link.DisconnectedAt = DateTime.UtcNow;
            link.LastUpdate = DateTime.UtcNow;

            if (link.Endpoints.TryGetValue(evt.Node, out var endpoint))
            {
                endpoint.UpForSecs = evt.UpForSecs ?? endpoint.UpForSecs;
                endpoint.FramesSent = evt.FramesSent;
                endpoint.FramesReceived = evt.FramesReceived;
                endpoint.FramesResent = evt.FramesResent;
                endpoint.Reason = evt.Reason;
                endpoint.LastUpdate = DateTime.UtcNow;
                link.MarkDirty(); // Explicitly mark dirty when modifying endpoint
            }

            _logger.LogDebug("Updated link state from LinkDownEvent: {Key}", link.CanonicalKey);
        }
    }

    public void UpdateFromCircuitUpEvent(CircuitUpEvent evt)
    {
        var circuit = _networkState.GetOrCreateCircuit(evt.Local, evt.Remote);
        
        circuit.Status = Models.NetworkState.CircuitStatus.Active;
        circuit.LastUpdate = DateTime.UtcNow;
        
        if (evt.TimeUnixSeconds.HasValue)
        {
            circuit.ConnectedAt = UnixSecondsToDateTime(evt.TimeUnixSeconds.Value);
        }

        var endpoint = new CircuitEndpointState
        {
            Node = evt.Node,
            Id = evt.Id,
            Direction = evt.Direction,
            Service = evt.Service,
            Local = evt.Local,
            Remote = evt.Remote,
            LastUpdate = DateTime.UtcNow
        };
        
        circuit.Endpoints[evt.Node] = endpoint;
        circuit.MarkDirty(); // Explicitly mark dirty when modifying Endpoints

        if (circuit.Initiator == null)
        {
            circuit.Initiator = evt.Direction.Equals("outgoing", StringComparison.OrdinalIgnoreCase) 
                ? evt.Local 
                : evt.Remote;
        }

        _logger.LogDebug("Updated circuit state from CircuitUpEvent: {Key} ({Local} <-> {Remote})", circuit.CanonicalKey, evt.Local, evt.Remote);
    }

    public void UpdateFromCircuitStatus(Models.CircuitStatus evt)
    {
        var circuit = _networkState.GetOrCreateCircuit(evt.Local, evt.Remote);
        
        circuit.Status = Models.NetworkState.CircuitStatus.Active;
        circuit.LastUpdate = DateTime.UtcNow;

        if (evt.TimeUnixSeconds.HasValue && evt.UpForSecs.HasValue)
        {
            circuit.ConnectedAt = UnixSecondsToDateTime(evt.TimeUnixSeconds.Value - evt.UpForSecs.Value);
        }

        var endpoint = new CircuitEndpointState
        {
            Node = evt.Node,
            Id = evt.Id,
            Direction = evt.Direction,
            Service = evt.Service,
            Local = evt.Local,
            Remote = evt.Remote,
            LastUpdate = DateTime.UtcNow,
            SegmentsSent = evt.SegmentsSent,
            SegmentsReceived = evt.SegmentsReceived,
            SegmentsResent = evt.SegmentsResent,
            SegmentsQueued = evt.SegmentsQueued,
            BytesSent = evt.BytesSent,
            BytesReceived = evt.BytesReceived,
            UpForSecs = evt.UpForSecs
        };
        
        circuit.Endpoints[evt.Node] = endpoint;
        circuit.MarkDirty(); // Explicitly mark dirty when modifying Endpoints

        if (circuit.Initiator == null)
        {
            circuit.Initiator = evt.Direction.Equals("outgoing", StringComparison.OrdinalIgnoreCase) 
                ? evt.Local 
                : evt.Remote;
        }

        _logger.LogDebug("Updated circuit state from CircuitStatus: {Key} ({Local} <-> {Remote})", circuit.CanonicalKey, evt.Local, evt.Remote);
    }

    public void UpdateFromCircuitDownEvent(CircuitDisconnectionEvent evt)
    {
        var canonicalKey = _networkState.GetCanonicalCircuitKey(evt.Local, evt.Remote);
        var circuit = _networkState.GetCircuit(canonicalKey);
        
        if (circuit != null)
        {
            circuit.Status = Models.NetworkState.CircuitStatus.Disconnected;
            circuit.DisconnectedAt = DateTime.UtcNow;
            circuit.LastUpdate = DateTime.UtcNow;

            if (circuit.Endpoints.TryGetValue(evt.Node, out var endpoint))
            {
                endpoint.SegmentsSent = evt.SegmentsSent;
                endpoint.SegmentsReceived = evt.SegmentsReceived;
                endpoint.SegmentsResent = evt.SegmentsResent;
                endpoint.SegmentsQueued = evt.SegmentsQueued;
                endpoint.BytesSent = evt.BytesSent;
                endpoint.BytesReceived = evt.BytesReceived;
                endpoint.Reason = evt.Reason;
                endpoint.UpForSecs = evt.UpForSecs ?? endpoint.UpForSecs;
                endpoint.LastUpdate = DateTime.UtcNow;
                circuit.MarkDirty(); // Explicitly mark dirty when modifying endpoint
            }

            _logger.LogDebug("Updated circuit state from CircuitDownEvent: {Key}", circuit.CanonicalKey);
        }
    }

    public void UpdateNodeIpInfo(string callsign, string ipObfuscated, string? geoCountryCode, string? geoCountryName, string? geoCity)
    {
        var node = _networkState.GetOrCreateNode(callsign);
        
        node.IpAddressObfuscated = ipObfuscated;
        node.LastIpUpdate = DateTime.UtcNow;
        
        if (!string.IsNullOrWhiteSpace(geoCountryCode))
            node.GeoIpCountryCode = geoCountryCode;
        
        if (!string.IsNullOrWhiteSpace(geoCountryName))
            node.GeoIpCountryName = geoCountryName;
        
        if (!string.IsNullOrWhiteSpace(geoCity))
            node.GeoIpCity = geoCity;

        _logger.LogDebug("Updated IP info for node: {Callsign}", callsign);
    }
    
    /// <summary>
    /// Tracks link flapping when a link comes back up after being disconnected
    /// </summary>
    /// <param name="link">The link to track flapping for</param>
    /// <param name="flapWindowMinutes">Time window in minutes for flap detection (default: 15)</param>
    private void TrackLinkFlap(LinkState link, int flapWindowMinutes = 15)
    {
        var now = DateTime.UtcNow;
        
        // If we don't have a flap window, or the window has expired, start a new one
        if (!link.FlapWindowStart.HasValue || 
            now > link.FlapWindowStart.Value.AddMinutes(flapWindowMinutes))
        {
            link.FlapWindowStart = now;
            link.FlapCount = 1;
        }
        else
        {
            // We're within the window, increment the flap count
            link.FlapCount++;
        }
        
        link.LastFlapTime = now;
        
        if (link.FlapCount >= 3)
        {
            _logger.LogWarning(
                "Link {Link} is flapping: {Count} transitions in the last {Minutes} minutes",
                link.CanonicalKey,
                link.FlapCount,
                flapWindowMinutes);
        }
    }
}
