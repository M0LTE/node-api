using Dapper;
using node_api.Models.NetworkState;
using System.Data;

namespace node_api.Services;

/// <summary>
/// MySQL-backed repository for persisting network state.
/// All operations are idempotent to support multi-instance deployments.
/// </summary>
public class MySqlNetworkStateRepository(ILogger<MySqlNetworkStateRepository> logger)
{
    private const int SlowQueryThresholdMs = 1000;

    #region Node Operations

    public async Task UpsertNodeAsync(NodeState node, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO `nodes` (
                `callsign`, `alias`, `locator`, `latitude`, `longitude`,
                `software`, `version`, `uptime_secs`, `links_in`, `links_out`,
                `circuits_in`, `circuits_out`, `l3_relayed`, `status`,
                `last_seen`, `first_seen`, `last_status_update`,
                `last_up_event`, `last_down_event`, `l2_trace_count`, `last_l2_trace`,
                `ip_address_obfuscated`, `geoip_country_code`, `geoip_country_name`, `geoip_city`, `last_ip_update`
            ) VALUES (
                @Callsign, @Alias, @Locator, @Latitude, @Longitude,
                @Software, @Version, @UptimeSecs, @LinksIn, @LinksOut,
                @CircuitsIn, @CircuitsOut, @L3Relayed, @Status,
                @LastSeen, @FirstSeen, @LastStatusUpdate,
                @LastUpEvent, @LastDownEvent, @L2TraceCount, @LastL2Trace,
                @IpAddressObfuscated, @GeoIpCountryCode, @GeoIpCountryName, @GeoIpCity, @LastIpUpdate
            )
            ON DUPLICATE KEY UPDATE
                `alias` = VALUES(`alias`),
                `locator` = VALUES(`locator`),
                `latitude` = VALUES(`latitude`),
                `longitude` = VALUES(`longitude`),
                `software` = VALUES(`software`),
                `version` = VALUES(`version`),
                `uptime_secs` = VALUES(`uptime_secs`),
                `links_in` = VALUES(`links_in`),
                `links_out` = VALUES(`links_out`),
                `circuits_in` = VALUES(`circuits_in`),
                `circuits_out` = VALUES(`circuits_out`),
                `l3_relayed` = VALUES(`l3_relayed`),
                `status` = VALUES(`status`),
                `last_seen` = VALUES(`last_seen`),
                `first_seen` = COALESCE(`first_seen`, VALUES(`first_seen`)),
                `last_status_update` = VALUES(`last_status_update`),
                `last_up_event` = VALUES(`last_up_event`),
                `last_down_event` = VALUES(`last_down_event`),
                `l2_trace_count` = VALUES(`l2_trace_count`),
                `last_l2_trace` = VALUES(`last_l2_trace`),
                `ip_address_obfuscated` = VALUES(`ip_address_obfuscated`),
                `geoip_country_code` = VALUES(`geoip_country_code`),
                `geoip_country_name` = VALUES(`geoip_country_name`),
                `geoip_city` = VALUES(`geoip_city`),
                `last_ip_update` = VALUES(`last_ip_update`)";

        try
        {
            using var conn = Database.GetConnection(open: false);
            await conn.OpenAsync(ct);

            await QueryLogger.ExecuteWithLoggingAsync(
                conn,
                new CommandDefinition(sql, new
                {
                    node.Callsign,
                    node.Alias,
                    node.Locator,
                    node.Latitude,
                    node.Longitude,
                    node.Software,
                    node.Version,
                    node.UptimeSecs,
                    node.LinksIn,
                    node.LinksOut,
                    node.CircuitsIn,
                    node.CircuitsOut,
                    node.L3Relayed,
                    Status = node.Status.ToString(),
                    node.LastSeen,
                    node.FirstSeen,
                    node.LastStatusUpdate,
                    node.LastUpEvent,
                    node.LastDownEvent,
                    node.L2TraceCount,
                    node.LastL2Trace,
                    node.IpAddressObfuscated,
                    node.GeoIpCountryCode,
                    node.GeoIpCountryName,
                    node.GeoIpCity,
                    node.LastIpUpdate
                }, cancellationToken: ct),
                logger,
                SlowQueryThresholdMs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upsert node {Callsign}", node.Callsign);
            throw;
        }
    }

    public async Task<NodeState?> GetNodeAsync(string callsign, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT 
                `callsign` AS Callsign, `alias` AS Alias, `locator` AS Locator,
                `latitude` AS Latitude, `longitude` AS Longitude,
                `software` AS Software, `version` AS Version,
                `uptime_secs` AS UptimeSecs, `links_in` AS LinksIn, `links_out` AS LinksOut,
                `circuits_in` AS CircuitsIn, `circuits_out` AS CircuitsOut,
                `l3_relayed` AS L3Relayed, `status` AS Status,
                `last_seen` AS LastSeen, `first_seen` AS FirstSeen,
                `last_status_update` AS LastStatusUpdate,
                `last_up_event` AS LastUpEvent, `last_down_event` AS LastDownEvent,
                `l2_trace_count` AS L2TraceCount, `last_l2_trace` AS LastL2Trace,
                `ip_address_obfuscated` AS IpAddressObfuscated, 
                `geoip_country_code` AS GeoIpCountryCode,
                `geoip_country_name` AS GeoIpCountryName,
                `geoip_city` AS GeoIpCity,
                `last_ip_update` AS LastIpUpdate
            FROM `nodes`
            WHERE `callsign` = @callsign";

        try
        {
            using var conn = Database.GetConnection(open: false);
            await conn.OpenAsync(ct);

            var row = await QueryLogger.QuerySingleOrDefaultWithLoggingAsync<NodeRow>(
                conn,
                new CommandDefinition(sql, new { callsign }, cancellationToken: ct),
                logger,
                SlowQueryThresholdMs);

            return row != null ? MapToNodeState(row) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get node {Callsign}", callsign);
            throw;
        }
    }

    public async Task<IReadOnlyList<NodeState>> GetAllNodesAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT 
                `callsign` AS Callsign, `alias` AS Alias, `locator` AS Locator,
                `latitude` AS Latitude, `longitude` AS Longitude,
                `software` AS Software, `version` AS Version,
                `uptime_secs` AS UptimeSecs, `links_in` AS LinksIn, `links_out` AS LinksOut,
                `circuits_in` AS CircuitsIn, `circuits_out` AS CircuitsOut,
                `l3_relayed` AS L3Relayed, `status` AS Status,
                `last_seen` AS LastSeen, `first_seen` AS FirstSeen,
                `last_status_update` AS LastStatusUpdate,
                `last_up_event` AS LastUpEvent, `last_down_event` AS LastDownEvent,
                `l2_trace_count` AS L2TraceCount, `last_l2_trace` AS LastL2Trace,
                `ip_address_obfuscated` AS IpAddressObfuscated,
                `geoip_country_code` AS GeoIpCountryCode,
                `geoip_country_name` AS GeoIpCountryName,
                `geoip_city` AS GeoIpCity,
                `last_ip_update` AS LastIpUpdate
            FROM `nodes`";

        try
        {
            using var conn = Database.GetConnection(open: false);
            await conn.OpenAsync(ct);

            var rows = await QueryLogger.QueryWithLoggingAsync<NodeRow>(
                conn,
                new CommandDefinition(sql, cancellationToken: ct),
                logger,
                SlowQueryThresholdMs);

            return rows.Select(MapToNodeState).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all nodes");
            throw;
        }
    }

    private static NodeState MapToNodeState(NodeRow row)
    {
        return new NodeState
        {
            Callsign = row.Callsign,
            Alias = row.Alias,
            Locator = row.Locator,
            Latitude = row.Latitude,
            Longitude = row.Longitude,
            Software = row.Software,
            Version = row.Version,
            UptimeSecs = row.UptimeSecs,
            LinksIn = row.LinksIn,
            LinksOut = row.LinksOut,
            CircuitsIn = row.CircuitsIn,
            CircuitsOut = row.CircuitsOut,
            L3Relayed = row.L3Relayed,
            Status = Enum.Parse<NodeStatus>(row.Status),
            LastSeen = row.LastSeen,
            FirstSeen = row.FirstSeen,
            LastStatusUpdate = row.LastStatusUpdate,
            LastUpEvent = row.LastUpEvent,
            LastDownEvent = row.LastDownEvent,
            L2TraceCount = row.L2TraceCount,
            LastL2Trace = row.LastL2Trace,
            IpAddressObfuscated = row.IpAddressObfuscated,
            GeoIpCountryCode = row.GeoIpCountryCode,
            GeoIpCountryName = row.GeoIpCountryName,
            GeoIpCity = row.GeoIpCity,
            LastIpUpdate = row.LastIpUpdate
        };
    }

    private class NodeRow
    {
        public string Callsign { get; set; } = null!;
        public string? Alias { get; set; }
        public string? Locator { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Software { get; set; }
        public string? Version { get; set; }
        public int? UptimeSecs { get; set; }
        public int? LinksIn { get; set; }
        public int? LinksOut { get; set; }
        public int? CircuitsIn { get; set; }
        public int? CircuitsOut { get; set; }
        public int? L3Relayed { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? LastSeen { get; set; }
        public DateTime? FirstSeen { get; set; }
        public DateTime? LastStatusUpdate { get; set; }
        public DateTime? LastUpEvent { get; set; }
        public DateTime? LastDownEvent { get; set; }
        public int L2TraceCount { get; set; }
        public DateTime? LastL2Trace { get; set; }
        public string? IpAddressObfuscated { get; set; }
        public string? GeoIpCountryCode { get; set; }
        public string? GeoIpCountryName { get; set; }
        public string? GeoIpCity { get; set; }
        public DateTime? LastIpUpdate { get; set; }
    }

    #endregion

    #region Link Operations

    public async Task UpsertLinkAsync(LinkState link, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO `links` (
                `canonical_key`, `endpoint1`, `endpoint2`, `status`,
                `connected_at`, `disconnected_at`, `last_update`, `initiator`,
                `ep1_node`, `ep1_link_id`, `ep1_direction`, `ep1_port`, `ep1_remote`, `ep1_local`,
                `ep1_frames_sent`, `ep1_frames_received`, `ep1_frames_resent`, `ep1_frames_queued`,
                `ep1_frames_queued_peak`, `ep1_bytes_sent`, `ep1_bytes_received`,
                `ep1_bps_tx_mean`, `ep1_bps_rx_mean`, `ep1_frame_queue_max`, `ep1_l2_rtt_ms`,
                `ep1_up_for_secs`, `ep1_last_update`,
                `ep2_node`, `ep2_link_id`, `ep2_direction`, `ep2_port`, `ep2_remote`, `ep2_local`,
                `ep2_frames_sent`, `ep2_frames_received`, `ep2_frames_resent`, `ep2_frames_queued`,
                `ep2_frames_queued_peak`, `ep2_bytes_sent`, `ep2_bytes_received`,
                `ep2_bps_tx_mean`, `ep2_bps_rx_mean`, `ep2_frame_queue_max`, `ep2_l2_rtt_ms`,
                `ep2_up_for_secs`, `ep2_last_update`
            ) VALUES (
                @CanonicalKey, @Endpoint1, @Endpoint2, @Status,
                @ConnectedAt, @DisconnectedAt, @LastUpdate, @Initiator,
                @Ep1Node, @Ep1LinkId, @Ep1Direction, @Ep1Port, @Ep1Remote, @Ep1Local,
                @Ep1FramesSent, @Ep1FramesReceived, @Ep1FramesResent, @Ep1FramesQueued,
                @Ep1FramesQueuedPeak, @Ep1BytesSent, @Ep1BytesReceived,
                @Ep1BpsTxMean, @Ep1BpsRxMean, @Ep1FrameQueueMax, @Ep1L2RttMs,
                @Ep1UpForSecs, @Ep1LastUpdate,
                @Ep2Node, @Ep2LinkId, @Ep2Direction, @Ep2Port, @Ep2Remote, @Ep2Local,
                @Ep2FramesSent, @Ep2FramesReceived, @Ep2FramesResent, @Ep2FramesQueued,
                @Ep2FramesQueuedPeak, @Ep2BytesSent, @Ep2BytesReceived,
                @Ep2BpsTxMean, @Ep2BpsRxMean, @Ep2FrameQueueMax, @Ep2L2RttMs,
                @Ep2UpForSecs, @Ep2LastUpdate
            )
            ON DUPLICATE KEY UPDATE
                `status` = VALUES(`status`),
                `connected_at` = COALESCE(`connected_at`, VALUES(`connected_at`)),
                `disconnected_at` = VALUES(`disconnected_at`),
                `last_update` = VALUES(`last_update`),
                `initiator` = COALESCE(`initiator`, VALUES(`initiator`)),
                `ep1_node` = COALESCE(VALUES(`ep1_node`), `ep1_node`),
                `ep1_link_id` = COALESCE(VALUES(`ep1_link_id`), `ep1_link_id`),
                `ep1_direction` = COALESCE(VALUES(`ep1_direction`), `ep1_direction`),
                `ep1_port` = COALESCE(VALUES(`ep1_port`), `ep1_port`),
                `ep1_remote` = COALESCE(VALUES(`ep1_remote`), `ep1_remote`),
                `ep1_local` = COALESCE(VALUES(`ep1_local`), `ep1_local`),
                `ep1_frames_sent` = COALESCE(VALUES(`ep1_frames_sent`), `ep1_frames_sent`),
                `ep1_frames_received` = COALESCE(VALUES(`ep1_frames_received`), `ep1_frames_received`),
                `ep1_frames_resent` = COALESCE(VALUES(`ep1_frames_resent`), `ep1_frames_resent`),
                `ep1_frames_queued` = COALESCE(VALUES(`ep1_frames_queued`), `ep1_frames_queued`),
                `ep1_frames_queued_peak` = COALESCE(VALUES(`ep1_frames_queued_peak`), `ep1_frames_queued_peak`),
                `ep1_bytes_sent` = COALESCE(VALUES(`ep1_bytes_sent`), `ep1_bytes_sent`),
                `ep1_bytes_received` = COALESCE(VALUES(`ep1_bytes_received`), `ep1_bytes_received`),
                `ep1_bps_tx_mean` = COALESCE(VALUES(`ep1_bps_tx_mean`), `ep1_bps_tx_mean`),
                `ep1_bps_rx_mean` = COALESCE(VALUES(`ep1_bps_rx_mean`), `ep1_bps_rx_mean`),
                `ep1_frame_queue_max` = COALESCE(VALUES(`ep1_frame_queue_max`), `ep1_frame_queue_max`),
                `ep1_l2_rtt_ms` = COALESCE(VALUES(`ep1_l2_rtt_ms`), `ep1_l2_rtt_ms`),
                `ep1_up_for_secs` = COALESCE(VALUES(`ep1_up_for_secs`), `ep1_up_for_secs`),
                `ep1_last_update` = COALESCE(VALUES(`ep1_last_update`), `ep1_last_update`),
                `ep2_node` = COALESCE(VALUES(`ep2_node`), `ep2_node`),
                `ep2_link_id` = COALESCE(VALUES(`ep2_link_id`), `ep2_link_id`),
                `ep2_direction` = COALESCE(VALUES(`ep2_direction`), `ep2_direction`),
                `ep2_port` = COALESCE(VALUES(`ep2_port`), `ep2_port`),
                `ep2_remote` = COALESCE(VALUES(`ep2_remote`), `ep2_remote`),
                `ep2_local` = COALESCE(VALUES(`ep2_local`), `ep2_local`),
                `ep2_frames_sent` = COALESCE(VALUES(`ep2_frames_sent`), `ep2_frames_sent`),
                `ep2_frames_received` = COALESCE(VALUES(`ep2_frames_received`), `ep2_frames_received`),
                `ep2_frames_resent` = COALESCE(VALUES(`ep2_frames_resent`), `ep2_frames_resent`),
                `ep2_frames_queued` = COALESCE(VALUES(`ep2_frames_queued`), `ep2_frames_queued`),
                `ep2_frames_queued_peak` = COALESCE(VALUES(`ep2_frames_queued_peak`), `ep2_frames_queued_peak`),
                `ep2_bytes_sent` = COALESCE(VALUES(`ep2_bytes_sent`), `ep2_bytes_sent`),
                `ep2_bytes_received` = COALESCE(VALUES(`ep2_bytes_received`), `ep2_bytes_received`),
                `ep2_bps_tx_mean` = COALESCE(VALUES(`ep2_bps_tx_mean`), `ep2_bps_tx_mean`),
                `ep2_bps_rx_mean` = COALESCE(VALUES(`ep2_bps_rx_mean`), `ep2_bps_rx_mean`),
                `ep2_frame_queue_max` = COALESCE(VALUES(`ep2_frame_queue_max`), `ep2_frame_queue_max`),
                `ep2_l2_rtt_ms` = COALESCE(VALUES(`ep2_l2_rtt_ms`), `ep2_l2_rtt_ms`),
                `ep2_up_for_secs` = COALESCE(VALUES(`ep2_up_for_secs`), `ep2_up_for_secs`),
                `ep2_last_update` = COALESCE(VALUES(`ep2_last_update`), `ep2_last_update`)";

        try
        {
            link.Endpoints.TryGetValue(link.Endpoint1, out var ep1);
            link.Endpoints.TryGetValue(link.Endpoint2, out var ep2);

            using var conn = Database.GetConnection(open: false);
            await conn.OpenAsync(ct);

            await QueryLogger.ExecuteWithLoggingAsync(
                conn,
                new CommandDefinition(sql, new
                {
                    link.CanonicalKey,
                    link.Endpoint1,
                    link.Endpoint2,
                    Status = link.Status.ToString(),
                    link.ConnectedAt,
                    link.DisconnectedAt,
                    link.LastUpdate,
                    link.Initiator,
                    Ep1Node = ep1?.Node,
                    Ep1LinkId = ep1?.Id,
                    Ep1Direction = ep1?.Direction,
                    Ep1Port = ep1?.Port,
                    Ep1Remote = ep1?.Remote,
                    Ep1Local = ep1?.Local,
                    Ep1FramesSent = ep1?.FramesSent,
                    Ep1FramesReceived = ep1?.FramesReceived,
                    Ep1FramesResent = ep1?.FramesResent,
                    Ep1FramesQueued = ep1?.FramesQueued,
                    Ep1FramesQueuedPeak = ep1?.FramesQueuedPeak,
                    Ep1BytesSent = ep1?.BytesSent,
                    Ep1BytesReceived = ep1?.BytesReceived,
                    Ep1BpsTxMean = ep1?.BpsTxMean,
                    Ep1BpsRxMean = ep1?.BpsRxMean,
                    Ep1FrameQueueMax = ep1?.FrameQueueMax,
                    Ep1L2RttMs = ep1?.L2RttMs,
                    Ep1UpForSecs = ep1?.UpForSecs,
                    Ep1LastUpdate = ep1?.LastUpdate,
                    Ep2Node = ep2?.Node,
                    Ep2LinkId = ep2?.Id,
                    Ep2Direction = ep2?.Direction,
                    Ep2Port = ep2?.Port,
                    Ep2Remote = ep2?.Remote,
                    Ep2Local = ep2?.Local,
                    Ep2FramesSent = ep2?.FramesSent,
                    Ep2FramesReceived = ep2?.FramesReceived,
                    Ep2FramesResent = ep2?.FramesResent,
                    Ep2FramesQueued = ep2?.FramesQueued,
                    Ep2FramesQueuedPeak = ep2?.FramesQueuedPeak,
                    Ep2BytesSent = ep2?.BytesSent,
                    Ep2BytesReceived = ep2?.BytesReceived,
                    Ep2BpsTxMean = ep2?.BpsTxMean,
                    Ep2BpsRxMean = ep2?.BpsRxMean,
                    Ep2FrameQueueMax = ep2?.FrameQueueMax,
                    Ep2L2RttMs = ep2?.L2RttMs,
                    Ep2UpForSecs = ep2?.UpForSecs,
                    Ep2LastUpdate = ep2?.LastUpdate
                }, cancellationToken: ct),
                logger,
                SlowQueryThresholdMs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upsert link {CanonicalKey}", link.CanonicalKey);
            throw;
        }
    }

    public async Task<LinkState?> GetLinkAsync(string canonicalKey, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT *
            FROM `links`
            WHERE `canonical_key` = @canonicalKey";

        try
        {
            using var conn = Database.GetConnection(open: false);
            await conn.OpenAsync(ct);

            var row = await QueryLogger.QuerySingleOrDefaultWithLoggingAsync<LinkRow>(
                conn,
                new CommandDefinition(sql, new { canonicalKey }, cancellationToken: ct),
                logger,
                SlowQueryThresholdMs);

            return row != null ? MapToLinkState(row) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get link {CanonicalKey}", canonicalKey);
            throw;
        }
    }

    public async Task<IReadOnlyList<LinkState>> GetAllLinksAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM `links`";

        try
        {
            using var conn = Database.GetConnection(open: false);
            await conn.OpenAsync(ct);

            var rows = await QueryLogger.QueryWithLoggingAsync<LinkRow>(
                conn,
                new CommandDefinition(sql, cancellationToken: ct),
                logger,
                SlowQueryThresholdMs);

            return rows.Select(MapToLinkState).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all links");
            throw;
        }
    }

    private static LinkState MapToLinkState(LinkRow row)
    {
        var endpoints = new Dictionary<string, LinkEndpointState>();

        if (row.ep1_node != null)
        {
            endpoints[row.endpoint1] = new LinkEndpointState
            {
                Node = row.ep1_node,
                Id = row.ep1_link_id!.Value,
                Direction = row.ep1_direction!,
                Port = row.ep1_port!,
                Local = row.ep1_local!,
                Remote = row.ep1_remote!,
                LastUpdate = row.ep1_last_update ?? DateTime.UtcNow,
                UpForSecs = row.ep1_up_for_secs,
                FramesSent = row.ep1_frames_sent,
                FramesReceived = row.ep1_frames_received,
                FramesResent = row.ep1_frames_resent,
                FramesQueued = row.ep1_frames_queued,
                FramesQueuedPeak = row.ep1_frames_queued_peak,
                BytesSent = row.ep1_bytes_sent,
                BytesReceived = row.ep1_bytes_received,
                BpsTxMean = row.ep1_bps_tx_mean,
                BpsRxMean = row.ep1_bps_rx_mean,
                FrameQueueMax = row.ep1_frame_queue_max,
                L2RttMs = row.ep1_l2_rtt_ms
            };
        }

        if (row.ep2_node != null)
        {
            endpoints[row.endpoint2] = new LinkEndpointState
            {
                Node = row.ep2_node,
                Id = row.ep2_link_id!.Value,
                Direction = row.ep2_direction!,
                Port = row.ep2_port!,
                Local = row.ep2_local!,
                Remote = row.ep2_remote!,
                LastUpdate = row.ep2_last_update ?? DateTime.UtcNow,
                UpForSecs = row.ep2_up_for_secs,
                FramesSent = row.ep2_frames_sent,
                FramesReceived = row.ep2_frames_received,
                FramesResent = row.ep2_frames_resent,
                FramesQueued = row.ep2_frames_queued,
                FramesQueuedPeak = row.ep2_frames_queued_peak,
                BytesSent = row.ep2_bytes_sent,
                BytesReceived = row.ep2_bytes_received,
                BpsTxMean = row.ep2_bps_tx_mean,
                BpsRxMean = row.ep2_bps_rx_mean,
                FrameQueueMax = row.ep2_frame_queue_max,
                L2RttMs = row.ep2_l2_rtt_ms
            };
        }

        return new LinkState
        {
            CanonicalKey = row.canonical_key,
            Endpoint1 = row.endpoint1,
            Endpoint2 = row.endpoint2,
            Status = Enum.Parse<LinkStatus>(row.status),
            ConnectedAt = row.connected_at ?? DateTime.UtcNow,
            DisconnectedAt = row.disconnected_at,
            LastUpdate = row.last_update,
            Initiator = row.initiator,
            Endpoints = endpoints
        };
    }

    private class LinkRow
    {
        public string canonical_key { get; set; } = null!;
        public string endpoint1 { get; set; } = null!;
        public string endpoint2 { get; set; } = null!;
        public string status { get; set; } = null!;
        public DateTime? connected_at { get; set; }
        public DateTime? disconnected_at { get; set; }
        public DateTime last_update { get; set; }
        public string? initiator { get; set; }
        public string? ep1_node { get; set; }
        public int? ep1_link_id { get; set; }
        public string? ep1_direction { get; set; }
        public string? ep1_port { get; set; }
        public string? ep1_remote { get; set; }
        public string? ep1_local { get; set; }
        public int? ep1_frames_sent { get; set; }
        public int? ep1_frames_received { get; set; }
        public int? ep1_frames_resent { get; set; }
        public int? ep1_frames_queued { get; set; }
        public int? ep1_frames_queued_peak { get; set; }
        public int? ep1_bytes_sent { get; set; }
        public int? ep1_bytes_received { get; set; }
        public int? ep1_bps_tx_mean { get; set; }
        public int? ep1_bps_rx_mean { get; set; }
        public int? ep1_frame_queue_max { get; set; }
        public int? ep1_l2_rtt_ms { get; set; }
        public int? ep1_up_for_secs { get; set; }
        public DateTime? ep1_last_update { get; set; }
        public string? ep2_node { get; set; }
        public int? ep2_link_id { get; set; }
        public string? ep2_direction { get; set; }
        public string? ep2_port { get; set; }
        public string? ep2_remote { get; set; }
        public string? ep2_local { get; set; }
        public int? ep2_frames_sent { get; set; }
        public int? ep2_frames_received { get; set; }
        public int? ep2_frames_resent { get; set; }
        public int? ep2_frames_queued { get; set; }
        public int? ep2_frames_queued_peak { get; set; }
        public int? ep2_bytes_sent { get; set; }
        public int? ep2_bytes_received { get; set; }
        public int? ep2_bps_tx_mean { get; set; }
        public int? ep2_bps_rx_mean { get; set; }
        public int? ep2_frame_queue_max { get; set; }
        public int? ep2_l2_rtt_ms { get; set; }
        public int? ep2_up_for_secs { get; set; }
        public DateTime? ep2_last_update { get; set; }
    }

    #endregion

    #region Circuit Operations

    public async Task UpsertCircuitAsync(CircuitState circuit, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO `circuits` (
                `canonical_key`, `endpoint1`, `endpoint2`, `status`,
                `connected_at`, `disconnected_at`, `last_update`, `initiator`,
                `ep1_node`, `ep1_circuit_id`, `ep1_direction`, `ep1_service`, `ep1_remote`, `ep1_local`,
                `ep1_segments_sent`, `ep1_segments_received`, `ep1_segments_resent`, `ep1_segments_queued`,
                `ep1_bytes_sent`, `ep1_bytes_received`, `ep1_last_update`,
                `ep2_node`, `ep2_circuit_id`, `ep2_direction`, `ep2_service`, `ep2_remote`, `ep2_local`,
                `ep2_segments_sent`, `ep2_segments_received`, `ep2_segments_resent`, `ep2_segments_queued`,
                `ep2_bytes_sent`, `ep2_bytes_received`, `ep2_last_update`
            ) VALUES (
                @CanonicalKey, @Endpoint1, @Endpoint2, @Status,
                @ConnectedAt, @DisconnectedAt, @LastUpdate, @Initiator,
                @Ep1Node, @Ep1CircuitId, @Ep1Direction, @Ep1Service, @Ep1Remote, @Ep1Local,
                @Ep1SegmentsSent, @Ep1SegmentsReceived, @Ep1SegmentsResent, @Ep1SegmentsQueued,
                @Ep1BytesSent, @Ep1BytesReceived, @Ep1LastUpdate,
                @Ep2Node, @Ep2CircuitId, @Ep2Direction, @Ep2Service, @Ep2Remote, @Ep2Local,
                @Ep2SegmentsSent, @Ep2SegmentsReceived, @Ep2SegmentsResent, @Ep2SegmentsQueued,
                @Ep2BytesSent, @Ep2BytesReceived, @Ep2LastUpdate
            )
            ON DUPLICATE KEY UPDATE
                `status` = VALUES(`status`),
                `connected_at` = COALESCE(`connected_at`, VALUES(`connected_at`)),
                `disconnected_at` = VALUES(`disconnected_at`),
                `last_update` = VALUES(`last_update`),
                `initiator` = COALESCE(`initiator`, VALUES(`initiator`)),
                `ep1_node` = COALESCE(VALUES(`ep1_node`), `ep1_node`),
                `ep1_circuit_id` = COALESCE(VALUES(`ep1_circuit_id`), `ep1_circuit_id`),
                `ep1_direction` = COALESCE(VALUES(`ep1_direction`), `ep1_direction`),
                `ep1_service` = COALESCE(VALUES(`ep1_service`), `ep1_service`),
                `ep1_remote` = COALESCE(VALUES(`ep1_remote`), `ep1_remote`),
                `ep1_local` = COALESCE(VALUES(`ep1_local`), `ep1_local`),
                `ep1_segments_sent` = COALESCE(VALUES(`ep1_segments_sent`), `ep1_segments_sent`),
                `ep1_segments_received` = COALESCE(VALUES(`ep1_segments_received`), `ep1_segments_received`),
                `ep1_segments_resent` = COALESCE(VALUES(`ep1_segments_resent`), `ep1_segments_resent`),
                `ep1_segments_queued` = COALESCE(VALUES(`ep1_segments_queued`), `ep1_segments_queued`),
                `ep1_bytes_sent` = COALESCE(VALUES(`ep1_bytes_sent`), `ep1_bytes_sent`),
                `ep1_bytes_received` = COALESCE(VALUES(`ep1_bytes_received`), `ep1_bytes_received`),
                `ep1_last_update` = COALESCE(VALUES(`ep1_last_update`), `ep1_last_update`),
                `ep2_node` = COALESCE(VALUES(`ep2_node`), `ep2_node`),
                `ep2_circuit_id` = COALESCE(VALUES(`ep2_circuit_id`), `ep2_circuit_id`),
                `ep2_direction` = COALESCE(VALUES(`ep2_direction`), `ep2_direction`),
                `ep2_service` = COALESCE(VALUES(`ep2_service`), `ep2_service`),
                `ep2_remote` = COALESCE(VALUES(`ep2_remote`), `ep2_remote`),
                `ep2_local` = COALESCE(VALUES(`ep2_local`), `ep2_local`),
                `ep2_segments_sent` = COALESCE(VALUES(`ep2_segments_sent`), `ep2_segments_sent`),
                `ep2_segments_received` = COALESCE(VALUES(`ep2_segments_received`), `ep2_segments_received`),
                `ep2_segments_resent` = COALESCE(VALUES(`ep2_segments_resent`), `ep2_segments_resent`),
                `ep2_segments_queued` = COALESCE(VALUES(`ep2_segments_queued`), `ep2_segments_queued`),
                `ep2_bytes_sent` = COALESCE(VALUES(`ep2_bytes_sent`), `ep2_bytes_sent`),
                `ep2_bytes_received` = COALESCE(VALUES(`ep2_bytes_received`), `ep2_bytes_received`),
                `ep2_last_update` = COALESCE(VALUES(`ep2_last_update`), `ep2_last_update`)";

        try
        {
            circuit.Endpoints.TryGetValue(circuit.Endpoint1, out var ep1);
            circuit.Endpoints.TryGetValue(circuit.Endpoint2, out var ep2);

            using var conn = Database.GetConnection(open: false);
            await conn.OpenAsync(ct);

            await QueryLogger.ExecuteWithLoggingAsync(
                conn,
                new CommandDefinition(sql, new
                {
                    circuit.CanonicalKey,
                    circuit.Endpoint1,
                    circuit.Endpoint2,
                    Status = circuit.Status.ToString(),
                    circuit.ConnectedAt,
                    circuit.DisconnectedAt,
                    circuit.LastUpdate,
                    circuit.Initiator,
                    Ep1Node = ep1?.Node,
                    Ep1CircuitId = ep1?.Id,
                    Ep1Direction = ep1?.Direction,
                    Ep1Service = ep1?.Service,
                    Ep1Remote = ep1?.Remote,
                    Ep1Local = ep1?.Local,
                    Ep1SegmentsSent = ep1?.SegmentsSent,
                    Ep1SegmentsReceived = ep1?.SegmentsReceived,
                    Ep1SegmentsResent = ep1?.SegmentsResent,
                    Ep1SegmentsQueued = ep1?.SegmentsQueued,
                    Ep1BytesSent = ep1?.BytesSent,
                    Ep1BytesReceived = ep1?.BytesReceived,
                    Ep1LastUpdate = ep1?.LastUpdate,
                    Ep2Node = ep2?.Node,
                    Ep2CircuitId = ep2?.Id,
                    Ep2Direction = ep2?.Direction,
                    Ep2Service = ep2?.Service,
                    Ep2Remote = ep2?.Remote,
                    Ep2Local = ep2?.Local,
                    Ep2SegmentsSent = ep2?.SegmentsSent,
                    Ep2SegmentsReceived = ep2?.SegmentsReceived,
                    Ep2SegmentsResent = ep2?.SegmentsResent,
                    Ep2SegmentsQueued = ep2?.SegmentsQueued,
                    Ep2BytesSent = ep2?.BytesSent,
                    Ep2BytesReceived = ep2?.BytesReceived,
                    Ep2LastUpdate = ep2?.LastUpdate
                }, cancellationToken: ct),
                logger,
                SlowQueryThresholdMs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upsert circuit {CanonicalKey}", circuit.CanonicalKey);
            throw;
        }
    }

    public async Task<CircuitState?> GetCircuitAsync(string canonicalKey, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT *
            FROM `circuits`
            WHERE `canonical_key` = @canonicalKey";

        try
        {
            using var conn = Database.GetConnection(open: false);
            await conn.OpenAsync(ct);

            var row = await QueryLogger.QuerySingleOrDefaultWithLoggingAsync<CircuitRow>(
                conn,
                new CommandDefinition(sql, new { canonicalKey }, cancellationToken: ct),
                logger,
                SlowQueryThresholdMs);

            return row != null ? MapToCircuitState(row) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get circuit {CanonicalKey}", canonicalKey);
            throw;
        }
    }

    public async Task<IReadOnlyList<CircuitState>> GetAllCircuitsAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM `circuits`";

        try
        {
            using var conn = Database.GetConnection(open: false);
            await conn.OpenAsync(ct);

            var rows = await QueryLogger.QueryWithLoggingAsync<CircuitRow>(
                conn,
                new CommandDefinition(sql, cancellationToken: ct),
                logger,
                SlowQueryThresholdMs);

            return rows.Select(MapToCircuitState).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all circuits");
            throw;
        }
    }

    private static CircuitState MapToCircuitState(CircuitRow row)
    {
        var endpoints = new Dictionary<string, CircuitEndpointState>();

        if (row.ep1_node != null)
        {
            endpoints[row.endpoint1] = new CircuitEndpointState
            {
                Node = row.ep1_node,
                Id = row.ep1_circuit_id!.Value,
                Direction = row.ep1_direction!,
                Service = row.ep1_service,
                Remote = row.ep1_remote!,
                Local = row.ep1_local!,
                LastUpdate = row.ep1_last_update ?? DateTime.UtcNow,
                SegmentsSent = row.ep1_segments_sent,
                SegmentsReceived = row.ep1_segments_received,
                SegmentsResent = row.ep1_segments_resent,
                SegmentsQueued = row.ep1_segments_queued,
                BytesSent = row.ep1_bytes_sent,
                BytesReceived = row.ep1_bytes_received
            };
        }

        if (row.ep2_node != null)
        {
            endpoints[row.endpoint2] = new CircuitEndpointState
            {
                Node = row.ep2_node,
                Id = row.ep2_circuit_id!.Value,
                Direction = row.ep2_direction!,
                Service = row.ep2_service,
                Remote = row.ep2_remote!,
                Local = row.ep2_local!,
                LastUpdate = row.ep2_last_update ?? DateTime.UtcNow,
                SegmentsSent = row.ep2_segments_sent,
                SegmentsReceived = row.ep2_segments_received,
                SegmentsResent = row.ep2_segments_resent,
                SegmentsQueued = row.ep2_segments_queued,
                BytesSent = row.ep2_bytes_sent,
                BytesReceived = row.ep2_bytes_received
            };
        }

        return new CircuitState
        {
            CanonicalKey = row.canonical_key,
            Endpoint1 = row.endpoint1,
            Endpoint2 = row.endpoint2,
            Status = Enum.Parse<CircuitStatus>(row.status),
            ConnectedAt = row.connected_at ?? DateTime.UtcNow,
            DisconnectedAt = row.disconnected_at,
            LastUpdate = row.last_update,
            Initiator = row.initiator,
            Endpoints = endpoints
        };
    }

    private class CircuitRow
    {
        public string canonical_key { get; set; } = null!;
        public string endpoint1 { get; set; } = null!;
        public string endpoint2 { get; set; } = null!;
        public string status { get; set; } = null!;
        public DateTime? connected_at { get; set; }
        public DateTime? disconnected_at { get; set; }
        public DateTime last_update { get; set; }
        public string? initiator { get; set; }
        public string? ep1_node { get; set; }
        public int? ep1_circuit_id { get; set; }
        public string? ep1_direction { get; set; }
        public int? ep1_service { get; set; }
        public string? ep1_remote { get; set; }
        public string? ep1_local { get; set; }
        public int? ep1_segments_sent { get; set; }
        public int? ep1_segments_received { get; set; }
        public int? ep1_segments_resent { get; set; }
        public int? ep1_segments_queued { get; set; }
        public int? ep1_bytes_sent { get; set; }
        public int? ep1_bytes_received { get; set; }
        public DateTime? ep1_last_update { get; set; }
        public string? ep2_node { get; set; }
        public int? ep2_circuit_id { get; set; }
        public string? ep2_direction { get; set; }
        public int? ep2_service { get; set; }
        public string? ep2_remote { get; set; }
        public string? ep2_local { get; set; }
        public int? ep2_segments_sent { get; set; }
        public int? ep2_segments_received { get; set; }
        public int? ep2_segments_resent { get; set; }
        public int? ep2_segments_queued { get; set; }
        public int? ep2_bytes_sent { get; set; }
        public int? ep2_bytes_received { get; set; }
        public DateTime? ep2_last_update { get; set; }
    }

    #endregion
}
