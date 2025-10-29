using node_api.Models.NetworkState;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace node_api.Services;

/// <summary>
/// Maintains the current state of all nodes, links, and circuits in the network.
/// This is an in-memory state that can later be backed by Redis or MySQL.
/// </summary>
public interface INetworkStateService
{
    // Node operations
    NodeState GetOrCreateNode(string callsign);
    NodeState? GetNode(string callsign);
    IReadOnlyDictionary<string, NodeState> GetAllNodes();
    IEnumerable<NodeState> GetNodesByBaseCallsign(string baseCallsign);
    
    // Link operations
    LinkState GetOrCreateLink(string local, string remote);
    LinkState? GetLink(string canonicalKey);
    IReadOnlyDictionary<string, LinkState> GetAllLinks();
    IEnumerable<LinkState> GetLinksForNode(string callsign);
    
    // Circuit operations
    CircuitState GetOrCreateCircuit(string local, string remote);
    CircuitState? GetCircuit(string canonicalKey);
    IReadOnlyDictionary<string, CircuitState> GetAllCircuits();
    IEnumerable<CircuitState> GetCircuitsForNode(string callsign);
    
    // Dirty tracking operations
    IEnumerable<NodeState> GetDirtyNodes();
    IEnumerable<LinkState> GetDirtyLinks();
    IEnumerable<CircuitState> GetDirtyCircuits();
    void MarkNodeClean(NodeState node);
    void MarkLinkClean(LinkState link);
    void MarkCircuitClean(CircuitState circuit);
    
    // Utility
    string GetCanonicalLinkKey(string local, string remote);
    string GetCanonicalCircuitKey(string local, string remote);
    bool IsTestCallsign(string callsign);
}

public partial class NetworkStateService : INetworkStateService
{
    private readonly ConcurrentDictionary<string, NodeState> _nodes = new();
    private readonly ConcurrentDictionary<string, LinkState> _links = new();
    private readonly ConcurrentDictionary<string, CircuitState> _circuits = new();
    private readonly ILogger<NetworkStateService> _logger;

    [GeneratedRegex(@"^TEST(-([0-9]|1[0-5]))?$", RegexOptions.IgnoreCase)]
    private static partial Regex TestCallsignRegex();

    public NetworkStateService(ILogger<NetworkStateService> logger)
    {
        _logger = logger;
    }

    public bool IsTestCallsign(string callsign)
    {
        if (string.IsNullOrWhiteSpace(callsign))
            return false;

        return TestCallsignRegex().IsMatch(callsign);
    }

    public NodeState GetOrCreateNode(string callsign)
    {
        return _nodes.GetOrAdd(callsign, cs =>
        {
            _logger.LogDebug("Creating new node state for {Callsign}", cs);
            var node = new NodeState
            {
                Callsign = cs,
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            };
            node.MarkDirty();
            return node;
        });
    }

    public NodeState? GetNode(string callsign)
    {
        _nodes.TryGetValue(callsign, out var node);
        return node;
    }

    public IReadOnlyDictionary<string, NodeState> GetAllNodes()
    {
        return _nodes;
    }

    public IEnumerable<NodeState> GetNodesByBaseCallsign(string baseCallsign)
    {
        var baseUpper = baseCallsign.ToUpperInvariant();
        
        return _nodes.Values.Where(node =>
        {
            var parts = node.Callsign.Split('-');
            var nodeBase = parts[0].ToUpperInvariant();
            return nodeBase == baseUpper;
        });
    }

    public string GetCanonicalLinkKey(string local, string remote)
    {
        var sorted = new[] { local, remote }.OrderBy(x => x).ToArray();
        return $"{sorted[0]}<->{sorted[1]}";
    }

    public LinkState GetOrCreateLink(string local, string remote)
    {
        var canonicalKey = GetCanonicalLinkKey(local, remote);
        
        return _links.GetOrAdd(canonicalKey, key =>
        {
            _logger.LogDebug("Creating new link state for {Key}", key);
            var sorted = new[] { local, remote }.OrderBy(x => x).ToArray();
            var link = new LinkState
            {
                CanonicalKey = key,
                Endpoint1 = sorted[0],
                Endpoint2 = sorted[1],
                ConnectedAt = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow
            };
            link.MarkDirty();
            return link;
        });
    }

    public LinkState? GetLink(string canonicalKey)
    {
        _links.TryGetValue(canonicalKey, out var link);
        return link;
    }

    public IReadOnlyDictionary<string, LinkState> GetAllLinks()
    {
        return _links;
    }

    public IEnumerable<LinkState> GetLinksForNode(string callsign)
    {
        return _links.Values.Where(link =>
            link.Endpoint1.Equals(callsign, StringComparison.OrdinalIgnoreCase) ||
            link.Endpoint2.Equals(callsign, StringComparison.OrdinalIgnoreCase));
    }

    public string GetCanonicalCircuitKey(string local, string remote)
    {
        var sorted = new[] { local, remote }.OrderBy(x => x).ToArray();
        return $"{sorted[0]}<->{sorted[1]}";
    }

    public CircuitState GetOrCreateCircuit(string local, string remote)
    {
        var canonicalKey = GetCanonicalCircuitKey(local, remote);
        
        return _circuits.GetOrAdd(canonicalKey, key =>
        {
            _logger.LogDebug("Creating new circuit state for {Key}", key);
            var sorted = new[] { local, remote }.OrderBy(x => x).ToArray();
            var circuit = new CircuitState
            {
                CanonicalKey = key,
                Endpoint1 = sorted[0],
                Endpoint2 = sorted[1],
                ConnectedAt = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow
            };
            circuit.MarkDirty();
            return circuit;
        });
    }

    public CircuitState? GetCircuit(string canonicalKey)
    {
        _circuits.TryGetValue(canonicalKey, out var circuit);
        return circuit;
    }

    public IReadOnlyDictionary<string, CircuitState> GetAllCircuits()
    {
        return _circuits;
    }

    public IEnumerable<CircuitState> GetCircuitsForNode(string callsign)
    {
        return _circuits.Values.Where(circuit =>
            circuit.Endpoint1.Equals(callsign, StringComparison.OrdinalIgnoreCase) ||
            circuit.Endpoint2.Equals(callsign, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<NodeState> GetDirtyNodes()
    {
        return _nodes.Values.Where(n => n.IsDirty);
    }

    public IEnumerable<LinkState> GetDirtyLinks()
    {
        return _links.Values.Where(l => l.IsDirty);
    }

    public IEnumerable<CircuitState> GetDirtyCircuits()
    {
        return _circuits.Values.Where(c => c.IsDirty);
    }

    public void MarkNodeClean(NodeState node)
    {
        node.MarkClean();
    }

    public void MarkLinkClean(LinkState link)
    {
        link.MarkClean();
    }

    public void MarkCircuitClean(CircuitState circuit)
    {
        circuit.MarkClean();
    }
}
