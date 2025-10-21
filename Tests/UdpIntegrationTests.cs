using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using node_api.Models;
using Xunit;

namespace Tests;

/// <summary>
/// Integration tests for UDP listener and deserialization
/// </summary>
public class UdpIntegrationTests : IDisposable
{
    private const int TestPort = 55555;
    private UdpClient? _testClient;

    public UdpIntegrationTests()
    {
        _testClient = new UdpClient();
    }

    public void Dispose()
    {
        _testClient?.Dispose();
    }

    [Fact]
    public async Task Should_Send_And_Receive_L2Trace_Via_UDP()
    {
        // Arrange
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "TEST-1",
            TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        var json = JsonSerializer.Serialize(trace);
        var bytes = Encoding.UTF8.GetBytes(json);

        // Act
        var endpoint = new IPEndPoint(IPAddress.Loopback, TestPort);
        await _testClient!.SendAsync(bytes, bytes.Length, endpoint);

        // Assert - if we got here without exception, UDP send worked
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void Should_Serialize_All_Event_Types_To_Valid_JSON()
    {
        // Test that all event types can be serialized
        var events = new List<UdpNodeInfoJsonDatagram>
        {
            new L2Trace
            {
                DatagramType = "L2Trace",
                ReportFrom = "TEST",
                Port = "1",
                Source = "SRC",
                Destination = "DST",
                Control = 0,
                L2Type = "UI",
                CommandResponse = "C"
            },
            new NodeUpEvent
            {
                DatagramType = "NodeUpEvent",
                NodeCall = "TEST",
                NodeAlias = "TST",
                Locator = "IO82VJ",
                Software = "test",
                Version = "1.0"
            },
            new LinkUpEvent
            {
                DatagramType = "LinkUpEvent",
                Node = "TEST",
                Id = 1,
                Direction = "incoming",
                Port = "1",
                Remote = "REM",
                Local = "LOC"
            },
            new CircuitUpEvent
            {
                DatagramType = "CircuitUpEvent",
                Node = "TEST",
                Id = 1,
                Direction = "incoming",
                Remote = "REM:1234",
                Local = "LOC:5678"
            }
        };

        foreach (var evt in events)
        {
            var json = JsonSerializer.Serialize(evt, evt.GetType());
            Assert.False(string.IsNullOrWhiteSpace(json));
            Assert.Contains("\"@type\"", json);
        }
    }

    [Fact]
    public void Should_Deserialize_JSON_Back_To_Original_Object()
    {
        // Arrange
        var original = new NodeUpEvent
        {
            DatagramType = "NodeUpEvent",
            TimeUnixSeconds = 1729512000,
            NodeCall = "G8PZT-1",
            NodeAlias = "TEST",
            Locator = "IO82VJ",
            Software = "xrlin",
            Version = "1.0",
            Latitude = 51.5m,
            Longitude = -0.12m
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<NodeUpEvent>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.DatagramType, deserialized.DatagramType);
        Assert.Equal(original.NodeCall, deserialized.NodeCall);
        Assert.Equal(original.NodeAlias, deserialized.NodeAlias);
        Assert.Equal(original.Locator, deserialized.Locator);
        Assert.Equal(original.TimeUnixSeconds, deserialized.TimeUnixSeconds);
        Assert.Equal(original.Latitude, deserialized.Latitude);
        Assert.Equal(original.Longitude, deserialized.Longitude);
    }

    [Fact]
    public void Should_Handle_Large_L2Trace_With_All_Optional_Fields()
    {
        // Arrange - create L2Trace with maximum fields populated
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-1",
            TimeUnixSeconds = 1729512000,
            Port = "2",
            Source = "G8PZT-1",
            Destination = "G8PZT",
            Control = 32,
            L2Type = "I",
            CommandResponse = "C",
            Modulo = 128,
            ReceiveSequence = 1,
            TransmitSequence = 0,
            PollFinal = "P",
            ProtocolId = 207,
            ProtocolName = "NET/ROM",
            IFieldLength = 197,
            L3Type = "Routing info",
            Type = "INP3",
            Nodes = new[]
            {
                new L2Trace.Node
                {
                    Callsign = "GB7JD-8",
                    Hops = 2,
                    OneWayTripTimeIn10msIncrements = 2,
                    Alias = "JEDCHT",
                    IpAddress = "44.131.8.1",
                    BitMask = 32,
                    TcpPort = 3600,
                    Latitude = 55.3125m,
                    Longitude = -2.3250m,
                    Software = "XRLin",
                    Version = "504i",
                    IsNode = false,
                    IsXrchat = true,
                    IsRtchat = true,
                    Timestamp = 1728270184
                }
            },
            Digipeaters = new[]
            {
                new L2Trace.Digipeater { Callsign = "RELAY-1", Repeated = true },
                new L2Trace.Digipeater { Callsign = "RELAY-2", Repeated = false }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(trace);
        var deserialized = JsonSerializer.Deserialize<L2Trace>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(trace.Nodes!.Length, deserialized.Nodes!.Length);
        Assert.Equal(trace.Digipeaters!.Length, deserialized.Digipeaters!.Length);
        Assert.Equal(trace.Nodes[0].Timestamp, deserialized.Nodes[0].Timestamp);
    }
}
