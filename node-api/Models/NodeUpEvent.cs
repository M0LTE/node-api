using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// 3.1.  Node Up Event
/// This report is sent when a node software starts running.
/// </summary>
public record NodeUpEvent : UdpNodeInfoJsonDatagram
{
    /// <summary>
    /// Timestamp (secs since 1/1/70) (Optional)
    /// </summary>
    [JsonPropertyName("time")]
    public long? TimeUnixSeconds { get; init; }

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
}