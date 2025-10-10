using System.Text.Json.Serialization;

namespace node_api.Models;

// 3.6 Link Status Report
public record LinkStatus : UdpNodeInfoJsonDatagram
{
    [JsonPropertyName("node")]
    public required string Node { get; init; }

    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("direction")]
    public required string Direction { get; init; }

    [JsonPropertyName("port")]
    public required string Port { get; init; }

    [JsonPropertyName("remote")]
    public required string Remote { get; init; }

    [JsonPropertyName("local")]
    public required string Local { get; init; }

    [JsonPropertyName("frmsSent")]
    public required int FramesSent { get; init; }

    [JsonPropertyName("frmsRcvd")]
    public required int FramesReceived { get; init; }

    [JsonPropertyName("frmsResent")]
    public required int FramesResent { get; init; }

    [JsonPropertyName("frmsQueued")]
    public required int FramesQueued { get; init; }
}
