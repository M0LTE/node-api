using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using node_api.Services;

namespace Tests;

public class QueryFrequencyTrackerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public QueryFrequencyTrackerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public void Should_Track_Query_Execution()
    {
        // Arrange
        var tracker = new QueryFrequencyTracker();

        // Act
        tracker.RecordQuery("GetTracesAsync", "SELECT * FROM traces WHERE id = @id");
        tracker.RecordQuery("GetTracesAsync", "SELECT * FROM traces WHERE id = @id");
        tracker.RecordQuery("GetEventsAsync", "SELECT * FROM events WHERE id = @id");

        // Assert
        var stats = tracker.GetStats();
        stats.Should().HaveCount(2);
        
        var traceStat = stats.FirstOrDefault(s => s.MethodName == "GetTracesAsync");
        traceStat.Should().NotBeNull();
        traceStat!.TotalCount.Should().Be(2);
        
        var eventStat = stats.FirstOrDefault(s => s.MethodName == "GetEventsAsync");
        eventStat.Should().NotBeNull();
        eventStat!.TotalCount.Should().Be(1);
    }

    [Fact]
    public void Should_Track_Hourly_Data()
    {
        // Arrange
        var tracker = new QueryFrequencyTracker();
        var methodName = "GetNodesAsync";
        var query = "SELECT * FROM nodes";

        // Act - Record multiple queries in the same hour
        for (int i = 0; i < 5; i++)
        {
            tracker.RecordQuery(methodName, query);
        }

        // Assert
        var stats = tracker.GetStats();
        stats.Should().HaveCount(1);
        
        var stat = stats.First();
        stat.HourlyData.Should().HaveCount(1);
        stat.HourlyData.First().Count.Should().Be(5);
    }

    [Fact]
    public void Should_Distinguish_Different_Queries()
    {
        // Arrange
        var tracker = new QueryFrequencyTracker();

        // Act
        tracker.RecordQuery("GetTracesAsync", "SELECT * FROM traces WHERE source = @source");
        tracker.RecordQuery("GetTracesAsync", "SELECT * FROM traces WHERE dest = @dest");

        // Assert
        var stats = tracker.GetStats();
        stats.Should().HaveCount(2);
        stats.All(s => s.MethodName == "GetTracesAsync").Should().BeTrue();
    }

    [Fact]
    public void Should_Update_LastSeen()
    {
        // Arrange
        var tracker = new QueryFrequencyTracker();
        var methodName = "TestMethod";
        var query = "SELECT 1";

        // Act
        tracker.RecordQuery(methodName, query);
        var initialStats = tracker.GetStats();
        var initialLastSeen = initialStats.First().LastSeen;

        // Small delay to ensure time difference
        Thread.Sleep(10);
        
        tracker.RecordQuery(methodName, query);
        var updatedStats = tracker.GetStats();
        var updatedLastSeen = updatedStats.First().LastSeen;

        // Assert
        updatedLastSeen.Should().BeAfter(initialLastSeen);
    }

    [Fact]
    public void Should_Return_Empty_Stats_When_No_Queries_Recorded()
    {
        // Arrange
        var tracker = new QueryFrequencyTracker();

        // Act
        var stats = tracker.GetStats();

        // Assert
        stats.Should().BeEmpty();
    }

    [Fact]
    public async Task Diagnostics_Endpoint_Should_Return_Query_Stats()
    {
        // Arrange - The endpoint should exist and return OK even with no data

        // Act
        var response = await _client.GetAsync("/api/diagnostics/db/query-frequency");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<List<QueryFrequencyTracker.QueryStatsDto>>();
        stats.Should().NotBeNull();
    }

    [Fact]
    public void Should_Truncate_Long_Queries_For_Key_Generation()
    {
        // Arrange
        var tracker = new QueryFrequencyTracker();
        var longQuery = new string('x', 200); // 200 character query

        // Act
        tracker.RecordQuery("LongQueryMethod", longQuery);
        var stats = tracker.GetStats();

        // Assert
        stats.Should().HaveCount(1);
        // The query text should be the full query, not truncated
        stats.First().QueryText.Should().HaveLength(200);
    }

    [Fact]
    public void Should_Sort_Stats_By_Total_Count_Descending()
    {
        // Arrange
        var tracker = new QueryFrequencyTracker();

        // Act - Record queries with different frequencies
        tracker.RecordQuery("Method1", "Query1");
        
        tracker.RecordQuery("Method2", "Query2");
        tracker.RecordQuery("Method2", "Query2");
        tracker.RecordQuery("Method2", "Query2");

        tracker.RecordQuery("Method3", "Query3");
        tracker.RecordQuery("Method3", "Query3");

        // Assert
        var stats = tracker.GetStats();
        stats.Should().HaveCount(3);
        stats[0].TotalCount.Should().Be(3); // Method2
        stats[1].TotalCount.Should().Be(2); // Method3
        stats[2].TotalCount.Should().Be(1); // Method1
    }
}
