using System.Text.Json.Serialization;

namespace node_api.Models;

// 3.7 Circuit Connection Event
public record CircuitUpEvent : UdpNodeInfoJsonDatagram
{
    [JsonPropertyName("node")]
    public required string Node { get; init; }

    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("direction")]
    public required string Direction { get; init; }

    [JsonPropertyName("service")]
    public int? Service { get; init; }

    [JsonPropertyName("remote")]
    public required string Remote { get; init; }

    [JsonPropertyName("local")]
    public required string Local { get; init; }
}
