using Dapper;
using node_api.Controllers;
using System.Text;
using System.Text.Json;

namespace node_api.Services;

public class MySqlEventRepository : IEventRepository
{
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
              `json` as event
            FROM `events`
            WHERE {string.Join(" AND ", where)}
            ORDER BY `timestamp` DESC, `id` DESC
            LIMIT @lim";

        p.Add("lim", limit);

        using var _conn = Database.GetConnection(open: false);

        await _conn.OpenAsync(ct);
        try
        {
            var rows = (await _conn.QueryAsync<EventRow>(new CommandDefinition(sql, p, cancellationToken: ct))).ToList();

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

    private static async Task<CountResult> GetTotalCountAsync(List<string> where, DynamicParameters p, CancellationToken ct)
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
            
            var count = await countConn.ExecuteScalarAsync<long>(
                new CommandDefinition(countSql, p, cancellationToken: ct));
            
            return CountResult.Success(count);
        }
        catch (Exception ex)
        {
            return CountResult.Failed(ex.Message);
        }
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
