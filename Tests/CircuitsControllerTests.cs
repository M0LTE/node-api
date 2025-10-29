using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using node_api.Controllers;
using node_api.Models.NetworkState;
using node_api.Services;
using Xunit;

namespace Tests;

public class CircuitsControllerTests
{
    private readonly INetworkStateService _networkState;
    private readonly ILogger<CircuitsController> _logger;
    private readonly CircuitsController _controller;

    public CircuitsControllerTests()
    {
        _networkState = Substitute.For<INetworkStateService>();
        _logger = Substitute.For<ILogger<CircuitsController>>();
        _controller = new CircuitsController(_networkState, _logger);
    }

    [Fact]
    public void GetAllCircuits_ReturnsAllCircuits_ExcludingTestCallsigns()
    {
        // Arrange
        var circuits = new Dictionary<string, CircuitState>
        {
            ["G8PZT:1234<->M0LTE:5678"] = new CircuitState 
            { 
                CanonicalKey = "G8PZT:1234<->M0LTE:5678",
                Endpoint1 = "G8PZT:1234",
                Endpoint2 = "M0LTE:5678"
            },
            ["M0LTE@M0LTE:1111<->TEST@TEST:2222"] = new CircuitState 
            { 
                CanonicalKey = "M0LTE@M0LTE:1111<->TEST@TEST:2222",
                Endpoint1 = "M0LTE@M0LTE:1111",
                Endpoint2 = "TEST@TEST:2222"
            },
            ["TEST:1111<->TEST-5:2222"] = new CircuitState 
            { 
                CanonicalKey = "TEST:1111<->TEST-5:2222",
                Endpoint1 = "TEST:1111",
                Endpoint2 = "TEST-5:2222"
            }
        };

        _networkState.GetAllCircuits().Returns(circuits);
        _networkState.IsTestCallsign("TEST").Returns(true);
        _networkState.IsTestCallsign("TEST-5").Returns(true);
        _networkState.IsTestCallsign("M0LTE").Returns(false);
        _networkState.IsTestCallsign("G8PZT").Returns(false);

        // Act
        var result = _controller.GetAllCircuits();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCircuits = Assert.IsAssignableFrom<IEnumerable<CircuitState>>(okResult.Value);
        Assert.Single(returnedCircuits);
        Assert.Contains(returnedCircuits, c => c.CanonicalKey == "G8PZT:1234<->M0LTE:5678");
    }

    [Fact]
    public void GetCircuit_ReturnsCircuit_WhenCircuitExists()
    {
        // Arrange
        var circuit = new CircuitState 
        { 
            CanonicalKey = "G8PZT:1234<->M0LTE:5678",
            Endpoint1 = "G8PZT:1234",
            Endpoint2 = "M0LTE:5678"
        };
        _networkState.GetCircuit("G8PZT:1234<->M0LTE:5678").Returns(circuit);

        // Act
        var result = _controller.GetCircuit("G8PZT:1234<->M0LTE:5678");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCircuit = Assert.IsType<CircuitState>(okResult.Value);
        Assert.Equal("G8PZT:1234<->M0LTE:5678", returnedCircuit.CanonicalKey);
    }

    [Fact]
    public void GetCircuit_ReturnsNotFound_WhenCircuitDoesNotExist()
    {
        // Arrange
        _networkState.GetCircuit("NONEXISTENT").Returns((CircuitState?)null);

        // Act
        var result = _controller.GetCircuit("NONEXISTENT");

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public void GetCircuitsForNode_ReturnsCircuitsForNode_ExcludingTest()
    {
        // Arrange
        var circuits = new[]
        {
            new CircuitState 
            { 
                CanonicalKey = "G8PZT:1234<->M0LTE:5678",
                Endpoint1 = "G8PZT:1234",
                Endpoint2 = "M0LTE:5678"
            },
            new CircuitState 
            { 
                CanonicalKey = "M0LTE:5678<->TEST:9999",
                Endpoint1 = "M0LTE:5678",
                Endpoint2 = "TEST:9999"
            }
        };

        _networkState.GetCircuitsForNode("M0LTE").Returns(circuits);
        _networkState.IsTestCallsign("M0LTE").Returns(false);
        _networkState.IsTestCallsign("TEST").Returns(true);
        _networkState.IsTestCallsign("G8PZT").Returns(false);

        // Act
        var result = _controller.GetCircuitsForNode("M0LTE");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCircuits = Assert.IsAssignableFrom<IEnumerable<CircuitState>>(okResult.Value);
        Assert.Single(returnedCircuits);
        Assert.Contains(returnedCircuits, c => c.CanonicalKey == "G8PZT:1234<->M0LTE:5678");
    }

    [Fact]
    public void GetCircuitsForNode_HandlesComplexAddressFormats()
    {
        // Arrange - Circuit addresses can be "CALL@NODE:ID" format
        var circuits = new[]
        {
            new CircuitState 
            { 
                CanonicalKey = "G8PZT@G8PZT:14c0<->M0LTE@M0LTE:abcd",
                Endpoint1 = "G8PZT@G8PZT:14c0",
                Endpoint2 = "M0LTE@M0LTE:abcd"
            },
            new CircuitState 
            { 
                CanonicalKey = "M0LTE-4:0001<->TEST@TEST:9999",
                Endpoint1 = "M0LTE-4:0001",
                Endpoint2 = "TEST@TEST:9999"
            }
        };

        _networkState.GetCircuitsForNode("M0LTE").Returns(circuits);
        _networkState.IsTestCallsign("M0LTE").Returns(false);
        _networkState.IsTestCallsign("M0LTE-4").Returns(false);
        _networkState.IsTestCallsign("TEST").Returns(true);
        _networkState.IsTestCallsign("G8PZT").Returns(false);

        // Act
        var result = _controller.GetCircuitsForNode("M0LTE");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCircuits = Assert.IsAssignableFrom<IEnumerable<CircuitState>>(okResult.Value);
        Assert.Single(returnedCircuits);
        Assert.Contains(returnedCircuits, c => c.Endpoint1.Contains("G8PZT"));
    }

    [Fact]
    public void GetCircuitsForNode_IncludesTestCircuits_WhenRequestingTestNode()
    {
        // Arrange
        var circuits = new[]
        {
            new CircuitState 
            { 
                CanonicalKey = "M0LTE:5678<->TEST:9999",
                Endpoint1 = "M0LTE:5678",
                Endpoint2 = "TEST:9999"
            },
            new CircuitState 
            { 
                CanonicalKey = "TEST:1111<->TEST-5:2222",
                Endpoint1 = "TEST:1111",
                Endpoint2 = "TEST-5:2222"
            }
        };

        _networkState.GetCircuitsForNode("TEST").Returns(circuits);
        _networkState.IsTestCallsign("TEST").Returns(true);
        _networkState.IsTestCallsign("TEST-5").Returns(true);
        _networkState.IsTestCallsign("M0LTE").Returns(false);

        // Act
        var result = _controller.GetCircuitsForNode("TEST");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCircuits = Assert.IsAssignableFrom<IEnumerable<CircuitState>>(okResult.Value);
        Assert.Equal(2, returnedCircuits.Count());
    }

    [Fact]
    public void GetCircuitsForBaseCallsign_ReturnsCircuitsForAllSSIDs()
    {
        // Arrange
        var nodes = new[]
        {
            new NodeState { Callsign = "M0LTE" },
            new NodeState { Callsign = "M0LTE-1" }
        };

        var circuits = new[]
        {
            new CircuitState 
            { 
                CanonicalKey = "G8PZT:1234<->M0LTE:5678",
                Endpoint1 = "G8PZT:1234",
                Endpoint2 = "M0LTE:5678",
                Status = CircuitStatus.Active,
                LastUpdate = DateTime.UtcNow
            },
            new CircuitState 
            { 
                CanonicalKey = "M0LTE-1:1111<->M0XYZ:2222",
                Endpoint1 = "M0LTE-1:1111",
                Endpoint2 = "M0XYZ:2222",
                Status = CircuitStatus.Active,
                LastUpdate = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        _networkState.GetNodesByBaseCallsign("M0LTE").Returns(nodes);
        _networkState.GetCircuitsForNode("M0LTE").Returns([circuits[0]]);
        _networkState.GetCircuitsForNode("M0LTE-1").Returns([circuits[1]]);
        _networkState.IsTestCallsign(Arg.Any<string>()).Returns(false);

        // Act
        var result = _controller.GetCircuitsForBaseCallsign("M0LTE");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCircuits = Assert.IsAssignableFrom<IEnumerable<CircuitState>>(okResult.Value).ToList();
        Assert.Equal(2, returnedCircuits.Count);
    }

    [Fact]
    public void GetCircuitsForBaseCallsign_OrdersByActiveStatusThenLastUpdate()
    {
        // Arrange
        var nodes = new[]
        {
            new NodeState { Callsign = "M0LTE" }
        };

        var now = DateTime.UtcNow;
        var circuits = new[]
        {
            new CircuitState 
            { 
                CanonicalKey = "G8PZT:1111<->M0LTE:2222",
                Endpoint1 = "G8PZT:1111",
                Endpoint2 = "M0LTE:2222",
                Status = CircuitStatus.Disconnected,
                LastUpdate = now
            },
            new CircuitState 
            { 
                CanonicalKey = "M0LTE:3333<->M0XYZ:4444",
                Endpoint1 = "M0LTE:3333",
                Endpoint2 = "M0XYZ:4444",
                Status = CircuitStatus.Active,
                LastUpdate = now.AddMinutes(-10)
            },
            new CircuitState 
            { 
                CanonicalKey = "M0ABC:5555<->M0LTE:6666",
                Endpoint1 = "M0ABC:5555",
                Endpoint2 = "M0LTE:6666",
                Status = CircuitStatus.Active,
                LastUpdate = now
            }
        };

        _networkState.GetNodesByBaseCallsign("M0LTE").Returns(nodes);
        _networkState.GetCircuitsForNode("M0LTE").Returns(circuits);
        _networkState.IsTestCallsign(Arg.Any<string>()).Returns(false);

        // Act
        var result = _controller.GetCircuitsForBaseCallsign("M0LTE");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCircuits = Assert.IsAssignableFrom<IEnumerable<CircuitState>>(okResult.Value).ToList();
        
        // Active circuits should come first
        Assert.Equal(CircuitStatus.Active, returnedCircuits[0].Status);
        Assert.Equal(CircuitStatus.Active, returnedCircuits[1].Status);
        Assert.Equal(CircuitStatus.Disconnected, returnedCircuits[2].Status);
        
        // Among active circuits, most recent should be first
        Assert.Equal("M0ABC:5555<->M0LTE:6666", returnedCircuits[0].CanonicalKey);
    }

    [Fact]
    public void GetCircuitsForBaseCallsign_RemovesDuplicates()
    {
        // Arrange
        var nodes = new[]
        {
            new NodeState { Callsign = "M0LTE" },
            new NodeState { Callsign = "M0LTE-1" }
        };

        var sharedCircuit = new CircuitState 
        { 
            CanonicalKey = "G8PZT:1234<->M0LTE:5678",
            Endpoint1 = "G8PZT:1234",
            Endpoint2 = "M0LTE:5678",
            Status = CircuitStatus.Active,
            LastUpdate = DateTime.UtcNow
        };

        _networkState.GetNodesByBaseCallsign("M0LTE").Returns(nodes);
        _networkState.GetCircuitsForNode("M0LTE").Returns([sharedCircuit]);
        _networkState.GetCircuitsForNode("M0LTE-1").Returns([sharedCircuit]);
        _networkState.IsTestCallsign(Arg.Any<string>()).Returns(false);

        // Act
        var result = _controller.GetCircuitsForBaseCallsign("M0LTE");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCircuits = Assert.IsAssignableFrom<IEnumerable<CircuitState>>(okResult.Value).ToList();
        Assert.Single(returnedCircuits);
    }

    [Fact]
    public void GetCircuitsForBaseCallsign_IncludesTestCircuits_WhenRequestingTestBase()
    {
        // Arrange
        var nodes = new[]
        {
            new NodeState { Callsign = "TEST" }
        };

        var circuits = new[]
        {
            new CircuitState 
            { 
                CanonicalKey = "TEST:1111<->TEST-5:2222",
                Endpoint1 = "TEST:1111",
                Endpoint2 = "TEST-5:2222",
                Status = CircuitStatus.Active,
                LastUpdate = DateTime.UtcNow
            }
        };

        _networkState.GetNodesByBaseCallsign("TEST").Returns(nodes);
        _networkState.GetCircuitsForNode("TEST").Returns(circuits);
        _networkState.IsTestCallsign(Arg.Any<string>()).Returns(true);

        // Act
        var result = _controller.GetCircuitsForBaseCallsign("TEST");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCircuits = Assert.IsAssignableFrom<IEnumerable<CircuitState>>(okResult.Value);
        Assert.Single(returnedCircuits);
    }
}
