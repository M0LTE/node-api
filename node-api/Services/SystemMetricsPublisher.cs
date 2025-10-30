using Dapper;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using node_api.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace node_api.Services;

/// <summary>
/// Background service that collects and publishes system metrics to MQTT.
/// Collects database performance stats, disk usage, system metrics, and application metrics.
/// </summary>
public class SystemMetricsPublisher : BackgroundService
{
    private readonly ILogger<SystemMetricsPublisher> _logger;
    private readonly QueryFrequencyTracker _tracker;
    private const int PublishIntervalSeconds = 10;
    private const int SlowQueryThresholdMs = 1000;
    private IManagedMqttClient? _mqttClient;
    private readonly DateTime _startTime;
    private readonly DateTime _systemStartTime;
    private const string MetricsTopic = "metrics/system";
    
    // Linux CPU tracking
    private long _previousTotalCpuTime;
    private long _previousIdleCpuTime;

    public SystemMetricsPublisher(ILogger<SystemMetricsPublisher> logger, QueryFrequencyTracker tracker)
    {
        _logger = logger;
        _tracker = tracker;
        _startTime = DateTime.UtcNow;
        _systemStartTime = DateTime.UtcNow - TimeSpan.FromMilliseconds(Environment.TickCount64);

        // Initialize Linux CPU tracking
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                ReadLinuxCpuStats(out _previousTotalCpuTime, out _previousIdleCpuTime);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Linux CPU tracking");
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("System metrics publisher starting. Publish interval: {Interval}s", PublishIntervalSeconds);

        // Initialize MQTT client
        try
        {
            await InitializeMqttClientAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MQTT client for metrics publishing. Metrics will not be published.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(PublishIntervalSeconds));
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                await CollectAndPublishMetricsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting or publishing system metrics");
            }
        }

        // Cleanup
        if (_mqttClient != null)
        {
            await _mqttClient.StopAsync();
            _mqttClient.Dispose();
        }
    }

    private async Task InitializeMqttClientAsync(CancellationToken ct)
    {
        var factory = new MQTTnet.MqttFactory();
        _mqttClient = factory.CreateManagedMqttClient();

        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithTcpServer("node-api.packet.oarc.uk", 1883)
                .WithCredentials("writer", Environment.GetEnvironmentVariable("MQTT_WRITER_PASSWORD") 
                    ?? throw new InvalidOperationException("MQTT_WRITER_PASSWORD environment variable is not set"))
                .WithCleanSession(true)
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .Build())
            .Build();

        await _mqttClient.StartAsync(options);
        _logger.LogInformation("MQTT client initialized for metrics publishing");
    }

    private async Task CollectAndPublishMetricsAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var metrics = new SystemMetrics
            {
                Timestamp = DateTime.UtcNow,
                Hostname = Environment.MachineName,
                Database = await CollectDatabaseMetricsAsync(ct),
                Disk = CollectDiskMetrics(),
                System = CollectSystemMetrics(),
                Application = CollectApplicationMetrics(),
                DotNet = CollectDotNetMetrics()
            };

            sw.Stop();
            _logger.LogDebug("Collected system metrics in {ElapsedMs}ms", sw.ElapsedMilliseconds);

            // Publish to MQTT
            await PublishMetricsAsync(metrics, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect system metrics");
        }
    }

    private async Task<DatabaseMetrics?> CollectDatabaseMetricsAsync(CancellationToken ct)
    {
        try
        {
            using var conn = Database.GetConnection(open: false);
            await conn.OpenAsync(ct);

            // Fetch global status variables
            var statusVars = (await QueryLogger.QueryWithLoggingAsync<StatusVariable>(
                conn,
                new CommandDefinition("SHOW GLOBAL STATUS", cancellationToken: ct),
                _logger,
                SlowQueryThresholdMs,
                _tracker)).ToDictionary(x => x.Variable_name, x => x.Value);

            // Fetch global variables for configuration info
            var globalVars = (await QueryLogger.QueryWithLoggingAsync<StatusVariable>(
                conn,
                new CommandDefinition("SHOW GLOBAL VARIABLES WHERE Variable_name IN ('version', 'innodb_buffer_pool_size')", cancellationToken: ct),
                _logger,
                SlowQueryThresholdMs,
                _tracker)).ToDictionary(x => x.Variable_name, x => x.Value);

            // Calculate database sizes
            var sizeQuery = @"
                SELECT 
                    SUM(data_length) as data_size,
                    SUM(index_length) as index_size
                FROM information_schema.TABLES";
            
            var sizes = await QueryLogger.QuerySingleOrDefaultWithLoggingAsync<DatabaseSize>(
                conn,
                new CommandDefinition(sizeQuery, cancellationToken: ct),
                _logger,
                SlowQueryThresholdMs,
                _tracker);

            // Calculate metrics
            long? uptime = statusVars.TryGetValue("Uptime", out var uptimeStr) && long.TryParse(uptimeStr, out var u) ? u : null;
            long? queries = statusVars.TryGetValue("Questions", out var queriesStr) && long.TryParse(queriesStr, out var q) ? q : null;
            
            decimal? qps = null;
            if (uptime.HasValue && queries.HasValue && uptime.Value > 0)
            {
                qps = Math.Round((decimal)queries.Value / uptime.Value, 2);
            }

            // InnoDB buffer pool calculations
            long? bufferPoolPagesTotal = statusVars.TryGetValue("Innodb_buffer_pool_pages_total", out var pagesTotal) && long.TryParse(pagesTotal, out var pt) ? pt : null;
            long? bufferPoolPagesFree = statusVars.TryGetValue("Innodb_buffer_pool_pages_free", out var pagesFree) && long.TryParse(pagesFree, out var pf) ? pf : null;
            
            decimal? bufferPoolUtilization = null;
            if (bufferPoolPagesTotal.HasValue && bufferPoolPagesFree.HasValue && bufferPoolPagesTotal.Value > 0)
            {
                bufferPoolUtilization = Math.Round((decimal)(bufferPoolPagesTotal.Value - bufferPoolPagesFree.Value) / bufferPoolPagesTotal.Value * 100, 2);
            }

            return new DatabaseMetrics
            {
                Connections = statusVars.TryGetValue("Threads_connected", out var tc) && int.TryParse(tc, out var tci) ? tci : null,
                ThreadsRunning = statusVars.TryGetValue("Threads_running", out var tr) && int.TryParse(tr, out var tri) ? tri : null,
                QueriesTotal = queries,
                SlowQueries = statusVars.TryGetValue("Slow_queries", out var sq) && long.TryParse(sq, out var sqi) ? sqi : null,
                UptimeSeconds = uptime,
                QueriesPerSecond = qps,
                CreatedTmpTables = statusVars.TryGetValue("Created_tmp_tables", out var ctt) && long.TryParse(ctt, out var ctti) ? ctti : null,
                CreatedTmpDiskTables = statusVars.TryGetValue("Created_tmp_disk_tables", out var ctdt) && long.TryParse(ctdt, out var ctdti) ? ctdti : null,
                InnoDbBufferPoolSize = globalVars.TryGetValue("innodb_buffer_pool_size", out var bps) && long.TryParse(bps, out var bpsi) ? bpsi : null,
                InnoDbBufferPoolPagesTotal = bufferPoolPagesTotal,
                InnoDbBufferPoolPagesFree = bufferPoolPagesFree,
                InnoDbBufferPoolUtilizationPct = bufferPoolUtilization,
                TotalDataSizeBytes = sizes?.data_size,
                TotalIndexSizeBytes = sizes?.index_size,
                Version = globalVars.TryGetValue("version", out var version) ? version : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect database metrics");
            return null;
        }
    }

    private DiskMetrics? CollectDiskMetrics()
    {
        try
        {
            // Get the drive where the application is running
            var appPath = AppContext.BaseDirectory;
            var driveInfo = new DriveInfo(Path.GetPathRoot(appPath) ?? "/");

            if (!driveInfo.IsReady)
            {
                _logger.LogWarning("Drive not ready: {DriveName}", driveInfo.Name);
                return null;
            }

            var totalBytes = driveInfo.TotalSize;
            var availableBytes = driveInfo.AvailableFreeSpace;
            var usedBytes = totalBytes - availableBytes;
            var usagePercent = totalBytes > 0 ? Math.Round((decimal)usedBytes / totalBytes * 100, 2) : 0;

            return new DiskMetrics
            {
                TotalBytes = totalBytes,
                AvailableBytes = availableBytes,
                UsedBytes = usedBytes,
                UsagePercent = usagePercent,
                MountPoint = driveInfo.Name
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect disk metrics");
            return null;
        }
    }

    private SystemLevelMetrics? CollectSystemMetrics()
    {
        try
        {
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            
            long? totalMemory = gcMemoryInfo.TotalAvailableMemoryBytes > 0 ? gcMemoryInfo.TotalAvailableMemoryBytes : null;
            long? availableMemory = null;
            decimal? cpuUsage = null;

            // Linux metrics (production environment)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    cpuUsage = GetLinuxCpuUsage();
                    var memInfo = GetLinuxMemoryInfo();
                    totalMemory = memInfo.Total;
                    availableMemory = memInfo.Available;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to read Linux /proc stats");
                }
            }

            long? usedMemory = null;
            decimal? memoryUsagePercent = null;

            if (totalMemory.HasValue && availableMemory.HasValue)
            {
                usedMemory = totalMemory.Value - availableMemory.Value;
                memoryUsagePercent = Math.Round((decimal)usedMemory.Value / totalMemory.Value * 100, 2);
            }

            var systemUptime = (long)(DateTime.UtcNow - _systemStartTime).TotalSeconds;

            return new SystemLevelMetrics
            {
                TotalMemoryBytes = totalMemory,
                AvailableMemoryBytes = availableMemory,
                UsedMemoryBytes = usedMemory,
                MemoryUsagePercent = memoryUsagePercent,
                CpuUsagePercent = cpuUsage,
                UptimeSeconds = systemUptime,
                OsDescription = RuntimeInformation.OSDescription,
                ProcessorCount = Environment.ProcessorCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect system metrics");
            return null;
        }
    }

    private decimal? GetLinuxCpuUsage()
    {
        try
        {
            ReadLinuxCpuStats(out var totalCpuTime, out var idleCpuTime);

            var totalDelta = totalCpuTime - _previousTotalCpuTime;
            var idleDelta = idleCpuTime - _previousIdleCpuTime;

            _previousTotalCpuTime = totalCpuTime;
            _previousIdleCpuTime = idleCpuTime;

            if (totalDelta <= 0)
            {
                return null;
            }

            var usage = 100.0m * (1.0m - ((decimal)idleDelta / (decimal)totalDelta));
            return Math.Round(usage, 2);
        }
        catch
        {
            return null;
        }
    }

    private static void ReadLinuxCpuStats(out long totalCpuTime, out long idleCpuTime)
    {
        var lines = File.ReadAllLines("/proc/stat");
        var cpuLine = lines.FirstOrDefault(l => l.StartsWith("cpu "));
        
        if (cpuLine == null)
        {
            throw new InvalidOperationException("Could not find CPU line in /proc/stat");
        }

        var parts = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // CPU line format: cpu user nice system idle iowait irq softirq steal guest guest_nice
        // We need at least: user, nice, system, idle
        if (parts.Length < 5)
        {
            throw new InvalidOperationException("Unexpected /proc/stat format");
        }

        var user = long.Parse(parts[1]);
        var nice = long.Parse(parts[2]);
        var system = long.Parse(parts[3]);
        var idle = long.Parse(parts[4]);
        var iowait = parts.Length > 5 ? long.Parse(parts[5]) : 0;
        var irq = parts.Length > 6 ? long.Parse(parts[6]) : 0;
        var softirq = parts.Length > 7 ? long.Parse(parts[7]) : 0;
        var steal = parts.Length > 8 ? long.Parse(parts[8]) : 0;

        totalCpuTime = user + nice + system + idle + iowait + irq + softirq + steal;
        idleCpuTime = idle + iowait;
    }

    private static (long Total, long Available) GetLinuxMemoryInfo()
    {
        var lines = File.ReadAllLines("/proc/meminfo");
        var memInfo = lines
            .Select(line => line.Split(':', StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(
                parts => parts[0],
                parts => long.Parse(parts[1].Split(' ')[0]) * 1024 // Convert KB to bytes
            );

        var total = memInfo.GetValueOrDefault("MemTotal", 0);
        var available = memInfo.GetValueOrDefault("MemAvailable", 0);

        // Fallback if MemAvailable is not present (older kernels)
        if (available == 0)
        {
            var free = memInfo.GetValueOrDefault("MemFree", 0);
            var buffers = memInfo.GetValueOrDefault("Buffers", 0);
            var cached = memInfo.GetValueOrDefault("Cached", 0);
            available = free + buffers + cached;
        }

        return (total, available);
    }

    private ApplicationMetrics CollectApplicationMetrics()
    {
        var process = Process.GetCurrentProcess();
        var uptime = (long)(DateTime.UtcNow - _startTime).TotalSeconds;

        return new ApplicationMetrics
        {
            UptimeSeconds = uptime,
            MemoryWorkingSetBytes = process.WorkingSet64,
            MemoryPrivateBytes = process.PrivateMemorySize64,
            MemoryVirtualBytes = process.VirtualMemorySize64,
            ThreadCount = process.Threads.Count,
            HandleCount = process.HandleCount,
            TotalProcessorTimeSeconds = process.TotalProcessorTime.TotalSeconds,
            UserProcessorTimeSeconds = process.UserProcessorTime.TotalSeconds
        };
    }

    private DotNetMetrics CollectDotNetMetrics()
    {
        var gcInfo = GC.GetGCMemoryInfo();
        
        ThreadPool.GetAvailableThreads(out _, out _);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out _);
        ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out _);
        var threadPoolThreadCount = maxWorkerThreads - availableWorkerThreads;

        long? pendingWorkItems = null;
        try
        {
            pendingWorkItems = ThreadPool.PendingWorkItemCount;
        }
        catch
        {
            // Not available in all .NET versions
        }

        // Calculate GC time percentage if pause data is available
        decimal? gcTimePercent = null;
        if (gcInfo.PauseDurations.Length > 0)
        {
            var totalPauseMs = 0.0;
            foreach (var pause in gcInfo.PauseDurations)
            {
                totalPauseMs += pause.TotalMilliseconds;
            }
            gcTimePercent = Math.Round((decimal)(totalPauseMs / gcInfo.PauseDurations.Length), 2);
        }

        return new DotNetMetrics
        {
            GcHeapSizeBytes = GC.GetTotalMemory(forceFullCollection: false),
            GcTotalMemoryBytes = gcInfo.TotalCommittedBytes,
            GcGen0Collections = GC.CollectionCount(0),
            GcGen1Collections = GC.CollectionCount(1),
            GcGen2Collections = GC.CollectionCount(2),
            GcTimePercent = gcTimePercent,
            ThreadPoolThreadCount = threadPoolThreadCount,
            ThreadPoolCompletedItems = ThreadPool.CompletedWorkItemCount,
            ThreadPoolQueueLength = pendingWorkItems,
            RuntimeVersion = Environment.Version.ToString(),
            AssembliesLoaded = AppDomain.CurrentDomain.GetAssemblies().Length,
            JitCompiledMethods = null, // Not easily accessible
            JitTimeMs = null // Not easily accessible
        };
    }

    private async Task PublishMetricsAsync(SystemMetrics metrics, CancellationToken ct)
    {
        if (_mqttClient == null || !_mqttClient.IsStarted)
        {
            _logger.LogWarning("MQTT client not available, skipping metrics publish");
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(metrics, new JsonSerializerOptions 
            { 
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(MetricsTopic)
                .WithPayload(json)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                .WithRetainFlag(true) // Retain so late subscribers get the latest metrics
                .Build();

            await _mqttClient.EnqueueAsync(message);

            _logger.LogInformation(
                "Published system metrics: DB Conn={Connections}, DB QPS={QPS}, Disk={DiskPct}%, CPU={CpuPct}%, Mem={MemPct}%, App Memory={MemoryMB}MB",
                metrics.Database?.Connections ?? 0,
                metrics.Database?.QueriesPerSecond ?? 0,
                metrics.Disk?.UsagePercent ?? 0,
                metrics.System?.CpuUsagePercent ?? 0,
                metrics.System?.MemoryUsagePercent ?? 0,
                (metrics.Application?.MemoryWorkingSetBytes ?? 0) / 1024 / 1024);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish metrics to MQTT");
        }
    }

    private class StatusVariable
    {
        public string Variable_name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    private class DatabaseSize
    {
        public long? data_size { get; set; }
        public long? index_size { get; set; }
    }
}
