using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// 3.3. Node Status Report
/// This report serves three purposes:
/// 1. It confirms the node's existence in case a NodeUpEvent was not seen, because some nodes may be up for months or years.
/// 2. It conveys additional status information such as "uptime" which may be useful if a node keeps disappearing after a certain uptime.
/// 3. It is sent at regular intervals, so a sudden lack of reports could indicate that a node had crashed without sending a NodeDownEvent.
/// This allows the consumer of the data to purge expired nodes from the database, and maybe to alert people that a node was potentially down.
/// </summary>
public record NodeStatusReportEvent : UdpNodeInfoJsonDatagram
{
    /// <summary>
    /// Node Callsign (Required)
    /// </summary>
    [JsonPropertyName("nodeCall")]
    public required string NodeCall { get; init; }

    /// <summary>
    /// Node Alias (Required)
    /// </summary>
    [JsonPropertyName("nodeAlias")]
    public required string NodeAlias { get; init; }

    /// <summary>
    /// Maidenhead locator e.g. "IO82VJ" (Required)
    /// </summary>
    [JsonPropertyName("locator")]
    public required string Locator { get; init; }

    /// <summary>
    /// Latitude in decimal degrees (Optional)
    /// </summary>
    [JsonPropertyName("latitude")]
    public decimal? Latitude { get; init; }

    /// <summary>
    /// Longitude in decimal degrees (Optional)
    /// </summary>
    [JsonPropertyName("longitude")]
    public decimal? Longitude { get; init; }

    /// <summary>
    /// Node software type, e.g. "xrlin" (Required)
    /// </summary>
    [JsonPropertyName("software")]
    public required string Software { get; init; }

    /// <summary>
    /// Node software Version, e.g. "v504j" (Required)
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    /// <summary>
    /// Node's uptime in seconds (Required)
    /// </summary>
    [JsonPropertyName("uptimeSecs")]
    public required int UptimeSecs { get; init; }
}
