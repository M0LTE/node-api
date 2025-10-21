using FluentValidation.TestHelper;
using node_api.Models;
using node_api.Validators;

namespace Tests;

public class CircuitStatusValidatorTests
{
    private readonly CircuitStatusValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_DatagramType_Is_Wrong()
    {
        var model = new CircuitStatus
        {
            DatagramType = "WrongType",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.DatagramType)
            .WithErrorMessage("DatagramType must be 'CircuitStatus'");
    }

    [Fact]
    public void Should_Have_Error_When_Node_Is_Empty()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Node)
            .WithErrorMessage("Node callsign is required");
    }

    [Fact]
    public void Should_Have_Error_When_Id_Is_Zero_Or_Negative()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 0,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Circuit ID must be greater than 0");
    }

    [Fact]
    public void Should_Have_Error_When_Direction_Is_Invalid()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "sideways",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Direction)
            .WithErrorMessage("Direction must be 'incoming' or 'outgoing'");
    }

    [Theory]
    [InlineData("incoming")]
    [InlineData("outgoing")]
    public void Should_Accept_Valid_Direction_Values(string direction)
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = direction,
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Direction);
    }

    [Fact]
    public void Should_Have_Error_When_Remote_Is_Empty()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Remote)
            .WithErrorMessage("Remote address is required");
    }

    [Fact]
    public void Should_Have_Error_When_Local_Is_Empty()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Local)
            .WithErrorMessage("Local address is required");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Have_Error_When_SegmentsSent_Is_Negative(int segmentsSent)
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = segmentsSent,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.SegmentsSent)
            .WithErrorMessage("SegmentsSent cannot be negative");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Have_Error_When_SegmentsReceived_Is_Negative(int segmentsReceived)
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = segmentsReceived,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.SegmentsReceived)
            .WithErrorMessage("SegmentsReceived cannot be negative");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Have_Error_When_SegmentsResent_Is_Negative(int segmentsResent)
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = segmentsResent,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.SegmentsResent)
            .WithErrorMessage("SegmentsResent cannot be negative");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Have_Error_When_SegmentsQueued_Is_Negative(int segmentsQueued)
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = segmentsQueued
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.SegmentsQueued)
            .WithErrorMessage("SegmentsQueued cannot be negative");
    }

    [Fact]
    public void Should_Accept_Zero_Values_For_Segment_Counters()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 0,
            SegmentsReceived = 0,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.SegmentsSent);
        result.ShouldNotHaveValidationErrorFor(x => x.SegmentsReceived);
        result.ShouldNotHaveValidationErrorFor(x => x.SegmentsResent);
        result.ShouldNotHaveValidationErrorFor(x => x.SegmentsQueued);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Service_Is_Null()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Service = null,
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Service_Has_Value()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Service = 0,
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Valid()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Validate_Example_From_Spec()
    {
        // Example from section 3.4.8 of the spec
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Direction = "incoming",
            Id = 1,
            Service = 0,
            Remote = "G8PZT@G8PZT:1ba8",
            Local = "G8PZT-4:0001",
            SegmentsReceived = 20,
            SegmentsSent = 6,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Null_Byte_Counters()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0,
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
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0,
            BytesReceived = 1024,
            BytesSent = 2048
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Reject_Negative_BytesReceived()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0,
            BytesReceived = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BytesReceived)
            .WithErrorMessage("BytesReceived cannot be negative");
    }

    [Fact]
    public void Should_Reject_Negative_BytesSent()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0,
            BytesSent = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BytesSent)
            .WithErrorMessage("BytesSent cannot be negative");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1024)]
    [InlineData(65536)]
    [InlineData(1048576)]
    public void Should_Accept_Valid_BytesReceived_Values(int bytes)
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0,
            BytesReceived = bytes
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.BytesReceived);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1024)]
    [InlineData(65536)]
    [InlineData(1048576)]
    public void Should_Accept_Valid_BytesSent_Values(int bytes)
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0,
            BytesSent = bytes
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.BytesSent);
    }

    #region TimeUnixSeconds Validation Tests

    [Fact]
    public void Should_Accept_Zero_For_TimeUnixSeconds()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 0,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_Negative_TimeUnixSeconds()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = -1,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
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
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = timestamp,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
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
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = timestamp,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_Current_Timestamp()
    {
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = currentTimestamp,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_Maximum_Valid_Unix_Timestamp()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = maxTimestamp,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_TimeUnixSeconds_Exceeding_Maximum()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        var exceedingTimestamp = maxTimestamp + 1;
        
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = exceedingTimestamp,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds exceeds maximum valid Unix timestamp");
    }

    #endregion
}
