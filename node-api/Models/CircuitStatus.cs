using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// 3.3.2. Circuit Status Report
/// This report is sent at regular intervals during the lifetime of the circuit,
/// to convey additional information about the performance of the circuit.
/// The interval between status reports is currently 5 minutes.
/// </summary>
public record CircuitStatus : UdpNodeInfoJsonDatagram
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
    /// Segments sent since circuit creation (Required)
    /// </summary>
    [JsonPropertyName("segsSent")]
    public required int SegmentsSent { get; init; }

    /// <summary>
    /// Segments received since circuit creation (Required)
    /// </summary>
    [JsonPropertyName("segsRcvd")]
    public required int SegmentsReceived { get; init; }

    /// <summary>
    /// Segments resent since circuit creation (Required)
    /// </summary>
    [JsonPropertyName("segsResent")]
    public required int SegmentsResent { get; init; }

    /// <summary>
    /// Current TX queue length (Required)
    /// </summary>
    [JsonPropertyName("segsQueued")]
    public required int SegmentsQueued { get; init; }

    /// <summary>
    /// Total bytes sent since circuit creation (Optional)
    /// </summary>
    [JsonPropertyName("bytesSent")]
    public int? BytesSent { get; init; }

    /// <summary>
    /// Total bytes received since circuit creation (Optional)
    /// </summary>
    [JsonPropertyName("bytesRcvd")]
    public int? BytesReceived { get; init; }
}
