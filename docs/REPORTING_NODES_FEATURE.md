# Reporting Nodes Feature

**Date**: 2025-01-21  
**Status**: ? Implemented

## Overview

The **Reporting Nodes** feature distinguishes between:
- **Reporting Nodes**: Nodes that send UDP telemetry (NodeUpEvent or NodeStatus) to this API
- **Discovered Nodes**: Nodes only seen via events from other nodes (e.g., in L2Traces, LinkEvents)

This distinction is important for accurate metrics on the homepage, as not all nodes in the network send telemetry directly to this API.

## Problem

The homepage showed "750 Total Reporting Nodes", but this included ALL nodes in the database:
- Nodes that send UDP datagrams ? (actually reporting)
- Nodes mentioned in traces/links from other nodes ? (discovered, but not reporting)

This inflated the count and misrepresented how many nodes were **actively sending telemetry**.

## Solution

### 1. Database Schema Change

Added `is_reporting_node` boolean column to the `nodes` table:

```sql
ALTER TABLE `nodes` 
ADD COLUMN `is_reporting_node` BOOLEAN NOT NULL DEFAULT FALSE
AFTER `last_ip_update`;

CREATE INDEX `idx_is_reporting_node` ON `nodes` (`is_reporting_node`);
```

**Migration Script**: `schema/migrations/010_add_is_reporting_node.sql`

### 2. Model Update

Added `IsReportingNode` property to `NodeState`:

```csharp
// node-api/Models/NetworkState/NodeState.cs
public bool IsReportingNode
{
    get => _isReportingNode;
    set
    {
        if (_isReportingNode != value)
        {
            _isReportingNode = value;
            MarkDirty();
        }
    }
}
```

### 3. State Updater Logic

Set `IsReportingNode = true` when processing UDP telemetry events:

```csharp
// node-api/Services/NetworkStateUpdater.cs
public void UpdateFromNodeUpEvent(NodeUpEvent evt)
{
    var node = _networkState.GetOrCreateNode(evt.NodeCall);
    node.IsReportingNode = true; // ? Mark as reporting
    // ... rest of update
}

public void UpdateFromNodeStatus(NodeStatusReportEvent evt)
{
    var node = _networkState.GetOrCreateNode(evt.NodeCall);
    node.IsReportingNode = true; // ? Mark as reporting
    // ... rest of update
}
```

**Note**: Nodes discovered via L2Traces, LinkEvents, etc. will have `IsReportingNode = false`.

### 4. Repository Updates

Updated `MySqlNetworkStateRepository` to:
- Include `is_reporting_node` in INSERT/UPDATE statements
- Include `is_reporting_node` in SELECT queries
- Map `is_reporting_node` to `NodeState.IsReportingNode`

```csharp
// node-api/Services/MySqlNetworkStateRepository.cs
// ... updated UpsertNodeAsync, GetNodeAsync, GetAllNodesAsync
```

### 5. Persistence Service

Updated `NetworkStatePersistenceService.CopyNodeState()` to include:

```csharp
target.IsReportingNode = source.IsReportingNode;
```

### 6. New API Endpoint

Added `/api/network/nodes/reporting` endpoint:

```csharp
// node-api/Controllers/NodesController.cs
[HttpGet("reporting")]
public IActionResult GetReportingNodes()
{
    var nodes = _networkState.GetAllNodes()
        .Values
        .Where(n => n.IsReportingNode &&
                   !_networkState.IsTestCallsign(n.Callsign) &&
                   !_networkState.IsHiddenCallsign(n.Callsign));
    
    _logger.LogInformation("GetReportingNodes called, returning {Count} reporting nodes", nodes.Count());
    return Ok(nodes);
}
```

### 7. Frontend Update

Updated `index.html` to use the new endpoint:

```javascript
// node-api/wwwroot/index.html
async function loadInitialState() {
    const response = await fetch('/api/network/nodes/reporting'); // ? Changed from '/api/network/nodes'
    if (response.ok) {
        const nodes = await response.json();
        console.log(`Loaded ${nodes.length} reporting nodes from API`);
        // ...
    }
}
```

## API Endpoints

### `/api/network/nodes` (existing)
Returns **all nodes** (reporting + discovered)

**Use case**: Network topology analysis

### `/api/network/nodes/reporting` (new)
Returns **only reporting nodes** (those sending UDP telemetry)

**Use case**: Homepage statistics, monitoring active nodes

### `/api/network/nodes/{callsign}` (existing)
Returns a specific node (regardless of reporting status)

### `/api/network/nodes/base/{baseCallsign}` (existing)
Returns all SSIDs for a base callsign (regardless of reporting status)

## Database Migration

### Before Running

1. Backup database:
   ```bash
   mysqldump -u root -p node_api > backup_before_is_reporting_node.sql
   ```

2. Run migration:
   ```bash
   mysql -u root -p node_api < schema/migrations/010_add_is_reporting_node.sql
   ```

### Migration Notes

- **Default**: All existing nodes get `is_reporting_node = FALSE`
- **Auto-fix**: Migration marks nodes with recent `last_up_event` or `last_status_update` as reporting
- **Index**: Creates `idx_is_reporting_node` for efficient filtering

### Post-Migration

Nodes will be automatically marked as reporting when they send:
- `NodeUpEvent`
- `NodeStatus`

Nodes discovered only via:
- `L2Trace` (srce/dest fields)
- `LinkUpEvent`/`LinkStatus` (local/remote fields)
- `CircuitUpEvent`/`CircuitStatus` (local/remote fields)

...will remain `is_reporting_node = FALSE` until they send direct telemetry.

## Expected Behavior

### Homepage Statistics

**Before**:
```
750 Total Reporting Nodes  ? Incorrect (all nodes)
```

**After**:
```
42 Active Reporting Nodes   ? Only nodes sending telemetry in last 60s
87 Total Reporting Nodes    ? All nodes that have ever sent telemetry
```

### Node Badges

Only nodes with `is_reporting_node = TRUE` appear in the "Reporting Nodes" section.

## Testing

### Manual Testing

1. Deploy with new code and run migration
2. Visit homepage - verify "Total Reporting Nodes" is lower than before
3. Wait for a node to send `NodeUpEvent` or `NodeStatus`
4. Verify it appears in the reporting nodes list
5. Check database:
   ```sql
   SELECT 
       COUNT(*) AS total_nodes,
       SUM(is_reporting_node) AS reporting_nodes,
       SUM(NOT is_reporting_node) AS discovered_only_nodes
   FROM `nodes`;
   ```

### Automated Testing

Add tests to verify:
- ? `UpdateFromNodeUpEvent` sets `IsReportingNode = true`
- ? `UpdateFromNodeStatus` sets `IsReportingNode = true`
- ? Nodes discovered via traces remain `IsReportingNode = false`
- ? `/api/network/nodes/reporting` filters correctly

## Rollback Plan

If issues occur:

1. **Revert frontend** to use old endpoint:
   ```javascript
   // Change back to:
   const response = await fetch('/api/network/nodes');
   ```

2. **Revert database** (optional):
   ```sql
   ALTER TABLE `nodes` DROP COLUMN `is_reporting_node`;
   DROP INDEX `idx_is_reporting_node` ON `nodes`;
   ```

3. **Revert code commits**

## Performance Impact

- **Minimal**: Boolean column is lightweight
- **Index**: `idx_is_reporting_node` added for fast filtering
- **Memory**: No significant change
- **Disk**: +1 byte per node row

## Related Files

**Model**:
- `node-api/Models/NetworkState/NodeState.cs`

**Services**:
- `node-api/Services/NetworkStateUpdater.cs`
- `node-api/Services/MySqlNetworkStateRepository.cs`
- `node-api/Services/NetworkStatePersistenceService.cs`

**Controllers**:
- `node-api/Controllers/NodesController.cs`

**Frontend**:
- `node-api/wwwroot/index.html`

**Database**:
- `schema/migrations/010_add_is_reporting_node.sql`

**Documentation**:
- `docs/REPORTING_NODES_FEATURE.md` (this file)

## Future Enhancements

1. **Dashboard**: Add "Discovered-only Nodes" section
2. **API**: Add query parameter `?reporting={true|false|all}` to `/api/network/nodes`
3. **Metrics**: Track ratio of reporting vs discovered nodes over time
4. **Alert**: Notify when a previously-reporting node stops sending telemetry

---

**Status**: ? **Ready for Deployment**

**Migration Required**: Yes - Run `schema/migrations/010_add_is_reporting_node.sql`
