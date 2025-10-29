using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using node_api.Controllers;
using node_api.Models.NetworkState;
using node_api.Services;
using Xunit;

namespace Tests;

public class NodesControllerTests
{
    private readonly INetworkStateService _networkState;
    private readonly ILogger<NodesController> _logger;
    private readonly NodesController _controller;

    public NodesControllerTests()
    {
        _networkState = Substitute.For<INetworkStateService>();
        _logger = Substitute.For<ILogger<NodesController>>();
        _controller = new NodesController(_networkState, _logger);
    }

    [Fact]
    public void GetAllNodes_ReturnsAllNodes_ExcludingTestCallsigns()
    {
        // Arrange
        var nodes = new Dictionary<string, NodeState>
        {
            ["M0LTE"] = new NodeState { Callsign = "M0LTE", Alias = "Tom" },
            ["G8PZT"] = new NodeState { Callsign = "G8PZT", Alias = "Peter" },
            ["TEST"] = new NodeState { Callsign = "TEST", Alias = "Test Node" },
            ["TEST-5"] = new NodeState { Callsign = "TEST-5", Alias = "Test Node 5" }
        };

        _networkState.GetAllNodes().Returns(nodes);
        _networkState.IsTestCallsign("TEST").Returns(true);
        _networkState.IsTestCallsign("TEST-5").Returns(true);
        _networkState.IsTestCallsign("M0LTE").Returns(false);
        _networkState.IsTestCallsign("G8PZT").Returns(false);

        // Act
        var result = _controller.GetAllNodes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedNodes = Assert.IsAssignableFrom<IEnumerable<NodeState>>(okResult.Value);
        Assert.Equal(2, returnedNodes.Count());
        Assert.Contains(returnedNodes, n => n.Callsign == "M0LTE");
        Assert.Contains(returnedNodes, n => n.Callsign == "G8PZT");
        Assert.DoesNotContain(returnedNodes, n => n.Callsign == "TEST");
        Assert.DoesNotContain(returnedNodes, n => n.Callsign == "TEST-5");
    }

    [Fact]
    public void GetAllNodes_ReturnsEmptyList_WhenNoNodes()
    {
        // Arrange
        _networkState.GetAllNodes().Returns(new Dictionary<string, NodeState>());

        // Act
        var result = _controller.GetAllNodes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedNodes = Assert.IsAssignableFrom<IEnumerable<NodeState>>(okResult.Value);
        Assert.Empty(returnedNodes);
    }

    [Fact]
    public void GetNode_ReturnsNode_WhenNodeExists()
    {
        // Arrange
        var node = new NodeState { Callsign = "M0LTE", Alias = "Tom" };
        _networkState.GetNode("M0LTE").Returns(node);

        // Act
        var result = _controller.GetNode("M0LTE");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedNode = Assert.IsType<NodeState>(okResult.Value);
        Assert.Equal("M0LTE", returnedNode.Callsign);
        Assert.Equal("Tom", returnedNode.Alias);
    }

    [Fact]
    public void GetNode_ReturnsNotFound_WhenNodeDoesNotExist()
    {
        // Arrange
        _networkState.GetNode("NONEXISTENT").Returns((NodeState?)null);

        // Act
        var result = _controller.GetNode("NONEXISTENT");

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public void GetNode_ReturnsTestNode_WhenExplicitlyRequested()
    {
        // Arrange
        var testNode = new NodeState { Callsign = "TEST", Alias = "Test Node" };
        _networkState.GetNode("TEST").Returns(testNode);

        // Act
        var result = _controller.GetNode("TEST");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedNode = Assert.IsType<NodeState>(okResult.Value);
        Assert.Equal("TEST", returnedNode.Callsign);
    }

    [Fact]
    public void GetNodesByBaseCallsign_ReturnsAllSSIDs_ExcludingTest()
    {
        // Arrange
        var nodes = new[]
        {
            new NodeState { Callsign = "M0LTE", Alias = "Tom" },
            new NodeState { Callsign = "M0LTE-1", Alias = "Tom Mobile" },
            new NodeState { Callsign = "M0LTE-2", Alias = "Tom Portable" },
            new NodeState { Callsign = "TEST", Alias = "Test Node" }
        };

        _networkState.GetNodesByBaseCallsign("M0LTE").Returns(nodes.Take(3));
        _networkState.IsTestCallsign(Arg.Any<string>()).Returns(false);

        // Act
        var result = _controller.GetNodesByBaseCallsign("M0LTE");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedNodes = Assert.IsAssignableFrom<IEnumerable<NodeState>>(okResult.Value);
        Assert.Equal(3, returnedNodes.Count());
    }

    [Fact]
    public void GetNodesByBaseCallsign_IncludesTestNodes_WhenExplicitlyRequestingTestBase()
    {
        // Arrange
        var nodes = new[]
        {
            new NodeState { Callsign = "TEST", Alias = "Test Node" },
            new NodeState { Callsign = "TEST-1", Alias = "Test Node 1" },
            new NodeState { Callsign = "TEST-15", Alias = "Test Node 15" }
        };

        _networkState.GetNodesByBaseCallsign("TEST").Returns(nodes);
        _networkState.IsTestCallsign(Arg.Any<string>()).Returns(true);

        // Act
        var result = _controller.GetNodesByBaseCallsign("TEST");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedNodes = Assert.IsAssignableFrom<IEnumerable<NodeState>>(okResult.Value);
        Assert.Equal(3, returnedNodes.Count());
    }

    [Fact]
    public void GetNodesByBaseCallsign_IsCaseInsensitive()
    {
        // Arrange
        var nodes = new[]
        {
            new NodeState { Callsign = "M0LTE", Alias = "Tom" }
        };

        _networkState.GetNodesByBaseCallsign("m0lte").Returns(nodes);
        _networkState.IsTestCallsign(Arg.Any<string>()).Returns(false);

        // Act
        var result = _controller.GetNodesByBaseCallsign("m0lte");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedNodes = Assert.IsAssignableFrom<IEnumerable<NodeState>>(okResult.Value);
        Assert.Single(returnedNodes);
    }

    [Fact]
    public void GetNodesByBaseCallsign_ReturnsEmptyList_WhenNoMatchingNodes()
    {
        // Arrange
        _networkState.GetNodesByBaseCallsign("NONEXISTENT")
            .Returns(Enumerable.Empty<NodeState>());

        // Act
        var result = _controller.GetNodesByBaseCallsign("NONEXISTENT");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedNodes = Assert.IsAssignableFrom<IEnumerable<NodeState>>(okResult.Value);
        Assert.Empty(returnedNodes);
    }
}
