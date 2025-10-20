using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace Tests;

// Custom factory that configures the app for testing
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureServices(services =>
        {
            // Remove hosted services that require external dependencies
            var hostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();
            
            foreach (var service in hostedServices)
            {
                services.Remove(service);
            }
        });
    }
}

public class CorsIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CorsIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task API_Should_Return_CORS_Headers_On_Simple_Request()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/traces");
        request.Headers.Add("Origin", "https://example.com");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("*");
    }

    [Fact]
    public async Task API_Should_Handle_Preflight_Request()
    {
        // Arrange - Simulate a preflight OPTIONS request from a browser
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/traces");
        request.Headers.Add("Origin", "https://example.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify CORS headers are present
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("*");
        
        response.Headers.Should().ContainKey("Access-Control-Allow-Methods");
        var allowedMethods = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Methods"));
        allowedMethods.Should().Contain("GET");
        
        response.Headers.Should().ContainKey("Access-Control-Allow-Headers");
        var allowedHeaders = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Headers"));
        allowedHeaders.Should().Contain("content-type");
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://localhost:3000")]
    [InlineData("https://packet-monitor.oarc.uk")]
    [InlineData("http://192.168.1.100:8080")]
    public async Task API_Should_Accept_Requests_From_Any_Origin(string origin)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/traces");
        request.Headers.Add("Origin", origin);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("*");
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Preflight_Should_Allow_All_HTTP_Methods(string method)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/traces");
        request.Headers.Add("Origin", "https://example.com");
        request.Headers.Add("Access-Control-Request-Method", method);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey("Access-Control-Allow-Methods");
        
        var allowedMethods = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Methods"));
        allowedMethods.Should().Contain(method);
    }

    [Theory]
    [InlineData("Content-Type")]
    [InlineData("Authorization")]
    [InlineData("X-Custom-Header")]
    [InlineData("Accept")]
    public async Task Preflight_Should_Allow_All_Request_Headers(string headerName)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/traces");
        request.Headers.Add("Origin", "https://example.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", headerName);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey("Access-Control-Allow-Headers");
        
        var allowedHeaders = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Headers"));
        allowedHeaders.Should().ContainEquivalentOf(headerName);
    }

    [Fact]
    public async Task GET_Request_With_Origin_Should_Include_CORS_Headers()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/traces?limit=10");
        request.Headers.Add("Origin", "https://example.com");

        // Act
        var response = await _client.SendAsync(request);

        // Assert - Request should succeed and include CORS headers
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("*");
    }

    [Fact]
    public async Task Request_Without_Origin_Should_Still_Work()
    {
        // Arrange - Request without Origin header (same-origin request)
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/traces?limit=10");
        // Don't add Origin header

        // Act
        var response = await _client.SendAsync(request);

        // Assert - Should still work, CORS only applies to cross-origin requests
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task EventsController_Should_Also_Have_CORS_Enabled()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/events");
        request.Headers.Add("Origin", "https://example.com");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("*");
    }

    [Fact]
    public async Task Root_Endpoint_Should_Have_CORS_Enabled()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Origin", "https://example.com");
        
        // Configure client to not follow redirects so we can check the redirect response
        var clientWithoutRedirect = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        
        // Act
        var response = await clientWithoutRedirect.SendAsync(request);

        // Assert
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("*");
    }
}
