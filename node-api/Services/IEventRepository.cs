using node_api.Controllers;

namespace node_api.Services;

public interface IEventRepository
{
    Task<(IReadOnlyList<EventsController.EventDto> Data, string? NextCursor, CountResult TotalCount)> GetEventsAsync(
        string? node,
        string? type,
        string? direction,
        string? remote,
        string? local,
        string? port,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        string? cursor,
        bool includeTotalCount,
        string sortOrder,
        CancellationToken ct);

    /// <summary>
    /// Get link events between two endpoints for connection analysis
    /// </summary>
    Task<IReadOnlyList<EventsController.EventDto>> GetLinkEventsBetweenEndpointsAsync(
        string endpoint1,
        string endpoint2,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);
}
