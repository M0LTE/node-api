using FluentValidation.TestHelper;
using node_api.Models;
using node_api.Validators;

namespace Tests;

public class L3TraceValidatorTests
{
    private readonly L3TraceValidator _validator = new();

    #region Basic L3Trace Tests

    [Fact]
    public void Should_Not_Have_Error_When_Basic_L3Trace_Is_Valid()
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            L3Source = "G8BPQ-4",
            L3Destination = "GM8BPQ-4",
            TimeToLive = 25,
            L4Type = "INFO",
            ToCircuit = 95,
            TransmitSequenceNumber = 72,
            ReceiveSequenceNumber = 56,
            PayloadLength = 20
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Optional_Fields_Are_Null()
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            L3Source = null,
            L3Destination = null,
            TimeToLive = null,
            L4Type = null
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Reject_Negative_Serial()
    {
        var model = new L3Trace
        {
            Serial = -1,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Serial);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_ReportFrom(string reportFrom)
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = reportFrom,
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ReportFrom);
    }

    #endregion

    #region TimeUnixSeconds Validation

    [Fact]
    public void Should_Accept_Zero_For_TimeUnixSeconds()
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 0,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Fact]
    public void Should_Reject_Negative_TimeUnixSeconds()
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = -1,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    [Theory]
    [InlineData(1609459200)]  // 2021-01-01
    [InlineData(1729512000)]  // 2024-10-21
    [InlineData(1762355570)]  // Example from issue
    public void Should_Accept_Valid_TimeUnixSeconds(long timestamp)
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = timestamp,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeUnixSeconds);
    }

    #endregion

    #region Direction Validation

    [Theory]
    [InlineData("sent")]
    [InlineData("rcvd")]
    public void Should_Accept_Valid_Directions(string direction)
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = direction,
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Direction);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("forward")]
    [InlineData("")]
    public void Should_Reject_Invalid_Directions(string direction)
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = direction,
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Direction);
    }

    #endregion

    #region Port Validation

    [Fact]
    public void Should_Reject_Negative_Port()
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = -1,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Port);
    }

    #endregion

    #region Protocol Validation

    [Fact]
    public void Should_Accept_NET_ROM_Protocol()
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom"
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.ProtocolName);
    }

    [Theory]
    [InlineData("IP")]
    [InlineData("ARP")]
    [InlineData("INVALID")]
    public void Should_Reject_Invalid_Protocol(string protocol)
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = protocol,
            L3Type = "NetRom"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ProtocolName);
    }

    #endregion

    #region L3Type Validation

    [Theory]
    [InlineData("NetRom")]
    [InlineData("Routing info")]
    [InlineData("Routing poll")]
    [InlineData("Unknown")]
    public void Should_Accept_Valid_L3Types(string l3Type)
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = l3Type
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.L3Type);
    }

    #endregion

    #region TimeToLive Validation

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(255)]
    public void Should_Accept_Valid_TimeToLive(int ttl)
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            TimeToLive = ttl
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeToLive);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Reject_Invalid_TimeToLive_When_Present(int ttl)
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            TimeToLive = ttl
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimeToLive);
    }

    [Fact]
    public void Should_Accept_Null_TimeToLive()
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            TimeToLive = null
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.TimeToLive);
    }

    #endregion

    #region L4Type Validation

    [Theory]
    [InlineData("CONN REQ")]
    [InlineData("CONN REQX")]
    [InlineData("CONN ACK")]
    [InlineData("CONN NAK")]
    [InlineData("DISC REQ")]
    [InlineData("DISC ACK")]
    [InlineData("INFO")]
    [InlineData("INFO ACK")]
    [InlineData("RSET")]
    [InlineData("PROT EXT")]
    [InlineData("unknown")]
    public void Should_Accept_Valid_L4Types(string l4Type)
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            L4Type = l4Type
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.L4Type);
    }

    [Fact]
    public void Should_Accept_Null_L4Type()
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            L4Type = null
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.L4Type);
    }

    #endregion

    #region Conditional Validation - INFO Frames

    [Fact]
    public void Should_Require_ToCircuit_For_INFO_Frame()
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            L4Type = "INFO",
            ToCircuit = null,
            TransmitSequenceNumber = 72,
            ReceiveSequenceNumber = 56
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ToCircuit);
    }

    [Fact]
    public void Should_Require_TransmitSequenceNumber_For_INFO_Frame()
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            L4Type = "INFO",
            ToCircuit = 95,
            TransmitSequenceNumber = null,
            ReceiveSequenceNumber = 56
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TransmitSequenceNumber);
    }

    [Fact]
    public void Should_Require_ReceiveSequenceNumber_For_INFO_Frame()
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            L4Type = "INFO",
            ToCircuit = 95,
            TransmitSequenceNumber = 72,
            ReceiveSequenceNumber = null
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ReceiveSequenceNumber);
    }

    #endregion

    #region Conditional Validation - INFO ACK Frames

    [Fact]
    public void Should_Require_ToCircuit_For_INFO_ACK_Frame()
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            L4Type = "INFO ACK",
            ToCircuit = null,
            ReceiveSequenceNumber = 56
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ToCircuit);
    }

    [Fact]
    public void Should_Require_ReceiveSequenceNumber_For_INFO_ACK_Frame()
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            L4Type = "INFO ACK",
            ToCircuit = 95,
            ReceiveSequenceNumber = null
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ReceiveSequenceNumber);
    }

    #endregion

    #region Conditional Validation - CONN ACK

    [Fact]
    public void Should_Require_ToCircuit_For_CONN_ACK_Frame()
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            L4Type = "CONN ACK",
            ToCircuit = null
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ToCircuit);
    }

    #endregion

    #region Conditional Validation - DISC Frames

    [Theory]
    [InlineData("DISC REQ")]
    [InlineData("DISC ACK")]
    public void Should_Require_ToCircuit_For_DISC_Frames(string l4Type)
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            L4Type = l4Type,
            ToCircuit = null
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ToCircuit);
    }

    #endregion

    #region Callsign Validation

    [Theory]
    [InlineData("INVALID!")]
    [InlineData("TOOLONGCALLSIGN")]
    public void Should_Reject_Invalid_L3Source_When_Present(string source)
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            L3Source = source
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.L3Source);
    }

    [Theory]
    [InlineData("INVALID!")]
    [InlineData("TOOLONGCALLSIGN")]
    public void Should_Reject_Invalid_L3Destination_When_Present(string destination)
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            L3Destination = destination
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.L3Destination);
    }

    [Fact]
    public void Should_Accept_Null_L3Source()
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            L3Source = null
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.L3Source);
    }

    [Fact]
    public void Should_Accept_Null_L3Destination()
    {
        var model = new L3Trace
        {
            Serial = 45,
            TimeUnixSeconds = 1762355570,
            Direction = "rcvd",
            IsRF = false,
            ReportFrom = "G8BPQ-2",
            Port = 2,
            IFieldLength = 40,
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            L3Destination = null
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.L3Destination);
    }

    #endregion
}
