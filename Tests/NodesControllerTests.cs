using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        _logger = Substitute.For<ILogger<NodesController>>();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        
        var networkStateLogger = Substitute.For<ILogger<NetworkStateService>>();
        _networkState = new NetworkStateService(networkStateLogger, configuration);
        
        _controller = new NodesController(_networkState, _logger);
    }

    [Fact]
    public void GetReportingNodes_ReturnsOnlyReportingNodes()
    {
        // Arrange
        var reportingNode = _networkState.GetOrCreateNode("M0LTE");
        reportingNode.IsReportingNode = true;
        
        var discoveredNode = _networkState.GetOrCreateNode("G8PZT");
        discoveredNode.IsReportingNode = false;

        // Act
        var result = _controller.GetReportingNodes() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var nodes = result.Value as IEnumerable<NodeState>;
        Assert.NotNull(nodes);
        Assert.Single(nodes);
        Assert.Contains(nodes, n => n.Callsign == "M0LTE");
        Assert.DoesNotContain(nodes, n => n.Callsign == "G8PZT");
    }

    [Fact]
    public void GetReportingNodes_ExcludesTestCallsigns()
    {
        // Arrange
        var normalNode = _networkState.GetOrCreateNode("M0LTE");
        normalNode.IsReportingNode = true;
        
        var testNode = _networkState.GetOrCreateNode("TEST");
        testNode.IsReportingNode = true;

        // Act
        var result = _controller.GetReportingNodes() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var nodes = result.Value as IEnumerable<NodeState>;
        Assert.NotNull(nodes);
        Assert.Single(nodes);
        Assert.Contains(nodes, n => n.Callsign == "M0LTE");
        Assert.DoesNotContain(nodes, n => n.Callsign == "TEST");
    }

    [Fact]
    public void GetNodesByBaseCallsign_ReturnsAllSSIDs()
    {
        // Arrange
        _networkState.GetOrCreateNode("M0LTE");
        _networkState.GetOrCreateNode("M0LTE-1");
        _networkState.GetOrCreateNode("M0LTE-2");
        _networkState.GetOrCreateNode("G8PZT"); // Different base

        // Act
        var result = _controller.GetNodesByBaseCallsign("M0LTE") as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var nodes = (result.Value as IEnumerable<NodeState>)?.ToList();
        Assert.NotNull(nodes);
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
        _networkState.GetOrCreateNode("M0LTE");
        _networkState.GetOrCreateNode("m0lte-1");

        // Act
        var result = _controller.GetNodesByBaseCallsign("m0lte") as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var nodes = (result.Value as IEnumerable<NodeState>)?.ToList();
        Assert.NotNull(nodes);
        Assert.Equal(2, nodes.Count);
    }

    [Fact]
    public void GetNodesByBaseCallsign_ExcludesTestCallsigns()
    {
        // Arrange
        _networkState.GetOrCreateNode("TEST");
        _networkState.GetOrCreateNode("TEST-1");

        // Act
        var result = _controller.GetNodesByBaseCallsign("TEST") as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var nodes = (result.Value as IEnumerable<NodeState>)?.ToList();
        Assert.NotNull(nodes);
        Assert.Empty(nodes);
    }

    [Fact]
    public void GetNodesByBaseCallsign_ReturnsBadRequest_WhenBaseCallsignIsEmpty()
    {
        // Act
        var result = _controller.GetNodesByBaseCallsign("");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetNodesByBaseCallsign_ReturnsBadRequest_WhenBaseCallsignIsNull()
    {
        // Act
        var result = _controller.GetNodesByBaseCallsign(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetNodesByBaseCallsign_ReturnsEmptyList_WhenNoNodesFound()
    {
        // Act
        var result = _controller.GetNodesByBaseCallsign("NONEXISTENT") as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var nodes = (result.Value as IEnumerable<NodeState>)?.ToList();
        Assert.NotNull(nodes);
        Assert.Empty(nodes);
    }
}
