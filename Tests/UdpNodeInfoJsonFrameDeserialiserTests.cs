using FluentAssertions;
using node_api;
using node_api.Models;

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
        UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped).Should().BeTrue();

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
        UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var frameUntyped).Should().BeTrue();

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
    }
}