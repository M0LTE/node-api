using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using node_api.Models;
using node_api.Services;

namespace Tests;

/// <summary>
/// Integration tests for UDP rate limiting functionality
/// </summary>
public class UdpRateLimitIntegrationTests
{
    private static ILogger<UdpRateLimitService> CreateLogger()
    {
        return LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<UdpRateLimitService>();
    }

    [Fact]
    public async Task RateLimiting_WithRapidRequests_BlocksExcessiveTraffic()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var ip = IPAddress.Parse("192.168.1.100");

        int allowedCount = 0;
        int blockedCount = 0;

        // Act - Simulate burst of 20 requests
        for (int i = 0; i < 20; i++)
        {
            if (await service.ShouldAllowRequestAsync(ip))
                allowedCount++;
            else
                blockedCount++;
        }

        // Assert
        allowedCount.Should().Be(10, "only 10 requests per second should be allowed");
        blockedCount.Should().Be(10, "excess requests should be blocked");
    }

    [Fact]
    public async Task RateLimiting_WithMultipleIPs_TracksIndependently()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 5,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        var ips = new[]
        {
            IPAddress.Parse("192.168.1.1"),
            IPAddress.Parse("192.168.1.2"),
            IPAddress.Parse("192.168.1.3")
        };

        // Act - Each IP makes 7 requests
        var results = new Dictionary<string, (int allowed, int blocked)>();

        foreach (var ip in ips)
        {
            int allowed = 0, blocked = 0;
            for (int i = 0; i < 7; i++)
            {
                if (await service.ShouldAllowRequestAsync(ip))
                    allowed++;
                else
                    blocked++;
            }
            results[ip.ToString()] = (allowed, blocked);
        }

        // Assert - Each IP should have independent rate limits
        foreach (var (ip, (allowed, blocked)) in results)
        {
            allowed.Should().Be(5, $"IP {ip} should have 5 allowed requests");
            blocked.Should().Be(2, $"IP {ip} should have 2 blocked requests");
        }
    }

    [Fact]
    public async Task RateLimiting_AfterTimePasses_AllowsNewRequests()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 3,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var ip = IPAddress.Parse("192.168.1.1");

        // Act - Fill the bucket
        (await service.ShouldAllowRequestAsync(ip)).Should().BeTrue();
        (await service.ShouldAllowRequestAsync(ip)).Should().BeTrue();
        (await service.ShouldAllowRequestAsync(ip)).Should().BeTrue();
        (await service.ShouldAllowRequestAsync(ip)).Should().BeFalse(); // Over limit

        // Wait for rate limit window to reset
        await Task.Delay(1100);

        // Make new requests
        var result1 = await service.ShouldAllowRequestAsync(ip);
        var result2 = await service.ShouldAllowRequestAsync(ip);
        var result3 = await service.ShouldAllowRequestAsync(ip);
        var result4 = await service.ShouldAllowRequestAsync(ip);

        // Assert
        result1.Should().BeTrue("first request after reset should be allowed");
        result2.Should().BeTrue("second request after reset should be allowed");
        result3.Should().BeTrue("third request after reset should be allowed");
        result4.Should().BeFalse("fourth request should be blocked");
    }

    [Fact]
    public async Task Blacklist_WithComplexScenario_BlocksCorrectIPs()
    {
        // Arrange - Simulate realistic blacklist scenario
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 100, // High limit so rate limiting doesn't interfere
            Blacklist = new[]
            {
                "10.0.0.0/8",           // Block entire private class A
                "172.16.0.0/12",        // Block private class B range
                "192.168.1.0/24",       // Block specific subnet
                "203.0.113.50",         // Block single public IP (TEST-NET-3)
                "2001:db8::/32"         // Block IPv6 documentation range
            }
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act & Assert - Test various IPs
        // Blocked IPs
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("10.0.0.1"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("10.255.255.254"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("172.16.0.1"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("172.31.255.254"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.100"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("203.0.113.50"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("2001:db8::1"))).Should().BeFalse();

        // Allowed IPs
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.2.1"))).Should().BeTrue();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("203.0.113.51"))).Should().BeTrue();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("8.8.8.8"))).Should().BeTrue();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("2001:4860:4860::8888"))).Should().BeTrue();
    }

    [Fact]
    public async Task Blacklist_TakesPrecedenceOverRateLimit()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 1000, // Very high rate limit
            Blacklist = new[] { "192.168.1.100" }
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var blacklistedIp = IPAddress.Parse("192.168.1.100");

        // Act - Try multiple times
        var results = new bool[5];
        for (int i = 0; i < 5; i++)
        {
            results[i] = await service.ShouldAllowRequestAsync(blacklistedIp);
        }

        // Assert - All should be blocked regardless of rate limit
        results.Should().AllBeEquivalentTo(false);
    }

    [Fact]
    public async Task Statistics_TrackMultipleBlockingReasons()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 2,
            Blacklist = new[] { "192.168.1.50" }
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act
        // Trigger blacklist blocks
        var blacklistedIp = IPAddress.Parse("192.168.1.50");
        await service.ShouldAllowRequestAsync(blacklistedIp);
        await service.ShouldAllowRequestAsync(blacklistedIp);
        await service.ShouldAllowRequestAsync(blacklistedIp);

        // Trigger rate limit blocks
        var normalIp = IPAddress.Parse("192.168.1.1");
        await service.ShouldAllowRequestAsync(normalIp); // 1
        await service.ShouldAllowRequestAsync(normalIp); // 2
        await service.ShouldAllowRequestAsync(normalIp); // Blocked by rate limit
        await service.ShouldAllowRequestAsync(normalIp); // Blocked by rate limit

        var stats = service.GetStats();

        // Assert
        stats.TotalBlacklisted.Should().Be(3, "three blacklist blocks occurred");
        stats.TotalRateLimited.Should().Be(2, "two rate limit blocks occurred");
        stats.ActiveIpAddresses.Should().Be(1, "only one IP got through to rate limiting");
    }

    [Fact]
    public async Task RateLimiting_WithBurstyTraffic_HandlesCorrectly()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var ip = IPAddress.Parse("192.168.1.1");

        // Act - Burst 1
        int burst1Allowed = 0;
        for (int i = 0; i < 15; i++)
        {
            if (await service.ShouldAllowRequestAsync(ip))
                burst1Allowed++;
        }

        // Assert burst 1
        burst1Allowed.Should().Be(10);

        // Wait for window to reset
        await Task.Delay(1100);

        // Act - Burst 2
        int burst2Allowed = 0;
        for (int i = 0; i < 15; i++)
        {
            if (await service.ShouldAllowRequestAsync(ip))
                burst2Allowed++;
        }

        // Assert burst 2
        burst2Allowed.Should().Be(10, "rate limit should reset after 1 second");
    }

    [Fact]
    public async Task EdgeCase_ZeroPrefixCIDR_BlocksEverything()
    {
        // Arrange - /0 would match everything
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = new[] { "0.0.0.0/0" }
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act
        var result = await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.1"));

        // Assert - 0.0.0.0/0 should block everything
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EdgeCase_LoopbackAddresses_CanBeBlacklisted()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = new[] { "127.0.0.0/8", "::1" }
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act & Assert
        (await service.ShouldAllowRequestAsync(IPAddress.Loopback)).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("127.0.0.1"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("127.100.50.25"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.IPv6Loopback)).Should().BeFalse();
    }

    [Fact]
    public async Task Performance_WithManyIPs_HandlesEfficiently()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act - Simulate 100 different IPs each making 5 requests
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 1; i <= 100; i++)
        {
            var ip = IPAddress.Parse($"192.168.1.{i}");
            for (int j = 0; j < 5; j++)
            {
                await service.ShouldAllowRequestAsync(ip);
            }
        }

        stopwatch.Stop();

        // Assert - Should complete quickly (less than 1 second for 500 operations)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, 
            "rate limiting should be performant");

        var stats = service.GetStats();
        stats.ActiveIpAddresses.Should().Be(100, "should track all 100 IPs");
    }
}
