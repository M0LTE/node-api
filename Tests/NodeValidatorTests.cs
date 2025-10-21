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
            TimeUnixSeconds = 1759682231,
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
            TimeUnixSeconds = 1759682231,
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
            TimeUnixSeconds = 1759682231,
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
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = ""
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.NodeAlias);
    }

    [Fact]
    public void Should_Accept_Valid_Optional_Statistics()
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            LinksIn = 100,
            LinksOut = 50,
            CircuitsIn = 25,
            CircuitsOut = 30,
            L3Relayed = 1000
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Null_Statistics()
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.LinksIn);
        result.ShouldNotHaveValidationErrorFor(x => x.LinksOut);
        result.ShouldNotHaveValidationErrorFor(x => x.CircuitsIn);
        result.ShouldNotHaveValidationErrorFor(x => x.CircuitsOut);
        result.ShouldNotHaveValidationErrorFor(x => x.L3Relayed);
    }

    [Fact]
    public void Should_Reject_Negative_LinksIn()
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            LinksIn = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.LinksIn);
    }

    [Fact]
    public void Should_Reject_Negative_LinksOut()
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            LinksOut = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.LinksOut);
    }

    [Fact]
    public void Should_Reject_Negative_CircuitsIn()
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            CircuitsIn = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.CircuitsIn);
    }

    [Fact]
    public void Should_Reject_Negative_CircuitsOut()
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            CircuitsOut = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.CircuitsOut);
    }

    [Fact]
    public void Should_Reject_Negative_L3Relayed()
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            L3Relayed = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.L3Relayed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    public void Should_Accept_Valid_LinksIn(int value)
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            LinksIn = value
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.LinksIn);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    public void Should_Accept_Valid_LinksOut(int value)
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            LinksOut = value
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.LinksOut);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    public void Should_Accept_Valid_CircuitsIn(int value)
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            CircuitsIn = value
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.CircuitsIn);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    public void Should_Accept_Valid_CircuitsOut(int value)
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            CircuitsOut = value
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.CircuitsOut);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    public void Should_Accept_Valid_L3Relayed(int value)
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            L3Relayed = value
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.L3Relayed);
    }

    #region TimeUnixSeconds Validation Tests

    [Fact]
    public void Should_Accept_Zero_For_TimeUnixSeconds()
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 0,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_Negative_TimeUnixSeconds()
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = -1,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64"
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
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = timestamp,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds cannot be negative");
    }

    [Theory]
    [InlineData(1609459200)]  // 2021-01-01 00:00:00 UTC
    [InlineData(1759682231)]  // From spec example
    public void Should_Accept_Valid_Recent_TimeUnixSeconds(long timestamp)
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = timestamp,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_Current_Timestamp()
    {
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = currentTimestamp,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_Maximum_Valid_Unix_Timestamp()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = maxTimestamp,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_TimeUnixSeconds_Exceeding_Maximum()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        var exceedingTimestamp = maxTimestamp + 1;
        
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = exceedingTimestamp,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds exceeds maximum valid Unix timestamp");
    }

    #endregion

    #region UptimeSecs Validation Tests

    [Fact]
    public void Should_Accept_Null_UptimeSecs()
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            UptimeSecs = null
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.UptimeSecs);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(14420)]  // From spec example
    [InlineData(86400)]  // 1 day
    [InlineData(2592000)]  // 30 days
    public void Should_Accept_Valid_UptimeSecs(int uptime)
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            UptimeSecs = uptime
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.UptimeSecs);
    }

    [Fact]
    public void Should_Reject_Negative_UptimeSecs()
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            UptimeSecs = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.UptimeSecs)
            .WithErrorMessage("UptimeSecs cannot be negative");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(-999999)]
    public void Should_Reject_Various_Negative_UptimeSecs(int uptime)
    {
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            UptimeSecs = uptime
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.UptimeSecs)
            .WithErrorMessage("UptimeSecs cannot be negative");
    }

    #endregion

    [Fact]
    public void Should_Validate_Complete_Spec_Example()
    {
        // Example from specification section 3.4.3
        var model = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            TimeUnixSeconds = 1759682231,
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            UptimeSecs = 14420,
            Reason = "Shutdown",
            LinksIn = 0,
            LinksOut = 2,
            CircuitsIn = 4,
            CircuitsOut = 5,
            L3Relayed = 0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
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

    [Fact]
    public void Should_Accept_Valid_Optional_Statistics()
    {
        var model = new NodeStatusReportEvent
        {
            DatagramType = "NodeStatus",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Software = "XrLin",
            Version = "504j",
            UptimeSecs = 86400,
            LinksIn = 150,
            LinksOut = 75,
            CircuitsIn = 40,
            CircuitsOut = 35,
            L3Relayed = 5000
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Null_Statistics()
    {
        var model = new NodeStatusReportEvent
        {
            DatagramType = "NodeStatus",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Software = "XrLin",
            Version = "504j",
            UptimeSecs = 100
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.LinksIn);
        result.ShouldNotHaveValidationErrorFor(x => x.LinksOut);
        result.ShouldNotHaveValidationErrorFor(x => x.CircuitsIn);
        result.ShouldNotHaveValidationErrorFor(x => x.CircuitsOut);
        result.ShouldNotHaveValidationErrorFor(x => x.L3Relayed);
    }

    [Fact]
    public void Should_Reject_Negative_LinksIn()
    {
        var model = new NodeStatusReportEvent
        {
            DatagramType = "NodeStatus",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Software = "XrLin",
            Version = "504j",
            UptimeSecs = 100,
            LinksIn = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.LinksIn);
    }

    [Fact]
    public void Should_Reject_Negative_LinksOut()
    {
        var model = new NodeStatusReportEvent
        {
            DatagramType = "NodeStatus",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Software = "XrLin",
            Version = "504j",
            UptimeSecs = 100,
            LinksOut = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.LinksOut);
    }

    [Fact]
    public void Should_Reject_Negative_CircuitsIn()
    {
        var model = new NodeStatusReportEvent
        {
            DatagramType = "NodeStatus",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Software = "XrLin",
            Version = "504j",
            UptimeSecs = 100,
            CircuitsIn = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.CircuitsIn);
    }

    [Fact]
    public void Should_Reject_Negative_CircuitsOut()
    {
        var model = new NodeStatusReportEvent
        {
            DatagramType = "NodeStatus",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Software = "XrLin",
            Version = "504j",
            UptimeSecs = 100,
            CircuitsOut = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.CircuitsOut);
    }

    [Fact]
    public void Should_Reject_Negative_L3Relayed()
    {
        var model = new NodeStatusReportEvent
        {
            DatagramType = "NodeStatus",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Software = "XrLin",
            Version = "504j",
            UptimeSecs = 100,
            L3Relayed = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.L3Relayed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(500)]
    [InlineData(999999)]
    public void Should_Accept_Valid_Statistics_Values(int value)
    {
        var model = new NodeStatusReportEvent
        {
            DatagramType = "NodeStatus",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Software = "XrLin",
            Version = "504j",
            UptimeSecs = 100,
            LinksIn = value,
            LinksOut = value,
            CircuitsIn = value,
            CircuitsOut = value,
            L3Relayed = value
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.LinksIn);
        result.ShouldNotHaveValidationErrorFor(x => x.LinksOut);
        result.ShouldNotHaveValidationErrorFor(x => x.CircuitsIn);
        result.ShouldNotHaveValidationErrorFor(x => x.CircuitsOut);
        result.ShouldNotHaveValidationErrorFor(x => x.L3Relayed);
    }
}
