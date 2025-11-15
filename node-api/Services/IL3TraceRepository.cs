namespace node_api.Services;

public interface IL3TraceRepository
{
    Task InsertL3TraceAsync(string json, DateTime? timestamp = null, CancellationToken ct = default);
}
