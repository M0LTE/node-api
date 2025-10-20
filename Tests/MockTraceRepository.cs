using node_api.Controllers;
using node_api.Services;

namespace Tests;

public class MockTraceRepository : ITraceRepository
{
    public Task<(IReadOnlyList<TracesController.TraceDto> Data, string? NextCursor)> GetTracesAsync(
        string? source,
        string? dest,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? type,
        string? reportFrom,
        int limit,
        string? cursor,
        CancellationToken ct)
    {
        // Return empty results for testing
        var emptyResult = (Array.Empty<TracesController.TraceDto>() as IReadOnlyList<TracesController.TraceDto>, (string?)null);
        return Task.FromResult(emptyResult);
    }
}
