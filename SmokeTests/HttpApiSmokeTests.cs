using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SmokeTests;

/// <summary>
/// Smoke tests for the HTTP API endpoints
/// These tests verify that a deployed instance of the service is working as expected
/// </summary>
[Collection("Smoke Tests")]
public class HttpApiSmokeTests : IClassFixture<SmokeTestFixture>
{
    private readonly SmokeTestFixture _fixture;
    private readonly HttpClient _client;

    public HttpApiSmokeTests(SmokeTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task Health_Check_Should_Return_Success()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(
            $"Health check failed. Service may not be running at {_fixture.Settings.BaseUrl}");
    }

    [Fact]
    public async Task OpenAPI_Endpoint_Should_Be_Accessible()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/openapi/v1.json");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue("OpenAPI spec should be accessible");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("openapi", "Response should contain OpenAPI specification");
    }

    [Fact]
    public async Task Scalar_Documentation_Should_Be_Accessible()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/scalar");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue("Scalar documentation should be accessible");
    }

    [Fact]
    public async Task Validate_Endpoint_Should_Accept_Valid_NodeUpEvent()
    {
        // Arrange - Use valid callsign format (max 6 chars base + optional -SSID)
        var json = """
        {
            "@type": "NodeUpEvent",
            "nodeCall": "TEST-1",
            "nodeAlias": "SMOKE",
            "locator": "IO82VJ",
            "software": "SmokeTest",
            "version": "1.0"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue("Valid NodeUpEvent should pass validation");
        result.Stage.Should().Be("complete");
        result.DatagramType.Should().Be("NodeUpEvent");
    }

    [Fact]
    public async Task Validate_Endpoint_Should_Reject_Invalid_Datagram()
    {
        // Arrange
        var json = """
        {
            "@type": "NodeUpEvent",
            "nodeCall": "",
            "nodeAlias": ""
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse("Invalid datagram should fail validation");
        
        // The error could be in ValidationErrors or Error field depending on validation stage
        var hasError = (result.ValidationErrors != null && result.ValidationErrors.Count > 0) ||
                       !string.IsNullOrEmpty(result.Error);
        hasError.Should().BeTrue("Response should contain error information");
    }

    [Fact]
    public async Task Validate_Endpoint_Should_Handle_Malformed_JSON()
    {
        // Arrange
        var json = "{ invalid json }";

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.Stage.Should().Be("json_parsing");
    }

    [Fact]
    public async Task Validate_Endpoint_Should_Support_CORS()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/diagnostics/validate")
        {
            Content = new StringContent("{\"@type\": \"NodeUpEvent\"}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Origin", "https://example.com");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
    }

    // Helper class to deserialize validation responses
    private class ValidationResponse
    {
        public bool IsValid { get; set; }
        public string? Stage { get; set; }
        public string? DatagramType { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
        public List<ValidationError>? ValidationErrors { get; set; }
    }

    private class ValidationError
    {
        public string? PropertyName { get; set; }
        public string? ErrorMessage { get; set; }
        public string? AttemptedValue { get; set; }
    }
}
