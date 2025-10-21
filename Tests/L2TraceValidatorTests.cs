using FluentValidation.TestHelper;
using node_api.Models;
using node_api.Validators;

namespace Tests;

public class L2TraceValidatorTests
{
    private readonly L2TraceValidator _validator = new();

    #region Basic L2Trace Tests

    [Fact]
    public void Should_Not_Have_Error_When_Basic_L2Trace_Is_Valid()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000, // Valid timestamp: 2024-10-21 12:00:00 UTC
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            IFieldLength = 24,
            ProtocolId = 240,
            ProtocolName = "DATA"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_DatagramType_Is_Wrong()
    {
        var model = new L2Trace
        {
            DatagramType = "WrongType",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            // Modulo is now optional
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.DatagramType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_FromCallsign(string callsign)
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = callsign,
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ReportFrom);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Port(string port)
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = port,
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Port);
    }

    [Fact]
    public void Should_Reject_Negative_Control()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = -1,
            L2Type = "UI",
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Control);
    }

    #endregion

    #region TimeUnixSeconds Validation

    [Fact]
    public void Should_Accept_Zero_For_TimeUnixSeconds()
    {
        // Unix epoch (1970-01-01 00:00:00 UTC)
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 0,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_Negative_TimeUnixSeconds()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = -1,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
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
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = timestamp,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
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
    public void Should_Accept_Valid_Recent_TimeUnixSeconds(long timestamp)
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = timestamp,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_Current_Timestamp()
    {
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = currentTimestamp,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_Maximum_Valid_Unix_Timestamp()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = maxTimestamp,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_TimeUnixSeconds_Exceeding_Maximum()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        var exceedingTimestamp = maxTimestamp + 1;
        
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = exceedingTimestamp,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds)
            .WithErrorMessage("TimeUnixSeconds exceeds maximum valid Unix timestamp");
    }

    [Fact]
    public void Should_Accept_TimeUnixSeconds_In_Past()
    {
        // Test with a timestamp from 1980 (315532800 = 1980-01-01 00:00:00 UTC)
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 315532800,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_TimeUnixSeconds_In_Future()
    {
        // Test with a timestamp far in the future (2100-01-01)
        var futureTimestamp = new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = futureTimestamp,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Accept_TimeUnixSeconds_With_All_Fields_Present()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            IFieldLength = 24,
            ProtocolId = 240,
            ProtocolName = "DATA",
            PollFinal = "P",
            Digipeaters = new[]
            {
                new L2Trace.Digipeater { Callsign = "RELAY-1", Repeated = true }
            }
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    #endregion

    #region L2Type Validation

    [Theory]
    [InlineData("SABME")]
    [InlineData("C")]
    [InlineData("D")]
    [InlineData("DM")]
    [InlineData("UA")]
    [InlineData("UI")]
    [InlineData("I")]
    [InlineData("FRMR")]
    [InlineData("RR")]
    [InlineData("RNR")]
    [InlineData("REJ")]
    [InlineData("TEST")]
    [InlineData("XID")]
    [InlineData("SREJ")]
    [InlineData("?")]
    public void Should_Accept_Valid_L2Types(string l2Type)
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = l2Type,
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.L2Type);
    }

    [Fact]
    public void Should_Have_Error_When_L2Type_Is_Invalid()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "INVALID",
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.L2Type);
    }

    #endregion

    #region TEST Frame Validation

    [Fact]
    public void Should_Validate_TEST_Frame_Command()
    {
        // Example from specification section 1.1.7
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-1",
            TimeUnixSeconds = 1761058466,
            Port = "2",
            Source = "G8PZT-11",
            Destination = "G8PZT-3",
            Control = 243,
            L2Type = "TEST",
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Validate_TEST_Frame_Response()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-3",
            TimeUnixSeconds = 1761058467,
            Port = "2",
            Source = "G8PZT-3",
            Destination = "G8PZT-11",
            Control = 243,
            L2Type = "TEST",
            CommandResponse = "R"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Validate_TEST_Frame_With_Poll_Bit()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "M0LTE",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "M0LTE-1",
            Destination = "M0ABC",
            Control = 227,
            L2Type = "TEST",
            CommandResponse = "C",
            PollFinal = "P"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Validate_TEST_Frame_With_Final_Bit()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "M0ABC",
            TimeUnixSeconds = 1729512001,
            Port = "1",
            Source = "M0ABC",
            Destination = "M0LTE-1",
            Control = 227,
            L2Type = "TEST",
            CommandResponse = "R",
            PollFinal = "F"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Validate_TEST_Frame_Extended_Modulo()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-1",
            TimeUnixSeconds = 1729512000,
            Port = "3",
            Source = "G8PZT-1",
            Destination = "G8PZT-2",
            Control = 227,
            L2Type = "TEST",
            CommandResponse = "C",
            PollFinal = "P",
            Modulo = 128
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Validate_TEST_Frame_With_Timestamp()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-1",
            TimeUnixSeconds = 1761058466,
            Port = "2",
            Source = "G8PZT-11",
            Destination = "G8PZT-3",
            Control = 243,
            L2Type = "TEST",
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    #endregion

    #region Modulo Validation

    [Fact]
    public void Should_Accept_Null_Modulo()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            Modulo = null
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Modulo);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(128)]
    public void Should_Accept_Valid_Modulo_Values(int modulo)
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            Modulo = modulo
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Modulo);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(16)]
    [InlineData(127)]
    [InlineData(256)]
    public void Should_Have_Error_When_Modulo_Is_Invalid(int modulo)
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            Modulo = modulo
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Modulo)
            .WithErrorMessage("Modulo must be 8 or 128");
    }

    #endregion

    #region CommandResponse Validation

    [Theory]
    [InlineData("C")]
    [InlineData("R")]
    [InlineData("V1")]
    public void Should_Accept_Valid_CommandResponse(string cr)
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = cr
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.CommandResponse);
    }

    [Theory]
    [InlineData("X")]
    [InlineData("")]
    [InlineData("CMD")]
    public void Should_Reject_Invalid_CommandResponse(string cr)
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = cr
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.CommandResponse);
    }

    #endregion

    #region Protocol Name Validation

    [Theory]
    [InlineData("SEG")]
    [InlineData("DATA")]
    [InlineData("NET/ROM")]
    [InlineData("IP")]
    [InlineData("ARP")]
    [InlineData("FLEXNET")]
    [InlineData("?")]
    public void Should_Accept_Valid_ProtocolNames(string protocol)
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = protocol
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.ProtocolName);
    }

    [Fact]
    public void Should_Reject_Invalid_ProtocolName()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "INVALID"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ProtocolName);
    }

    #endregion

    #region IFieldLength Validation

    [Fact]
    public void Should_Accept_IFieldLength_For_I_Frames()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "I",
            CommandResponse = "C",
            IFieldLength = 100
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.IFieldLength);
    }

    [Fact]
    public void Should_Accept_IFieldLength_For_UI_Frames()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            IFieldLength = 100
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.IFieldLength);
    }

    [Fact]
    public void Should_Reject_Negative_IFieldLength()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            IFieldLength = -1
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.IFieldLength);
    }

    #endregion

    #region Digipeater Validation

    [Fact]
    public void Should_Validate_Digipeaters()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            Digipeaters =
            [
                new L2Trace.Digipeater { Callsign = "DIGI1", Repeated = true },
                new L2Trace.Digipeater { Callsign = "DIGI2", Repeated = false }
            ]
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Reject_Digipeater_With_Empty_Callsign()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            TimeUnixSeconds = 1729512000,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            Digipeaters =
            [
                new L2Trace.Digipeater { Callsign = "", Repeated = true }
            ]
        };

        var result = _validator.TestValidate(model);
        Assert.False(result.IsValid);
    }

    #endregion

    #region NET/ROM Validation

    [Fact]
    public void Should_Require_L3Type_When_Protocol_Is_NetRom()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 32,
            L2Type = "I",
            CommandResponse = "C",
            ProtocolName = "NET/ROM"
            // Missing L3Type
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.L3Type);
    }

    [Theory]
    [InlineData("NetRom")]
    [InlineData("Routing info")]
    [InlineData("Routing poll")]
    [InlineData("Unknown")]
    public void Should_Accept_Valid_L3Type_Values(string l3Type)
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 32,
            L2Type = "I",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = l3Type
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.L3Type);
    }

    [Fact]
    public void Should_Require_NetRom_Fields_When_L3Type_Is_NetRom()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT-1",
            Destination = "G8PZT",
            Control = 232,
            L2Type = "I",
            CommandResponse = "C",
            IFieldLength = 36,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom"
            // Missing required NetRom fields
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.L3Source);
        result.ShouldHaveValidationErrorFor(x => x.L3Destination);
        result.ShouldHaveValidationErrorFor(x => x.TimeToLive);
        result.ShouldHaveValidationErrorFor(x => x.L4Type);
    }

    [Fact]
    public void Should_Validate_Complete_NetRom_ConnReq()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT-1",
            Destination = "G8PZT",
            Control = 232,
            L2Type = "I",
            CommandResponse = "C",
            IFieldLength = 36,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            L3Source = "G8PZT-1",
            L3Destination = "G8PZT",
            TimeToLive = 25,
            L4Type = "CONN REQ",
            FromCircuit = 4,
            OriginatingUserCallsign = "G8PZT-4",
            OriginatingNodeCallsign = "G8PZT-1",
            ProposedWindow = 4
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Require_FromCircuit_For_ConnReq()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT-1",
            Destination = "G8PZT",
            Control = 232,
            L2Type = "I",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            L3Source = "G8PZT-1",
            L3Destination = "G8PZT",
            TimeToLive = 25,
            L4Type = "CONN REQ"
            // Missing FromCircuit and other required fields
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.FromCircuit);
        result.ShouldHaveValidationErrorFor(x => x.OriginatingUserCallsign);
        result.ShouldHaveValidationErrorFor(x => x.OriginatingNodeCallsign);
    }

    [Fact]
    public void Should_Require_Service_For_ConnReqX()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT-1",
            Destination = "G8PZT",
            Control = 232,
            L2Type = "I",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            L3Source = "G8PZT-1",
            L3Destination = "G8PZT",
            TimeToLive = 25,
            L4Type = "CONN REQX",
            FromCircuit = 4,
            OriginatingUserCallsign = "G8PZT-4",
            OriginatingNodeCallsign = "G8PZT-1",
            ProposedWindow = 4
            // Missing NetRomXServiceNumber
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.NetRomXServiceNumber);
    }

    #endregion

    #region Routing Info Validation

    [Fact]
    public void Should_Validate_INP3_Routing_Info()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "INP3",
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "GB7JD-8",
                    Hops = 2,
                    OneWayTripTimeIn10msIncrements = 2,
                    Alias = "JEDCHT",
                    Latitude = 55.3125m,
                    Longitude = -2.3250m
                }
            ]
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Require_Hops_And_TripTime_For_INP3_Nodes()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "INP3",
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "GB7JD-8"
                    // Missing Hops and OneWayTripTimeIn10msIncrements
                }
            ]
        };

        var result = _validator.TestValidate(model);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Validate_NODES_Routing_Info()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "NODES",
            FromAlias = "SENDER",
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "NODE1",
                    Alias = "ALIAS1",
                    Via = "VIA1",
                    Quality = 200
                }
            ]
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Validate_Empty_NODES_Routing_Info()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "NODES",
            FromAlias = "SENDER",
            Nodes =
            []
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Require_FromAlias_For_NETROM_Routing()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "NODES",  // Changed from "NETROM" to "NODES" per spec v0.7
            // Missing FromAlias
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "NODE1",
                    Alias = "ALIAS1",
                    Via = "VIA1",
                    Quality = 200
                }
            ]
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.FromAlias);
    }

    [Fact]
    public void Should_Reject_Invalid_Quality_Range()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "NODES",  // Changed from "NETROM" to "NODES" per spec v0.7
            FromAlias = "SENDER",
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "NODE1",
                    Alias = "ALIAS1",
                    Via = "VIA1",
                    Quality = 256  // Out of range
                }
            ]
        };

        var result = _validator.TestValidate(model);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Reject_Invalid_Latitude_In_INP3()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "INP3",
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "GB7JD-8",
                    Hops = 2,
                    OneWayTripTimeIn10msIncrements = 2,
                    Latitude = 91m  // Out of range
                }
            ]
        };

        var result = _validator.TestValidate(model);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Reject_Invalid_Longitude_In_INP3()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "INP3",
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "GB7JD-8",
                    Hops = 2,
                    OneWayTripTimeIn10msIncrements = 2,
                    Longitude = 181m  // Out of range
                }
            ]
        };

        var result = _validator.TestValidate(model);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Reject_Invalid_BitMask_In_INP3()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "INP3",
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "GB7JD-8",
                    Hops = 2,
                    OneWayTripTimeIn10msIncrements = 2,
                    BitMask = 33  // Out of range (must be 0-32)
                }
            ]
        };

        var result = _validator.TestValidate(model);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(32)]
    public void Should_Accept_Valid_BitMask_Values_In_INP3(int bitMask)
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "INP3",
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "GB7JD-8",
                    Hops = 2,
                    OneWayTripTimeIn10msIncrements = 2,
                    BitMask = bitMask
                }
            ]
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Valid_Timestamp_In_INP3_Node()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "INP3",
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "GB7JD-8",
                    Hops = 2,
                    OneWayTripTimeIn10msIncrements = 2,
                    Timestamp = 1759861384  // Valid Unix timestamp
                }
            ]
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Null_Timestamp_In_INP3_Node()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "INP3",
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "GB7JD-8",
                    Hops = 2,
                    OneWayTripTimeIn10msIncrements = 2,
                    Timestamp = null
                }
            ]
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Reject_Negative_Timestamp_In_INP3_Node()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "INP3",
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "GB7JD-8",
                    Hops = 2,
                    OneWayTripTimeIn10msIncrements = 2,
                    Timestamp = -1  // Invalid negative timestamp
                }
            ]
        };

        var result = _validator.TestValidate(model);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Accept_Zero_Timestamp_In_INP3_Node()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "INP3",
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "GB7JD-8",
                    Hops = 2,
                    OneWayTripTimeIn10msIncrements = 2,
                    Timestamp = 0  // Unix epoch
                }
            ]
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Reject_Timestamp_Exceeding_Maximum_In_INP3_Node()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        var exceedingTimestamp = maxTimestamp + 1;

        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "INP3",
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "GB7JD-8",
                    Hops = 2,
                    OneWayTripTimeIn10msIncrements = 2,
                    Timestamp = exceedingTimestamp
                }
            ]
        };

        var result = _validator.TestValidate(model);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Accept_Empty_Timestamp_In_INP3_Node()
    {
        var maxTimestamp = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        var exceedingTimestamp = maxTimestamp + 1;

        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "INP3",
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "GB7JD-8",
                    Hops = 2,
                    OneWayTripTimeIn10msIncrements = 2,
                    Timestamp = exceedingTimestamp
                }
            ]
        };

        var result = _validator.TestValidate(model);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Accept_Empty_Alias_In_NODES_Routing()
    {
        // Real-world example: some nodes send empty alias strings
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "HB9DHG-1",
            TimeUnixSeconds = 1761087084,
            Port = "2",
            Source = "HB9ON-15",
            Destination = "NODES",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "NODES",
            FromAlias = "ONVPS ",
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "KB8UVN-4",
                    Alias = "JTNET",
                    Via = "DK0WUE",
                    Quality = 210
                },
                new L2Trace.Node
                {
                    Callsign = "MB7NBA",
                    Alias = "",  // Empty alias - should be accepted
                    Via = "VE3MCH-8",
                    Quality = 248
                }
            ]
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Reject_Null_Alias_In_NODES_Routing()
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "HB9DHG-1",
            TimeUnixSeconds = 1761087084,
            Port = "2",
            Source = "HB9ON-15",
            Destination = "NODES",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "NODES",
            FromAlias = "ONVPS",
            Nodes =
            [
                new L2Trace.Node
                {
                    Callsign = "MB7NBA",
                    Alias = null,  // Null alias - should be rejected
                    Via = "VE3MCH-8",
                    Quality = 248
                }
            ]
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("Nodes[0].Alias");
    }

    [Fact]
    public void Should_Validate_Real_World_NODES_Routing_With_Empty_Aliases()
    {
        // Complete real-world example from the error report
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "HB9DHG-1",
            TimeUnixSeconds = 1761087084,
            Port = "2",
            Source = "HB9ON-15",
            Destination = "NODES",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
            CommandResponse = "C",
            IFieldLength = 238,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "NODES",
            FromAlias = "ONVPS ",
            Nodes =
            [
                new L2Trace.Node { Callsign = "KB8UVN-4", Alias = "JTNET", Via = "DK0WUE", Quality = 210 },
                new L2Trace.Node { Callsign = "MB7NBA", Alias = "", Via = "VE3MCH-8", Quality = 248 },
                new L2Trace.Node { Callsign = "N2MH-8", Alias = "MH2BBS", Via = "VE3MCH-8", Quality = 246 },
                new L2Trace.Node { Callsign = "N2NOV-7", Alias = "ARECS", Via = "VE3MCH-8", Quality = 244 },
                new L2Trace.Node { Callsign = "N9LYA-8", Alias = "LYANOD", Via = "VE3MCH-8", Quality = 247 },
                new L2Trace.Node { Callsign = "N9SEO-11", Alias = "BAXXRP", Via = "VE3MCH-8", Quality = 248 },
                new L2Trace.Node { Callsign = "N9SEO-12", Alias = "BAXXCT", Via = "VE3MCH-8", Quality = 248 },
                new L2Trace.Node { Callsign = "OH5RM-10", Alias = "RMGTW", Via = "VE3MCH-8", Quality = 246 },
                new L2Trace.Node { Callsign = "PA3GJX-9", Alias = "GJXTCP", Via = "VE3MCH-8", Quality = 177 },
                new L2Trace.Node { Callsign = "SV1DZI-7", Alias = "ATHXRT", Via = "VE3MCH-8", Quality = 242 },
                new L2Trace.Node { Callsign = "VA2OM-8", Alias = "OMCHAT", Via = "VE3MCH-8", Quality = 248 }
            ]
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
    #endregion
}
