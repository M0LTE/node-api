using node_api.Utilities;
using System.Net;
using Xunit;

namespace Tests;

public class UdpRateLimiterTests
{
    [Fact]
    public void AllowPacket_WhenDisabled_AlwaysReturnsTrue()
    {
        // Arrange
        var rateLimiter = new UdpRateLimiter(enabled: false, maxPacketsPerSecondPerIp: 1, maxTotalPacketsPerSecond: 10);
        var ip = IPAddress.Parse("192.168.1.1");

        // Act & Assert - should allow unlimited packets
        for (int i = 0; i < 100; i++)
        {
            Assert.True(rateLimiter.AllowPacket(ip));
        }
    }

    [Fact]
    public void AllowPacket_WithinPerIpLimit_ReturnsTrue()
    {
        // Arrange
        var rateLimiter = new UdpRateLimiter(enabled: true, maxPacketsPerSecondPerIp: 10, maxTotalPacketsPerSecond: 100);
        var ip = IPAddress.Parse("192.168.1.1");

        // Act - request within limit
        var results = new List<bool>();
        for (int i = 0; i < 5; i++)
        {
            results.Add(rateLimiter.AllowPacket(ip));
        }

        // Assert
        Assert.All(results, result => Assert.True(result));
    }

    [Fact]
    public void AllowPacket_ExceedsPerIpLimit_ReturnsFalse()
    {
        // Arrange
        var rateLimiter = new UdpRateLimiter(enabled: true, maxPacketsPerSecondPerIp: 5, maxTotalPacketsPerSecond: 100);
        var ip = IPAddress.Parse("192.168.1.1");

        // Act - exhaust the limit
        for (int i = 0; i < 5; i++)
        {
            rateLimiter.AllowPacket(ip);
        }

        // Additional request should be rejected
        var result = rateLimiter.AllowPacket(ip);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AllowPacket_DifferentIps_TrackedSeparately()
    {
        // Arrange
        var rateLimiter = new UdpRateLimiter(enabled: true, maxPacketsPerSecondPerIp: 3, maxTotalPacketsPerSecond: 100);
        var ip1 = IPAddress.Parse("192.168.1.1");
        var ip2 = IPAddress.Parse("192.168.1.2");

        // Act - exhaust limit for ip1
        for (int i = 0; i < 3; i++)
        {
            rateLimiter.AllowPacket(ip1);
        }

        // ip1 should be blocked
        var ip1Result = rateLimiter.AllowPacket(ip1);
        
        // ip2 should still be allowed
        var ip2Result = rateLimiter.AllowPacket(ip2);

        // Assert
        Assert.False(ip1Result);
        Assert.True(ip2Result);
    }

    [Fact]
    public void AllowPacket_ExceedsGlobalLimit_ReturnsFalse()
    {
        // Arrange
        var rateLimiter = new UdpRateLimiter(enabled: true, maxPacketsPerSecondPerIp: 100, maxTotalPacketsPerSecond: 5);
        var ips = Enumerable.Range(1, 10).Select(i => IPAddress.Parse($"192.168.1.{i}")).ToList();

        // Act - exhaust global limit
        for (int i = 0; i < 5; i++)
        {
            rateLimiter.AllowPacket(ips[i]);
        }

        // Additional request from any IP should be rejected
        var result = rateLimiter.AllowPacket(ips[6]);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AllowPacket_AfterDelay_AllowsMoreRequests()
    {
        // Arrange
        var rateLimiter = new UdpRateLimiter(enabled: true, maxPacketsPerSecondPerIp: 2, maxTotalPacketsPerSecond: 100);
        var ip = IPAddress.Parse("192.168.1.1");

        // Act - exhaust limit
        rateLimiter.AllowPacket(ip);
        rateLimiter.AllowPacket(ip);
        
        // Should be rejected immediately
        var immediateResult = rateLimiter.AllowPacket(ip);

        // Wait for tokens to refill
        await Task.Delay(1100); // Wait just over 1 second

        // Should be allowed again
        var delayedResult = rateLimiter.AllowPacket(ip);

        // Assert
        Assert.False(immediateResult);
        Assert.True(delayedResult);
    }
}
