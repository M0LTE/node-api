using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// 3.3.  Node Status Report
///    This report serves three purposes.  Firstly it confirms the node's
///    existence in case a NodeUpEvent was not seen, because some nodes
///    may be up for months or years.Secondly, it conveys additional
///    status information such as "uptime" which may be useful if a node
///    keeps disappearing after a certain uptime.Thirdly, it is sent at
///    regular intervals, so a sudden lack of reports could indicate that
///    a node had crashed without sending a NodeDownEvent.This allows
///    the consumer of the data to purge expired nodes from the database,
///    and maybe to alert people that a node was potentially down.
/// </summary>
public record NodeStatusReportEvent : UdpNodeInfoJsonDatagram
{
    /*
      Name          Type     Description
      -------------------------------------------------------------
      "@type"       String   Report type: "NodeStatus"
      "nodeCall"    String   Node Callsign
      "nodeAlias"   String   Node Alias
      "locator"     String   Maidenhead locator e.g. "IO82VJ"
      "latitude"    Number   Decimal degrees (optional)
      "longitude"   Number   Decimal degrees (optional)
      "software"    String   Node software type, e.g. "xrlin"
      "version"     String   Node software Version, e.g. "v504j"
      "uptimeSecs"  Integer  Node's uptime in seconds
     */

    [JsonPropertyName("nodeCall")]
    public required string NodeCall { get; init; }

    [JsonPropertyName("nodeAlias")]
    public required string NodeAlias { get; init; }

    [JsonPropertyName("locator")]
    public required string Locator { get; init; }

    [JsonPropertyName("latitude")]
    public decimal? Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public decimal? Longitude { get; init; }

    [JsonPropertyName("software")]
    public required string Software { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("uptimeSecs")]
    public int UptimeSecs { get; init; }
}
