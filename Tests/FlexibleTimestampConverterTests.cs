using node_api.Converters;
using System.Text.Json;

namespace Tests;

public class FlexibleTimestampConverterTests
{
    private readonly JsonSerializerOptions _options;

    public FlexibleTimestampConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new FlexibleTimestampConverter() }
        };
    }

    [Fact]
    public void Should_Deserialize_Integer_Timestamp()
    {
        var json = "1728270184";
        var result = JsonSerializer.Deserialize<long?>(json, _options);
        
        Assert.Equal(1728270184, result);
    }

    [Fact]
    public void Should_Deserialize_Null_Timestamp()
    {
        var json = "null";
        var result = JsonSerializer.Deserialize<long?>(json, _options);
        
        Assert.Null(result);
    }

    [Fact]
    public void Should_Deserialize_ISO8601_String_Timestamp()
    {
        // "2025-10-22T11:57:40Z" should be converted to Unix timestamp
        var json = "\"2025-10-22T11:57:40Z\"";
        var result = JsonSerializer.Deserialize<long?>(json, _options);
        
        // Expected Unix timestamp for 2025-10-22T11:57:40Z
        var expectedDate = new DateTime(2025, 10, 22, 11, 57, 40, DateTimeKind.Utc);
        var expectedTimestamp = ((DateTimeOffset)expectedDate).ToUnixTimeSeconds();
        
        Assert.Equal(expectedTimestamp, result);
    }

    [Fact]
    public void Should_Deserialize_ISO8601_String_With_Milliseconds()
    {
        var json = "\"2025-10-22T11:57:40.123Z\"";
        var result = JsonSerializer.Deserialize<long?>(json, _options);
        
        // Should truncate to seconds
        var expectedDate = new DateTime(2025, 10, 22, 11, 57, 40, DateTimeKind.Utc);
        var expectedTimestamp = ((DateTimeOffset)expectedDate).ToUnixTimeSeconds();
        
        Assert.Equal(expectedTimestamp, result);
    }

    [Fact]
    public void Should_Deserialize_Empty_String_As_Null()
    {
        var json = "\"\"";
        var result = JsonSerializer.Deserialize<long?>(json, _options);
        
        Assert.Null(result);
    }

    [Fact]
    public void Should_Deserialize_Whitespace_String_As_Null()
    {
        var json = "\"   \"";
        var result = JsonSerializer.Deserialize<long?>(json, _options);
        
        Assert.Null(result);
    }

    [Fact]
    public void Should_Throw_On_Invalid_String_Format()
    {
        var json = "\"not-a-date\"";
        
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<long?>(json, _options));
    }

    [Fact]
    public void Should_Serialize_To_Integer()
    {
        long? value = 1728270184;
        var json = JsonSerializer.Serialize(value, _options);
        
        Assert.Equal("1728270184", json);
    }

    [Fact]
    public void Should_Serialize_Null_To_Null()
    {
        long? value = null;
        var json = JsonSerializer.Serialize(value, _options);
        
        Assert.Equal("null", json);
    }

    [Fact]
    public void Should_Handle_Real_World_Example()
    {
        // From the error report - just a node object
        var json = """
        {
            "call": "ZL2BAU",
            "hops": 1,
            "tt": 1,
            "alias": "BAUBBS",
            "latitude": -44.4426,
            "longitude": 171.0322,
            "version": "504k",
            "timestamp": "2025-10-22T11:57:40Z",
            "tzMins": 780
        }
        """;

        var node = JsonSerializer.Deserialize<node_api.Models.L2Trace.Node>(json);
        
        Assert.NotNull(node);
        Assert.NotNull(node.Timestamp);
        
        // Verify it converted to a valid Unix timestamp
        var expectedDate = new DateTime(2025, 10, 22, 11, 57, 40, DateTimeKind.Utc);
        var expectedTimestamp = ((DateTimeOffset)expectedDate).ToUnixTimeSeconds();
        Assert.Equal(expectedTimestamp, node.Timestamp);
    }
}
