using node_api.Models;
using node_api.Utilities;

namespace Tests;

public class JsonPropertyNameMapperTests
{
    [Fact]
    public void Should_Map_Simple_Property_Name()
    {
        // C# property "Node" -> JSON property "node"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(LinkUpEvent), "Node");
        Assert.Equal("node", result);
    }

    [Fact]
    public void Should_Map_Property_With_Different_Name()
    {
        // C# property "TimeUnixSeconds" -> JSON property "time"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(LinkUpEvent), "TimeUnixSeconds");
        Assert.Equal("time", result);
    }

    [Fact]
    public void Should_Map_Property_On_L2Trace()
    {
        // C# property "ReportFrom" -> JSON property "reportFrom"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(L2Trace), "ReportFrom");
        Assert.Equal("reportFrom", result);
    }

    [Fact]
    public void Should_Map_Control_Property()
    {
        // C# property "Control" -> JSON property "ctrl"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(L2Trace), "Control");
        Assert.Equal("ctrl", result);
    }

    [Fact]
    public void Should_Map_Source_Property()
    {
        // C# property "Source" -> JSON property "srce"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(L2Trace), "Source");
        Assert.Equal("srce", result);
    }

    [Fact]
    public void Should_Map_Destination_Property()
    {
        // C# property "Destination" -> JSON property "dest"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(L2Trace), "Destination");
        Assert.Equal("dest", result);
    }

    [Fact]
    public void Should_Map_CommandResponse_Property()
    {
        // C# property "CommandResponse" -> JSON property "cr"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(L2Trace), "CommandResponse");
        Assert.Equal("cr", result);
    }

    [Fact]
    public void Should_Map_L2Type_Property()
    {
        // C# property "L2Type" -> JSON property "l2Type"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(L2Trace), "L2Type");
        Assert.Equal("l2Type", result);
    }

    [Fact]
    public void Should_Map_Nested_Property_With_Indexer()
    {
        // C# property "Nodes[0].Callsign" -> JSON property "nodes[0].call"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(L2Trace), "Nodes[0].Callsign");
        Assert.Equal("nodes[0].call", result);
    }

    [Fact]
    public void Should_Map_Digipeater_Property()
    {
        // C# property "Digipeaters[0].Callsign" -> JSON property "digis[0].call"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(L2Trace), "Digipeaters[0].Callsign");
        Assert.Equal("digis[0].call", result);
    }

    [Fact]
    public void Should_Map_Direction_Property()
    {
        // C# property "Direction" -> JSON property "direction"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(LinkUpEvent), "Direction");
        Assert.Equal("direction", result);
    }

    [Fact]
    public void Should_Return_Original_For_Unknown_Property()
    {
        // Unknown property should return original
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(LinkUpEvent), "UnknownProperty");
        Assert.Equal("UnknownProperty", result);
    }

    [Fact]
    public void Should_Handle_Empty_String()
    {
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(LinkUpEvent), "");
        Assert.Equal("", result);
    }

    [Fact]
    public void Should_Handle_Null_String()
    {
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(LinkUpEvent), null!);
        Assert.Null(result);
    }

    [Fact]
    public void Should_Cache_Results_For_Performance()
    {
        // Clear cache first
        JsonPropertyNameMapper.ClearCache();

        // First call
        var result1 = JsonPropertyNameMapper.GetJsonPropertyName(typeof(LinkUpEvent), "Node");
        
        // Second call should use cache
        var result2 = JsonPropertyNameMapper.GetJsonPropertyName(typeof(LinkUpEvent), "Node");
        
        Assert.Equal(result1, result2);
        Assert.Equal("node", result1);
    }

    [Fact]
    public void Should_Map_NodeCall_Property()
    {
        // C# property "NodeCall" -> JSON property "nodeCall"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(NodeUpEvent), "NodeCall");
        Assert.Equal("nodeCall", result);
    }

    [Fact]
    public void Should_Map_NodeAlias_Property()
    {
        // C# property "NodeAlias" -> JSON property "nodeAlias"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(NodeUpEvent), "NodeAlias");
        Assert.Equal("nodeAlias", result);
    }

    [Fact]
    public void Should_Map_FramesSent_Property()
    {
        // C# property "FramesSent" -> JSON property "frmsSent"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(LinkStatus), "FramesSent");
        Assert.Equal("frmsSent", result);
    }

    [Fact]
    public void Should_Map_FramesReceived_Property()
    {
        // C# property "FramesReceived" -> JSON property "frmsRcvd"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(LinkStatus), "FramesReceived");
        Assert.Equal("frmsRcvd", result);
    }

    [Fact]
    public void Should_Map_SegmentsSent_Property()
    {
        // C# property "SegmentsSent" -> JSON property "segsSent"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(CircuitStatus), "SegmentsSent");
        Assert.Equal("segsSent", result);
    }

    [Fact]
    public void Should_Map_SegmentsReceived_Property()
    {
        // C# property "SegmentsReceived" -> JSON property "segsRcvd"
        var result = JsonPropertyNameMapper.GetJsonPropertyName(typeof(CircuitStatus), "SegmentsReceived");
        Assert.Equal("segsRcvd", result);
    }

    [Fact]
    public void Should_Be_Case_Insensitive()
    {
        // Should work with different casing
        var result1 = JsonPropertyNameMapper.GetJsonPropertyName(typeof(LinkUpEvent), "node");
        var result2 = JsonPropertyNameMapper.GetJsonPropertyName(typeof(LinkUpEvent), "NODE");
        var result3 = JsonPropertyNameMapper.GetJsonPropertyName(typeof(LinkUpEvent), "Node");
        
        Assert.Equal("node", result1);
        Assert.Equal("node", result2);
        Assert.Equal("node", result3);
    }

    [Fact]
    public void Should_Map_NetRom_Properties()
    {
        // L3Source -> l3src
        var result1 = JsonPropertyNameMapper.GetJsonPropertyName(typeof(L2Trace), "L3Source");
        Assert.Equal("l3src", result1);

        // L3Destination -> l3dst
        var result2 = JsonPropertyNameMapper.GetJsonPropertyName(typeof(L2Trace), "L3Destination");
        Assert.Equal("l3dst", result2);

        // TimeToLive -> ttl
        var result3 = JsonPropertyNameMapper.GetJsonPropertyName(typeof(L2Trace), "TimeToLive");
        Assert.Equal("ttl", result3);

        // L4Type -> l4type
        var result4 = JsonPropertyNameMapper.GetJsonPropertyName(typeof(L2Trace), "L4Type");
        Assert.Equal("l4type", result4);
    }

    [Fact]
    public void Should_Map_Routing_Properties()
    {
        // FromAlias -> fromAlias
        var result1 = JsonPropertyNameMapper.GetJsonPropertyName(typeof(L2Trace), "FromAlias");
        Assert.Equal("fromAlias", result1);

        // OriginatingUserCallsign -> srcUser
        var result2 = JsonPropertyNameMapper.GetJsonPropertyName(typeof(L2Trace), "OriginatingUserCallsign");
        Assert.Equal("srcUser", result2);

        // OriginatingNodeCallsign -> srcNode
        var result3 = JsonPropertyNameMapper.GetJsonPropertyName(typeof(L2Trace), "OriginatingNodeCallsign");
        Assert.Equal("srcNode", result3);
    }

    #region TransformErrorMessage Tests

    [Fact]
    public void TransformErrorMessage_Should_Replace_CSharp_Property_Names_With_JSON_Names()
    {
        // Arrange
        var errorMessage = "OriginatingUserCallsign is required for CONN REQ/CONN REQX";
        var type = typeof(L2Trace);

        // Act
        var result = JsonPropertyNameMapper.TransformErrorMessage(type, errorMessage);

        // Assert
        Assert.Equal("srcUser is required for CONN REQ/CONN REQX", result);
    }

    [Fact]
    public void TransformErrorMessage_Should_Replace_Multiple_Property_Names()
    {
        // Arrange
        var errorMessage = "OriginatingUserCallsign and OriginatingNodeCallsign are required";
        var type = typeof(L2Trace);

        // Act
        var result = JsonPropertyNameMapper.TransformErrorMessage(type, errorMessage);

        // Assert
        Assert.Equal("srcUser and srcNode are required", result);
    }

    [Fact]
    public void TransformErrorMessage_Should_Handle_FromCircuit_And_ToCircuit()
    {
        // Arrange
        var errorMessage = "FromCircuit is required for CONN REQ";
        var type = typeof(L2Trace);

        // Act
        var result = JsonPropertyNameMapper.TransformErrorMessage(type, errorMessage);

        // Assert
        Assert.Equal("fromCct is required for CONN REQ", result);
    }

    [Fact]
    public void TransformErrorMessage_Should_Handle_Sequence_Numbers()
    {
        // Arrange
        var errorMessage = "TransmitSequenceNumber and ReceiveSequenceNumber are required";
        var type = typeof(L2Trace);

        // Act
        var result = JsonPropertyNameMapper.TransformErrorMessage(type, errorMessage);

        // Assert
        Assert.Equal("txSeq and rxSeq are required", result);
    }

    [Fact]
    public void TransformErrorMessage_Should_Handle_Window_Properties()
    {
        // Arrange
        var errorMessage = "ProposedWindow and AcceptableWindow are required";
        var type = typeof(L2Trace);

        // Act
        var result = JsonPropertyNameMapper.TransformErrorMessage(type, errorMessage);

        // Assert
        Assert.Equal("window and accWin are required", result);
    }

    [Fact]
    public void TransformErrorMessage_Should_Handle_NetRomXServiceNumber()
    {
        // Arrange
        var errorMessage = "NetRomXServiceNumber is required for CONN REQX";
        var type = typeof(L2Trace);

        // Act
        var result = JsonPropertyNameMapper.TransformErrorMessage(type, errorMessage);

        // Assert
        Assert.Equal("service is required for CONN REQX", result);
    }

    [Fact]
    public void TransformErrorMessage_Should_Handle_L2Trace_Basic_Properties()
    {
        // Arrange
        var errorMessage = "Source and Destination callsigns are required";
        var type = typeof(L2Trace);

        // Act
        var result = JsonPropertyNameMapper.TransformErrorMessage(type, errorMessage);

        // Assert
        Assert.Equal("srce and dest callsigns are required", result);
    }

    [Fact]
    public void TransformErrorMessage_Should_Handle_CircuitStatus_Properties()
    {
        // Arrange
        var errorMessage = "SegmentsSent and SegmentsReceived cannot be negative";
        var type = typeof(CircuitStatus);

        // Act
        var result = JsonPropertyNameMapper.TransformErrorMessage(type, errorMessage);

        // Assert
        Assert.Equal("segsSent and segsRcvd cannot be negative", result);
    }

    [Fact]
    public void TransformErrorMessage_Should_Handle_LinkStatus_Properties()
    {
        // Arrange
        var errorMessage = "FramesSent and FramesReceived cannot be negative";
        var type = typeof(LinkStatus);

        // Act
        var result = JsonPropertyNameMapper.TransformErrorMessage(type, errorMessage);

        // Assert
        Assert.Equal("frmsSent and frmsRcvd cannot be negative", result);
    }

    [Fact]
    public void TransformErrorMessage_Should_Return_Original_For_Null_Or_Empty()
    {
        // Arrange
        var type = typeof(L2Trace);

        // Act & Assert
        Assert.Equal("", JsonPropertyNameMapper.TransformErrorMessage(type, ""));
        Assert.Null(JsonPropertyNameMapper.TransformErrorMessage(type, null!));
    }

    [Fact]
    public void TransformErrorMessage_Should_Use_Word_Boundaries()
    {
        // Arrange - "Window" in "ProposedWindow" should be replaced, but "window" as a standalone word should be preserved
        var errorMessage = "ProposedWindow defines the window size";
        var type = typeof(L2Trace);

        // Act
        var result = JsonPropertyNameMapper.TransformErrorMessage(type, errorMessage);

        // Assert
        Assert.Contains("window defines the window size", result);
    }

    #endregion
}
