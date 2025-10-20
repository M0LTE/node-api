using Microsoft.AspNetCore.Mvc;
using node_api.Services;
using System.Text.Json;

namespace node_api.Controllers;

[ApiController]
[Route("api/traces")]
public class TracesController(ITraceRepository repository) : ControllerBase
{
    // GET /api/traces?source=...&dest=...&from=...&to=...&limit=...&cursor=...
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
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 500);

        var (data, next) = await repository.GetTracesAsync(
            source, dest, from, to, type, reportFrom, limit, cursor, ct);

        return Ok(new PagedResult<TraceDto>(data, new PageInfo(limit, next)));
    }

    public record TraceDto(long Id, DateTime Timestamp, JsonElement Report);
    public record PagedResult<T>(IReadOnlyList<T> Data, PageInfo Page);
    public record PageInfo(int Limit, string? Next);
}
