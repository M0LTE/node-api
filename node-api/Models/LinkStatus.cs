using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// 3.6. Link Status Report
/// This report is sent at regular intervals during the lifetime of the connection,
/// to convey additional information about the performance of the link.
/// </summary>
public record LinkStatus : UdpNodeInfoJsonDatagram
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
    /// "incoming" or "outgoing" (Required)
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
    /// Total frames re-sent (Required)
    /// </summary>
    [JsonPropertyName("frmsResent")]
    public required int FramesResent { get; init; }

    /// <summary>
    /// Current TX queue length (Required)
    /// </summary>
    [JsonPropertyName("frmsQueued")]
    public required int FramesQueued { get; init; }

    /// <summary>
    /// Peak TX queue length (frames)
    /// </summary>
    [JsonPropertyName("frmsQdPeak")]
    public int? FramesQueuedPeak { get; init; }

    [JsonPropertyName("bytesSent")]
    public int? BytesSent { get; init; }

    [JsonPropertyName("bytesRcvd")]
    public int? BytesReceived { get; init; }

    /*
      "bpsTxMean"   N  Integer  Ave TX bytes/sec since last status
      "bpsRxMean"   N  Integer  Ave RX bytes/sec since last status
      "frmQMax"     N  Integer  Max TX queue length since last status
      "l2rttMs"     N  Integer  Average Round Trip Time in millisecs
     */

    [JsonPropertyName("bpsTxMean")]
    public int? BpsTxMean { get; init; }

    [JsonPropertyName("bpsRxMean")]
    public int? BpsRxMean { get; init; }

    [JsonPropertyName("frmQMax")]
    public int? FrmQMax { get; init; }

    [JsonPropertyName("l2rttMs")]
    public int? L2RttMs { get; init; }
}
