using node_api.Models;
using node_api.Services;
using Xunit;

namespace Tests;

public class PortMetadataStoreTests
{
    [Fact]
    public void Get_returns_exact_node_port_match_case_insensitively()
    {
        var store = new PortMetadataStore();
        store.Replace([new PortMetadata("GB7RDG", "3", LinkType: "RF", FreqHz: 7051600, Band: "40m",
            FreqSource: "reported", Mode: "ax.25", Modulation: null, Baud: 300, Bitrate: 300, Usage: "Mixed", Comment: null)]);

        var pm = store.Get("gb7rdg", "3");
        Assert.NotNull(pm);
        Assert.Equal("40m", pm!.Band);
        Assert.Equal(7051600, pm.FreqHz);
    }

    [Fact]
    public void Get_falls_back_to_base_call_for_an_ssid_endpoint()
    {
        var store = new PortMetadataStore();
        store.Replace([Meta("GB7RDG", "3", "40m")]);

        // node-api may name the endpoint GB7RDG-2 while we keyed it under the base call.
        var pm = store.Get("GB7RDG-2", "3");
        Assert.NotNull(pm);
        Assert.Equal("40m", pm!.Band);
    }

    [Fact]
    public void Replace_swaps_the_whole_set()
    {
        var store = new PortMetadataStore();
        store.Replace([Meta("A", "1", "b1")]);
        store.Replace([Meta("B", "1", "b2")]);

        Assert.Null(store.Get("A", "1"));   // gone after replace
        Assert.Equal("b2", store.Get("B", "1")!.Band);
        Assert.Equal(1, store.Count);
    }

    private static PortMetadata Meta(string node, string port, string band) =>
        new(node, port, LinkType: "RF", FreqHz: null, Band: band, FreqSource: null,
            Mode: null, Modulation: null, Baud: null, Bitrate: null, Usage: null, Comment: null);
}
