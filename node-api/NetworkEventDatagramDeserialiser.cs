using node_api.Models;
using node_api.Constants;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace node_api;

/// <summary>
/// Deserializes JSON strings into strongly-typed NetworkEventDatagram objects.
/// Supports multiple transport protocols (UDP, HTTP, etc.)
/// </summary>
public static class NetworkEventDatagramDeserialiser
{
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static bool TryDeserialise(string json, out NetworkEventDatagram? frame, out JsonException? jsonException)
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
            // Preprocess L2Trace JSON to strip invalid tseq field from supervisory frames
            if (typeString == DatagramTypes.L2Trace)
            {
                json = StripInvalidTseqFromSupervisoryFrames(json);
            }

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

    /// <summary>
    /// Strips the tseq (transmit sequence) field from supervisory frames (RR, RNR, REJ, SREJ)
    /// which do not have transmit sequence numbers according to the AX.25 protocol specification.
    /// </summary>
    private static string StripInvalidTseqFromSupervisoryFrames(string json)
    {
        var supervisoryFrames = new[] { "RR", "RNR", "REJ", "SREJ" };
        
        try
        {
            var jsonNode = JsonNode.Parse(json);
            if (jsonNode is JsonObject jsonObject)
            {
                // Check if this is a supervisory frame
                var l2Type = jsonObject["l2Type"]?.GetValue<string>() ?? jsonObject["l2type"]?.GetValue<string>();
                
                if (l2Type != null && supervisoryFrames.Contains(l2Type, StringComparer.OrdinalIgnoreCase))
                {
                    // Remove tseq if present
                    if (jsonObject.ContainsKey("tseq"))
                    {
                        jsonObject.Remove("tseq");
                        return jsonObject.ToJsonString();
                    }
                }
            }
        }
        catch
        {
            // If preprocessing fails, return original JSON
        }

        return json;
    }
}
