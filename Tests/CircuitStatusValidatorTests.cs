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
            Node = "G8PZT-1",
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
        result.ShouldHaveValidationErrorFor(x => x.DatagramType)
            .WithErrorMessage("DatagramType must be 'CircuitStatus'");
    }

    [Fact]
    public void Should_Have_Error_When_Node_Is_Empty()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            Node = "",
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
        result.ShouldHaveValidationErrorFor(x => x.Node)
            .WithErrorMessage("Node callsign is required");
    }

    [Fact]
    public void Should_Have_Error_When_Id_Is_Zero_Or_Negative()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            Node = "G8PZT-1",
            Id = 0,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
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
            Node = "G8PZT-1",
            Id = 1,
            Direction = "sideways",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
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
            Node = "G8PZT-1",
            Id = 1,
            Direction = direction,
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
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
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
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
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Local)
            .WithErrorMessage("Local address is required");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Have_Error_When_SegsSent_Is_Negative(int segsSent)
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = segsSent,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.SegsSent)
            .WithErrorMessage("SegsSent cannot be negative");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Have_Error_When_SegsRcvd_Is_Negative(int segsRcvd)
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = segsRcvd,
            SegsResent = 0,
            SegsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.SegsRcvd)
            .WithErrorMessage("SegsRcvd cannot be negative");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Have_Error_When_SegsResent_Is_Negative(int segsResent)
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = segsResent,
            SegsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.SegsResent)
            .WithErrorMessage("SegsResent cannot be negative");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Have_Error_When_SegsQueued_Is_Negative(int segsQueued)
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = segsQueued
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.SegsQueued)
            .WithErrorMessage("SegsQueued cannot be negative");
    }

    [Fact]
    public void Should_Accept_Zero_Values_For_Segment_Counters()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 0,
            SegsRcvd = 0,
            SegsResent = 0,
            SegsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.SegsSent);
        result.ShouldNotHaveValidationErrorFor(x => x.SegsRcvd);
        result.ShouldNotHaveValidationErrorFor(x => x.SegsResent);
        result.ShouldNotHaveValidationErrorFor(x => x.SegsQueued);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Service_Is_Null()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Service = null,
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

    [Fact]
    public void Should_Not_Have_Error_When_Service_Has_Value()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Service = 0,
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

    [Fact]
    public void Should_Not_Have_Error_When_Valid()
    {
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            Node = "G8PZT-1",
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

    [Fact]
    public void Should_Validate_Example_From_Spec()
    {
        // Example from section 3.9 of the spec
        var model = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            Node = "G8PZT-1",
            Direction = "incoming",
            Id = 1,
            Service = 0,
            Remote = "G8PZT@G8PZT:1ba8",
            Local = "G8PZT-4:0001",
            SegsRcvd = 20,
            SegsSent = 6,
            SegsResent = 0,
            SegsQueued = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
