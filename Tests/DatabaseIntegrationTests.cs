using node_api.Services;
using System.Text.Json;
using Xunit;
using Microsoft.Extensions.Configuration;

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
/// </summary>
[Trait("Category", "DatabaseIntegration")]
[Trait("Category", "ManualTest")]
public class DatabaseIntegrationTests : IDisposable
{
    private readonly ILogger<MySqlTraceRepository> _traceLogger;
    private readonly ILogger<MySqlEventRepository> _eventLogger;
    private readonly ILogger<MySqlNetworkStateRepository> _stateLogger;
    private readonly ILogger<MySqlErroredMessageRepository> _errorLogger;
    private readonly QueryFrequencyTracker _tracker;
    
    public DatabaseIntegrationTests()
    {
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
        _tracker = new QueryFrequencyTracker();
    }

    public void Dispose()
    {
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
        var repository = new MySqlTraceRepository(_traceLogger, _tracker);
        var json = JsonSerializer.Serialize(new { type = "L2Trace", reportFrom = "TEST-INT" });
        
        // Act
        await repository.InsertTraceAsync(json, DateTime.UtcNow);
        await Task.Delay(100);
        
        var (traces, _, _) = await repository.GetTracesAsync(
            null, null, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, 
            null, new[] { "TEST-INT" }, 5, null, false, default);

        // Assert
        Assert.NotNull(traces);
    }

    [Fact]
    public async Task TraceRepository_Schema_Should_Support_All_Filters()
    {
        // Arrange
        var repository = new MySqlTraceRepository(_traceLogger, _tracker);
        
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
            ct: default);

        Assert.NotNull(traces);
    }

    #endregion

    #region Event Repository Tests

    [Fact]
    public async Task EventRepository_Should_Insert_And_Query()
    {
        // Arrange
        var repository = new MySqlEventRepository(_eventLogger, _tracker);
        var json = JsonSerializer.Serialize(new { type = "LinkUpEvent", node = "TEST-NODE" });
        
        // Act
        await repository.InsertEventAsync(json, DateTime.UtcNow);
        await Task.Delay(100);
        
        var (events, _, _) = await repository.GetEventsAsync(
            "TEST-NODE", null, null, null, null, null,
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, 5, null, false, default);

        // Assert
        Assert.NotNull(events);
    }

    [Fact]
    public async Task EventRepository_Schema_Should_Support_All_Filters()
    {
        // Arrange
        var repository = new MySqlEventRepository(_eventLogger, _tracker);
        
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
            ct: default);

        Assert.NotNull(events);
    }

    #endregion

    #region Network State Repository Tests

    [Fact]
    public async Task NetworkStateRepository_Should_Query_Nodes()
    {
        // Arrange
        var repository = new MySqlNetworkStateRepository(_stateLogger, _tracker);
        
        // Act & Assert - Should not throw
        var nodes = await repository.GetAllNodesAsync();
        Assert.NotNull(nodes);
    }

    [Fact]
    public async Task NetworkStateRepository_Should_Query_Links()
    {
        // Arrange
        var repository = new MySqlNetworkStateRepository(_stateLogger, _tracker);
        
        // Act & Assert - Should not throw
        var links = await repository.GetAllLinksAsync();
        Assert.NotNull(links);
    }

    [Fact]
    public async Task NetworkStateRepository_Should_Query_Circuits()
    {
        // Arrange
        var repository = new MySqlNetworkStateRepository(_stateLogger, _tracker);
        
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
        var repository = new MySqlErroredMessageRepository(_errorLogger, _tracker);

        // Act & Assert - Should not throw
        await repository.InsertErroredMessageAsync(
            reason: "Integration test validation error",
            datagram: "LinkUpEvent",
            type: "LinkUpEvent",
            errors: "Test error message",
            json: null);
    }

    [Fact]
    public async Task ErroredMessageRepository_Should_Insert_Generic_Error()
    {
        // Arrange
        var repository = new MySqlErroredMessageRepository(_errorLogger, _tracker);

        // Act & Assert - Should not throw
        await repository.InsertErroredMessageAsync(
            reason: "Integration test generic error",
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
        var traceRepo = new MySqlTraceRepository(_traceLogger, _tracker);
        var eventRepo = new MySqlEventRepository(_eventLogger, _tracker);
        var stateRepo = new MySqlNetworkStateRepository(_stateLogger, _tracker);

        // Act & Assert - Should complete without errors
        await traceRepo.GetTracesAsync(null, null, null, null, null, null, 1, null, false, default);
        await eventRepo.GetEventsAsync(null, null, null, null, null, null, null, null, 1, null, false, default);
        await stateRepo.GetAllNodesAsync();
    }

    [Fact]
    public async Task Database_Should_Handle_Concurrent_Read_Operations()
    {
        // Arrange
        var traceRepo = new MySqlTraceRepository(_traceLogger, _tracker);
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(traceRepo.GetTracesAsync(
                null, null, null, null, null, null, 5, null, false, default));
        }

        // Assert - Should not throw or deadlock
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task Database_Connection_Should_Support_Long_Running_Query()
    {
        // Arrange
        var repository = new MySqlTraceRepository(_traceLogger, _tracker);

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
        var traceRepo = new MySqlTraceRepository(_traceLogger, _tracker);
        var eventRepo = new MySqlEventRepository(_eventLogger, _tracker);
        var stateRepo = new MySqlNetworkStateRepository(_stateLogger, _tracker);
        var errorRepo = new MySqlErroredMessageRepository(_errorLogger, _tracker);

        // Act & Assert - Each operation should succeed
        await traceRepo.GetTracesAsync(null, null, null, null, null, null, 1, null, true, default);
        await eventRepo.GetEventsAsync(null, null, null, null, null, null, null, null, 1, null, true, default);
        await stateRepo.GetAllNodesAsync();
        await stateRepo.GetAllLinksAsync();
        await stateRepo.GetAllCircuitsAsync();

        var testJson = JsonSerializer.Serialize(new { type = "Test" });
        await traceRepo.InsertTraceAsync(testJson);
        await eventRepo.InsertEventAsync(testJson);
        await errorRepo.InsertErroredMessageAsync("Test", json: testJson);
    }

    #endregion
}
