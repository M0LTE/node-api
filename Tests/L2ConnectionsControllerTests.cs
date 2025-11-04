using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using node_api.Controllers;
using node_api.Services;
using System.Text.Json;
using ConnectionInfo = node_api.Services.ConnectionInfo;

namespace Tests;

public class L2ConnectionsControllerTests
{
    private readonly IL2ConnectionAnalysisService _service;
    private readonly L2ConnectionsController _controller;

    public L2ConnectionsControllerTests()
    {
        _service = Substitute.For<IL2ConnectionAnalysisService>();
        _controller = new L2ConnectionsController(_service);
    }

    [Fact]
    public async Task GetAsync_WithValidParameters_ReturnsOk()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;
        var analysis = CreateSampleAnalysis("G8PZT-1", "M0LTE-5", from, to);
        
        _service.AnalyzeConnectionAsync(
            "G8PZT-1", "M0LTE-5", from, to, null, true, true, 100, null, Arg.Any<CancellationToken>())
            .Returns(analysis);

        // Act
        var result = await _controller.GetAsync("G8PZT-1", "M0LTE-5", from, to);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAnalysis = Assert.IsType<L2ConnectionAnalysis>(okResult.Value);
        returnedAnalysis.Connection.Callsign1.Should().Be("G8PZT-1");
        returnedAnalysis.Connection.Callsign2.Should().Be("M0LTE-5");
    }

    [Fact]
    public async Task GetAsync_WithMissingCallsign1_ReturnsBadRequest()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;

        // Act
        var result = await _controller.GetAsync("", "M0LTE-5", from, to);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAsync_WithMissingCallsign2_ReturnsBadRequest()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;

        // Act
        var result = await _controller.GetAsync("G8PZT-1", "", from, to);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAsync_WithSameCallsigns_ReturnsBadRequest()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;

        // Act
        var result = await _controller.GetAsync("G8PZT-1", "G8PZT-1", from, to);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        badRequestResult.Value.Should().NotBeNull();
        var errorMessage = badRequestResult.Value.ToString();
        errorMessage.Should().Contain("different");
    }

    [Fact]
    public async Task GetAsync_WithIncludeMetricsFalse_DoesNotIncludeMetrics()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;
        var analysis = new L2ConnectionAnalysis(
            new ConnectionInfo("G8PZT-1", "M0LTE-5", new TimeRange(from, to)),
            Array.Empty<ConnectionSession>(),
            null, // No metrics
            null
        );
        
        _service.AnalyzeConnectionAsync(
            "G8PZT-1", "M0LTE-5", from, to, null, false, true, 100, null, Arg.Any<CancellationToken>())
            .Returns(analysis);

        // Act
        var result = await _controller.GetAsync("G8PZT-1", "M0LTE-5", from, to, includeMetrics: false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAnalysis = Assert.IsType<L2ConnectionAnalysis>(okResult.Value);
        returnedAnalysis.Metrics.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithIncludeTracesFalse_DoesNotIncludeTraces()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;
        var analysis = new L2ConnectionAnalysis(
            new ConnectionInfo("G8PZT-1", "M0LTE-5", new TimeRange(from, to)),
            Array.Empty<ConnectionSession>(),
            null,
            null // No traces
        );
        
        _service.AnalyzeConnectionAsync(
            "G8PZT-1", "M0LTE-5", from, to, null, true, false, 100, null, Arg.Any<CancellationToken>())
            .Returns(analysis);

        // Act
        var result = await _controller.GetAsync("G8PZT-1", "M0LTE-5", from, to, includeTraces: false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAnalysis = Assert.IsType<L2ConnectionAnalysis>(okResult.Value);
        returnedAnalysis.Traces.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithReportFromFilter_PassesToService()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;
        var reportFrom = new[] { "G8PZT-3", "M0ABC" };
        var analysis = CreateSampleAnalysis("G8PZT-1", "M0LTE-5", from, to);
        
        _service.AnalyzeConnectionAsync(
            "G8PZT-1", "M0LTE-5", from, to, reportFrom, true, true, 100, null, Arg.Any<CancellationToken>())
            .Returns(analysis);

        // Act
        var result = await _controller.GetAsync("G8PZT-1", "M0LTE-5", from, to, reportFrom);

        // Assert
        await _service.Received(1).AnalyzeConnectionAsync(
            "G8PZT-1", "M0LTE-5", from, to, 
            Arg.Is<string[]>(r => r.Length == 2 && r[0] == "G8PZT-3" && r[1] == "M0ABC"),
            true, true, 100, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WithCustomTracesLimit_PassesToService()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;
        var analysis = CreateSampleAnalysis("G8PZT-1", "M0LTE-5", from, to);
        
        _service.AnalyzeConnectionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string[]>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Is<int>(x => x == 250), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(analysis);

        // Act
        var result = await _controller.GetAsync("G8PZT-1", "M0LTE-5", from, to, tracesLimit: 250);

        // Assert
        await _service.Received(1).AnalyzeConnectionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string[]>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Is<int>(x => x == 250), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WithTracesLimitAboveMax_ClampsTo500()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;
        var analysis = CreateSampleAnalysis("G8PZT-1", "M0LTE-5", from, to);
        
        _service.AnalyzeConnectionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string[]>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Is<int>(x => x == 500), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(analysis);

        // Act
        var result = await _controller.GetAsync("G8PZT-1", "M0LTE-5", from, to, tracesLimit: 1000);

        // Assert
        await _service.Received(1).AnalyzeConnectionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string[]>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Is<int>(x => x == 500), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WithTracesLimitBelowMin_ClampsTo1()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;
        var analysis = CreateSampleAnalysis("G8PZT-1", "M0LTE-5", from, to);
        
        _service.AnalyzeConnectionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string[]>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Is<int>(x => x == 1), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(analysis);

        // Act
        var result = await _controller.GetAsync("G8PZT-1", "M0LTE-5", from, to, tracesLimit: 0);

        // Assert
        await _service.Received(1).AnalyzeConnectionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string[]>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Is<int>(x => x == 1), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WithTracesCursor_PassesToService()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;
        var cursor = "eyJ0cyI6IjIwMjQtMDEtMDEiLCJpZCI6MTIzfQ==";
        var analysis = CreateSampleAnalysis("G8PZT-1", "M0LTE-5", from, to);
        
        _service.AnalyzeConnectionAsync(
            "G8PZT-1", "M0LTE-5", from, to, null, true, true, 100, cursor, Arg.Any<CancellationToken>())
            .Returns(analysis);

        // Act
        var result = await _controller.GetAsync("G8PZT-1", "M0LTE-5", from, to, tracesCursor: cursor);

        // Assert
        await _service.Received(1).AnalyzeConnectionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string[]>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<int>(), cursor, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_CaseInsensitiveCallsigns_WorksCorrectly()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;
        var analysis = CreateSampleAnalysis("g8pzt-1", "m0lte-5", from, to);
        
        _service.AnalyzeConnectionAsync(
            "g8pzt-1", "m0lte-5", from, to, null, true, true, 100, null, Arg.Any<CancellationToken>())
            .Returns(analysis);

        // Act
        var result = await _controller.GetAsync("g8pzt-1", "m0lte-5", from, to);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    private static L2ConnectionAnalysis CreateSampleAnalysis(string call1, string call2, DateTimeOffset from, DateTimeOffset to)
    {
        return new L2ConnectionAnalysis(
            new ConnectionInfo(call1, call2, new TimeRange(from, to)),
            new List<ConnectionSession>
            {
                new ConnectionSession(
                    1,
                    new LinkUpEventInfo(DateTime.UtcNow.AddMinutes(-30), "NODE", "1", "outgoing", 123),
                    new LinkDownEventInfo(DateTime.UtcNow, 1800, 100, 50, 2, 10000, 5000, "Normal"),
                    Array.Empty<LinkStatusInfo>(),
                    new SessionMetrics(1800, 150, 0.013m, 250, 83)
                )
            },
            new OverallMetrics(
                1,
                1800,
                150,
                new DirectionalMetrics(75, 60, 5000, new Dictionary<string, int> { ["I"] = 60, ["RR"] = 15 }),
                new DirectionalMetrics(75, 50, 5000, new Dictionary<string, int> { ["I"] = 50, ["RR"] = 25 })
            ),
            new PagedTraces(
                new TracesPageInfo(100, null),
                Array.Empty<TraceWithContext>()
            )
        );
    }
}
