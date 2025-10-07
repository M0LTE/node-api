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

    public static bool TryDeserialise(string json, out UdpNodeInfoJsonDatagram? frame)
    {
        try
        {
            var typeString = JsonNode.Parse(json)?["@type"]?.GetValue<string>();
            
            frame = typeString switch
            {
                "l2trace" => JsonSerializer.Deserialize<L2Trace>(json, options),
                "event" => JsonSerializer.Deserialize<Event>(json, options),
                _ => null
            };

            return frame is not null;
        }
        catch (JsonException)
        {
            frame = null;
            return false;
        }
    }
}
