using Microsoft.AspNetCore.Mvc;
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
        _networkState = Substitute.For<INetworkStateService>();
        _logger = Substitute.For<ILogger<LinksController>>();
        _controller = new LinksController(_networkState, _logger);
    }

    [Fact]
    public void GetAllLinks_ReturnsAllLinks_ExcludingTestCallsigns()
    {
        // Arrange
        var links = new Dictionary<string, LinkState>
        {
            ["G8PZT<->M0LTE"] = new LinkState 
            { 
                CanonicalKey = "G8PZT<->M0LTE",
                Endpoint1 = "G8PZT",
                Endpoint2 = "M0LTE"
            },
            ["M0LTE<->TEST"] = new LinkState 
            { 
                CanonicalKey = "M0LTE<->TEST",
                Endpoint1 = "M0LTE",
                Endpoint2 = "TEST"
            },
            ["TEST<->TEST-5"] = new LinkState 
            { 
                CanonicalKey = "TEST<->TEST-5",
                Endpoint1 = "TEST",
                Endpoint2 = "TEST-5"
            }
        };

        _networkState.GetAllLinks().Returns(links);
        _networkState.IsTestCallsign("TEST").Returns(true);
        _networkState.IsTestCallsign("TEST-5").Returns(true);
        _networkState.IsTestCallsign("M0LTE").Returns(false);
        _networkState.IsTestCallsign("G8PZT").Returns(false);
        _networkState.IsHiddenCallsign(Arg.Any<string>()).Returns(false);

        // Act
        var result = _controller.GetAllLinks();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLinks = Assert.IsAssignableFrom<IEnumerable<LinkState>>(okResult.Value);
        Assert.Single(returnedLinks);
        Assert.Contains(returnedLinks, l => l.CanonicalKey == "G8PZT<->M0LTE");
    }

    [Fact]
    public void GetLink_ReturnsLink_WhenLinkExists()
    {
        // Arrange
        var link = new LinkState 
        { 
            CanonicalKey = "G8PZT<->M0LTE",
            Endpoint1 = "G8PZT",
            Endpoint2 = "M0LTE"
        };
        _networkState.GetLink("G8PZT<->M0LTE").Returns(link);

        // Act
        var result = _controller.GetLink("G8PZT<->M0LTE");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLink = Assert.IsType<LinkState>(okResult.Value);
        Assert.Equal("G8PZT<->M0LTE", returnedLink.CanonicalKey);
    }

    [Fact]
    public void GetLink_ReturnsNotFound_WhenLinkDoesNotExist()
    {
        // Arrange
        _networkState.GetLink("NONEXISTENT").Returns((LinkState?)null);

        // Act
        var result = _controller.GetLink("NONEXISTENT");

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public void GetLinksForNode_ReturnsLinksForNode_ExcludingTest()
    {
        // Arrange
        var links = new[]
        {
            new LinkState 
            { 
                CanonicalKey = "G8PZT<->M0LTE",
                Endpoint1 = "G8PZT",
                Endpoint2 = "M0LTE"
            },
            new LinkState 
            { 
                CanonicalKey = "M0LTE<->TEST",
                Endpoint1 = "M0LTE",
                Endpoint2 = "TEST"
            }
        };

        _networkState.GetLinksForNode("M0LTE").Returns(links);
        _networkState.IsTestCallsign("M0LTE").Returns(false);
        _networkState.IsTestCallsign("TEST").Returns(true);
        _networkState.IsTestCallsign("G8PZT").Returns(false);
        _networkState.IsHiddenCallsign("M0LTE").Returns(false);
        _networkState.IsHiddenCallsign("TEST").Returns(false);
        _networkState.IsHiddenCallsign("G8PZT").Returns(false);

        // Act
        var result = _controller.GetLinksForNode("M0LTE");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLinks = Assert.IsAssignableFrom<IEnumerable<LinkState>>(okResult.Value);
        Assert.Single(returnedLinks);
        Assert.Contains(returnedLinks, l => l.CanonicalKey == "G8PZT<->M0LTE");
    }

    [Fact]
    public void GetLinksForNode_IncludesTestLinks_WhenRequestingTestNode()
    {
        // Arrange
        var links = new[]
        {
            new LinkState 
            { 
                CanonicalKey = "M0LTE<->TEST",
                Endpoint1 = "M0LTE",
                Endpoint2 = "TEST"
            },
            new LinkState 
            { 
                CanonicalKey = "TEST<->TEST-5",
                Endpoint1 = "TEST",
                Endpoint2 = "TEST-5"
            }
        };

        _networkState.GetLinksForNode("TEST").Returns(links);
        _networkState.IsTestCallsign("TEST").Returns(true);
        _networkState.IsTestCallsign("TEST-5").Returns(true);
        _networkState.IsTestCallsign("M0LTE").Returns(false);
        _networkState.IsHiddenCallsign("TEST").Returns(false);

        // Act
        var result = _controller.GetLinksForNode("TEST");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLinks = Assert.IsAssignableFrom<IEnumerable<LinkState>>(okResult.Value);
        Assert.Equal(2, returnedLinks.Count());
    }

    [Fact]
    public void GetLinksForBaseCallsign_ReturnsLinksForAllSSIDs()
    {
        // Arrange
        var nodes = new[]
        {
            new NodeState { Callsign = "M0LTE" },
            new NodeState { Callsign = "M0LTE-1" }
        };

        var links = new[]
        {
            new LinkState 
            { 
                CanonicalKey = "G8PZT<->M0LTE",
                Endpoint1 = "G8PZT",
                Endpoint2 = "M0LTE",
                Status = LinkStatus.Active,
                LastUpdate = DateTime.UtcNow
            },
            new LinkState 
            { 
                CanonicalKey = "M0LTE-1<->M0XYZ",
                Endpoint1 = "M0LTE-1",
                Endpoint2 = "M0XYZ",
                Status = LinkStatus.Active,
                LastUpdate = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        _networkState.GetNodesByBaseCallsign("M0LTE").Returns(nodes);
        _networkState.GetLinksForNode("M0LTE").Returns([links[0]]);
        _networkState.GetLinksForNode("M0LTE-1").Returns([links[1]]);
        _networkState.IsTestCallsign(Arg.Any<string>()).Returns(false);
        _networkState.IsHiddenCallsign("M0LTE").Returns(false);
        _networkState.IsHiddenCallsign(Arg.Any<string>()).Returns(false);

        // Act
        var result = _controller.GetLinksForBaseCallsign("M0LTE");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLinks = Assert.IsAssignableFrom<IEnumerable<LinkState>>(okResult.Value).ToList();
        Assert.Equal(2, returnedLinks.Count);
    }

    [Fact]
    public void GetLinksForBaseCallsign_OrdersByActiveStatusThenLastUpdate()
    {
        // Arrange
        var nodes = new[]
        {
            new NodeState { Callsign = "M0LTE" }
        };

        var now = DateTime.UtcNow;
        var links = new[]
        {
            new LinkState 
            { 
                CanonicalKey = "G8PZT<->M0LTE",
                Endpoint1 = "G8PZT",
                Endpoint2 = "M0LTE",
                Status = LinkStatus.Disconnected,
                LastUpdate = now
            },
            new LinkState 
            { 
                CanonicalKey = "M0LTE<->M0XYZ",
                Endpoint1 = "M0LTE",
                Endpoint2 = "M0XYZ",
                Status = LinkStatus.Active,
                LastUpdate = now.AddMinutes(-10)
            },
            new LinkState 
            { 
                CanonicalKey = "M0ABC<->M0LTE",
                Endpoint1 = "M0ABC",
                Endpoint2 = "M0LTE",
                Status = LinkStatus.Active,
                LastUpdate = now
            }
        };

        _networkState.GetNodesByBaseCallsign("M0LTE").Returns(nodes);
        _networkState.GetLinksForNode("M0LTE").Returns(links);
        _networkState.IsTestCallsign(Arg.Any<string>()).Returns(false);

        // Act
        var result = _controller.GetLinksForBaseCallsign("M0LTE");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLinks = Assert.IsAssignableFrom<IEnumerable<LinkState>>(okResult.Value).ToList();
        
        // Active links should come first
        Assert.Equal(LinkStatus.Active, returnedLinks[0].Status);
        Assert.Equal(LinkStatus.Active, returnedLinks[1].Status);
        Assert.Equal(LinkStatus.Disconnected, returnedLinks[2].Status);
        
        // Among active links, most recent should be first
        Assert.Equal("M0ABC<->M0LTE", returnedLinks[0].CanonicalKey);
    }

    [Fact]
    public void GetLinksForBaseCallsign_RemovesDuplicates()
    {
        // Arrange
        var nodes = new[]
        {
            new NodeState { Callsign = "M0LTE" },
            new NodeState { Callsign = "M0LTE-1" }
        };

        var sharedLink = new LinkState 
        { 
            CanonicalKey = "G8PZT<->M0LTE",
            Endpoint1 = "G8PZT",
            Endpoint2 = "M0LTE",
            Status = LinkStatus.Active,
            LastUpdate = DateTime.UtcNow
        };

        // Both nodes might return the same link
        _networkState.GetNodesByBaseCallsign("M0LTE").Returns(nodes);
        _networkState.GetLinksForNode("M0LTE").Returns([sharedLink]);
        _networkState.GetLinksForNode("M0LTE-1").Returns([sharedLink]);
        _networkState.IsTestCallsign(Arg.Any<string>()).Returns(false);

        // Act
        var result = _controller.GetLinksForBaseCallsign("M0LTE");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLinks = Assert.IsAssignableFrom<IEnumerable<LinkState>>(okResult.Value).ToList();
        Assert.Single(returnedLinks);
    }

    [Fact]
    public void GetLinksForBaseCallsign_IncludesTestLinks_WhenRequestingTestBase()
    {
        // Arrange
        var nodes = new[]
        {
            new NodeState { Callsign = "TEST" }
        };

        var links = new[]
        {
            new LinkState 
            { 
                CanonicalKey = "TEST<->TEST-5",
                Endpoint1 = "TEST",
                Endpoint2 = "TEST-5",
                Status = LinkStatus.Active,
                LastUpdate = DateTime.UtcNow
            }
        };

        _networkState.GetNodesByBaseCallsign("TEST").Returns(nodes);
        _networkState.GetLinksForNode("TEST").Returns(links);
        _networkState.IsTestCallsign(Arg.Any<string>()).Returns(true);
        _networkState.IsHiddenCallsign(Arg.Any<string>()).Returns(false);

        // Act
        var result = _controller.GetLinksForBaseCallsign("TEST");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLinks = Assert.IsAssignableFrom<IEnumerable<LinkState>>(okResult.Value);
        Assert.Single(returnedLinks);
    }

    [Fact]
    public void GetAllLinks_ExcludesHiddenCallsigns()
    {
        // Arrange
        var links = new Dictionary<string, LinkState>
        {
            ["G8PZT<->M0LTE"] = new LinkState 
            { 
                CanonicalKey = "G8PZT<->M0LTE",
                Endpoint1 = "G8PZT",
                Endpoint2 = "M0LTE"
            },
            ["M0LTE<->M2"] = new LinkState 
            { 
                CanonicalKey = "M0LTE<->M2",
                Endpoint1 = "M0LTE",
                Endpoint2 = "M2"
            },
            ["M2<->M2-5"] = new LinkState 
            { 
                CanonicalKey = "M2<->M2-5",
                Endpoint1 = "M2",
                Endpoint2 = "M2-5"
            }
        };

        _networkState.GetAllLinks().Returns(links);
        _networkState.IsTestCallsign(Arg.Any<string>()).Returns(false);
        _networkState.IsHiddenCallsign("M2").Returns(true);
        _networkState.IsHiddenCallsign("M2-5").Returns(true);
        _networkState.IsHiddenCallsign("M0LTE").Returns(false);
        _networkState.IsHiddenCallsign("G8PZT").Returns(false);

        // Act
        var result = _controller.GetAllLinks();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLinks = Assert.IsAssignableFrom<IEnumerable<LinkState>>(okResult.Value);
        Assert.Single(returnedLinks);
        Assert.Contains(returnedLinks, l => l.CanonicalKey == "G8PZT<->M0LTE");
    }

    [Fact]
    public void GetLinksForNode_ExcludesHiddenCallsigns()
    {
        // Arrange
        var links = new[]
        {
            new LinkState 
            { 
                CanonicalKey = "G8PZT<->M0LTE",
                Endpoint1 = "G8PZT",
                Endpoint2 = "M0LTE"
            },
            new LinkState 
            { 
                CanonicalKey = "M0LTE<->M2",
                Endpoint1 = "M0LTE",
                Endpoint2 = "M2"
            }
        };

        _networkState.GetLinksForNode("M0LTE").Returns(links);
        _networkState.IsTestCallsign(Arg.Any<string>()).Returns(false);
        _networkState.IsHiddenCallsign("M0LTE").Returns(false);
        _networkState.IsHiddenCallsign("M2").Returns(true);
        _networkState.IsHiddenCallsign("G8PZT").Returns(false);

        // Act
        var result = _controller.GetLinksForNode("M0LTE");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLinks = Assert.IsAssignableFrom<IEnumerable<LinkState>>(okResult.Value);
        Assert.Single(returnedLinks);
        Assert.Contains(returnedLinks, l => l.CanonicalKey == "G8PZT<->M0LTE");
    }

    [Fact]
    public void GetLinksForNode_IncludesHiddenLinks_WhenRequestingHiddenCallsign()
    {
        // Arrange
        var links = new[]
        {
            new LinkState 
            { 
                CanonicalKey = "M0LTE<->M2",
                Endpoint1 = "M0LTE",
                Endpoint2 = "M2"
            },
            new LinkState 
            { 
                CanonicalKey = "M2<->M2-5",
                Endpoint1 = "M2",
                Endpoint2 = "M2-5"
            }
        };

        _networkState.GetLinksForNode("M2").Returns(links);
        _networkState.IsTestCallsign(Arg.Any<string>()).Returns(false);
        _networkState.IsHiddenCallsign("M2").Returns(true);

        // Act
        var result = _controller.GetLinksForNode("M2");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLinks = Assert.IsAssignableFrom<IEnumerable<LinkState>>(okResult.Value);
        Assert.Equal(2, returnedLinks.Count());
    }
}
