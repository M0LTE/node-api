using Microsoft.Extensions.Logging;
using NSubstitute;
using node_api.Models;
using node_api.Models.NetworkState;
using node_api.Services;
using Xunit;

namespace Tests;

public class LinkFlappingTests
{
    private readonly INetworkStateService _networkState;
    private readonly ILogger<NetworkStateUpdater> _logger;
    private readonly NetworkStateUpdater _updater;

    public LinkFlappingTests()
    {
        _logger = Substitute.For<ILogger<NetworkStateUpdater>>();
        _networkState = new NetworkStateService(
            Substitute.For<ILogger<NetworkStateService>>(),
            Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>());
        _updater = new NetworkStateUpdater(_networkState, _logger);
    }

    [Fact]
    public void NewLink_ShouldNotBeFlapping()
    {
        // Arrange
        var link = _networkState.GetOrCreateLink("M0LTE", "G8PZT");

        // Assert
        Assert.False(link.IsFlapping());
        Assert.Equal(0, link.FlapCount);
        Assert.Null(link.FlapWindowStart);
        Assert.Null(link.LastFlapTime);
    }

    [Fact]
    public void LinkGoingUp_AfterBeingDown_ShouldIncrementFlapCount()
    {
        // Arrange - First establish link
        var initialLinkUpEvent = new LinkUpEvent
        {
            DatagramType = "@link-up",
            Node = "M0LTE",
            Id = 1,
            Direction = "outgoing",
            Port = "2",
            Remote = "G8PZT",
            Local = "M0LTE",
            TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _updater.UpdateFromLinkUpEvent(initialLinkUpEvent);
        
        // Now simulate it going down
        var linkDownEvent = new LinkDisconnectionEvent
        {
            DatagramType = "@link-disconnection",
            Node = "M0LTE",
            Id = 1,
            Direction = "outgoing",
            Port = "2",
            Remote = "G8PZT",
            Local = "M0LTE",
            FramesSent = 100,
            FramesReceived = 100,
            FramesResent = 0,
            FramesQueued = 0,
            Reason = "Disconnect request"
        };
        _updater.UpdateFromLinkDownEvent(linkDownEvent);
        
        var link = _networkState.GetLink("G8PZT<->M0LTE");
        Assert.NotNull(link);
        Assert.Equal(node_api.Models.NetworkState.LinkStatus.Disconnected, link.Status);

        // Act - Simulate link coming back up
        var linkUpEvent = new LinkUpEvent
        {
            DatagramType = "@link-up",
            Node = "M0LTE",
            Id = 2,
            Direction = "outgoing",
            Port = "2",
            Remote = "G8PZT",
            Local = "M0LTE",
            TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _updater.UpdateFromLinkUpEvent(linkUpEvent);

        // Assert
        Assert.NotNull(link);
        Assert.Equal(node_api.Models.NetworkState.LinkStatus.Active, link.Status);
        Assert.Equal(1, link.FlapCount);
        Assert.NotNull(link.FlapWindowStart);
        Assert.NotNull(link.LastFlapTime);
    }

    [Fact]
    public void LinkGoingUp_MultipleTimes_ShouldAccumulateFlapCount()
    {
        // First, establish the link
        var initialLinkUpEvent = new LinkUpEvent
        {
            DatagramType = "@link-up",
            Node = "M0LTE",
            Id = 0,
            Direction = "outgoing",
            Port = "2",
            Remote = "G8PZT",
            Local = "M0LTE",
            TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _updater.UpdateFromLinkUpEvent(initialLinkUpEvent);
        
        // Simulate multiple down/up cycles (flaps)
        for (int i = 0; i < 4; i++)
        {
            // Link goes down
            var linkDownEvent = new LinkDisconnectionEvent
            {
                DatagramType = "@link-disconnection",
                Node = "M0LTE",
                Id = i * 2 + 1,
                Direction = "outgoing",
                Port = "2",
                Remote = "G8PZT",
                Local = "M0LTE",
                FramesSent = 100,
                FramesReceived = 100,
                FramesResent = 0,
                FramesQueued = 0
            };
            _updater.UpdateFromLinkDownEvent(linkDownEvent);

            // Link comes back up (this is a flap)
            var linkUpEvent = new LinkUpEvent
            {
                DatagramType = "@link-up",
                Node = "M0LTE",
                Id = i * 2 + 2,
                Direction = "outgoing",
                Port = "2",
                Remote = "G8PZT",
                Local = "M0LTE",
                TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            _updater.UpdateFromLinkUpEvent(linkUpEvent);
        }

        // Assert
        var link = _networkState.GetLink("G8PZT<->M0LTE");
        Assert.NotNull(link);
        Assert.Equal(4, link.FlapCount);
        Assert.True(link.IsFlapping());
    }

    [Fact]
    public void LinkFlapping_ShouldBeDetectedWithDefaultThreshold()
    {
        // Arrange - First establish the link
        var initialUp = new LinkUpEvent
        {
            DatagramType = "@link-up",
            Node = "M0LTE",
            Id = 0,
            Direction = "outgoing",
            Port = "2",
            Remote = "G8PZT",
            Local = "M0LTE",
            TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _updater.UpdateFromLinkUpEvent(initialUp);
        
        // Simulate 3 flaps (threshold)
        for (int i = 0; i < 3; i++)
        {
            var linkDownEvent = new LinkDisconnectionEvent
            {
                DatagramType = "@link-disconnection",
                Node = "M0LTE",
                Id = i * 2 + 1,
                Direction = "outgoing",
                Port = "2",
                Remote = "G8PZT",
                Local = "M0LTE",
                FramesSent = 100,
                FramesReceived = 100,
                FramesResent = 0,
                FramesQueued = 0
            };
            _updater.UpdateFromLinkDownEvent(linkDownEvent);

            var linkUpEvent = new LinkUpEvent
            {
                DatagramType = "@link-up",
                Node = "M0LTE",
                Id = i * 2 + 2,
                Direction = "outgoing",
                Port = "2",
                Remote = "G8PZT",
                Local = "M0LTE",
                TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            _updater.UpdateFromLinkUpEvent(linkUpEvent);
        }

        // Assert
        var link = _networkState.GetLink("G8PZT<->M0LTE");
        Assert.NotNull(link);
        Assert.Equal(3, link.FlapCount);
        Assert.True(link.IsFlapping(flapThreshold: 3));
    }

    [Fact]
    public void LinkFlapping_ShouldNotBeDetectedBelowThreshold()
    {
        // Arrange - First establish the link
        var initialUp = new LinkUpEvent
        {
            DatagramType = "@link-up",
            Node = "M0LTE",
            Id = 0,
            Direction = "outgoing",
            Port = "2",
            Remote = "G8PZT",
            Local = "M0LTE",
            TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _updater.UpdateFromLinkUpEvent(initialUp);
        
        // Simulate 2 flaps (below default threshold of 3)
        for (int i = 0; i < 2; i++)
        {
            var linkDownEvent = new LinkDisconnectionEvent
            {
                DatagramType = "@link-disconnection",
                Node = "M0LTE",
                Id = i * 2 + 1,
                Direction = "outgoing",
                Port = "2",
                Remote = "G8PZT",
                Local = "M0LTE",
                FramesSent = 100,
                FramesReceived = 100,
                FramesResent = 0,
                FramesQueued = 0
            };
            _updater.UpdateFromLinkDownEvent(linkDownEvent);

            var linkUpEvent = new LinkUpEvent
            {
                DatagramType = "@link-up",
                Node = "M0LTE",
                Id = i * 2 + 2,
                Direction = "outgoing",
                Port = "2",
                Remote = "G8PZT",
                Local = "M0LTE",
                TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            _updater.UpdateFromLinkUpEvent(linkUpEvent);
        }

        // Assert
        var link = _networkState.GetLink("G8PZT<->M0LTE");
        Assert.NotNull(link);
        Assert.Equal(2, link.FlapCount);
        Assert.False(link.IsFlapping(flapThreshold: 3));
    }

    [Fact]
    public void LinkFlapping_ShouldUseCustomThreshold()
    {
        // Arrange - First establish the link
        var initialUp = new LinkUpEvent
        {
            DatagramType = "@link-up",
            Node = "M0LTE",
            Id = 0,
            Direction = "outgoing",
            Port = "2",
            Remote = "G8PZT",
            Local = "M0LTE",
            TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _updater.UpdateFromLinkUpEvent(initialUp);
        
        // Simulate 2 flaps
        for (int i = 0; i < 2; i++)
        {
            var linkDownEvent = new LinkDisconnectionEvent
            {
                DatagramType = "@link-disconnection",
                Node = "M0LTE",
                Id = i * 2 + 1,
                Direction = "outgoing",
                Port = "2",
                Remote = "G8PZT",
                Local = "M0LTE",
                FramesSent = 100,
                FramesReceived = 100,
                FramesResent = 0,
                FramesQueued = 0
            };
            _updater.UpdateFromLinkDownEvent(linkDownEvent);

            var linkUpEvent = new LinkUpEvent
            {
                DatagramType = "@link-up",
                Node = "M0LTE",
                Id = i * 2 + 2,
                Direction = "outgoing",
                Port = "2",
                Remote = "G8PZT",
                Local = "M0LTE",
                TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            _updater.UpdateFromLinkUpEvent(linkUpEvent);
        }

        // Assert - Should be flapping with threshold of 2
        var link = _networkState.GetLink("G8PZT<->M0LTE");
        Assert.NotNull(link);
        Assert.Equal(2, link.FlapCount);
        Assert.True(link.IsFlapping(flapThreshold: 2));
        Assert.False(link.IsFlapping(flapThreshold: 3));
    }

    [Fact]
    public void LinkFlapping_ShouldResetAfterWindowExpires()
    {
        // Arrange - Manually set up a link with expired flap window
        var link = _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        link.FlapCount = 5;
        link.FlapWindowStart = DateTime.UtcNow.AddMinutes(-20); // 20 minutes ago
        link.LastFlapTime = DateTime.UtcNow.AddMinutes(-20);

        // Assert - Should not be flapping with default 15-minute window
        Assert.False(link.IsFlapping(flapThreshold: 3, windowMinutes: 15));
    }

    [Fact]
    public void LinkFlapping_NewUpEvent_AfterExpiredWindow_ShouldStartNewWindow()
    {
        // Arrange - Set up link with old flap data
        var link = _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        link.Status = node_api.Models.NetworkState.LinkStatus.Disconnected;
        link.FlapCount = 5;
        link.FlapWindowStart = DateTime.UtcNow.AddMinutes(-20); // Expired window
        link.LastFlapTime = DateTime.UtcNow.AddMinutes(-20);

        // Act - New up event after expired window
        var linkUpEvent = new LinkUpEvent
        {
            DatagramType = "@link-up",
            Node = "M0LTE",
            Id = 1,
            Direction = "outgoing",
            Port = "2",
            Remote = "G8PZT",
            Local = "M0LTE",
            TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _updater.UpdateFromLinkUpEvent(linkUpEvent);

        // Assert - Should start a new window with count reset to 1
        Assert.Equal(1, link.FlapCount);
        Assert.NotNull(link.FlapWindowStart);
        // Window should be recent (within last minute)
        Assert.True(link.FlapWindowStart.Value > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void FirstLinkUp_ShouldNotTrackAsFlap()
    {
        // Arrange & Act - First time a link comes up (not disconnected before)
        var linkUpEvent = new LinkUpEvent
        {
            DatagramType = "@link-up",
            Node = "M0LTE",
            Id = 1,
            Direction = "outgoing",
            Port = "2",
            Remote = "G8PZT",
            Local = "M0LTE",
            TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _updater.UpdateFromLinkUpEvent(linkUpEvent);

        // Assert - Should not be counted as a flap
        var link = _networkState.GetLink("G8PZT<->M0LTE");
        Assert.NotNull(link);
        Assert.Equal(0, link.FlapCount);
        Assert.Null(link.FlapWindowStart);
    }

    [Fact]
    public void IsFlapping_WithCustomWindowMinutes_ShouldRespectWindow()
    {
        // Arrange - Set up link with flaps from 10 minutes ago
        var link = _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        link.FlapCount = 5;
        link.FlapWindowStart = DateTime.UtcNow.AddMinutes(-10);
        link.LastFlapTime = DateTime.UtcNow.AddMinutes(-10);

        // Assert
        Assert.False(link.IsFlapping(flapThreshold: 3, windowMinutes: 5)); // Window expired
        Assert.True(link.IsFlapping(flapThreshold: 3, windowMinutes: 15)); // Within window
    }

    [Fact]
    public void MultipleDifferentLinks_ShouldTrackFlapsIndependently()
    {
        // Arrange & Act - Establish first link
        var initialUp1 = new LinkUpEvent
        {
            DatagramType = "@link-up",
            Node = "M0LTE",
            Id = 0,
            Direction = "outgoing",
            Port = "2",
            Remote = "G8PZT",
            Local = "M0LTE",
            TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _updater.UpdateFromLinkUpEvent(initialUp1);
        
        // Create flaps for first link
        for (int i = 0; i < 3; i++)
        {
            var downEvent = new LinkDisconnectionEvent
            {
                DatagramType = "@link-disconnection",
                Node = "M0LTE",
                Id = i,
                Direction = "outgoing",
                Port = "2",
                Remote = "G8PZT",
                Local = "M0LTE",
                FramesSent = 100,
                FramesReceived = 100,
                FramesResent = 0,
                FramesQueued = 0
            };
            _updater.UpdateFromLinkDownEvent(downEvent);

            var upEvent = new LinkUpEvent
            {
                DatagramType = "@link-up",
                Node = "M0LTE",
                Id = i,
                Direction = "outgoing",
                Port = "2",
                Remote = "G8PZT",
                Local = "M0LTE",
                TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            _updater.UpdateFromLinkUpEvent(upEvent);
        }

        // Establish second link first
        var initialUp2 = new LinkUpEvent
        {
            DatagramType = "@link-up",
            Node = "M0LTE",
            Id = 0,
            Direction = "outgoing",
            Port = "2",
            Remote = "M0XYZ",
            Local = "M0LTE",
            TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _updater.UpdateFromLinkUpEvent(initialUp2);
        
        // Create one flap for second link
        var downEvent2 = new LinkDisconnectionEvent
        {
            DatagramType = "@link-disconnection",
            Node = "M0LTE",
            Id = 1,
            Direction = "outgoing",
            Port = "2",
            Remote = "M0XYZ",
            Local = "M0LTE",
            FramesSent = 100,
            FramesReceived = 100,
            FramesResent = 0,
            FramesQueued = 0
        };
        _updater.UpdateFromLinkDownEvent(downEvent2);

        var upEvent2 = new LinkUpEvent
        {
            DatagramType = "@link-up",
            Node = "M0LTE",
            Id = 2,
            Direction = "outgoing",
            Port = "2",
            Remote = "M0XYZ",
            Local = "M0LTE",
            TimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _updater.UpdateFromLinkUpEvent(upEvent2);

        // Assert
        var link1 = _networkState.GetLink("G8PZT<->M0LTE");
        var link2 = _networkState.GetLink("M0LTE<->M0XYZ");
        
        Assert.NotNull(link1);
        Assert.NotNull(link2);
        Assert.Equal(3, link1.FlapCount);
        Assert.Equal(1, link2.FlapCount);
        Assert.True(link1.IsFlapping());
        Assert.False(link2.IsFlapping());
    }
}
