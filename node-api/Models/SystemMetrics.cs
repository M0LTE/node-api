using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// System and database metrics for monitoring
/// </summary>
public record SystemMetrics
{
    /// <summary>
    /// Timestamp when metrics were collected
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Hostname of the server
    /// </summary>
    [JsonPropertyName("hostname")]
    public required string Hostname { get; init; }

    /// <summary>
    /// Database metrics
    /// </summary>
    [JsonPropertyName("database")]
    public DatabaseMetrics? Database { get; init; }

    /// <summary>
    /// Disk usage metrics
    /// </summary>
    [JsonPropertyName("disk")]
    public DiskMetrics? Disk { get; init; }

    /// <summary>
    /// System-level metrics (OS)
    /// </summary>
    [JsonPropertyName("system")]
    public SystemLevelMetrics? System { get; init; }

    /// <summary>
    /// Application metrics
    /// </summary>
    [JsonPropertyName("application")]
    public ApplicationMetrics? Application { get; init; }

    /// <summary>
    /// .NET runtime metrics
    /// </summary>
    [JsonPropertyName("dotnet")]
    public DotNetMetrics? DotNet { get; init; }
}

/// <summary>
/// Database performance and usage metrics
/// </summary>
public record DatabaseMetrics
{
    /// <summary>
    /// Number of active connections
    /// </summary>
    [JsonPropertyName("connections")]
    public int? Connections { get; init; }

    /// <summary>
    /// Number of running threads
    /// </summary>
    [JsonPropertyName("threads_running")]
    public int? ThreadsRunning { get; init; }

    /// <summary>
    /// Total queries executed since server start
    /// </summary>
    [JsonPropertyName("queries_total")]
    public long? QueriesTotal { get; init; }

    /// <summary>
    /// Slow queries count
    /// </summary>
    [JsonPropertyName("slow_queries")]
    public long? SlowQueries { get; init; }

    /// <summary>
    /// Database uptime in seconds
    /// </summary>
    [JsonPropertyName("uptime_seconds")]
    public long? UptimeSeconds { get; init; }

    /// <summary>
    /// Questions (queries) per second (approximate)
    /// </summary>
    [JsonPropertyName("queries_per_second")]
    public decimal? QueriesPerSecond { get; init; }

    /// <summary>
    /// Number of temporary tables created
    /// </summary>
    [JsonPropertyName("created_tmp_tables")]
    public long? CreatedTmpTables { get; init; }

    /// <summary>
    /// Number of temporary tables created on disk
    /// </summary>
    [JsonPropertyName("created_tmp_disk_tables")]
    public long? CreatedTmpDiskTables { get; init; }

    /// <summary>
    /// InnoDB buffer pool size in bytes
    /// </summary>
    [JsonPropertyName("innodb_buffer_pool_size")]
    public long? InnoDbBufferPoolSize { get; init; }

    /// <summary>
    /// InnoDB buffer pool pages total
    /// </summary>
    [JsonPropertyName("innodb_buffer_pool_pages_total")]
    public long? InnoDbBufferPoolPagesTotal { get; init; }

    /// <summary>
    /// InnoDB buffer pool pages free
    /// </summary>
    [JsonPropertyName("innodb_buffer_pool_pages_free")]
    public long? InnoDbBufferPoolPagesFree { get; init; }

    /// <summary>
    /// InnoDB buffer pool utilization percentage
    /// </summary>
    [JsonPropertyName("innodb_buffer_pool_utilization_pct")]
    public decimal? InnoDbBufferPoolUtilizationPct { get; init; }

    /// <summary>
    /// Total data size in bytes across all databases
    /// </summary>
    [JsonPropertyName("total_data_size_bytes")]
    public long? TotalDataSizeBytes { get; init; }

    /// <summary>
    /// Total index size in bytes across all databases
    /// </summary>
    [JsonPropertyName("total_index_size_bytes")]
    public long? TotalIndexSizeBytes { get; init; }

    /// <summary>
    /// Database version
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; init; }
}

/// <summary>
/// Disk usage metrics
/// </summary>
public record DiskMetrics
{
    /// <summary>
    /// Total disk space in bytes
    /// </summary>
    [JsonPropertyName("total_bytes")]
    public long? TotalBytes { get; init; }

    /// <summary>
    /// Available disk space in bytes
    /// </summary>
    [JsonPropertyName("available_bytes")]
    public long? AvailableBytes { get; init; }

    /// <summary>
    /// Used disk space in bytes
    /// </summary>
    [JsonPropertyName("used_bytes")]
    public long? UsedBytes { get; init; }

    /// <summary>
    /// Disk usage percentage
    /// </summary>
    [JsonPropertyName("usage_percent")]
    public decimal? UsagePercent { get; init; }

    /// <summary>
    /// Mount point or drive path
    /// </summary>
    [JsonPropertyName("mount_point")]
    public string? MountPoint { get; init; }
}

/// <summary>
/// Operating system level metrics
/// </summary>
public record SystemLevelMetrics
{
    /// <summary>
    /// Total physical memory in bytes
    /// </summary>
    [JsonPropertyName("total_memory_bytes")]
    public long? TotalMemoryBytes { get; init; }

    /// <summary>
    /// Available physical memory in bytes
    /// </summary>
    [JsonPropertyName("available_memory_bytes")]
    public long? AvailableMemoryBytes { get; init; }

    /// <summary>
    /// Used physical memory in bytes
    /// </summary>
    [JsonPropertyName("used_memory_bytes")]
    public long? UsedMemoryBytes { get; init; }

    /// <summary>
    /// Memory usage percentage
    /// </summary>
    [JsonPropertyName("memory_usage_percent")]
    public decimal? MemoryUsagePercent { get; init; }

    /// <summary>
    /// CPU usage percentage (0-100)
    /// </summary>
    [JsonPropertyName("cpu_usage_percent")]
    public decimal? CpuUsagePercent { get; init; }

    /// <summary>
    /// System uptime in seconds
    /// </summary>
    [JsonPropertyName("uptime_seconds")]
    public long? UptimeSeconds { get; init; }

    /// <summary>
    /// Operating system description
    /// </summary>
    [JsonPropertyName("os_description")]
    public string? OsDescription { get; init; }

    /// <summary>
    /// Number of logical processors
    /// </summary>
    [JsonPropertyName("processor_count")]
    public int? ProcessorCount { get; init; }
}

/// <summary>
/// Application-level metrics
/// </summary>
public record ApplicationMetrics
{
    /// <summary>
    /// Application uptime in seconds
    /// </summary>
    [JsonPropertyName("uptime_seconds")]
    public long UptimeSeconds { get; init; }

    /// <summary>
    /// Working set memory in bytes
    /// </summary>
    [JsonPropertyName("memory_working_set_bytes")]
    public long? MemoryWorkingSetBytes { get; init; }

    /// <summary>
    /// Private memory in bytes
    /// </summary>
    [JsonPropertyName("memory_private_bytes")]
    public long? MemoryPrivateBytes { get; init; }

    /// <summary>
    /// Virtual memory in bytes
    /// </summary>
    [JsonPropertyName("memory_virtual_bytes")]
    public long? MemoryVirtualBytes { get; init; }

    /// <summary>
    /// Thread count
    /// </summary>
    [JsonPropertyName("thread_count")]
    public int? ThreadCount { get; init; }

    /// <summary>
    /// Handle count
    /// </summary>
    [JsonPropertyName("handle_count")]
    public int? HandleCount { get; init; }

    /// <summary>
    /// Total processor time in seconds
    /// </summary>
    [JsonPropertyName("total_processor_time_seconds")]
    public double? TotalProcessorTimeSeconds { get; init; }

    /// <summary>
    /// User processor time in seconds
    /// </summary>
    [JsonPropertyName("user_processor_time_seconds")]
    public double? UserProcessorTimeSeconds { get; init; }
}

/// <summary>
/// .NET runtime metrics
/// </summary>
public record DotNetMetrics
{
    /// <summary>
    /// GC heap size in bytes
    /// </summary>
    [JsonPropertyName("gc_heap_size_bytes")]
    public long? GcHeapSizeBytes { get; init; }

    /// <summary>
    /// Total memory allocated in bytes
    /// </summary>
    [JsonPropertyName("gc_total_memory_bytes")]
    public long? GcTotalMemoryBytes { get; init; }

    /// <summary>
    /// Generation 0 collection count
    /// </summary>
    [JsonPropertyName("gc_gen0_collections")]
    public int? GcGen0Collections { get; init; }

    /// <summary>
    /// Generation 1 collection count
    /// </summary>
    [JsonPropertyName("gc_gen1_collections")]
    public int? GcGen1Collections { get; init; }

    /// <summary>
    /// Generation 2 collection count
    /// </summary>
    [JsonPropertyName("gc_gen2_collections")]
    public int? GcGen2Collections { get; init; }

    /// <summary>
    /// Time spent in GC (percentage)
    /// </summary>
    [JsonPropertyName("gc_time_percent")]
    public decimal? GcTimePercent { get; init; }

    /// <summary>
    /// Thread pool thread count
    /// </summary>
    [JsonPropertyName("threadpool_thread_count")]
    public int? ThreadPoolThreadCount { get; init; }

    /// <summary>
    /// Thread pool completed work item count
    /// </summary>
    [JsonPropertyName("threadpool_completed_items")]
    public long? ThreadPoolCompletedItems { get; init; }

    /// <summary>
    /// Thread pool queue length
    /// </summary>
    [JsonPropertyName("threadpool_queue_length")]
    public long? ThreadPoolQueueLength { get; init; }

    /// <summary>
    /// .NET runtime version
    /// </summary>
    [JsonPropertyName("runtime_version")]
    public string? RuntimeVersion { get; init; }

    /// <summary>
    /// Assembly count loaded
    /// </summary>
    [JsonPropertyName("assemblies_loaded")]
    public int? AssembliesLoaded { get; init; }

    /// <summary>
    /// JIT compiled methods count
    /// </summary>
    [JsonPropertyName("jit_compiled_methods")]
    public long? JitCompiledMethods { get; init; }

    /// <summary>
    /// JIT compilation time in milliseconds
    /// </summary>
    [JsonPropertyName("jit_time_ms")]
    public long? JitTimeMs { get; init; }
}
