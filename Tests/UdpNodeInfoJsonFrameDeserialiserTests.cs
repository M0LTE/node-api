using FluentAssertions;
using node_api;
using node_api.Models;
using System.Text.Json;

namespace Tests;

public class UdpNodeInfoJsonFrameDeserialiserTests
{
    [Fact]
    public void Should_Deserialize_UI_Frame_Example()
    {
        // Arrange
        var json = """
        {
            "@type": "l2trace",
            "port": "2",
            "srce": "G8PZT-9",
            "dest": "KIDDER",
            "ctrl": 193,
            "l2Type": "RR",
            "modulo": 8,
            "rseq": 6,
            "cr": "R"
        }
        """;

        // Act
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert
        frame.DatagramType.Should().Be("l2trace");
        frame.Port.Should().Be("2");
        frame.Source.Should().Be("G8PZT-9");
        frame.Destination.Should().Be("KIDDER");
        frame.Control.Should().Be(193);
        frame.L2Type.Should().Be("RR");
        frame.Modulo.Should().Be(8);
        frame.ReceiveSequence.Should().Be(6);
        frame.CommandResponse.Should().Be("R");
        
        parsed.Should().BeTrue();
    }

    [Fact]
    public void Should_Deserialize_L2TraceWithDigis()
    {
        // Arrange
        var json = """
        {
        "@type": "l2trace",
        "port": "2",
        "srce": "G8PZT",
        "dest": "G8PZT-1",
        "ctrl": 136,
        "l2Type": "I",
        "modulo": 8,
        "rseq": 4,
        "tseq": 4,
        "cr": "C",
        "ilen": 63,
        "pid": 207,
        "ptcl": "NET/ROM",
        "l3Type": "Routing info",
        "type": "INP3",
        "nodes": [
        {
        "call": "VA3BAL-8",
        "hops": 3,
        "tt": 11,
        "alias": "XBAL",
        "ipAddr": "44.135.92.103",
        "bitMask": 32
        },
        {
        "call": "SM0YOS",
        "hops": 3,
        "tt": 67,
        "alias": "YOSNOD"
        },
        {
        "call": "VE7ASS-7",
        "hops": 3,
        "tt": 44,
        "alias": "YVRASS"
        }
        ]
        }
        """;

        // Act
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var frameUntyped, out _);
        var frame = frameUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert
        frame.DatagramType.Should().Be("l2trace");
        frame.Port.Should().Be("2");
        frame.Source.Should().Be("G8PZT");
        frame.Destination.Should().Be("G8PZT-1");
        frame.Control.Should().Be(136);
        frame.L2Type.Should().Be("I");
        frame.Modulo.Should().Be(8);
        frame.ReceiveSequence.Should().Be(4);
        frame.TransmitSequence.Should().Be(4);
        frame.CommandResponse.Should().Be("C");
        frame.IFieldLength.Should().Be(63);
        frame.ProtocolId.Should().Be(207);
        frame.ProtocolName.Should().Be("NET/ROM");
        frame.L3Type.Should().Be("Routing info");
        frame.Type.Should().Be("INP3");
        
        // Assert nodes array
        frame.Nodes.Should().NotBeNull();
        frame.Nodes.Should().HaveCount(3);
        
        // First node
        frame.Nodes![0].Callsign.Should().Be("VA3BAL-8");
        frame.Nodes[0].Hops.Should().Be(3);
        frame.Nodes[0].OneWayTripTimeIn10msIncrements.Should().Be(11);
        frame.Nodes[0].Alias.Should().Be("XBAL");
        frame.Nodes[0].IpAddress.Should().Be("44.135.92.103");
        frame.Nodes[0].BitMask.Should().Be(32);
        
        // Second node
        frame.Nodes[1].Callsign.Should().Be("SM0YOS");
        frame.Nodes[1].Hops.Should().Be(3);
        frame.Nodes[1].OneWayTripTimeIn10msIncrements.Should().Be(67);
        frame.Nodes[1].Alias.Should().Be("YOSNOD");
        frame.Nodes[1].IpAddress.Should().BeNull();
        frame.Nodes[1].BitMask.Should().BeNull();
        
        // Third node
        frame.Nodes[2].Callsign.Should().Be("VE7ASS-7");
        frame.Nodes[2].Hops.Should().Be(3);
        frame.Nodes[2].OneWayTripTimeIn10msIncrements.Should().Be(44);
        frame.Nodes[2].Alias.Should().Be("YVRASS");
        frame.Nodes[2].IpAddress.Should().BeNull();
        frame.Nodes[2].BitMask.Should().BeNull();
        
        parsed.Should().BeTrue();
    }

    #region AX25 Layer 2 Trace Examples from Spec

    [Fact]
    public void Should_Deserialize_Spec_Example_Unnumbered_Information_Frame()
    {
        // Example from specification section 1.1
        var json = """
        {
        "@type": "l2trace",
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
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert
        frame.DatagramType.Should().Be("l2trace");
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
        
        parsed.Should().BeTrue();
    }

    [Fact]
    public void Should_Deserialize_Spec_Example_AX25_Connect_Request()
    {
        // Example from specification section 1.1
        var json = """
        {
        "@type": "l2trace",
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
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert
        frame.DatagramType.Should().Be("l2trace");
        frame.Port.Should().Be("8");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("G8PZT-14");
        frame.Control.Should().Be(63);
        frame.L2Type.Should().Be("C");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("C");
        frame.PollFinal.Should().Be("P");
        
        parsed.Should().BeTrue();
    }

    [Fact]
    public void Should_Deserialize_Spec_Example_AX25_Unnumbered_Acknowledgement()
    {
        // Example from specification section 1.1
        var json = """
        {
        "@type": "l2trace",
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
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert
        frame.DatagramType.Should().Be("l2trace");
        frame.Port.Should().Be("8");
        frame.Source.Should().Be("G8PZT-14");
        frame.Destination.Should().Be("G8PZT-1");
        frame.Control.Should().Be(115);
        frame.L2Type.Should().Be("UA");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("R");
        frame.PollFinal.Should().Be("F");
        
        parsed.Should().BeTrue();
    }

    [Fact]
    public void Should_Deserialize_Spec_Example_Numbered_Information_Frame()
    {
        // Example from specification section 1.1
        var json = """
        {
        "@type": "l2trace",
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
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert
        frame.DatagramType.Should().Be("l2trace");
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
        
        parsed.Should().BeTrue();
    }

    [Fact]
    public void Should_Deserialize_Spec_Example_Supervisory_Receive_Ready_Frame()
    {
        // Example from specification section 1.1
        var json = """
        {
        "@type": "l2trace",
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
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert
        frame.DatagramType.Should().Be("l2trace");
        frame.Port.Should().Be("2");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("G8PZT");
        frame.Control.Should().Be(161);
        frame.L2Type.Should().Be("RR");
        frame.Modulo.Should().Be(8);
        frame.ReceiveSequence.Should().Be(5);
        frame.CommandResponse.Should().Be("R");
        
        parsed.Should().BeTrue();
    }

    [Fact]
    public void Should_Deserialize_Spec_Example_UI_Frame_Containing_Non_Text_Data()
    {
        // Example from specification section 1.1
        var json = """
        {
        "@type": "l2trace",
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
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert
        frame.DatagramType.Should().Be("l2trace");
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
        
        parsed.Should().BeTrue();
    }

    #endregion

    #region NetRom Layer 3/4 Examples from Spec

    [Fact]
    public void Should_Deserialize_Spec_Example_NetRom_Layer4_Connect_Request()
    {
        // Example from specification section 2.1.1
        var json = """
        {
        "@type": "l2trace",
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
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert L2 fields
        frame.DatagramType.Should().Be("l2trace");
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

        // Assert L3/L4 fields
        frame.L3Type.Should().Be("NetRom");
        frame.L3Source.Should().Be("G8PZT-1");
        frame.L3Destination.Should().Be("G8PZT");
        frame.TimeToLive.Should().Be(25);
        frame.L4Type.Should().Be("CONN REQ");
        frame.FromCircuit.Should().Be(4);
        frame.OriginatingUserCallsign.Should().Be("G8PZT-4");
        frame.OriginatingNodeCallsign.Should().Be("G8PZT-1");
        frame.ProposedWindow.Should().Be(4);
        
        parsed.Should().BeTrue();
    }

    [Fact]
    public void Should_Deserialize_Spec_Example_NetRom_Layer4_Connect_Acknowledgement()
    {
        // Example from specification section 2.1.1
        var json = """
        {
        "@type": "l2trace",
        "l3type": "NetRom",
        "l3src": "G8PZT",
        "l3dst": "G8PZT-1",
        "ttl": 4,
        "l4type": "CONN ACK",
        "toCct": 4,
        "fromCct": 4888,
        "accWin": 4,
        "port": "1",
        "srce": "G8PZT",
        "dest": "G8PZT-1",
        "ctrl": 1,
        "l2type": "UI",
        "modulo": 8,
        "cr": "C"
        }
        """;

        // Act
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert L2 fields
        frame.DatagramType.Should().Be("l2trace");
        frame.Port.Should().Be("1");
        frame.Source.Should().Be("G8PZT");
        frame.Destination.Should().Be("G8PZT-1");
        frame.Control.Should().Be(1);
        frame.L2Type.Should().Be("UI");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("C");

        // Assert L3/L4 fields
        frame.L3Type.Should().Be("NetRom");
        frame.L3Source.Should().Be("G8PZT");
        frame.L3Destination.Should().Be("G8PZT-1");
        frame.TimeToLive.Should().Be(4);
        frame.L4Type.Should().Be("CONN ACK");
        frame.ToCircuit.Should().Be(4);
        frame.FromCircuit.Should().Be(4888);
        frame.AcceptableWindow.Should().Be(4);
        
        parsed.Should().BeTrue();
    }

    [Fact]
    public void Should_Deserialize_Spec_Example_NetRom_Layer4_INFO_Frame()
    {
        // Example from specification section 2.1.1 (modified to include required L2 fields)
        var json = """
        {
        "@type": "l2trace",
        "l3type": "NetRom",
        "l3src": "G8PZT",
        "l3dst": "G8PZT-1",
        "ttl": 4,
        "l4type": "INFO",
        "toCct": 4,
        "txSeq": 0,
        "rxSeq": 1,
        "infoLen": 236,
        "port": "1",
        "srce": "G8PZT",
        "dest": "G8PZT-1",
        "ctrl": 1,
        "l2type": "UI",
        "modulo": 8,
        "cr": "C"
        }
        """;

        // Act
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert L2 fields
        frame.DatagramType.Should().Be("l2trace");
        frame.Port.Should().Be("1");
        frame.Source.Should().Be("G8PZT");
        frame.Destination.Should().Be("G8PZT-1");
        frame.Control.Should().Be(1);
        frame.L2Type.Should().Be("UI");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("C");

        // Assert L3/L4 fields
        frame.L3Type.Should().Be("NetRom");
        frame.L3Source.Should().Be("G8PZT");
        frame.L3Destination.Should().Be("G8PZT-1");
        frame.TimeToLive.Should().Be(4);
        frame.L4Type.Should().Be("INFO");
        frame.ToCircuit.Should().Be(4);
        frame.TransmitSequenceNumber.Should().Be(0);
        frame.ReceiveSequenceNumber.Should().Be(1);
        frame.InfoFieldLength.Should().Be(236);
        
        parsed.Should().BeTrue();
    }

    [Fact]
    public void Should_Deserialize_Spec_Example_NetRom_Layer4_INFO_Acknowledgement()
    {
        // Example from specification section 2.1.1 (modified to include required L2 fields)
        var json = """
        {
        "@type": "l2trace",
        "l3type": "NetRom",
        "l3src": "G8PZT-1",
        "l3dst": "G8PZT",
        "ttl": 25,
        "l4type": "INFO ACK",
        "toCct": 4888,
        "rxSeq": 1,
        "port": "1",
        "srce": "G8PZT-1",
        "dest": "G8PZT",
        "ctrl": 1,
        "l2type": "UI",
        "modulo": 8,
        "cr": "C"
        }
        """;
        
        // Act
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert L2 fields
        frame.DatagramType.Should().Be("l2trace");
        frame.Port.Should().Be("1");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("G8PZT");
        frame.Control.Should().Be(1);
        frame.L2Type.Should().Be("UI");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("C");

        // Assert L3/L4 fields
        frame.L3Type.Should().Be("NetRom");
        frame.L3Source.Should().Be("G8PZT-1");
        frame.L3Destination.Should().Be("G8PZT");
        frame.TimeToLive.Should().Be(25);
        frame.L4Type.Should().Be("INFO ACK");
        frame.ToCircuit.Should().Be(4888);
        frame.ReceiveSequenceNumber.Should().Be(1);
        
        parsed.Should().BeTrue();
    }

    [Fact]
    public void Should_Deserialize_Spec_Example_NetRom_Layer4_Disconnect_Request()
    {
        // Example from specification section 2.1.1 (modified to include required L2 fields)
        var json = """
        {
        "@type": "l2trace",
        "l3type": "NetRom",
        "l3src": "G8PZT",
        "l3dst": "G8PZT-1",
        "ttl": 4,
        "l4type": "DISC REQ",
        "toCct": 4,
        "port": "1",
        "srce": "G8PZT",
        "dest": "G8PZT-1",
        "ctrl": 1,
        "l2type": "UI",
        "modulo": 8,
        "cr": "C"
        }
        """;
        
        // Act
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert L2 fields
        frame.DatagramType.Should().Be("l2trace");
        frame.Port.Should().Be("1");
        frame.Source.Should().Be("G8PZT");
        frame.Destination.Should().Be("G8PZT-1");
        frame.Control.Should().Be(1);
        frame.L2Type.Should().Be("UI");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("C");

        // Assert L3/L4 fields
        frame.L3Type.Should().Be("NetRom");
        frame.L3Source.Should().Be("G8PZT");
        frame.L3Destination.Should().Be("G8PZT-1");
        frame.TimeToLive.Should().Be(4);
        frame.L4Type.Should().Be("DISC REQ");
        frame.ToCircuit.Should().Be(4);
        
        parsed.Should().BeTrue();
    }

    [Fact]
    public void Should_Deserialize_Spec_Example_NetRom_Layer4_Disconnect_Acknowledgement()
    {
        // Example from specification section 2.1.1 (modified to include required L2 fields)
        var json = """
        {
        "@type": "l2trace",
        "l3type": "NetRom",
        "l3src": "G8PZT-1",
        "l3dst": "G8PZT",
        "ttl": 25,
        "l4type": "DISC ACK",
        "toCct": 4888,
        "port": "1",
        "srce": "G8PZT-1",
        "dest": "G8PZT",
        "ctrl": 1,
        "l2type": "UI",
        "modulo": 8,
        "cr": "C"
        }
        """;
        
        // Act
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert L2 fields
        frame.DatagramType.Should().Be("l2trace");
        frame.Port.Should().Be("1");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("G8PZT");
        frame.Control.Should().Be(1);
        frame.L2Type.Should().Be("UI");
        frame.Modulo.Should().Be(8);
        frame.CommandResponse.Should().Be("C");

        // Assert L3/L4 fields
        frame.L3Type.Should().Be("NetRom");
        frame.L3Source.Should().Be("G8PZT-1");
        frame.L3Destination.Should().Be("G8PZT");
        frame.TimeToLive.Should().Be(25);
        frame.L4Type.Should().Be("DISC ACK");
        frame.ToCircuit.Should().Be(4888);
        
        parsed.Should().BeTrue();
    }

    #endregion

    #region Routing Info Examples from Spec

    [Fact]
    public void Should_Deserialize_Spec_Example_INP3_Routing_Info()
    {
        // Example from specification section 2.2.2
        var json = """
        {
        "@type": "l2trace",
        "l3Type": "Routing info",
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
        ],
        "port": "1",
        "srce": "G8PZT",
        "dest": "NODES",
        "ctrl": 1,
        "l2type": "UI",
        "cr": "C",
        "modulo": 8
        }
        """;


        // Act
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert L2 fields
        frame.DatagramType.Should().Be("l2trace");
        frame.Port.Should().Be("1");
        frame.Source.Should().Be("G8PZT");
        frame.Destination.Should().Be("NODES");
        frame.Control.Should().Be(1);
        frame.L2Type.Should().Be("UI");
        frame.CommandResponse.Should().Be("C");
        frame.Modulo.Should().Be(8);

        // Assert routing info fields
        frame.L3Type.Should().Be("Routing info");
        frame.Type.Should().Be("INP3");
        frame.Nodes.Should().NotBeNull();
        frame.Nodes.Should().HaveCount(2);

        // First node - minimal fields
        var firstNode = frame.Nodes![0];
        firstNode.Callsign.Should().Be("OH5RM-10");
        firstNode.Hops.Should().Be(31);
        firstNode.OneWayTripTimeIn10msIncrements.Should().Be(60000);
        firstNode.Alias.Should().BeNull();

        // Second node - full fields
        var secondNode = frame.Nodes[1];
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
        secondNode.IsNode.Should().Be(false);
        secondNode.IsXrchat.Should().Be(true);
        secondNode.IsRtchat.Should().Be(true);
        secondNode.LocalTime.Should().Be(DateTimeOffset.Parse("2025-10-07T03:43:04Z"));

        parsed.Should().BeTrue();
    }

    #endregion

    #region Event Examples from Spec

    [Fact]
    public void Should_Deserialize_Spec_Example_Connection_Event()
    {
        // Example from specification section 3.1
        var json = """
        {
        "@type": "event",
        "eventSource": "link",
        "time": "2025-10-07T16:43:17Z",
        "id": 3,
        "direction": "outgoing",
        "port": "2",
        "remote": "KIDDER-1",
        "local": "G8PZT-11",
        "event": "connect"
        }
        """;

        // Act
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var eventFrame = datagramUntyped.Should().BeOfType<Event>().Subject;

        // Assert
        eventFrame.DatagramType.Should().Be("event");
        eventFrame.EventSource.Should().Be("link");
        eventFrame.Time.Should().Be(DateTimeOffset.Parse("2025-10-07T16:43:17Z"));
        eventFrame.Id.Should().Be(3);
        eventFrame.Direction.Should().Be("outgoing");
        eventFrame.Port.Should().Be("2");
        eventFrame.RemoteCallsign.Should().Be("KIDDER-1");
        eventFrame.LocalCallsign.Should().Be("G8PZT-11");
        eventFrame.EventType.Should().Be("connect");
        
        parsed.Should().BeTrue();
    }

    [Fact]
    public void Should_Deserialize_Spec_Example_Disconnection_Event()
    {
        // Example from specification section 3.1
        var json = """
        {
        "@type": "event",
        "eventSource": "link",
        "time": "2025-10-07T16:43:23Z",
        "id": 3,
        "direction": "outgoing",
        "port": "2",
        "remote": "KIDDER-1",
        "local": "G8PZT-11",
        "event": "disconnect"
        }
        """;

        // Act
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var eventFrame = datagramUntyped.Should().BeOfType<Event>().Subject;

        // Assert
        eventFrame.DatagramType.Should().Be("event");
        eventFrame.EventSource.Should().Be("link");
        eventFrame.Time.Should().Be(DateTimeOffset.Parse("2025-10-07T16:43:23Z"));
        eventFrame.Id.Should().Be(3);
        eventFrame.Direction.Should().Be("outgoing");
        eventFrame.Port.Should().Be("2");
        eventFrame.RemoteCallsign.Should().Be("KIDDER-1");
        eventFrame.LocalCallsign.Should().Be("G8PZT-11");
        eventFrame.EventType.Should().Be("disconnect");
        
        parsed.Should().BeTrue();
    }

    #endregion

    #region Additional Test Cases

    [Fact]
    public void Should_Deserialize_NetRom_Routing_Info_Example()
    {
        // Test NetRom routing info frame
        var json = """
        {
        "@type": "l2trace",
        "port": "1",
        "srce": "G8PZT-1",
        "dest": "NODES",
        "ctrl": 3,
        "l2type": "UI",
        "cr": "C",
        "ilen": 100,
        "pid": 207,
        "ptcl": "NET/ROM",
        "l3type": "Routing info",
        "type": "NETROM",
        "fromAlias": "G8PZT-NODE",
        "nodes": [
            {
            "call": "G8PZT-2",
            "alias": "NODE2",
            "via": "G8PZT-3",
            "qual": 200
            },
            {
            "call": "G8PZT-4", 
            "alias": "NODE4",
            "via": "G8PZT-5",
            "qual": 150
            }
        ],
        "modulo": 8
        }
        """;


        // Act
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert L2 fields
        frame.DatagramType.Should().Be("l2trace");
        frame.Port.Should().Be("1");
        frame.Source.Should().Be("G8PZT-1");
        frame.Destination.Should().Be("NODES");
        frame.Control.Should().Be(3);
        frame.L2Type.Should().Be("UI");
        frame.CommandResponse.Should().Be("C");
        frame.IFieldLength.Should().Be(100);
        frame.ProtocolId.Should().Be(207);
        frame.ProtocolName.Should().Be("NET/ROM");
        frame.Modulo.Should().Be(8);

        // Assert routing info fields
        frame.L3Type.Should().Be("Routing info");
        frame.Type.Should().Be("NETROM");
        frame.FromAlias.Should().Be("G8PZT-NODE");
        frame.Nodes.Should().NotBeNull();
        frame.Nodes.Should().HaveCount(2);

        // First node
        frame.Nodes![0].Callsign.Should().Be("G8PZT-2");
        frame.Nodes[0].Alias.Should().Be("NODE2");
        frame.Nodes[0].Via.Should().Be("G8PZT-3");
        frame.Nodes[0].Quality.Should().Be(200);
        
        // Second node
        frame.Nodes[1].Callsign.Should().Be("G8PZT-4");
        frame.Nodes[1].Alias.Should().Be("NODE4");
        frame.Nodes[1].Via.Should().Be("G8PZT-5");
        frame.Nodes[1].Quality.Should().Be(150);

        parsed.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_False_For_Invalid_JSON()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act & Assert
        UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(invalidJson, out var frame, out var exception).Should().BeFalse();
        frame.Should().BeNull();
        exception.Should().NotBeNull();
        exception.Should().BeAssignableTo<JsonException>();
    }

    [Fact]
    public void Should_Return_False_For_Unknown_Type()
    {
        // Arrange
        var json = """
        {
        "@type": "unknown_type",
        "someField": "someValue"
        }
        """;

        // Act & Assert
        UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var frame, out _).Should().BeFalse();
        frame.Should().BeNull();
    }

    [Fact]
    public void Should_Return_False_For_Missing_Type_Field()
    {
        // Arrange
        var json = """
        {
        "port": "1",
        "srce": "TEST",
        "dest": "TEST2"
        }
        """;

        // Act & Assert
        UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var frame, out _).Should().BeFalse();
        frame.Should().BeNull();
    }

    [Fact]
    public void Should_Deserialize_L3Fields_Correctly()
    {
        // Arrange
        var json = """
        {
        "@type": "l2trace",
        "port": "2",
        "srce": "G8PZT-1",
        "dest": "KIDDER-1",
        "ctrl": 66,
        "l2type": "I",
        "modulo": 8,
        "rseq": 2,
        "tseq": 1,
        "cr": "C",
        "ilen": 2,
        "pid": 240,
        "ptcl": "DATA",
        "l3type": "TestType",
        "l3src": "G8PZT-1",
        "l3dst": "KIDDER-1",
        "ttl": 10
        }
        """;
        
        // Act
        var parsed = UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped.Should().BeOfType<L2Trace>().Subject;

        // Assert L3 fields
        frame.L3Type.Should().Be("TestType");
        frame.L3Source.Should().Be("G8PZT-1");
        frame.L3Destination.Should().Be("KIDDER-1");
        frame.TimeToLive.Should().Be(10);
        
        parsed.Should().BeTrue();
    }

    #endregion
}
