using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using node_api.Models;
using node_api.Models.NetworkState;
using node_api.Services;
using Xunit;

namespace Tests;

/// <summary>
/// Tests for the AX.25 routing heuristic in NetworkStateUpdater.UpdateFromL2Trace
/// 
/// Background: In AX.25, when a user makes an L2 connection through an intermediate node,
/// that node transmits using the user's callsign (not its own). This means we can hear
/// frames where the source callsign is not the actual transmitter.
/// 
/// The heuristic: If dirn=="sent" AND base(source) != base(reportFrom), the source
/// callsign is being impersonated by the reporter, and we should NOT infer a direct link.
/// </summary>
public class NetworkStateUpdaterL2TraceTests
{
    private readonly NetworkStateService _networkState;
    private readonly NetworkStateUpdater _updater;
    private readonly ILogger<NetworkStateUpdater> _logger;

    public NetworkStateUpdaterL2TraceTests()
    {
        _logger = Substitute.For<ILogger<NetworkStateUpdater>>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        _networkState = new NetworkStateService(Substitute.For<ILogger<NetworkStateService>>(), config);
        _updater = new NetworkStateUpdater(_networkState, _logger);
    }

    #region Basic L2Trace Processing

    [Fact]
    public void UpdateFromL2Trace_UpdatesReporterNode()
    {
        // Arrange
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-1",
            Port = "1",
            Source = "M0LTE",
            Destination = "G8PZT-2",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        // Act
        _updater.UpdateFromL2Trace(trace);

        // Assert
        var reporterNode = _networkState.GetNode("G8PZT-1");
        Assert.NotNull(reporterNode);
        Assert.Equal(1, reporterNode.L2TraceCount);
        Assert.NotNull(reporterNode.LastL2Trace);
        Assert.Equal(NodeStatus.Online, reporterNode.Status);
    }

    [Fact]
    public void UpdateFromL2Trace_TracksSourceAndDestinationNodes()
    {
        // Arrange
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-1",
            Port = "1",
            Source = "M0LTE",
            Destination = "G8PZT-2",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C"
        };

        // Act
        _updater.UpdateFromL2Trace(trace);

        // Assert
        var sourceNode = _networkState.GetNode("M0LTE");
        Assert.NotNull(sourceNode);
        Assert.Equal(NodeStatus.Online, sourceNode.Status);

        var destNode = _networkState.GetNode("G8PZT-2");
        Assert.NotNull(destNode);
        Assert.Equal(NodeStatus.Online, destNode.Status);
    }

    #endregion

    #region Link Inference - Direction "rcvd"

    [Fact]
    public void UpdateFromL2Trace_WithRcvd_CanInferLink()
    {
        // Arrange - Reporter received a frame from M0LTE to G8PZT-2
        // This is a legitimate observation, we can infer the link
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-1",
            Port = "1",
            Direction = "rcvd",
            Source = "M0LTE",
            Destination = "G8PZT-2",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            IsRF = true
        };

        // Create the link first (links are created by LinkUpEvent, not L2Trace)
        var existingLink = _networkState.GetOrCreateLink("M0LTE", "G8PZT-2");
        Assert.Null(existingLink.IsRF); // Initially unknown

        // Act
        _updater.UpdateFromL2Trace(trace);

        // Assert - Link RF status should be updated
        var link = _networkState.GetLink("G8PZT-2<->M0LTE");
        Assert.NotNull(link);
        Assert.True(link.IsRF);
    }

    #endregion

    #region Link Inference - Direction "sent" with matching base callsigns

    [Fact]
    public void UpdateFromL2Trace_WithSent_SameBaseCallsign_CanInferLink()
    {
        // Arrange - Reporter G8PZT-1 sent a frame from G8PZT-1 to M0LTE
        // Source base matches reporter base - this is legitimate
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-1",
            Port = "1",
            Direction = "sent",
            Source = "G8PZT-1",
            Destination = "M0LTE",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            IsRF = true
        };

        var existingLink = _networkState.GetOrCreateLink("G8PZT-1", "M0LTE");
        Assert.Null(existingLink.IsRF);

        // Act
        _updater.UpdateFromL2Trace(trace);

        // Assert - Link RF status should be updated
        var link = _networkState.GetLink("G8PZT-1<->M0LTE");
        Assert.NotNull(link);
        Assert.True(link.IsRF);
    }

    [Fact]
    public void UpdateFromL2Trace_WithSent_DifferentSSID_SameBase_CanInferLink()
    {
        // Arrange - Reporter G8PZT-1 sent a frame from G8PZT-2 to M0LTE
        // Different SSIDs but same base callsign - this is legitimate
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-1",
            Port = "1",
            Direction = "sent",
            Source = "G8PZT-2",
            Destination = "M0LTE",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            IsRF = false
        };

        var existingLink = _networkState.GetOrCreateLink("G8PZT-2", "M0LTE");
        Assert.Null(existingLink.IsRF);

        // Act
        _updater.UpdateFromL2Trace(trace);

        // Assert - Link RF status should be updated
        var link = _networkState.GetLink("G8PZT-2<->M0LTE");
        Assert.NotNull(link);
        Assert.False(link.IsRF);
    }

    #endregion

    #region Link Inference - Direction "sent" with different base callsigns (IMPERSONATION)

    [Fact]
    public void UpdateFromL2Trace_WithSent_DifferentBaseCallsign_DoesNotInferLink()
    {
        // Arrange - Reporter G8PZT-1 sent a frame from M0LTE to M0ABC
        // Source base (M0LTE) != reporter base (G8PZT) - M0LTE is being impersonated!
        // This happens when G8PZT is forwarding a connection for M0LTE
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-1",
            Port = "1",
            Direction = "sent",
            Source = "M0LTE",
            Destination = "M0ABC",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            IsRF = true
        };

        var existingLink = _networkState.GetOrCreateLink("M0LTE", "M0ABC");
        Assert.Null(existingLink.IsRF); // Initially unknown

        // Act
        _updater.UpdateFromL2Trace(trace);

        // Assert - Link RF status should NOT be updated (link remains uncertain)
        var link = _networkState.GetLink("M0ABC<->M0LTE");
        Assert.NotNull(link);
        Assert.Null(link.IsRF); // Should still be null - we didn't update it
    }

    [Fact]
    public void UpdateFromL2Trace_WithSent_DifferentBaseCallsign_WithSSID_DoesNotInferLink()
    {
        // Arrange - Reporter G8PZT-1 sent a frame from M0LTE-5 to M0ABC-2
        // Even with SSIDs, base callsigns are different - still impersonation
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-1",
            Port = "1",
            Direction = "sent",
            Source = "M0LTE-5",
            Destination = "M0ABC-2",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            IsRF = false
        };

        var existingLink = _networkState.GetOrCreateLink("M0LTE-5", "M0ABC-2");
        Assert.Null(existingLink.IsRF);

        // Act
        _updater.UpdateFromL2Trace(trace);

        // Assert - Link RF status should NOT be updated
        var link = _networkState.GetLink("M0ABC-2<->M0LTE-5");
        Assert.NotNull(link);
        Assert.Null(link.IsRF); // Should still be null
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void UpdateFromL2Trace_WithoutDirection_CanInferLink()
    {
        // Arrange - No direction specified (older format or missing field)
        // Be conservative and allow link inference
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-1",
            Port = "1",
            Direction = null,
            Source = "M0LTE",
            Destination = "G8PZT-2",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            IsRF = true
        };

        var existingLink = _networkState.GetOrCreateLink("M0LTE", "G8PZT-2");
        Assert.Null(existingLink.IsRF);

        // Act
        _updater.UpdateFromL2Trace(trace);

        // Assert - Link RF status should be updated (conservative approach)
        var link = _networkState.GetLink("G8PZT-2<->M0LTE");
        Assert.NotNull(link);
        Assert.True(link.IsRF);
    }

    [Fact]
    public void UpdateFromL2Trace_WithoutIsRF_DoesNotUpdateLink()
    {
        // Arrange - No IsRF information
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-1",
            Port = "1",
            Direction = "rcvd",
            Source = "M0LTE",
            Destination = "G8PZT-2",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            IsRF = null
        };

        var existingLink = _networkState.GetOrCreateLink("M0LTE", "G8PZT-2");
        Assert.Null(existingLink.IsRF);

        // Act
        _updater.UpdateFromL2Trace(trace);

        // Assert - Link RF status should remain null (no info to update)
        var link = _networkState.GetLink("G8PZT-2<->M0LTE");
        Assert.NotNull(link);
        Assert.Null(link.IsRF);
    }

    [Fact]
    public void UpdateFromL2Trace_NonExistentLink_DoesNotCreateLink()
    {
        // Arrange - L2Trace for a link that doesn't exist yet
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-1",
            Port = "1",
            Direction = "rcvd",
            Source = "M0LTE",
            Destination = "M0ABC",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            IsRF = true
        };

        // Don't create the link beforehand

        // Act
        _updater.UpdateFromL2Trace(trace);

        // Assert - Link should NOT be created (L2Traces don't create links, only LinkUpEvent does)
        var link = _networkState.GetLink("M0ABC<->M0LTE");
        Assert.Null(link);
    }

    #endregion

    #region Case Sensitivity

    [Fact]
    public void UpdateFromL2Trace_CaseInsensitive_SameBase_CanInferLink()
    {
        // Arrange - Mixed case, but same base
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "g8pzt-1",
            Port = "1",
            Direction = "sent",
            Source = "G8PZT-2",
            Destination = "M0LTE",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            IsRF = true
        };

        var existingLink = _networkState.GetOrCreateLink("G8PZT-2", "M0LTE");
        Assert.Null(existingLink.IsRF);

        // Act
        _updater.UpdateFromL2Trace(trace);

        // Assert - Should recognize same base despite case difference
        var link = _networkState.GetLink("G8PZT-2<->M0LTE");
        Assert.NotNull(link);
        Assert.True(link.IsRF);
    }

    [Fact]
    public void UpdateFromL2Trace_CaseInsensitive_DifferentBase_DoesNotInferLink()
    {
        // Arrange - Mixed case, different bases
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "g8pzt-1",
            Port = "1",
            Direction = "sent",
            Source = "M0LTE",
            Destination = "M0ABC",
            Control = 3,
            L2Type = "UI",
            CommandResponse = "C",
            IsRF = true
        };

        var existingLink = _networkState.GetOrCreateLink("M0LTE", "M0ABC");
        Assert.Null(existingLink.IsRF);

        // Act
        _updater.UpdateFromL2Trace(trace);

        // Assert - Should recognize different bases despite case difference
        var link = _networkState.GetLink("M0ABC<->M0LTE");
        Assert.NotNull(link);
        Assert.Null(link.IsRF); // Should not be updated
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
    public void UpdateFromL2Trace_RealWorld_UserConnectingThroughNode()
    {
        // Arrange - Real scenario:
        // User M0LTE connects to M0ABC through intermediate node G8PZT
        // G8PZT transmits frames using M0LTE's callsign
        
        // G8PZT reports sending a frame from M0LTE to M0ABC
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT",
            Port = "2",
            Direction = "sent",
            Source = "M0LTE",
            Destination = "M0ABC",
            Control = 0,
            L2Type = "I",
            CommandResponse = "C",
            IsRF = true
        };

        var existingLink = _networkState.GetOrCreateLink("M0LTE", "M0ABC");
        Assert.Null(existingLink.IsRF);

        // Act
        _updater.UpdateFromL2Trace(trace);

        // Assert - Should NOT update the link as this is impersonation
        // The actual RF link is G8PZT <-> M0ABC, not M0LTE <-> M0ABC
        var link = _networkState.GetLink("M0ABC<->M0LTE");
        Assert.NotNull(link);
        Assert.Null(link.IsRF); // Should not be updated
    }

    [Fact]
    public void UpdateFromL2Trace_RealWorld_NodeTransmittingAsItself()
    {
        // Arrange - Normal scenario:
        // Node G8PZT transmits a frame from itself to M0LTE
        
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT-1",
            Port = "2",
            Direction = "sent",
            Source = "G8PZT-1",
            Destination = "M0LTE",
            Control = 0,
            L2Type = "I",
            CommandResponse = "C",
            IsRF = true
        };

        var existingLink = _networkState.GetOrCreateLink("G8PZT-1", "M0LTE");
        Assert.Null(existingLink.IsRF);

        // Act
        _updater.UpdateFromL2Trace(trace);

        // Assert - Should update the link as this is legitimate
        var link = _networkState.GetLink("G8PZT-1<->M0LTE");
        Assert.NotNull(link);
        Assert.True(link.IsRF);
    }

    [Fact]
    public void UpdateFromL2Trace_RealWorld_NodeOverhearingTraffic()
    {
        // Arrange - Node G8PZT overhears traffic from M0LTE to M0ABC
        // This is a legitimate observation
        
        var trace = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G8PZT",
            Port = "2",
            Direction = "rcvd",
            Source = "M0LTE",
            Destination = "M0ABC",
            Control = 0,
            L2Type = "I",
            CommandResponse = "C",
            IsRF = true
        };

        var existingLink = _networkState.GetOrCreateLink("M0LTE", "M0ABC");
        Assert.Null(existingLink.IsRF);

        // Act
        _updater.UpdateFromL2Trace(trace);

        // Assert - Should update the link as this is a genuine observation
        var link = _networkState.GetLink("M0ABC<->M0LTE");
        Assert.NotNull(link);
        Assert.True(link.IsRF);
    }

    #endregion
}
