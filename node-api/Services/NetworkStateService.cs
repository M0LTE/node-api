using node_api.Models.NetworkState;
using System.Collections.Concurrent;

namespace node_api.Services;

/// <summary>
/// Maintains the current state of all nodes and links in the network.
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
    
    // Utility
    string GetCanonicalLinkKey(string local, string remote);
}

public class NetworkStateService : INetworkStateService
{
    private readonly ConcurrentDictionary<string, NodeState> _nodes = new();
    private readonly ConcurrentDictionary<string, LinkState> _links = new();
    private readonly ILogger<NetworkStateService> _logger;

    public NetworkStateService(ILogger<NetworkStateService> logger)
    {
        _logger = logger;
    }

    public NodeState GetOrCreateNode(string callsign)
    {
        return _nodes.GetOrAdd(callsign, cs =>
        {
            _logger.LogDebug("Creating new node state for {Callsign}", cs);
            return new NodeState
            {
                Callsign = cs,
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            };
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
            return new LinkState
            {
                CanonicalKey = key,
                Endpoint1 = sorted[0],
                Endpoint2 = sorted[1],
                ConnectedAt = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow
            };
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
}
