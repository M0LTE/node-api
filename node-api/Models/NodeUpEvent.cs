using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// 3.1.  Node Up Event
/// This report is sent when a node software starts running.
/// </summary>
public record NodeUpEvent : UdpNodeInfoJsonDatagram
{
    /*
     *Name          Type     Description
      -------------------------------------------------------------
      "@type"       String   Report type: "NodeUpEvent"
      "nodeCall"    String   Node Callsign
      "nodeAlias"   String   Node Alias
      "locator"     String   Maidenhead locator e.g. "IO82VJ"
      "latitude"    Number   Decimal degrees (optional)
      "longitude"   Number   Decimal degrees (optional)
      "software"    String   Node software type, e.g. "xrlin"
      "version"     String   Node software Version, e.g. "v504j"
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
}