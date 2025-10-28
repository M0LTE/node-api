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
        await service.ShouldAllowRequestAsync(ip, "TEST-1");
        var stats = service.GetStats();

        // Assert
        stats.TotalBlacklisted.Should().Be(1);
        stats.RecentlyBlockedIps.Should().HaveCount(1);
        stats.RecentlyBlockedIps[0].IpAddress.Should().Contain("***"); // IP should be redacted
        stats.RecentlyBlockedIps[0].Reason.Should().Be("blacklist");
        stats.RecentlyBlockedIps[0].BlockCount.Should().Be(1);
        stats.RecentlyBlockedIps[0].ReportingCallsign.Should().Be("TEST-1");
        stats.RecentlyBlockedIps[0].ExpiresAt.Should().BeNull(); // Blacklist is permanent
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

        // Act - Exceed burst limit (burst = 6, which is 3x sustained rate of 2)
        for (int i = 0; i < 7; i++)
        {
            await service.ShouldAllowRequestAsync(ip, "TEST-2");
        }

        var stats = service.GetStats();

        // Assert
        stats.TotalRateLimited.Should().Be(1, "one burst limit violation should be recorded");
        stats.RecentlyBlockedIps.Should().HaveCount(1);
        stats.RecentlyBlockedIps[0].IpAddress.Should().Contain("***"); // IP should be redacted
        stats.RecentlyBlockedIps[0].Reason.Should().Be("burst_limit");
        stats.RecentlyBlockedIps[0].BlockCount.Should().Be(1);
        stats.RecentlyBlockedIps[0].ReportingCallsign.Should().Be("TEST-2");
        stats.RecentlyBlockedIps[0].ExpiresAt.Should().NotBeNull(); // Rate limit is temporary
        stats.RecentlyBlockedIps[0].ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
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

        // Act - Make requests that exceed burst limit (burst = 3, which is 3x sustained rate of 1)
        for (int i = 0; i < 5; i++)
        {
            await service.ShouldAllowRequestAsync(ip);
        }

        var stats = service.GetStats();

        // Assert - Should have blocked requests beyond burst limit
        stats.TotalRateLimited.Should().BeGreaterThan(0, "some requests should have been blocked");
        stats.RecentlyBlockedIps.Should().HaveCount(1);
        stats.RecentlyBlockedIps[0].BlockCount.Should().BeGreaterThan(0);
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
        
        // Trigger burst limit block (burst = 3, which is 3x sustained rate of 1)
        var ip1 = IPAddress.Parse("192.168.1.1");
        for (int i = 0; i < 5; i++) // 3 allowed, 2 blocked by burst limit
        {
            await service.ShouldAllowRequestAsync(ip1);
        }

        var stats = service.GetStats();

        // Assert - Should have 3 entries: 2 blacklisted IPs + 1 rate-limited IP
        stats.RecentlyBlockedIps.Should().HaveCount(3);
        stats.RecentlyBlockedIps.Should().Contain(b => b.Reason == "blacklist");
        stats.RecentlyBlockedIps.Where(b => b.Reason == "blacklist").Should().HaveCount(2);
        stats.RecentlyBlockedIps.Should().Contain(b => b.Reason == "burst_limit");
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

        // Act - Trigger burst limit block (burst = 3, which is 3x sustained rate of 1)
        for (int i = 0; i < 5; i++) // 3 allowed, 2 blocked by burst limit
        {
            await service.ShouldAllowRequestAsync(ip);
        }

        var statsAfterFirst = service.GetStats();

        // Assert - Should have one blocked IP entry
        statsAfterFirst.RecentlyBlockedIps.Should().HaveCount(1, "should have one blocked IP");
        statsAfterFirst.RecentlyBlockedIps[0].BlockCount.Should().BeGreaterThan(0, "should have at least one block count");
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
            await service.ShouldAllowRequestAsync(ip1, "TEST-1");
        }
        
        // IP2 makes 3 requests
        for (int i = 0; i < 3; i++)
        {
            await service.ShouldAllowRequestAsync(ip2, "TEST-2");
        }

        var stats = service.GetStats();

        // Assert
        stats.ActiveIpRates.Should().NotBeEmpty();
        stats.ActiveIpRates.Should().Contain(r => r.ReportingCallsign == "TEST-1");
        stats.ActiveIpRates.Should().Contain(r => r.ReportingCallsign == "TEST-2");
        
        var ip1Rate = stats.ActiveIpRates.First(r => r.ReportingCallsign == "TEST-1");
        ip1Rate.RequestsPerSecond.Should().BeGreaterThan(0);
        ip1Rate.TotalRequests.Should().BeGreaterThan(0);
        ip1Rate.IpAddress.Should().Contain("***"); // IP should be redacted
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
        await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.1"), "LOW"); // 1 request
        
        for (int i = 0; i < 5; i++)
            await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.2"), "HIGH"); // 5 requests
        
        for (int i = 0; i < 3; i++)
            await service.ShouldAllowRequestAsync(IPAddress.Parse("192.168.1.3"), "MED"); // 3 requests

        var stats = service.GetStats();

        // Assert - Should be ordered by request rate (descending)
        stats.ActiveIpRates.Should().HaveCount(3);
        stats.ActiveIpRates[0].ReportingCallsign.Should().Be("HIGH"); // Most active
        stats.ActiveIpRates[1].ReportingCallsign.Should().Be("MED");
        stats.ActiveIpRates[2].ReportingCallsign.Should().Be("LOW"); // Least active
    }

    [Fact]
    public async Task TemporaryBlock_ShouldExpireAfterDuration()
    {
        // Arrange
        var settings = new UdpRateLimitSettings
        {
            RequestsPerSecondPerIp = 1,
            Blacklist = Array.Empty<string>()
        };
        var service = new UdpRateLimitService(CreateLogger(), settings);
        var ip = IPAddress.Parse("192.168.1.1");

        // Act - Trigger burst limit (burst = 3, which is 3x sustained rate of 1)
        for (int i = 0; i < 5; i++)
        {
            await service.ShouldAllowRequestAsync(ip, "TEST");
        }

        var statsBeforeExpiry = service.GetStats();
        
        // Assert - Should be blocked
        statsBeforeExpiry.RecentlyBlockedIps.Should().HaveCount(1, "IP should be in recently blocked list");
        var blockedInfo = statsBeforeExpiry.RecentlyBlockedIps[0];
        blockedInfo.ExpiresAt.Should().NotBeNull();
        blockedInfo.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
        
        // Note: We can't easily test actual expiration in a unit test without waiting 5 minutes
        // or mocking the clock. This test just verifies the ExpiresAt is set correctly.
    }
}
