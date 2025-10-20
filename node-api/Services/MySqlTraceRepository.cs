using Dapper;
using node_api.Controllers;
using System.Text;
using System.Text.Json;

namespace node_api.Services;

public class MySqlTraceRepository : ITraceRepository
{
    public async Task<(IReadOnlyList<TracesController.TraceDto> Data, string? NextCursor)> GetTracesAsync(
        string? source,
        string? dest,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? type,
        string? reportFrom,
        int limit,
        string? cursor,
        CancellationToken ct)
    {
        var where = new List<string> { "1=1" };
        var p = new DynamicParameters();

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

        if (!string.IsNullOrWhiteSpace(reportFrom))
        {
            where.Add("`reportFrom_idx` = @reportFrom");
            p.Add("reportFrom", reportFrom);
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
            var rows = (await _conn.QueryAsync<TraceRow>(new CommandDefinition(sql, p, cancellationToken: ct))).ToList();

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

            return (data, next);
        }
        finally
        {
            await _conn.CloseAsync();
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
