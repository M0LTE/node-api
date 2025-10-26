using Microsoft.AspNetCore.Mvc;
using node_api.Services;

namespace node_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CircuitsController : ControllerBase
{
    private readonly INetworkStateService _networkState;
    private readonly ILogger<CircuitsController> _logger;

    public CircuitsController(
        INetworkStateService networkState,
        ILogger<CircuitsController> logger)
    {
        _networkState = networkState;
        _logger = logger;
    }

    /// <summary>
    /// Get all circuits currently known to the system
    /// </summary>
    [HttpGet]
    public IActionResult GetAllCircuits()
    {
        var circuits = _networkState.GetAllCircuits();
        _logger.LogInformation("GetAllCircuits called, returning {Count} circuits", circuits.Count);
        return Ok(circuits.Values);
    }

    /// <summary>
    /// Get a specific circuit by canonical key (e.g., "M0LTE-4:0001<->G8PZT@G8PZT:14c0")
    /// </summary>
    [HttpGet("{canonicalKey}")]
    public IActionResult GetCircuit(string canonicalKey)
    {
        var circuit = _networkState.GetCircuit(canonicalKey);
        
        if (circuit == null)
        {
            return NotFound(new { message = $"Circuit {canonicalKey} not found" });
        }

        return Ok(circuit);
    }

    /// <summary>
    /// Get all circuits involving a specific callsign
    /// </summary>
    [HttpGet("node/{callsign}")]
    public IActionResult GetCircuitsForNode(string callsign)
    {
        var circuits = _networkState.GetCircuitsForNode(callsign);
        return Ok(circuits);
    }

    /// <summary>
    /// Get all circuits involving any SSID of a base callsign
    /// </summary>
    [HttpGet("base/{baseCallsign}")]
    public IActionResult GetCircuitsForBaseCallsign(string baseCallsign)
    {
        var nodes = _networkState.GetNodesByBaseCallsign(baseCallsign);
        var allCircuits = nodes
            .SelectMany(node => _networkState.GetCircuitsForNode(node.Callsign))
            .DistinctBy(circuit => circuit.CanonicalKey)
            .OrderByDescending(circuit => circuit.Status == Models.NetworkState.CircuitStatus.Active)
            .ThenByDescending(circuit => circuit.LastUpdate);
        
        return Ok(allCircuits);
    }
}
