using Dapper;

namespace node_api.Services;

public class MySqlL3TraceRepository(ILogger<MySqlL3TraceRepository> logger) : IL3TraceRepository
{
    private const int SlowQueryThresholdMs = 5000;

    public async Task InsertL3TraceAsync(string json, DateTime? timestamp = null, CancellationToken ct = default)
    {
        try
        {
            using var conn = Database.GetConnection(open: false);
            await conn.OpenAsync(ct);

            if (timestamp.HasValue)
            {
                const string sql = "INSERT INTO l3traces (json, timestamp) VALUES (@json, @timestamp)";
                await QueryLogger.ExecuteWithLoggingAsync(
                    conn,
                    new CommandDefinition(sql, new { json, timestamp = timestamp.Value }, cancellationToken: ct),
                    logger,
                    SlowQueryThresholdMs);
            }
            else
            {
                const string sql = "INSERT INTO l3traces (json) VALUES (@json)";
                await QueryLogger.ExecuteWithLoggingAsync(
                    conn,
                    new CommandDefinition(sql, new { json }, cancellationToken: ct),
                    logger,
                    SlowQueryThresholdMs);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to insert L3 trace");
            throw;
        }
    }
}
