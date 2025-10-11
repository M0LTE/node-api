using Dapper;
using Microsoft.AspNetCore.Mvc;
using node_api.Services;
using System.Text;
using System.Text.Json;

namespace node_api.Controllers;

[ApiController]
[Route("api/traces")]
public class TracesController : ControllerBase
{
    // GET /api/traces?source=...&dest=...&from=...&to=...&limit=...&cursor=...
    [HttpGet]
    public async Task<ActionResult<PagedResult<TraceDto>>> GetAsync(
        [FromQuery] string? source,
        [FromQuery] string? dest,
        [FromQuery] DateTimeOffset? from,  // accepts ISO8601; stored column is DATETIME
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? l3type,
        [FromQuery] int limit = 100,
        [FromQuery] string? cursor = null,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 500);

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

        if (!string.IsNullOrWhiteSpace(l3type))
        {
            where.Add("`l3type_idx` = @l3type");
            p.Add("l3type", l3type);
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
                return BadRequest("Invalid cursor.");

            where.Add("(`timestamp` < @cts OR (`timestamp` = @cts AND `id` < @cid))");
            p.Add("cts", tsLast);
            p.Add("cid", idLast);
        }

        /*
ALTER TABLE `traces`
  ADD COLUMN `srce_idx` VARCHAR(32)
    GENERATED ALWAYS AS (JSON_VALUE(`json`, '$.srce')) PERSISTENT INVISIBLE,
  ADD COLUMN `dest_idx` VARCHAR(32)
    GENERATED ALWAYS AS (JSON_VALUE(`json`, '$.dest')) PERSISTENT INVISIBLE;

CREATE INDEX ix_traces_srce_dest_ts ON `traces` (`srce_idx`, `dest_idx`, `timestamp`);
CREATE INDEX ix_traces_ts_id       ON `traces` (`timestamp` DESC, `id` DESC);
         */

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

        // Important: honour CancellationToken
        await _conn.OpenAsync(ct);
        try
        {
            var rows = (await _conn.QueryAsync<TraceRow>(new CommandDefinition(sql, p, cancellationToken: ct))).ToList();

            // Materialize JSON column to JsonElement (so it returns as real JSON, not a string)
            var data = new List<TraceDto>(rows.Count);
            foreach (var r in rows)
            {
                using var doc = JsonDocument.Parse(r.report ?? "null");
                data.Add(new TraceDto(
                    r.id,
                    DateTime.SpecifyKind(r.timestamp, DateTimeKind.Utc),
                    doc.RootElement.Clone()  // clone because JsonDocument is disposed
                ));
            }

            string? next = null;
            if (data.Count == limit)
            {
                var last = rows[^1];
                next = EncodeCursor(DateTime.SpecifyKind(last.timestamp, DateTimeKind.Utc), last.id);
            }

            return Ok(new PagedResult<TraceDto>(data, new PageInfo(limit, next)));
        }
        finally
        {
            await _conn.CloseAsync();
        }
    }

    public record TraceDto(long Id, DateTime Timestamp, JsonElement Report);
    public record PagedResult<T>(IReadOnlyList<T> Data, PageInfo Page);
    public record PageInfo(int Limit, string? Next);

    private sealed class TraceRow
    {
        public long id { get; set; }
        public DateTime timestamp { get; set; }
        public string? report { get; set; } // MariaDB JSON maps to text; we'll parse it
    }

    // Cursor helpers: base64("timestamp|id") where timestamp is ISO8601 (round-trip)
    private static string EncodeCursor(DateTime timestampUtc, long id)
    {
        var token = $"{timestampUtc:O}|{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
        // Example: eyIyMDI1LTEwLTEwVDA5OjAwOjAwLjAwMDAwMDBaMXwxMjM0NTYi
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
