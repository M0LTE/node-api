using System.Collections.Concurrent;
using System.Net;

namespace node_api.Middleware;

/// <summary>
/// Middleware for rate limiting API requests based on IP address
/// </summary>
public class ApiRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiRateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, TokenBucket> _ipBuckets = new();
    private readonly bool _enabled;
    private readonly int _permitLimit;
    private readonly int _windowSeconds;

    public ApiRateLimitingMiddleware(
        RequestDelegate next,
        ILogger<ApiRateLimitingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        
        var settings = configuration.GetSection("Security:ApiRateLimiting");
        _enabled = settings.GetValue<bool>("Enabled", true);
        _permitLimit = settings.GetValue<int>("PermitLimit", 100);
        _windowSeconds = settings.GetValue<int>("Window", 60);

        _logger.LogInformation("API Rate limiting: {Status}, Limit: {Limit} requests per {Window}s",
            _enabled ? "Enabled" : "Disabled", _permitLimit, _windowSeconds);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_enabled)
        {
            await _next(context);
            return;
        }

        var ipAddress = GetClientIpAddress(context);
        var bucket = _ipBuckets.GetOrAdd(ipAddress, _ => new TokenBucket(_permitLimit, _windowSeconds));

        if (!bucket.TryConsume())
        {
            // Security: Sanitize IP address before logging to prevent log forging
            // The SanitizeForLogging method removes all control characters including newlines
            // to prevent malicious log injection even though the value originates from HTTP headers
            var sanitizedIp = SanitizeForLogging(ipAddress);
            _logger.LogWarning("Rate limit exceeded for IP: {IpAddress}", sanitizedIp);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = _windowSeconds.ToString();
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                retryAfter = _windowSeconds
            });
            return;
        }

        await _next(context);
    }

    private static string SanitizeForLogging(string input)
    {
        // Remove newlines and control characters to prevent log forging
        if (string.IsNullOrEmpty(input))
            return "unknown";
        
        return new string(input.Where(c => !char.IsControl(c)).ToArray());
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (for proxies/load balancers)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
                return ips[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private class TokenBucket
    {
        private readonly int _capacity;
        private readonly int _windowSeconds;
        private readonly Queue<DateTime> _timestamps = new();
        private readonly object _lock = new();

        public TokenBucket(int capacity, int windowSeconds)
        {
            _capacity = capacity;
            _windowSeconds = windowSeconds;
        }

        public bool TryConsume()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var cutoff = now.AddSeconds(-_windowSeconds);

                // Remove old timestamps outside the window
                while (_timestamps.Count > 0 && _timestamps.Peek() < cutoff)
                {
                    _timestamps.Dequeue();
                }

                if (_timestamps.Count < _capacity)
                {
                    _timestamps.Enqueue(now);
                    return true;
                }

                return false;
            }
        }
    }
}
