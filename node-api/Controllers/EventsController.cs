using Microsoft.AspNetCore.Mvc;
using node_api.Services;
using System.Text.Json;

namespace node_api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(IEventRepository repository) : ControllerBase
{
    // GET /api/events?node=...&type=...&direction=...&remote=...&local=...&port=...&from=...&to=...&limit=...&cursor=...
    [HttpGet]
    public async Task<ActionResult<PagedResult<EventDto>>> GetAsync(
        [FromQuery] string? node,
        [FromQuery] string? type,
        [FromQuery] string? direction,
        [FromQuery] string? remote,
        [FromQuery] string? local,
        [FromQuery] string? port,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int limit = 100,
        [FromQuery] string? cursor = null,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 500);

        var (data, next) = await repository.GetEventsAsync(
            node, type, direction, remote, local, port, from, to, limit, cursor, ct);

        return Ok(new PagedResult<EventDto>(data, new PageInfo(limit, next)));
    }

    public record EventDto(long Id, DateTime Timestamp, JsonElement Event);
    public record PagedResult<T>(IReadOnlyList<T> Data, PageInfo Page);
    public record PageInfo(int Limit, string? Next);
}