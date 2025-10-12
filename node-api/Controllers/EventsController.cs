using Dapper;
using Microsoft.AspNetCore.Mvc;
using node_api.Services;
using System.Text;
using System.Text.Json;

namespace node_api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    // GET /api/events?node=...&type=...&direction=...&remote=...&local=...&port=...&from=...&to=...&limit=...&cursor=...
    [HttpGet]
    public async Task<ActionResult<PagedResult<EventDto>>> GetAsync(
        [FromQuery] string? node,
        [FromQuery] string? type,
        [FromQuery] string? direction,
        [FromQuery] string? remote,
        [FromQuery] string? local,
        [FromQuery] string? port,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int limit = 100,
        [FromQuery] string? cursor = null,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 500);

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
                return BadRequest("Invalid cursor.");

            where.Add("(`timestamp` < @cts OR (`timestamp` = @cts AND `id` < @cid))");
            p.Add("cts", tsLast);
            p.Add("cid", idLast);
        }

        /*
Expected indexes to be added manually:

ALTER TABLE `events`
  ADD COLUMN `node_idx` VARCHAR(32)
    GENERATED ALWAYS AS (JSON_VALUE(`json`, '$.node')) PERSISTENT INVISIBLE,
  ADD COLUMN `nodeCall_idx` VARCHAR(32)
    GENERATED ALWAYS AS (JSON_VALUE(`json`, '$.nodeCall')) PERSISTENT INVISIBLE,
  ADD COLUMN `type_idx` VARCHAR(64)
    GENERATED ALWAYS AS (JSON_VALUE(`json`, '$."@type"')) PERSISTENT INVISIBLE,
  ADD COLUMN `direction_idx` VARCHAR(16)
    GENERATED ALWAYS AS (JSON_VALUE(`json`, '$.direction')) PERSISTENT INVISIBLE,
  ADD COLUMN `remote_idx` VARCHAR(64)
    GENERATED ALWAYS AS (JSON_VALUE(`json`, '$.remote')) PERSISTENT INVISIBLE,
  ADD COLUMN `local_idx` VARCHAR(64)
    GENERATED ALWAYS AS (JSON_VALUE(`json`, '$.local')) PERSISTENT INVISIBLE,
  ADD COLUMN `port_idx` VARCHAR(16)
    GENERATED ALWAYS AS (JSON_VALUE(`json`, '$.port')) PERSISTENT INVISIBLE;

CREATE INDEX ix_events_node_ts ON `events` (`node_idx`, `timestamp` DESC, `id` DESC);
CREATE INDEX ix_events_nodeCall_ts ON `events` (`nodeCall_idx`, `timestamp` DESC, `id` DESC);
CREATE INDEX ix_events_type_ts ON `events` (`type_idx`, `timestamp` DESC, `id` DESC);
CREATE INDEX ix_events_direction_ts ON `events` (`direction_idx`, `timestamp` DESC, `id` DESC);
CREATE INDEX ix_events_remote_ts ON `events` (`remote_idx`, `timestamp` DESC, `id` DESC);
CREATE INDEX ix_events_local_ts ON `events` (`local_idx`, `timestamp` DESC, `id` DESC);
CREATE INDEX ix_events_port_ts ON `events` (`port_idx`, `timestamp` DESC, `id` DESC);
CREATE INDEX ix_events_ts_id ON `events` (`timestamp` DESC, `id` DESC);
         */

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
            var data = new List<EventDto>(rows.Count);
            foreach (var r in rows)
            {
                using var doc = JsonDocument.Parse(r.@event ?? "null");
                data.Add(new EventDto(
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

            return Ok(new PagedResult<EventDto>(data, new PageInfo(limit, next)));
        }
        finally
        {
            await _conn.CloseAsync();
        }
    }

    public record EventDto(long Id, DateTime Timestamp, JsonElement Event);
    public record PagedResult<T>(IReadOnlyList<T> Data, PageInfo Page);
    public record PageInfo(int Limit, string? Next);

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