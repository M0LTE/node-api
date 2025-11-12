using Microsoft.AspNetCore.Mvc;
using node_api.Models.NetworkState;
using node_api.Services;

namespace node_api.Controllers;

[ApiController]
[Route("api/network/links")]
public class LinksController : ControllerBase
{
    private readonly INetworkStateService _networkState;
    private readonly ILogger<LinksController> _logger;

    public LinksController(
        INetworkStateService networkState,
        ILogger<LinksController> logger)
    {
        _networkState = networkState;
        _logger = logger;
    }

    /// <summary>
    /// Get all links currently known to the system (excluding TEST and hidden callsigns)
    /// </summary>
    [HttpGet]
    public IActionResult GetAllLinks()
    {
        var links = _networkState.GetAllLinks()
            .Values
            .Where(l => !_networkState.IsTestCallsign(l.Endpoint1) && 
                       !_networkState.IsTestCallsign(l.Endpoint2) &&
                       !_networkState.IsHiddenCallsign(l.Endpoint1) &&
                       !_networkState.IsHiddenCallsign(l.Endpoint2));
        
        _logger.LogInformation("GetAllLinks called, returning {Count} links", links.Count());
        return Ok(links);
    }

    /// <summary>
    /// Get all links involving any SSID of a base callsign
    /// For example, base/M0LTE returns links involving M0LTE, M0LTE-1, M0LTE-2, etc.
    /// </summary>
    [HttpGet("base/{baseCallsign}")]
    public IActionResult GetLinksByBaseCallsign(string baseCallsign)
    {
        if (string.IsNullOrWhiteSpace(baseCallsign))
        {
            return BadRequest("Base callsign is required");
        }

        // Get all SSIDs for this base callsign
        var ssids = _networkState.GetNodesByBaseCallsign(baseCallsign)
            .Select(n => n.Callsign)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!ssids.Any())
        {
            _logger.LogInformation("GetLinksByBaseCallsign called for {BaseCallsign}, no nodes found", baseCallsign);
            return Ok(Enumerable.Empty<LinkState>());
        }

        // Get all links involving any of these SSIDs
        var links = _networkState.GetAllLinks()
            .Values
            .Where(l => ssids.Contains(l.Endpoint1) || ssids.Contains(l.Endpoint2))
            .Where(l => !_networkState.IsTestCallsign(l.Endpoint1) && 
                       !_networkState.IsTestCallsign(l.Endpoint2) &&
                       !_networkState.IsHiddenCallsign(l.Endpoint1) &&
                       !_networkState.IsHiddenCallsign(l.Endpoint2));

        _logger.LogInformation("GetLinksByBaseCallsign called for {BaseCallsign}, returning {Count} links", 
            baseCallsign, links.Count());
        
        return Ok(links);
    }
}
