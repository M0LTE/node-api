using node_api.Models;

namespace node_api.Services;

/// <summary>
/// In-memory store of per-port metadata, replaced wholesale on each push. On restart it is empty until
/// the next push (~1 minute), which is fine — it only annotates links, never gates them.
/// </summary>
public interface IPortMetadataStore
{
    void Replace(IEnumerable<PortMetadata> items);
    PortMetadata? Get(string node, string port);
    int Count { get; }
}

public sealed class PortMetadataStore : IPortMetadataStore
{
    private volatile IReadOnlyDictionary<(string Node, string Port), PortMetadata> _map =
        new Dictionary<(string, string), PortMetadata>();

    public int Count => _map.Count;

    public void Replace(IEnumerable<PortMetadata> items)
    {
        var map = new Dictionary<(string, string), PortMetadata>();
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Node) || string.IsNullOrWhiteSpace(item.Port)) continue;
            map[(item.Node.ToUpperInvariant(), item.Port)] = item;
        }
        _map = map;
    }

    public PortMetadata? Get(string node, string port)
    {
        if (string.IsNullOrEmpty(node) || string.IsNullOrEmpty(port)) return null;
        if (_map.TryGetValue((node.ToUpperInvariant(), port), out var exact)) return exact;

        // Fall back to the base call: node-api may name an endpoint with an SSID the source keyed under
        // its base call (e.g. GB7RDG-2 vs GB7RDG), same physical site/port.
        var dash = node.IndexOf('-');
        return dash > 0 && _map.TryGetValue((node[..dash].ToUpperInvariant(), port), out var byBase) ? byBase : null;
    }
}
