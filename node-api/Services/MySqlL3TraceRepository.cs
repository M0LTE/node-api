using Dapper;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace node_api.Services;

public partial class MySqlL3TraceRepository(ILogger<MySqlL3TraceRepository> logger) : IL3TraceRepository
{
    private const int SlowQueryThresholdMs = 5000;

    [GeneratedRegex(@"^TEST(-([0-9]|1[0-5]))?$", RegexOptions.IgnoreCase)]
    private static partial Regex TestCallsignRegex();

    public async Task InsertL3TraceAsync(string json, DateTime? timestamp = null, CancellationToken ct = default)
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
                const string sql = "INSERT INTO l3traces (json, timestamp, reported_time) VALUES (@json, @timestamp, @reportedTime)";
                await QueryLogger.ExecuteWithLoggingAsync(
                    conn,
                    new CommandDefinition(sql, new { json, timestamp = timestamp.Value, reportedTime }, cancellationToken: ct),
                    logger,
                    SlowQueryThresholdMs);
            }
            else
            {
                const string sql = "INSERT INTO l3traces (json, reported_time) VALUES (@json, @reportedTime)";
                await QueryLogger.ExecuteWithLoggingAsync(
                    conn,
                    new CommandDefinition(sql, new { json, reportedTime }, cancellationToken: ct),
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

    public async Task<(IReadOnlyList<Controllers.TracesController.TraceDto> Data, string? NextCursor, CountResult TotalCount)> GetL3TracesAsync(
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

        if (!string.IsNullOrWhiteSpace(l3Source))
        {
            where.Add("`l3src_idx` = @l3Source");
            p.Add("l3Source", l3Source);
        }
        if (!string.IsNullOrWhiteSpace(l3Dest))
        {
            where.Add("`l3dst_idx` = @l3Dest");
            p.Add("l3Dest", l3Dest);
        }

        if (!string.IsNullOrWhiteSpace(l3Type))
        {
            where.Add("`l3type_idx` = @l3Type");
            p.Add("l3Type", l3Type);
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
            FROM `l3traces`
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
            var data = new List<Controllers.TracesController.TraceDto>(rows.Count);
            foreach (var r in rows)
            {
                using var doc = JsonDocument.Parse(r.report ?? "null");
                data.Add(new Controllers.TracesController.TraceDto(
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
                ? await GetL3TotalCountAsync(where, p, ct)
                : CountResult.NotRequested;

            return (data, next, countResult);
        }
        finally
        {
            await _conn.CloseAsync();
        }
    }

    private async Task<CountResult> GetL3TotalCountAsync(
        List<string> where, 
        DynamicParameters p, 
        CancellationToken ct)
    {
        // Build count query without cursor filter and without LIMIT
        var countWhere = where.Where(w => !w.Contains("timestamp") || !w.Contains("@cts")).ToList();
        
        var countSql = $@"
            SELECT COUNT(*) 
            FROM `l3traces` 
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
