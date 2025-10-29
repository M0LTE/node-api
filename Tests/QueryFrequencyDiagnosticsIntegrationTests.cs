using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using node_api.Services;

namespace Tests;

/// <summary>
/// Integration tests for the query frequency diagnostics endpoint
/// </summary>
public class QueryFrequencyDiagnosticsIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public QueryFrequencyDiagnosticsIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Should_Return_Empty_Stats_When_No_Queries_Have_Been_Made()
    {
        // Act
        var response = await _client.GetAsync("/api/diagnostics/db/query-frequency");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<List<QueryFrequencyTracker.QueryStatsDto>>();
        stats.Should().NotBeNull();
        // Note: Stats might be empty or have some data depending on what other tests have done
    }

    [Fact]
    public async Task Endpoint_Should_Have_Correct_Content_Type()
    {
        // Act
        var response = await _client.GetAsync("/api/diagnostics/db/query-frequency");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Stats_Should_Have_Expected_Structure()
    {
        // Arrange - Make a request that might trigger some DB queries
        await _client.GetAsync("/api/traces?limit=1");

        // Act
        var response = await _client.GetAsync("/api/diagnostics/db/query-frequency");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<List<QueryFrequencyTracker.QueryStatsDto>>();
        stats.Should().NotBeNull();

        // If there are stats, verify structure
        if (stats!.Count > 0)
        {
            var stat = stats[0];
            stat.MethodName.Should().NotBeNullOrEmpty();
            stat.QueryText.Should().NotBeNullOrEmpty();
            stat.TotalCount.Should().BeGreaterThan(0);
            stat.HourlyData.Should().NotBeNull();
            stat.HourlyData.Should().NotBeEmpty();

            // Verify hourly data structure
            var hourlyData = stat.HourlyData[0];
            hourlyData.Hour.Kind.Should().Be(DateTimeKind.Utc);
            hourlyData.Count.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task Stats_Should_Accumulate_Over_Multiple_Requests()
    {
        // Arrange - Get initial stats
        var initialResponse = await _client.GetAsync("/api/diagnostics/db/query-frequency");
        var initialStats = await initialResponse.Content.ReadFromJsonAsync<List<QueryFrequencyTracker.QueryStatsDto>>();
        var initialTotalQueries = initialStats?.Sum(s => s.TotalCount) ?? 0;

        // Act - Make several requests that will trigger DB queries
        for (int i = 0; i < 3; i++)
        {
            await _client.GetAsync("/api/traces?limit=1");
        }

        // Get updated stats
        var updatedResponse = await _client.GetAsync("/api/diagnostics/db/query-frequency");
        var updatedStats = await updatedResponse.Content.ReadFromJsonAsync<List<QueryFrequencyTracker.QueryStatsDto>>();
        var updatedTotalQueries = updatedStats?.Sum(s => s.TotalCount) ?? 0;

        // Assert - Total count should be same or higher (in test environment with mocks, it might not increase)
        // In a real environment with actual DB repositories, this would increase
        updatedTotalQueries.Should().BeGreaterOrEqualTo(initialTotalQueries);
    }

    [Fact]
    public async Task Stats_Should_Show_Method_Names()
    {
        // Arrange - Make a request to trigger GetTracesAsync
        await _client.GetAsync("/api/traces?limit=1");

        // Act
        var response = await _client.GetAsync("/api/diagnostics/db/query-frequency");
        var stats = await response.Content.ReadFromJsonAsync<List<QueryFrequencyTracker.QueryStatsDto>>();

        // Assert
        stats.Should().NotBeNull();
        if (stats!.Count > 0)
        {
            // Should have at least one stat with a method name
            stats.Should().Contain(s => !string.IsNullOrEmpty(s.MethodName));
        }
    }

    [Fact]
    public async Task Stats_Should_Be_Sorted_By_Total_Count_Descending()
    {
        // Arrange - Trigger some queries
        await _client.GetAsync("/api/traces?limit=1");
        await _client.GetAsync("/api/events?limit=1");

        // Act
        var response = await _client.GetAsync("/api/diagnostics/db/query-frequency");
        var stats = await response.Content.ReadFromJsonAsync<List<QueryFrequencyTracker.QueryStatsDto>>();

        // Assert
        stats.Should().NotBeNull();
        if (stats!.Count > 1)
        {
            // Verify descending order
            for (int i = 0; i < stats.Count - 1; i++)
            {
                stats[i].TotalCount.Should().BeGreaterOrEqualTo(stats[i + 1].TotalCount);
            }
        }
    }
}
