using Microsoft.AspNetCore.Mvc;
using node_api.Services;

namespace node_api.Controllers;

[ApiController]
[Route("api/network/nodes")]
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
    /// Get only nodes that are actively reporting (sending UDP telemetry)
    /// Excludes nodes that were only discovered via events from other nodes
    /// </summary>
    [HttpGet("reporting")]
    public IActionResult GetReportingNodes()
    {
        var nodes = _networkState.GetAllNodes()
            .Values
            .Where(n => n.IsReportingNode &&
                       !_networkState.IsTestCallsign(n.Callsign) &&
                       !_networkState.IsHiddenCallsign(n.Callsign));
        
        _logger.LogInformation("GetReportingNodes called, returning {Count} reporting nodes", nodes.Count());
        return Ok(nodes);
    }

    /// <summary>
    /// Get all nodes (SSIDs) that match a base callsign
    /// For example, base/M0LTE returns M0LTE, M0LTE-1, M0LTE-2, etc.
    /// </summary>
    [HttpGet("base/{baseCallsign}")]
    public IActionResult GetNodesByBaseCallsign(string baseCallsign)
    {
        if (string.IsNullOrWhiteSpace(baseCallsign))
        {
            return BadRequest("Base callsign is required");
        }

        var nodes = _networkState.GetNodesByBaseCallsign(baseCallsign)
            .Where(n => !_networkState.IsTestCallsign(n.Callsign) &&
                       !_networkState.IsHiddenCallsign(n.Callsign));

        _logger.LogInformation("GetNodesByBaseCallsign called for {BaseCallsign}, returning {Count} nodes", 
            baseCallsign, nodes.Count());
        
        return Ok(nodes);
    }
}
