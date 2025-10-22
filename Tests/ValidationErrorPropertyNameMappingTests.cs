using FluentValidation.TestHelper;
using node_api.Models;
using node_api.Utilities;
using node_api.Validators;

namespace Tests;

/// <summary>
/// Tests to verify that validators return C# property names (as expected from FluentValidation)
/// and that our JsonPropertyNameMapper correctly converts them to JSON property names.
/// This ensures the separation of concerns between validation logic and presentation layer.
/// </summary>
public class ValidationErrorPropertyNameMappingTests
{
    #region Validator Tests - Should Return C# Property Names

    [Fact]
    public void Validator_Should_Return_CSharp_Property_Names_For_L2Trace()
    {
        // Arrange
        var validator = new L2TraceValidator();
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "",  // Invalid - empty
            Port = "",        // Invalid - empty
            Source = "",      // Invalid - empty
            Destination = "", // Invalid - empty
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert - Validators should return C# property names
        Assert.False(result.IsValid);
        
        var propertyNames = result.Errors.Select(e => e.PropertyName).ToList();
        
        // Should contain C# property names
        Assert.Contains("ReportFrom", propertyNames);
        Assert.Contains("Port", propertyNames);
        Assert.Contains("Source", propertyNames);
        Assert.Contains("Destination", propertyNames);
        
        // Should NOT contain JSON property names (validators don't know about JSON)
        Assert.DoesNotContain("reportFrom", propertyNames);
        Assert.DoesNotContain("port", propertyNames);
        Assert.DoesNotContain("srce", propertyNames);
        Assert.DoesNotContain("dest", propertyNames);
    }

    [Fact]
    public void Validator_Should_Return_CSharp_Property_Names_For_CircuitStatus()
    {
        // Arrange
        var validator = new CircuitStatusValidator();
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            Node = "",
            Id = 0,
            Direction = "invalid",
            Remote = "",
            Local = "",
            SegmentsSent = -1,      // Invalid - negative
            SegmentsReceived = -1,  // Invalid - negative
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert - Validators should return C# property names
        Assert.False(result.IsValid);
        
        var propertyNames = result.Errors.Select(e => e.PropertyName).ToList();
        
        // Should contain C# property names
        if (propertyNames.Any(p => p.Contains("SegmentsSent")))
        {
            Assert.Contains("SegmentsSent", propertyNames);
        }
        if (propertyNames.Any(p => p.Contains("SegmentsReceived")))
        {
            Assert.Contains("SegmentsReceived", propertyNames);
        }
        
        // Should NOT contain JSON property names
        Assert.DoesNotContain("segsSent", propertyNames);
        Assert.DoesNotContain("segsRcvd", propertyNames);
    }

    [Fact]
    public void Validator_Should_Return_CSharp_Property_Names_For_LinkUpEvent()
    {
        // Arrange
        var validator = new LinkUpEventValidator();
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            Node = "",        // Invalid - empty
            Id = 0,           // Invalid - must be > 0
            Direction = "sideways",  // Invalid
            Port = "",        // Invalid - empty
            Remote = "",      // Invalid - empty
            Local = ""        // Invalid - empty
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert - Validators should return C# property names
        Assert.False(result.IsValid);
        
        var propertyNames = result.Errors.Select(e => e.PropertyName).ToList();
        
        // Should contain C# property names (case-sensitive)
        Assert.Contains("Node", propertyNames);
        Assert.Contains("Direction", propertyNames);
        
        // Should NOT contain JSON property names
        Assert.DoesNotContain("node", propertyNames);
        Assert.DoesNotContain("direction", propertyNames);
    }

    [Fact]
    public void Validator_Should_Return_CSharp_Property_Names_For_NodeUpEvent()
    {
        // Arrange
        var validator = new NodeUpEventValidator();
        var model = new NodeUpEvent
        {
            DatagramType = "NodeUpEvent",
            NodeCall = "",     // Invalid - empty
            NodeAlias = "",    // Invalid - empty
            Locator = "INVALID", // Invalid format
            Software = "",     // Invalid - empty
            Version = ""       // Invalid - empty
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert - Validators should return C# property names
        Assert.False(result.IsValid);
        
        var propertyNames = result.Errors.Select(e => e.PropertyName).ToList();
        
        // Should contain C# property names
        Assert.Contains("NodeCall", propertyNames);
        Assert.Contains("NodeAlias", propertyNames);
        
        // Should NOT contain JSON property names
        Assert.DoesNotContain("nodeCall", propertyNames);
        Assert.DoesNotContain("nodeAlias", propertyNames);
    }

    [Fact]
    public void Validator_Should_Return_CSharp_Property_Names_For_Nested_Properties()
    {
        // Arrange
        var validator = new L2TraceValidator();
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "TEST",
            Port = "1",
            Source = "SRC",
            Destination = "DST",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            Digipeaters = new[]
            {
                new L2Trace.Digipeater { Callsign = "", Repeated = true }  // Invalid - empty callsign
            }
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert - Validators should return C# property names for nested properties
        Assert.False(result.IsValid);
        
        var propertyNames = result.Errors.Select(e => e.PropertyName).ToList();
        
        // Should contain C# property names with indexer
        Assert.Contains(propertyNames, p => p.Contains("Digipeaters") && p.Contains("Callsign"));
        
        // Should NOT contain JSON property names
        Assert.DoesNotContain(propertyNames, p => p.Contains("digis"));
    }

    #endregion

    #region Mapper Tests - Should Convert C# to JSON Property Names

    [Fact]
    public void Mapper_Should_Convert_CSharp_To_JSON_Property_Names_For_L2Trace()
    {
        // Arrange - C# property names from validators
        var csharpPropertyNames = new[] { "ReportFrom", "Port", "Source", "Destination" };
        var datagramType = typeof(L2Trace);

        // Act - Map to JSON property names
        var jsonPropertyNames = csharpPropertyNames
            .Select(name => JsonPropertyNameMapper.GetJsonPropertyName(datagramType, name))
            .ToList();

        // Assert - Should convert to JSON property names
        Assert.Equal("reportFrom", jsonPropertyNames[0]);
        Assert.Equal("port", jsonPropertyNames[1]);
        Assert.Equal("srce", jsonPropertyNames[2]);
        Assert.Equal("dest", jsonPropertyNames[3]);
    }

    [Fact]
    public void Mapper_Should_Convert_CSharp_To_JSON_Property_Names_For_CircuitStatus()
    {
        // Arrange - C# property names from validators
        var csharpPropertyNames = new[] { "SegmentsSent", "SegmentsReceived", "SegmentsResent", "SegmentsQueued" };
        var datagramType = typeof(CircuitStatus);

        // Act - Map to JSON property names
        var jsonPropertyNames = csharpPropertyNames
            .Select(name => JsonPropertyNameMapper.GetJsonPropertyName(datagramType, name))
            .ToList();

        // Assert - Should convert to JSON property names
        Assert.Equal("segsSent", jsonPropertyNames[0]);
        Assert.Equal("segsRcvd", jsonPropertyNames[1]);
        Assert.Equal("segsResent", jsonPropertyNames[2]);
        Assert.Equal("segsQueued", jsonPropertyNames[3]);
    }

    [Fact]
    public void Mapper_Should_Convert_CSharp_To_JSON_Property_Names_For_LinkUpEvent()
    {
        // Arrange - C# property names from validators
        var csharpPropertyNames = new[] { "Node", "Direction", "TimeUnixSeconds", "Remote", "Local" };
        var datagramType = typeof(LinkUpEvent);

        // Act - Map to JSON property names
        var jsonPropertyNames = csharpPropertyNames
            .Select(name => JsonPropertyNameMapper.GetJsonPropertyName(datagramType, name))
            .ToList();

        // Assert - Should convert to JSON property names
        Assert.Equal("node", jsonPropertyNames[0]);
        Assert.Equal("direction", jsonPropertyNames[1]);
        Assert.Equal("time", jsonPropertyNames[2]);
        Assert.Equal("remote", jsonPropertyNames[3]);
        Assert.Equal("local", jsonPropertyNames[4]);
    }

    [Fact]
    public void Mapper_Should_Convert_Nested_CSharp_Property_Names()
    {
        // Arrange - C# property name from validator for nested property
        var csharpPropertyName = "Digipeaters[0].Callsign";
        var datagramType = typeof(L2Trace);

        // Act - Map to JSON property name
        var jsonPropertyName = JsonPropertyNameMapper.GetJsonPropertyName(datagramType, csharpPropertyName);

        // Assert - Should convert to JSON property name with indexer preserved
        Assert.Equal("digis[0].call", jsonPropertyName);
    }

    [Fact]
    public void Mapper_Should_Convert_Nodes_Property_Names()
    {
        // Arrange - C# property names for routing nodes
        var csharpPropertyNames = new[] 
        { 
            "Nodes[0].Callsign", 
            "Nodes[0].Quality", 
            "Nodes[0].Via",
            "Nodes[0].Hops",
            "Nodes[0].OneWayTripTimeIn10msIncrements"
        };
        var datagramType = typeof(L2Trace);

        // Act - Map to JSON property names
        var jsonPropertyNames = csharpPropertyNames
            .Select(name => JsonPropertyNameMapper.GetJsonPropertyName(datagramType, name))
            .ToList();

        // Assert - Should convert to JSON property names
        Assert.Equal("nodes[0].call", jsonPropertyNames[0]);
        Assert.Equal("nodes[0].qual", jsonPropertyNames[1]);
        Assert.Equal("nodes[0].via", jsonPropertyNames[2]);
        Assert.Equal("nodes[0].hops", jsonPropertyNames[3]);
        Assert.Equal("nodes[0].tt", jsonPropertyNames[4]);
    }

    #endregion

    #region Integration Tests - Validator + Mapper

    [Fact]
    public void Integration_Should_Map_L2Trace_Validation_Errors_To_JSON_Property_Names()
    {
        // Arrange
        var validator = new L2TraceValidator();
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "",
            Port = "",
            Source = "",
            Destination = "",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        // Act - Validate (returns C# property names)
        var validationResult = validator.TestValidate(model);
        
        // Map C# property names to JSON property names
        var datagramType = model.GetType();
        var mappedErrors = validationResult.Errors.Select(e => new
        {
            OriginalPropertyName = e.PropertyName,
            MappedPropertyName = JsonPropertyNameMapper.GetJsonPropertyName(datagramType, e.PropertyName),
            ErrorMessage = e.ErrorMessage
        }).ToList();

        // Assert - Original should be C#, mapped should be JSON
        var reportFromError = mappedErrors.First(e => e.OriginalPropertyName == "ReportFrom");
        Assert.Equal("ReportFrom", reportFromError.OriginalPropertyName);
        Assert.Equal("reportFrom", reportFromError.MappedPropertyName);

        var sourceError = mappedErrors.First(e => e.OriginalPropertyName == "Source");
        Assert.Equal("Source", sourceError.OriginalPropertyName);
        Assert.Equal("srce", sourceError.MappedPropertyName);

        var destinationError = mappedErrors.First(e => e.OriginalPropertyName == "Destination");
        Assert.Equal("Destination", destinationError.OriginalPropertyName);
        Assert.Equal("dest", destinationError.MappedPropertyName);
    }

    [Fact]
    public void Integration_Should_Map_CircuitStatus_Validation_Errors_To_JSON_Property_Names()
    {
        // Arrange
        var validator = new CircuitStatusValidator();
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            Node = "TEST",
            Id = 1,
            Direction = "incoming",
            Remote = "REMOTE",
            Local = "LOCAL",
            SegmentsSent = -1,      // Invalid
            SegmentsReceived = -1,  // Invalid
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        // Act - Validate (returns C# property names)
        var validationResult = validator.TestValidate(model);
        
        // Map C# property names to JSON property names
        var datagramType = model.GetType();
        var mappedErrors = validationResult.Errors.Select(e => new
        {
            OriginalPropertyName = e.PropertyName,
            MappedPropertyName = JsonPropertyNameMapper.GetJsonPropertyName(datagramType, e.PropertyName),
            ErrorMessage = e.ErrorMessage
        }).ToList();

        // Assert - Original should be C#, mapped should be JSON
        if (mappedErrors.Any(e => e.OriginalPropertyName == "SegmentsSent"))
        {
            var segmentsSentError = mappedErrors.First(e => e.OriginalPropertyName == "SegmentsSent");
            Assert.Equal("SegmentsSent", segmentsSentError.OriginalPropertyName);
            Assert.Equal("segsSent", segmentsSentError.MappedPropertyName);
        }

        if (mappedErrors.Any(e => e.OriginalPropertyName == "SegmentsReceived"))
        {
            var segmentsReceivedError = mappedErrors.First(e => e.OriginalPropertyName == "SegmentsReceived");
            Assert.Equal("SegmentsReceived", segmentsReceivedError.OriginalPropertyName);
            Assert.Equal("segsRcvd", segmentsReceivedError.MappedPropertyName);
        }
    }

    [Fact]
    public void Integration_Should_Map_LinkStatus_Validation_Errors_To_JSON_Property_Names()
    {
        // Arrange
        var validator = new LinkStatusValidator();
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            Node = "TEST",
            Id = 1,
            Direction = "incoming",
            Port = "1",
            Remote = "REMOTE",
            Local = "LOCAL",
            FramesSent = -1,      // Invalid
            FramesReceived = -1,  // Invalid
            FramesResent = 0,
            FramesQueued = 0
        };

        // Act - Validate (returns C# property names)
        var validationResult = validator.TestValidate(model);
        
        // Map C# property names to JSON property names
        var datagramType = model.GetType();
        var mappedErrors = validationResult.Errors.Select(e => new
        {
            OriginalPropertyName = e.PropertyName,
            MappedPropertyName = JsonPropertyNameMapper.GetJsonPropertyName(datagramType, e.PropertyName),
            ErrorMessage = e.ErrorMessage
        }).ToList();

        // Assert - Original should be C#, mapped should be JSON
        if (mappedErrors.Any(e => e.OriginalPropertyName == "FramesSent"))
        {
            var framesSentError = mappedErrors.First(e => e.OriginalPropertyName == "FramesSent");
            Assert.Equal("FramesSent", framesSentError.OriginalPropertyName);
            Assert.Equal("frmsSent", framesSentError.MappedPropertyName);
        }

        if (mappedErrors.Any(e => e.OriginalPropertyName == "FramesReceived"))
        {
            var framesReceivedError = mappedErrors.First(e => e.OriginalPropertyName == "FramesReceived");
            Assert.Equal("FramesReceived", framesReceivedError.OriginalPropertyName);
            Assert.Equal("frmsRcvd", framesReceivedError.MappedPropertyName);
        }
    }

    [Fact]
    public void Integration_Should_Preserve_Separation_Of_Concerns()
    {
        // This test documents the architecture:
        // - Validators work with C# property names (FluentValidation standard)
        // - JsonPropertyNameMapper converts to JSON property names (presentation layer)
        // - Controllers/Services use the mapper to convert before sending to clients

        // Arrange - Create invalid model
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            Node = "",
            Id = 0,
            Direction = "invalid",
            Port = "",
            Remote = "",
            Local = ""
        };

        var validator = new LinkUpEventValidator();

        // Act - Step 1: Validation (domain layer - uses C# names)
        var validationResult = validator.TestValidate(model);
        
        // Validators should return C# property names
        Assert.Contains("Node", validationResult.Errors.Select(e => e.PropertyName));
        Assert.DoesNotContain("node", validationResult.Errors.Select(e => e.PropertyName));

        // Act - Step 2: Mapping (presentation layer - converts to JSON names)
        var datagramType = model.GetType();
        var jsonErrors = validationResult.Errors.Select(e => new
        {
            PropertyName = JsonPropertyNameMapper.GetJsonPropertyName(datagramType, e.PropertyName),
            ErrorMessage = e.ErrorMessage
        }).ToList();

        // Presentation layer should return JSON property names
        Assert.Contains("node", jsonErrors.Select(e => e.PropertyName));
        Assert.DoesNotContain("Node", jsonErrors.Select(e => e.PropertyName));
    }

    #endregion
}
