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
            FromCallsign = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = callsign,
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.FromCallsign);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Port(string port)
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            FromCallsign = "G9XXX",
            Port = port,
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = -1,
            L2Type = "UI",
            Modulo = 8,
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Control);
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
    [InlineData("?")]
    public void Should_Accept_Valid_L2Types(string l2Type)
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            FromCallsign = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = l2Type,
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "INVALID",
            Modulo = 8,
            CommandResponse = "C"
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.L2Type);
    }

    #endregion

    #region Modulo Validation

    [Theory]
    [InlineData(8)]
    [InlineData(128)]
    public void Should_Accept_Valid_Modulo_Values(int modulo)
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            FromCallsign = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            Modulo = modulo,
            CommandResponse = "C"
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
            FromCallsign = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            Modulo = modulo,
            CommandResponse = "C"
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
            FromCallsign = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
    [InlineData("?")]
    public void Should_Accept_Valid_ProtocolNames(string protocol)
    {
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            FromCallsign = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "I",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 32,
            L2Type = "I",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 32,
            L2Type = "I",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "2",
            Source = "G8PZT-1",
            Destination = "G8PZT",
            Control = 232,
            L2Type = "I",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "2",
            Source = "G8PZT-1",
            Destination = "G8PZT",
            Control = 232,
            L2Type = "I",
            Modulo = 8,
            ReceiveSequence = 7,
            TransmitSequence = 4,
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
            FromCallsign = "G9XXX",
            Port = "2",
            Source = "G8PZT-1",
            Destination = "G8PZT",
            Control = 232,
            L2Type = "I",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "2",
            Source = "G8PZT-1",
            Destination = "G8PZT",
            Control = 232,
            L2Type = "I",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "NETROM",
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
            FromCallsign = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "NETROM",
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
            FromCallsign = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
            FromCallsign = "G9XXX",
            Port = "2",
            Source = "G8PZT",
            Destination = "G8PZT-1",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
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
    #endregion
}
