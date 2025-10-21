using FluentValidation.TestHelper;
using node_api.Models;
using node_api.Validators;

namespace Tests;

public class CircuitUpEventValidatorTests
{
    private readonly CircuitUpEventValidator _validator = new();

    [Fact]
    public void Should_Validate_Valid_CircuitUpEvent()
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Service = 0,
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Null_Service()
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Service = null,
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("incoming")]
    [InlineData("outgoing")]
    public void Should_Accept_Valid_Directions(string direction)
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = direction,
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Direction);
    }

    [Fact]
    public void Should_Reject_Invalid_Direction()
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "sideways",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Direction);
    }

    [Fact]
    public void Should_Reject_Empty_Remote()
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Remote);
    }

    [Fact]
    public void Should_Reject_Empty_Local()
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = ""
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Local);
    }

    #region TimeUnixSeconds Validation Tests

    [Fact]
    public void Should_Accept_Zero_For_TimeUnixSeconds()
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 0,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_Negative_TimeUnixSeconds()
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = -1,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
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
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = timestamp,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds cannot be negative");
    }

    [Theory]
    [InlineData(1609459200)]  // 2021-01-01 00:00:00 UTC
    [InlineData(1640995200)]  // 2022-01-01 00:00:00 UTC
    [InlineData(1759688220)]  // From spec examples
    public void Should_Accept_Valid_Recent_TimeUnixSeconds(long timestamp)
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = timestamp,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_Current_Timestamp()
    {
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = currentTimestamp,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_Maximum_Valid_Unix_Timestamp()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = maxTimestamp,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_TimeUnixSeconds_Exceeding_Maximum()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        var exceedingTimestamp = maxTimestamp + 1;
        
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = exceedingTimestamp,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds exceeds maximum valid Unix timestamp");
    }

    [Fact]
    public void Should_Validate_Spec_Example()
    {
        // Example from specification section 3.4.7
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Service = 0,
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

public class CircuitDisconnectionEventValidatorTests
{
    private readonly CircuitDisconnectionEventValidator _validator = new();

    [Fact]
    public void Should_Validate_Valid_CircuitDownEvent()
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Service = 0,
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0,
            Reason = "rcvd DREQ"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Null_Reason()
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Reject_Negative_Segment_Counts(int count)
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = count,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.SegsSent);
    }

    [Fact]
    public void Should_Validate_Example_From_Spec()
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Service = 0,
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0,
            Reason = "rcvd DREQ"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Null_Byte_Counters()
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0,
            BytesReceived = null,
            BytesSent = null
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.BytesReceived);
        result.ShouldNotHaveValidationErrorFor(x => x.BytesSent);
    }

    [Fact]
    public void Should_Accept_Valid_Byte_Counters()
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0,
            BytesReceived = 5120,
            BytesSent = 3072
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Reject_Negative_BytesReceived()
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0,
            BytesReceived = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BytesReceived)
            .WithErrorMessage("BytesReceived cannot be negative");
    }

    [Fact]
    public void Should_Reject_Negative_BytesSent()
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0,
            BytesSent = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BytesSent)
            .WithErrorMessage("BytesSent cannot be negative");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(256)]
    [InlineData(1024)]
    [InlineData(65536)]
    public void Should_Accept_Valid_BytesReceived_Values(int bytes)
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0,
            BytesReceived = bytes
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.BytesReceived);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(256)]
    [InlineData(1024)]
    [InlineData(65536)]
    public void Should_Accept_Valid_BytesSent_Values(int bytes)
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0,
            BytesSent = bytes
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.BytesSent);
    }

    [Fact]
    public void Should_Accept_Zero_Byte_Counters()
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0,
            BytesReceived = 0,
            BytesSent = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.BytesReceived);
        result.ShouldNotHaveValidationErrorFor(x => x.BytesSent);
    }

    [Fact]
    public void Should_Validate_Complete_Example_With_All_Optional_Fields()
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Service = 0,
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0,
            Reason = "rcvd DREQ",
            BytesReceived = 6750,
            BytesSent = 1250
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #region TimeUnixSeconds Validation Tests

    [Fact]
    public void Should_Accept_Zero_For_TimeUnixSeconds()
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 0,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_Negative_TimeUnixSeconds()
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = -1,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
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
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = timestamp,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds cannot be negative");
    }

    [Theory]
    [InlineData(1609459200)]  // 2021-01-01 00:00:00 UTC
    [InlineData(1759688220)]  // From spec examples
    public void Should_Accept_Valid_Recent_TimeUnixSeconds(long timestamp)
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = timestamp,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_Current_Timestamp()
    {
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = currentTimestamp,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_Maximum_Valid_Unix_Timestamp()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = maxTimestamp,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_TimeUnixSeconds_Exceeding_Maximum()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        var exceedingTimestamp = maxTimestamp + 1;
        
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = exceedingTimestamp,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds exceeds maximum valid Unix timestamp");
    }

    #endregion
}
