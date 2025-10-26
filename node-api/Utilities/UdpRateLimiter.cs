using System.Collections.Concurrent;
using System.Net;

namespace node_api.Utilities;

/// <summary>
/// Rate limiter for UDP traffic based on source IP addresses
/// </summary>
public class UdpRateLimiter
{
    private readonly ConcurrentDictionary<string, TokenBucket> _ipBuckets = new();
    private readonly TokenBucket _globalBucket;
    private readonly int _maxPacketsPerSecondPerIp;
    private readonly bool _enabled;

    public UdpRateLimiter(bool enabled, int maxPacketsPerSecondPerIp, int maxTotalPacketsPerSecond)
    {
        _enabled = enabled;
        _maxPacketsPerSecondPerIp = maxPacketsPerSecondPerIp;
        _globalBucket = new TokenBucket(maxTotalPacketsPerSecond);
    }

    /// <summary>
    /// Checks if a packet from the given IP address should be allowed
    /// </summary>
    public bool AllowPacket(IPAddress ipAddress)
    {
        if (!_enabled)
            return true;

        // Check global rate limit first
        if (!_globalBucket.TryConsume())
            return false;

        // Check per-IP rate limit
        var ipKey = ipAddress.ToString();
        var bucket = _ipBuckets.GetOrAdd(ipKey, _ => new TokenBucket(_maxPacketsPerSecondPerIp));
        
        if (!bucket.TryConsume())
        {
            // Refund the global token since we're rejecting
            _globalBucket.Refund();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Cleans up old IP buckets to prevent memory leaks
    /// </summary>
    public void CleanupOldBuckets()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        var keysToRemove = _ipBuckets
            .Where(kvp => kvp.Value.LastAccess < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _ipBuckets.TryRemove(key, out _);
        }
    }

    private class TokenBucket
    {
        private readonly int _capacity;
        private double _tokens;
        private DateTime _lastRefill;
        private readonly object _lock = new();

        public DateTime LastAccess { get; private set; }

        public TokenBucket(int tokensPerSecond)
        {
            _capacity = tokensPerSecond;
            _tokens = tokensPerSecond;
            _lastRefill = DateTime.UtcNow;
            LastAccess = DateTime.UtcNow;
        }

        public bool TryConsume()
        {
            lock (_lock)
            {
                Refill();
                LastAccess = DateTime.UtcNow;

                if (_tokens >= 1)
                {
                    _tokens -= 1;
                    return true;
                }

                return false;
            }
        }

        public void Refund()
        {
            lock (_lock)
            {
                if (_tokens < _capacity)
                    _tokens += 1;
            }
        }

        private void Refill()
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastRefill).TotalSeconds;
            
            if (elapsed > 0)
            {
                _tokens = Math.Min(_capacity, _tokens + elapsed * _capacity);
                _lastRefill = now;
            }
        }
    }
}
