using Microsoft.AspNetCore.Mvc;
using node_api.Services;

namespace node_api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    /// Get all links currently known to the system
    /// </summary>
    [HttpGet]
    public IActionResult GetAllLinks()
    {
        var links = _networkState.GetAllLinks();
        _logger.LogInformation("GetAllLinks called, returning {Count} links", links.Count);
        return Ok(links.Values);
    }

    /// <summary>
    /// Get a specific link by canonical key (e.g., "M0LTE<->G8PZT-2")
    /// </summary>
    [HttpGet("{canonicalKey}")]
    public IActionResult GetLink(string canonicalKey)
    {
        var link = _networkState.GetLink(canonicalKey);
        
        if (link == null)
        {
            return NotFound(new { message = $"Link {canonicalKey} not found" });
        }

        return Ok(link);
    }

    /// <summary>
    /// Get all links involving a specific callsign
    /// </summary>
    [HttpGet("node/{callsign}")]
    public IActionResult GetLinksForNode(string callsign)
    {
        var links = _networkState.GetLinksForNode(callsign);
        return Ok(links);
    }

    /// <summary>
    /// Get all links involving any SSID of a base callsign
    /// </summary>
    [HttpGet("base/{baseCallsign}")]
    public IActionResult GetLinksForBaseCallsign(string baseCallsign)
    {
        var nodes = _networkState.GetNodesByBaseCallsign(baseCallsign);
        var allLinks = nodes
            .SelectMany(node => _networkState.GetLinksForNode(node.Callsign))
            .DistinctBy(link => link.CanonicalKey)
            .OrderByDescending(link => link.Status == Models.NetworkState.LinkStatus.Active)
            .ThenByDescending(link => link.LastUpdate);
        
        return Ok(allLinks);
    }
}
