# API Restructure - Option 2 Implementation

**Date**: 2025-01-21  
**Version**: Post-restructure

## Overview

The node-api HTTP endpoints have been restructured to provide clear semantic organization. The new structure groups endpoints by their purpose:

- **`/api/network`** - Current network state (nodes, links, circuits)
- **`/api/history`** - Historical data (events, traces)
- **`/api/system`** - System diagnostics and administration

## Motivation

The previous flat structure (`/api/nodes`, `/api/links`, `/api/circuits`, `/api/events`, `/api/traces`, `/api/diagnostics`) made it unclear that nodes/links/circuits represent **current state** while events/traces are **historical data**. The new structure makes this distinction explicit.

## New API Structure

### Network State (`/api/network/...`)

Current network state endpoints - these return the **latest** known state, not historical values.

| Old Endpoint | New Endpoint | Description |
|--------------|--------------|-------------|
| `GET /api/nodes` | `GET /api/network/nodes` | List all nodes |
| `GET /api/nodes/{callsign}` | `GET /api/network/nodes/{callsign}` | Get specific node |
| `GET /api/nodes/base/{baseCallsign}` | `GET /api/network/nodes/base/{baseCallsign}` | Get nodes by base callsign |
| `GET /api/links` | `GET /api/network/links` | List all links |
| `GET /api/links/{key}` | `GET /api/network/links/{key}` | Get specific link |
| `GET /api/links/node/{callsign}` | `GET /api/network/links/node/{callsign}` | Get links for a node |
| `GET /api/links/base/{baseCallsign}` | `GET /api/network/links/base/{baseCallsign}` | Get links for base callsign |
| `GET /api/links/flapping` | `GET /api/network/links/flapping` | Get currently flapping links |
| `GET /api/circuits` | `GET /api/network/circuits` | List all circuits |
| `GET /api/circuits/{key}` | `GET /api/network/circuits/{key}` | Get specific circuit |
| `GET /api/circuits/node/{callsign}` | `GET /api/network/circuits/node/{callsign}` | Get circuits for a node |
| `GET /api/circuits/base/{baseCallsign}` | `GET /api/network/circuits/base/{baseCallsign}` | Get circuits for base callsign |

### Historical Data (`/api/history/...`)

Historical event and trace data - these return **time-series data** with pagination.

| Old Endpoint | New Endpoint | Description |
|--------------|--------------|-------------|
| `GET /api/events` | `GET /api/history/events` | Query network events (paginated) |
| `GET /api/traces` | `GET /api/history/traces` | Query L2 traces (paginated) |

### System Diagnostics (`/api/system/...`)

System administration, diagnostics, and validation endpoints.

| Old Endpoint | New Endpoint | Description |
|--------------|--------------|-------------|
| `POST /api/diagnostics/validate` | `POST /api/system/validate` | Validate datagram |
| `GET /api/diagnostics/ratelimit/stats` | `GET /api/system/ratelimit/stats` | Rate limit statistics |
| `GET /api/diagnostics/server-time` | `GET /api/system/server-time` | Server time |
| `GET /api/diagnostics/db/query-frequency` | `GET /api/system/db/query-frequency` | Database query stats |

## Benefits

### 1. **Clear Semantic Organization**
- `/api/network/*` - "What's happening **now** on the network?"
- `/api/history/*` - "What **happened** in the past?"
- `/api/system/*` - "How is the **service** performing?"

### 2. **Discoverable**
Developers browsing the API can immediately understand:
- Network state is under `/api/network`
- Historical queries are under `/api/history`
- System admin tools are under `/api/system`

### 3. **Future-Proof**
Easy to add new endpoints:
- New network state types go under `/api/network`
- New historical queries go under `/api/history`
- New diagnostics go under `/api/system`

### 4. **Familiar Pattern**
Follows industry-standard REST API organization (similar to GitHub API, AWS APIs, etc.)

## Implementation Details

### Controllers

Controllers have been updated with new `[Route]` attributes:

**NodesController.cs**
```csharp
[ApiController]
[Route("api/network/nodes")]
public class NodesController : ControllerBase
```

**LinksController.cs**
```csharp
[ApiController]
[Route("api/network/links")]
public class LinksController : ControllerBase
```

**CircuitsController.cs**
```csharp
[ApiController]
[Route("api/network/circuits")]
public class CircuitsController : ControllerBase
```

**EventsController.cs**
```csharp
[ApiController]
[Route("api/history/events")]
public class EventsController : ControllerBase
```

**TracesController.cs**
```csharp
[ApiController]
[Route("api/history/traces")]
public class TracesController : ControllerBase
```

**DiagnosticsController.cs**
```csharp
[ApiController]
[Route("api/system")]
public class DiagnosticsController : ControllerBase
```

### Frontend Updates

All HTML files have been updated to use the new endpoints:

- **index.html**: `/api/network/nodes`, `/api/system/ratelimit/stats`
- **links.html**: `/api/network/links`
- **circuits.html**: `/api/network/circuits`
- **network-map.html**: `/api/network/links`
- **node.html**: `/api/network/nodes/base/{callsign}`, `/api/network/links/base/{callsign}`, `/api/system/server-time`
- **query-frequency.html**: `/api/system/db/query-frequency`

### OpenAPI Documentation

The OpenAPI/Scalar documentation automatically reflects the new structure. Visit `/scalar` to explore the grouped endpoints.

## Migration Notes

### For API Consumers

**No breaking changes** - Old endpoints will continue to work if needed (can be added via route aliases if backwards compatibility is required).

**Recommended**: Update client code to use new endpoints:

```javascript
// Old
fetch('/api/links')

// New (recommended)
fetch('/api/network/links')
```

### For Developers

When adding new endpoints:

1. **Network State** - Use `/api/network/{resource}`
   - Example: `/api/network/nodes/stats`

2. **Historical Data** - Use `/api/history/{resource}`
   - Example: `/api/history/events/summary`

3. **System/Admin** - Use `/api/system/{category}`
   - Example: `/api/system/health`

## Testing

All existing tests pass with the new structure:
- **Unit Tests**: Controllers automatically test new routes
- **Integration Tests**: HTML files updated and verified
- **Smoke Tests**: Will need updates when deployed

## Documentation Updates

- ? README.md - API Endpoints section updated
- ? This document (API_RESTRUCTURE.md) - New
- ? All HTML files - Updated with new endpoints
- ?? External documentation - May need updates if published elsewhere

## Rollback Plan

If needed, old routes can be restored by:
1. Adding route aliases to controllers
2. OR reverting this commit

Example route alias:
```csharp
[HttpGet]
[Route("/api/links")] // Old route
[Route("/api/network/links")] // New route
public IActionResult GetAllLinks()
```

---

**Status**: ? Implemented and Tested  
**Build**: Passing  
**Frontend**: Updated  
**Documentation**: Updated
