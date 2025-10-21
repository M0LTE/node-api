using System.Text.Json;
using System.Text.Json.Serialization;

namespace node_api.Converters;

/// <summary>
/// Converts timestamp values that can be either ISO 8601 strings or Unix timestamp integers.
/// The specification (v0.8a) states that timestamp should be an integer (Unix seconds since 1/1/70),
/// but some nodes send ISO 8601 strings. This converter handles both formats.
/// </summary>
public class FlexibleTimestampConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.Number:
                // Standard case: Unix timestamp as integer
                return reader.GetInt64();

            case JsonTokenType.String:
                // Non-standard case: ISO 8601 string
                var stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null;
                }

                // Try to parse as ISO 8601 DateTime and convert to Unix timestamp
                if (DateTime.TryParse(stringValue, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dateTime))
                {
                    // Convert to Unix timestamp
                    var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    return (long)(dateTime.ToUniversalTime() - unixEpoch).TotalSeconds;
                }

                throw new JsonException($"Unable to parse '{stringValue}' as a timestamp");

            default:
                throw new JsonException($"Unexpected token type {reader.TokenType} when parsing timestamp");
        }
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
