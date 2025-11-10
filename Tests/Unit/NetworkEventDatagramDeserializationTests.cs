using System.Text.Json;
using node_api.Models;
using Xunit;

namespace Tests.Unit;

/// <summary>
/// Unit tests for NetworkEventDatagram polymorphic JSON deserialization.
/// These tests verify that System.Text.Json correctly uses the [JsonPolymorphic] attributes
/// to deserialize JSON strings to the appropriate derived types based on the @type discriminator.
/// </summary>
public class NetworkEventDatagramDeserializationTests
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Deserialize_NodeUpEvent_ViaDiscriminator()
    {
        // Arrange
        var json = """
        {
            "@type": "NodeUpEvent",
            "nodeCall": "TEST-1",
            "nodeAlias": "TEST1",
            "locator": "IO91EC",
            "software": "test",
            "version": "v1"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<NetworkEventDatagram>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<NodeUpEvent>(result);
        var nodeUpEvent = (NodeUpEvent)result;
        Assert.Equal("NodeUpEvent", nodeUpEvent.DatagramType);
        Assert.Equal("TEST-1", nodeUpEvent.NodeCall);
    }

    [Fact]
    public void Deserialize_NodeStatus_ViaDiscriminator()
    {
        // Arrange
        var json = """
        {
            "@type": "NodeStatus",
            "nodeCall": "TEST-2",
            "nodeAlias": "TEST2",
            "locator": "IO91EC",
            "software": "test",
            "version": "v1",
            "uptimeSecs": 3600
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<NetworkEventDatagram>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<NodeStatusReportEvent>(result);
        var statusEvent = (NodeStatusReportEvent)result;
        Assert.Equal("NodeStatus", statusEvent.DatagramType);
        Assert.Equal(3600, statusEvent.UptimeSecs);
    }

    [Fact]
    public void Deserialize_NodeDownEvent_ViaDiscriminator()
    {
        // Arrange
        var json = """
        {
            "@type": "NodeDownEvent",
            "nodeCall": "TEST-3",
            "nodeAlias": "TEST3",
            "reason": "shutdown"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<NetworkEventDatagram>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<NodeDownEvent>(result);
        var downEvent = (NodeDownEvent)result;
        Assert.Equal("NodeDownEvent", downEvent.DatagramType);
        Assert.Equal("shutdown", downEvent.Reason);
    }

    [Fact]
    public void Deserialize_LinkUpEvent_ViaDiscriminator()
    {
        // Arrange
        var json = """
        {
            "@type": "LinkUpEvent",
            "node": "TEST-4",
            "id": 1,
            "direction": "outgoing",
            "port": "1",
            "local": "TEST-4",
            "remote": "TEST-5"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<NetworkEventDatagram>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<LinkUpEvent>(result);
        var linkUpEvent = (LinkUpEvent)result;
        Assert.Equal("LinkUpEvent", linkUpEvent.DatagramType);
        Assert.Equal("TEST-4", linkUpEvent.Node);
    }

    [Fact]
    public void Deserialize_L2Trace_ViaDiscriminator()
    {
        // Arrange
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "TEST-6",
            "port": "1",
            "srce": "TEST-6",
            "dest": "TEST-7",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<NetworkEventDatagram>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<L2Trace>(result);
        var l2Trace = (L2Trace)result;
        Assert.Equal("L2Trace", l2Trace.DatagramType);
        Assert.Equal("TEST-6", l2Trace.ReportFrom);
    }

    [Fact]
    public void Deserialize_UnknownType_ReturnsNull()
    {
        // Arrange
        var json = """
        {
            "@type": "UnknownType",
            "someField": "someValue"
        }
        """;

        // Act & Assert
        Assert.Throws<JsonException>(() => 
            JsonSerializer.Deserialize<NetworkEventDatagram>(json, _options));
    }

    [Fact]
    public void Deserialize_MissingTypeField_DeserializesToBaseType()
    {
        // Arrange
        var json = """
        {
            "nodeCall": "TEST-1",
            "nodeAlias": "TEST1"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<NetworkEventDatagram>(json, _options);

        // Assert
        Assert.NotNull(result);
        // Without @type, it deserializes to the base NetworkEventDatagram type
        Assert.Equal(typeof(NetworkEventDatagram), result.GetType());
        // DatagramType will be empty string since there's no matching derived type
        Assert.Equal(string.Empty, result.DatagramType);
    }

    [Fact]
    public void Deserialize_CircuitUpEvent_ViaDiscriminator()
    {
        // Arrange
        var json = """
        {
            "@type": "CircuitUpEvent",
            "node": "TEST-8",
            "id": 1,
            "direction": "incoming",
            "remote": "TEST-9@TEST-9:1234",
            "local": "TEST-8:5678"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<NetworkEventDatagram>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<CircuitUpEvent>(result);
        var circuitUpEvent = (CircuitUpEvent)result;
        Assert.Equal("CircuitUpEvent", circuitUpEvent.DatagramType);
        Assert.Equal("TEST-8", circuitUpEvent.Node);
    }

    [Fact]
    public void Serialize_Then_Deserialize_RoundTrip()
    {
        // Arrange
        var original = new NodeUpEvent
        {
            DatagramType = "NodeUpEvent",
            NodeCall = "ROUNDTRIP-1",
            NodeAlias = "RT1",
            Locator = "IO91EC",
            Software = "test",
            Version = "v1",
            Latitude = 51.5074m,
            Longitude = -0.1278m
        };

        // Act - Serialize to JSON
        var json = JsonSerializer.Serialize<NetworkEventDatagram>(original, _options);
        
        // Act - Deserialize back
        var result = JsonSerializer.Deserialize<NetworkEventDatagram>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<NodeUpEvent>(result);
        var deserialized = (NodeUpEvent)result;
        Assert.Equal(original.NodeCall, deserialized.NodeCall);
        Assert.Equal(original.NodeAlias, deserialized.NodeAlias);
        Assert.Equal(original.Locator, deserialized.Locator);
    }
}
