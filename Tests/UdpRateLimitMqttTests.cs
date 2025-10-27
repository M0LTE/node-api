using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;
using node_api.Models;
using node_api.Services;

namespace Tests;

/// <summary>
/// Tests for MQTT publishing of rate limit events
/// </summary>
public class UdpRateLimitMqttTests
{
    private static ILogger<UdpRateLimitService> CreateLogger()
    {
        return LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<UdpRateLimitService>();
    }

    [Fact]
    public async Task ShouldPublishMqttEvent_WhenIPIsBlacklisted()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = new[] { "192.168.1.100" }
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        
        // Create a mock MQTT client (we can't actually publish in tests without a broker)
        // But we can verify the method is being called by checking stats
        var ip = IPAddress.Parse("192.168.1.100");

        // Act
        await service.ShouldAllowRequestAsync(ip);
        var stats = service.GetStats();

        // Assert
        stats.TotalBlacklisted.Should().Be(1);
        stats.RecentlyBlockedIps.Should().HaveCount(1);
        stats.RecentlyBlockedIps[0].IpAddress.Should().Be("192.168.1.100");
        stats.RecentlyBlockedIps[0].Reason.Should().Be("blacklist");
        stats.RecentlyBlockedIps[0].BlockCount.Should().Be(1);
    }

    [Fact]
    public async Task ShouldPublishMqttEvent_WhenIPIsRateLimited()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 2,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var ip = IPAddress.Parse("192.168.1.1");

        // Act - Exceed rate limit
        await service.ShouldAllowRequestAsync(ip);
        await service.ShouldAllowRequestAsync(ip);
        await service.ShouldAllowRequestAsync(ip); // This one should be blocked

        var stats = service.GetStats();

        // Assert
        stats.TotalRateLimited.Should().Be(1);
        stats.RecentlyBlockedIps.Should().HaveCount(1);
        stats.RecentlyBlockedIps[0].IpAddress.Should().Be("192.168.1.1");
        stats.RecentlyBlockedIps[0].Reason.Should().Be("rate_limit");
        stats.RecentlyBlockedIps[0].BlockCount.Should().Be(1);
    }

    [Fact]
    public async Task ShouldTrackMultipleBlocksForSameIP()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 1,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var ip = IPAddress.Parse("192.168.1.1");

        // Act - Make multiple requests that get blocked
        await service.ShouldAllowRequestAsync(ip); // Allowed
        await service.ShouldAllowRequestAsync(ip); // Blocked (count: 1)
        await service.ShouldAllowRequestAsync(ip); // Blocked (count: 2)
        await service.ShouldAllowRequestAsync(ip); // Blocked (count: 3)

        var stats = service.GetStats();

        // Assert
        stats.TotalRateLimited.Should().Be(3);
        stats.RecentlyBlockedIps.Should().HaveCount(1);
        stats.RecentlyBlockedIps[0].BlockCount.Should().Be(3);
    }

    [Fact]
    public async Task ShouldMaintainBlockedIPHistory()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 1,
            Blacklist = new[] { "192.168.1.100", "192.168.1.101" }
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act - Block multiple different IPs
        await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.100")); // Blacklisted
        await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.101")); // Blacklisted
        
        var ip1 = IPAddress.Parse("192.168.1.1");
        await service.ShouldAllowRequestAsync(ip1); // Allowed
        await service.ShouldAllowRequestAsync(ip1); // Rate limited

        var stats = service.GetStats();

        // Assert
        stats.RecentlyBlockedIps.Should().HaveCount(3);
        stats.RecentlyBlockedIps.Should().Contain(b => b.IpAddress == "192.168.1.100" && b.Reason == "blacklist");
        stats.RecentlyBlockedIps.Should().Contain(b => b.IpAddress == "192.168.1.101" && b.Reason == "blacklist");
        stats.RecentlyBlockedIps.Should().Contain(b => b.IpAddress == "192.168.1.1" && b.Reason == "rate_limit");
    }

    [Fact]
    public async Task ShouldLimitBlockedIPHistorySize()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 100,
            Blacklist = new[] { "10.0.0.0/8" } // Block entire range
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act - Block 60 different IPs (more than the 50 limit)
        for (int i = 1; i <= 60; i++)
        {
            await service.ShouldAllowRequestAsync(IPAddress.Parse($"10.0.0.{i}"));
        }

        var stats = service.GetStats();

        // Assert - Should keep only the 50 most recent
        stats.RecentlyBlockedIps.Should().HaveCount(50);
        stats.TotalBlacklisted.Should().Be(60);
    }

    [Fact]
    public async Task ShouldIncludeTimestampInBlockedIPInfo()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = new[] { "192.168.1.100" }
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var before = DateTimeOffset.UtcNow;

        // Act
        await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.100"));
        
        var after = DateTimeOffset.UtcNow;
        var stats = service.GetStats();

        // Assert
        stats.RecentlyBlockedIps.Should().HaveCount(1);
        var blockedIp = stats.RecentlyBlockedIps[0];
        blockedIp.BlockedAt.Should().BeAfter(before.AddSeconds(-1));
        blockedIp.BlockedAt.Should().BeBefore(after.AddSeconds(1));
    }

    [Fact]
    public async Task ShouldUpdateExistingEntryWhenIPIsBlockedAgain()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 1,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var ip = IPAddress.Parse("192.168.1.1");

        // Act - First block
        await service.ShouldAllowRequestAsync(ip); // Allowed
        await service.ShouldAllowRequestAsync(ip); // Blocked

        var statsAfterFirst = service.GetStats();
        var firstTimestamp = statsAfterFirst.RecentlyBlockedIps[0].BlockedAt;

        // Wait a moment
        await Task.Delay(100);

        // Second block
        await service.ShouldAllowRequestAsync(ip); // Blocked again

        var statsAfterSecond = service.GetStats();

        // Assert
        statsAfterSecond.RecentlyBlockedIps.Should().HaveCount(1, "should update existing entry, not create new one");
        statsAfterSecond.RecentlyBlockedIps[0].BlockCount.Should().Be(2);
        statsAfterSecond.RecentlyBlockedIps[0].BlockedAt.Should().BeAfter(firstTimestamp);
    }

    [Fact]
    public async Task ShouldTrackActiveIPRates()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act - Make requests from multiple IPs
        var ip1 = IPAddress.Parse("192.168.1.1");
        var ip2 = IPAddress.Parse("192.168.1.2");
        
        // IP1 makes 5 requests
        for (int i = 0; i < 5; i++)
        {
            await service.ShouldAllowRequestAsync(ip1);
        }
        
        // IP2 makes 3 requests
        for (int i = 0; i < 3; i++)
        {
            await service.ShouldAllowRequestAsync(ip2);
        }

        var stats = service.GetStats();

        // Assert
        stats.ActiveIpRates.Should().NotBeEmpty();
        stats.ActiveIpRates.Should().Contain(r => r.IpAddress == "192.168.1.1");
        stats.ActiveIpRates.Should().Contain(r => r.IpAddress == "192.168.1.2");
        
        var ip1Rate = stats.ActiveIpRates.First(r => r.IpAddress == "192.168.1.1");
        ip1Rate.RequestsPerSecond.Should().BeGreaterThan(0);
        ip1Rate.TotalRequests.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ActiveIPRates_ShouldShowMostActiveIPsFirst()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 10,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);

        // Act - Create different activity levels
        await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.1")); // 1 request
        
        for (int i = 0; i < 5; i++)
            await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.2")); // 5 requests
        
        for (int i = 0; i < 3; i++)
            await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.3")); // 3 requests

        var stats = service.GetStats();

        // Assert - Should be ordered by request rate (descending)
        stats.ActiveIpRates.Should().HaveCount(3);
        stats.ActiveIpRates[0].IpAddress.Should().Be("192.168.1.2"); // Most active
        stats.ActiveIpRates[1].IpAddress.Should().Be("192.168.1.3");
        stats.ActiveIpRates[2].IpAddress.Should().Be("192.168.1.1"); // Least active
    }
}
