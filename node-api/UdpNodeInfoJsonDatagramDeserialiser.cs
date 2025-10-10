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
            frame = null;
            jsonException = ex;
            return false;
        }

        try
        {
            frame = typeString switch
            {
                "L2Trace" => JsonSerializer.Deserialize<L2Trace>(json, options),
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
