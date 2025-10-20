using Microsoft.AspNetCore.Mvc;
using node_api.Services;
using System.Text.Json;

namespace node_api.Controllers;

[ApiController]
[Route("api/traces")]
public class TracesController(ITraceRepository repository) : ControllerBase
{
    // GET /api/traces?source=...&dest=...&from=...&to=...&type=...&reportFrom=...&limit=...&cursor=...&includeCount=...
    [HttpGet]
    public async Task<ActionResult<PagedResult<TraceDto>>> GetAsync(
        [FromQuery] string? source,
        [FromQuery] string? dest,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? type,
        [FromQuery] string? reportFrom,
        [FromQuery] int limit = 100,
        [FromQuery] string? cursor = null,
        [FromQuery] bool includeCount = false,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 500);

        var (data, next, countResult) = await repository.GetTracesAsync(
            source, dest, from, to, type, reportFrom, limit, cursor, includeCount, ct);

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
