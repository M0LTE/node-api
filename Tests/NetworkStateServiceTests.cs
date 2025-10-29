using Microsoft.Extensions.Logging;
using NSubstitute;
using node_api.Models.NetworkState;
using node_api.Services;
using Xunit;

namespace Tests;

public class NetworkStateServiceTests
{
    private readonly ILogger<NetworkStateService> _logger;
    private readonly NetworkStateService _service;

    public NetworkStateServiceTests()
    {
        _logger = Substitute.For<ILogger<NetworkStateService>>();
        _service = new NetworkStateService(_logger);
    }

    #region Node Tests

    [Fact]
    public void GetOrCreateNode_CreatesNewNode_WhenNodeDoesNotExist()
    {
        // Act
        var node = _service.GetOrCreateNode("M0LTE");

        // Assert
        Assert.NotNull(node);
        Assert.Equal("M0LTE", node.Callsign);
        Assert.Equal(NodeStatus.Unknown, node.Status);
        Assert.True(node.IsDirty);
        Assert.NotNull(node.FirstSeen);
        Assert.NotNull(node.LastSeen);
        Assert.True((DateTime.UtcNow - node.FirstSeen.Value).TotalSeconds < 1);
        Assert.True((DateTime.UtcNow - node.LastSeen.Value).TotalSeconds < 1);
    }

    [Fact]
    public void GetOrCreateNode_ReturnsExistingNode_WhenNodeExists()
    {
        // Arrange
        var existingNode = _service.GetOrCreateNode("M0LTE");
        existingNode.Alias = "Tom";
        existingNode.MarkClean();

        // Act
        var retrievedNode = _service.GetOrCreateNode("M0LTE");

        // Assert
        Assert.Same(existingNode, retrievedNode);
        Assert.Equal("Tom", retrievedNode.Alias);
        Assert.False(retrievedNode.IsDirty); // Should not mark as dirty if already exists
    }

    [Fact]
    public void GetNode_ReturnsNode_WhenNodeExists()
    {
        // Arrange
        var createdNode = _service.GetOrCreateNode("M0LTE");

        // Act
        var retrievedNode = _service.GetNode("M0LTE");

        // Assert
        Assert.Same(createdNode, retrievedNode);
    }

    [Fact]
    public void GetNode_ReturnsNull_WhenNodeDoesNotExist()
    {
        // Act
        var node = _service.GetNode("NONEXISTENT");

        // Assert
        Assert.Null(node);
    }

    [Fact]
    public void GetAllNodes_ReturnsAllNodes()
    {
        // Arrange
        _service.GetOrCreateNode("M0LTE");
        _service.GetOrCreateNode("G8PZT");
        _service.GetOrCreateNode("M0XYZ");

        // Act
        var nodes = _service.GetAllNodes();

        // Assert
        Assert.Equal(3, nodes.Count);
        Assert.Contains("M0LTE", nodes.Keys);
        Assert.Contains("G8PZT", nodes.Keys);
        Assert.Contains("M0XYZ", nodes.Keys);
    }

    [Fact]
    public void GetNodesByBaseCallsign_ReturnsAllSSIDs()
    {
        // Arrange
        _service.GetOrCreateNode("M0LTE");
        _service.GetOrCreateNode("M0LTE-1");
        _service.GetOrCreateNode("M0LTE-2");
        _service.GetOrCreateNode("G8PZT"); // Different base

        // Act
        var nodes = _service.GetNodesByBaseCallsign("M0LTE").ToList();

        // Assert
        Assert.Equal(3, nodes.Count);
        Assert.Contains(nodes, n => n.Callsign == "M0LTE");
        Assert.Contains(nodes, n => n.Callsign == "M0LTE-1");
        Assert.Contains(nodes, n => n.Callsign == "M0LTE-2");
        Assert.DoesNotContain(nodes, n => n.Callsign == "G8PZT");
    }

    [Fact]
    public void GetNodesByBaseCallsign_IsCaseInsensitive()
    {
        // Arrange
        _service.GetOrCreateNode("M0LTE");
        _service.GetOrCreateNode("m0lte-1");

        // Act
        var nodes = _service.GetNodesByBaseCallsign("m0lte").ToList();

        // Assert
        Assert.Equal(2, nodes.Count);
    }

    [Fact]
    public void IsTestCallsign_ReturnsTrueForTestCallsigns()
    {
        // Act & Assert
        Assert.True(_service.IsTestCallsign("TEST"));
        Assert.True(_service.IsTestCallsign("test"));
        Assert.True(_service.IsTestCallsign("TEST-0"));
        Assert.True(_service.IsTestCallsign("TEST-1"));
        Assert.True(_service.IsTestCallsign("TEST-15"));
    }

    [Fact]
    public void IsTestCallsign_ReturnsFalseForNonTestCallsigns()
    {
        // Act & Assert
        Assert.False(_service.IsTestCallsign("M0LTE"));
        Assert.False(_service.IsTestCallsign("G8PZT"));
        Assert.False(_service.IsTestCallsign("TEST-16")); // Out of range
        Assert.False(_service.IsTestCallsign("TESTNODE"));
        Assert.False(_service.IsTestCallsign(""));
        Assert.False(_service.IsTestCallsign(null!));
    }

    #endregion

    #region Link Tests

    [Fact]
    public void GetCanonicalLinkKey_SortsCallsignsAlphabetically()
    {
        // Act
        var key1 = _service.GetCanonicalLinkKey("M0LTE", "G8PZT");
        var key2 = _service.GetCanonicalLinkKey("G8PZT", "M0LTE");

        // Assert
        Assert.Equal("G8PZT<->M0LTE", key1);
        Assert.Equal("G8PZT<->M0LTE", key2); // Same regardless of order
    }

    [Fact]
    public void GetOrCreateLink_CreatesNewLink_WhenLinkDoesNotExist()
    {
        // Act
        var link = _service.GetOrCreateLink("M0LTE", "G8PZT");

        // Assert
        Assert.NotNull(link);
        Assert.Equal("G8PZT<->M0LTE", link.CanonicalKey);
        Assert.Equal("G8PZT", link.Endpoint1);
        Assert.Equal("M0LTE", link.Endpoint2);
        Assert.True(link.IsDirty);
        Assert.True((DateTime.UtcNow - link.ConnectedAt).TotalSeconds < 1);
        Assert.True((DateTime.UtcNow - link.LastUpdate).TotalSeconds < 1);
    }

    [Fact]
    public void GetOrCreateLink_ReturnsExistingLink_WhenLinkExists()
    {
        // Arrange
        var existingLink = _service.GetOrCreateLink("M0LTE", "G8PZT");
        existingLink.MarkClean();

        // Act
        var retrievedLink = _service.GetOrCreateLink("G8PZT", "M0LTE"); // Reversed order

        // Assert
        Assert.Same(existingLink, retrievedLink);
        Assert.False(retrievedLink.IsDirty);
    }

    [Fact]
    public void GetLink_ReturnsLink_WhenLinkExists()
    {
        // Arrange
        var createdLink = _service.GetOrCreateLink("M0LTE", "G8PZT");
        var canonicalKey = createdLink.CanonicalKey;

        // Act
        var retrievedLink = _service.GetLink(canonicalKey);

        // Assert
        Assert.Same(createdLink, retrievedLink);
    }

    [Fact]
    public void GetLink_ReturnsNull_WhenLinkDoesNotExist()
    {
        // Act
        var link = _service.GetLink("NONEXISTENT<->LINK");

        // Assert
        Assert.Null(link);
    }

    [Fact]
    public void GetAllLinks_ReturnsAllLinks()
    {
        // Arrange
        _service.GetOrCreateLink("M0LTE", "G8PZT");
        _service.GetOrCreateLink("M0LTE", "M0XYZ");
        _service.GetOrCreateLink("G8PZT", "M0ABC");

        // Act
        var links = _service.GetAllLinks();

        // Assert
        Assert.Equal(3, links.Count);
    }

    [Fact]
    public void GetLinksForNode_ReturnsLinksInvolvingNode()
    {
        // Arrange
        _service.GetOrCreateLink("M0LTE", "G8PZT");
        _service.GetOrCreateLink("M0LTE", "M0XYZ");
        _service.GetOrCreateLink("G8PZT", "M0ABC");

        // Act
        var links = _service.GetLinksForNode("M0LTE").ToList();

        // Assert
        Assert.Equal(2, links.Count);
        Assert.All(links, link => 
            Assert.True(link.Endpoint1 == "M0LTE" || link.Endpoint2 == "M0LTE"));
    }

    [Fact]
    public void GetLinksForNode_IsCaseInsensitive()
    {
        // Arrange
        _service.GetOrCreateLink("M0LTE", "G8PZT");

        // Act
        var links = _service.GetLinksForNode("m0lte").ToList();

        // Assert
        Assert.Single(links);
    }

    #endregion

    #region Circuit Tests

    [Fact]
    public void GetCanonicalCircuitKey_SortsAddressesAlphabetically()
    {
        // Act
        var key1 = _service.GetCanonicalCircuitKey("M0LTE:1234", "G8PZT:5678");
        var key2 = _service.GetCanonicalCircuitKey("G8PZT:5678", "M0LTE:1234");

        // Assert
        Assert.Equal("G8PZT:5678<->M0LTE:1234", key1);
        Assert.Equal("G8PZT:5678<->M0LTE:1234", key2);
    }

    [Fact]
    public void GetOrCreateCircuit_CreatesNewCircuit_WhenCircuitDoesNotExist()
    {
        // Act
        var circuit = _service.GetOrCreateCircuit("M0LTE:1234", "G8PZT:5678");

        // Assert
        Assert.NotNull(circuit);
        Assert.Equal("G8PZT:5678<->M0LTE:1234", circuit.CanonicalKey);
        Assert.Equal("G8PZT:5678", circuit.Endpoint1);
        Assert.Equal("M0LTE:1234", circuit.Endpoint2);
        Assert.True(circuit.IsDirty);
        Assert.True((DateTime.UtcNow - circuit.ConnectedAt).TotalSeconds < 1);
        Assert.True((DateTime.UtcNow - circuit.LastUpdate).TotalSeconds < 1);
    }

    [Fact]
    public void GetOrCreateCircuit_ReturnsExistingCircuit_WhenCircuitExists()
    {
        // Arrange
        var existingCircuit = _service.GetOrCreateCircuit("M0LTE:1234", "G8PZT:5678");
        existingCircuit.MarkClean();

        // Act
        var retrievedCircuit = _service.GetOrCreateCircuit("G8PZT:5678", "M0LTE:1234");

        // Assert
        Assert.Same(existingCircuit, retrievedCircuit);
        Assert.False(retrievedCircuit.IsDirty);
    }

    [Fact]
    public void GetCircuit_ReturnsCircuit_WhenCircuitExists()
    {
        // Arrange
        var createdCircuit = _service.GetOrCreateCircuit("M0LTE:1234", "G8PZT:5678");
        var canonicalKey = createdCircuit.CanonicalKey;

        // Act
        var retrievedCircuit = _service.GetCircuit(canonicalKey);

        // Assert
        Assert.Same(createdCircuit, retrievedCircuit);
    }

    [Fact]
    public void GetCircuit_ReturnsNull_WhenCircuitDoesNotExist()
    {
        // Act
        var circuit = _service.GetCircuit("NONEXISTENT<->CIRCUIT");

        // Assert
        Assert.Null(circuit);
    }

    [Fact]
    public void GetAllCircuits_ReturnsAllCircuits()
    {
        // Arrange
        _service.GetOrCreateCircuit("M0LTE:1111", "G8PZT:2222");
        _service.GetOrCreateCircuit("M0LTE:3333", "M0XYZ:4444");
        _service.GetOrCreateCircuit("G8PZT:5555", "M0ABC:6666");

        // Act
        var circuits = _service.GetAllCircuits();

        // Assert
        Assert.Equal(3, circuits.Count);
    }

    [Fact]
    public void GetCircuitsForNode_ReturnsCircuitsInvolvingNode()
    {
        // Arrange
        _service.GetOrCreateCircuit("M0LTE:1111", "G8PZT:2222");
        _service.GetOrCreateCircuit("M0LTE:3333", "M0XYZ:4444");
        _service.GetOrCreateCircuit("G8PZT:5555", "M0ABC:6666");

        // Act - Search using full circuit address format
        var circuits = _service.GetCircuitsForNode("M0LTE:3333").ToList();

        // Assert - Should return circuits where either endpoint matches the search term
        Assert.Single(circuits); // Only the second circuit matches exactly
        Assert.All(circuits, circuit => 
            Assert.True(circuit.Endpoint1.Contains("M0LTE") || circuit.Endpoint2.Contains("M0LTE")));
    }

    [Fact]
    public void GetCircuitsForNode_IsCaseInsensitive()
    {
        // Arrange
        _service.GetOrCreateCircuit("M0LTE:1111", "G8PZT:2222");

        // Act
        var circuits = _service.GetCircuitsForNode("m0lte:1111").ToList();

        // Assert
        Assert.Single(circuits);
    }

    #endregion

    #region Dirty Tracking Tests

    [Fact]
    public void GetDirtyNodes_ReturnsOnlyDirtyNodes()
    {
        // Arrange
        var node1 = _service.GetOrCreateNode("M0LTE");
        var node2 = _service.GetOrCreateNode("G8PZT");
        var node3 = _service.GetOrCreateNode("M0XYZ");
        
        _service.MarkNodeClean(node2);

        // Act
        var dirtyNodes = _service.GetDirtyNodes().ToList();

        // Assert
        Assert.Equal(2, dirtyNodes.Count);
        Assert.Contains(node1, dirtyNodes);
        Assert.Contains(node3, dirtyNodes);
        Assert.DoesNotContain(node2, dirtyNodes);
    }

    [Fact]
    public void GetDirtyLinks_ReturnsOnlyDirtyLinks()
    {
        // Arrange
        var link1 = _service.GetOrCreateLink("M0LTE", "G8PZT");
        var link2 = _service.GetOrCreateLink("M0LTE", "M0XYZ");
        var link3 = _service.GetOrCreateLink("G8PZT", "M0ABC");
        
        _service.MarkLinkClean(link2);

        // Act
        var dirtyLinks = _service.GetDirtyLinks().ToList();

        // Assert
        Assert.Equal(2, dirtyLinks.Count);
        Assert.Contains(link1, dirtyLinks);
        Assert.Contains(link3, dirtyLinks);
        Assert.DoesNotContain(link2, dirtyLinks);
    }

    [Fact]
    public void GetDirtyCircuits_ReturnsOnlyDirtyCircuits()
    {
        // Arrange
        var circuit1 = _service.GetOrCreateCircuit("M0LTE:1111", "G8PZT:2222");
        var circuit2 = _service.GetOrCreateCircuit("M0LTE:3333", "M0XYZ:4444");
        var circuit3 = _service.GetOrCreateCircuit("G8PZT:5555", "M0ABC:6666");
        
        _service.MarkCircuitClean(circuit2);

        // Act
        var dirtyCircuits = _service.GetDirtyCircuits().ToList();

        // Assert
        Assert.Equal(2, dirtyCircuits.Count);
        Assert.Contains(circuit1, dirtyCircuits);
        Assert.Contains(circuit3, dirtyCircuits);
        Assert.DoesNotContain(circuit2, dirtyCircuits);
    }

    [Fact]
    public void MarkNodeClean_ClearsIsDirtyFlag()
    {
        // Arrange
        var node = _service.GetOrCreateNode("M0LTE");
        Assert.True(node.IsDirty);

        // Act
        _service.MarkNodeClean(node);

        // Assert
        Assert.False(node.IsDirty);
    }

    [Fact]
    public void MarkLinkClean_ClearsIsDirtyFlag()
    {
        // Arrange
        var link = _service.GetOrCreateLink("M0LTE", "G8PZT");
        Assert.True(link.IsDirty);

        // Act
        _service.MarkLinkClean(link);

        // Assert
        Assert.False(link.IsDirty);
    }

    [Fact]
    public void MarkCircuitClean_ClearsIsDirtyFlag()
    {
        // Arrange
        var circuit = _service.GetOrCreateCircuit("M0LTE:1111", "G8PZT:2222");
        Assert.True(circuit.IsDirty);

        // Act
        _service.MarkCircuitClean(circuit);

        // Assert
        Assert.False(circuit.IsDirty);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task GetOrCreateNode_IsThreadSafe()
    {
        // Arrange & Act
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => _service.GetOrCreateNode("M0LTE")))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - All tasks should return the same instance
        var firstNode = tasks[0].Result;
        Assert.All(tasks, task => Assert.Same(firstNode, task.Result));
    }

    [Fact]
    public async Task GetOrCreateLink_IsThreadSafe()
    {
        // Arrange & Act
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => _service.GetOrCreateLink("M0LTE", "G8PZT")))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - All tasks should return the same instance
        var firstLink = tasks[0].Result;
        Assert.All(tasks, task => Assert.Same(firstLink, task.Result));
    }

    [Fact]
    public async Task GetOrCreateCircuit_IsThreadSafe()
    {
        // Arrange & Act
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => _service.GetOrCreateCircuit("M0LTE:1111", "G8PZT:2222")))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - All tasks should return the same instance
        var firstCircuit = tasks[0].Result;
        Assert.All(tasks, task => Assert.Same(firstCircuit, task.Result));
    }

    #endregion
}
