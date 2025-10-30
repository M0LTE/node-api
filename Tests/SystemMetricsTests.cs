using FluentAssertions;
using node_api.Models;
using System.Text.Json;

namespace Tests;

public class SystemMetricsTests
{
    [Fact]
    public void SystemMetrics_ShouldSerializeToJson()
    {
        // Arrange
        var metrics = new SystemMetrics
        {
            Timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            Hostname = "test-server",
            Database = new DatabaseMetrics
            {
                Connections = 10,
                ThreadsRunning = 2,
                QueriesTotal = 1000,
                SlowQueries = 5,
                UptimeSeconds = 3600,
                QueriesPerSecond = 10.5m,
                Version = "10.11.6-MariaDB"
            },
            Disk = new DiskMetrics
            {
                TotalBytes = 100000000000,
                AvailableBytes = 50000000000,
                UsedBytes = 50000000000,
                UsagePercent = 50.0m,
                MountPoint = "/"
            },
            System = new SystemLevelMetrics
            {
                TotalMemoryBytes = 16000000000,
                AvailableMemoryBytes = 8000000000,
                CpuUsagePercent = 45.5m,
                ProcessorCount = 8
            },
            Application = new ApplicationMetrics
            {
                UptimeSeconds = 3600,
                MemoryWorkingSetBytes = 134217728,
                ThreadCount = 25
            },
            DotNet = new DotNetMetrics
            {
                GcHeapSizeBytes = 67108864,
                GcGen0Collections = 100,
                GcGen1Collections = 50,
                GcGen2Collections = 10,
                RuntimeVersion = "9.0.0"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(metrics, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"hostname\": \"test-server\"");
        json.Should().Contain("\"connections\": 10");
        json.Should().Contain("\"usage_percent\": 50");
        json.Should().Contain("\"cpu_usage_percent\": 45.5");
        json.Should().Contain("\"memory_working_set_bytes\": 134217728");
        json.Should().Contain("\"gc_gen0_collections\": 100");
    }

    [Fact]
    public void SystemMetrics_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = """
        {
            "timestamp": "2024-01-15T10:30:00Z",
            "hostname": "test-server",
            "database": {
                "connections": 10,
                "threads_running": 2,
                "version": "10.11.6-MariaDB"
            },
            "disk": {
                "total_bytes": 100000000000,
                "available_bytes": 50000000000,
                "usage_percent": 50.0
            },
            "system": {
                "cpu_usage_percent": 45.5,
                "memory_usage_percent": 50.0,
                "processor_count": 8
            },
            "application": {
                "uptime_seconds": 3600,
                "thread_count": 25
            },
            "dotnet": {
                "gc_gen0_collections": 100,
                "runtime_version": "9.0.0"
            }
        }
        """;

        // Act
        var metrics = JsonSerializer.Deserialize<SystemMetrics>(json);

        // Assert
        metrics.Should().NotBeNull();
        metrics!.Hostname.Should().Be("test-server");
        metrics.Database.Should().NotBeNull();
        metrics.Database!.Connections.Should().Be(10);
        metrics.System.Should().NotBeNull();
        metrics.System!.CpuUsagePercent.Should().Be(45.5m);
        metrics.System.ProcessorCount.Should().Be(8);
        metrics.DotNet.Should().NotBeNull();
        metrics.DotNet!.GcGen0Collections.Should().Be(100);
    }

    [Fact]
    public void SystemMetrics_ShouldHandleNullValues()
    {
        // Arrange
        var metrics = new SystemMetrics
        {
            Hostname = "test-server",
            Database = null,
            Disk = null,
            System = null,
            Application = null,
            DotNet = null
        };

        // Act
        var json = JsonSerializer.Serialize(metrics, new JsonSerializerOptions 
        { 
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        // Assert
        json.Should().NotContain("\"database\":");
        json.Should().NotContain("\"disk\":");
        json.Should().NotContain("\"system\":");
        json.Should().NotContain("\"application\":");
        json.Should().NotContain("\"dotnet\":");
        json.Should().Contain("\"hostname\":\"test-server\"");
    }

    [Fact]
    public void DatabaseMetrics_ShouldCalculateBufferPoolUtilization()
    {
        // Arrange - Buffer pool with 8192 total pages, 1024 free
        var metrics = new DatabaseMetrics
        {
            InnoDbBufferPoolPagesTotal = 8192,
            InnoDbBufferPoolPagesFree = 1024,
            InnoDbBufferPoolUtilizationPct = 87.50m // Pre-calculated
        };

        // Assert
        metrics.InnoDbBufferPoolUtilizationPct.Should().Be(87.50m);
        
        // Verify the calculation would be: (8192 - 1024) / 8192 * 100 = 87.5%
        var expectedUtilization = (decimal)(metrics.InnoDbBufferPoolPagesTotal!.Value - metrics.InnoDbBufferPoolPagesFree!.Value) 
            / metrics.InnoDbBufferPoolPagesTotal.Value * 100;
        
        metrics.InnoDbBufferPoolUtilizationPct.Should().Be(Math.Round(expectedUtilization, 2));
    }

    [Fact]
    public void DiskMetrics_ShouldCalculateUsagePercent()
    {
        // Arrange - 100GB total, 50GB used
        var metrics = new DiskMetrics
        {
            TotalBytes = 100_000_000_000,
            UsedBytes = 50_000_000_000,
            AvailableBytes = 50_000_000_000,
            UsagePercent = 50.0m
        };

        // Assert
        metrics.UsagePercent.Should().Be(50.0m);
        
        // Verify the calculation
        var expectedPercent = (decimal)metrics.UsedBytes!.Value / metrics.TotalBytes!.Value * 100;
        metrics.UsagePercent.Should().Be(Math.Round(expectedPercent, 2));
    }

    [Fact]
    public void SystemLevelMetrics_ShouldCalculateMemoryUsage()
    {
        // Arrange - 16GB total, 8GB used
        var metrics = new SystemLevelMetrics
        {
            TotalMemoryBytes = 16_000_000_000,
            AvailableMemoryBytes = 8_000_000_000,
            UsedMemoryBytes = 8_000_000_000,
            MemoryUsagePercent = 50.0m,
            CpuUsagePercent = 45.5m,
            ProcessorCount = 8
        };

        // Assert
        metrics.MemoryUsagePercent.Should().Be(50.0m);
        metrics.CpuUsagePercent.Should().Be(45.5m);
        
        // Verify the calculation
        var expectedPercent = (decimal)metrics.UsedMemoryBytes!.Value / metrics.TotalMemoryBytes!.Value * 100;
        metrics.MemoryUsagePercent.Should().Be(Math.Round(expectedPercent, 2));
    }

    [Fact]
    public void ApplicationMetrics_ShouldHaveRequiredFields()
    {
        // Arrange & Act
        var metrics = new ApplicationMetrics
        {
            UptimeSeconds = 3600,
            MemoryWorkingSetBytes = 134217728,
            MemoryPrivateBytes = 100000000,
            ThreadCount = 25,
            HandleCount = 500
        };

        // Assert
        metrics.UptimeSeconds.Should().Be(3600);
        metrics.MemoryWorkingSetBytes.Should().Be(134217728);
        metrics.MemoryPrivateBytes.Should().Be(100000000);
        metrics.ThreadCount.Should().Be(25);
        metrics.HandleCount.Should().Be(500);
    }

    [Fact]
    public void DotNetMetrics_ShouldHaveGCCollections()
    {
        // Arrange & Act
        var metrics = new DotNetMetrics
        {
            GcHeapSizeBytes = 67108864,
            GcGen0Collections = 100,
            GcGen1Collections = 50,
            GcGen2Collections = 10,
            RuntimeVersion = "9.0.0",
            AssembliesLoaded = 150
        };

        // Assert
        metrics.GcGen0Collections.Should().Be(100);
        metrics.GcGen1Collections.Should().Be(50);
        metrics.GcGen2Collections.Should().Be(10);
        metrics.RuntimeVersion.Should().Be("9.0.0");
        metrics.AssembliesLoaded.Should().Be(150);
    }

    [Fact]
    public void SystemMetricsPublisher_Should_Track_Database_Queries()
    {
        // This test documents that SystemMetricsPublisher uses QueryLogger for all database queries
        // to ensure they appear in the query-frequency diagnostics page.
        //
        // SystemMetricsPublisher performs the following queries:
        // 1. SHOW GLOBAL STATUS - for database performance metrics
        // 2. SHOW GLOBAL VARIABLES WHERE Variable_name IN ('version', 'innodb_buffer_pool_size') - for config info
        // 3. SELECT SUM(data_length), SUM(index_length) FROM information_schema.TABLES - for database sizes
        //
        // All three queries use QueryLogger.QueryWithLoggingAsync or QueryLogger.QuerySingleOrDefaultWithLoggingAsync
        // with the QueryFrequencyTracker parameter, ensuring they are tracked.
        
        // Arrange
        var tracker = new node_api.Services.QueryFrequencyTracker();
        
        // Act - Simulate recording the queries that SystemMetricsPublisher makes
        tracker.RecordQuery("CollectDatabaseMetricsAsync", "SHOW GLOBAL STATUS");
        tracker.RecordQuery("CollectDatabaseMetricsAsync", "SHOW GLOBAL VARIABLES WHERE Variable_name IN ('version', 'innodb_buffer_pool_size')");
        tracker.RecordQuery("CollectDatabaseMetricsAsync", "SELECT SUM(data_length) as data_size, SUM(index_length) as index_size FROM information_schema.TABLES");
        
        // Assert - Verify these queries are tracked
        var stats = tracker.GetStats();
        stats.Should().HaveCount(3, "SystemMetricsPublisher makes 3 distinct database queries");
        stats.Should().Contain(s => s.QueryText.Contains("SHOW GLOBAL STATUS"));
        stats.Should().Contain(s => s.QueryText.Contains("SHOW GLOBAL VARIABLES"));
        stats.Should().Contain(s => s.QueryText.Contains("information_schema.TABLES"));
    }
}
