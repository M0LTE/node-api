using node_api;
using node_api.Models;
using System.Text.Json;

namespace Tests;

public class L3TraceJsonDeserializationTests
{
    [Fact]
    public void Should_Deserialize_L3Trace_From_Example()
    {
        // Example from the issue
        var json = @"{
            ""@type"": ""L3Trace"",
            ""serial"": 45,
            ""time"": 1762355570,
            ""dirn"": ""rcvd"",
            ""isRF"": false,
            ""reportFrom"": ""G8BPQ-2"",
            ""port"": 2,
            ""ilen"": 40,
            ""pid"": 207,
            ""ptcl"": ""NET/ROM"",
            ""l3Type"": ""NetRom"",
            ""l3src"": ""G8BPQ-4"",
            ""l3dst"": ""GM8BPQ-4"",
            ""ttl"": 25,
            ""l4Type"": ""INFO"",
            ""toCct"": 95,
            ""txSeq"": 72,
            ""rxSeq"": 56,
            ""paylen"": 20
        }";

        var success = NetworkEventDatagramDeserialiser.TryDeserialise(json, out var datagram, out var exception);

        Assert.True(success);
        Assert.Null(exception);
        Assert.NotNull(datagram);
        Assert.IsType<L3Trace>(datagram);

        var trace = (L3Trace)datagram!;
        Assert.Equal(45, trace.Serial);
        Assert.Equal(1762355570, trace.TimeUnixSeconds);
        Assert.Equal("rcvd", trace.Direction);
        Assert.False(trace.IsRF);
        Assert.Equal("G8BPQ-2", trace.ReportFrom);
        Assert.Equal(2, trace.Port);
        Assert.Equal(40, trace.IFieldLength);
        Assert.Equal(207, trace.ProtocolId);
        Assert.Equal("NET/ROM", trace.ProtocolName);
        Assert.Equal("NetRom", trace.L3Type);
        Assert.Equal("G8BPQ-4", trace.L3Source);
        Assert.Equal("GM8BPQ-4", trace.L3Destination);
        Assert.Equal(25, trace.TimeToLive);
        Assert.Equal("INFO", trace.L4Type);
        Assert.Equal(95, trace.ToCircuit);
        Assert.Equal(72, trace.TransmitSequenceNumber);
        Assert.Equal(56, trace.ReceiveSequenceNumber);
        Assert.Equal(20, trace.PayloadLength);
    }

    [Fact]
    public void Should_Deserialize_L3Trace_With_Sent_Direction()
    {
        var json = @"{
            ""@type"": ""L3Trace"",
            ""serial"": 100,
            ""time"": 1762355570,
            ""dirn"": ""sent"",
            ""isRF"": true,
            ""reportFrom"": ""G8BPQ-2"",
            ""port"": 1,
            ""ilen"": 50,
            ""pid"": 207,
            ""ptcl"": ""NET/ROM"",
            ""l3Type"": ""NetRom"",
            ""l3src"": ""G8BPQ-4"",
            ""l3dst"": ""GM8BPQ-4"",
            ""ttl"": 30,
            ""l4Type"": ""CONN ACK"",
            ""toCct"": 10
        }";

        var success = NetworkEventDatagramDeserialiser.TryDeserialise(json, out var datagram, out var exception);

        Assert.True(success);
        Assert.Null(exception);
        Assert.NotNull(datagram);
        Assert.IsType<L3Trace>(datagram);

        var trace = (L3Trace)datagram!;
        Assert.Equal("sent", trace.Direction);
        Assert.True(trace.IsRF);
        Assert.Equal("CONN ACK", trace.L4Type);
        Assert.Equal(10, trace.ToCircuit);
    }

    [Fact]
    public void Should_Deserialize_L3Trace_With_INFO_ACK()
    {
        var json = @"{
            ""@type"": ""L3Trace"",
            ""serial"": 200,
            ""time"": 1762355570,
            ""dirn"": ""rcvd"",
            ""isRF"": false,
            ""reportFrom"": ""G8BPQ-2"",
            ""port"": 2,
            ""ilen"": 0,
            ""pid"": 207,
            ""ptcl"": ""NET/ROM"",
            ""l3Type"": ""NetRom"",
            ""l3src"": ""G8BPQ-4"",
            ""l3dst"": ""GM8BPQ-4"",
            ""ttl"": 25,
            ""l4Type"": ""INFO ACK"",
            ""toCct"": 95,
            ""rxSeq"": 73
        }";

        var success = NetworkEventDatagramDeserialiser.TryDeserialise(json, out var datagram, out var exception);

        Assert.True(success);
        Assert.Null(exception);
        Assert.NotNull(datagram);
        Assert.IsType<L3Trace>(datagram);

        var trace = (L3Trace)datagram!;
        Assert.Equal("INFO ACK", trace.L4Type);
        Assert.Equal(95, trace.ToCircuit);
        Assert.Equal(73, trace.ReceiveSequenceNumber);
        Assert.Null(trace.TransmitSequenceNumber);
        Assert.Null(trace.PayloadLength);
    }

    [Fact]
    public void Should_Deserialize_L3Trace_With_DISC_REQ()
    {
        var json = @"{
            ""@type"": ""L3Trace"",
            ""serial"": 300,
            ""time"": 1762355570,
            ""dirn"": ""sent"",
            ""isRF"": true,
            ""reportFrom"": ""G8BPQ-2"",
            ""port"": 2,
            ""ilen"": 10,
            ""pid"": 207,
            ""ptcl"": ""NET/ROM"",
            ""l3Type"": ""NetRom"",
            ""l3src"": ""G8BPQ-4"",
            ""l3dst"": ""GM8BPQ-4"",
            ""ttl"": 25,
            ""l4Type"": ""DISC REQ"",
            ""toCct"": 95
        }";

        var success = NetworkEventDatagramDeserialiser.TryDeserialise(json, out var datagram, out var exception);

        Assert.True(success);
        Assert.Null(exception);
        Assert.NotNull(datagram);
        Assert.IsType<L3Trace>(datagram);

        var trace = (L3Trace)datagram!;
        Assert.Equal("DISC REQ", trace.L4Type);
        Assert.Equal(95, trace.ToCircuit);
    }

    [Fact]
    public void Should_Deserialize_L3Trace_Without_Optional_Fields()
    {
        var json = @"{
            ""@type"": ""L3Trace"",
            ""serial"": 400,
            ""time"": 1762355570,
            ""dirn"": ""rcvd"",
            ""isRF"": false,
            ""reportFrom"": ""G8BPQ-2"",
            ""port"": 2,
            ""ilen"": 40,
            ""pid"": 207,
            ""ptcl"": ""NET/ROM"",
            ""l3Type"": ""NetRom"",
            ""l3src"": ""G8BPQ-4"",
            ""l3dst"": ""GM8BPQ-4"",
            ""ttl"": 25,
            ""l4Type"": ""RSET""
        }";

        var success = NetworkEventDatagramDeserialiser.TryDeserialise(json, out var datagram, out var exception);

        Assert.True(success);
        Assert.Null(exception);
        Assert.NotNull(datagram);
        Assert.IsType<L3Trace>(datagram);

        var trace = (L3Trace)datagram!;
        Assert.Equal("RSET", trace.L4Type);
        Assert.Null(trace.ToCircuit);
        Assert.Null(trace.TransmitSequenceNumber);
        Assert.Null(trace.ReceiveSequenceNumber);
        Assert.Null(trace.PayloadLength);
    }

    [Fact]
    public void Should_Fail_To_Deserialize_Invalid_JSON()
    {
        var json = @"{
            ""@type"": ""L3Trace"",
            ""serial"": ""not a number"",
        }";

        var success = NetworkEventDatagramDeserialiser.TryDeserialise(json, out var datagram, out var exception);

        Assert.False(success);
        Assert.Null(datagram);
        Assert.NotNull(exception);
    }

    [Fact]
    public void Should_Return_Correct_DatagramType()
    {
        var json = @"{
            ""@type"": ""L3Trace"",
            ""serial"": 45,
            ""time"": 1762355570,
            ""dirn"": ""rcvd"",
            ""isRF"": false,
            ""reportFrom"": ""G8BPQ-2"",
            ""port"": 2,
            ""ilen"": 40,
            ""pid"": 207,
            ""ptcl"": ""NET/ROM"",
            ""l3Type"": ""NetRom"",
            ""l3src"": ""G8BPQ-4"",
            ""l3dst"": ""GM8BPQ-4"",
            ""ttl"": 25,
            ""l4Type"": ""INFO""
        }";

        var success = NetworkEventDatagramDeserialiser.TryDeserialise(json, out var datagram, out _);

        Assert.True(success);
        Assert.NotNull(datagram);
        Assert.Equal("L3Trace", datagram!.DatagramType);
    }

    [Fact]
    public void Should_Serialize_And_Deserialize_L3Trace()
    {
        var original = new L3Trace
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

        // Serialize as NetworkEventDatagram to get the @type discriminator
        NetworkEventDatagram datagramToSerialize = original;
        var json = JsonSerializer.Serialize(datagramToSerialize, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });

        // Deserialize
        var success = NetworkEventDatagramDeserialiser.TryDeserialise(json, out var datagram, out _);

        Assert.True(success);
        Assert.NotNull(datagram);
        Assert.IsType<L3Trace>(datagram);

        var deserialized = (L3Trace)datagram!;
        Assert.Equal(original.Serial, deserialized.Serial);
        Assert.Equal(original.TimeUnixSeconds, deserialized.TimeUnixSeconds);
        Assert.Equal(original.Direction, deserialized.Direction);
        Assert.Equal(original.IsRF, deserialized.IsRF);
        Assert.Equal(original.ReportFrom, deserialized.ReportFrom);
        Assert.Equal(original.Port, deserialized.Port);
        Assert.Equal(original.IFieldLength, deserialized.IFieldLength);
        Assert.Equal(original.ProtocolId, deserialized.ProtocolId);
        Assert.Equal(original.ProtocolName, deserialized.ProtocolName);
        Assert.Equal(original.L3Type, deserialized.L3Type);
        Assert.Equal(original.L3Source, deserialized.L3Source);
        Assert.Equal(original.L3Destination, deserialized.L3Destination);
        Assert.Equal(original.TimeToLive, deserialized.TimeToLive);
        Assert.Equal(original.L4Type, deserialized.L4Type);
        Assert.Equal(original.ToCircuit, deserialized.ToCircuit);
        Assert.Equal(original.TransmitSequenceNumber, deserialized.TransmitSequenceNumber);
        Assert.Equal(original.ReceiveSequenceNumber, deserialized.ReceiveSequenceNumber);
        Assert.Equal(original.PayloadLength, deserialized.PayloadLength);
    }
}
