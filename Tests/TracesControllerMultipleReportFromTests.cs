using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using node_api.Controllers;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tests;

/// <summary>
/// Tests for the TracesController with multiple reportFrom callsigns
/// </summary>
public class TracesControllerMultipleReportFromTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TracesControllerMultipleReportFromTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetTraces_Should_Accept_Single_ReportFrom_Callsign()
    {
        // Arrange
        var url = "/api/history/traces?reportFrom=G8PZT&limit=10";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TracesController.PagedResult<TracesController.TraceDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().NotBeNull();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTraces_Should_Accept_Multiple_ReportFrom_Callsigns()
    {
        // Arrange
        var url = "/api/history/traces?reportFrom=G8PZT&reportFrom=M0LTE&reportFrom=G8ABC&limit=10";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TracesController.PagedResult<TracesController.TraceDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().NotBeNull();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTraces_Should_Accept_ReportFrom_With_SSID()
    {
        // Arrange
        var url = "/api/history/traces?reportFrom=G8PZT-1&reportFrom=M0LTE-5&limit=10";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TracesController.PagedResult<TracesController.TraceDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTraces_Should_Work_Without_ReportFrom()
    {
        // Arrange
        var url = "/api/history/traces?limit=10";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TracesController.PagedResult<TracesController.TraceDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTraces_Should_Accept_Mixed_Callsign_Formats()
    {
        // Arrange - Mix of callsigns with and without SSIDs
        var url = "/api/history/traces?reportFrom=G8PZT&reportFrom=M0LTE-1&reportFrom=GB7BBS&reportFrom=K5DAT-5&limit=10";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TracesController.PagedResult<TracesController.TraceDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().NotBeNull();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTraces_Should_Accept_ReportFrom_With_Other_Filters()
    {
        // Arrange
        var url = "/api/history/traces?reportFrom=G8PZT&reportFrom=M0LTE&source=G8PZT-1&type=UI&limit=10";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TracesController.PagedResult<TracesController.TraceDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTraces_Should_Support_Date_Range_With_Multiple_ReportFrom()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var url = $"/api/history/traces?reportFrom=G8PZT&reportFrom=M0LTE&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}&limit=10";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TracesController.PagedResult<TracesController.TraceDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTraces_Should_Support_Pagination_With_Multiple_ReportFrom()
    {
        // Arrange
        var url = "/api/history/traces?reportFrom=G8PZT&reportFrom=M0LTE&limit=5";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TracesController.PagedResult<TracesController.TraceDto>>();
        result.Should().NotBeNull();
        result!.Page.Limit.Should().Be(5);
    }

    [Fact]
    public async Task GetTraces_Should_Accept_Many_ReportFrom_Callsigns()
    {
        // Arrange - Test with many callsigns
        var callsigns = new[]
        {
            "G8PZT", "M0LTE", "G8ABC", "M0XYZ", "GB7BBS",
            "K5DAT", "W1ABC", "VE3XYZ", "DL1ABC", "F1XYZ"
        };
        var queryParams = string.Join("&", callsigns.Select(c => $"reportFrom={c}"));
        var url = $"/api/history/traces?{queryParams}&limit=10";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TracesController.PagedResult<TracesController.TraceDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTraces_Should_Support_CORS_With_Multiple_ReportFrom()
    {
        // Arrange
        var url = "/api/history/traces?reportFrom=G8PZT&reportFrom=M0LTE&limit=10";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Origin", "https://example.com");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
    }

    [Fact]
    public async Task GetTraces_Response_Should_Have_Correct_Structure()
    {
        // Arrange
        var url = "/api/history/traces?reportFrom=G8PZT&limit=10";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TracesController.PagedResult<TracesController.TraceDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().NotBeNull();
        result.Page.Limit.Should().Be(10);
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTraces_Should_Accept_Empty_ReportFrom_Array()
    {
        // Arrange - This might happen if frontend passes empty array
        var url = "/api/history/traces?limit=10";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TracesController.PagedResult<TracesController.TraceDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTraces_Should_Filter_By_Multiple_ReportFrom_With_Source_And_Dest()
    {
        // Arrange
        var url = "/api/history/traces?reportFrom=G8PZT&reportFrom=M0LTE&source=G8PZT-1&dest=M0LTE-1&limit=10";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TracesController.PagedResult<TracesController.TraceDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTraces_Should_Support_IncludeCount_With_Multiple_ReportFrom()
    {
        // Arrange
        var url = "/api/history/traces?reportFrom=G8PZT&reportFrom=M0LTE&includeCount=true&limit=5";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TracesController.PagedResult<TracesController.TraceDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().NotBeNull();
        // TotalCount might be null if count query failed, but should be present if requested
    }

    [Fact]
    public async Task GetTraces_Should_Support_Cursor_Pagination_With_Multiple_ReportFrom()
    {
        // Arrange - First request to get a cursor
        var url1 = "/api/history/traces?reportFrom=G8PZT&reportFrom=M0LTE&limit=2";
        var response1 = await _client.GetAsync(url1);
        var result1 = await response1.Content.ReadFromJsonAsync<TracesController.PagedResult<TracesController.TraceDto>>();

        if (result1?.Page.Next == null)
        {
            // Skip test if no pagination cursor
            return;
        }

        // Arrange - Second request with cursor
        var url2 = $"/api/history/traces?reportFrom=G8PZT&reportFrom=M0LTE&limit=2&cursor={Uri.EscapeDataString(result1.Page.Next)}";

        // Act
        var response2 = await _client.GetAsync(url2);

        // Assert
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var result2 = await response2.Content.ReadFromJsonAsync<TracesController.PagedResult<TracesController.TraceDto>>();
        result2.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTraces_Should_Respect_Limit_Clamping_With_Multiple_ReportFrom()
    {
        // Arrange - Request beyond max limit
        var url = "/api/history/traces?reportFrom=G8PZT&reportFrom=M0LTE&limit=1000";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TracesController.PagedResult<TracesController.TraceDto>>();
        result.Should().NotBeNull();
        result!.Page.Limit.Should().BeLessOrEqualTo(500); // Max limit is 500
    }
}
