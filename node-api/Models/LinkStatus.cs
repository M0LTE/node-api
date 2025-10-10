using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// 3.6. Link Status Report
/// This report is sent at regular intervals during the lifetime of the connection,
/// to convey additional information about the performance of the link.
/// </summary>
public record LinkStatus : UdpNodeInfoJsonDatagram
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
}
