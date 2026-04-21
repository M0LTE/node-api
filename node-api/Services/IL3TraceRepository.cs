namespace node_api.Services;

public interface IL3TraceRepository
{
    Task InsertL3TraceAsync(string json, DateTime? timestamp = null, DateTime? reportedTime = null, CancellationToken ct = default);

    /// <summary>
    /// Get L3 (NET/ROM layer 3) traces with filtering and pagination
    /// </summary>
    Task<(IReadOnlyList<Controllers.TracesController.TraceDto> Data, string? NextCursor, CountResult TotalCount)> GetL3TracesAsync(
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
        CancellationToken ct);
}
