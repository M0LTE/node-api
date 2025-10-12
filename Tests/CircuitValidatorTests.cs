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
            Node = "G8PZT",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = ""
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Local);
    }
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
}
