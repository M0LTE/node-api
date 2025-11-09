using node_api.Services;
using System.Text.Json;
using Xunit;
using Microsoft.Extensions.Configuration;
using Dapper;

namespace Tests;

/// <summary>
/// Database integration tests that verify compatibility with the deployed MySQL database.
/// These tests require a live database connection and should be run manually after:
/// - Schema migrations
/// - Database configuration changes
/// - Connection string parameter changes
/// - Major refactoring of repository code
/// 
/// To run these tests:
/// dotnet test --filter "Category=DatabaseIntegration"
/// 
/// Or in Visual Studio Test Explorer, filter by Trait: Category=DatabaseIntegration
/// 
/// Setup:
/// 1. Initialize user secrets: dotnet user-secrets init
/// 2. Set database credentials:
///    dotnet user-secrets set "DB_HOST" "your-host"
///    dotnet user-secrets set "DB_PORT" "3306"
///    dotnet user-secrets set "DB_USER" "your-user"
///    dotnet user-secrets set "DB_PASSWORD" "your-password"
///    dotnet user-secrets set "DB_NAME" "your-database"
/// 
/// Note: These tests clean up after themselves by deleting ONLY test-specific data.
/// Test data is marked with special markers (TEST-INT-{guid}, TEST-NODE-{guid}, etc.)
/// </summary>
[Trait("Category", "DatabaseIntegration")]
[Trait("Category", "ManualTest")]
public class DatabaseIntegrationTests : IDisposable
{
    private readonly ILogger<MySqlTraceRepository> _traceLogger;
    private readonly ILogger<MySqlEventRepository> _eventLogger;
    private readonly ILogger<MySqlNetworkStateRepository> _stateLogger;
    private readonly ILogger<MySqlErroredMessageRepository> _errorLogger;
    
    // Unique test run identifier to safely distinguish test data from production data
    private readonly string _testRunId;
    // Track test start time for efficient timestamp-based filtering
    private readonly DateTime _testStartTime;
    
    public DatabaseIntegrationTests()
    {
        // Generate unique ID for this test run to safely identify test data
        _testRunId = Guid.NewGuid().ToString("N")[..8];
        _testStartTime = DateTime.UtcNow.AddSeconds(-1); // Buffer for timing issues
        
        // Build configuration that includes User Secrets
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<DatabaseIntegrationTests>() // Loads from User Secrets
            .AddEnvironmentVariables() // Fallback to environment variables
            .Build();
        
        // Initialize Database with configuration
        Database.Initialize(configuration);
        
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _traceLogger = loggerFactory.CreateLogger<MySqlTraceRepository>();
        _eventLogger = loggerFactory.CreateLogger<MySqlEventRepository>();
        _stateLogger = loggerFactory.CreateLogger<MySqlNetworkStateRepository>();
        _errorLogger = loggerFactory.CreateLogger<MySqlErroredMessageRepository>();
    }

    public void Dispose()
    {
        try
        {
            using var connection = Database.GetConnection();
            
            // SAFE + FAST cleanup: Combine timestamp range (for speed) with unique test ID (for safety)
            // This uses the indexed timestamp column for fast filtering, 
            // then applies the unique ID pattern as a secondary safety check
            var testEndTime = DateTime.UtcNow.AddSeconds(1); // Buffer for timing issues
            
            var deletedTraces = connection.Execute(@"
                DELETE FROM traces 
                WHERE timestamp >= @start 
                  AND timestamp <= @end 
                  AND (json LIKE @testIntPattern OR json LIKE @testSchemaPattern)",
                new { 
                    start = _testStartTime, 
                    end = testEndTime,
                    testIntPattern = $"%TEST-INT-{_testRunId}%",
                    testSchemaPattern = $"%TEST-SCHEMA-{_testRunId}%"
                });
            
            var deletedEvents = connection.Execute(@"
                DELETE FROM events 
                WHERE timestamp >= @start 
                  AND timestamp <= @end 
                  AND (json LIKE @testNodePattern OR json LIKE @testSchemaPattern)",
                new { 
                    start = _testStartTime, 
                    end = testEndTime,
                    testNodePattern = $"%TEST-NODE-{_testRunId}%",
                    testSchemaPattern = $"%TEST-SCHEMA-{_testRunId}%"
                });
            
            var deletedErrors = connection.Execute(@"
                DELETE FROM errored_messages 
                WHERE timestamp >= @start 
                  AND timestamp <= @end 
                  AND (reason LIKE @integrationTestPattern OR reason LIKE @testPattern)",
                new { 
                    start = _testStartTime, 
                    end = testEndTime,
                    integrationTestPattern = $"Integration test %{_testRunId}%",
                    testPattern = $"Test-{_testRunId}%"
                });
            
            if (deletedTraces > 0 || deletedEvents > 0 || deletedErrors > 0)
            {
                Console.WriteLine($"[{_testRunId}] Cleaned up test data: {deletedTraces} traces, {deletedEvents} events, {deletedErrors} errors");
            }
        }
        catch (Exception ex)
        {
            // Log cleanup failures but don't throw - we're already disposing
            Console.WriteLine($"Warning: Failed to clean up test data: {ex.Message}");
        }
        
        GC.SuppressFinalize(this);
    }

    #region Connection Tests

    [Fact]
    public void Database_ConnectionString_Should_Be_Valid()
    {
        // Arrange & Act
        var connectionString = Database.ConnectionStringBuilder.Value;

        // Assert
        Assert.NotNull(connectionString);
        Assert.NotEmpty(connectionString);
        Assert.Contains("server=", connectionString);
        Assert.Contains("database=", connectionString);
        Assert.Contains("Pooling=true", connectionString);
        Assert.Contains("Connection Lifetime=300", connectionString);
        Assert.Contains("Connection Timeout=10", connectionString);
        Assert.Contains("Default Command Timeout=30", connectionString);
    }

    [Fact]
    public void Database_Should_Connect_Successfully()
    {
        // Arrange & Act
        using var connection = Database.GetConnection();

        // Assert
        Assert.NotNull(connection);
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
        Assert.False(string.IsNullOrWhiteSpace(connection.Database));
    }

    [Fact]
    public void Database_Should_Reconnect_After_Close()
    {
        // Arrange
        using var connection = Database.GetConnection();
        var originalDatabase = connection.Database;
        
        // Act
        connection.Close();
        connection.Open();

        // Assert
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
        Assert.Equal(originalDatabase, connection.Database);
    }

    [Fact]
    public async Task Database_Connection_Pool_Should_Handle_Multiple_Connections()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - Open 20 connections concurrently
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                using var conn = Database.GetConnection();
                Assert.Equal(System.Data.ConnectionState.Open, conn.State);
                Thread.Sleep(100);
            }));
        }

        // Assert
        await Task.WhenAll(tasks);
    }

    #endregion

    #region Trace Repository Tests

    [Fact]
    public async Task TraceRepository_Should_Insert_And_Query()
    {
        // Arrange
        var repository = new MySqlTraceRepository(_traceLogger);
        var testCallsign = $"TEST-INT-{_testRunId}";
        var json = JsonSerializer.Serialize(new { type = "L2Trace", reportFrom = testCallsign });
        
        // Act
        await repository.InsertTraceAsync(json, DateTime.UtcNow);
        await Task.Delay(100);
        
        var (traces, _, _) = await repository.GetTracesAsync(
            null, null, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, 
            null, new[] { testCallsign }, 5, null, false, "ASC", default);

        // Assert
        Assert.NotNull(traces);
    }

    [Fact]
    public async Task TraceRepository_Schema_Should_Support_All_Filters()
    {
        // Arrange
        var repository = new MySqlTraceRepository(_traceLogger);
        
        // Act & Assert - Should not throw
        var (traces, _, totalCount) = await repository.GetTracesAsync(
            source: "ANY",
            dest: "ANY",
            from: DateTime.UtcNow.AddDays(-1),
            to: DateTime.UtcNow,
            type: "UI",
            reportFrom: new[] { "ANY" },
            limit: 1,
            cursor: null,
            includeTotalCount: true,
            sortOrder: "ASC",
            ct: default);

        Assert.NotNull(traces);
    }

    #endregion

    #region Event Repository Tests

    [Fact]
    public async Task EventRepository_Should_Insert_And_Query()
    {
        // Arrange
        var repository = new MySqlEventRepository(_eventLogger);
        var testNode = $"TEST-NODE-{_testRunId}";
        var json = JsonSerializer.Serialize(new { type = "LinkUpEvent", node = testNode });
        
        // Act
        await repository.InsertEventAsync(json, DateTime.UtcNow);
        await Task.Delay(100);
        
        var (events, _, _) = await repository.GetEventsAsync(
            testNode, null, null, null, null, null,
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, 5, null, false, "ASC", default);

        // Assert
        Assert.NotNull(events);
    }

    [Fact]
    public async Task EventRepository_Schema_Should_Support_All_Filters()
    {
        // Arrange
        var repository = new MySqlEventRepository(_eventLogger);
        
        // Act & Assert - Should not throw
        var (events, _, _) = await repository.GetEventsAsync(
            node: "ANY",
            type: "LinkUpEvent",
            direction: "incoming",
            remote: "ANY",
            local: "ANY",
            port: "1",
            from: DateTime.UtcNow.AddDays(-1),
            to: DateTime.UtcNow,
            limit: 1,
            cursor: null,
            includeTotalCount: true,
            sortOrder: "ASC",
            ct: default);

        Assert.NotNull(events);
    }

    #endregion

    #region Network State Repository Tests

    [Fact]
    public async Task NetworkStateRepository_Should_Query_Nodes()
    {
        // Arrange
        var repository = new MySqlNetworkStateRepository(_stateLogger);
        
        // Act & Assert - Should not throw
        var nodes = await repository.GetAllNodesAsync();
        Assert.NotNull(nodes);
    }

    [Fact]
    public async Task NetworkStateRepository_Should_Query_Links()
    {
        // Arrange
        var repository = new MySqlNetworkStateRepository(_stateLogger);
        
        // Act & Assert - Should not throw
        var links = await repository.GetAllLinksAsync();
        Assert.NotNull(links);
    }

    [Fact]
    public async Task NetworkStateRepository_Should_Query_Circuits()
    {
        // Arrange
        var repository = new MySqlNetworkStateRepository(_stateLogger);
        
        // Act & Assert - Should not throw
        var circuits = await repository.GetAllCircuitsAsync();
        Assert.NotNull(circuits);
    }

    #endregion

    #region Errored Message Repository Tests

    [Fact]
    public async Task ErroredMessageRepository_Should_Insert_Validation_Error()
    {
        // Arrange
        var repository = new MySqlErroredMessageRepository(_errorLogger);

        // Act & Assert - Should not throw
        await repository.InsertErroredMessageAsync(
            reason: $"Integration test validation error {_testRunId}",
            datagram: "LinkUpEvent",
            type: "LinkUpEvent",
            errors: "Test error message",
            json: null);
    }

    [Fact]
    public async Task ErroredMessageRepository_Should_Insert_Generic_Error()
    {
        // Arrange
        var repository = new MySqlErroredMessageRepository(_errorLogger);

        // Act & Assert - Should not throw
        await repository.InsertErroredMessageAsync(
            reason: $"Integration test generic error {_testRunId}",
            datagram: null,
            type: null,
            errors: null,
            json: "{\"test\": \"data\"}");
    }

    #endregion

    #region Connection Resilience Tests

    [Fact]
    public async Task Database_Should_Handle_Multiple_Sequential_Operations()
    {
        // Arrange
        var traceRepo = new MySqlTraceRepository(_traceLogger);
        var eventRepo = new MySqlEventRepository(_eventLogger);
        var stateRepo = new MySqlNetworkStateRepository(_stateLogger);

        // Act & Assert - Should complete without errors
        await traceRepo.GetTracesAsync(null, null, null, null, null, null, 1, null, false, "ASC", default);
        await eventRepo.GetEventsAsync(null, null, null, null, null, null, null, null, 1, null, false, "ASC", default);
        await stateRepo.GetAllNodesAsync();
    }

    [Fact]
    public async Task Database_Should_Handle_Concurrent_Read_Operations()
    {
        // Arrange
        var traceRepo = new MySqlTraceRepository(_traceLogger);
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(traceRepo.GetTracesAsync(
                null, null, null, null, null, null, 5, null, false, "ASC", default));
        }

        // Assert - Should not throw or deadlock
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task Database_Connection_Should_Support_Long_Running_Query()
    {
        // Arrange
        var repository = new MySqlTraceRepository(_traceLogger);

        // Act
        var (traces, _, _) = await repository.GetTracesAsync(
            source: null,
            dest: null,
            from: DateTime.UtcNow.AddDays(-7),
            to: DateTime.UtcNow,
            type: null,
            reportFrom: null,
            limit: 100,
            cursor: null,
            includeTotalCount: true,
            sortOrder: "ASC",
            ct: default);

        // Assert
        Assert.NotNull(traces);
    }

    #endregion

    #region Schema Compatibility Tests

    [Fact]
    public async Task All_Repositories_Should_Execute_Without_Schema_Errors()
    {
        // Arrange
        var traceRepo = new MySqlTraceRepository(_traceLogger);
        var eventRepo = new MySqlEventRepository(_eventLogger);
        var stateRepo = new MySqlNetworkStateRepository(_stateLogger);
        var errorRepo = new MySqlErroredMessageRepository(_errorLogger);

        // Act & Assert - Each operation should succeed
        await traceRepo.GetTracesAsync(null, null, null, null, null, null, 1, null, true, "ASC", default);
        await eventRepo.GetEventsAsync(null, null, null, null, null, null, null, null, 1, null, true, "ASC", default);
        await stateRepo.GetAllNodesAsync();
        await stateRepo.GetAllLinksAsync();
        await stateRepo.GetAllCircuitsAsync();

        // Use unique test identifiers for inserted data
        var testJson = JsonSerializer.Serialize(new { type = $"TEST-SCHEMA-{_testRunId}" });
        await traceRepo.InsertTraceAsync(testJson);
        await eventRepo.InsertEventAsync(testJson);
        await errorRepo.InsertErroredMessageAsync($"Test-{_testRunId}", json: testJson);
    }

    #endregion
}
