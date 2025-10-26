using node_api.Utilities;
using System.Net;
using Xunit;

namespace Tests;

public class IpNetworkValidatorTests
{
    [Theory]
    [InlineData("192.168.1.1", "192.168.1.0/24", true)]
    [InlineData("192.168.1.255", "192.168.1.0/24", true)]
    [InlineData("192.168.2.1", "192.168.1.0/24", false)]
    [InlineData("10.0.0.1", "10.0.0.0/8", true)]
    [InlineData("11.0.0.1", "10.0.0.0/8", false)]
    [InlineData("127.0.0.1", "127.0.0.0/8", true)]
    [InlineData("8.8.8.8", "0.0.0.0/0", true)]
    [InlineData("192.168.1.1", "192.168.1.1/32", true)]
    [InlineData("192.168.1.2", "192.168.1.1/32", false)]
    public void IsIpAllowed_SingleNetwork_ReturnsExpectedResult(string ipAddress, string cidr, bool expected)
    {
        // Arrange
        var ip = IPAddress.Parse(ipAddress);
        var networks = new[] { cidr };

        // Act
        var result = IpNetworkValidator.IsIpAllowed(ip, networks);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsIpAllowed_MultipleNetworks_AllowsAnyMatch()
    {
        // Arrange
        var ip = IPAddress.Parse("192.168.1.100");
        var networks = new[] { "10.0.0.0/8", "192.168.0.0/16", "172.16.0.0/12" };

        // Act
        var result = IpNetworkValidator.IsIpAllowed(ip, networks);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsIpAllowed_NoMatchingNetworks_ReturnsFalse()
    {
        // Arrange
        var ip = IPAddress.Parse("8.8.8.8");
        var networks = new[] { "10.0.0.0/8", "192.168.0.0/16" };

        // Act
        var result = IpNetworkValidator.IsIpAllowed(ip, networks);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsIpAllowed_AnyNetwork_AllowsAllIps()
    {
        // Arrange
        var ip = IPAddress.Parse("1.2.3.4");
        var networks = new[] { "0.0.0.0/0" };

        // Act
        var result = IpNetworkValidator.IsIpAllowed(ip, networks);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsIpAllowed_InvalidCidr_ReturnsFalse()
    {
        // Arrange
        var ip = IPAddress.Parse("192.168.1.1");
        var networks = new[] { "invalid-cidr" };

        // Act
        var result = IpNetworkValidator.IsIpAllowed(ip, networks);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsIpAllowed_NullIpAddress_ReturnsFalse()
    {
        // Arrange
        IPAddress? ip = null;
        var networks = new[] { "192.168.0.0/16" };

        // Act
        var result = IpNetworkValidator.IsIpAllowed(ip!, networks);

        // Assert
        Assert.False(result);
    }
}
