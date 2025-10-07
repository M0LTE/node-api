using FluentAssertions;
using node_api.Models;
using System.Text.Json;

namespace Tests;

public class XrouterUdpJsonFrameDeserializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false
    };

    [Fact]
    public void Should_Deserialize_UI_Frame_Example()
    {
        // Arrange
        var json = """
        {
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "ID",
            "ctrl": 3,
            "l2type": "UI",
            "modulo": 8,
            "cr": "C",
            "ilen": 24,
            "pid": 240,
            "ptcl": "DATA"
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("1");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("ID");
        frame.Control.Should().Be(3);
        frame.L2Type.Should().Be("UI");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("C");
        frame.IFieldLength.Should().Be(24);
        frame.ProtocolId.Should().Be(240);
        frame.ProtocolName.Should().Be("DATA");
    }

    [Fact]
    public void Should_Deserialize_AX25_Connect_Request_Example()
    {
        // Arrange
        var json = """
        {
            "port": "8",
            "srce": "G8PZT-1",
            "dest": "G8PZT-14",
            "ctrl": 63,
            "l2type": "C",
            "modulo": 8,
            "cr": "C",
            "pf": "P"
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("8");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("G8PZT-14");
        frame.Control.Should().Be(63);
        frame.L2Type.Should().Be("C");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("C");
        frame.PollFinal.Should().Be("P");
    }

    [Fact]
    public void Should_Deserialize_AX25_UA_Response_Example()
    {
        // Arrange
        var json = """
        {
            "port": "8",
            "srce": "G8PZT-14",
            "dest": "G8PZT-1",
            "ctrl": 115,
            "l2type": "UA",
            "modulo": 8,
            "cr": "R",
            "pf": "F"
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("8");
        frame.Source.Should().Be("G8PZT-14");
        frame.Destination.Should().Be("G8PZT-1");
        frame.Control.Should().Be(115);
        frame.L2Type.Should().Be("UA");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("R");
        frame.PollFinal.Should().Be("F");
    }

    [Fact]
    public void Should_Deserialize_Numbered_Information_Frame_Example()
    {
        // Arrange
        var json = """
        {
            "port": "2",
            "srce": "G8PZT-11",
            "dest": "KIDDER-1",
            "ctrl": 66,
            "l2type": "I",
            "modulo": 8,
            "rseq": 2,
            "tseq": 1,
            "cr": "C",
            "ilen": 2,
            "pid": 240,
            "ptcl": "DATA"
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("2");
        frame.Source.Should().Be("G8PZT-11");
        frame.Destination.Should().Be("KIDDER-1");
        frame.Control.Should().Be(66);
        frame.L2Type.Should().Be("I");
        frame.Modulo.Should().Be(8);
        frame.ReceiveSequence.Should().Be(2);
        frame.TransmitSequence.Should().Be(1);
        frame.CommandResponse.Should().Be("C");
        frame.IFieldLength.Should().Be(2);
        frame.ProtocolId.Should().Be(240);
        frame.ProtocolName.Should().Be("DATA");
    }

    [Fact]
    public void Should_Deserialize_Supervisory_RR_Frame_Example()
    {
        // Arrange
        var json = """
        {
            "port": "2",
            "srce": "G8PZT-1",
            "dest": "G8PZT",
            "ctrl": 161,
            "l2type": "RR",
            "modulo": 8,
            "rseq": 5,
            "cr": "R"
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("2");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("G8PZT");
        frame.Control.Should().Be(161);
        frame.L2Type.Should().Be("RR");
        frame.Modulo.Should().Be(8);
        frame.ReceiveSequence.Should().Be(5);
        frame.CommandResponse.Should().Be("R");
    }

    [Fact]
    public void Should_Deserialize_UI_Frame_With_ARP_Example()
    {
        // Arrange
        var json = """
        {
            "port": "2",
            "srce": "G8PZT-1",
            "dest": "QST",
            "ctrl": 3,
            "l2type": "UI",
            "modulo": 8,
            "cr": "C",
            "ilen": 30,
            "pid": 205,
            "ptcl": "ARP"
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("2");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("QST");
        frame.Control.Should().Be(3);
        frame.L2Type.Should().Be("UI");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("C");
        frame.IFieldLength.Should().Be(30);
        frame.ProtocolId.Should().Be(205);
        frame.ProtocolName.Should().Be("ARP");
    }

    [Fact]
    public void Should_Deserialize_NetRom_Connect_Request_Example()
    {
        // Arrange
        var json = """
        {
            "port": "2",
            "srce": "G8PZT-1",
            "dest": "G8PZT",
            "ctrl": 232,
            "l2type": "I",
            "modulo": 8,
            "rseq": 7,
            "tseq": 4,
            "cr": "C",
            "ilen": 36,
            "pid": 207,
            "ptcl": "NET/ROM",
            "l3type": "NetRom",
            "l3src": "G8PZT-1",
            "l3dst": "G8PZT",
            "ttl": 25,
            "l4type": "CONN REQ",
            "fromCct": 4,
            "srcUser": "G8PZT-4",
            "srcNode": "G8PZT-1",
            "window": 4
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("2");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("G8PZT");
        frame.Control.Should().Be(232);
        frame.L2Type.Should().Be("I");
        frame.Modulo.Should().Be(8);
        frame.ReceiveSequence.Should().Be(7);
        frame.TransmitSequence.Should().Be(4);
        frame.CommandResponse.Should().Be("C");
        frame.IFieldLength.Should().Be(36);
        frame.ProtocolId.Should().Be(207);
        frame.ProtocolName.Should().Be("NET/ROM");
        frame.L3Type.Should().Be("NetRom");
        frame.L3Source.Should().Be("G8PZT-1");
        frame.L3Destination.Should().Be("G8PZT");
        frame.TimeToLive.Should().Be(25);
        frame.L4Type.Should().Be("CONN REQ");
        frame.FromCircuit.Should().Be(4);
        frame.OriginatingUserCallsign.Should().Be("G8PZT-4");
        frame.OriginatingNodeCallsign.Should().Be("G8PZT-1");
        frame.ProposedWindow.Should().Be(4);
    }

    [Fact]
    public void Should_Deserialize_NetRom_Connect_Acknowledgement_Example()
    {
        // Arrange
        var json = """
        {
            "port": "2",
            "srce": "G8PZT",
            "dest": "G8PZT-1",
            "ctrl": 0,
            "l2type": "I",
            "modulo": 8,
            "cr": "R",
            "l3type": "NetRom",
            "l3src": "G8PZT",
            "l3dst": "G8PZT-1",
            "ttl": 4,
            "l4type": "CONN ACK",
            "toCct": 4,
            "fromCct": 4888,
            "accWin": 4
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("2");
        frame.Source.Should().Be("G8PZT");
        frame.Destination.Should().Be("G8PZT-1");
        frame.Control.Should().Be(0);
        frame.L2Type.Should().Be("I");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("R");
        frame.L3Type.Should().Be("NetRom");
        frame.L3Source.Should().Be("G8PZT");
        frame.L3Destination.Should().Be("G8PZT-1");
        frame.TimeToLive.Should().Be(4);
        frame.L4Type.Should().Be("CONN ACK");
        frame.ToCircuit.Should().Be(4);
        frame.FromCircuit.Should().Be(4888);
        frame.AcceptableWindow.Should().Be(4);
    }

    [Fact]
    public void Should_Deserialize_NetRom_Info_Frame_Example()
    {
        // Arrange
        var json = """
        {
            "port": "2",
            "srce": "G8PZT",
            "dest": "G8PZT-1",
            "ctrl": 0,
            "l2type": "I",
            "modulo": 8,
            "cr": "R",
            "l3type": "NetRom",
            "l3src": "G8PZT",
            "l3dst": "G8PZT-1",
            "ttl": 4,
            "l4type": "INFO",
            "toCct": 4,
            "txSeq": 0,
            "rxSeq": 1,
            "infoLen": 236
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("2");
        frame.Source.Should().Be("G8PZT");
        frame.Destination.Should().Be("G8PZT-1");
        frame.Control.Should().Be(0);
        frame.L2Type.Should().Be("I");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("R");
        frame.L3Type.Should().Be("NetRom");
        frame.L3Source.Should().Be("G8PZT");
        frame.L3Destination.Should().Be("G8PZT-1");
        frame.TimeToLive.Should().Be(4);
        frame.L4Type.Should().Be("INFO");
        frame.ToCircuit.Should().Be(4);
        frame.TransmitSequenceNumber.Should().Be(0);
        frame.ReceiveSequenceNumber.Should().Be(1);
        frame.InfoFieldLength.Should().Be(236);
    }

    [Fact]
    public void Should_Deserialize_NetRom_Info_Ack_Example()
    {
        // Arrange
        var json = """
        {
            "port": "2",
            "srce": "G8PZT-1",
            "dest": "G8PZT",
            "ctrl": 0,
            "l2type": "I",
            "modulo": 8,
            "cr": "C",
            "l3type": "NetRom",
            "l3src": "G8PZT-1",
            "l3dst": "G8PZT",
            "ttl": 25,
            "l4type": "INFO ACK",
            "toCct": 4888,
            "rxSeq": 1
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("2");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("G8PZT");
        frame.Control.Should().Be(0);
        frame.L2Type.Should().Be("I");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("C");
        frame.L3Type.Should().Be("NetRom");
        frame.L3Source.Should().Be("G8PZT-1");
        frame.L3Destination.Should().Be("G8PZT");
        frame.TimeToLive.Should().Be(25);
        frame.L4Type.Should().Be("INFO ACK");
        frame.ToCircuit.Should().Be(4888);
        frame.ReceiveSequenceNumber.Should().Be(1);
    }

    [Fact]
    public void Should_Deserialize_NetRom_Disconnect_Request_Example()
    {
        // Arrange
        var json = """
        {
            "port": "2",
            "srce": "G8PZT",
            "dest": "G8PZT-1",
            "ctrl": 0,
            "l2type": "I",
            "modulo": 8,
            "cr": "R",
            "l3type": "NetRom",
            "l3src": "G8PZT",
            "l3dst": "G8PZT-1",
            "ttl": 4,
            "l4type": "DISC REQ",
            "toCct": 4
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("2");
        frame.Source.Should().Be("G8PZT");
        frame.Destination.Should().Be("G8PZT-1");
        frame.Control.Should().Be(0);
        frame.L2Type.Should().Be("I");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("R");
        frame.L3Type.Should().Be("NetRom");
        frame.L3Source.Should().Be("G8PZT");
        frame.L3Destination.Should().Be("G8PZT-1");
        frame.TimeToLive.Should().Be(4);
        frame.L4Type.Should().Be("DISC REQ");
        frame.ToCircuit.Should().Be(4);
    }

    [Fact]
    public void Should_Deserialize_NetRom_Disconnect_Acknowledgement_Example()
    {
        // Arrange
        var json = """
        {
            "port": "2",
            "srce": "G8PZT-1",
            "dest": "G8PZT",
            "ctrl": 0,
            "l2type": "I",
            "modulo": 8,
            "cr": "C",
            "l3type": "NetRom",
            "l3src": "G8PZT-1",
            "l3dst": "G8PZT",
            "ttl": 25,
            "l4type": "DISC ACK",
            "toCct": 4888
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("2");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("G8PZT");
        frame.Control.Should().Be(0);
        frame.L2Type.Should().Be("I");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("C");
        frame.L3Type.Should().Be("NetRom");
        frame.L3Source.Should().Be("G8PZT-1");
        frame.L3Destination.Should().Be("G8PZT");
        frame.TimeToLive.Should().Be(25);
        frame.L4Type.Should().Be("DISC ACK");
        frame.ToCircuit.Should().Be(4888);
    }

    [Fact]
    public void Should_Deserialize_INP3_Routing_Info_Example()
    {
        // Arrange
        var json = """
        {
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "NODES",
            "ctrl": 3,
            "l2type": "UI",
            "modulo": 8,
            "cr": "C",
            "pid": 207,
            "ptcl": "NET/ROM",
            "l3type": "Routing info",
            "type": "INP3",
            "nodes": [
                {
                    "call": "OH5RM-10",
                    "hops": 31,
                    "tt": 60000
                },
                {
                    "call": "GB7JD-8",
                    "hops": 2,
                    "tt": 2,
                    "alias": "JEDCHT",
                    "ipAddr": "44.131.8.1",
                    "bitMask": 32,
                    "tcpPort": 3600,
                    "latitude": 55.3125,
                    "longitude": -2.3250,
                    "software": "XRLin",
                    "version": "504i",
                    "isNode": false,
                    "isXRCHAT": true,
                    "isRTCHAT": true,
                    "localTime": "2025-10-07T03:43:04Z"
                }
            ]
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("1");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("NODES");
        frame.Control.Should().Be(3);
        frame.L2Type.Should().Be("UI");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("C");
        frame.ProtocolId.Should().Be(207);
        frame.ProtocolName.Should().Be("NET/ROM");
        frame.L3Type.Should().Be("Routing info");
        frame.Type.Should().Be("INP3");

        frame.Nodes.Should().NotBeNull();
        frame.Nodes.Should().HaveCount(2);

        var firstNode = frame.Nodes![0];
        firstNode.Callsign.Should().Be("OH5RM-10");
        firstNode.Hops.Should().Be(31);
        firstNode.OneWayTripTimeIn10msIncrements.Should().Be(60000);

        var secondNode = frame.Nodes![1];
        secondNode.Callsign.Should().Be("GB7JD-8");
        secondNode.Hops.Should().Be(2);
        secondNode.OneWayTripTimeIn10msIncrements.Should().Be(2);
        secondNode.Alias.Should().Be("JEDCHT");
        secondNode.IpAddress.Should().Be("44.131.8.1");
        secondNode.BitMask.Should().Be(32);
        secondNode.TcpPort.Should().Be(3600);
        secondNode.Latitude.Should().Be(55.3125m);
        secondNode.Longitude.Should().Be(-2.3250m);
        secondNode.Software.Should().Be("XRLin");
        secondNode.Version.Should().Be("504i");
        secondNode.IsNode.Should().BeFalse();
        secondNode.IsXrchat.Should().BeTrue();
        secondNode.IsRtchat.Should().BeTrue();
        secondNode.LocalTime.Should().Be(DateTimeOffset.Parse("2025-10-07T03:43:04Z"));
    }

    [Fact]
    public void Should_Deserialize_Frame_With_Digipeaters()
    {
        // Arrange - This example includes digipeaters (not in the spec examples, but mentioned in the spec)
        var json = """
        {
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "G8PZT-2",
            "ctrl": 3,
            "l2type": "UI",
            "modulo": 8,
            "cr": "C",
            "digis": [
                {
                    "call": "GB7RDG",
                    "rptd": true
                },
                {
                    "call": "GB7MBC-2",
                    "rptd": false
                }
            ],
            "pid": 240,
            "ptcl": "DATA"
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("1");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("G8PZT-2");
        frame.Control.Should().Be(3);
        frame.L2Type.Should().Be("UI");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("C");
        frame.ProtocolId.Should().Be(240);
        frame.ProtocolName.Should().Be("DATA");

        frame.Digipeaters.Should().NotBeNull();
        frame.Digipeaters.Should().HaveCount(2);

        var firstDigi = frame.Digipeaters![0];
        firstDigi.Callsign.Should().Be("GB7RDG");
        firstDigi.Repeated.Should().BeTrue();

        var secondDigi = frame.Digipeaters![1];
        secondDigi.Callsign.Should().Be("GB7MBC-2");
        secondDigi.Repeated.Should().BeFalse();
    }

    [Fact]
    public void Should_Deserialize_NetRom_Routing_Info_Example()
    {
        // Arrange - NetRom routing info example (based on spec description)
        var json = """
        {
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "NODES",
            "ctrl": 3,
            "l2type": "UI",
            "modulo": 8,
            "cr": "C",
            "pid": 207,
            "ptcl": "NET/ROM",
            "l3type": "Routing info",
            "type": "NETROM",
            "fromAlias": "G8PZT1",
            "nodes": [
                {
                    "call": "GB7RDG",
                    "alias": "RDGHAM",
                    "via": "GB7RDG",
                    "qual": 255
                },
                {
                    "call": "GB7MBC",
                    "alias": "MBCNET",
                    "via": "GB7RDG-1",
                    "qual": 192
                }
            ]
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("1");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("NODES");
        frame.Control.Should().Be(3);
        frame.L2Type.Should().Be("UI");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("C");
        frame.ProtocolId.Should().Be(207);
        frame.ProtocolName.Should().Be("NET/ROM");
        frame.L3Type.Should().Be("Routing info");
        frame.Type.Should().Be("NETROM");
        frame.FromAlias.Should().Be("G8PZT1");

        frame.Nodes.Should().NotBeNull();
        frame.Nodes.Should().HaveCount(2);

        var firstNode = frame.Nodes![0];
        firstNode.Callsign.Should().Be("GB7RDG");
        firstNode.Alias.Should().Be("RDGHAM");
        firstNode.Via.Should().Be("GB7RDG");
        firstNode.Quality.Should().Be(255);

        var secondNode = frame.Nodes![1];
        secondNode.Callsign.Should().Be("GB7MBC");
        secondNode.Alias.Should().Be("MBCNET");
        secondNode.Via.Should().Be("GB7RDG-1");
        secondNode.Quality.Should().Be(192);
    }

    [Fact]
    public void Should_Handle_Missing_Optional_Fields()
    {
        // Arrange - Minimal frame with only required fields
        var json = """
        {
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "G8PZT-2",
            "ctrl": 3,
            "l2type": "UI",
            "cr": "C",
            "modulo": null
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("1");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("G8PZT-2");
        frame.Control.Should().Be(3);
        frame.L2Type.Should().Be("UI");
        frame.CommandResponse.Should().Be("C");
        frame.Modulo.Should().BeNull();

        // All other optional fields should be null/default
        frame.Digipeaters.Should().BeNull();
        frame.ReceiveSequence.Should().BeNull();
        frame.TransmitSequence.Should().BeNull();
        frame.PollFinal.Should().BeNull();
        frame.ProtocolId.Should().BeNull();
        frame.ProtocolName.Should().BeNull();
        frame.IFieldLength.Should().BeNull();
        frame.L3Type.Should().BeNull();
        frame.L3Source.Should().BeNull();
        frame.L3Destination.Should().BeNull();
        frame.TimeToLive.Should().BeNull();
        frame.L4Type.Should().BeNull();
        frame.Nodes.Should().BeNull();
    }

    [Fact]
    public void Should_Handle_Port_As_String_Name()
    {
        // Arrange - Port specified as a named port instead of number (from spec example)
        var json = """
        {
            "port": "4mlink",
            "srce": "G8PZT-1",
            "dest": "CQ",
            "ctrl": 3,
            "l2type": "UI",
            "modulo": 8,
            "cr": "C"
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("4mlink");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("CQ");
        frame.Control.Should().Be(3);
        frame.L2Type.Should().Be("UI");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("C");
    }

    [Fact]
    public void Should_Deserialize_Extended_Connect_Request_With_All_NetRom_Fields()
    {
        // Arrange - NetRom Extended Connect Request with additional fields
        var json = """
        {
            "port": "3",
            "srce": "G0ABC-1",
            "dest": "GB7XYZ",
            "ctrl": 47,
            "l2type": "I",
            "modulo": 128,
            "rseq": 0,
            "tseq": 0,
            "cr": "C",
            "ilen": 42,
            "pid": 207,
            "ptcl": "NET/ROM",
            "l3type": "NetRom",
            "l3src": "G0ABC-1",
            "l3dst": "GB7XYZ-8",
            "ttl": 30,
            "l4type": "CONN REQX",
            "fromCct": 1234,
            "srcUser": "G0ABC-4",
            "srcNode": "G0ABC-1",
            "service": 7,
            "window": 7,
            "l4t1": 180,
            "bpqSpy": 1
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("3");
        frame.Source.Should().Be("G0ABC-1");
        frame.Destination.Should().Be("GB7XYZ");
        frame.Control.Should().Be(47);
        frame.L2Type.Should().Be("I");
        frame.Modulo.Should().Be(128);
        frame.ReceiveSequence.Should().Be(0);
        frame.TransmitSequence.Should().Be(0);
        frame.CommandResponse.Should().Be("C");
        frame.IFieldLength.Should().Be(42);
        frame.ProtocolId.Should().Be(207);
        frame.ProtocolName.Should().Be("NET/ROM");
        frame.L3Type.Should().Be("NetRom");
        frame.L3Source.Should().Be("G0ABC-1");
        frame.L3Destination.Should().Be("GB7XYZ-8");
        frame.TimeToLive.Should().Be(30);
        frame.L4Type.Should().Be("CONN REQX");
        frame.FromCircuit.Should().Be(1234);
        frame.OriginatingUserCallsign.Should().Be("G0ABC-4");
        frame.OriginatingNodeCallsign.Should().Be("G0ABC-1");
        frame.NetRomXServiceNumber.Should().Be(7);
        frame.ProposedWindow.Should().Be(7);
        frame.Layer4T1Timer.Should().Be(180);
        frame.BpqExtension.Should().Be(1);
    }

    [Fact]
    public void Should_Deserialize_Frame_With_Flags()
    {
        // Arrange - Frame with choke, nak, and more flags set
        var json = """
        {
            "port": "4",
            "srce": "TEST-1",
            "dest": "TEST-2",
            "ctrl": 16,
            "l2type": "I",
            "modulo": 8,
            "cr": "C",
            "chokeFlag": true,
            "nakFlag": false,
            "moreFlag": true
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("4");
        frame.Source.Should().Be("TEST-1");
        frame.Destination.Should().Be("TEST-2");
        frame.Control.Should().Be(16);
        frame.L2Type.Should().Be("I");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("C");
        frame.ChokeFlag.Should().BeTrue();
        frame.NakFlag.Should().BeFalse();
        frame.MoreFlag.Should().BeTrue();
    }

    [Fact]
    public void Should_Deserialize_Frame_With_Complete_Node_Information()
    {
        // Arrange - Node with all possible fields populated
        var json = """
        {
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "NODES",
            "ctrl": 3,
            "l2type": "UI",
            "cr": "C",
            "pid": 207,
            "ptcl": "NET/ROM",
            "l3type": "Routing info",
            "type": "INP3",
            "modulo": 8,
            "nodes": [
                {
                    "call": "GB7TEST",
                    "alias": "TESTND",
                    "via": "GB7RDG",
                    "qual": 200,
                    "hops": 3,
                    "tt": 150,
                    "ipAddr": "44.131.1.1",
                    "bitMask": 24,
                    "tcpPort": 8010,
                    "latitude": 51.5074,
                    "longitude": -0.1278,
                    "software": "LinBPQ",
                    "version": "6.0.24",
                    "isNode": true,
                    "isBBS": true,
                    "isPMS": false,
                    "isXRCHAT": false,
                    "isRTCHAT": true,
                    "isRMS": true,
                    "isDXCLUS": false,
                    "tzMins": 0,
                    "localTime": "2025-12-25T12:00:00Z"
                }
            ]
        }
        """;

        // Act
        var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json, JsonOptions);

        // Assert
        frame.Port.Should().Be("1");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("NODES");
        frame.Control.Should().Be(3);
        frame.L2Type.Should().Be("UI");
        frame.CommandResponse.Should().Be("C");
        frame.ProtocolId.Should().Be(207);
        frame.ProtocolName.Should().Be("NET/ROM");
        frame.L3Type.Should().Be("Routing info");
        frame.Type.Should().Be("INP3");
        frame.Modulo.Should().Be(8);

        frame.Nodes.Should().NotBeNull().And.HaveCount(1);

        var node = frame.Nodes![0];
        node.Callsign.Should().Be("GB7TEST");
        node.Alias.Should().Be("TESTND");
        node.Via.Should().Be("GB7RDG");
        node.Quality.Should().Be(200);
        node.Hops.Should().Be(3);
        node.OneWayTripTimeIn10msIncrements.Should().Be(150);
        node.IpAddress.Should().Be("44.131.1.1");
        node.BitMask.Should().Be(24);
        node.TcpPort.Should().Be(8010);
        node.Latitude.Should().Be(51.5074m);
        node.Longitude.Should().Be(-0.1278m);
        node.Software.Should().Be("LinBPQ");
        node.Version.Should().Be("6.0.24");
        node.IsNode.Should().BeTrue();
        node.IsBbs.Should().BeTrue();
        node.IsPms.Should().BeFalse();
        node.IsXrchat.Should().BeFalse();
        node.IsRtchat.Should().BeTrue();
        node.IsRms.Should().BeTrue();
        node.IsDxcluster.Should().BeFalse();
        node.TimeZoneMinutesOffsetFromGmt.Should().Be(0);
        node.LocalTime.Should().Be(DateTimeOffset.Parse("2025-12-25T12:00:00Z"));
    }
}