using node_api.Models.NetworkState;

namespace node_api.Services;

/// <summary>
/// Background service that periodically persists network state to MySQL.
/// Runs every N seconds to sync in-memory state to database.
/// If database is unavailable, gracefully falls back to in-memory-only mode.
/// Only persists entities that have changed since last persist (dirty tracking).
/// </summary>
public class NetworkStatePersistenceService : BackgroundService
{
    private readonly INetworkStateService _networkState;
    private readonly MySqlNetworkStateRepository _repository;
    private readonly ILogger<NetworkStatePersistenceService> _logger;
    private readonly TimeSpan _persistInterval;
    private bool _databaseAvailable = true;

    public NetworkStatePersistenceService(
        INetworkStateService networkState,
        MySqlNetworkStateRepository repository,
        ILogger<NetworkStatePersistenceService> logger)
    {
        _networkState = networkState;
        _repository = repository;
        _logger = logger;
        
        // Default to persisting every 30 seconds
        var intervalSeconds = Environment.GetEnvironmentVariable("NETWORK_STATE_PERSIST_INTERVAL_SECONDS");
        _persistInterval = int.TryParse(intervalSeconds, out var seconds) 
            ? TimeSpan.FromSeconds(seconds) 
            : TimeSpan.FromSeconds(30);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Network state persistence service started. Persist interval: {Interval}s", _persistInterval.TotalSeconds);

        // Attempt to load initial state from database
        await LoadInitialStateAsync(stoppingToken);

        if (!_databaseAvailable)
        {
            _logger.LogWarning("Database unavailable - running in memory-only mode");
        }

        // Periodically persist state to database (if available)
        using var timer = new PeriodicTimer(_persistInterval);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                
                if (_databaseAvailable)
                {
                    await PersistStateAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting network state");
            }
        }

        // Final persist on shutdown (if database is available)
        if (_databaseAvailable)
        {
            try
            {
                await PersistStateAsync(CancellationToken.None);
                _logger.LogInformation("Network state persisted on shutdown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting network state on shutdown");
            }
        }
    }

    private async Task LoadInitialStateAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Loading initial network state from database...");
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            // Load nodes
            var nodes = await _repository.GetAllNodesAsync(ct);
            var nodeCount = 0;
            foreach (var node in nodes)
            {
                var existingNode = _networkState.GetOrCreateNode(node.Callsign);
                CopyNodeState(node, existingNode);
                _networkState.MarkNodeClean(existingNode); // Mark as clean since we just loaded it
                nodeCount++;
            }

            // Load links
            var links = await _repository.GetAllLinksAsync(ct);
            var linkCount = 0;
            foreach (var link in links)
            {
                var existingLink = _networkState.GetOrCreateLink(link.Endpoint1, link.Endpoint2);
                CopyLinkState(link, existingLink);
                _networkState.MarkLinkClean(existingLink); // Mark as clean since we just loaded it
                linkCount++;
            }

            // Load circuits
            var circuits = await _repository.GetAllCircuitsAsync(ct);
            var circuitCount = 0;
            foreach (var circuit in circuits)
            {
                var existingCircuit = _networkState.GetOrCreateCircuit(circuit.Endpoint1, circuit.Endpoint2);
                CopyCircuitState(circuit, existingCircuit);
                _networkState.MarkCircuitClean(existingCircuit); // Mark as clean since we just loaded it
                circuitCount++;
            }

            sw.Stop();
            _logger.LogInformation(
                "Loaded {NodeCount} nodes, {LinkCount} links, {CircuitCount} circuits from database in {ElapsedMs}ms",
                nodeCount, linkCount, circuitCount, sw.ElapsedMilliseconds);
            
            _databaseAvailable = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load initial network state from database - continuing in memory-only mode");
            _databaseAvailable = false;
        }
    }

    private async Task PersistStateAsync(CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var nodeCount = 0;
        var linkCount = 0;
        var circuitCount = 0;

        try
        {
            // Persist only dirty nodes
            var dirtyNodes = _networkState.GetDirtyNodes().ToList();
            foreach (var node in dirtyNodes)
            {
                await _repository.UpsertNodeAsync(node, ct);
                _networkState.MarkNodeClean(node);
                nodeCount++;
            }

            // Persist only dirty links
            var dirtyLinks = _networkState.GetDirtyLinks().ToList();
            foreach (var link in dirtyLinks)
            {
                await _repository.UpsertLinkAsync(link, ct);
                _networkState.MarkLinkClean(link);
                linkCount++;
            }

            // Persist only dirty circuits
            var dirtyCircuits = _networkState.GetDirtyCircuits().ToList();
            foreach (var circuit in dirtyCircuits)
            {
                await _repository.UpsertCircuitAsync(circuit, ct);
                _networkState.MarkCircuitClean(circuit);
                circuitCount++;
            }

            sw.Stop();
            
            if (nodeCount > 0 || linkCount > 0 || circuitCount > 0)
            {
                _logger.LogInformation(
                    "Persisted {NodeCount} dirty nodes, {LinkCount} dirty links, {CircuitCount} dirty circuits in {ElapsedMs}ms",
                    nodeCount, linkCount, circuitCount, sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogDebug("No dirty entities to persist");
            }
            
            // If we successfully persisted and database was previously unavailable, mark it as available
            if (!_databaseAvailable)
            {
                _logger.LogInformation("Database connection restored - resuming persistence");
                _databaseAvailable = true;
            }
        }
        catch (Exception ex)
        {
            if (_databaseAvailable)
            {
                _logger.LogWarning(ex, "Database became unavailable - switching to memory-only mode");
                _databaseAvailable = false;
            }
            else
            {
                _logger.LogDebug(ex, "Database still unavailable - continuing in memory-only mode");
            }
        }
    }

    private static void CopyNodeState(NodeState source, NodeState target)
    {
        target.Alias = source.Alias;
        target.Locator = source.Locator;
        target.Latitude = source.Latitude;
        target.Longitude = source.Longitude;
        target.Software = source.Software;
        target.Version = source.Version;
        target.UptimeSecs = source.UptimeSecs;
        target.LinksIn = source.LinksIn;
        target.LinksOut = source.LinksOut;
        target.CircuitsIn = source.CircuitsIn;
        target.CircuitsOut = source.CircuitsOut;
        target.L3Relayed = source.L3Relayed;
        target.Status = source.Status;
        target.LastSeen = source.LastSeen;
        target.FirstSeen = source.FirstSeen;
        target.LastStatusUpdate = source.LastStatusUpdate;
        target.LastUpEvent = source.LastUpEvent;
        target.LastDownEvent = source.LastDownEvent;
        target.L2TraceCount = source.L2TraceCount;
        target.LastL2Trace = source.LastL2Trace;
    }

    private static void CopyLinkState(LinkState source, LinkState target)
    {
        target.Status = source.Status;
        target.ConnectedAt = source.ConnectedAt;
        target.DisconnectedAt = source.DisconnectedAt;
        target.LastUpdate = source.LastUpdate;
        target.Initiator = source.Initiator;
        target.IsRF = source.IsRF;
        
        foreach (var (key, endpoint) in source.Endpoints)
        {
            target.Endpoints[key] = endpoint;
        }
    }

    private static void CopyCircuitState(CircuitState source, CircuitState target)
    {
        target.Status = source.Status;
        target.ConnectedAt = source.ConnectedAt;
        target.DisconnectedAt = source.DisconnectedAt;
        target.LastUpdate = source.LastUpdate;
        target.Initiator = source.Initiator;
        
        foreach (var (key, endpoint) in source.Endpoints)
        {
            target.Endpoints[key] = endpoint;
        }
    }
}
