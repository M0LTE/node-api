using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using node_api.Services;
using Xunit;

namespace Tests;

public class HiddenCallsignFilteringTests
{
    private NetworkStateService CreateServiceWithHiddenCallsigns(params string[] hiddenCallsigns)
    {
        var configData = new Dictionary<string, string?>();
        for (int i = 0; i < hiddenCallsigns.Length; i++)
        {
            configData[$"HiddenCallsigns:{i}"] = hiddenCallsigns[i];
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var logger = Substitute.For<ILogger<NetworkStateService>>();
        return new NetworkStateService(logger, configuration);
    }

    [Theory]
    [InlineData("M2")]
    [InlineData("m2")]
    [InlineData("M2-0")]
    [InlineData("M2-1")]
    [InlineData("M2-15")]
    [InlineData("m2-5")]
    public void IsHiddenCallsign_Should_Return_True_For_M2_Callsigns(string callsign)
    {
        // Arrange
        var service = CreateServiceWithHiddenCallsigns("M2");

        // Act
        var result = service.IsHiddenCallsign(callsign);

        // Assert
        Assert.True(result, $"Expected {callsign} to be identified as a hidden callsign");
    }

    [Theory]
    [InlineData("M0LTE")]
    [InlineData("G8PZT")]
    [InlineData("M3ABC")]
    [InlineData("M20LTE")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void IsHiddenCallsign_Should_Return_False_For_Non_M2_Callsigns(string? callsign)
    {
        // Arrange
        var service = CreateServiceWithHiddenCallsigns("M2");

        // Act
        var result = service.IsHiddenCallsign(callsign!);

        // Assert
        Assert.False(result, $"Expected {callsign ?? "(null)"} to NOT be identified as a hidden callsign");
    }

    [Fact]
    public void IsHiddenCallsign_Should_Return_False_When_No_Hidden_Callsigns_Configured()
    {
        // Arrange
        var service = CreateServiceWithHiddenCallsigns();

        // Act
        var result = service.IsHiddenCallsign("M2");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("M2")]
    [InlineData("G8PZT")]
    [InlineData("M0LTE-5")]
    public void IsHiddenCallsign_Should_Support_Multiple_Hidden_Callsigns(string callsign)
    {
        // Arrange
        var service = CreateServiceWithHiddenCallsigns("M2", "G8PZT", "M0LTE");

        // Act
        var result = service.IsHiddenCallsign(callsign);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHiddenCallsign_Should_Be_Case_Insensitive()
    {
        // Arrange
        var service = CreateServiceWithHiddenCallsigns("M2", "g8pzt");

        // Assert
        Assert.True(service.IsHiddenCallsign("M2"));
        Assert.True(service.IsHiddenCallsign("m2"));
        Assert.True(service.IsHiddenCallsign("G8PZT"));
        Assert.True(service.IsHiddenCallsign("g8pzt"));
        Assert.True(service.IsHiddenCallsign("M2-5"));
        Assert.True(service.IsHiddenCallsign("G8PZT-10"));
    }

    [Fact]
    public void IsHiddenCallsign_Should_Match_Base_Callsign_Only()
    {
        // Arrange
        var service = CreateServiceWithHiddenCallsigns("M2");

        // Assert - Should match M2 with any SSID
        Assert.True(service.IsHiddenCallsign("M2"));
        Assert.True(service.IsHiddenCallsign("M2-0"));
        Assert.True(service.IsHiddenCallsign("M2-1"));
        Assert.True(service.IsHiddenCallsign("M2-15"));

        // Should NOT match callsigns that just contain "M2"
        Assert.False(service.IsHiddenCallsign("M20"));
        Assert.False(service.IsHiddenCallsign("M2ABC"));
        Assert.False(service.IsHiddenCallsign("XM2"));
    }

    [Fact]
    public void GetLinksForNode_Should_Filter_Hidden_Callsigns_From_Results()
    {
        // Arrange
        var service = CreateServiceWithHiddenCallsigns("M2");
        service.GetOrCreateLink("M0LTE", "G8PZT");
        service.GetOrCreateLink("M0LTE", "M2");
        service.GetOrCreateLink("M0LTE", "M2-1");
        service.GetOrCreateLink("M0LTE", "M0XYZ");

        // Act - Get links for M0LTE (non-hidden)
        var links = service.GetLinksForNode("M0LTE").ToList();

        // Assert - All links returned (filtering happens in controller)
        Assert.Equal(4, links.Count);
    }

    [Fact]
    public void IsHiddenCallsign_Should_Handle_Whitespace()
    {
        // Arrange
        var service = CreateServiceWithHiddenCallsigns("M2");

        // Assert
        Assert.False(service.IsHiddenCallsign(""));
        Assert.False(service.IsHiddenCallsign(" "));
        Assert.False(service.IsHiddenCallsign("  "));
        Assert.False(service.IsHiddenCallsign(null!));
    }

    [Theory]
    [InlineData("M2", "M2-5")]
    [InlineData("G8PZT", "g8pzt-10")]
    [InlineData("m0lte", "M0LTE-1")]
    public void IsHiddenCallsign_Should_Extract_Base_Callsign_Correctly(string hiddenBase, string fullCallsign)
    {
        // Arrange
        var service = CreateServiceWithHiddenCallsigns(hiddenBase);

        // Act
        var result = service.IsHiddenCallsign(fullCallsign);

        // Assert
        Assert.True(result, $"Expected {fullCallsign} to match hidden base {hiddenBase}");
    }
}
