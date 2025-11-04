using Microsoft.AspNetCore.Mvc;
using node_api.Services;
using System.ComponentModel.DataAnnotations;

namespace node_api.Controllers;

[ApiController]
[Route("api/history/connections/l2")]
public class L2ConnectionsController(IL2ConnectionAnalysisService analysisService) : ControllerBase
{
    /// <summary>
    /// Analyze bidirectional L2 communication between two callsigns
    /// </summary>
    /// <param name="callsign1">First callsign (with SSID)</param>
    /// <param name="callsign2">Second callsign (with SSID)</param>
    /// <param name="from">Start timestamp (required)</param>
    /// <param name="to">End timestamp (required)</param>
    /// <param name="reportFrom">Filter by reporting station(s). Can be specified multiple times.</param>
    /// <param name="includeMetrics">Include aggregated metrics (default: true)</param>
    /// <param name="includeTraces">Include frame-level traces (default: true)</param>
    /// <param name="tracesLimit">Maximum number of trace results (1-500, default: 100)</param>
    /// <param name="tracesCursor">Pagination cursor for traces</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>L2 connection analysis including sessions, events, and optional traces</returns>
    [HttpGet]
    public async Task<ActionResult<L2ConnectionAnalysis>> GetAsync(
        [FromQuery][Required] string callsign1,
        [FromQuery][Required] string callsign2,
        [FromQuery][Required] DateTimeOffset from,
        [FromQuery][Required] DateTimeOffset to,
        [FromQuery] string[]? reportFrom = null,
        [FromQuery] bool includeMetrics = true,
        [FromQuery] bool includeTraces = true,
        [FromQuery] int tracesLimit = 100,
        [FromQuery] string? tracesCursor = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(callsign1))
            return BadRequest(new { error = "callsign1 is required" });
            
        if (string.IsNullOrWhiteSpace(callsign2))
            return BadRequest(new { error = "callsign2 is required" });

        if (callsign1.Equals(callsign2, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "callsign1 and callsign2 must be different" });

        tracesLimit = Math.Clamp(tracesLimit, 1, 500);

        var analysis = await analysisService.AnalyzeConnectionAsync(
            callsign1,
            callsign2,
            from,
            to,
            reportFrom,
            includeMetrics,
            includeTraces,
            tracesLimit,
            tracesCursor,
            ct);

        return Ok(analysis);
    }
}
