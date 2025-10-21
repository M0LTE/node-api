using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// 3.2.2. Link Status Report
/// This report is sent at regular intervals during the lifetime of the connection,
/// to convey additional information about the performance of the link.
/// The interval is currently 5 minutes.
/// </summary>
public record LinkStatus : UdpNodeInfoJsonDatagram
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
    /// "Incoming" or "outgoing" (Required)
    /// </summary>
    [JsonPropertyName("direction")]
    public required string Direction { get; init; }

    /// <summary>
    /// Port identifier, e.g. "2" (Required)
    /// </summary>
    [JsonPropertyName("port")]
    public required string Port { get; init; }

    /// <summary>
    /// Remote callsign, e.g. "G8PZT-2" (Required)
    /// </summary>
    [JsonPropertyName("remote")]
    public required string Remote { get; init; }

    /// <summary>
    /// Local callsign (Required)
    /// </summary>
    [JsonPropertyName("local")]
    public required string Local { get; init; }

    /// <summary>
    /// Link uptime in seconds (Required)
    /// </summary>
    [JsonPropertyName("upForSecs")]
    public required int UpForSecs { get; init; }

    /// <summary>
    /// Total frames sent since link creation (Required)
    /// </summary>
    [JsonPropertyName("frmsSent")]
    public required int FramesSent { get; init; }

    /// <summary>
    /// Total frames rcvd since link creation (Required)
    /// </summary>
    [JsonPropertyName("frmsRcvd")]
    public required int FramesReceived { get; init; }

    /// <summary>
    /// Total frames resent since link creation (Required)
    /// </summary>
    [JsonPropertyName("frmsResent")]
    public required int FramesResent { get; init; }

    /// <summary>
    /// Current TX queue length (frames) (Required)
    /// </summary>
    [JsonPropertyName("frmsQueued")]
    public required int FramesQueued { get; init; }

    /// <summary>
    /// Peak TX queue length (frames) (Optional)
    /// </summary>
    [JsonPropertyName("frmsQdPeak")]
    public int? FramesQueuedPeak { get; init; }

    /// <summary>
    /// Info bytes sent since link creation (Optional)
    /// </summary>
    [JsonPropertyName("bytesSent")]
    public int? BytesSent { get; init; }

    /// <summary>
    /// Info bytes rcvd since link creation (Optional)
    /// </summary>
    [JsonPropertyName("bytesRcvd")]
    public int? BytesReceived { get; init; }

    /// <summary>
    /// Ave TX bytes/sec since last status (Optional)
    /// </summary>
    [JsonPropertyName("bpsTxMean")]
    public int? BpsTxMean { get; init; }

    /// <summary>
    /// Ave RX bytes/sec since last status (Optional)
    /// </summary>
    [JsonPropertyName("bpsRxMean")]
    public int? BpsRxMean { get; init; }

    /// <summary>
    /// Max TX queue length since last status (Optional)
    /// </summary>
    [JsonPropertyName("frmQMax")]
    public int? FrmQMax { get; init; }

    /// <summary>
    /// Average Round Trip Time in millisecs (Optional)
    /// </summary>
    [JsonPropertyName("l2rttMs")]
    public int? L2RttMs { get; init; }
}
