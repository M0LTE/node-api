using Dapper;
using node_api.Controllers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace node_api.Services;

public partial class MySqlTraceRepository(ILogger<MySqlTraceRepository> logger, QueryFrequencyTracker tracker) : ITraceRepository
{
    private const int SlowQueryThresholdMs = 5000;

    [GeneratedRegex(@"^TEST(-([0-9]|1[0-5]))?$", RegexOptions.IgnoreCase)]
    private static partial Regex TestCallsignRegex();

    private static bool IsTestCallsign(string? callsign)
    {
        if (string.IsNullOrWhiteSpace(callsign))
            return false;

        return TestCallsignRegex().IsMatch(callsign);
    }

    public async Task InsertTraceAsync(string json, DateTime? timestamp = null, CancellationToken ct = default)
    {
        try
        {
            using var conn = Database.GetConnection(open: false);
            await conn.OpenAsync(ct);

            if (timestamp.HasValue)
            {
                const string sql = "INSERT INTO traces (json, timestamp) VALUES (@json, @timestamp)";
                await QueryLogger.ExecuteWithLoggingAsync(
                    conn,
                    new CommandDefinition(sql, new { json, timestamp = timestamp.Value }, cancellationToken: ct),
                    logger,
                    SlowQueryThresholdMs,
                    tracker);
            }
            else
            {
                const string sql = "INSERT INTO traces (json) VALUES (@json)";
                await QueryLogger.ExecuteWithLoggingAsync(
                    conn,
                    new CommandDefinition(sql, new { json }, cancellationToken: ct),
                    logger,
                    SlowQueryThresholdMs,
                    tracker);
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
        string? reportFrom,
        int limit,
        string? cursor,
        bool includeTotalCount,
        CancellationToken ct)
    {
        var where = new List<string> { "1=1" };
        var p = new DynamicParameters();

        // Exclude TEST callsigns from reportFrom unless explicitly requested
        if (!string.IsNullOrWhiteSpace(reportFrom))
        {
            where.Add("`reportFrom_idx` = @reportFrom");
            p.Add("reportFrom", reportFrom);
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

        // Keyset pagination on (timestamp DESC, id DESC)
        if (!string.IsNullOrEmpty(cursor))
        {
            if (!TryDecodeCursor(cursor, out var tsLast, out var idLast))
                throw new ArgumentException("Invalid cursor.");

            where.Add("(`timestamp` < @cts OR (`timestamp` = @cts AND `id` < @cid))");
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
            ORDER BY `timestamp` DESC, `id` DESC
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
                SlowQueryThresholdMs,
                tracker)).ToList();

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
                SlowQueryThresholdMs,
                tracker);
            
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
