using FluentValidation.TestHelper;
using node_api.Models;
using node_api.Validators;

namespace Tests;

public class LinkUpEventValidatorTests
{
    private readonly LinkUpEventValidator _validator = new();

    [Fact]
    public void Should_Validate_Valid_LinkUpEvent()
    {
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #region TimeUnixSeconds Validation Tests

    [Fact]
    public void Should_Accept_Zero_For_TimeUnixSeconds()
    {
        // Unix epoch (1970-01-01 00:00:00 UTC)
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            TimeUnixSeconds = 0,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_Negative_TimeUnixSeconds()
    {
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            TimeUnixSeconds = -1,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds cannot be negative");
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(-1000)]
    [InlineData(-999999999)]
    public void Should_Reject_Various_Negative_TimeUnixSeconds(long timestamp)
    {
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            TimeUnixSeconds = timestamp,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds cannot be negative");
    }

    [Theory]
    [InlineData(1609459200)]  // 2021-01-01 00:00:00 UTC
    [InlineData(1640995200)]  // 2022-01-01 00:00:00 UTC
    [InlineData(1672531200)]  // 2023-01-01 00:00:00 UTC
    [InlineData(1704067200)]  // 2024-01-01 00:00:00 UTC
    [InlineData(1729512000)]  // 2024-10-21 12:00:00 UTC
    [InlineData(1735689600)]  // 2025-01-01 00:00:00 UTC
    [InlineData(1759688220)]  // From spec examples
    public void Should_Accept_Valid_Recent_TimeUnixSeconds(long timestamp)
    {
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            TimeUnixSeconds = timestamp,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_Current_Timestamp()
    {
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            TimeUnixSeconds = currentTimestamp,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_Maximum_Valid_Unix_Timestamp()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            TimeUnixSeconds = maxTimestamp,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_TimeUnixSeconds_Exceeding_Maximum()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        var exceedingTimestamp = maxTimestamp + 1;
        
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            TimeUnixSeconds = exceedingTimestamp,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds exceeds maximum valid Unix timestamp");
    }

    [Fact]
    public void Should_Accept_TimeUnixSeconds_In_Past()
    {
        // Test with a timestamp from 1980 (315532800 = 1980-01-01 00:00:00 UTC)
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            TimeUnixSeconds = 315532800,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_TimeUnixSeconds_In_Future()
    {
        // Test with a timestamp far in the future (2100-01-01)
        var futureTimestamp = new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            TimeUnixSeconds = futureTimestamp,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Validate_Spec_Example_With_Timestamp()
    {
        // Example from specification section 3.4.4 with time field
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Node(string node)
    {
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            TimeUnixSeconds = 1729512000,
            Node = node,
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Node);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Reject_Invalid_Id(int id)
    {
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = id,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData("incoming")]
    [InlineData("outgoing")]
    public void Should_Accept_Valid_Directions(string direction)
    {
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = direction,
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Direction);
    }

    [Theory]
    [InlineData("up")]
    [InlineData("down")]
    [InlineData("")]
    public void Should_Reject_Invalid_Directions(string direction)
    {
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = direction,
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Direction);
    }

    [Fact]
    public void Should_Reject_Wrong_DatagramType()
    {
        var model = new LinkUpEvent
        {
            DatagramType = "WrongType",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.DatagramType);
    }
}

public class LinkDisconnectionEventValidatorTests
{
    private readonly LinkDisconnectionEventValidator _validator = new();

    [Fact]
    public void Should_Validate_Valid_LinkDownEvent()
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Validate_Spec_Example_LinkDownEvent()
    {
        // Example from specification section 3.4.6
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Direction = "outgoing",
            Id = 2,
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 3,
            FramesReceived = 6,
            FramesResent = 0,
            FramesQueued = 0,
            FramesQueuedPeak = 1,
            BytesSent = 15,
            BytesReceived = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #region TimeUnixSeconds Validation Tests

    [Fact]
    public void Should_Accept_Zero_For_TimeUnixSeconds()
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 0,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_Negative_TimeUnixSeconds()
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = -1,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds cannot be negative");
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(-1000)]
    [InlineData(-999999999)]
    public void Should_Reject_Various_Negative_TimeUnixSeconds(long timestamp)
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = timestamp,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds cannot be negative");
    }

    [Theory]
    [InlineData(1609459200)]  // 2021-01-01 00:00:00 UTC
    [InlineData(1640995200)]  // 2022-01-01 00:00:00 UTC
    [InlineData(1761053424)]  // From spec example
    public void Should_Accept_Valid_Recent_TimeUnixSeconds(long timestamp)
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = timestamp,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_Current_Timestamp()
    {
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = currentTimestamp,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_Maximum_Valid_Unix_Timestamp()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = maxTimestamp,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_TimeUnixSeconds_Exceeding_Maximum()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        var exceedingTimestamp = maxTimestamp + 1;
        
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = exceedingTimestamp,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds exceeds maximum valid Unix timestamp");
    }

    #endregion

    #region UpForSecs Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(78)]
    [InlineData(300)]
    [InlineData(86400)]
    public void Should_Accept_Valid_UpForSecs(int uptime)
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = uptime,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.UpForSecs);
    }

    [Fact]
    public void Should_Reject_Negative_UpForSecs()
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = -1,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.UpForSecs)
            .WithErrorMessage("UpForSecs cannot be negative");
    }

    #endregion

    #region Optional Fields Validation Tests

    [Fact]
    public void Should_Accept_Valid_Optional_Fields()
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0,
            FramesQueuedPeak = 10,
            BytesSent = 5000,
            BytesReceived = 3000,
            Reason = "Retried out"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Null_Optional_Fields()
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0,
            FramesQueuedPeak = null,
            BytesSent = null,
            BytesReceived = null,
            Reason = null
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Reject_Negative_FramesQueuedPeak()
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0,
            FramesQueuedPeak = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.FramesQueuedPeak)
            .WithErrorMessage("FramesQueuedPeak cannot be negative");
    }

    [Fact]
    public void Should_Reject_Negative_BytesSent()
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0,
            BytesSent = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BytesSent)
            .WithErrorMessage("BytesSent cannot be negative");
    }

    [Fact]
    public void Should_Reject_Negative_BytesReceived()
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0,
            BytesReceived = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BytesReceived)
            .WithErrorMessage("BytesReceived cannot be negative");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Should_Accept_Valid_Zero_And_Positive_Optional_Values(int value)
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0,
            FramesQueuedPeak = value,
            BytesSent = value,
            BytesReceived = value
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Optional_Reason()
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0,
            Reason = "Retried out"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Retried out")]
    [InlineData("User disconnected")]
    [InlineData("Connection timeout")]
    [InlineData("")]
    public void Should_Accept_Any_Reason_String(string reason)
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0,
            Reason = reason
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Required Fields Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Node(string node)
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = node,
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Node);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Reject_Invalid_Id(int id)
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = id,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData("incoming")]
    [InlineData("outgoing")]
    public void Should_Accept_Valid_Directions(string direction)
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = direction,
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Direction);
    }

    [Theory]
    [InlineData("up")]
    [InlineData("down")]
    [InlineData("")]
    public void Should_Reject_Invalid_Directions(string direction)
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = direction,
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Direction);
    }

    [Fact]
    public void Should_Reject_Wrong_DatagramType()
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "WrongType",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.DatagramType);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Reject_Negative_Frame_Counts(int count)
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = count,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.FramesSent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void Should_Accept_Valid_Frame_Counts(int count)
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = count,
            FramesReceived = count,
            FramesResent = count,
            FramesQueued = count
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Port(string port)
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = port,
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Port);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Remote(string remote)
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = remote,
            Local = "G8PZT-11",
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Remote);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Local(string local)
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            TimeUnixSeconds = 1761053424,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = local,
            UpForSecs = 78,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Local);
    }

    #endregion
}

public class LinkStatusValidatorTests
{
    private readonly LinkStatusValidator _validator = new();

    [Fact]
    public void Should_Validate_Valid_LinkStatus()
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Zero_Counters()
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 0,
            FramesSent = 0,
            FramesReceived = 0,
            FramesResent = 0,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #region TimeUnixSeconds Validation Tests

    [Fact]
    public void Should_Accept_Zero_For_TimeUnixSeconds()
    {
        // Unix epoch (1970-01-01 00:00:00 UTC)
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 0,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_Negative_TimeUnixSeconds()
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = -1,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds cannot be negative");
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(-1000)]
    [InlineData(-999999999)]
    public void Should_Reject_Various_Negative_TimeUnixSeconds(long timestamp)
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = timestamp,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds cannot be negative");
    }

    [Theory]
    [InlineData(1609459200)]  // 2021-01-01 00:00:00 UTC
    [InlineData(1640995200)]  // 2022-01-01 00:00:00 UTC
    [InlineData(1672531200)]  // 2023-01-01 00:00:00 UTC
    [InlineData(1704067200)]  // 2024-01-01 00:00:00 UTC
    [InlineData(1729512000)]  // 2024-10-21 12:00:00 UTC
    [InlineData(1735689600)]  // 2025-01-01 00:00:00 UTC
    [InlineData(1761053619)]  // From spec example
    public void Should_Accept_Valid_Recent_TimeUnixSeconds(long timestamp)
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = timestamp,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_Current_Timestamp()
    {
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = currentTimestamp,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_Maximum_Valid_Unix_Timestamp()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = maxTimestamp,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_TimeUnixSeconds_Exceeding_Maximum()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        var exceedingTimestamp = maxTimestamp + 1;
        
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = exceedingTimestamp,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds exceeds maximum valid Unix timestamp");
    }

    [Fact]
    public void Should_Validate_Spec_Example_With_All_Fields()
    {
        // Example from specification section 3.4.5
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1761053619,
            Node = "G8PZT-1",
            Direction = "outgoing",
            Id = 1,
            Port = "2",
            Remote = "G8PZT",
            Local = "G8PZT-1",
            UpForSecs = 300,
            FramesSent = 4,
            FramesReceived = 9,
            FramesResent = 0,
            FramesQueued = 0,
            FramesQueuedPeak = 1,
            BytesSent = 402,
            BytesReceived = 1354,
            BpsTxMean = 1,
            BpsRxMean = 4,
            FrameQueueMax = 1,
            L2RttMs = 379
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Optional Fields Validation Tests

    [Fact]
    public void Should_Accept_Valid_Optional_Fields()
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2,
            FramesQueuedPeak = 10,
            BytesSent = 5000,
            BytesReceived = 3000,
            BpsTxMean = 50,
            BpsRxMean = 30,
            FrameQueueMax = 8,
            L2RttMs = 250
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Null_Optional_Fields()
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2,
            FramesQueuedPeak = null,
            BytesSent = null,
            BytesReceived = null,
            BpsTxMean = null,
            BpsRxMean = null,
            FrameQueueMax = null,
            L2RttMs = null
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Reject_Negative_FramesQueuedPeak()
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2,
            FramesQueuedPeak = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.FramesQueuedPeak)
            .WithErrorMessage("FramesQueuedPeak cannot be negative");
    }

    [Fact]
    public void Should_Reject_Negative_BytesSent()
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2,
            BytesSent = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BytesSent)
            .WithErrorMessage("BytesSent cannot be negative");
    }

    [Fact]
    public void Should_Reject_Negative_BytesReceived()
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2,
            BytesReceived = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BytesReceived)
            .WithErrorMessage("BytesReceived cannot be negative");
    }

    [Fact]
    public void Should_Reject_Negative_BpsTxMean()
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2,
            BpsTxMean = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BpsTxMean)
            .WithErrorMessage("BpsTxMean cannot be negative");
    }

    [Fact]
    public void Should_Reject_Negative_BpsRxMean()
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2,
            BpsRxMean = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BpsRxMean)
            .WithErrorMessage("BpsRxMean cannot be negative");
    }

    [Fact]
    public void Should_Reject_Negative_FrameQueueMax()
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2,
            FrameQueueMax = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.FrameQueueMax)
            .WithErrorMessage("FrameQueueMax cannot be negative");
    }

    [Fact]
    public void Should_Reject_Negative_L2RttMs()
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2,
            L2RttMs = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.L2RttMs)
            .WithErrorMessage("L2RttMs cannot be negative");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Should_Accept_Valid_Zero_And_Positive_Optional_Values(int value)
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2,
            FramesQueuedPeak = value,
            BytesSent = value,
            BytesReceived = value,
            BpsTxMean = value,
            BpsRxMean = value,
            FrameQueueMax = value,
            L2RttMs = value
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Required Fields Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Node(string node)
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = node,
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Node);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Reject_Invalid_Id(int id)
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = id,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData("incoming")]
    [InlineData("outgoing")]
    public void Should_Accept_Valid_Directions(string direction)
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = direction,
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Direction);
    }

    [Theory]
    [InlineData("up")]
    [InlineData("down")]
    [InlineData("")]
    public void Should_Reject_Invalid_Directions(string direction)
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = direction,
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Direction);
    }

    [Fact]
    public void Should_Reject_Wrong_DatagramType()
    {
        var model = new LinkStatus
        {
            DatagramType = "WrongType",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = 300,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.DatagramType);
    }

    [Fact]
    public void Should_Reject_Negative_UpForSecs()
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = -1,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.UpForSecs)
            .WithErrorMessage("UpForSecs cannot be negative");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    [InlineData(300)]
    [InlineData(86400)]
    public void Should_Accept_Valid_UpForSecs(int uptime)
    {
        var model = new LinkStatus
        {
            DatagramType = "LinkStatus",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            UpForSecs = uptime,
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 2
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.UpForSecs);
    }
    #endregion
}
