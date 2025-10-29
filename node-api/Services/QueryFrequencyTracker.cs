using System.Collections.Concurrent;
using System.Diagnostics;

namespace node_api.Services;

/// <summary>
/// Tracks database query frequency to help diagnose increasing database traffic.
/// Records which repository methods and SQL statements are being called and how often per hour.
/// </summary>
public class QueryFrequencyTracker
{
    private readonly ConcurrentDictionary<string, QueryStats> _queryStats = new();
    private readonly object _cleanupLock = new();
    private DateTime _lastCleanup = DateTime.UtcNow;
    
    /// <summary>
    /// Records a database query execution with its source information
    /// </summary>
    /// <param name="methodName">The repository method name (e.g., "GetTracesAsync")</param>
    /// <param name="queryText">The SQL query text (sanitized)</param>
    public void RecordQuery(string methodName, string queryText)
    {
        var now = DateTime.UtcNow;
        var key = GetKey(methodName, queryText);
        
        _queryStats.AddOrUpdate(
            key,
            _ => new QueryStats 
            { 
                MethodName = methodName,
                QueryText = queryText,
                TotalCount = 1,
                LastSeen = now,
                HourlyBuckets = new ConcurrentDictionary<DateTime, int>
                {
                    [GetHourBucket(now)] = 1
                }
            },
            (_, stats) =>
            {
                var bucket = GetHourBucket(now);
                stats.HourlyBuckets.AddOrUpdate(bucket, 1, (_, count) => count + 1);
                stats.LastSeen = now;
                stats.TotalCount++;
                return stats;
            });
        
        // Periodically cleanup old data (older than 24 hours)
        if ((now - _lastCleanup).TotalMinutes > 60)
        {
            CleanupOldData(now);
        }
    }
    
    /// <summary>
    /// Gets all query statistics
    /// </summary>
    public IReadOnlyList<QueryStatsDto> GetStats()
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddHours(-24);
        
        return _queryStats.Values
            .Select(stats => new QueryStatsDto
            {
                MethodName = stats.MethodName,
                QueryText = stats.QueryText,
                TotalCount = stats.TotalCount,
                LastSeen = stats.LastSeen,
                HourlyData = stats.HourlyBuckets
                    .Where(kvp => kvp.Key >= cutoff)
                    .OrderBy(kvp => kvp.Key)
                    .Select(kvp => new HourlyDataPoint
                    {
                        Hour = kvp.Key,
                        Count = kvp.Value
                    })
                    .ToList()
            })
            .OrderByDescending(s => s.TotalCount)
            .ToList();
    }
    
    /// <summary>
    /// Gets all query statistics with server time for clock synchronization
    /// </summary>
    public QueryFrequencyResponse GetStatsWithServerTime()
    {
        return new QueryFrequencyResponse
        {
            ServerTime = DateTime.UtcNow,
            Queries = GetStats()
        };
    }
    
    private static string GetKey(string methodName, string queryText)
    {
        // Use first 100 chars of query to differentiate similar queries while keeping key manageable
        var truncatedQuery = queryText.Length > 100 
            ? queryText.Substring(0, 100) 
            : queryText;
        return $"{methodName}::{truncatedQuery}";
    }
    
    private static DateTime GetHourBucket(DateTime timestamp)
    {
        return new DateTime(
            timestamp.Year,
            timestamp.Month,
            timestamp.Day,
            timestamp.Hour,
            0,
            0,
            DateTimeKind.Utc);
    }
    
    private void CleanupOldData(DateTime now)
    {
        lock (_cleanupLock)
        {
            if ((now - _lastCleanup).TotalMinutes <= 60)
                return;
            
            var cutoff = now.AddHours(-24);
            
            foreach (var stats in _queryStats.Values)
            {
                var oldBuckets = stats.HourlyBuckets.Keys
                    .Where(k => k < cutoff)
                    .ToList();
                
                foreach (var bucket in oldBuckets)
                {
                    stats.HourlyBuckets.TryRemove(bucket, out _);
                }
            }
            
            _lastCleanup = now;
        }
    }
    
    private class QueryStats
    {
        public string MethodName { get; set; } = string.Empty;
        public string QueryText { get; set; } = string.Empty;
        public long TotalCount { get; set; }
        public DateTime LastSeen { get; set; }
        public ConcurrentDictionary<DateTime, int> HourlyBuckets { get; set; } = new();
    }
    
    public class QueryFrequencyResponse
    {
        public DateTime ServerTime { get; set; }
        public required IReadOnlyList<QueryStatsDto> Queries { get; set; }
    }
    
    public class QueryStatsDto
    {
        public required string MethodName { get; set; }
        public required string QueryText { get; set; }
        public long TotalCount { get; set; }
        public DateTime LastSeen { get; set; }
        public required List<HourlyDataPoint> HourlyData { get; set; }
    }
    
    public class HourlyDataPoint
    {
        public DateTime Hour { get; set; }
        public int Count { get; set; }
    }
}
