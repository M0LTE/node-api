using Dapper;
using Microsoft.AspNetCore.Mvc;
using node_api.Services;
using System.Text;

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

        var sql = $@"
            SELECT
              `id`,
              `timestamp`,
              JSON_VALUE(`json`, '$.srce') AS source,
              JSON_VALUE(`json`, '$.dest') AS dest
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

            var data = rows.Select(r => new TraceDto(
                r.id,
                DateTime.SpecifyKind(r.timestamp, DateTimeKind.Utc),
                r.source,
                r.dest
            )).ToList();

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

    // Simple DTOs
    public record TraceDto(long Id, DateTime Timestamp, string? Source, string? Dest);
    public record PagedResult<T>(IReadOnlyList<T> Data, PageInfo Page);
    public record PageInfo(int Limit, string? Next);

    // Private row shape for Dapper mapping
    private sealed class TraceRow
    {
        public long id { get; set; }
        public DateTime timestamp { get; set; } // assumes UTC or naive; we treat as UTC
        public string? source { get; set; }
        public string? dest { get; set; }
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
