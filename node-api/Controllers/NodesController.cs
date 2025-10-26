using Microsoft.AspNetCore.Mvc;
using node_api.Services;

namespace node_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NodesController : ControllerBase
{
    private readonly INetworkStateService _networkState;
    private readonly ILogger<NodesController> _logger;

    public NodesController(
        INetworkStateService networkState,
        ILogger<NodesController> logger)
    {
        _networkState = networkState;
        _logger = logger;
    }

    /// <summary>
    /// Get all nodes currently known to the system
    /// </summary>
    [HttpGet]
    public IActionResult GetAllNodes()
    {
        var nodes = _networkState.GetAllNodes();
        _logger.LogInformation("GetAllNodes called, returning {Count} nodes", nodes.Count);
        return Ok(nodes.Values);
    }

    /// <summary>
    /// Get a specific node by callsign
    /// </summary>
    [HttpGet("{callsign}")]
    public IActionResult GetNode(string callsign)
    {
        var node = _networkState.GetNode(callsign);
        
        if (node == null)
        {
            return NotFound(new { message = $"Node {callsign} not found" });
        }

        return Ok(node);
    }

    /// <summary>
    /// Get all SSIDs for a base callsign (e.g., M0LTE returns M0LTE, M0LTE-1, M0LTE-2, etc.)
    /// </summary>
    [HttpGet("base/{baseCallsign}")]
    public IActionResult GetNodesByBaseCallsign(string baseCallsign)
    {
        var nodes = _networkState.GetNodesByBaseCallsign(baseCallsign);
        return Ok(nodes);
    }
}
