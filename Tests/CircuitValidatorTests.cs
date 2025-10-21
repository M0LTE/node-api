using FluentValidation.TestHelper;
using node_api.Models;
using node_api.Validators;

namespace Tests;

public class CircuitUpEventValidatorTests
{
    private readonly CircuitUpEventValidator _validator = new();

    #region Valid CircuitUpEvent Tests

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

    [Fact]
    public void Should_Accept_Null_TimeUnixSeconds()
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = null,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    #endregion

    #region DatagramType Validation

    [Fact]
    public void Should_Reject_Wrong_DatagramType()
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "WrongType",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.DatagramType)
            .WithErrorMessage("DatagramType must be 'CircuitUpEvent'");
    }

    [Theory]
    [InlineData("CircuitDownEvent")]
    [InlineData("CircuitStatus")]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Invalid_DatagramTypes(string type)
    {
        var model = new CircuitUpEvent
        {
            DatagramType = type,
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.DatagramType);
    }

    #endregion

    #region TimeUnixSeconds Validation

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
    [InlineData(1609459200)]  // 2021-01-01
    [InlineData(1759688220)]  // From spec
    public void Should_Accept_Valid_TimeUnixSeconds(long timestamp)
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

    #endregion

    #region Node Validation

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Node(string node)
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = node,
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Node)
            .WithErrorMessage("Node callsign is required");
    }

    [Theory]
    [InlineData("G8PZT")]
    [InlineData("G8PZT-1")]
    [InlineData("M0LTE-15")]
    public void Should_Accept_Valid_Node_Callsigns(string node)
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = node,
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Node);
    }

    #endregion

    #region Id Validation

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Reject_Invalid_Id(int id)
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = id,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Circuit ID must be greater than 0");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(65535)]
    public void Should_Accept_Valid_Id(int id)
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = id,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    #endregion

    #region Direction Validation

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

    [Theory]
    [InlineData("up")]
    [InlineData("down")]
    [InlineData("in")]
    [InlineData("out")]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Invalid_Directions(string direction)
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
        result.ShouldHaveValidationErrorFor(x => x.Direction)
            .WithErrorMessage("Direction must be 'incoming' or 'outgoing'");
    }

    #endregion

    #region Remote and Local Address Validation

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Remote(string remote)
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = remote,
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Remote)
            .WithErrorMessage("Remote address is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Local(string local)
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = local
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Local)
            .WithErrorMessage("Local address is required");
    }

    [Theory]
    [InlineData("G8PZT@G8PZT:14c0")]
    [InlineData("M0LTE@M0ABC:1234")]
    [InlineData("G8PZT:0001")]
    public void Should_Accept_Valid_Remote_Addresses(string remote)
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = remote,
            Local = "G8PZT-4:0001"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Remote);
    }

    [Theory]
    [InlineData("G8PZT-4:0001")]
    [InlineData("M0LTE:ffff")]
    [InlineData("NODE:1234")]
    public void Should_Accept_Valid_Local_Addresses(string local)
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = local
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Local);
    }

    #endregion

    #region Service Field Validation

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    public void Should_Accept_Valid_Service_Values(int service)
    {
        var model = new CircuitUpEvent
        {
            DatagramType = "CircuitUpEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Service = service,
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

    #region Valid CircuitDownEvent Tests

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
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Validate_Spec_Example()
    {
        // Example from specification section 3.4.9
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
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0,
            BytesReceived = 14214,
            BytesSent = 6266,
            Reason = "rcvd DREQ"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Null_Optional_Fields()
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = null,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Service = null,
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0,
            BytesReceived = null,
            BytesSent = null,
            Reason = null
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region DatagramType Validation

    [Fact]
    public void Should_Reject_Wrong_DatagramType()
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "WrongType",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
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
            .WithErrorMessage("DatagramType must be 'CircuitDownEvent'");
    }

    #endregion

    #region TimeUnixSeconds Validation

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
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = -1,
            Node = "G8PZT",
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
        
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = exceedingTimestamp,
            Node = "G8PZT",
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

    #region Required Fields Validation

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Node(string node)
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = node,
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
        result.ShouldHaveValidationErrorFor(x => x.Node);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Reject_Invalid_Id(int id)
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = id,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData("incoming")]
    [InlineData("outgoing")]
    public void Should_Accept_Valid_Directions(string direction)
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
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

    [Theory]
    [InlineData("up")]
    [InlineData("down")]
    [InlineData("")]
    public void Should_Reject_Invalid_Directions(string direction)
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
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
        result.ShouldHaveValidationErrorFor(x => x.Direction);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Remote(string remote)
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = remote,
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Remote);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Local(string local)
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = local,
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Local);
    }

    #endregion

    #region Segment Counter Validation

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Reject_Negative_SegmentsSent(int segments)
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
            SegmentsSent = segments,
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
    public void Should_Reject_Negative_SegmentsReceived(int segments)
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
            SegmentsSent = 5,
            SegmentsReceived = segments,
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
    public void Should_Reject_Negative_SegmentsResent(int segments)
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
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = segments,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.SegmentsResent)
            .WithErrorMessage("SegmentsResent cannot be negative");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Reject_Negative_SegmentsQueued(int segments)
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
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = segments
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.SegmentsQueued)
            .WithErrorMessage("SegmentsQueued cannot be negative");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(65535)]
    public void Should_Accept_Valid_Segment_Counts(int segments)
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
            SegmentsSent = segments,
            SegmentsReceived = segments,
            SegmentsResent = segments,
            SegmentsQueued = segments
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Optional Fields Validation

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Reject_Negative_Service(int service)
    {
        var model = new CircuitDisconnectionEvent
        {
            DatagramType = "CircuitDownEvent",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Service = service,
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Service)
            .WithErrorMessage("Service cannot be negative");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Reject_Negative_BytesSent(int bytes)
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
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0,
            BytesSent = bytes
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BytesSent)
            .WithErrorMessage("BytesSent cannot be negative");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Reject_Negative_BytesReceived(int bytes)
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
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0,
            BytesReceived = bytes
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BytesReceived)
            .WithErrorMessage("BytesReceived cannot be negative");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1024)]
    [InlineData(65536)]
    public void Should_Accept_Valid_Byte_Counts(int bytes)
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
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0,
            BytesSent = bytes,
            BytesReceived = bytes
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("rcvd DREQ")]
    [InlineData("Connection timeout")]
    [InlineData("User disconnected")]
    [InlineData("")]
    public void Should_Accept_Any_Reason_String(string reason)
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
            SegmentsSent = 5,
            SegmentsReceived = 27,
            SegmentsResent = 0,
            SegmentsQueued = 0,
            Reason = reason
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
