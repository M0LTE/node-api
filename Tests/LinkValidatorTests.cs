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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Node(string node)
    {
        var model = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
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
