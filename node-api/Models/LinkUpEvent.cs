using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// 3.4. Link Connection Event
/// This report is sent when an AX25 connection first enters the "connected" state.
/// For the "direction" field, "incoming" indicates that the event was initiated from the remote end (i.e. an uplink),
/// and "outgoing" means that it was initiated from the local end, i.e. a downlink.
/// </summary>
public record LinkUpEvent : UdpNodeInfoJsonDatagram
{
    [JsonPropertyName("time")]
    public required long TimeUnixSeconds { get; init; }
    
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
    /// "incoming" indicates that the event was initiated from the remote end (i.e. an uplink).
    /// "outgoing" means that it was initiated from the local end, i.e. a downlink.
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
}
