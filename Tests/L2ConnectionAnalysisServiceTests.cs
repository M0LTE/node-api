using FluentAssertions;
using node_api.Controllers;
using node_api.Services;
using NSubstitute;
using System.Text.Json;

namespace Tests;

public class L2ConnectionAnalysisServiceTests
{
    private readonly IEventRepository _eventRepository;
    private readonly ITraceRepository _traceRepository;
    private readonly L2ConnectionAnalysisService _service;

    public L2ConnectionAnalysisServiceTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _traceRepository = Substitute.For<ITraceRepository>();
        _service = new L2ConnectionAnalysisService(_eventRepository, _traceRepository);
    }

    #region BuildSessionsFromEvents Tests

    [Fact]
    public async Task AnalyzeConnectionAsync_WithLinkUpAndDown_CreatesSession()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddHours(-1);
        var to = DateTimeOffset.UtcNow;
        var events = new List<EventsController.EventDto>
        {
            CreateLinkUpEvent(from.UtcDateTime, "G8PZT-1", "M0LTE-5", 1),
            CreateLinkDownEvent(from.AddMinutes(30).UtcDateTime, 1800, 100, 50, 2)
        };

        _eventRepository.GetLinkEventsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), from, to, Arg.Any<CancellationToken>())
            .Returns(events);

        _traceRepository.GetFrameStatisticsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), from, to, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<FrameStatistic>());

        _eventRepository.GetEventsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((events, (string?)null, CountResult.NotRequested));

        // Act
        var result = await _service.AnalyzeConnectionAsync(
            "G8PZT-1", "M0LTE-5", from, to, null, true, false, 100, null, CancellationToken.None);

        // Assert
        result.Sessions.Should().HaveCount(1);
        result.Sessions[0].SessionId.Should().Be(1);
        result.Sessions[0].LinkUp.Should().NotBeNull();
        result.Sessions[0].LinkDown.Should().NotBeNull();
        result.Sessions[0].LinkUp!.LinkId.Should().Be(1);
        result.Sessions[0].LinkDown!.UpForSecs.Should().Be(1800);
    }

    [Fact]
    public async Task AnalyzeConnectionAsync_WithMultipleSessions_CreatesMultipleSessions()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddHours(-2);
        var to = DateTimeOffset.UtcNow;
        var events = new List<EventsController.EventDto>
        {
            // Session 1
            CreateLinkUpEvent(from.UtcDateTime, "G8PZT-1", "M0LTE-5", 1),
            CreateLinkDownEvent(from.AddMinutes(20).UtcDateTime, 1200, 50, 25, 1),
            // Session 2
            CreateLinkUpEvent(from.AddMinutes(30).UtcDateTime, "G8PZT-1", "M0LTE-5", 2),
            CreateLinkDownEvent(from.AddMinutes(50).UtcDateTime, 1200, 60, 30, 2)
        };

        _eventRepository.GetLinkEventsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), from, to, Arg.Any<CancellationToken>())
            .Returns(events);

        _traceRepository.GetFrameStatisticsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), from, to, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<FrameStatistic>());

        _eventRepository.GetEventsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((events, (string?)null, CountResult.NotRequested));

        // Act
        var result = await _service.AnalyzeConnectionAsync(
            "G8PZT-1", "M0LTE-5", from, to, null, true, false, 100, null, CancellationToken.None);

        // Assert
        result.Sessions.Should().HaveCount(2);
        result.Sessions[0].SessionId.Should().Be(1);
        result.Sessions[1].SessionId.Should().Be(2);
    }

    [Fact]
    public async Task AnalyzeConnectionAsync_WithLinkStatusReports_IncludesInSession()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddHours(-1);
        var to = DateTimeOffset.UtcNow;
        var events = new List<EventsController.EventDto>
        {
            CreateLinkUpEvent(from.UtcDateTime, "G8PZT-1", "M0LTE-5", 1),
            CreateLinkStatusEvent(from.AddMinutes(5).UtcDateTime, 300, 20, 15, 1, 200),
            CreateLinkStatusEvent(from.AddMinutes(10).UtcDateTime, 600, 40, 30, 2, 250),
            CreateLinkDownEvent(from.AddMinutes(30).UtcDateTime, 1800, 100, 50, 2)
        };

        _eventRepository.GetLinkEventsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), from, to, Arg.Any<CancellationToken>())
            .Returns(events);

        _traceRepository.GetFrameStatisticsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), from, to, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<FrameStatistic>());

        _eventRepository.GetEventsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((events, (string?)null, CountResult.NotRequested));

        // Act
        var result = await _service.AnalyzeConnectionAsync(
            "G8PZT-1", "M0LTE-5", from, to, null, true, false, 100, null, CancellationToken.None);

        // Assert
        result.Sessions.Should().HaveCount(1);
        result.Sessions[0].StatusReports.Should().HaveCount(2);
        result.Sessions[0].StatusReports[0].UpForSecs.Should().Be(300);
        result.Sessions[0].StatusReports[1].UpForSecs.Should().Be(600);
    }

    [Fact]
    public async Task AnalyzeConnectionAsync_WithUnclosedSession_IncludesSession()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddHours(-1);
        var to = DateTimeOffset.UtcNow;
        var events = new List<EventsController.EventDto>
        {
            CreateLinkUpEvent(from.UtcDateTime, "G8PZT-1", "M0LTE-5", 1)
            // No LinkDown event
        };

        _eventRepository.GetLinkEventsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), from, to, Arg.Any<CancellationToken>())
            .Returns(events);

        _traceRepository.GetFrameStatisticsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), from, to, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<FrameStatistic>());

        _eventRepository.GetEventsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<EventsController.EventDto>(), (string?)null, CountResult.NotRequested));

        // Act
        var result = await _service.AnalyzeConnectionAsync(
            "G8PZT-1", "M0LTE-5", from, to, null, true, false, 100, null, CancellationToken.None);

        // Assert
        result.Sessions.Should().HaveCount(1);
        result.Sessions[0].LinkUp.Should().NotBeNull();
        result.Sessions[0].LinkDown.Should().BeNull();
        result.Sessions[0].Metrics.Should().BeNull(); // Can't calculate metrics without LinkDown
    }

    [Fact]
    public async Task AnalyzeConnectionAsync_CalculatesSessionMetrics_Correctly()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddHours(-1);
        var to = DateTimeOffset.UtcNow;
        var events = new List<EventsController.EventDto>
        {
            CreateLinkUpEvent(from.UtcDateTime, "G8PZT-1", "M0LTE-5", 1),
            CreateLinkStatusEvent(from.AddMinutes(5).UtcDateTime, 300, 20, 15, 1, 200),
            CreateLinkStatusEvent(from.AddMinutes(10).UtcDateTime, 600, 40, 30, 2, 250),
            CreateLinkDownEvent(from.AddMinutes(30).UtcDateTime, 1800, 100, 80, 5, 50000, 40000, "Normal")
        };

        _eventRepository.GetLinkEventsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), from, to, Arg.Any<CancellationToken>())
            .Returns(events);

        _traceRepository.GetFrameStatisticsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), from, to, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<FrameStatistic>());

        _eventRepository.GetEventsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((events, (string?)null, CountResult.NotRequested));

        // Act
        var result = await _service.AnalyzeConnectionAsync(
            "G8PZT-1", "M0LTE-5", from, to, null, true, false, 100, null, CancellationToken.None);

        // Assert
        var metrics = result.Sessions[0].Metrics;
        metrics.Should().NotBeNull();
        metrics!.DurationSecs.Should().Be(1800);
        metrics.TotalFrames.Should().Be(180); // 100 sent + 80 received
        metrics.RetransmissionRate.Should().BeApproximately(0.0277m, 0.001m); // 5 / 180
        metrics.AvgRttMs.Should().Be(225); // Average of 200 and 250
        metrics.ThroughputBps.Should().Be(400); // (50000 + 40000) * 8 / 1800
    }

    #endregion

    #region OverallMetrics Tests

    [Fact]
    public async Task AnalyzeConnectionAsync_WithIncludeMetrics_BuildsOverallMetrics()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddHours(-1);
        var to = DateTimeOffset.UtcNow;
        
        _eventRepository.GetLinkEventsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), from, to, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EventsController.EventDto>());

        var frameStats = new List<FrameStatistic>
        {
            new FrameStatistic("G8PZT-1", "M0LTE-5", "I", 100, 50000),
            new FrameStatistic("G8PZT-1", "M0LTE-5", "RR", 20, 0),
            new FrameStatistic("M0LTE-5", "G8PZT-1", "I", 80, 40000),
            new FrameStatistic("M0LTE-5", "G8PZT-1", "RR", 30, 0)
        };

        _traceRepository.GetFrameStatisticsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), from, to, Arg.Any<CancellationToken>())
            .Returns(frameStats);

        var linkDownEvents = new List<EventsController.EventDto>
        {
            CreateLinkDownEvent(from.AddMinutes(30).UtcDateTime, 1800, 100, 50, 2)
        };

        _eventRepository.GetEventsAsync(
            null, "LinkDownEvent", null, null, null, null, from, to, 10000, null, false, "ASC", Arg.Any<CancellationToken>())
            .Returns((linkDownEvents, (string?)null, CountResult.NotRequested));

        // Act
        var result = await _service.AnalyzeConnectionAsync(
            "G8PZT-1", "M0LTE-5", from, to, null, true, false, 100, null, CancellationToken.None);

        // Assert
        result.Metrics.Should().NotBeNull();
        result.Metrics!.TotalFrames.Should().Be(230); // 120 + 110
        result.Metrics.Direction1To2.Frames.Should().Be(120);
        result.Metrics.Direction1To2.IFrames.Should().Be(100);
        result.Metrics.Direction1To2.Bytes.Should().Be(50000);
        result.Metrics.Direction2To1.Frames.Should().Be(110);
        result.Metrics.Direction2To1.IFrames.Should().Be(80);
        result.Metrics.Direction2To1.Bytes.Should().Be(40000);
    }

    [Fact]
    public async Task AnalyzeConnectionAsync_WithoutIncludeMetrics_DoesNotBuildMetrics()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddHours(-1);
        var to = DateTimeOffset.UtcNow;
        
        _eventRepository.GetLinkEventsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), from, to, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EventsController.EventDto>());

        // Act
        var result = await _service.AnalyzeConnectionAsync(
            "G8PZT-1", "M0LTE-5", from, to, null, false, false, 100, null, CancellationToken.None);

        // Assert
        result.Metrics.Should().BeNull();
        await _traceRepository.DidNotReceive().GetFrameStatisticsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Traces Tests

    [Fact]
    public async Task AnalyzeConnectionAsync_WithIncludeTraces_BuildsPagedTraces()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddHours(-1);
        var to = DateTimeOffset.UtcNow;
        
        _eventRepository.GetLinkEventsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), from, to, Arg.Any<CancellationToken>())
            .Returns(new List<EventsController.EventDto>
            {
                CreateLinkUpEvent(from.UtcDateTime, "G8PZT-1", "M0LTE-5", 1)
            });

        var traces = new List<TracesController.TraceDto>
        {
            CreateTrace(1, from.AddMinutes(1).UtcDateTime, "G8PZT-1", "M0LTE-5", "I"),
            CreateTrace(2, from.AddMinutes(2).UtcDateTime, "M0LTE-5", "G8PZT-1", "RR")
        };

        _traceRepository.GetBidirectionalTracesAsync(
            Arg.Any<string>(), Arg.Any<string>(), 
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), 
            Arg.Is<string[]?>(x => x == null), 
            Arg.Is<int>(x => x == 100), 
            Arg.Is<string?>(x => x == null), 
            Arg.Any<CancellationToken>())
            .Returns((traces, (string?)null));

        // Act
        var result = await _service.AnalyzeConnectionAsync(
            "G8PZT-1", "M0LTE-5", from, to, null, false, true, 100, null, CancellationToken.None);

        // Assert
        result.Traces.Should().NotBeNull();
        result.Traces!.Data.Should().HaveCount(2);
        result.Traces.Data[0].FrameType.Should().Be("I");
        result.Traces.Data[0].Direction.Should().Contain("?");
        result.Traces.Data[1].FrameType.Should().Be("RR");
    }

    [Fact]
    public async Task AnalyzeConnectionAsync_WithoutIncludeTraces_DoesNotBuildTraces()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddHours(-1);
        var to = DateTimeOffset.UtcNow;
        
        _eventRepository.GetLinkEventsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), from, to, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EventsController.EventDto>());

        // Act
        var result = await _service.AnalyzeConnectionAsync(
            "G8PZT-1", "M0LTE-5", from, to, null, false, false, 100, null, CancellationToken.None);

        // Assert
        result.Traces.Should().BeNull();
        await _traceRepository.DidNotReceive().GetBidirectionalTracesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string[]>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeConnectionAsync_AssociatesTracesWithSessions()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddHours(-1);
        var to = DateTimeOffset.UtcNow;
        
        var sessionStart = from.AddMinutes(10).UtcDateTime;
        var sessionEnd = from.AddMinutes(30).UtcDateTime;
        
        _eventRepository.GetLinkEventsBetweenEndpointsAsync(
            Arg.Any<string>(), Arg.Any<string>(), from, to, Arg.Any<CancellationToken>())
            .Returns(new List<EventsController.EventDto>
            {
                CreateLinkUpEvent(sessionStart, "G8PZT-1", "M0LTE-5", 1),
                CreateLinkDownEvent(sessionEnd, 1200, 50, 25, 1)
            });

        var traces = new List<TracesController.TraceDto>
        {
            CreateTrace(1, from.AddMinutes(5).UtcDateTime, "G8PZT-1", "M0LTE-5", "I"),  // Before session
            CreateTrace(2, from.AddMinutes(15).UtcDateTime, "G8PZT-1", "M0LTE-5", "I"), // During session
            CreateTrace(3, from.AddMinutes(35).UtcDateTime, "G8PZT-1", "M0LTE-5", "I")  // After session
        };

        _traceRepository.GetBidirectionalTracesAsync(
            Arg.Any<string>(), Arg.Any<string>(), 
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), 
            Arg.Is<string[]?>(x => x == null), 
            Arg.Is<int>(x => x == 100), 
            Arg.Is<string?>(x => x == null), 
            Arg.Any<CancellationToken>())
            .Returns((traces, (string?)null));

        // Act
        var result = await _service.AnalyzeConnectionAsync(
            "G8PZT-1", "M0LTE-5", from, to, null, false, true, 100, null, CancellationToken.None);

        // Assert
        result.Traces!.Data[0].SessionId.Should().BeNull(); // Before session
        result.Traces.Data[1].SessionId.Should().Be(1);     // During session
        result.Traces.Data[2].SessionId.Should().BeNull();  // After session
    }

    #endregion

    #region Callsign Ordering Tests

    [Fact]
    public async Task AnalyzeConnectionAsync_NormalizesCallsignOrder_Alphabetically()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddHours(-1);
        var to = DateTimeOffset.UtcNow;
        
        _eventRepository.GetLinkEventsBetweenEndpointsAsync(
            "G8PZT-1", "M0LTE-5", from, to, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EventsController.EventDto>());

        _traceRepository.GetFrameStatisticsBetweenEndpointsAsync(
            "G8PZT-1", "M0LTE-5", from, to, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<FrameStatistic>());

        _eventRepository.GetEventsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<EventsController.EventDto>(), (string?)null, CountResult.NotRequested));

        // Act - Pass in reverse order
        var result = await _service.AnalyzeConnectionAsync(
            "M0LTE-5", "G8PZT-1", from, to, null, true, false, 100, null, CancellationToken.None);

        // Assert - Should be normalized to alphabetical order
        result.Connection.Callsign1.Should().Be("G8PZT-1");
        result.Connection.Callsign2.Should().Be("M0LTE-5");

        await _eventRepository.Received(1).GetLinkEventsBetweenEndpointsAsync(
            "G8PZT-1", "M0LTE-5", from, to, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Methods

    private static EventsController.EventDto CreateLinkUpEvent(DateTime timestamp, string local, string remote, int id)
    {
        var json = JsonDocument.Parse($$"""
        {
            "@type": "LinkUpEvent",
            "node": "{{local}}",
            "port": "1",
            "direction": "outgoing",
            "local": "{{local}}",
            "remote": "{{remote}}",
            "id": {{id}}
        }
        """);

        return new EventsController.EventDto(1, timestamp, json.RootElement.Clone());
    }

    private static EventsController.EventDto CreateLinkDownEvent(
        DateTime timestamp, int upForSecs, int framesSent, int framesReceived, int framesResent,
        int? bytesSent = null, int? bytesReceived = null, string? reason = null)
    {
        var optionalFields = new List<string>();
        if (bytesSent.HasValue) optionalFields.Add($"\"bytesSent\": {bytesSent.Value}");
        if (bytesReceived.HasValue) optionalFields.Add($"\"bytesRcvd\": {bytesReceived.Value}");
        if (reason != null) optionalFields.Add($"\"reason\": \"{reason}\"");

        var optionalJson = optionalFields.Count > 0 ? "," + string.Join(",", optionalFields) : "";

        var json = JsonDocument.Parse($$"""
        {
            "@type": "LinkDownEvent",
            "node": "NODE",
            "port": "1",
            "direction": "outgoing",
            "local": "LOCAL",
            "remote": "REMOTE",
            "id": 1,
            "upForSecs": {{upForSecs}},
            "frmsSent": {{framesSent}},
            "frmsRcvd": {{framesReceived}},
            "frmsResent": {{framesResent}},
            "frmsQueued": 0{{optionalJson}}
        }
        """);

        return new EventsController.EventDto(2, timestamp, json.RootElement.Clone());
    }

    private static EventsController.EventDto CreateLinkStatusEvent(
        DateTime timestamp, int upForSecs, int framesSent, int framesReceived, int framesResent, int l2RttMs)
    {
        var json = JsonDocument.Parse($$"""
        {
            "@type": "LinkStatus",
            "node": "NODE",
            "port": "1",
            "direction": "outgoing",
            "local": "LOCAL",
            "remote": "REMOTE",
            "id": 1,
            "upForSecs": {{upForSecs}},
            "frmsSent": {{framesSent}},
            "frmsRcvd": {{framesReceived}},
            "frmsResent": {{framesResent}},
            "frmsQueued": 0,
            "l2rttMs": {{l2RttMs}}
        }
        """);

        return new EventsController.EventDto(3, timestamp, json.RootElement.Clone());
    }

    private static TracesController.TraceDto CreateTrace(long id, DateTime timestamp, string source, string dest, string frameType)
    {
        var json = JsonDocument.Parse($$"""
        {
            "@type": "L2Trace",
            "reportFrom": "REPORTER",
            "srce": "{{source}}",
            "dest": "{{dest}}",
            "port": "1",
            "ctrl": 0,
            "l2Type": "{{frameType}}",
            "cr": "C"
        }
        """);

        return new TracesController.TraceDto(id, timestamp, json.RootElement.Clone());
    }

    #endregion
}
