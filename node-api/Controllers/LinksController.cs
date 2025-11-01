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

        // Don't filter TEST when explicitly requested by canonical key
        return Ok(link);
    }

    /// <summary>
    /// Get all links involving a specific callsign (excludes TEST and hidden unless explicitly requesting them)
    /// </summary>
    [HttpGet("node/{callsign}")]
    public IActionResult GetLinksForNode(string callsign)
    {
        var links = _networkState.GetLinksForNode(callsign);
        
        // Don't filter if explicitly requesting TEST or hidden callsign
        if (!_networkState.IsTestCallsign(callsign) && !_networkState.IsHiddenCallsign(callsign))
        {
            links = links.Where(l => !_networkState.IsTestCallsign(l.Endpoint1) && 
                                    !_networkState.IsTestCallsign(l.Endpoint2) &&
                                    !_networkState.IsHiddenCallsign(l.Endpoint1) &&
                                    !_networkState.IsHiddenCallsign(l.Endpoint2));
        }
        
        return Ok(links);
    }

    /// <summary>
    /// Get all links involving any SSID of a base callsign (excludes TEST and hidden unless explicitly requesting them)
    /// </summary>
    [HttpGet("base/{baseCallsign}")]
    public IActionResult GetLinksForBaseCallsign(string baseCallsign)
    {
        var nodes = _networkState.GetNodesByBaseCallsign(baseCallsign);
        var allLinks = nodes
            .SelectMany(node => _networkState.GetLinksForNode(node.Callsign))
            .DistinctBy(link => link.CanonicalKey);
        
        // Don't filter if explicitly requesting TEST or hidden base callsign
        if (!baseCallsign.Equals("TEST", StringComparison.OrdinalIgnoreCase) && 
            !_networkState.IsHiddenCallsign(baseCallsign))
        {
            allLinks = allLinks.Where(l => !_networkState.IsTestCallsign(l.Endpoint1) && 
                                          !_networkState.IsTestCallsign(l.Endpoint2) &&
                                          !_networkState.IsHiddenCallsign(l.Endpoint1) &&
                                          !_networkState.IsHiddenCallsign(l.Endpoint2));
        }
        
        allLinks = allLinks
            .OrderByDescending(link => link.Status == Models.NetworkState.LinkStatus.Active)
            .ThenByDescending(link => link.LastUpdate);
        
        return Ok(allLinks);
    }

    /// <summary>
    /// Get all links that are currently flapping (multiple up/down transitions in a short time)
    /// </summary>
    /// <param name="flapThreshold">Minimum number of flaps to be considered flapping (default: 3)</param>
    /// <param name="windowMinutes">Time window in minutes for flap detection (default: 15)</param>
    [HttpGet("flapping")]
    public IActionResult GetFlappingLinks(
        [FromQuery] int flapThreshold = 3,
        [FromQuery] int windowMinutes = 15)
    {
        var flappingLinks = _networkState.GetAllLinks()
            .Values
            .Where(l => l.IsFlapping(flapThreshold, windowMinutes) &&
                       !_networkState.IsTestCallsign(l.Endpoint1) && 
                       !_networkState.IsTestCallsign(l.Endpoint2) &&
                       !_networkState.IsHiddenCallsign(l.Endpoint1) &&
                       !_networkState.IsHiddenCallsign(l.Endpoint2))
            .OrderByDescending(l => l.FlapCount)
            .ThenByDescending(l => l.LastFlapTime);
        
        _logger.LogInformation(
            "GetFlappingLinks called (threshold={Threshold}, window={Window}min), returning {Count} links",
            flapThreshold,
            windowMinutes,
            flappingLinks.Count());
        
        return Ok(flappingLinks);
    }
}
