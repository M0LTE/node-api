using node_api.Services;

namespace Tests;

public class MockL3TraceRepository : IL3TraceRepository
{
    public Task InsertL3TraceAsync(string json, DateTime? timestamp = null, DateTime? reportedTime = null, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<node_api.Controllers.TracesController.TraceDto> Data, string? NextCursor, CountResult TotalCount)> GetL3TracesAsync(
        string? l3Source,
        string? l3Dest,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? l3Type,
        string[]? reportFrom,
        int limit,
        string? cursor,
        bool includeTotalCount,
        string sortOrder,
        CancellationToken ct)
    {
        // Return empty results for testing
        var emptyResult = (
            Array.Empty<node_api.Controllers.TracesController.TraceDto>() as IReadOnlyList<node_api.Controllers.TracesController.TraceDto>, 
            (string?)null,
            CountResult.NotRequested
        );
        return Task.FromResult(emptyResult);
    }
}
