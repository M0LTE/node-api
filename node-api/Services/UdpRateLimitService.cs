using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;

namespace node_api.Services;

/// <summary>
/// Service for rate limiting UDP datagrams by IP address with CIDR-based blacklist support
/// </summary>
public interface IUdpRateLimitService
{
    /// <summary>
    /// Checks if a request from the given IP should be allowed
    /// </summary>
    /// <param name="ipAddress">The IP address to check</param>
    /// <param name="reportingCallsign">Optional callsign from reportFrom field</param>
    /// <returns>True if the request should be allowed, false if it should be blocked</returns>
    Task<bool> ShouldAllowRequestAsync(IPAddress ipAddress, string? reportingCallsign = null);

    /// <summary>
    /// Gets statistics about rate limiting
    /// </summary>
    RateLimitStats GetStats();
    
    /// <summary>
    /// Sets the MQTT client for publishing rate limit events
    /// </summary>
    void SetMqttClient(IManagedMqttClient mqttClient);
}

/// <summary>
/// Statistics about rate limiting activity
/// </summary>
public class RateLimitStats
{
    public DateTimeOffset ServerTime { get; set; } = DateTimeOffset.UtcNow;
    public int TotalBlacklisted { get; set; }
    public int TotalRateLimited { get; set; }
    public int ActiveIpAddresses { get; set; }
    public List<BlockedIpInfo> RecentlyBlockedIps { get; set; } = new();
    public List<IpRateInfo> ActiveIpRates { get; set; } = new();
}

/// <summary>
/// Information about a blocked IP
/// </summary>
public class BlockedIpInfo
{
    public required string IpAddress { get; set; }
    public required string Reason { get; set; }
    public required DateTimeOffset BlockedAt { get; set; }
    public int BlockCount { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? ReportingCallsign { get; set; }
}

/// <summary>
/// Information about an active IP's request rate
/// </summary>
public class IpRateInfo
{
    public required string IpAddress { get; set; }
    public required double RequestsPerSecond { get; set; }
    public required double AverageRequestsPerSecond { get; set; }
    public required int TotalRequests { get; set; }
    public required DateTimeOffset LastRequest { get; set; }
    public string? ReportingCallsign { get; set; }
}

/// <summary>
/// Implementation of UDP rate limiting service with rolling average and burst support
/// </summary>
public class UdpRateLimitService : IUdpRateLimitService, IDisposable
{
    private readonly ILogger<UdpRateLimitService> _logger;
    private readonly int _requestsPerSecond;
    private readonly int _burstLimit;
    private readonly TimeSpan _rollingWindowDuration;
    private readonly List<(IPAddress Network, int PrefixLength)> _blacklistedNetworks;
    private readonly ConcurrentDictionary<string, RateLimitBucket> _rateLimitBuckets;
    private readonly ConcurrentDictionary<string, BlockedIpInfo> _blockedIpHistory;
    private readonly ConcurrentDictionary<string, string> _ipToCallsignMap;
    private readonly Timer _cleanupTimer;
    private long _totalBlacklisted;
    private long _totalRateLimited;
    private IManagedMqttClient? _mqttClient;
    private const string RateLimitTopic = "metrics/ratelimit";
    private const int MaxHistorySize = 100;
    private static readonly TimeSpan TemporaryBlockDuration = TimeSpan.FromMinutes(1);

    private class RateLimitBucket
    {
        public Queue<DateTimeOffset> RequestTimestamps { get; } = new();
        public DateTimeOffset LastAccess { get; set; }
        public string? ReportingCallsign { get; set; }
        public long TotalRequestCount { get; set; }
    }

    public UdpRateLimitService(
        ILogger<UdpRateLimitService> logger,
        Models.UdpRateLimitSettings settings)
    {
        _logger = logger;
        _requestsPerSecond = settings.RequestsPerSecondPerIp;
        
        // Burst limit is 3x the sustained rate (allows short spikes)
        _burstLimit = _requestsPerSecond * 3;
        
        // Rolling window for average calculation (10 seconds)
        _rollingWindowDuration = TimeSpan.FromSeconds(10);
        
        _blacklistedNetworks = ParseBlacklist(settings.Blacklist);
        _rateLimitBuckets = new ConcurrentDictionary<string, RateLimitBucket>();
        _blockedIpHistory = new ConcurrentDictionary<string, BlockedIpInfo>();
        _ipToCallsignMap = new ConcurrentDictionary<string, string>();

        // Clean up stale buckets every minute
        _cleanupTimer = new Timer(CleanupStaleBuckets, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        _logger.LogInformation(
            "UDP rate limiting initialized: {SustainedRate} req/s sustained (avg over {WindowSeconds}s), {BurstLimit} req/s burst, {BlacklistCount} blacklisted networks",
            _requestsPerSecond,
            _rollingWindowDuration.TotalSeconds,
            _burstLimit,
            _blacklistedNetworks.Count);

        if (_blacklistedNetworks.Count > 0)
        {
            _logger.LogInformation("Blacklisted networks: {Networks}",
                string.Join(", ", _blacklistedNetworks.Select(n => $"{n.Network}/{n.PrefixLength}")));
        }
    }

    public void SetMqttClient(IManagedMqttClient mqttClient)
    {
        _mqttClient = mqttClient;
    }

    public async Task<bool> ShouldAllowRequestAsync(IPAddress ipAddress, string? reportingCallsign = null)
    {
        var ipKey = ipAddress.ToString();

        // Store callsign mapping if provided
        if (!string.IsNullOrWhiteSpace(reportingCallsign))
        {
            _ipToCallsignMap[ipKey] = reportingCallsign;
        }

        // Check blacklist first (permanent)
        if (IsBlacklisted(ipAddress))
        {
            Interlocked.Increment(ref _totalBlacklisted);
            _logger.LogWarning("Blocked blacklisted IP: {IpAddress} (Callsign: {Callsign})", 
                ipAddress, reportingCallsign ?? "Unknown");
            await RecordBlockedIpAsync(ipAddress, "blacklist", reportingCallsign);
            return false;
        }

        // Check rate limit with rolling average and burst support
        var bucket = _rateLimitBuckets.GetOrAdd(ipKey, _ => new RateLimitBucket());
        bucket.LastAccess = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(reportingCallsign))
        {
            bucket.ReportingCallsign = reportingCallsign;
        }

        bool shouldBlock = false;
        string? blockReason = null;
        double averageRate = 0;
        int burstRate = 0;

        lock (bucket.RequestTimestamps)
        {
            var now = DateTimeOffset.UtcNow;
            var rollingWindowStart = now.Subtract(_rollingWindowDuration);
            var burstWindowStart = now.AddSeconds(-1); // 1-second window for burst check

            // Remove timestamps older than the rolling window
            while (bucket.RequestTimestamps.Count > 0 && bucket.RequestTimestamps.Peek() < rollingWindowStart)
            {
                bucket.RequestTimestamps.Dequeue();
            }

            // Calculate current rates BEFORE adding this request
            var requestsInRollingWindow = bucket.RequestTimestamps.Count;
            var requestsInLastSecond = bucket.RequestTimestamps.Count(t => t >= burstWindowStart);
            var averageRequestsPerSecond = requestsInRollingWindow / _rollingWindowDuration.TotalSeconds;

            averageRate = averageRequestsPerSecond;
            burstRate = requestsInLastSecond;

            // Check burst limit first (immediate spike protection)
            if (requestsInLastSecond >= _burstLimit)
            {
                shouldBlock = true;
                blockReason = "burst_limit";
            }
            // Check sustained average rate limit
            else if (averageRequestsPerSecond >= _requestsPerSecond)
            {
                shouldBlock = true;
                blockReason = "sustained_rate_limit";
            }
            else
            {
                // Add this request
                bucket.RequestTimestamps.Enqueue(now);
                bucket.TotalRequestCount++;
            }
        }

        // Handle blocking outside the lock
        if (shouldBlock)
        {
            Interlocked.Increment(ref _totalRateLimited);
            
            if (blockReason == "burst_limit")
            {
                _logger.LogWarning(
                    "Burst limit exceeded for IP: {IpAddress} (Callsign: {Callsign}) - {BurstRate} req/s (limit: {BurstLimit})", 
                    ipAddress, 
                    reportingCallsign ?? "Unknown", 
                    burstRate,
                    _burstLimit);
            }
            else
            {
                _logger.LogWarning(
                    "Sustained rate limit exceeded for IP: {IpAddress} (Callsign: {Callsign}) - {AvgRate:F2} avg req/s (limit: {SustainedLimit})", 
                    ipAddress, 
                    reportingCallsign ?? "Unknown", 
                    averageRate,
                    _requestsPerSecond);
            }
            
            await RecordBlockedIpAsync(ipAddress, blockReason!, reportingCallsign, averageRate, burstRate);
            return false;
        }

        return true;
    }

    private async Task RecordBlockedIpAsync(
        IPAddress ipAddress, 
        string reason, 
        string? reportingCallsign = null,
        double? averageRate = null,
        int? burstRate = null)
    {
        var ipKey = ipAddress.ToString();
        var now = DateTimeOffset.UtcNow;
        // Set expiry for display purposes - rate limits reset as requests age out
        // Blacklist blocks are permanent (null expiry)
        var expiresAt = reason != "blacklist" ? now.Add(TemporaryBlockDuration) : (DateTimeOffset?)null;

        // Try to get callsign from map if not provided
        if (string.IsNullOrWhiteSpace(reportingCallsign))
        {
            _ipToCallsignMap.TryGetValue(ipKey, out reportingCallsign);
        }

        var blockedInfo = _blockedIpHistory.AddOrUpdate(
            ipKey,
            _ => new BlockedIpInfo
            {
                IpAddress = RedactIpAddress(ipKey),
                Reason = reason,
                BlockedAt = now,
                BlockCount = 1,
                ExpiresAt = expiresAt,
                ReportingCallsign = reportingCallsign
            },
            (_, existing) =>
            {
                existing.BlockedAt = now;
                existing.BlockCount++;
                existing.Reason = reason;
                existing.ExpiresAt = expiresAt;
                if (!string.IsNullOrWhiteSpace(reportingCallsign))
                {
                    existing.ReportingCallsign = reportingCallsign;
                }
                return existing;
            });

        // Trim history if too large
        if (_blockedIpHistory.Count > MaxHistorySize)
        {
            var oldest = _blockedIpHistory
                .OrderBy(kvp => kvp.Value.BlockedAt)
                .Take(_blockedIpHistory.Count - MaxHistorySize)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldest)
            {
                _blockedIpHistory.TryRemove(key, out _);
            }
        }

        // Publish to MQTT if available
        if (_mqttClient != null)
        {
            try
            {
                var payload = JsonSerializer.SerializeToUtf8Bytes(new
                {
                    ip = RedactIpAddress(ipKey),
                    callsign = reportingCallsign ?? "Unknown",
                    reason,
                    timestamp = now,
                    block_count = blockedInfo.BlockCount,
                    expires_at = expiresAt,
                    average_rate = averageRate,
                    burst_rate = burstRate,
                    sustained_limit = _requestsPerSecond,
                    burst_limit = _burstLimit
                });

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(RateLimitTopic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                    .Build();

                await _mqttClient.EnqueueAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing rate limit event to MQTT");
            }
        }
    }

    private static string RedactIpAddress(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return ipAddress;

        // IPv4: Redact first two octets (e.g., 192.168.1.100 -> ***.***. 1.100)
        if (ipAddress.Contains('.') && !ipAddress.Contains(':'))
        {
            var parts = ipAddress.Split('.');
            if (parts.Length == 4)
            {
                return $"***.***.{parts[2]}.{parts[3]}";
            }
        }
        // IPv6: Redact first half (e.g., 2001:db8::1 -> ****:****::1)
        else if (ipAddress.Contains(':'))
        {
            var parts = ipAddress.Split(':');
            if (parts.Length >= 2)
            {
                var redacted = new string('*', parts[0].Length) + ":" + new string('*', parts[1].Length);
                return redacted + ":" + string.Join(":", parts.Skip(2));
            }
        }

        return ipAddress; // Fallback, return as-is
    }

    public RateLimitStats GetStats()
    {
        var now = DateTimeOffset.UtcNow;
        
        // Filter out expired temporary blocks
        var activeBlocks = _blockedIpHistory.Values
            .Where(b => b.Reason == "blacklist" || // Permanent
                       !b.ExpiresAt.HasValue ||      // No expiry
                       b.ExpiresAt.Value > now)      // Not expired
            .OrderByDescending(b => b.BlockedAt)
            .Take(50)
            .ToList();

        // Get active IP rates with callsigns
        var activeIpRates = _rateLimitBuckets
            .Where(kvp => (now - kvp.Value.LastAccess).TotalSeconds < 60) // Active in last minute
            .Select(kvp =>
            {
                var bucket = kvp.Value;
                double currentRps;
                double avgRps;
                int total;
                
                lock (bucket.RequestTimestamps)
                {
                    var rollingWindowStart = now.Subtract(_rollingWindowDuration);
                    var oneSecondAgo = now.AddSeconds(-1);
                    
                    // Current rate (last second)
                    var recentCount = bucket.RequestTimestamps.Count(t => t >= oneSecondAgo);
                    currentRps = recentCount;
                    
                    // Average rate over rolling window
                    var requestsInWindow = bucket.RequestTimestamps.Count(t => t >= rollingWindowStart);
                    avgRps = requestsInWindow / _rollingWindowDuration.TotalSeconds;
                    
                    total = (int)bucket.TotalRequestCount;
                }

                // Get callsign from bucket or map
                var callsign = bucket.ReportingCallsign;
                if (string.IsNullOrWhiteSpace(callsign))
                {
                    _ipToCallsignMap.TryGetValue(kvp.Key, out callsign);
                }
                
                return new IpRateInfo
                {
                    IpAddress = RedactIpAddress(kvp.Key),
                    RequestsPerSecond = currentRps,
                    AverageRequestsPerSecond = avgRps,
                    TotalRequests = total,
                    LastRequest = bucket.LastAccess,
                    ReportingCallsign = callsign ?? "Unknown"
                };
            })
            .OrderByDescending(r => r.AverageRequestsPerSecond)
            .Take(20) // Top 20 most active IPs
            .ToList();

        return new RateLimitStats
        {
            ServerTime = now,
            TotalBlacklisted = (int)Interlocked.Read(ref _totalBlacklisted),
            TotalRateLimited = (int)Interlocked.Read(ref _totalRateLimited),
            ActiveIpAddresses = _rateLimitBuckets.Count,
            RecentlyBlockedIps = activeBlocks,
            ActiveIpRates = activeIpRates
        };
    }

    private bool IsBlacklisted(IPAddress ipAddress)
    {
        foreach (var (network, prefixLength) in _blacklistedNetworks)
        {
            if (IsInNetwork(ipAddress, network, prefixLength))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsInNetwork(IPAddress address, IPAddress network, int prefixLength)
    {
        var addressBytes = address.GetAddressBytes();
        var networkBytes = network.GetAddressBytes();

        // Different address families can't match
        if (addressBytes.Length != networkBytes.Length)
            return false;

        var bytesToCheck = prefixLength / 8;
        var bitsToCheck = prefixLength % 8;

        // Check whole bytes
        for (int i = 0; i < bytesToCheck; i++)
        {
            if (addressBytes[i] != networkBytes[i])
                return false;
        }

        // Check remaining bits if any
        if (bitsToCheck > 0)
        {
            var mask = (byte)(0xFF << (8 - bitsToCheck));
            if ((addressBytes[bytesToCheck] & mask) != (networkBytes[bytesToCheck] & mask))
                return false;
        }

        return true;
    }

    private static List<(IPAddress Network, int PrefixLength)> ParseBlacklist(string[] blacklist)
    {
        var result = new List<(IPAddress, int)>();

        foreach (var entry in blacklist)
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            try
            {
                // Check if CIDR notation (contains /)
                if (entry.Contains('/'))
                {
                    var parts = entry.Split('/');
                    if (parts.Length != 2)
                        continue;

                    if (!IPAddress.TryParse(parts[0].Trim(), out var network))
                        continue;

                    if (!int.TryParse(parts[1].Trim(), out var prefixLength))
                        continue;

                    // Validate prefix length
                    var maxPrefixLength = network.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
                    if (prefixLength < 0 || prefixLength > maxPrefixLength)
                        continue;

                    result.Add((network, prefixLength));
                }
                else
                {
                    // Single IP address
                    if (IPAddress.TryParse(entry.Trim(), out var ip))
                    {
                        var prefixLength = ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
                        result.Add((ip, prefixLength));
                    }
                }
            }
            catch (Exception)
            {
                // Skip invalid entries
                continue;
            }
        }

        return result;
    }

    private void CleanupStaleBuckets(object? state)
    {
        try
        {
            var cutoff = DateTimeOffset.UtcNow.AddMinutes(-5);
            var keysToRemove = _rateLimitBuckets
                .Where(kvp => kvp.Value.LastAccess < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _rateLimitBuckets.TryRemove(key, out _);
            }

            if (keysToRemove.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} stale rate limit buckets", keysToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up rate limit buckets");
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}
