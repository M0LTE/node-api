using node_api.Controllers;
using node_api.Services;

namespace Tests;

public class MockL2TraceRepository : IL2TraceRepository
{
    public Task InsertTraceAsync(string json, DateTime? timestamp = null, DateTime? reportedTime = null, CancellationToken ct = default)
    {
        // Mock implementation - does nothing for testing
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<TracesController.TraceDto> Data, string? NextCursor, CountResult TotalCount)> GetTracesAsync(
        string? source,
        string? dest,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? type,
        string[]? reportFrom,
        int limit,
        string? cursor,
        bool includeTotalCount,
        string sortOrder,
        CancellationToken ct)
    {
        // Return empty results for testing
        var emptyResult = (
            Array.Empty<TracesController.TraceDto>() as IReadOnlyList<TracesController.TraceDto>, 
            (string?)null,
            CountResult.NotRequested
        );
        return Task.FromResult(emptyResult);
    }

    public Task<(IReadOnlyList<TracesController.TraceDto> Data, string? NextCursor)> GetBidirectionalTracesAsync(
        string endpoint1,
        string endpoint2,
        DateTimeOffset from,
        DateTimeOffset to,
        string[]? reportFrom,
        int limit,
        string? cursor,
        CancellationToken ct)
    {
        var emptyResult = (
            Array.Empty<TracesController.TraceDto>() as IReadOnlyList<TracesController.TraceDto>,
            (string?)null
        );
        return Task.FromResult(emptyResult);
    }

    public Task<IReadOnlyList<node_api.Services.FrameStatistic>> GetFrameStatisticsBetweenEndpointsAsync(
        string endpoint1,
        string endpoint2,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        return Task.FromResult(Array.Empty<node_api.Services.FrameStatistic>() as IReadOnlyList<node_api.Services.FrameStatistic>);
    }
}
