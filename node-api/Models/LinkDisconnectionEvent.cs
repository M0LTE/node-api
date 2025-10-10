using System.Text.Json.Serialization;

namespace node_api.Models;

// 3.5 Link Disconnection Event
public record LinkDisconnectionEvent : UdpNodeInfoJsonDatagram
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

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}
