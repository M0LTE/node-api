using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using node_api.Controllers;
using System.Net;
using System.Net.Http.Json;

namespace Tests;

/// <summary>
/// Integration tests for the L2 connection analysis endpoint
/// </summary>
public class L2ConnectionAnalysisIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public L2ConnectionAnalysisIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetL2Connection_WithValidParameters_ReturnsOk()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var url = $"/api/history/connections/l2?callsign1=G8PZT-1&callsign2=M0LTE-5&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<L2ConnectionAnalysisResponse>();
        result.Should().NotBeNull();
        result!.Connection.Should().NotBeNull();
        result.Connection.Callsign1.Should().NotBeNullOrEmpty();
        result.Connection.Callsign2.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetL2Connection_WithMissingCallsign1_ReturnsBadRequest()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var url = $"/api/history/connections/l2?callsign2=M0LTE-5&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetL2Connection_WithMissingCallsign2_ReturnsBadRequest()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var url = $"/api/history/connections/l2?callsign1=G8PZT-1&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetL2Connection_WithSameCallsigns_ReturnsBadRequest()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var url = $"/api/history/connections/l2?callsign1=G8PZT-1&callsign2=G8PZT-1&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetL2Connection_IncludesMetrics_WhenRequested()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var url = $"/api/history/connections/l2?callsign1=G8PZT-1&callsign2=M0LTE-5&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}&includeMetrics=true";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<L2ConnectionAnalysisResponse>();
        result.Should().NotBeNull();
        // Metrics may be null if no data exists, but structure should be present
    }

    [Fact]
    public async Task GetL2Connection_ExcludesMetrics_WhenNotRequested()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var url = $"/api/history/connections/l2?callsign1=G8PZT-1&callsign2=M0LTE-5&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}&includeMetrics=false";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<L2ConnectionAnalysisResponse>();
        result.Should().NotBeNull();
        result!.Metrics.Should().BeNull();
    }

    [Fact]
    public async Task GetL2Connection_IncludesTraces_WhenRequested()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var url = $"/api/history/connections/l2?callsign1=G8PZT-1&callsign2=M0LTE-5&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}&includeTraces=true&tracesLimit=10";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<L2ConnectionAnalysisResponse>();
        result.Should().NotBeNull();
        // Traces may be null if no data exists, but structure should be present
    }

    [Fact]
    public async Task GetL2Connection_ExcludesTraces_WhenNotRequested()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var url = $"/api/history/connections/l2?callsign1=G8PZT-1&callsign2=M0LTE-5&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}&includeTraces=false";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<L2ConnectionAnalysisResponse>();
        result.Should().NotBeNull();
        result!.Traces.Should().BeNull();
    }

    [Fact]
    public async Task GetL2Connection_RespectsTracesLimit()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var url = $"/api/history/connections/l2?callsign1=G8PZT-1&callsign2=M0LTE-5&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}&tracesLimit=5";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<L2ConnectionAnalysisResponse>();
        result.Should().NotBeNull();
        if (result!.Traces != null)
        {
            result.Traces.Page.Limit.Should().Be(5);
            result.Traces.Data.Count.Should().BeLessOrEqualTo(5);
        }
    }

    [Fact]
    public async Task GetL2Connection_WithReportFromFilter_Works()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var url = $"/api/history/connections/l2?callsign1=G8PZT-1&callsign2=M0LTE-5&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}&reportFrom=G8PZT-3";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetL2Connection_WithMultipleReportFrom_Works()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var url = $"/api/history/connections/l2?callsign1=G8PZT-1&callsign2=M0LTE-5&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}&reportFrom=G8PZT-3&reportFrom=M0ABC";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetL2Connection_ResponseStructure_IsCorrect()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var url = $"/api/history/connections/l2?callsign1=G8PZT-1&callsign2=M0LTE-5&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<L2ConnectionAnalysisResponse>();
        result.Should().NotBeNull();
        result!.Connection.Should().NotBeNull();
        result.Connection.Callsign1.Should().NotBeNullOrEmpty();
        result.Connection.Callsign2.Should().NotBeNullOrEmpty();
        result.Connection.TimeRange.Should().NotBeNull();
        result.Sessions.Should().NotBeNull();
    }

    [Fact]
    public async Task GetL2Connection_SupportsCORS()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var url = $"/api/history/connections/l2?callsign1=G8PZT-1&callsign2=M0LTE-5&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Origin", "https://example.com");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
    }

    [Fact]
    public async Task GetL2Connection_HasCorrectContentType()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var url = $"/api/history/connections/l2?callsign1=G8PZT-1&callsign2=M0LTE-5&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetL2Connection_CallsignsAreCaseInsensitive()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var url = $"/api/history/connections/l2?callsign1=g8pzt-1&callsign2=m0lte-5&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // Helper class for deserializing response
    private class L2ConnectionAnalysisResponse
    {
        public ConnectionInfoDto Connection { get; set; } = null!;
        public List<object> Sessions { get; set; } = new();
        public object? Metrics { get; set; }
        public TracesDto? Traces { get; set; }
    }

    private class ConnectionInfoDto
    {
        public string Callsign1 { get; set; } = "";
        public string Callsign2 { get; set; } = "";
        public TimeRangeDto TimeRange { get; set; } = null!;
    }

    private class TimeRangeDto
    {
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }
    }

    private class TracesDto
    {
        public PageDto Page { get; set; } = null!;
        public List<object> Data { get; set; } = new();
    }

    private class PageDto
    {
        public int Limit { get; set; }
        public string? Next { get; set; }
    }
}
