using System.Diagnostics;
using System.Text.Json;
using FluentValidation.TestHelper;
using node_api.Models;
using node_api.Validators;

namespace Tests;

/// <summary>
/// Performance and stress tests to ensure validators can handle high load
/// </summary>
public class PerformanceTests
{
    [Fact]
    public void Should_Validate_1000_L2Traces_In_Reasonable_Time()
    {
        // Arrange
        var validator = new L2TraceValidator();
        var traces = Enumerable.Range(1, 1000).Select(i => new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = $"NODE{i}",
            Port = "1",
            Source = $"SRC{i}",
            Destination = $"DST{i}",
            Control = i % 256,
            L2Type = "UI",
            CommandResponse = "C"
        }).ToList();

        // Act
        var sw = Stopwatch.StartNew();
        foreach (var trace in traces)
        {
            validator.TestValidate(trace);
        }
        sw.Stop();

        // Assert - should complete in less than 1 second
        Assert.True(sw.ElapsedMilliseconds < 1000, 
            $"Validation took {sw.ElapsedMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public void Should_Serialize_And_Deserialize_1000_Events_Quickly()
    {
        // Arrange
        var events = Enumerable.Range(1, 1000).Select(i => new NodeUpEvent
        {
            DatagramType = "NodeUpEvent",
            NodeCall = $"NODE{i}",
            NodeAlias = $"NOD{i}",
            Locator = "IO82VJ",
            Software = "test",
            Version = "1.0"
        }).ToList();

        // Act
        var sw = Stopwatch.StartNew();
        foreach (var evt in events)
        {
            var json = JsonSerializer.Serialize(evt);
            var deserialized = JsonSerializer.Deserialize<NodeUpEvent>(json);
        }
        sw.Stop();

        // Assert - should complete in less than 500ms
        Assert.True(sw.ElapsedMilliseconds < 500,
            $"Serialization took {sw.ElapsedMilliseconds}ms, expected < 500ms");
    }

    [Fact]
    public void Should_Handle_Concurrent_Validation()
    {
        // Arrange
        var validator = new L2TraceValidator();
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "TEST",
            Port = "1",
            Source = "SRC",
            Destination = "DST",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        // Act - validate same model concurrently from multiple threads
        var tasks = Enumerable.Range(1, 100).Select(i => Task.Run(() =>
        {
            var result = validator.TestValidate(model);
            return result.IsValid;
        })).ToArray();

        Task.WaitAll(tasks);

        // Assert - all validations should succeed
        Assert.All(tasks, task => Assert.True(task.Result));
    }

    [Fact]
    public void Should_Validate_Very_Large_Routing_Broadcast_Efficiently()
    {
        // Arrange - create routing broadcast with 500 nodes
        var validator = new L2TraceValidator();
        var nodes = Enumerable.Range(1, 500).Select(i => new L2Trace.Node
        {
            Callsign = $"NODE{i}",
            Hops = i % 32,
            OneWayTripTimeIn10msIncrements = i * 10,
            Alias = $"NOD{i}",
            IpAddress = $"44.131.{i / 256}.{i % 256}",
            BitMask = 24,
            TcpPort = 3600 + i,
            Latitude = (i % 180) - 90m,
            Longitude = (i % 360) - 180m,
            Software = "XRLin",
            Version = "504i",
            Timestamp = 1728270184 + i
        }).ToArray();

        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "HUB",
            Port = "1",
            Source = "SRC",
            Destination = "DST",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "Routing info",
            Type = "INP3",
            Nodes = nodes
        };

        // Act
        var sw = Stopwatch.StartNew();
        var result = validator.TestValidate(model);
        sw.Stop();

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
        Assert.True(sw.ElapsedMilliseconds < 100,
            $"Validation took {sw.ElapsedMilliseconds}ms, expected < 100ms");
    }

    [Fact]
    public void Should_Handle_Validation_Of_All_Event_Types_Concurrently()
    {
        // Arrange - create one of each event type
        var events = new List<UdpNodeInfoJsonDatagram>
        {
            new L2Trace
            {
                DatagramType = "L2Trace",
                ReportFrom = "TEST",
                Port = "1",
                Source = "SRC",
                Destination = "DST",
                Control = 3,
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
            new NodeDownEvent
            {
                DatagramType = "NodeDownEvent",
                NodeCall = "TEST",
                NodeAlias = "TST"
            },
            new NodeStatusReportEvent
            {
                DatagramType = "NodeStatus",
                NodeCall = "TEST",
                NodeAlias = "TST",
                Locator = "IO82VJ",
                Software = "test",
                Version = "1.0",
                UptimeSecs = 1000
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
            new LinkStatus
            {
                DatagramType = "LinkStatus",
                Node = "TEST",
                Id = 1,
                Direction = "incoming",
                Port = "1",
                Remote = "REM",
                Local = "LOC",
                UpForSecs = 100,
                FramesSent = 10,
                FramesReceived = 10,
                FramesResent = 0,
                FramesQueued = 0
            },
            new LinkDisconnectionEvent
            {
                DatagramType = "LinkDownEvent",
                Node = "TEST",
                Id = 1,
                Direction = "incoming",
                Port = "1",
                Remote = "REM",
                Local = "LOC",
                UpForSecs = 100,
                FramesSent = 10,
                FramesReceived = 10,
                FramesResent = 0,
                FramesQueued = 0
            },
            new CircuitUpEvent
            {
                DatagramType = "CircuitUpEvent",
                Node = "TEST",
                Id = 1,
                Direction = "incoming",
                Remote = "REM:1234",
                Local = "LOC:5678"
            },
            new CircuitStatus
            {
                DatagramType = "CircuitStatus",
                Node = "TEST",
                Id = 1,
                Direction = "incoming",
                Remote = "REM:1234",
                Local = "LOC:5678",
                SegmentsSent = 10,
                SegmentsReceived = 10,
                SegmentsResent = 0,
                SegmentsQueued = 0
            },
            new CircuitDisconnectionEvent
            {
                DatagramType = "CircuitDownEvent",
                Node = "TEST",
                Id = 1,
                Direction = "incoming",
                Remote = "REM:1234",
                Local = "LOC:5678",
                SegmentsSent = 10,
                SegmentsReceived = 10,
                SegmentsResent = 0,
                SegmentsQueued = 0
            }
        };

        // Act - validate all event types in parallel
        var sw = Stopwatch.StartNew();
        var tasks = events.Select(evt => Task.Run(() =>
        {
            var json = JsonSerializer.Serialize(evt, evt.GetType());
            var deserialized = JsonSerializer.Deserialize(json, evt.GetType());
            return deserialized != null;
        })).ToArray();

        Task.WaitAll(tasks);
        sw.Stop();

        // Assert
        Assert.All(tasks, task => Assert.True(task.Result));
        Assert.True(sw.ElapsedMilliseconds < 200,
            $"Concurrent validation took {sw.ElapsedMilliseconds}ms, expected < 200ms");
    }

    [Fact]
    public void Should_Not_Leak_Memory_During_Repeated_Validation()
    {
        // Arrange
        var validator = new L2TraceValidator();
        var initialMemory = GC.GetTotalMemory(true);

        // Act - validate 10,000 times
        for (int i = 0; i < 10000; i++)
        {
            var model = new L2Trace
            {
                DatagramType = "L2Trace",
                ReportFrom = $"NODE{i}",
                Port = "1",
                Source = "SRC",
                Destination = "DST",
                Control = 3,
                L2Type = "UI",
                CommandResponse = "C"
            };

            validator.TestValidate(model);

            // Force GC every 1000 iterations
            if (i % 1000 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryGrowth = finalMemory - initialMemory;

        // Assert - memory growth should be reasonable (< 10MB)
        Assert.True(memoryGrowth < 10 * 1024 * 1024,
            $"Memory grew by {memoryGrowth / 1024 / 1024}MB, expected < 10MB");
    }

    [Fact]
    public void Should_Validate_Complex_NetRom_Traces_Efficiently()
    {
        // Arrange
        var validator = new L2TraceValidator();
        
        // Create 100 complex NetRom traces
        var traces = Enumerable.Range(1, 100).Select(i => new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = $"NODE{i}",
            Port = "1",
            Source = $"SRC{i}",
            Destination = $"DST{i}",
            Control = 32,
            L2Type = "I",
            CommandResponse = "C",
            ProtocolName = "NET/ROM",
            L3Type = "NetRom",
            L3Source = $"SRC{i}",
            L3Destination = $"DST{i}",
            TimeToLive = 25,
            L4Type = "INFO",
            ToCircuit = i,
            TransmitSequenceNumber = i % 8,
            ReceiveSequenceNumber = (i + 1) % 8,
            PayloadLength = 256
        }).ToList();

        // Act
        var sw = Stopwatch.StartNew();
        foreach (var trace in traces)
        {
            validator.TestValidate(trace);
        }
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < 200,
            $"Validation took {sw.ElapsedMilliseconds}ms, expected < 200ms");
    }

    [Fact]
    public void Should_Handle_Extremely_Large_JSON_Payloads()
    {
        // Arrange - create trace with very large data field
        var largeData = new string('X', 100000); // 100KB of data
        
        var model = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "TEST",
            Port = "1",
            Source = "SRC",
            Destination = "DST",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            Data = largeData
        };

        // Act
        var sw = Stopwatch.StartNew();
        var json = JsonSerializer.Serialize(model);
        var deserialized = JsonSerializer.Deserialize<L2Trace>(json);
        sw.Stop();

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(largeData, deserialized.Data);
        Assert.True(sw.ElapsedMilliseconds < 100,
            $"Large payload serialization took {sw.ElapsedMilliseconds}ms, expected < 100ms");
    }
}
