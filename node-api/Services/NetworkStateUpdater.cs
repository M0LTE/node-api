using node_api.Models;
using node_api.Models.NetworkState;

namespace node_api.Services;

/// <summary>
/// Updates the network state based on incoming events from MQTT/UDP
/// </summary>
public class NetworkStateUpdater : IHostedService
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

        // Also track activity for source and destination
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
    }

    public void UpdateFromLinkUpEvent(LinkUpEvent evt)
    {
        var link = _networkState.GetOrCreateLink(evt.Local, evt.Remote);
        
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

        if (link.Initiator == null)
        {
            link.Initiator = evt.Direction.Equals("outgoing", StringComparison.OrdinalIgnoreCase) 
                ? evt.Local 
                : evt.Remote;
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
            }

            _logger.LogDebug("Updated circuit state from CircuitDownEvent: {Key}", circuit.CanonicalKey);
        }
    }
}
