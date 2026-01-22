using node_api.Services;
using System.Text.Json;
using Xunit;
using Microsoft.Extensions.Configuration;
using Dapper;

namespace Tests;

/// <summary>
/// Tests for the reported_time column extraction from JSON payloads.
/// These tests verify that the time field is correctly extracted from JSON
/// and stored in the reported_time column for traces, l3traces, and events.
/// </summary>
[Trait("Category", "DatabaseIntegration")]
[Trait("Category", "ManualTest")]
public class ReportedTimeExtractionTests : IDisposable
{
    private readonly ILogger<MySqlL2TraceRepository> _traceLogger;
    private readonly ILogger<MySqlL3TraceRepository> _l3TraceLogger;
    private readonly ILogger<MySqlEventRepository> _eventLogger;
    private readonly string _testRunId;
    private readonly DateTime _testStartTime;

    public ReportedTimeExtractionTests()
    {
        _testRunId = Guid.NewGuid().ToString("N")[..8];
        _testStartTime = DateTime.UtcNow.AddSeconds(-1);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<ReportedTimeExtractionTests>()
            .AddEnvironmentVariables()
            .Build();

        Database.Initialize(configuration);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _traceLogger = loggerFactory.CreateLogger<MySqlL2TraceRepository>();
        _l3TraceLogger = loggerFactory.CreateLogger<MySqlL3TraceRepository>();
        _eventLogger = loggerFactory.CreateLogger<MySqlEventRepository>();
    }

    public void Dispose()
    {
        try
        {
            using var connection = Database.GetConnection();
            var testEndTime = DateTime.UtcNow.AddSeconds(1);

            connection.Execute(@"
                DELETE FROM traces
                WHERE timestamp >= @start
                  AND timestamp <= @end
                  AND json LIKE @pattern",
                new {
                    start = _testStartTime,
                    end = testEndTime,
                    pattern = $"%REPORTED-TIME-TEST-{_testRunId}%"
                });

            connection.Execute(@"
                DELETE FROM l3traces
                WHERE timestamp >= @start
                  AND timestamp <= @end
                  AND json LIKE @pattern",
                new {
                    start = _testStartTime,
                    end = testEndTime,
                    pattern = $"%REPORTED-TIME-TEST-{_testRunId}%"
                });

            connection.Execute(@"
                DELETE FROM events
                WHERE timestamp >= @start
                  AND timestamp <= @end
                  AND json LIKE @pattern",
                new {
                    start = _testStartTime,
                    end = testEndTime,
                    pattern = $"%REPORTED-TIME-TEST-{_testRunId}%"
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to clean up test data: {ex.Message}");
        }

        GC.SuppressFinalize(this);
    }

    #region L2 Trace Tests

    [Fact]
    public async Task TraceRepository_Should_Extract_ReportedTime_From_Unix_Timestamp()
    {
        // Arrange
        var repository = new MySqlL2TraceRepository(_traceLogger);
        var testCallsign = $"REPORTED-TIME-TEST-{_testRunId}";
        var unixTime = 1705334400.123; // 2024-01-15 12:00:00.123 UTC
        var expectedTime = DateTimeOffset.FromUnixTimeMilliseconds((long)(unixTime * 1000)).UtcDateTime;

        var json = JsonSerializer.Serialize(new {
            type = "L2Trace",
            reportFrom = testCallsign,
            time = unixTime
        });

        // Act
        await repository.InsertTraceAsync(json, DateTime.UtcNow);
        await Task.Delay(100);

        // Assert
        using var conn = Database.GetConnection();
        var result = await conn.QuerySingleOrDefaultAsync<ReportedTimeRow>(@"
            SELECT reported_time
            FROM traces
            WHERE json LIKE @pattern
            ORDER BY id DESC
            LIMIT 1",
            new { pattern = $"%{testCallsign}%" });

        Assert.NotNull(result);
        Assert.NotNull(result.reported_time);
        Assert.Equal(expectedTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                     result.reported_time.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"));
    }

    [Fact]
    public async Task TraceRepository_Should_Handle_Missing_Time_Field()
    {
        // Arrange
        var repository = new MySqlL2TraceRepository(_traceLogger);
        var testCallsign = $"REPORTED-TIME-TEST-NO-TIME-{_testRunId}";

        var json = JsonSerializer.Serialize(new {
            type = "L2Trace",
            reportFrom = testCallsign
            // No time field
        });

        // Act
        await repository.InsertTraceAsync(json, DateTime.UtcNow);
        await Task.Delay(100);

        // Assert
        using var conn = Database.GetConnection();
        var result = await conn.QuerySingleOrDefaultAsync<ReportedTimeRow>(@"
            SELECT reported_time
            FROM traces
            WHERE json LIKE @pattern
            ORDER BY id DESC
            LIMIT 1",
            new { pattern = $"%{testCallsign}%" });

        Assert.NotNull(result);
        Assert.Null(result.reported_time);
    }

    [Fact]
    public async Task TraceRepository_Should_Handle_Invalid_Time_Format()
    {
        // Arrange
        var repository = new MySqlL2TraceRepository(_traceLogger);
        var testCallsign = $"REPORTED-TIME-TEST-INVALID-{_testRunId}";

        var json = JsonSerializer.Serialize(new {
            type = "L2Trace",
            reportFrom = testCallsign,
            time = "not-a-number" // Invalid format
        });

        // Act
        await repository.InsertTraceAsync(json, DateTime.UtcNow);
        await Task.Delay(100);

        // Assert - Should insert successfully with NULL reported_time
        using var conn = Database.GetConnection();
        var result = await conn.QuerySingleOrDefaultAsync<ReportedTimeRow>(@"
            SELECT reported_time
            FROM traces
            WHERE json LIKE @pattern
            ORDER BY id DESC
            LIMIT 1",
            new { pattern = $"%{testCallsign}%" });

        Assert.NotNull(result);
        Assert.Null(result.reported_time);
    }

    #endregion

    #region L3 Trace Tests

    [Fact]
    public async Task L3TraceRepository_Should_Extract_ReportedTime_From_Unix_Timestamp()
    {
        // Arrange
        var repository = new MySqlL3TraceRepository(_l3TraceLogger);
        var testCallsign = $"REPORTED-TIME-TEST-L3-{_testRunId}";
        var unixTime = 1705334400.456; // 2024-01-15 12:00:00.456 UTC
        var expectedTime = DateTimeOffset.FromUnixTimeMilliseconds((long)(unixTime * 1000)).UtcDateTime;

        var json = JsonSerializer.Serialize(new {
            type = "L3Trace",
            reportFrom = testCallsign,
            time = unixTime
        });

        // Act
        await repository.InsertL3TraceAsync(json, DateTime.UtcNow);
        await Task.Delay(100);

        // Assert
        using var conn = Database.GetConnection();
        var result = await conn.QuerySingleOrDefaultAsync<ReportedTimeRow>(@"
            SELECT reported_time
            FROM l3traces
            WHERE json LIKE @pattern
            ORDER BY id DESC
            LIMIT 1",
            new { pattern = $"%{testCallsign}%" });

        Assert.NotNull(result);
        Assert.NotNull(result.reported_time);
        Assert.Equal(expectedTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                     result.reported_time.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"));
    }

    [Fact]
    public async Task L3TraceRepository_Should_Handle_Missing_Time_Field()
    {
        // Arrange
        var repository = new MySqlL3TraceRepository(_l3TraceLogger);
        var testCallsign = $"REPORTED-TIME-TEST-L3-NO-TIME-{_testRunId}";

        var json = JsonSerializer.Serialize(new {
            type = "L3Trace",
            reportFrom = testCallsign
        });

        // Act
        await repository.InsertL3TraceAsync(json, DateTime.UtcNow);
        await Task.Delay(100);

        // Assert
        using var conn = Database.GetConnection();
        var result = await conn.QuerySingleOrDefaultAsync<ReportedTimeRow>(@"
            SELECT reported_time
            FROM l3traces
            WHERE json LIKE @pattern
            ORDER BY id DESC
            LIMIT 1",
            new { pattern = $"%{testCallsign}%" });

        Assert.NotNull(result);
        Assert.Null(result.reported_time);
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task EventRepository_Should_Extract_ReportedTime_From_Unix_Timestamp()
    {
        // Arrange
        var repository = new MySqlEventRepository(_eventLogger);
        var testNode = $"REPORTED-TIME-TEST-EVENT-{_testRunId}";
        var unixTime = 1705334400.789; // 2024-01-15 12:00:00.789 UTC
        var expectedTime = DateTimeOffset.FromUnixTimeMilliseconds((long)(unixTime * 1000)).UtcDateTime;

        var json = JsonSerializer.Serialize(new {
            type = "NodeUpEvent",
            node = testNode,
            time = unixTime
        });

        // Act
        await repository.InsertEventAsync(json, DateTime.UtcNow);
        await Task.Delay(100);

        // Assert
        using var conn = Database.GetConnection();
        var result = await conn.QuerySingleOrDefaultAsync<ReportedTimeRow>(@"
            SELECT reported_time
            FROM events
            WHERE json LIKE @pattern
            ORDER BY id DESC
            LIMIT 1",
            new { pattern = $"%{testNode}%" });

        Assert.NotNull(result);
        Assert.NotNull(result.reported_time);
        Assert.Equal(expectedTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                     result.reported_time.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"));
    }

    [Fact]
    public async Task EventRepository_Should_Handle_Missing_Time_Field()
    {
        // Arrange
        var repository = new MySqlEventRepository(_eventLogger);
        var testNode = $"REPORTED-TIME-TEST-EVENT-NO-TIME-{_testRunId}";

        var json = JsonSerializer.Serialize(new {
            type = "NodeUpEvent",
            node = testNode
        });

        // Act
        await repository.InsertEventAsync(json, DateTime.UtcNow);
        await Task.Delay(100);

        // Assert
        using var conn = Database.GetConnection();
        var result = await conn.QuerySingleOrDefaultAsync<ReportedTimeRow>(@"
            SELECT reported_time
            FROM events
            WHERE json LIKE @pattern
            ORDER BY id DESC
            LIMIT 1",
            new { pattern = $"%{testNode}%" });

        Assert.NotNull(result);
        Assert.Null(result.reported_time);
    }

    #endregion

    #region Precision Tests

    [Fact]
    public async Task ReportedTime_Should_Preserve_Millisecond_Precision()
    {
        // Arrange
        var repository = new MySqlL2TraceRepository(_traceLogger);
        var testCallsign = $"REPORTED-TIME-TEST-PRECISION-{_testRunId}";
        var unixTime = 1705334400.999; // Should preserve .999 milliseconds
        var expectedTime = DateTimeOffset.FromUnixTimeMilliseconds((long)(unixTime * 1000)).UtcDateTime;

        var json = JsonSerializer.Serialize(new {
            type = "L2Trace",
            reportFrom = testCallsign,
            time = unixTime
        });

        // Act
        await repository.InsertTraceAsync(json, DateTime.UtcNow);
        await Task.Delay(100);

        // Assert
        using var conn = Database.GetConnection();
        var result = await conn.QuerySingleOrDefaultAsync<ReportedTimeRow>(@"
            SELECT reported_time
            FROM traces
            WHERE json LIKE @pattern
            ORDER BY id DESC
            LIMIT 1",
            new { pattern = $"%{testCallsign}%" });

        Assert.NotNull(result);
        Assert.NotNull(result.reported_time);

        // Verify millisecond precision
        var actualMs = result.reported_time.Value.Millisecond;
        var expectedMs = expectedTime.Millisecond;
        Assert.Equal(expectedMs, actualMs);
    }

    #endregion

    private class ReportedTimeRow
    {
        public DateTime? reported_time { get; set; }
    }
}
