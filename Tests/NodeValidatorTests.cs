using FluentValidation.TestHelper;
using node_api.Models;
using node_api.Validators;

namespace Tests;

public class NodeUpEventValidatorTests
{
    private readonly NodeUpEventValidator _validator = new();

    [Fact]
    public void Should_Validate_Valid_NodeUpEvent()
    {
        var model = new NodeUpEvent
        {
            DatagramType = "NodeUpEvent",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Latitude = 50.145832m,
            Longitude = -5.125000m,
            Software = "XrLin",
            Version = "504j"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Null_Coordinates()
    {
        var model = new NodeUpEvent
        {
            DatagramType = "NodeUpEvent",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Software = "XrLin",
            Version = "504j"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("IO82VJ")]  // All uppercase
    [InlineData("IO82vj")]  // Lowercase subsquare
    [InlineData("JN39AA")]  // All uppercase
    [InlineData("JN39aa")]  // Lowercase subsquare
    [InlineData("FN31PR")]  // All uppercase
    [InlineData("FN31pr")]  // Lowercase subsquare
    [InlineData("IO82Vj")]  // Mixed case subsquare
    [InlineData("IO82vJ")]  // Mixed case subsquare
    public void Should_Accept_Valid_Maidenhead_Locators(string locator)
    {
        var model = new NodeUpEvent
        {
            DatagramType = "NodeUpEvent",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = locator,
            Software = "XrLin",
            Version = "504j"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Locator);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("AA00")]       // Missing subsquare
    [InlineData("ZZ99ZZ")]     // Invalid field (Z > R)
    [InlineData("")]
    [InlineData("io82vj")]     // Lowercase field (should be uppercase)
    [InlineData("IO82")]       // Incomplete (4 chars instead of 6)
    [InlineData("IO82V")]      // Incomplete (5 chars instead of 6)
    public void Should_Reject_Invalid_Maidenhead_Locators(string locator)
    {
        var model = new NodeUpEvent
        {
            DatagramType = "NodeUpEvent",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = locator,
            Software = "XrLin",
            Version = "504j"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Locator);
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    [InlineData(-100)]
    [InlineData(100)]
    public void Should_Reject_Invalid_Latitude(decimal latitude)
    {
        var model = new NodeUpEvent
        {
            DatagramType = "NodeUpEvent",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Latitude = latitude,
            Software = "XrLin",
            Version = "504j"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Latitude);
    }

    [Theory]
    [InlineData(-90)]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(50.145832)]
    public void Should_Accept_Valid_Latitude(decimal latitude)
    {
        var model = new NodeUpEvent
        {
            DatagramType = "NodeUpEvent",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Latitude = latitude,
            Software = "XrLin",
            Version = "504j"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Latitude);
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    [InlineData(-200)]
    [InlineData(200)]
    public void Should_Reject_Invalid_Longitude(decimal longitude)
    {
        var model = new NodeUpEvent
        {
            DatagramType = "NodeUpEvent",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Longitude = longitude,
            Software = "XrLin",
            Version = "504j"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Longitude);
    }

    [Theory]
    [InlineData(-180)]
    [InlineData(0)]
    [InlineData(180)]
    [InlineData(-5.125000)]
    public void Should_Accept_Valid_Longitude(decimal longitude)
    {
        var model = new NodeUpEvent
        {
            DatagramType = "NodeUpEvent",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Longitude = longitude,
            Software = "XrLin",
            Version = "504j"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Longitude);
    }
}

public class NodeDownEventValidatorTests
{
    private readonly NodeDownEventValidator _validator = new();

    [Fact]
    public void Should_Validate_Valid_NodeDownEvent()
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Reason = "Reboot"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Null_Reason()
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Reject_Empty_NodeCall()
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            NodeCall = "",
            NodeAlias = "XRLN64"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.NodeCall);
    }

    [Fact]
    public void Should_Reject_Empty_NodeAlias()
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            NodeCall = "G8PZT-1",
            NodeAlias = ""
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.NodeAlias);
    }
}

public class NodeStatusReportEventValidatorTests
{
    private readonly NodeStatusReportEventValidator _validator = new();

    [Fact]
    public void Should_Validate_Valid_NodeStatus()
    {
        var model = new NodeStatusReportEvent
        {
            DatagramType = "NodeStatus",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Latitude = 50.145832m,
            Longitude = -5.125000m,
            Software = "XrLin",
            Version = "504j",
            UptimeSecs = 86400
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Zero_Uptime()
    {
        var model = new NodeStatusReportEvent
        {
            DatagramType = "NodeStatus",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Software = "XrLin",
            Version = "504j",
            UptimeSecs = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.UptimeSecs);
    }

    [Fact]
    public void Should_Reject_Negative_Uptime()
    {
        var model = new NodeStatusReportEvent
        {
            DatagramType = "NodeStatus",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Software = "XrLin",
            Version = "504j",
            UptimeSecs = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.UptimeSecs);
    }

    [Theory]
    [InlineData("IO82VJ")]  // All uppercase
    [InlineData("IO82vj")]  // Lowercase subsquare
    [InlineData("JN39AA")]  // All uppercase
    [InlineData("JN39aa")]  // Lowercase subsquare
    public void Should_Accept_Valid_Locators(string locator)
    {
        var model = new NodeStatusReportEvent
        {
            DatagramType = "NodeStatus",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = locator,
            Software = "XrLin",
            Version = "504j",
            UptimeSecs = 100
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Locator);
    }
}
