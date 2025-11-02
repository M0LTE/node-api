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

    [GeneratedRegex(@"^([A-Z0-9]+)(?:-\d+)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BaseCallsignRegex();

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

    public void UpdateFromNodeUpEvent(NodeUpEvent evt)
    {
        var node = _networkState.GetOrCreateNode(evt.NodeCall);
        
        node.Alias = evt.NodeAlias;
        node.Locator = evt.Locator;
        node.Latitude = evt.Latitude;
        node.Longitude = evt.Longitude;
        node.Software = evt.Software;
        node.Version = evt.Version;
        node.Status = NodeStatus.Online;
        node.LastSeen = DateTime.UtcNow;
        node.LastUpEvent = DateTime.UtcNow;

        _logger.LogDebug("Updated node state from NodeUpEvent: {Callsign}", evt.NodeCall);
    }

    public void UpdateFromNodeStatus(NodeStatusReportEvent evt)
    {
        var node = _networkState.GetOrCreateNode(evt.NodeCall);
        
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

        _logger.LogDebug("Updated node state from NodeStatus: {Callsign}", evt.NodeCall);
    }

    public void UpdateFromNodeDownEvent(NodeDownEvent evt)
    {
        var node = _networkState.GetOrCreateNode(evt.NodeCall);
        
        node.Alias = evt.NodeAlias;
        node.Status = NodeStatus.Offline;
        node.LastSeen = DateTime.UtcNow;
        node.LastDownEvent = DateTime.UtcNow;

        _logger.LogDebug("Updated node state from NodeDownEvent: {Callsign}", evt.NodeCall);
    }

    /// <summary>
    /// Extracts the base callsign without SSID (e.g., "M0LTE-5" -> "M0LTE")
    /// </summary>
    private static string? GetBaseCallsign(string? callsign)
    {
        if (string.IsNullOrWhiteSpace(callsign))
            return null;

        var match = BaseCallsignRegex().Match(callsign);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }

    /// <summary>
    /// Determines if a link can be reliably inferred from an L2Trace.
    /// 
    /// In AX.25, when a user makes an L2 connection through an intermediate node,
    /// that node transmits using the user's callsign (not its own). This means
    /// we can hear frames where the source callsign is not the actual transmitter.
    /// 
    /// Heuristic:
    /// - If dirn == "sent" AND base(source) != base(reportFrom):
    ///   The reporting node is forwarding/proxying for another station.
    ///   The source callsign is NOT the actual transmitter - it's being impersonated.
    ///   We should NOT infer a direct link between source and destination.
    /// 
    /// - If dirn == "rcvd":
    ///   The reporting node received/overheard the frame.
    ///   The source is the actual sender (not impersonated).
    ///   We CAN infer a link.
    /// 
    /// - If dirn == "sent" AND base(source) == base(reportFrom):
    ///   The reporting node is transmitting as itself (or one of its SSIDs).
    ///   We CAN infer a link.
    /// </summary>
    /// <param name="trace">The L2Trace to analyze</param>
    /// <returns>True if we can reliably infer a link, false if the source may be impersonated</returns>
    private bool CanInferLinkFromTrace(L2Trace trace)
    {
        // If direction is not specified or is "rcvd", we can infer the link
        if (string.IsNullOrEmpty(trace.Direction) || 
            trace.Direction.Equals("rcvd", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // If direction is "sent", check if source matches reporter
        if (trace.Direction.Equals("sent", StringComparison.OrdinalIgnoreCase))
        {
            var sourceBase = GetBaseCallsign(trace.Source);
            var reporterBase = GetBaseCallsign(trace.ReportFrom);

            // If we can't extract base callsigns, be conservative and allow it
            if (sourceBase == null || reporterBase == null)
            {
                return true;
            }

            // If the base callsigns match, the reporter is transmitting as itself
            if (sourceBase.Equals(reporterBase, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Base callsigns don't match - the reporter is forwarding for someone else
            // The source is being impersonated - DO NOT infer a direct link
            _logger.LogDebug(
                "Not inferring link from L2Trace: reporter={Reporter} is forwarding for source={Source} (direction=sent, base callsigns differ)",
                trace.ReportFrom,
                trace.Source);
            return false;
        }

        // Unknown direction value - be conservative and allow it
        return true;
    }

    public void UpdateFromL2Trace(L2Trace trace)
    {
        if (trace.ReportFrom != null)
        {
            var node = _networkState.GetOrCreateNode(trace.ReportFrom);
            node.L2TraceCount++;
            node.LastL2Trace = DateTime.UtcNow;
            node.LastSeen = DateTime.UtcNow;
            
            if (node.Status == NodeStatus.Unknown)
            {
                node.Status = NodeStatus.Online;
            }

            _logger.LogDebug("Updated node state from L2Trace: {Callsign}", trace.ReportFrom);
        }

        // Track activity for source and destination nodes
        // We always track node activity regardless of link inference
        if (trace.Source != null)
        {
            var node = _networkState.GetOrCreateNode(trace.Source);
            node.LastSeen = DateTime.UtcNow;
            if (node.Status == NodeStatus.Unknown)
            {
                node.Status = NodeStatus.Online;
            }
        }

        if (trace.Destination != null)
        {
            var node = _networkState.GetOrCreateNode(trace.Destination);
            node.LastSeen = DateTime.UtcNow;
            if (node.Status == NodeStatus.Unknown)
            {
                node.Status = NodeStatus.Online;
            }
        }

        // Update link RF status ONLY if we can reliably infer this link
        // Only update when we have definitive information (not null)
        if (trace.IsRF.HasValue && trace.Source != null && trace.Destination != null)
        {
            // Apply the AX.25 routing heuristic
            if (!CanInferLinkFromTrace(trace))
            {
                // Don't update link information when source is being impersonated
                _logger.LogTrace(
                    "Skipping link RF update: source {Source} appears to be impersonated by {Reporter}",
                    trace.Source,
                    trace.ReportFrom);
                return;
            }

            var canonicalKey = _networkState.GetCanonicalLinkKey(trace.Source, trace.Destination);
            var link = _networkState.GetLink(canonicalKey);
            
            // Only update if link exists and we don't already know the RF status
            // or if the RF status has changed
            if (link != null && link.IsRF != trace.IsRF)
            {
                link.IsRF = trace.IsRF;
                _logger.LogDebug(
                    "Updated link RF status from L2Trace: {Link} is {RFStatus}",
                    canonicalKey,
                    trace.IsRF.Value ? "RF" : "not RF");
            }
        }
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
            link.ConnectedAt = DateTimeOffset.FromUnixTimeSeconds(evt.TimeUnixSeconds.Value).UtcDateTime;
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
            link.ConnectedAt = DateTimeOffset.FromUnixTimeSeconds(
                evt.TimeUnixSeconds.Value - evt.UpForSecs.Value).UtcDateTime;
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
            circuit.ConnectedAt = DateTimeOffset.FromUnixTimeSeconds(evt.TimeUnixSeconds.Value).UtcDateTime;
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
            BytesReceived = evt.BytesReceived
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
