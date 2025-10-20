using node_api.Controllers;
using node_api.Services;

namespace Tests;

public class MockEventRepository : IEventRepository
{
    public Task<(IReadOnlyList<EventsController.EventDto> Data, string? NextCursor, CountResult TotalCount)> GetEventsAsync(
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
        CancellationToken ct)
    {
        // Return empty results for testing
        var emptyResult = (
            Array.Empty<EventsController.EventDto>() as IReadOnlyList<EventsController.EventDto>, 
            (string?)null,
            CountResult.NotRequested
        );
        return Task.FromResult(emptyResult);
    }
}
