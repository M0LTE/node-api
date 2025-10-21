using FluentValidation.TestHelper;
using node_api.Models;
using node_api.Validators;

namespace Tests;

/// <summary>
/// Tests for edge cases, boundary conditions, and error scenarios
/// </summary>
public class EdgeCaseTests
{
    #region Unicode and Special Character Tests

    [Fact]
    public void Should_Handle_Unicode_Callsigns()
    {
        var validator = new L2TraceValidator();
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-1",
            Port = "1",
            Source = "Tëst-1", // Unicode character
            Destination = "Dëst",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        var result = validator.TestValidate(model);
        // Should accept unicode in callsigns
        result.ShouldNotHaveValidationErrorFor(x => x.Source);
    }

    [Fact]
    public void Should_Handle_Very_Long_Strings()
    {
        var validator = new LinkUpEventValidator();
        var veryLongString = new string('X', 10000);
        
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            Node = veryLongString,
            Id = 1,
            Direction = "incoming",
            Port = "1",
            Remote = "REMOTE",
            Local = "LOCAL"
        };

        var result = validator.TestValidate(model);
        // Should not crash with very long strings
        Assert.NotNull(result);
    }

    [Fact]
    public void Should_Handle_Special_Characters_In_Reason_Field()
    {
        var validator = new LinkDisconnectionEventValidator();
        var specialReason = "Disconnected: \"Error\" @#$%^&*()_+{}|:<>?[];',./`~";
        
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            Node = "TEST",
            Id = 1,
            Direction = "incoming",
            Port = "1",
            Remote = "REMOTE",
            Local = "LOCAL",
            UpForSecs = 100,
            FramesSent = 10,
            FramesReceived = 10,
            FramesResent = 0,
            FramesQueued = 0,
            Reason = specialReason
        };

        var result = validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Boundary Value Tests

    [Fact]
    public void Should_Handle_Maximum_Integer_Values()
    {
        var validator = new LinkStatusValidator();
        
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            Node = "TEST",
            Id = int.MaxValue,
            Direction = "incoming",
            Port = "1",
            Remote = "REMOTE",
            Local = "LOCAL",
            UpForSecs = int.MaxValue,
            FramesSent = int.MaxValue,
            FramesReceived = int.MaxValue,
            FramesResent = int.MaxValue,
            FramesQueued = int.MaxValue
        };

        var result = validator.TestValidate(model);
        // Should accept maximum valid integer values
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
        result.ShouldNotHaveValidationErrorFor(x => x.UpForSecs);
    }

    [Fact]
    public void Should_Handle_Latitude_Boundary_Values()
    {
        var validator = new NodeUpEventValidator();

        // Test exact boundaries
        var models = new[]
        {
            (lat: -90m, valid: true),
            (lat: -89.999999m, valid: true),
            (lat: 0m, valid: true),
            (lat: 89.999999m, valid: true),
            (lat: 90m, valid: true),
            (lat: -90.000001m, valid: false),
            (lat: 90.000001m, valid: false)
        };

        foreach (var test in models)
        {
            var model = new NodeUpEvent
            {
                DatagramType = "NodeUpEvent",
                NodeCall = "TEST",
                NodeAlias = "TST",
                Locator = "IO82VJ",
                Software = "test",
                Version = "1.0",
                Latitude = test.lat
            };

            var result = validator.TestValidate(model);
            
            if (test.valid)
            {
                result.ShouldNotHaveValidationErrorFor(x => x.Latitude);
            }
            else
            {
                result.ShouldHaveValidationErrorFor(x => x.Latitude);
            }
        }
    }

    [Fact]
    public void Should_Handle_Longitude_Boundary_Values()
    {
        var validator = new NodeUpEventValidator();

        var models = new[]
        {
            (lon: -180m, valid: true),
            (lon: -179.999999m, valid: true),
            (lon: 0m, valid: true),
            (lon: 179.999999m, valid: true),
            (lon: 180m, valid: true),
            (lon: -180.000001m, valid: false),
            (lon: 180.000001m, valid: false)
        };

        foreach (var test in models)
        {
            var model = new NodeUpEvent
            {
                DatagramType = "NodeUpEvent",
                NodeCall = "TEST",
                NodeAlias = "TST",
                Locator = "IO82VJ",
                Software = "test",
                Version = "1.0",
                Longitude = test.lon
            };

            var result = validator.TestValidate(model);
            
            if (test.valid)
            {
                result.ShouldNotHaveValidationErrorFor(x => x.Longitude);
            }
            else
            {
                result.ShouldHaveValidationErrorFor(x => x.Longitude);
            }
        }
    }

    #endregion

    #region NetRom Quality Boundary Tests

    [Fact]
    public void Should_Handle_NetRom_Quality_Boundaries()
    {
        var validator = new L2TraceValidator();

        // Quality must be 0-255 for NODES routing
        var testCases = new []
        {
            (quality: 0, shouldBeValid: true),
            (quality: 1, shouldBeValid: true),
            (quality: 127, shouldBeValid: true),
            (quality: 255, shouldBeValid: true),
            (quality: 256, shouldBeValid: false),
            (quality: -1, shouldBeValid: false)
        };
        
        foreach (var (quality, shouldBeValid) in testCases)
        {
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
                ProtocolName = "NET/ROM",
                L3Type = "Routing info",
                Type = "NODES",
                FromAlias = "SENDER",
                Nodes = new[]
                {
                    new L2Trace.Node
                    {
                        Callsign = "NODE",
                        Alias = "NOD",
                        Via = "VIA",
                        Quality = quality
                    }
                }
            };

            var result = validator.TestValidate(model);
            
            if (shouldBeValid)
            {
                result.ShouldNotHaveAnyValidationErrors();
            }
            else
            {
                // Should have validation error on the Quality field
                Assert.False(result.IsValid, 
                    $"Quality {quality} should be invalid but validation passed");
            }
        }
    }

    #endregion

    #region Timestamp Edge Cases

    [Fact]
    public void Should_Handle_Year_2038_Problem()
    {
        // Unix timestamp signed int32 overflow (Y2038 problem)
        var year2038 = new DateTimeOffset(2038, 1, 19, 3, 14, 7, TimeSpan.Zero).ToUnixTimeSeconds();
        
        var validator = new NodeUpEventValidator();
        var model = new NodeUpEvent
        {
            DatagramType = "NodeUpEvent",
            TimeUnixSeconds = year2038,
            NodeCall = "TEST",
            NodeAlias = "TST",
            Locator = "IO82VJ",
            Software = "test",
            Version = "1.0"
        };

        var result = validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Handle_Very_Far_Future_Timestamps()
    {
        // Year 9999
        var farFuture = new DateTimeOffset(9999, 12, 31, 23, 59, 59, TimeSpan.Zero).ToUnixTimeSeconds();
        
        var validator = new L2TraceValidator();
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "TEST",
            TimeUnixSeconds = farFuture,
            Port = "1",
            Source = "SRC",
            Destination = "DST",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        var result = validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    #endregion

    #region Control Field Boundary Tests

    [Fact]
    public void Should_Accept_All_Valid_Control_Field_Values()
    {
        var validator = new L2TraceValidator();
        
        // Control field is 8-bit (0-255) or 16-bit for extended mode
        var controlValues = new[] { 0, 1, 127, 255, 256, 32767, 65535 };
        
        foreach (var control in controlValues)
        {
            var model = new L2Trace
            {
                DatagramType = "L2Trace",
                ReportFrom = "TEST",
                Port = "1",
                Source = "SRC",
                Destination = "DST",
                Control = control,
                L2Type = "UI",
                CommandResponse = "C"
            };

            var result = validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.Control);
        }
    }

    [Fact]
    public void Should_Reject_Negative_Control_Field()
    {
        var validator = new L2TraceValidator();
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "TEST",
            Port = "1",
            Source = "SRC",
            Destination = "DST",
            Control = -1,
            L2Type = "UI",
            CommandResponse = "C"
        };

        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Control);
    }

    #endregion

    #region Empty Array Tests

    [Fact]
    public void Should_Accept_Empty_Nodes_Array()
    {
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
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "NODES",
            FromAlias = "SENDER",
            Nodes = Array.Empty<L2Trace.Node>()
        };

        var result = validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Empty_Digipeaters_Array()
    {
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
            Digipeaters = Array.Empty<L2Trace.Digipeater>()
        };

        var result = validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Digipeaters);
    }

    [Fact]
    public void Should_Handle_Large_Nodes_Array()
    {
        var validator = new L2TraceValidator();
        
        // Create 100 nodes
        var nodes = Enumerable.Range(1, 100).Select(i => new L2Trace.Node
        {
            Callsign = $"NODE{i}",
            Hops = i % 32,
            OneWayTripTimeIn10msIncrements = i * 10
        }).ToArray();

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
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "INP3",
            Nodes = nodes
        };

        var result = validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Case Sensitivity Tests

    [Fact]
    public void Should_Be_Case_Sensitive_For_Direction()
    {
        var validator = new LinkUpEventValidator();
        
        var invalidDirections = new[] { "Incoming", "INCOMING", "Outgoing", "OUTGOING", "InComing" };
        
        foreach (var direction in invalidDirections)
        {
            var model = new LinkUpEvent
            {
                DatagramType = "LinkUpEvent",
                Node = "TEST",
                Id = 1,
                Direction = direction,
                Port = "1",
                Remote = "REMOTE",
                Local = "LOCAL"
            };

            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Direction);
        }
    }

    [Fact]
    public void Should_Accept_Exact_Case_For_L2Type()
    {
        var validator = new L2TraceValidator();
        
        // Valid L2 types are case-sensitive
        var validTypes = new[] { "UI", "I", "RR", "SABME", "C", "D", "DM", "UA", "FRMR", "RNR", "REJ", "TEST", "XID", "SREJ", "?" };
        
        foreach (var type in validTypes)
        {
            var model = new L2Trace
            {
                DatagramType = "L2Trace",
                ReportFrom = "TEST",
                Port = "1",
                Source = "SRC",
                Destination = "DST",
                Control = 3,
                L2Type = type,
                CommandResponse = "C"
            };

            var result = validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.L2Type);
        }
    }

    #endregion

    #region Whitespace Handling Tests

    [Fact]
    public void Should_Reject_Whitespace_Only_Strings()
    {
        var validator = new NodeUpEventValidator();
        
        var whitespaceStrings = new[] { " ", "  ", "\t", "\n", "\r\n", "   \t\n   " };
        
        foreach (var ws in whitespaceStrings)
        {
            var model = new NodeUpEvent
            {
                DatagramType = "NodeUpEvent",
                NodeCall = ws,
                NodeAlias = "TST",
                Locator = "IO82VJ",
                Software = "test",
                Version = "1.0"
            };

            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.NodeCall);
        }
    }

    [Fact]
    public void Should_Accept_Strings_With_Internal_Whitespace()
    {
        var validator = new LinkDisconnectionEventValidator();
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            Node = "TEST NODE", // Space in middle
            Id = 1,
            Direction = "incoming",
            Port = "1",
            Remote = "REMOTE",
            Local = "LOCAL",
            UpForSecs = 100,
            FramesSent = 10,
            FramesReceived = 10,
            FramesResent = 0,
            FramesQueued = 0
        };

        var result = validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Node);
    }

    #endregion

    #region Multiple Validation Errors

    [Fact]
    public void Should_Report_Multiple_Validation_Errors()
    {
        var validator = new L2TraceValidator();
        var model = new L2Trace
        {
            DatagramType = "WrongType",
            ReportFrom = "",
            Port = "",
            Source = "",
            Destination = "",
            Control = -1,
            L2Type = "INVALID",
            CommandResponse = "X"
        };

        var result = validator.TestValidate(model);
        
        // Should have multiple errors
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 5);
        
        result.ShouldHaveValidationErrorFor(x => x.DatagramType);
        result.ShouldHaveValidationErrorFor(x => x.ReportFrom);
        result.ShouldHaveValidationErrorFor(x => x.Port);
        result.ShouldHaveValidationErrorFor(x => x.Control);
        result.ShouldHaveValidationErrorFor(x => x.L2Type);
        result.ShouldHaveValidationErrorFor(x => x.CommandResponse);
    }

    #endregion
}
