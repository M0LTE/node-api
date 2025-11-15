using Microsoft.AspNetCore.Mvc;
using node_api.Services;
using System.Text.Json;

namespace node_api.Controllers;

[ApiController]
[Route("api/history/traces")]
public class TracesController(IL2TraceRepository l2Repository, IL3TraceRepository l3Repository) : ControllerBase
{
    /// <summary>
    /// Get L2 traces from the network history
    /// </summary>
    /// <param name="source">Filter by source callsign</param>
    /// <param name="dest">Filter by destination callsign</param>
    /// <param name="from">Filter traces from this timestamp (inclusive)</param>
    /// <param name="to">Filter traces to this timestamp (inclusive)</param>
    /// <param name="type">Filter by L2 frame type (e.g., "UI", "I", "RR")</param>
    /// <param name="reportFrom">Filter by reporting station callsign(s). Can be specified multiple times.</param>
    /// <param name="limit">Maximum number of results to return (1-500, default: 100)</param>
    /// <param name="cursor">Pagination cursor for next page</param>
    /// <param name="includeCount">Include total count of matching records (expensive operation)</param>
    /// <param name="sortOrder">Sort order by timestamp: "asc" (oldest first, default) or "desc" (newest first)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of L2 traces</returns>
    // GET /api/history/traces/l2?source=...&dest=...&from=...&to=...&type=...&reportFrom=...&reportFrom=...&limit=...&cursor=...&includeCount=...&sortOrder=...
    [HttpGet]
    [HttpGet("l2")]
    public async Task<ActionResult<PagedResult<TraceDto>>> GetL2TracesAsync(
        [FromQuery] string? source,
        [FromQuery] string? dest,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? type,
        [FromQuery] string[]? reportFrom,
        [FromQuery] int limit = 100,
        [FromQuery] string? cursor = null,
        [FromQuery] bool includeCount = false,
        [FromQuery] 
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindingBehavior(Microsoft.AspNetCore.Mvc.ModelBinding.BindingBehavior.Optional)]
        [System.ComponentModel.DataAnnotations.RegularExpression("^(asc|desc)$", ErrorMessage = "Must be 'asc' or 'desc'")]
        string sortOrder = "asc",
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 500);

        // Validate sortOrder parameter
        var order = sortOrder.ToLowerInvariant() switch
        {
            "asc" => "ASC",
            "desc" => "DESC",
            _ => "ASC" // Default to ascending if invalid value provided
        };

        var (data, next, countResult) = await l2Repository.GetTracesAsync(
            source, dest, from, to, type, reportFrom, limit, cursor, includeCount, order, ct);

        // If count was requested but failed, return error
        if (includeCount && countResult.Error != null)
        {
            return StatusCode(500, new { error = $"Failed to retrieve count: {countResult.Error}" });
        }

        return Ok(new PagedResult<TraceDto>(new PageInfo(limit, next, countResult.Value), data));
    }

    /// <summary>
    /// Get L3 (NET/ROM layer 3) traces from the network history
    /// </summary>
    /// <param name="l3Source">Filter by L3 source callsign</param>
    /// <param name="l3Dest">Filter by L3 destination callsign</param>
    /// <param name="from">Filter traces from this timestamp (inclusive)</param>
    /// <param name="to">Filter traces to this timestamp (inclusive)</param>
    /// <param name="l3Type">Filter by L3 frame type (e.g., "NetRom", "CONN REQ", "INFO")</param>
    /// <param name="reportFrom">Filter by reporting station callsign(s). Can be specified multiple times.</param>
    /// <param name="limit">Maximum number of results to return (1-500, default: 100)</param>
    /// <param name="cursor">Pagination cursor for next page</param>
    /// <param name="includeCount">Include total count of matching records (expensive operation)</param>
    /// <param name="sortOrder">Sort order by timestamp: "asc" (oldest first, default) or "desc" (newest first)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of L3 traces</returns>
    // GET /api/history/traces/l3?l3Source=...&l3Dest=...&from=...&to=...&l3Type=...&reportFrom=...&reportFrom=...&limit=...&cursor=...&includeCount=...&sortOrder=...
    [HttpGet("l3")]
    public async Task<ActionResult<PagedResult<TraceDto>>> GetL3TracesAsync(
        [FromQuery] string? l3Source,
        [FromQuery] string? l3Dest,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? l3Type,
        [FromQuery] string[]? reportFrom,
        [FromQuery] int limit = 100,
        [FromQuery] string? cursor = null,
        [FromQuery] bool includeCount = false,
        [FromQuery] 
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindingBehavior(Microsoft.AspNetCore.Mvc.ModelBinding.BindingBehavior.Optional)]
        [System.ComponentModel.DataAnnotations.RegularExpression("^(asc|desc)$", ErrorMessage = "Must be 'asc' or 'desc'")]
        string sortOrder = "asc",
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 500);

        // Validate sortOrder parameter
        var order = sortOrder.ToLowerInvariant() switch
        {
            "asc" => "ASC",
            "desc" => "DESC",
            _ => "ASC" // Default to ascending if invalid value provided
        };

        var (data, next, countResult) = await l3Repository.GetL3TracesAsync(
            l3Source, l3Dest, from, to, l3Type, reportFrom, limit, cursor, includeCount, order, ct);

        // If count was requested but failed, return error
        if (includeCount && countResult.Error != null)
        {
            return StatusCode(500, new { error = $"Failed to retrieve count: {countResult.Error}" });
        }

        return Ok(new PagedResult<TraceDto>(new PageInfo(limit, next, countResult.Value), data));
    }

    public record TraceDto(long Id, DateTime Timestamp, JsonElement Report);
    public record PagedResult<T>(PageInfo Page, IReadOnlyList<T> Data);
    public record PageInfo(int Limit, string? Next, long? TotalCount);
}
