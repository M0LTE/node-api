using Microsoft.AspNetCore.Mvc;
using node_api.Services;
using System.Text.Json;

namespace node_api.Controllers;

[ApiController]
[Route("api/history/events")]
public class EventsController(IEventRepository repository) : ControllerBase
{
    /// <summary>
    /// Get events from the network history
    /// </summary>
    /// <param name="node">Filter by node callsign</param>
    /// <param name="type">Filter by event type (e.g., "LinkUpEvent", "NodeUpEvent")</param>
    /// <param name="direction">Filter by connection direction: "incoming" or "outgoing"</param>
    /// <param name="remote">Filter by remote endpoint callsign</param>
    /// <param name="local">Filter by local endpoint callsign</param>
    /// <param name="port">Filter by port identifier</param>
    /// <param name="from">Filter events from this timestamp (inclusive)</param>
    /// <param name="to">Filter events to this timestamp (inclusive)</param>
    /// <param name="limit">Maximum number of results to return (1-500, default: 100)</param>
    /// <param name="cursor">Pagination cursor for next page</param>
    /// <param name="includeCount">Include total count of matching records (expensive operation)</param>
    /// <param name="sortOrder">Sort order by timestamp: "asc" (oldest first, default) or "desc" (newest first)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of events</returns>
    // GET /api/events?node=...&type=...&direction=...&remote=...&local=...&port=...&from=...&to=...&limit=...&cursor=...&includeCount=...&sortOrder=...
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

        var (data, next, countResult) = await repository.GetEventsAsync(
            node, type, direction, remote, local, port, from, to, limit, cursor, includeCount, order, ct);

        // If count was requested but failed, return error
        if (includeCount && countResult.Error != null)
        {
            return StatusCode(500, new { error = $"Failed to retrieve count: {countResult.Error}" });
        }

        return Ok(new PagedResult<EventDto>(new PageInfo(limit, next, countResult.Value), data));
    }

    public record EventDto(long Id, DateTime Timestamp, JsonElement Event);
    public record PagedResult<T>(PageInfo Page, IReadOnlyList<T> Data);
    public record PageInfo(int Limit, string? Next, long? TotalCount);
}