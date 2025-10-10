using node_api.Models;
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
                "L2Trace" => JsonSerializer.Deserialize<L2Trace>(json, options),
                "NodeUpEvent" => JsonSerializer.Deserialize<NodeUpEvent>(json, options),
                "NodeDownEvent" => JsonSerializer.Deserialize<NodeDownEvent>(json, options),
                "NodeStatus" => JsonSerializer.Deserialize<NodeStatusReportEvent>(json, options),
                "LinkUpEvent" => JsonSerializer.Deserialize<LinkUpEvent>(json, options),
                "LinkDownEvent" => JsonSerializer.Deserialize<LinkDisconnectionEvent>(json, options),
                "LinkStatus" => JsonSerializer.Deserialize<LinkStatus>(json, options),
                "CircuitUpEvent" => JsonSerializer.Deserialize<CircuitUpEvent>(json, options),
                "CircuitDownEvent" => JsonSerializer.Deserialize<CircuitDisconnectionEvent>(json, options),
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
