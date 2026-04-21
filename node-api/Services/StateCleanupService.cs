using node_api.Models.NetworkState;

namespace node_api.Services;

/// <summary>
/// Background service that periodically removes stale entities (links, circuits) from the network state.
/// Runs every configured interval to clean up entities that have not been updated within the configured threshold.
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
        
        // Default: Remove entities that have not been updated for more than 1 hour
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
                         && l.LastUpdate < cutoff)
                .Select(l => l.CanonicalKey)
                .ToList();

            if (disconnectedLinks.Count > 0)
            {
                try
                {
                    // Batch delete from database
                    await _repository.BatchDeleteLinksAsync(disconnectedLinks, ct);
                    
                    // Remove from in-memory state
                    foreach (var key in disconnectedLinks)
                    {
                        if (_networkState.RemoveLink(key))
                        {
                            linksRemoved++;
                        }
                    }
                    
                    _logger.LogDebug("Batch removed {Count} stale links", linksRemoved);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to batch remove stale links");
                }
            }

            // Clean up stale circuits
            // A circuit is stale if:
            // 1. It hasn't been updated recently, regardless of current status
            // Note: CircuitStatus reports arrive every 5 minutes for active circuits.
            // If a circuit stops reporting without a disconnect event, it must still age out.
            var staleCircuits = _networkState.GetAllCircuits().Values
                .Where(c => c.LastUpdate < cutoff)
                .Select(c => c.CanonicalKey)
                .ToList();

            if (staleCircuits.Count > 0)
            {
                try
                {
                    // Batch delete from database
                    await _repository.BatchDeleteCircuitsAsync(staleCircuits, ct);
                    
                    // Remove from in-memory state
                    foreach (var key in staleCircuits)
                    {
                        if (_networkState.RemoveCircuit(key))
                        {
                            circuitsRemoved++;
                        }
                    }
                    
                    _logger.LogDebug("Batch removed {Count} stale circuits", circuitsRemoved);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to batch remove stale circuits");
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
                _logger.LogInformation("Cleanup completed in {ElapsedMs}ms: No stale entities found", sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup: Removed {LinksRemoved} links and {CircuitsRemoved} circuits before error", 
                linksRemoved, circuitsRemoved);
        }
    }
}
