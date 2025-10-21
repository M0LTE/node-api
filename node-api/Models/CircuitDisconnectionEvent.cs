using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// 3.8. Circuit Disconnection Event
/// This report is sent when a NetRom Layer 4 connection is torn down.
/// </summary>
public record CircuitDisconnectionEvent : UdpNodeInfoJsonDatagram
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

    /// <summary>
    /// Segments sent since circuit creation
    /// </summary>
    [JsonPropertyName("segsSent")]
    public required int SegsSent { get; init; }

    /// <summary>
    /// Segments rcvd since circuit creation
    /// </summary>
    [JsonPropertyName("segsRcvd")]
    public required int SegsRcvd { get; init; }

    /// <summary>
    /// Segments re-sent since cct creation
    /// </summary>
    [JsonPropertyName("segsResent")]
    public required int SegsResent { get; init; }

    /// <summary>
    /// Current TX queue length
    /// </summary>
    [JsonPropertyName("segsQueued")]
    public required int SegsQueued { get; init; }

    /// <summary>
    /// Reason for disconnect (Optional)
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>
    /// Number of info bytes received and queued for the consumer (Optional)
    /// </summary>
    [JsonPropertyName("bytesRcvd")]
    public int? BytesReceived { get; init; }

    /// <summary>
    /// Number of info bytes transferred and acknowledged by the other end (Optional)
    /// </summary>
    [JsonPropertyName("bytesSent")]
    public int? BytesSent { get; init; }
}
