using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// 3.7. Circuit Connection Event
/// This report is sent when a NetRom Layer 4 connection enters the fully connected state.
/// </summary>
public record CircuitUpEvent : UdpNodeInfoJsonDatagram
{
    /// <summary>
    /// Timestamp (secs since 1/1/70) (Optional)
    /// </summary>
    [JsonPropertyName("time")]
    public long? TimeUnixSeconds { get; init; }

    /// <summary>
    /// Callsign of reporting node (Required)
    /// </summary>
    [JsonPropertyName("node")]
    public required string Node { get; init; }

    /// <summary>
    /// Circuit serial number (Required)
    /// </summary>
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    /// <summary>
    /// "incoming" or "outgoing" (Required)
    /// </summary>
    [JsonPropertyName("direction")]
    public required string Direction { get; init; }

    /// <summary>
    /// NetRomX "service number" (Optional)
    /// </summary>
    [JsonPropertyName("service")]
    public int? Service { get; init; }

    /// <summary>
    /// Remote address, e.g. "G8PZT@G8PZT:14c0" (Required)
    /// </summary>
    [JsonPropertyName("remote")]
    public required string Remote { get; init; }

    /// <summary>
    /// Local address, e.g. "G8PZT-4:0001" (Required)
    /// </summary>
    [JsonPropertyName("local")]
    public required string Local { get; init; }
}
