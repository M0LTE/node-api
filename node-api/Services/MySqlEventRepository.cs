using Dapper;
using node_api.Controllers;
using System.Text;
using System.Text.Json;

namespace node_api.Services;

public class MySqlEventRepository(ILogger<MySqlEventRepository> logger) : IEventRepository
{
    private const int SlowQueryThresholdMs = 5000;

    public async Task InsertEventAsync(string json, DateTime? timestamp = null, CancellationToken ct = default)
    {
        try
        {
            using var conn = Database.GetConnection(open: false);
            await conn.OpenAsync(ct);

            // Extract reported_time from JSON
            DateTime? reportedTime = null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("time", out var timeElement))
                {
                    if (timeElement.ValueKind == JsonValueKind.Number)
                    {
                        var unixTime = timeElement.GetDouble();
                        reportedTime = DateTimeOffset.FromUnixTimeMilliseconds((long)(unixTime * 1000)).UtcDateTime;
                    }
                }
            }
            catch
            {
                // If we can't parse the time, just continue without it
            }

            if (timestamp.HasValue)
            {
                const string sql = "INSERT INTO events (json, timestamp, reported_time) VALUES (@json, @timestamp, @reportedTime)";
                await QueryLogger.ExecuteWithLoggingAsync(
                    conn,
                    new CommandDefinition(sql, new { json, timestamp = timestamp.Value, reportedTime }, cancellationToken: ct),
                    logger,
                    SlowQueryThresholdMs);
            }
            else
            {
                const string sql = "INSERT INTO events (json, reported_time) VALUES (@json, @reportedTime)";
                await QueryLogger.ExecuteWithLoggingAsync(
                    conn,
                    new CommandDefinition(sql, new { json, reportedTime }, cancellationToken: ct),
                    logger,
                    SlowQueryThresholdMs);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to insert event");
            throw;
        }
    }

    public async Task<(IReadOnlyList<EventsController.EventDto> Data, string? NextCursor, CountResult TotalCount)> GetEventsAsync(
        string? node,
        string? type,
        string? direction,
        string? remote,
        string? local,
        string? port,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        string? cursor,
        bool includeTotalCount,
        string sortOrder,
        CancellationToken ct)
    {
        var where = new List<string> { "1=1" };
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(node))
        {
            where.Add("(`node_idx` = @node OR `nodeCall_idx` = @node)");
            p.Add("node", node);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            where.Add("`type_idx` = @type");
            p.Add("type", type);
        }

        if (!string.IsNullOrWhiteSpace(direction))
        {
            where.Add("`direction_idx` = @direction");
            p.Add("direction", direction);
        }

        if (!string.IsNullOrWhiteSpace(remote))
        {
            where.Add("`remote_idx` = @remote");
            p.Add("remote", remote);
        }

        if (!string.IsNullOrWhiteSpace(local))
        {
            where.Add("`local_idx` = @local");
            p.Add("local", local);
        }

        if (!string.IsNullOrWhiteSpace(port))
        {
            where.Add("`port_idx` = @port");
            p.Add("port", port);
        }

        if (from.HasValue)
        {
            where.Add("`timestamp` >= @from");
            p.Add("from", from.Value.UtcDateTime);
        }

        if (to.HasValue)
        {
            where.Add("`timestamp` <= @to");
            p.Add("to", to.Value.UtcDateTime);
        }

        // Determine sort direction and pagination operator
        var isAscending = sortOrder == "ASC";
        var comparisonOp = isAscending ? ">" : "<";
        var orderByClause = $"ORDER BY `timestamp` {sortOrder}, `id` {sortOrder}";

        // Keyset pagination
        if (!string.IsNullOrEmpty(cursor))
        {
            if (!TryDecodeCursor(cursor, out var tsLast, out var idLast))
                throw new ArgumentException("Invalid cursor.");

            where.Add($"(`timestamp` {comparisonOp} @cts OR (`timestamp` = @cts AND `id` {comparisonOp} @cid))");
            p.Add("cts", tsLast);
            p.Add("cid", idLast);
        }

        var sql = $@"
            SELECT
              `id`,
              `timestamp`,
              `json` as event
            FROM `events`
            WHERE {string.Join(" AND ", where)}
            {orderByClause}
            LIMIT @lim";

        p.Add("lim", limit);

        using var _conn = Database.GetConnection(open: false);

        await _conn.OpenAsync(ct);
        try
        {
            var rows = (await QueryLogger.QueryWithLoggingAsync<EventRow>(
                _conn,
                new CommandDefinition(sql, p, cancellationToken: ct),
                logger,
                SlowQueryThresholdMs)).ToList();

            // Materialize JSON column to JsonElement
            var data = new List<EventsController.EventDto>(rows.Count);
            foreach (var r in rows)
            {
                using var doc = JsonDocument.Parse(r.@event ?? "null");
                data.Add(new EventsController.EventDto(
                    r.id,
                    DateTime.SpecifyKind(r.timestamp, DateTimeKind.Utc),
                    doc.RootElement.Clone()
                ));
            }

            string? next = null;
            if (data.Count == limit)
            {
                var last = rows[^1];
                next = EncodeCursor(DateTime.SpecifyKind(last.timestamp, DateTimeKind.Utc), last.id);
            }

            // Optional total count (expensive operation, only when requested)
            var countResult = includeTotalCount 
                ? await GetTotalCountAsync(where, p, ct)
                : CountResult.NotRequested;

            return (data, next, countResult);
        }
        finally
        {
            await _conn.CloseAsync();
        }
    }

    private async Task<CountResult> GetTotalCountAsync(
        List<string> where, 
        DynamicParameters p,
        CancellationToken ct)
    {
        // Build count query without cursor filter and without LIMIT
        var countWhere = where.Where(w => !w.Contains("timestamp") || !w.Contains("@cts")).ToList();
        
        var countSql = $@"
            SELECT COUNT(*) 
            FROM `events` 
            WHERE {string.Join(" AND ", countWhere)}";

        try
        {
            using var countConn = Database.GetConnection(open: false);
            await countConn.OpenAsync(ct);
            
            var count = await QueryLogger.ExecuteScalarWithLoggingAsync<long>(
                countConn,
                new CommandDefinition(countSql, p, cancellationToken: ct),
                logger,
                SlowQueryThresholdMs);
            
            return CountResult.Success(count);
        }
        catch (Exception ex)
        {
            return CountResult.Failed(ex.Message);
        }
    }

    public async Task<IReadOnlyList<EventsController.EventDto>> GetLinkEventsBetweenEndpointsAsync(
        string endpoint1,
        string endpoint2,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        using var conn = Database.GetConnection(open: false);
        await conn.OpenAsync(ct);

        var sql = @"
            SELECT 
                id,
                timestamp,
                json as event
            FROM events
            WHERE type_idx IN ('LinkUpEvent', 'LinkDownEvent', 'LinkStatus')
              AND ((local_idx = @endpoint1 AND remote_idx = @endpoint2)
                OR (local_idx = @endpoint2 AND remote_idx = @endpoint1))
              AND timestamp >= @from
              AND timestamp <= @to
            ORDER BY timestamp ASC, id ASC";

        var p = new DynamicParameters();
        p.Add("endpoint1", endpoint1);
        p.Add("endpoint2", endpoint2);
        p.Add("from", from.UtcDateTime);
        p.Add("to", to.UtcDateTime);

        var rows = await QueryLogger.QueryWithLoggingAsync<EventRow>(
            conn,
            new CommandDefinition(sql, p, cancellationToken: ct),
            logger,
            SlowQueryThresholdMs);

        var data = new List<EventsController.EventDto>();
        foreach (var r in rows)
        {
            using var doc = JsonDocument.Parse(r.@event ?? "null");
            data.Add(new EventsController.EventDto(
                r.id,
                DateTime.SpecifyKind(r.timestamp, DateTimeKind.Utc),
                doc.RootElement.Clone()
            ));
        }

        return data;
    }

    private sealed class EventRow
    {
        public long id { get; set; }
        public DateTime timestamp { get; set; }
        public string? @event { get; set; }
    }

    private static string EncodeCursor(DateTime timestampUtc, long id)
    {
        var token = $"{timestampUtc:O}|{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
    }

    private static bool TryDecodeCursor(string cursor, out DateTime tsUtc, out long id)
    {
        tsUtc = default;
        id = default;

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split('|', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return false;

            tsUtc = DateTime.Parse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind);
            id = long.Parse(parts[1]);
            if (tsUtc.Kind == DateTimeKind.Unspecified) tsUtc = DateTime.SpecifyKind(tsUtc, DateTimeKind.Utc);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
