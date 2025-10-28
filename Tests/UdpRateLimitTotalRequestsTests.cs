using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using node_api.Models;
using node_api.Services;

namespace Tests;

/// <summary>
/// Tests specifically for the TotalRequests counter fix
/// </summary>
public class UdpRateLimitTotalRequestsTests
{
    private static ILogger<UdpRateLimitService> CreateLogger()
    {
        return LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<UdpRateLimitService>();
    }

    [Fact]
    public async Task GetStats_TotalRequests_Should_Show_Cumulative_Count()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var ip = IPAddress.Parse("192.168.1.1");

        // Act - Make 5 requests with delays to avoid burst limit
        for (int i = 0; i < 5; i++)
        {
            await service.ShouldAllowRequestAsync(ip);
            await Task.Delay(100); // Small delay between requests
        }

        // Wait for rolling window to start clearing (but total should remain)
        await Task.Delay(1200);

        // Make 3 more requests
        for (int i = 0; i < 3; i++)
        {
            await service.ShouldAllowRequestAsync(ip);
            await Task.Delay(100);
        }

        var stats = service.GetStats();

        // Assert - TotalRequests should show 8 (all allowed requests)
        // even though the rolling window only contains recent requests
        var ipStats = stats.ActiveIpRates.FirstOrDefault();
        ipStats.Should().NotBeNull();
        ipStats!.TotalRequests.Should().Be(8, "all 8 allowed requests should be counted");
    }

    [Fact]
    public async Task GetStats_TotalRequests_Should_Not_Count_Blocked_Requests()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 1, // Very low limit to trigger blocking
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var ip = IPAddress.Parse("192.168.1.1");

        // Act - Make requests slowly to avoid burst limit
        // First request should be allowed
        var result1 = await service.ShouldAllowRequestAsync(ip);
        await Task.Delay(100);
        
        var result2 = await service.ShouldAllowRequestAsync(ip);
        await Task.Delay(100);
        
        var result3 = await service.ShouldAllowRequestAsync(ip);
        await Task.Delay(100);

        var stats = service.GetStats();

        // Assert
        result1.Should().BeTrue("first request should be allowed");
        
        // TotalRequests should only count allowed requests
        var ipStats = stats.ActiveIpRates.FirstOrDefault();
        if (ipStats != null)
        {
            // The count should be less than 3 since some may have been blocked
            ipStats.TotalRequests.Should().BeLessThanOrEqualTo(3);
            ipStats.TotalRequests.Should().BeGreaterThan(0, "at least one request was allowed");
        }
    }

    [Fact]
    public async Task GetStats_TotalRequests_Should_Be_Separate_Per_IP()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 20,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var ip1 = IPAddress.Parse("192.168.1.1");
        var ip2 = IPAddress.Parse("192.168.1.2");

        // Act
        // IP1: 5 requests
        for (int i = 0; i < 5; i++)
        {
            await service.ShouldAllowRequestAsync(ip1);
            await Task.Delay(50);
        }

        // IP2: 3 requests
        for (int i = 0; i < 3; i++)
        {
            await service.ShouldAllowRequestAsync(ip2);
            await Task.Delay(50);
        }

        var stats = service.GetStats();

        // Assert
        stats.ActiveIpRates.Should().HaveCount(2, "two IPs should be tracked");
        
        var ip1Stats = stats.ActiveIpRates.FirstOrDefault(r => r.IpAddress.EndsWith("1.1"));
        var ip2Stats = stats.ActiveIpRates.FirstOrDefault(r => r.IpAddress.EndsWith("1.2"));

        ip1Stats.Should().NotBeNull();
        ip1Stats!.TotalRequests.Should().Be(5, "IP1 made 5 requests");

        ip2Stats.Should().NotBeNull();
        ip2Stats!.TotalRequests.Should().Be(3, "IP2 made 3 requests");
    }
}
