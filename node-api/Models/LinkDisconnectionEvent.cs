using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// 3.2.3. Link Disconnection Event
/// This report is sent when an AX25 connection is destroyed.
/// For the "direction" field, "incoming" indicates that the disconnection was initiated from the remote end,
/// and "outgoing" means that it was initiated from the local end.
/// </summary>
public record LinkDisconnectionEvent : UdpNodeInfoJsonDatagram
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
    /// Link uptime in seconds (Optional)
    /// </summary>
    [JsonPropertyName("upForSecs")]
    public int? UpForSecs { get; init; }

    /// <summary>
    /// Total frames sent since link creation (Required)
    /// </summary>
    [JsonPropertyName("frmsSent")]
    public required int FramesSent { get; init; }

    /// <summary>
    /// Total frames received since link creation (Required)
    /// </summary>
    [JsonPropertyName("frmsRcvd")]
    public required int FramesReceived { get; init; }

    /// <summary>
    /// Total frames resent (Required)
    /// </summary>
    [JsonPropertyName("frmsResent")]
    public required int FramesResent { get; init; }

    /// <summary>
    /// Current TX queue length (Required)
    /// </summary>
    [JsonPropertyName("frmsQueued")]
    public required int FramesQueued { get; init; }

    /// <summary>
    /// Peak TX queue length (frames) over lifetime of connection (Optional)
    /// </summary>
    [JsonPropertyName("frmsQdPeak")]
    public int? FramesQueuedPeak { get; init; }

    /// <summary>
    /// Total bytes sent since link creation (Optional)
    /// </summary>
    [JsonPropertyName("bytesSent")]
    public int? BytesSent { get; init; }

    /// <summary>
    /// Total bytes received since link creation (Optional)
    /// </summary>
    [JsonPropertyName("bytesRcvd")]
    public int? BytesReceived { get; init; }

    /// <summary>
    /// Reason for disconnect, e.g. "Retried out" (Optional)
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}
