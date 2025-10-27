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
    /// <returns>True if the request should be allowed, false if it should be blocked</returns>
    Task<bool> ShouldAllowRequestAsync(IPAddress ipAddress);

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
    public int TotalBlacklisted { get; set; }
    public int TotalRateLimited { get; set; }
    public int ActiveIpAddresses { get; set; }
    public List<BlockedIpInfo> RecentlyBlockedIps { get; set; } = new();
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
}

/// <summary>
/// Implementation of UDP rate limiting service
/// </summary>
public class UdpRateLimitService : IUdpRateLimitService, IDisposable
{
    private readonly ILogger<UdpRateLimitService> _logger;
    private readonly int _requestsPerSecond;
    private readonly List<(IPAddress Network, int PrefixLength)> _blacklistedNetworks;
    private readonly ConcurrentDictionary<string, RateLimitBucket> _rateLimitBuckets;
    private readonly ConcurrentDictionary<string, BlockedIpInfo> _blockedIpHistory;
    private readonly Timer _cleanupTimer;
    private long _totalBlacklisted;
    private long _totalRateLimited;
    private IManagedMqttClient? _mqttClient;
    private const string RateLimitTopic = "metrics/ratelimit";
    private const int MaxHistorySize = 100;

    private class RateLimitBucket
    {
        public Queue<DateTimeOffset> RequestTimestamps { get; } = new();
        public DateTimeOffset LastAccess { get; set; }
    }

    public UdpRateLimitService(
        ILogger<UdpRateLimitService> logger,
        Models.UdpRateLimitSettings settings)
    {
        _logger = logger;
        _requestsPerSecond = settings.RequestsPerSecondPerIp;
        _blacklistedNetworks = ParseBlacklist(settings.Blacklist);
        _rateLimitBuckets = new ConcurrentDictionary<string, RateLimitBucket>();
        _blockedIpHistory = new ConcurrentDictionary<string, BlockedIpInfo>();

        // Clean up stale buckets every minute
        _cleanupTimer = new Timer(CleanupStaleBuckets, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        _logger.LogInformation(
            "UDP rate limiting initialized: {RequestsPerSecond} req/s per IP, {BlacklistCount} blacklisted networks",
            _requestsPerSecond,
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

    public async Task<bool> ShouldAllowRequestAsync(IPAddress ipAddress)
    {
        // Check blacklist first
        if (IsBlacklisted(ipAddress))
        {
            Interlocked.Increment(ref _totalBlacklisted);
            _logger.LogWarning("Blocked blacklisted IP: {IpAddress}", ipAddress);
            await RecordBlockedIpAsync(ipAddress, "blacklist");
            return false;
        }

        // Check rate limit
        var ipKey = ipAddress.ToString();
        var bucket = _rateLimitBuckets.GetOrAdd(ipKey, _ => new RateLimitBucket());
        bucket.LastAccess = DateTimeOffset.UtcNow;

        lock (bucket.RequestTimestamps)
        {
            var now = DateTimeOffset.UtcNow;
            var oneSecondAgo = now.AddSeconds(-1);

            // Remove timestamps older than 1 second
            while (bucket.RequestTimestamps.Count > 0 && bucket.RequestTimestamps.Peek() < oneSecondAgo)
            {
                bucket.RequestTimestamps.Dequeue();
            }

            // Check if we're over the limit
            if (bucket.RequestTimestamps.Count >= _requestsPerSecond)
            {
                Interlocked.Increment(ref _totalRateLimited);
                _logger.LogWarning("Rate limit exceeded for IP: {IpAddress} ({Count} req/s)", ipAddress, bucket.RequestTimestamps.Count);
                _ = RecordBlockedIpAsync(ipAddress, "rate_limit");
                return false;
            }

            // Add this request
            bucket.RequestTimestamps.Enqueue(now);
            return true;
        }
    }

    private async Task RecordBlockedIpAsync(IPAddress ipAddress, string reason)
    {
        var ipKey = ipAddress.ToString();
        var now = DateTimeOffset.UtcNow;

        var blockedInfo = _blockedIpHistory.AddOrUpdate(
            ipKey,
            _ => new BlockedIpInfo
            {
                IpAddress = ipKey,
                Reason = reason,
                BlockedAt = now,
                BlockCount = 1
            },
            (_, existing) =>
            {
                existing.BlockedAt = now;
                existing.BlockCount++;
                existing.Reason = reason;
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
                    ip = ipKey,
                    reason,
                    timestamp = now,
                    block_count = blockedInfo.BlockCount
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

    public RateLimitStats GetStats()
    {
        var recentBlocked = _blockedIpHistory.Values
            .OrderByDescending(b => b.BlockedAt)
            .Take(50)
            .ToList();

        return new RateLimitStats
        {
            TotalBlacklisted = (int)Interlocked.Read(ref _totalBlacklisted),
            TotalRateLimited = (int)Interlocked.Read(ref _totalRateLimited),
            ActiveIpAddresses = _rateLimitBuckets.Count,
            RecentlyBlockedIps = recentBlocked
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
