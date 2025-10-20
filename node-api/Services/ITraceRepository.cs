using node_api.Controllers;

namespace node_api.Services;

public interface ITraceRepository
{
    Task<(IReadOnlyList<TracesController.TraceDto> Data, string? NextCursor, CountResult TotalCount)> GetTracesAsync(
        string? source,
        string? dest,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? type,
        string? reportFrom,
        int limit,
        string? cursor,
        bool includeTotalCount,
        CancellationToken ct);
}
