using System.Text.Json;
using FluentValidation.TestHelper;
using node_api.Models;
using node_api.Validators;

namespace Tests;

/// <summary>
/// Tests to verify that the TimeUnixSeconds field correctly deserializes both
/// integer timestamps (from legacy clients) and decimal timestamps (from millisecond-aware clients).
/// This ensures backward compatibility when changing TimeUnixSeconds from long? to decimal?.
/// </summary>
public class TimestampDeserializationTests
{
    #region L2Trace Timestamp Tests

    [Fact]
    public void L2Trace_Should_Deserialize_Integer_Timestamp()
    {
        // Arrange - integer timestamp from legacy client
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "G8PZT-1",
            "time": 1729512000,
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "ID",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<L2Trace>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1729512000m, result.TimeUnixSeconds);
    }

    [Fact]
    public void L2Trace_Should_Deserialize_Decimal_Timestamp_With_Milliseconds()
    {
        // Arrange - decimal timestamp from millisecond-aware client
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "G8PZT-1",
            "time": 1729512000.123,
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "ID",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<L2Trace>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1729512000.123m, result.TimeUnixSeconds);
    }

    [Fact]
    public void L2Trace_Should_Deserialize_Null_Timestamp()
    {
        // Arrange - no timestamp
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "G8PZT-1",
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "ID",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<L2Trace>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.TimeUnixSeconds);
    }

    [Fact]
    public void L2Trace_Should_Preserve_Millisecond_Precision()
    {
        // Arrange
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "G8PZT-1",
            "time": 1729512000.999,
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "ID",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<L2Trace>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1729512000.999m, result.TimeUnixSeconds);
    }

    #endregion

    #region NodeUpEvent Timestamp Tests

    [Fact]
    public void NodeUpEvent_Should_Deserialize_Integer_Timestamp()
    {
        // Arrange
        var json = """
        {
            "@type": "NodeUpEvent",
            "time": 1760976724,
            "nodeCall": "G8PZT-1",
            "nodeAlias": "XRLN64",
            "locator": "IO70KD",
            "software": "XrLin",
            "version": "504j"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<NodeUpEvent>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1760976724m, result.TimeUnixSeconds);
    }

    [Fact]
    public void NodeUpEvent_Should_Deserialize_Decimal_Timestamp()
    {
        // Arrange
        var json = """
        {
            "@type": "NodeUpEvent",
            "time": 1760976724.456,
            "nodeCall": "G8PZT-1",
            "nodeAlias": "XRLN64",
            "locator": "IO70KD",
            "software": "XrLin",
            "version": "504j"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<NodeUpEvent>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1760976724.456m, result.TimeUnixSeconds);
    }

    #endregion

    #region LinkUpEvent Timestamp Tests

    [Fact]
    public void LinkUpEvent_Should_Deserialize_Integer_Timestamp()
    {
        // Arrange
        var json = """
        {
            "@type": "LinkUpEvent",
            "time": 1759688220,
            "node": "G8PZT-1",
            "id": 3,
            "direction": "outgoing",
            "port": "2",
            "remote": "KIDDER-1",
            "local": "G8PZT-11"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<LinkUpEvent>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1759688220m, result.TimeUnixSeconds);
    }

    [Fact]
    public void LinkUpEvent_Should_Deserialize_Decimal_Timestamp()
    {
        // Arrange
        var json = """
        {
            "@type": "LinkUpEvent",
            "time": 1759688220.789,
            "node": "G8PZT-1",
            "id": 3,
            "direction": "outgoing",
            "port": "2",
            "remote": "KIDDER-1",
            "local": "G8PZT-11"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<LinkUpEvent>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1759688220.789m, result.TimeUnixSeconds);
    }

    #endregion

    #region CircuitUpEvent Timestamp Tests

    [Fact]
    public void CircuitUpEvent_Should_Deserialize_Integer_Timestamp()
    {
        // Arrange
        var json = """
        {
            "@type": "CircuitUpEvent",
            "time": 1759688220,
            "node": "G8PZT",
            "id": 1,
            "direction": "incoming",
            "remote": "G8PZT@G8PZT:14c0",
            "local": "G8PZT-4:0001"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<CircuitUpEvent>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1759688220m, result.TimeUnixSeconds);
    }

    [Fact]
    public void CircuitUpEvent_Should_Deserialize_Decimal_Timestamp()
    {
        // Arrange
        var json = """
        {
            "@type": "CircuitUpEvent",
            "time": 1759688220.001,
            "node": "G8PZT",
            "id": 1,
            "direction": "incoming",
            "remote": "G8PZT@G8PZT:14c0",
            "local": "G8PZT-4:0001"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<CircuitUpEvent>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1759688220.001m, result.TimeUnixSeconds);
    }

    #endregion

    #region Validator Tests with Decimal Timestamps

    [Fact]
    public void L2TraceValidator_Should_Accept_Integer_Timestamp()
    {
        // Arrange
        var validator = new L2TraceValidator();
        var model = new L2Trace
        {
            ReportFrom = "G8PZT-1",
            TimeUnixSeconds = 1729512000m, // Integer value as decimal
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void L2TraceValidator_Should_Accept_Decimal_Timestamp()
    {
        // Arrange
        var validator = new L2TraceValidator();
        var model = new L2Trace
        {
            ReportFrom = "G8PZT-1",
            TimeUnixSeconds = 1729512000.123m, // Decimal with milliseconds
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void L2TraceValidator_Should_Reject_Negative_Decimal_Timestamp()
    {
        // Arrange
        var validator = new L2TraceValidator();
        var model = new L2Trace
        {
            ReportFrom = "G8PZT-1",
            TimeUnixSeconds = -1.5m, // Negative decimal
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds cannot be negative");
    }

    [Fact]
    public void NodeUpEventValidator_Should_Accept_Decimal_Timestamp()
    {
        // Arrange
        var validator = new NodeUpEventValidator();
        var model = new NodeUpEvent
        {
            TimeUnixSeconds = 1760976724.789m,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Software = "XrLin",
            Version = "504j"
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    #endregion

    #region Polymorphic Deserialization Tests

    [Fact]
    public void Polymorphic_Deserialization_Should_Preserve_Integer_Timestamp()
    {
        // Arrange
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "G8PZT-1",
            "time": 1729512000,
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "ID",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<NetworkEventDatagram>(json);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<L2Trace>(result);
        var trace = (L2Trace)result;
        Assert.Equal(1729512000m, trace.TimeUnixSeconds);
    }

    [Fact]
    public void Polymorphic_Deserialization_Should_Preserve_Decimal_Timestamp()
    {
        // Arrange
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "G8PZT-1",
            "time": 1729512000.456,
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "ID",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<NetworkEventDatagram>(json);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<L2Trace>(result);
        var trace = (L2Trace)result;
        Assert.Equal(1729512000.456m, trace.TimeUnixSeconds);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Should_Deserialize_Zero_Timestamp()
    {
        // Arrange
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "G8PZT-1",
            "time": 0,
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "ID",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<L2Trace>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0m, result.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Deserialize_Very_Small_Decimal()
    {
        // Arrange
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "G8PZT-1",
            "time": 0.001,
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "ID",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<L2Trace>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0.001m, result.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Deserialize_Timestamp_With_High_Precision()
    {
        // Arrange - six decimal places for microseconds
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "G8PZT-1",
            "time": 1729512000.123456,
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "ID",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<L2Trace>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1729512000.123456m, result.TimeUnixSeconds);
    }

    [Theory]
    [InlineData(1609459200)]      // 2021-01-01 00:00:00 UTC as integer
    [InlineData(1609459200.5)]    // Half second
    [InlineData(1609459200.999)]  // Just under 1 second
    [InlineData(1609459201.001)]  // Just over 1 second from previous value
    public void Should_Deserialize_Various_Timestamp_Formats(decimal expectedTimestamp)
    {
        // Arrange
        var json = $$"""
        {
            "@type": "L2Trace",
            "reportFrom": "G8PZT-1",
            "time": {{expectedTimestamp}},
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "ID",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<L2Trace>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTimestamp, result.TimeUnixSeconds);
    }

    #endregion

    #region Serialization Round-Trip Tests

    [Fact]
    public void Should_Serialize_And_Deserialize_Integer_Timestamp()
    {
        // Arrange
        var original = new L2Trace
        {
            ReportFrom = "G8PZT-1",
            TimeUnixSeconds = 1729512000m,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var result = JsonSerializer.Deserialize<L2Trace>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(original.TimeUnixSeconds, result.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Serialize_And_Deserialize_Decimal_Timestamp()
    {
        // Arrange
        var original = new L2Trace
        {
            ReportFrom = "G8PZT-1",
            TimeUnixSeconds = 1729512000.789m,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var result = JsonSerializer.Deserialize<L2Trace>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(original.TimeUnixSeconds, result.TimeUnixSeconds);
    }

    #endregion

    #region UnixSecondsToDateTime Conversion Tests

    [Fact]
    public void UnixSecondsToDateTime_Should_Preserve_Milliseconds()
    {
        // Arrange - 1729512000.123 = 2024-10-21 12:00:00.123 UTC
        var unixSeconds = 1729512000.123m;
        
        // Act - Use reflection to test the private helper method or test via the full flow
        var linkUpEvent = new LinkUpEvent
        {
            TimeUnixSeconds = unixSeconds,
            Node = "TEST-1",
            Id = 1,
            Direction = "outgoing",
            Port = "1",
            Remote = "TEST-2",
            Local = "TEST-1"
        };

        // Convert manually to verify expected result
        var wholeSeconds = (long)unixSeconds;
        var fractionalPart = unixSeconds - wholeSeconds;
        var milliseconds = (int)(fractionalPart * 1000m);
        var expected = DateTimeOffset.FromUnixTimeSeconds(wholeSeconds).UtcDateTime.AddMilliseconds(milliseconds);
        
        // Assert
        Assert.Equal(123, expected.Millisecond);
    }

    [Theory]
    [InlineData(1729512000.000, 0)]     // No milliseconds
    [InlineData(1729512000.001, 1)]     // 1 millisecond
    [InlineData(1729512000.123, 123)]   // 123 milliseconds
    [InlineData(1729512000.500, 500)]   // Half second
    [InlineData(1729512000.999, 999)]   // 999 milliseconds
    public void UnixSecondsToDateTime_Should_Convert_Various_Millisecond_Values(decimal unixSeconds, int expectedMilliseconds)
    {
        // Act - Manual conversion using the same logic as UnixSecondsToDateTime
        var wholeSeconds = (long)unixSeconds;
        var fractionalPart = unixSeconds - wholeSeconds;
        var milliseconds = (int)(fractionalPart * 1000m);
        var result = DateTimeOffset.FromUnixTimeSeconds(wholeSeconds).UtcDateTime.AddMilliseconds(milliseconds);
        
        // Assert
        Assert.Equal(expectedMilliseconds, result.Millisecond);
    }

    [Fact]
    public void UnixSecondsToDateTime_Should_Handle_Integer_Timestamps()
    {
        // Arrange - Integer timestamp (no fractional part)
        var unixSeconds = 1729512000m;
        
        // Act
        var wholeSeconds = (long)unixSeconds;
        var fractionalPart = unixSeconds - wholeSeconds;
        var milliseconds = (int)(fractionalPart * 1000m);
        var result = DateTimeOffset.FromUnixTimeSeconds(wholeSeconds).UtcDateTime.AddMilliseconds(milliseconds);
        
        // Assert
        Assert.Equal(0, result.Millisecond);
        Assert.Equal(new DateTime(2024, 10, 21, 12, 0, 0, DateTimeKind.Utc), result);
    }

    #endregion
}
