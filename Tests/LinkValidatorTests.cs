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
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            FramesSent = 100,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
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
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
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
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Reject_Negative_Frame_Counts(int count)
    {
        var model = new LinkDisconnectionEvent
        {
            DatagramType = "LinkDownEvent",
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            FramesSent = count,
            FramesReceived = 50,
            FramesResent = 5,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.FramesSent);
    }
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
            Node = "G8PZT-1",
            Id = 3,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
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
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11",
            FramesSent = 0,
            FramesReceived = 0,
            FramesResent = 0,
            FramesQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
