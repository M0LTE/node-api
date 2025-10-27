using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using node_api.Models;
using node_api.Services;

namespace Tests;

public class UdpRateLimitServiceTests
{
    private static ILogger<UdpRateLimitService> CreateLogger()
    {
        return LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<UdpRateLimitService>();
    }

    [Fact]
    public async Task ShouldAllowRequest_WhenUnderRateLimit_ReturnsTrue()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var ip = IPAddress.Parse("192.168.1.1");

        // Act
        var result = await service.ShouldAllowRequestAsync(ip);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldAllowRequest_WhenExceedingRateLimit_ReturnsFalse()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 5,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var ip = IPAddress.Parse("192.168.1.1");

        // Act - Make 5 allowed requests
        for (int i = 0; i < 5; i++)
        {
            (await service.ShouldAllowRequestAsync(ip)).Should().BeTrue();
        }

        // 6th request should be blocked
        var result = await service.ShouldAllowRequestAsync(ip);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldAllowRequest_AfterOneSecond_AllowsNewRequests()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 2,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var ip = IPAddress.Parse("192.168.1.1");

        // Act - Fill the bucket
        (await service.ShouldAllowRequestAsync(ip)).Should().BeTrue();
        (await service.ShouldAllowRequestAsync(ip)).Should().BeTrue();
        (await service.ShouldAllowRequestAsync(ip)).Should().BeFalse(); // Over limit

        // Wait for rate limit window to pass
        await Task.Delay(1100);

        // Should allow new requests now
        var result = await service.ShouldAllowRequestAsync(ip);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldAllowRequest_WithDifferentIPs_TracksIndependently()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 2,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var ip1 = IPAddress.Parse("192.168.1.1");
        var ip2 = IPAddress.Parse("192.168.1.2");

        // Act - Max out ip1
        (await service.ShouldAllowRequestAsync(ip1)).Should().BeTrue();
        (await service.ShouldAllowRequestAsync(ip1)).Should().BeTrue();
        (await service.ShouldAllowRequestAsync(ip1)).Should().BeFalse();

        // ip2 should still be allowed
        var result = await service.ShouldAllowRequestAsync(ip2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldAllowRequest_WithBlacklistedSingleIP_ReturnsFalse()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = new[] { "192.168.1.100" }
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var blockedIp = IPAddress.Parse("192.168.1.100");
        var allowedIp = IPAddress.Parse("192.168.1.101");

        // Act
        var blockedResult = await service.ShouldAllowRequestAsync(blockedIp);
        var allowedResult = await service.ShouldAllowRequestAsync(allowedIp);

        // Assert
        blockedResult.Should().BeFalse();
        allowedResult.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldAllowRequest_WithBlacklistedCIDR_BlocksEntireRange()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = new[] { "192.168.1.0/24" }
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act & Assert - All IPs in 192.168.1.0/24 should be blocked
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.0"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.1"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.100"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.255"))).Should().BeFalse();

        // IPs outside the range should be allowed
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.2.1"))).Should().BeTrue();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("10.0.0.1"))).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldAllowRequest_WithVariousCIDRRanges_WorksCorrectly()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = new[] 
            { 
                "10.0.0.0/8",      // Entire 10.x.x.x range
                "172.16.0.0/12",   // 172.16.x.x - 172.31.x.x
                "192.168.1.128/25" // 192.168.1.128 - 192.168.1.255
            }
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act & Assert - Test 10.0.0.0/8
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("10.0.0.1"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("10.255.255.254"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("11.0.0.1"))).Should().BeTrue();

        // Test 172.16.0.0/12
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("172.16.0.1"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("172.31.255.254"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("172.15.0.1"))).Should().BeTrue();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("172.32.0.1"))).Should().BeTrue();

        // Test 192.168.1.128/25
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.128"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.200"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.255"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.127"))).Should().BeTrue();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.0"))).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldAllowRequest_WithIPv6_WorksCorrectly()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = new[] { "2001:db8::/32" }
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act & Assert
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("2001:db8::1"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("2001:db8:ffff:ffff:ffff:ffff:ffff:ffff"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("2001:db9::1"))).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldAllowRequest_WithIPv6SingleAddress_WorksCorrectly()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = new[] { "::1" } // IPv6 loopback
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act & Assert
        (await service.ShouldAllowRequestAsync(IPAddress.IPv6Loopback)).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("::2"))).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldAllowRequest_WithMixedBlacklist_HandlesAllFormats()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = new[] 
            { 
                "192.168.1.50",       // Single IPv4
                "10.0.0.0/16",        // IPv4 CIDR
                "::1",                 // Single IPv6
                "2001:db8::/64"       // IPv6 CIDR
            }
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act & Assert
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.50"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("10.0.100.1"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.IPv6Loopback)).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("2001:db8::100"))).Should().BeFalse();
        
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.51"))).Should().BeTrue();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("10.1.0.1"))).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldAllowRequest_WithInvalidBlacklistEntries_IgnoresThem()
    {
        // Arrange - Include some invalid entries
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = new[] 
            { 
                "192.168.1.50",       // Valid
                "invalid-ip",         // Invalid
                "192.168.1.0/999",    // Invalid prefix
                "",                   // Empty
                "  ",                 // Whitespace
                "10.0.0.0/8"          // Valid
            }
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act & Assert - Should only block valid entries
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.50"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("10.5.5.5"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.51"))).Should().BeTrue();
    }

    [Fact]
    public async Task GetStats_ReturnsCorrectStatistics()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 2,
            Blacklist = new[] { "192.168.1.100" }
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act
        await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.100")); // Blacklisted
        await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.100")); // Blacklisted again
        
        var ip1 = IPAddress.Parse("192.168.1.1");
        await service.ShouldAllowRequestAsync(ip1); // Allowed
        await service.ShouldAllowRequestAsync(ip1); // Allowed
        await service.ShouldAllowRequestAsync(ip1); // Rate limited

        var ip2 = IPAddress.Parse("192.168.1.2");
        await service.ShouldAllowRequestAsync(ip2); // Allowed

        var stats = service.GetStats();

        // Assert
        stats.TotalBlacklisted.Should().Be(2);
        stats.TotalRateLimited.Should().Be(1);
        stats.ActiveIpAddresses.Should().Be(2); // ip1 and ip2
    }

    [Fact]
    public async Task ShouldAllowRequest_WithDefaultSettings_Uses10RequestsPerSecond()
    {
        // Arrange
        var settings = new UdpRateLimitSettings(); // Uses default of 10
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var ip = IPAddress.Parse("192.168.1.1");

        // Act - Make 10 requests (should all succeed)
        for (int i = 0; i < 10; i++)
        {
            (await service.ShouldAllowRequestAsync(ip)).Should().BeTrue($"request {i + 1} should be allowed");
        }

        // 11th request should be blocked
        var result = await service.ShouldAllowRequestAsync(ip);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Constructor_WithEmptyBlacklist_InitializesCorrectly()
    {
        // Arrange & Act
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Assert - Should not throw and should allow requests
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.1"))).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldAllowRequest_WithVerySmallCIDR_WorksCorrectly()
    {
        // Arrange - /32 for IPv4 (single IP) and /128 for IPv6 (single IP)
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = new[] 
            { 
                "192.168.1.50/32",
                "2001:db8::1/128"
            }
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act & Assert
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.50"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.51"))).Should().BeTrue();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("2001:db8::1"))).Should().BeFalse();
        (await service.ShouldAllowRequestAsync(IPAddress.Parse("2001:db8::2"))).Should().BeTrue();
    }

    [Fact]
    public void Dispose_CleansUpResources()
    {
        // Arrange
        var settings = new UdpRateLimitSettings();
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act & Assert - Should not throw
        service.Dispose();
    }
}
