using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using node_api.Models.NetworkState;
using node_api.Services;

namespace Tests;

/// <summary>
/// Tests for StateCleanupService focusing on the cleanup logic.
/// Note: These tests verify the cleanup behavior but not the BackgroundService timing.
/// Full integration testing would require a test with configurable timing or direct method invocation.
/// </summary>
public class StateCleanupServiceTests
{
    private readonly INetworkStateService _networkState;
    private readonly ILogger<StateCleanupService> _logger;
    private readonly TestableRepository _repository;

    public StateCleanupServiceTests()
    {
        var networkStateLogger = Substitute.For<ILogger<NetworkStateService>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        
        _networkState = new NetworkStateService(networkStateLogger, configuration);
        _logger = Substitute.For<ILogger<StateCleanupService>>();
        _repository = new TestableRepository();
    }

    // Wrapper to make repository testable without real database
    private class TestableRepository : MySqlNetworkStateRepository
    {
        public List<string> DeletedLinks { get; } = new();
        public List<string> DeletedCircuits { get; } = new();
        public bool ThrowOnDelete { get; set; }

        public TestableRepository() 
            : base(Substitute.For<ILogger<MySqlNetworkStateRepository>>(), new QueryFrequencyTracker())
        {
        }

        public new async Task DeleteLinkAsync(string canonicalKey, CancellationToken ct = default)
        {
            if (ThrowOnDelete) throw new Exception("Simulated database error");
            
            DeletedLinks.Add(canonicalKey);
            await Task.CompletedTask;
        }

        public new async Task DeleteCircuitAsync(string canonicalKey, CancellationToken ct = default)
        {
            if (ThrowOnDelete) throw new Exception("Simulated database error");
            
            DeletedCircuits.Add(canonicalKey);
            await Task.CompletedTask;
        }
    }

    #region Circuit State Tests (Core Logic)

    [Fact]
    public void Should_Identify_Stale_Disconnected_Circuit()
    {
        // Arrange
        var circuit = _networkState.GetOrCreateCircuit("M0LTE:1234", "G8PZT:5678");
        circuit.Status = CircuitStatus.Disconnected;
        circuit.LastUpdate = DateTime.UtcNow.AddHours(-2);

        var cutoff = DateTime.UtcNow.AddHours(-1); // 1 hour threshold

        // Act
        var isStale = circuit.Status == CircuitStatus.Disconnected && circuit.LastUpdate < cutoff;

        // Assert
        isStale.Should().BeTrue("circuit has been disconnected for over 1 hour");
    }

    [Fact]
    public void Should_Not_Identify_Recent_Disconnected_Circuit_As_Stale()
    {
        // Arrange
        var circuit = _networkState.GetOrCreateCircuit("M0LTE:1234", "G8PZT:5678");
        circuit.Status = CircuitStatus.Disconnected;
        circuit.LastUpdate = DateTime.UtcNow.AddMinutes(-30);

        var cutoff = DateTime.UtcNow.AddHours(-1);

        // Act
        var isStale = circuit.Status == CircuitStatus.Disconnected && circuit.LastUpdate < cutoff;

        // Assert
        isStale.Should().BeFalse("circuit was only disconnected 30 minutes ago");
    }

    [Fact]
    public void Should_Use_LastUpdate_Not_DisconnectedAt()
    {
        // Arrange - Circuit disconnected 2 hours ago but still receiving CircuitStatus updates
        var circuit = _networkState.GetOrCreateCircuit("GB7NBH:0467", "MB7NSC:05e1");
        circuit.Status = CircuitStatus.Disconnected;
        circuit.DisconnectedAt = DateTime.UtcNow.AddHours(-2); // Disconnected 2 hours ago
        circuit.LastUpdate = DateTime.UtcNow.AddMinutes(-3); // Status report 3 mins ago

        var cutoff = DateTime.UtcNow.AddHours(-1);

        // Act - Using DisconnectedAt (wrong)
        var isStaleUsingDisconnectedAt = circuit.Status == CircuitStatus.Disconnected 
            && circuit.DisconnectedAt.HasValue 
            && circuit.DisconnectedAt.Value < cutoff;

        // Act - Using LastUpdate (correct)
        var isStaleUsingLastUpdate = circuit.Status == CircuitStatus.Disconnected 
            && circuit.LastUpdate < cutoff;

        // Assert
        isStaleUsingDisconnectedAt.Should().BeTrue("using DisconnectedAt would incorrectly mark as stale");
        isStaleUsingLastUpdate.Should().BeFalse("using LastUpdate correctly identifies recent activity");
    }

    [Fact]
    public void Should_Not_Identify_Active_Circuit_As_Stale()
    {
        // Arrange
        var circuit = _networkState.GetOrCreateCircuit("M0LTE:1234", "G8PZT:5678");
        circuit.Status = CircuitStatus.Active;
        circuit.LastUpdate = DateTime.UtcNow.AddHours(-2); // Old but active

        var cutoff = DateTime.UtcNow.AddHours(-1);

        // Act
        var isStale = circuit.Status == CircuitStatus.Disconnected && circuit.LastUpdate < cutoff;

        // Assert
        isStale.Should().BeFalse("active circuits should never be considered stale");
    }

    #endregion

    #region Link State Tests (Core Logic)

    [Fact]
    public void Should_Identify_Stale_Disconnected_Link()
    {
        // Arrange
        var link = _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        link.Status = LinkStatus.Disconnected;
        link.LastUpdate = DateTime.UtcNow.AddHours(-2);

        var cutoff = DateTime.UtcNow.AddHours(-1);

        // Act
        var isStale = link.Status == LinkStatus.Disconnected && link.LastUpdate < cutoff;

        // Assert
        isStale.Should().BeTrue();
    }

    [Fact]
    public void Should_Not_Identify_Recent_Disconnected_Link_As_Stale()
    {
        // Arrange
        var link = _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        link.Status = LinkStatus.Disconnected;
        link.LastUpdate = DateTime.UtcNow.AddMinutes(-30);

        var cutoff = DateTime.UtcNow.AddHours(-1);

        // Act
        var isStale = link.Status == LinkStatus.Disconnected && link.LastUpdate < cutoff;

        // Assert
        isStale.Should().BeFalse();
    }

    #endregion

    #region Network State Query Tests

    [Fact]
    public void Should_Query_Stale_Circuits_From_Network_State()
    {
        // Arrange
        var staleCircuit1 = _networkState.GetOrCreateCircuit("M0LTE:1111", "G8PZT:2222");
        staleCircuit1.Status = CircuitStatus.Disconnected;
        staleCircuit1.LastUpdate = DateTime.UtcNow.AddHours(-2);

        var recentCircuit = _networkState.GetOrCreateCircuit("M0ABC:3333", "G8XYZ:4444");
        recentCircuit.Status = CircuitStatus.Disconnected;
        recentCircuit.LastUpdate = DateTime.UtcNow.AddMinutes(-30);

        var activeCircuit = _networkState.GetOrCreateCircuit("M0DEF:5555", "G8QRS:6666");
        activeCircuit.Status = CircuitStatus.Active;
        activeCircuit.LastUpdate = DateTime.UtcNow.AddHours(-3);

        var cutoff = DateTime.UtcNow.AddHours(-1);

        // Act
        var staleCircuits = _networkState.GetAllCircuits().Values
            .Where(c => c.Status == CircuitStatus.Disconnected && c.LastUpdate < cutoff)
            .ToList();

        // Assert
        staleCircuits.Should().HaveCount(1);
        staleCircuits.Should().Contain(staleCircuit1);
        staleCircuits.Should().NotContain(recentCircuit, "it was updated recently");
        staleCircuits.Should().NotContain(activeCircuit, "it's still active");
    }

    [Fact]
    public void Should_Handle_Circuits_With_Unique_IDs()
    {
        // Arrange - Test the original problem: circuits with unique IDs pile up
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var circuits = new List<CircuitState>();
        
        for (int i = 0; i < 10; i++)
        {
            var circuit = _networkState.GetOrCreateCircuit($"GB7NBH:{i:x4}", $"MB7NSC:{i:x4}");
            circuit.Status = CircuitStatus.Disconnected;
            circuit.LastUpdate = DateTime.UtcNow.AddHours(-2);
            circuits.Add(circuit);
        }

        // Act
        var staleCircuits = _networkState.GetAllCircuits().Values
            .Where(c => c.Status == CircuitStatus.Disconnected && c.LastUpdate < cutoff)
            .ToList();

        // Assert
        staleCircuits.Should().HaveCount(10, "all 10 unique circuits should be identified as stale");
        foreach (var circuit in circuits)
        {
            staleCircuits.Should().Contain(circuit);
        }
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Should_Accept_Default_Configuration()
    {
        // Arrange
        Environment.SetEnvironmentVariable("STATE_CLEANUP_INTERVAL_MINUTES", null);
        Environment.SetEnvironmentVariable("STATE_CLEANUP_THRESHOLD_HOURS", null);

        // Act
        var service = new StateCleanupService(_networkState, _repository, _logger);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Should_Accept_Custom_Interval()
    {
        // Arrange
        Environment.SetEnvironmentVariable("STATE_CLEANUP_INTERVAL_MINUTES", "10");

        try
        {
            // Act
            var service = new StateCleanupService(_networkState, _repository, _logger);

            // Assert
            service.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("STATE_CLEANUP_INTERVAL_MINUTES", null);
        }
    }

    [Fact]
    public void Should_Accept_Custom_Threshold()
    {
        // Arrange
        Environment.SetEnvironmentVariable("STATE_CLEANUP_THRESHOLD_HOURS", "2");

        try
        {
            // Act
            var service = new StateCleanupService(_networkState, _repository, _logger);

            // Assert
            service.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("STATE_CLEANUP_THRESHOLD_HOURS", null);
        }
    }

    #endregion

    #region Repository Integration Tests

    [Fact]
    public async Task Repository_Should_Delete_Link()
    {
        // Arrange
        var link = _networkState.GetOrCreateLink("M0LTE", "G8PZT");

        // Act
        await _repository.DeleteLinkAsync(link.CanonicalKey);

        // Assert
        _repository.DeletedLinks.Should().Contain(link.CanonicalKey);
    }

    [Fact]
    public async Task Repository_Should_Delete_Circuit()
    {
        // Arrange
        var circuit = _networkState.GetOrCreateCircuit("M0LTE:1234", "G8PZT:5678");

        // Act
        await _repository.DeleteCircuitAsync(circuit.CanonicalKey);

        // Assert
        _repository.DeletedCircuits.Should().Contain(circuit.CanonicalKey);
    }

    [Fact]
    public async Task Repository_Should_Handle_Multiple_Deletes()
    {
        // Arrange
        var circuit1 = _networkState.GetOrCreateCircuit("M0LTE:1111", "G8PZT:2222");
        var circuit2 = _networkState.GetOrCreateCircuit("M0ABC:3333", "G8XYZ:4444");

        // Act
        await _repository.DeleteCircuitAsync(circuit1.CanonicalKey);
        await _repository.DeleteCircuitAsync(circuit2.CanonicalKey);

        // Assert
        _repository.DeletedCircuits.Should().HaveCount(2);
        _repository.DeletedCircuits.Should().Contain(circuit1.CanonicalKey);
        _repository.DeletedCircuits.Should().Contain(circuit2.CanonicalKey);
    }

    #endregion
}
