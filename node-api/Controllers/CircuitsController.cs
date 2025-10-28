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
    /// Get all circuits currently known to the system (excluding TEST callsigns)
    /// </summary>
    [HttpGet]
    public IActionResult GetAllCircuits()
    {
        var circuits = _networkState.GetAllCircuits()
            .Values
            .Where(c => !ContainsTestCallsign(c.Endpoint1) && 
                       !ContainsTestCallsign(c.Endpoint2));
        
        _logger.LogInformation("GetAllCircuits called, returning {Count} circuits", circuits.Count());
        return Ok(circuits);
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

        // Don't filter TEST when explicitly requested by canonical key
        return Ok(circuit);
    }

    /// <summary>
    /// Get all circuits involving a specific callsign (excludes TEST unless explicitly requesting TEST)
    /// </summary>
    [HttpGet("node/{callsign}")]
    public IActionResult GetCircuitsForNode(string callsign)
    {
        var circuits = _networkState.GetCircuitsForNode(callsign);
        
        // Don't filter if explicitly requesting TEST callsign
        if (!ContainsTestCallsign(callsign))
        {
            circuits = circuits.Where(c => !ContainsTestCallsign(c.Endpoint1) && 
                                          !ContainsTestCallsign(c.Endpoint2));
        }
        
        return Ok(circuits);
    }

    /// <summary>
    /// Get all circuits involving any SSID of a base callsign (excludes TEST unless explicitly requesting TEST)
    /// </summary>
    [HttpGet("base/{baseCallsign}")]
    public IActionResult GetCircuitsForBaseCallsign(string baseCallsign)
    {
        var nodes = _networkState.GetNodesByBaseCallsign(baseCallsign);
        var allCircuits = nodes
            .SelectMany(node => _networkState.GetCircuitsForNode(node.Callsign))
            .DistinctBy(circuit => circuit.CanonicalKey);
        
        // Don't filter if explicitly requesting TEST base callsign
        if (!baseCallsign.Equals("TEST", StringComparison.OrdinalIgnoreCase))
        {
            allCircuits = allCircuits.Where(c => !ContainsTestCallsign(c.Endpoint1) && 
                                                !ContainsTestCallsign(c.Endpoint2));
        }
        
        allCircuits = allCircuits
            .OrderByDescending(circuit => circuit.Status == Models.NetworkState.CircuitStatus.Active)
            .ThenByDescending(circuit => circuit.LastUpdate);
        
        return Ok(allCircuits);
    }

    private bool ContainsTestCallsign(string address)
    {
        // Circuit addresses can be complex like "G8PZT@G8PZT:14c0" or "G8PZT-4:0001"
        // Extract the callsign part before @ or :
        var callsignPart = address.Split('@', ':')[0];
        return _networkState.IsTestCallsign(callsignPart);
    }
}
