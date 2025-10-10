using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// 3.2.  Node Down Event
/// This report is when a node is in the process of going offline.  It
/// is unlikely to be sent when a node crashes, unless the error handler
/// is hooked. However a crash may be inferred if a node fails to report
/// within a reasonable interval, or if a subsequent NodeUpEvent
/// is received with the same callsign and alias.
/// </summary>
public record NodeDownEvent : UdpNodeInfoJsonDatagram
{
    /*
     *Name          Type     Description
      -------------------------------------------------------------
      "@type"       String   Report type: "NodeDownEvent"
      "nodeCall"    String   Node Callsign
      "nodeAlias"   String   Node Alias
      "reason"      String   Reason for the shutdown, e.g. "reboot"
     */

    [JsonPropertyName("nodeCall")]
    public required string NodeCall { get; init; }

    [JsonPropertyName("nodeAlias")]
    public required string NodeAlias { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}