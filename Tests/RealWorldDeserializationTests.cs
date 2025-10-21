using System.Text.Json;
using node_api.Models;

namespace Tests;

/// <summary>
/// Tests for real-world JSON messages that caused deserialization errors
/// </summary>
public class RealWorldDeserializationTests
{
    [Fact]
    public void Should_Deserialize_ZL2BAU_INP3_Message_With_ISO8601_Timestamps()
    {
        // This is the actual JSON from the error report that failed to deserialize
        // The issue was that timestamp fields were being sent as ISO 8601 strings instead of Unix timestamps
        var json = """
        {
        "@type": "L2Trace",
        "reportFrom": "ZL2BAU-3",
        "time": 1761087470,
        "port": "40",
        "srce": "ZL2BAU-3",
        "dest": "K5DAT-5",
        "ctrl": 232,
        "l2Type": "I",
        "modulo": 8,
        "rseq": 7,
        "tseq": 4,
        "cr": "C",
        "ilen": 164,
        "pid": 207,
        "ptcl": "NET/ROM",
        "l3type": "Routing info",
        "type": "INP3",
        "nodes": [
        {
        "call": "N9SEO-3",
        "hops": 3,
        "tt": 20,
        "alias": "ARMNTH"
        },
        {
        "call": "VK3AT-1",
        "hops": 2,
        "tt": 34,
        "alias": "AUSMEL"
        },
        {
        "call": "MB7NBA",
        "hops": 3,
        "tt": 16,
        "alias": "BAMPTN"
        },
        {
        "call": "ZL2BAU",
        "hops": 1,
        "tt": 1,
        "alias": "BAUBBS",
        "latitude": -44.4426,
        "longitude": 171.0322,
        "version": "504k",
        "timestamp": "2025-10-22T11:57:40Z",
        "tzMins": 780
        },
        {
        "call": "ZL2BAU-8",
        "hops": 1,
        "tt": 1,
        "alias": "BAUCHT",
        "ipAddr": "44.147.210.26",
        "bitMask": 32,
        "tcpPort": 3600,
        "latitude": -44.4426,
        "longitude": 171.0322,
        "software": "XRLin",
        "isNode": false,
        "isXRCHAT": true,
        "version": "504k",
        "timestamp": "2025-10-22T11:57:40Z",
        "tzMins": 780
        }
        ]
        }
        """;

        // This should now deserialize successfully with the FlexibleTimestampConverter
        var result = JsonSerializer.Deserialize<L2Trace>(json);
        
        Assert.NotNull(result);
        Assert.Equal("L2Trace", result.DatagramType);
        Assert.Equal("ZL2BAU-3", result.ReportFrom);
        Assert.Equal(1761087470, result.TimeUnixSeconds);
        Assert.Equal("40", result.Port);
        Assert.Equal("ZL2BAU-3", result.Source);
        Assert.Equal("K5DAT-5", result.Destination);
        Assert.Equal(232, result.Control);
        Assert.Equal("I", result.L2Type);
        Assert.Equal(8, result.Modulo);
        Assert.Equal(7, result.ReceiveSequence);
        Assert.Equal(4, result.TransmitSequence);
        Assert.Equal("C", result.CommandResponse);
        Assert.Equal(164, result.IFieldLength);
        Assert.Equal(207, result.ProtocolId);
        Assert.Equal("NET/ROM", result.ProtocolName);
        Assert.Equal("Routing info", result.L3Type);
        Assert.Equal("INP3", result.Type);
        
        Assert.NotNull(result.Nodes);
        Assert.Equal(5, result.Nodes.Length);
        
        // Verify nodes without timestamps
        var nodesWithoutTimestamp = result.Nodes.Where(n => n.Callsign is "N9SEO-3" or "VK3AT-1" or "MB7NBA").ToArray();
        Assert.Equal(3, nodesWithoutTimestamp.Length);
        Assert.All(nodesWithoutTimestamp, node => Assert.Null(node.Timestamp));
        
        // Verify ZL2BAU node with timestamp
        var zl2bauNode = result.Nodes.First(n => n.Callsign == "ZL2BAU");
        Assert.NotNull(zl2bauNode);
        Assert.Equal("BAUBBS", zl2bauNode.Alias);
        Assert.Equal(1, zl2bauNode.Hops);
        Assert.Equal(1, zl2bauNode.OneWayTripTimeIn10msIncrements);
        Assert.Equal(-44.4426m, zl2bauNode.Latitude);
        Assert.Equal(171.0322m, zl2bauNode.Longitude);
        Assert.Equal("504k", zl2bauNode.Version);
        Assert.Equal(780, zl2bauNode.TimeZoneMinutesOffsetFromGmt);
        
        // Verify timestamp was converted from ISO 8601 string to Unix timestamp
        Assert.NotNull(zl2bauNode.Timestamp);
        var expectedDate = new DateTime(2025, 10, 22, 11, 57, 40, DateTimeKind.Utc);
        var expectedTimestamp = ((DateTimeOffset)expectedDate).ToUnixTimeSeconds();
        Assert.Equal(expectedTimestamp, zl2bauNode.Timestamp);
        
        // Verify ZL2BAU-8 node with timestamp
        var zl2bauNode8 = result.Nodes.First(n => n.Callsign == "ZL2BAU-8");
        Assert.NotNull(zl2bauNode8);
        Assert.Equal("BAUCHT", zl2bauNode8.Alias);
        Assert.Equal(1, zl2bauNode8.Hops);
        Assert.Equal(1, zl2bauNode8.OneWayTripTimeIn10msIncrements);
        Assert.Equal("44.147.210.26", zl2bauNode8.IpAddress);
        Assert.Equal(32, zl2bauNode8.BitMask);
        Assert.Equal(3600, zl2bauNode8.TcpPort);
        Assert.Equal(-44.4426m, zl2bauNode8.Latitude);
        Assert.Equal(171.0322m, zl2bauNode8.Longitude);
        Assert.Equal("XRLin", zl2bauNode8.Software);
        Assert.Equal(false, zl2bauNode8.IsNode);
        Assert.Equal(true, zl2bauNode8.IsXrchat);
        Assert.Equal("504k", zl2bauNode8.Version);
        Assert.Equal(780, zl2bauNode8.TimeZoneMinutesOffsetFromGmt);
        
        // Verify timestamp was converted
        Assert.NotNull(zl2bauNode8.Timestamp);
        Assert.Equal(expectedTimestamp, zl2bauNode8.Timestamp);
    }

    [Fact]
    public void Should_Deserialize_Mixed_Timestamp_Formats()
    {
        // Test a message with both integer timestamps and ISO 8601 string timestamps
        var json = """
        {
        "@type": "L2Trace",
        "reportFrom": "TEST-1",
        "time": 1761087470,
        "port": "1",
        "srce": "TEST-1",
        "dest": "TEST-2",
        "ctrl": 3,
        "l2Type": "UI",
        "cr": "C",
        "ptcl": "NET/ROM",
        "l3type": "Routing info",
        "type": "INP3",
        "nodes": [
        {
        "call": "NODE1",
        "hops": 1,
        "tt": 1,
        "timestamp": 1728270184
        },
        {
        "call": "NODE2",
        "hops": 2,
        "tt": 2,
        "timestamp": "2025-10-22T11:57:40Z"
        },
        {
        "call": "NODE3",
        "hops": 3,
        "tt": 3
        }
        ]
        }
        """;

        var result = JsonSerializer.Deserialize<L2Trace>(json);
        
        Assert.NotNull(result);
        Assert.NotNull(result.Nodes);
        Assert.Equal(3, result.Nodes.Length);
        
        // First node has integer timestamp
        Assert.Equal(1728270184, result.Nodes[0].Timestamp);
        
        // Second node has ISO 8601 string timestamp (converted to Unix timestamp)
        var expectedDate = new DateTime(2025, 10, 22, 11, 57, 40, DateTimeKind.Utc);
        var expectedTimestamp = ((DateTimeOffset)expectedDate).ToUnixTimeSeconds();
        Assert.Equal(expectedTimestamp, result.Nodes[1].Timestamp);
        
        // Third node has no timestamp
        Assert.Null(result.Nodes[2].Timestamp);
    }
}
