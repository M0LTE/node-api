using Dapper;

namespace node_api.Services;

/// <summary>
/// Repository for persisting errored/malformed messages to MySQL.
/// </summary>
public class MySqlErroredMessageRepository
{
    private readonly ILogger<MySqlErroredMessageRepository> _logger;
    private readonly QueryFrequencyTracker _tracker;
    private const int SlowQueryThresholdMs = 1000;

    public MySqlErroredMessageRepository(
        ILogger<MySqlErroredMessageRepository> logger,
        QueryFrequencyTracker tracker)
    {
        _logger = logger;
        _tracker = tracker;
    }

    public async Task InsertErroredMessageAsync(
        string reason,
        string? datagram = null,
        string? type = null,
        string? errors = null,
        string? json = null,
        CancellationToken ct = default)
    {
        try
        {
            using var conn = Database.GetConnection(open: false);
            await conn.OpenAsync(ct);

            if (!string.IsNullOrWhiteSpace(datagram))
            {
                // Validation error with structured data
                const string sql = "INSERT INTO errored_messages (reason, datagram, type, errors) VALUES (@reason, @datagram, @type, @errors)";
                await QueryLogger.ExecuteWithLoggingAsync(
                    conn,
                    new CommandDefinition(sql, new { reason, datagram, type, errors }, cancellationToken: ct),
                    _logger,
                    SlowQueryThresholdMs,
                    _tracker);
            }
            else
            {
                // Generic errored message
                const string sql = "INSERT INTO errored_messages (reason, json) VALUES (@reason, @json)";
                await QueryLogger.ExecuteWithLoggingAsync(
                    conn,
                    new CommandDefinition(sql, new { reason, json }, cancellationToken: ct),
                    _logger,
                    SlowQueryThresholdMs,
                    _tracker);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert errored message");
            throw;
        }
    }
}
