# Fix: UpsertCircuitAsync Being Called Excessively

## Problem

The `UpsertCircuitAsync` method (and similarly `UpsertLinkAsync` and `UpsertNodeAsync`) were being called extremely frequently, causing massive database load.

### Root Cause

The `NetworkStatePersistenceService` was configured to run every 30 seconds and persist **all** circuits, links, and nodes to the database on each run - regardless of whether they had changed:

```csharp
// OLD CODE - Persists everything every 30 seconds
var circuits = _networkState.GetAllCircuits();
foreach (var circuit in circuits.Values)
{
    await _repository.UpsertCircuitAsync(circuit, ct);  // Database write!
    circuitCount++;
}
```

**Impact:**
- If you have 100 circuits, this executes 100 MySQL `INSERT ... ON DUPLICATE KEY UPDATE` statements every 30 seconds
- 200 calls/minute, 12,000 calls/hour, 288,000 calls/day per 100 circuits
- With 1000+ circuits, this creates enormous unnecessary database load
- 99% of these writes are redundant (data hasn't changed)

## Solution

Implemented **dirty tracking** to only persist entities that have actually changed since the last database write.

### Changes Made

#### 1. Added Dirty Tracking to State Models

**CircuitState.cs**, **LinkState.cs**, **NodeState.cs**:
- Added `IsDirty` flag
- Added `MarkDirty()` and `MarkClean()` methods
- Modified all property setters to call `MarkDirty()` when values change

```csharp
private CircuitStatus _status = CircuitStatus.Active;
public CircuitStatus Status
{
    get => _status;
    set
    {
        if (_status != value)
        {
            _status = value;
            MarkDirty();  // Track the change!
        }
    }
}
```

#### 2. Updated NetworkStateService Interface

Added methods to query and manage dirty state:
```csharp
IEnumerable<NodeState> GetDirtyNodes();
IEnumerable<LinkState> GetDirtyLinks();
IEnumerable<CircuitState> GetDirtyCircuits();
void MarkNodeClean(NodeState node);
void MarkLinkClean(LinkState link);
void MarkCircuitClean(CircuitState circuit);
```

#### 3. Updated NetworkStatePersistenceService

**NEW CODE - Only persists changed entities:**
```csharp
// Persist only dirty circuits
var dirtyCircuits = _networkState.GetDirtyCircuits().ToList();
foreach (var circuit in dirtyCircuits)
{
    await _repository.UpsertCircuitAsync(circuit, ct);
    _networkState.MarkCircuitClean(circuit);  // Mark clean after successful persist
    circuitCount++;
}

if (circuitCount > 0)
{
    _logger.LogInformation(
        "Persisted {CircuitCount} dirty circuits in {ElapsedMs}ms",
        circuitCount, sw.ElapsedMilliseconds);
}
else
{
    _logger.LogDebug("No dirty entities to persist");
}
```

#### 4. Added Explicit MarkDirty() Calls

When modifying Endpoints dictionaries (which don't trigger property setters), we explicitly call `MarkDirty()`:

```csharp
circuit.Endpoints[evt.Node] = endpoint;
circuit.MarkDirty(); // Explicitly mark dirty when modifying Endpoints
```

## Performance Improvement

### Before (Example with 100 circuits)
- **Every 30 seconds**: 100 database writes
- **Per minute**: ~200 database writes
- **Per hour**: ~12,000 database writes
- **Per day**: ~288,000 database writes

### After (Example with 100 circuits, 5 changing per minute)
- **Every 30 seconds**: ~2-3 database writes (only changed circuits)
- **Per minute**: ~5 database writes
- **Per hour**: ~300 database writes
- **Per day**: ~7,200 database writes

**Result: ~97% reduction in database writes!**

## Benefits

1. **Dramatically reduced database load** - Only writes what has changed
2. **Better scalability** - Can handle many more circuits/links/nodes
3. **Improved performance** - Less database contention
4. **Lower network overhead** - Fewer database round trips
5. **Reduced I/O** - Less disk writes on MySQL server

## Logging Improvements

The service now logs differently based on activity:
- **When there are changes**: `LogInformation` with count of dirty entities persisted
- **When nothing changed**: `LogDebug` "No dirty entities to persist"

This makes it easy to monitor persistence activity without spam in the logs.

## Backward Compatibility

The changes are fully backward compatible:
- Existing API endpoints unchanged
- Database schema unchanged
- MQTT topics unchanged
- Initial state loading works as before

## Testing Recommendations

1. Monitor logs to confirm dirty tracking is working:
   ```
   Persisted 3 dirty nodes, 1 dirty links, 2 dirty circuits in 15ms
   ```

2. Verify database load reduction using MySQL slow query log or performance monitoring

3. Check that circuit/link/node updates still persist correctly by:
   - Creating a new circuit
   - Updating an existing circuit
   - Waiting 30+ seconds
   - Querying the database to confirm the changes were persisted

## Related Files Changed

- `node-api/Models/NetworkState/CircuitState.cs`
- `node-api/Models/NetworkState/LinkState.cs`
- `node-api/Models/NetworkState/NodeState.cs`
- `node-api/Services/NetworkStateService.cs`
- `node-api/Services/NetworkStatePersistenceService.cs`
- `node-api/Services/NetworkStateUpdater.cs`
