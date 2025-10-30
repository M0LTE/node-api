using node_api.Models.NetworkState;

namespace node_api.Services;

/// <summary>
/// Background service that periodically removes stale entities (links, circuits) from the network state.
/// Runs every configured interval to clean up disconnected entities older than a threshold.
/// </summary>
public class StateCleanupService : BackgroundService
{
    private readonly INetworkStateService _networkState;
    private readonly MySqlNetworkStateRepository _repository;
    private readonly ILogger<StateCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;
    private readonly TimeSpan _staleThreshold;

    public StateCleanupService(
        INetworkStateService networkState,
        MySqlNetworkStateRepository repository,
        ILogger<StateCleanupService> logger)
    {
        _networkState = networkState;
        _repository = repository;
        _logger = logger;
        
        // Default: Run cleanup every 5 minutes
        var intervalMinutes = Environment.GetEnvironmentVariable("STATE_CLEANUP_INTERVAL_MINUTES");
        _cleanupInterval = int.TryParse(intervalMinutes, out var minutes) 
            ? TimeSpan.FromMinutes(minutes) 
            : TimeSpan.FromMinutes(5);
        
        // Default: Remove entities that have been disconnected for more than 1 hour
        var thresholdHours = Environment.GetEnvironmentVariable("STATE_CLEANUP_THRESHOLD_HOURS");
        _staleThreshold = int.TryParse(thresholdHours, out var hours) 
            ? TimeSpan.FromHours(hours) 
            : TimeSpan.FromHours(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "State cleanup service started. Cleanup interval: {Interval}m, Stale threshold: {Threshold}h", 
            _cleanupInterval.TotalMinutes, 
            _staleThreshold.TotalHours);

        // Wait a bit before first cleanup to allow state to load
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        using var timer = new PeriodicTimer(_cleanupInterval);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                await CleanupStaleEntitiesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during state cleanup");
            }
        }
    }

    private async Task CleanupStaleEntitiesAsync(CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var cutoff = DateTime.UtcNow - _staleThreshold;
        
        int linksRemoved = 0;
        int circuitsRemoved = 0;

        try
        {
            // Clean up stale disconnected links
            // A link is stale if:
            // 1. It's disconnected AND
            // 2. It hasn't been updated recently (no status reports)
            var disconnectedLinks = _networkState.GetAllLinks().Values
                .Where(l => l.Status == LinkStatus.Disconnected 
                         && l.LastUpdate < cutoff)  // Changed: Use LastUpdate instead of DisconnectedAt
                .ToList();

            foreach (var link in disconnectedLinks)
            {
                try
                {
                    await _repository.DeleteLinkAsync(link.CanonicalKey, ct);
                    linksRemoved++;
                    _logger.LogDebug(
                        "Removed stale link: {CanonicalKey} (status: {Status}, last update: {LastUpdate})", 
                        link.CanonicalKey, link.Status, link.LastUpdate);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove stale link: {CanonicalKey}", link.CanonicalKey);
                }
            }

            // Clean up stale disconnected circuits
            // A circuit is stale if:
            // 1. It's disconnected AND
            // 2. It hasn't been updated recently (no status reports or disconnect events)
            // Note: CircuitStatus reports arrive every 5 minutes for active circuits,
            // so LastUpdate will be recent even if circuit was marked disconnected earlier
            var disconnectedCircuits = _networkState.GetAllCircuits().Values
                .Where(c => c.Status == CircuitStatus.Disconnected 
                         && c.LastUpdate < cutoff)  // Changed: Use LastUpdate instead of DisconnectedAt
                .ToList();

            foreach (var circuit in disconnectedCircuits)
            {
                try
                {
                    await _repository.DeleteCircuitAsync(circuit.CanonicalKey, ct);
                    circuitsRemoved++;
                    _logger.LogDebug(
                        "Removed stale circuit: {CanonicalKey} (status: {Status}, last update: {LastUpdate})", 
                        circuit.CanonicalKey, circuit.Status, circuit.LastUpdate);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove stale circuit: {CanonicalKey}", circuit.CanonicalKey);
                }
            }

            sw.Stop();
            
            if (linksRemoved > 0 || circuitsRemoved > 0)
            {
                _logger.LogInformation(
                    "Cleanup completed in {ElapsedMs}ms: Removed {LinksRemoved} stale links and {CircuitsRemoved} stale circuits (no updates for {ThresholdHours}h)",
                    sw.ElapsedMilliseconds, linksRemoved, circuitsRemoved, _staleThreshold.TotalHours);
            }
            else
            {
                _logger.LogDebug("Cleanup completed in {ElapsedMs}ms: No stale entities found", sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup: Removed {LinksRemoved} links and {CircuitsRemoved} circuits before error", 
                linksRemoved, circuitsRemoved);
        }
    }
}
