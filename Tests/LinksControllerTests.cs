using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using node_api.Controllers;
using node_api.Models.NetworkState;
using node_api.Services;
using Xunit;

namespace Tests;

public class LinksControllerTests
{
    private readonly INetworkStateService _networkState;
    private readonly ILogger<LinksController> _logger;
    private readonly LinksController _controller;

    public LinksControllerTests()
    {
        _logger = Substitute.For<ILogger<LinksController>>();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        
        var networkStateLogger = Substitute.For<ILogger<NetworkStateService>>();
        _networkState = new NetworkStateService(networkStateLogger, configuration);
        
        _controller = new LinksController(_networkState, new PortMetadataStore(), _logger);
    }

    [Fact]
    public void GetAllLinks_ReturnsAllLinks()
    {
        // Arrange
        _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        _networkState.GetOrCreateLink("M0LTE", "M0XYZ");

        // Act
        var result = _controller.GetAllLinks() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var links = (result.Value as IEnumerable<LinkState>)?.ToList();
        Assert.NotNull(links);
        Assert.Equal(2, links.Count);
    }

    [Fact]
    public void GetAllLinks_ExcludesTestCallsigns()
    {
        // Arrange
        _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        _networkState.GetOrCreateLink("TEST", "M0XYZ");

        // Act
        var result = _controller.GetAllLinks() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var links = (result.Value as IEnumerable<LinkState>)?.ToList();
        Assert.NotNull(links);
        Assert.Single(links);
        Assert.Contains(links, l => l.Endpoint1 == "G8PZT" || l.Endpoint2 == "G8PZT");
        Assert.DoesNotContain(links, l => l.Endpoint1 == "TEST" || l.Endpoint2 == "TEST");
    }

    [Fact]
    public void GetLinksByBaseCallsign_ReturnsLinksForAllSSIDs()
    {
        // Arrange
        _networkState.GetOrCreateNode("M0LTE");
        _networkState.GetOrCreateNode("M0LTE-1");
        _networkState.GetOrCreateNode("M0LTE-2");
        
        _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        _networkState.GetOrCreateLink("M0LTE-1", "M0XYZ");
        _networkState.GetOrCreateLink("M0LTE-2", "M0ABC");
        _networkState.GetOrCreateLink("G1ABC", "G8PZT"); // Should not be included

        // Act
        var result = _controller.GetLinksByBaseCallsign("M0LTE") as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var links = (result.Value as IEnumerable<LinkState>)?.ToList();
        Assert.NotNull(links);
        Assert.Equal(3, links.Count);
        
        // Verify all links involve M0LTE, M0LTE-1, or M0LTE-2
        Assert.All(links, link =>
        {
            var involvesM0LTE = 
                link.Endpoint1.StartsWith("M0LTE", StringComparison.OrdinalIgnoreCase) ||
                link.Endpoint2.StartsWith("M0LTE", StringComparison.OrdinalIgnoreCase);
            Assert.True(involvesM0LTE);
        });
    }

    [Fact]
    public void GetLinksByBaseCallsign_IsCaseInsensitive()
    {
        // Arrange
        _networkState.GetOrCreateNode("M0LTE");
        _networkState.GetOrCreateNode("m0lte-1");
        _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        _networkState.GetOrCreateLink("m0lte-1", "M0XYZ");

        // Act
        var result = _controller.GetLinksByBaseCallsign("m0lte") as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var links = (result.Value as IEnumerable<LinkState>)?.ToList();
        Assert.NotNull(links);
        Assert.Equal(2, links.Count);
    }

    [Fact]
    public void GetLinksByBaseCallsign_ExcludesTestCallsigns()
    {
        // Arrange
        _networkState.GetOrCreateNode("M0LTE");
        _networkState.GetOrCreateLink("M0LTE", "TEST");
        _networkState.GetOrCreateLink("M0LTE", "G8PZT");

        // Act
        var result = _controller.GetLinksByBaseCallsign("M0LTE") as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var links = (result.Value as IEnumerable<LinkState>)?.ToList();
        Assert.NotNull(links);
        Assert.Single(links);
        Assert.DoesNotContain(links, l => l.Endpoint1 == "TEST" || l.Endpoint2 == "TEST");
    }

    [Fact]
    public void GetLinksByBaseCallsign_ReturnsBadRequest_WhenBaseCallsignIsEmpty()
    {
        // Act
        var result = _controller.GetLinksByBaseCallsign("");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetLinksByBaseCallsign_ReturnsBadRequest_WhenBaseCallsignIsNull()
    {
        // Act
        var result = _controller.GetLinksByBaseCallsign(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetLinksByBaseCallsign_ReturnsEmptyList_WhenNoNodesFound()
    {
        // Act
        var result = _controller.GetLinksByBaseCallsign("NONEXISTENT") as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var links = (result.Value as IEnumerable<LinkState>)?.ToList();
        Assert.NotNull(links);
        Assert.Empty(links);
    }

    [Fact]
    public void GetLinksByBaseCallsign_ReturnsEmptyList_WhenNoLinksForNode()
    {
        // Arrange
        _networkState.GetOrCreateNode("M0LTE");

        // Act
        var result = _controller.GetLinksByBaseCallsign("M0LTE") as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var links = (result.Value as IEnumerable<LinkState>)?.ToList();
        Assert.NotNull(links);
        Assert.Empty(links);
    }
}
