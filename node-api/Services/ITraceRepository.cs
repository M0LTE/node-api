using node_api.Controllers;

namespace node_api.Services;

public interface ITraceRepository
{
    Task<(IReadOnlyList<TracesController.TraceDto> Data, string? NextCursor)> GetTracesAsync(
        string? source,
        string? dest,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? type,
        string? reportFrom,
        int limit,
        string? cursor,
        CancellationToken ct);
}
