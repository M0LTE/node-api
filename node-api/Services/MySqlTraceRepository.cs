using Dapper;
using node_api.Controllers;
using System.Text;
using System.Text.Json;

namespace node_api.Services;

public partial class MySqlL2TraceRepository(ILogger<MySqlL2TraceRepository> logger) : IL2TraceRepository
{
    private const int SlowQueryThresholdMs = 5000;

    public async Task InsertTraceAsync(string json, DateTime? timestamp = null, CancellationToken ct = default)
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
                const string sql = "INSERT INTO traces (json, timestamp, reported_time) VALUES (@json, @timestamp, @reportedTime)";
                await QueryLogger.ExecuteWithLoggingAsync(
                    conn,
                    new CommandDefinition(sql, new { json, timestamp = timestamp.Value, reportedTime }, cancellationToken: ct),
                    logger,
                    SlowQueryThresholdMs);
            }
            else
            {
                const string sql = "INSERT INTO traces (json, reported_time) VALUES (@json, @reportedTime)";
                await QueryLogger.ExecuteWithLoggingAsync(
                    conn,
                    new CommandDefinition(sql, new { json, reportedTime }, cancellationToken: ct),
                    logger,
                    SlowQueryThresholdMs);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to insert trace");
            throw;
        }
    }

    public async Task<(IReadOnlyList<TracesController.TraceDto> Data, string? NextCursor, CountResult TotalCount)> GetTracesAsync(
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
        var where = new List<string> { "1=1" };
        var p = new DynamicParameters();

        // Exclude TEST callsigns from reportFrom unless explicitly requested
        if (reportFrom != null && reportFrom.Length > 0)
        {
            // Filter out null/empty values
            var validCallsigns = reportFrom.Where(c => !string.IsNullOrWhiteSpace(c)).ToArray();
            
            if (validCallsigns.Length > 0)
            {
                // Build IN clause for multiple callsigns
                var paramNames = new List<string>();
                for (int i = 0; i < validCallsigns.Length; i++)
                {
                    var paramName = $"reportFrom{i}";
                    paramNames.Add($"@{paramName}");
                    p.Add(paramName, validCallsigns[i]);
                }
                where.Add($"`reportFrom_idx` IN ({string.Join(", ", paramNames)})");
            }
        }
        else
        {
            // Exclude TEST and TEST-0 through TEST-15
            where.Add("`reportFrom_idx` NOT REGEXP @testPattern");
            p.Add("testPattern", "^TEST(-([0-9]|1[0-5]))?$");
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            where.Add("`srce_idx` = @source");
            p.Add("source", source);
        }
        if (!string.IsNullOrWhiteSpace(dest))
        {
            where.Add("`dest_idx` = @dest");
            p.Add("dest", dest);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            where.Add("`type_idx` = @type");
            p.Add("type", type);
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
              `json` as report
            FROM `traces`
            WHERE {string.Join(" AND ", where)}
            {orderByClause}
            LIMIT @lim";

        p.Add("lim", limit);

        using var _conn = Database.GetConnection(open: false);

        await _conn.OpenAsync(ct);
        try
        {
            var rows = (await QueryLogger.QueryWithLoggingAsync<TraceRow>(
                _conn, 
                new CommandDefinition(sql, p, cancellationToken: ct),
                logger,
                SlowQueryThresholdMs)).ToList();

            // Materialize JSON column to JsonElement
            var data = new List<TracesController.TraceDto>(rows.Count);
            foreach (var r in rows)
            {
                using var doc = JsonDocument.Parse(r.report ?? "null");
                data.Add(new TracesController.TraceDto(
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
            FROM `traces` 
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

    public async Task<(IReadOnlyList<TracesController.TraceDto> Data, string? NextCursor)> GetBidirectionalTracesAsync(
        string endpoint1,
        string endpoint2,
        DateTimeOffset from,
        DateTimeOffset to,
        string[]? reportFrom,
        int limit,
        string? cursor,
        CancellationToken ct)
    {
        using var conn = Database.GetConnection(open: false);
        await conn.OpenAsync(ct);

        var where = new List<string>
        {
            "((srce_idx = @endpoint1 AND dest_idx = @endpoint2) OR (srce_idx = @endpoint2 AND dest_idx = @endpoint1))",
            "timestamp >= @from",
            "timestamp <= @to"
        };

        var p = new DynamicParameters();
        p.Add("endpoint1", endpoint1);
        p.Add("endpoint2", endpoint2);
        p.Add("from", from.UtcDateTime);
        p.Add("to", to.UtcDateTime);

        // Handle reportFrom filter
        if (reportFrom != null && reportFrom.Length > 0)
        {
            var validCallsigns = reportFrom.Where(c => !string.IsNullOrWhiteSpace(c)).ToArray();
            if (validCallsigns.Length > 0)
            {
                var paramNames = new List<string>();
                for (int i = 0; i < validCallsigns.Length; i++)
                {
                    var paramName = $"reportFrom{i}";
                    paramNames.Add($"@{paramName}");
                    p.Add(paramName, validCallsigns[i]);
                }
                where.Add($"reportFrom_idx IN ({string.Join(", ", paramNames)})");
            }
        }
        else
        {
            where.Add("reportFrom_idx NOT REGEXP @testPattern");
            p.Add("testPattern", "^TEST(-([0-9]|1[0-5]))?$");
        }

        // Keyset pagination
        if (!string.IsNullOrEmpty(cursor))
        {
            if (TryDecodeCursor(cursor, out var tsLast, out var idLast))
            {
                where.Add("(timestamp > @cts OR (timestamp = @cts AND id > @cid))");
                p.Add("cts", tsLast);
                p.Add("cid", idLast);
            }
        }

        var sql = $@"
            SELECT 
                id,
                timestamp,
                json as report
            FROM traces
            WHERE {string.Join(" AND ", where)}
            ORDER BY timestamp ASC, id ASC
            LIMIT @limit";

        p.Add("limit", limit);

        var rows = (await QueryLogger.QueryWithLoggingAsync<TraceRow>(
            conn,
            new CommandDefinition(sql, p, cancellationToken: ct),
            logger,
            SlowQueryThresholdMs)).ToList();

        var data = new List<TracesController.TraceDto>();
        foreach (var r in rows)
        {
            using var doc = JsonDocument.Parse(r.report ?? "null");
            data.Add(new TracesController.TraceDto(
                r.id,
                DateTime.SpecifyKind(r.timestamp, DateTimeKind.Utc),
                doc.RootElement.Clone()
            ));
        }

        string? next = null;
        if (data.Count == limit && data.Count > 0)
        {
            var last = rows[^1];
            next = EncodeCursor(DateTime.SpecifyKind(last.timestamp, DateTimeKind.Utc), last.id);
        }

        return (data, next);
    }

    public async Task<IReadOnlyList<FrameStatistic>> GetFrameStatisticsBetweenEndpointsAsync(
        string endpoint1,
        string endpoint2,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        using var conn = Database.GetConnection(open: false);
        await conn.OpenAsync(ct);

        // Get all traces between the endpoints and let C# calculate statistics
        var sql = @"
            SELECT 
                json
            FROM traces
            WHERE ((srce_idx = @endpoint1 AND dest_idx = @endpoint2)
                OR (srce_idx = @endpoint2 AND dest_idx = @endpoint1))
              AND timestamp >= @from
              AND timestamp <= @to
              AND reportFrom_idx NOT REGEXP @testPattern";

        var p = new DynamicParameters();
        p.Add("endpoint1", endpoint1);
        p.Add("endpoint2", endpoint2);
        p.Add("from", from.UtcDateTime);
        p.Add("to", to.UtcDateTime);
        p.Add("testPattern", "^TEST(-([0-9]|1[0-5]))?$");

        var rows = await QueryLogger.QueryWithLoggingAsync<JsonRow>(
            conn,
            new CommandDefinition(sql, p, cancellationToken: ct),
            logger,
            SlowQueryThresholdMs);

        // Calculate statistics in C# from JSON
        var stats = new Dictionary<(string source, string dest, string? frameType), FrameStatBuilder>();

        foreach (var row in rows)
        {
            using var doc = JsonDocument.Parse(row.json ?? "{}");
            var root = doc.RootElement;

            var source = root.TryGetProperty("srce", out var s) ? s.GetString() : null;
            var dest = root.TryGetProperty("dest", out var d) ? d.GetString() : null;
            var frameType = root.TryGetProperty("l2Type", out var ft) ? ft.GetString() : null;
            var ilen = root.TryGetProperty("ilen", out var il) ? il.GetInt32() : 0;

            if (source == null || dest == null) continue;

            var key = (source, dest, frameType);
            if (!stats.ContainsKey(key))
            {
                stats[key] = new FrameStatBuilder();
            }

            stats[key].Count++;
            stats[key].TotalBytes += ilen;
        }

        return stats.Select(kvp => new FrameStatistic(
            kvp.Key.source,
            kvp.Key.dest,
            kvp.Key.frameType,
            kvp.Value.Count,
            kvp.Value.TotalBytes
        )).ToList();
    }

    private sealed class JsonRow
    {
        public string? json { get; set; }
    }

    private class FrameStatBuilder
    {
        public long Count { get; set; }
        public long TotalBytes { get; set; }
    }

    private sealed class TraceRow
    {
        public long id { get; set; }
        public DateTime timestamp { get; set; }
        public string? report { get; set; }
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
