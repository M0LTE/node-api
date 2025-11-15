using node_api.Controllers;

namespace node_api.Services;

public interface IL2TraceRepository
{
    Task<(IReadOnlyList<TracesController.TraceDto> Data, string? NextCursor, CountResult TotalCount)> GetTracesAsync(
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
        CancellationToken ct);

    /// <summary>
    /// Get bidirectional traces between two endpoints for connection analysis
    /// </summary>
    Task<(IReadOnlyList<TracesController.TraceDto> Data, string? NextCursor)> GetBidirectionalTracesAsync(
        string endpoint1,
        string endpoint2,
        DateTimeOffset from,
        DateTimeOffset to,
        string[]? reportFrom,
        int limit,
        string? cursor,
        CancellationToken ct);

    /// <summary>
    /// Get aggregated frame statistics between two endpoints
    /// </summary>
    Task<IReadOnlyList<FrameStatistic>> GetFrameStatisticsBetweenEndpointsAsync(
        string endpoint1,
        string endpoint2,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);
}

public record FrameStatistic(
    string Source,
    string Dest,
    string? FrameType,
    long Count,
    long TotalBytes
);
