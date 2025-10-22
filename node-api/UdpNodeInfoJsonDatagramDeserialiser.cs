using node_api.Models;
using node_api.Constants;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace node_api;

public static class UdpNodeInfoJsonDatagramDeserialiser
{
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static bool TryDeserialise(string json, out UdpNodeInfoJsonDatagram? frame, out JsonException? jsonException)
    {
        string? typeString;
        try
        {
            typeString = JsonNode.Parse(json)?["@type"]?.GetValue<string>();
        }
        catch (JsonException ex)
        {
            //throw;
            frame = null;
            jsonException = ex;
            return false;
        }

        try
        {
            frame = typeString switch
            {
                DatagramTypes.L2Trace => JsonSerializer.Deserialize<L2Trace>(json, options),
                DatagramTypes.NodeUpEvent => JsonSerializer.Deserialize<NodeUpEvent>(json, options),
                DatagramTypes.NodeDownEvent => JsonSerializer.Deserialize<NodeDownEvent>(json, options),
                DatagramTypes.NodeStatus => JsonSerializer.Deserialize<NodeStatusReportEvent>(json, options),
                DatagramTypes.LinkUpEvent => JsonSerializer.Deserialize<LinkUpEvent>(json, options),
                DatagramTypes.LinkDownEvent => JsonSerializer.Deserialize<LinkDisconnectionEvent>(json, options),
                DatagramTypes.LinkStatus => JsonSerializer.Deserialize<LinkStatus>(json, options),
                DatagramTypes.CircuitUpEvent => JsonSerializer.Deserialize<CircuitUpEvent>(json, options),
                DatagramTypes.CircuitDownEvent => JsonSerializer.Deserialize<CircuitDisconnectionEvent>(json, options),
                DatagramTypes.CircuitStatus => JsonSerializer.Deserialize<CircuitStatus>(json, options),
                _ => null
            };
            jsonException = null;
            return frame is not null;
        }
        catch (JsonException ex)
        {
            frame = null;
            jsonException = ex;
            return false;
        }
    }
}
