using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// 3.5. Link Disconnection Event
/// This report is sent when an AX25 connection is destroyed.
/// For the "direction" field, "incoming" indicates that the disconnection was initiated from the remote end,
/// and "outgoing" means that it was initiated from the local end.
/// </summary>
public record LinkDisconnectionEvent : UdpNodeInfoJsonDatagram
{
    /// <summary>
    /// Callsign of reporting node (Required)
    /// </summary>
    [JsonPropertyName("node")]
    public required string Node { get; init; }

    /// <summary>
    /// Link serial number (Required)
    /// </summary>
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    /// <summary>
    /// Initiator: "incoming" or "outgoing" (Required)
    /// "incoming" indicates that the disconnection was initiated from the remote end.
    /// "outgoing" means that it was initiated from the local end.
    /// </summary>
    [JsonPropertyName("direction")]
    public required string Direction { get; init; }

    /// <summary>
    /// Port identifier, e.g. "2" (Required)
    /// </summary>
    [JsonPropertyName("port")]
    public required string Port { get; init; }

    /// <summary>
    /// Remote callsign (Required)
    /// </summary>
    [JsonPropertyName("remote")]
    public required string Remote { get; init; }

    /// <summary>
    /// Local callsign (Required)
    /// </summary>
    [JsonPropertyName("local")]
    public required string Local { get; init; }

    /// <summary>
    /// Reason for disconnect, e.g. "Retried out" (Optional)
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}
