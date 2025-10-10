using System.Text.Json.Serialization;

namespace node_api.Models;

public record UdpNodeInfoJsonDatagram
{
    [JsonPropertyName("@type")]
    public required string DatagramType { get; init; }
}
