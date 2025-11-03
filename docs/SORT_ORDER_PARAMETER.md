# Sort Order Parameter

## Overview

The `/api/history/traces` and `/api/history/events` endpoints support an optional `sortOrder` query parameter that controls the ordering of results by timestamp.

## Parameter

**Name**: `sortOrder`  
**Type**: `string`  
**Default**: `"asc"` (ascending order - oldest first)  
**Valid Values**: 
- `"asc"` - Ascending order (oldest to newest)
- `"desc"` - Descending order (newest to oldest)

Invalid values will default to ascending order.

**Discoverability**: This parameter is documented in the OpenAPI/Scalar documentation at `/scalar`. The API will return a validation error if you provide an invalid value.

## Behavior

### Ascending Order (`sortOrder=asc`)
- Results are ordered from oldest to newest timestamp
- Useful for:
  - Following chronological progression of events
  - Analyzing event sequences in order
  - Replay scenarios
  - Time-series analysis starting from a specific point

### Descending Order (`sortOrder=desc`)
- Results are ordered from newest to oldest timestamp
- Useful for:
  - Viewing most recent events first
  - Real-time monitoring dashboards
  - "What happened most recently?" queries
  - Quick access to latest activity

## Examples

### Traces - Ascending Order (Default)
```bash
# Get oldest traces first
curl "https://api.example.com/api/history/traces?reportFrom=G8PZT&limit=10"

# Explicit ascending order
curl "https://api.example.com/api/history/traces?reportFrom=G8PZT&sortOrder=asc&limit=10"
```

### Traces - Descending Order
```bash
# Get newest traces first
curl "https://api.example.com/api/history/traces?reportFrom=G8PZT&sortOrder=desc&limit=10"
```

### Events - Ascending Order
```bash
# Get events in chronological order
curl "https://api.example.com/api/history/events?node=G8PZT&sortOrder=asc&limit=10"
```

### Events - Descending Order
```bash
# Get most recent events first
curl "https://api.example.com/api/history/events?node=G8PZT&sortOrder=desc&limit=10"
```

### With Date Range
```bash
# Get events from last week, oldest first
curl "https://api.example.com/api/history/events?\
node=G8PZT&\
from=2025-01-14T00:00:00Z&\
to=2025-01-21T00:00:00Z&\
sortOrder=asc&\
limit=50"

# Get events from last week, newest first
curl "https://api.example.com/api/history/events?\
node=G8PZT&\
from=2025-01-14T00:00:00Z&\
to=2025-01-21T00:00:00Z&\
sortOrder=desc&\
limit=50"
```

## JavaScript Examples

### Using URLSearchParams
```javascript
// Ascending order (default)
const params = new URLSearchParams({
  reportFrom: 'G8PZT',
  limit: '10',
  sortOrder: 'asc'
});

const response = await fetch(`/api/history/traces?${params}`);
const data = await response.json();

// Descending order
const paramsDesc = new URLSearchParams({
  reportFrom: 'G8PZT',
  limit: '10',
  sortOrder: 'desc'
});

const responseDesc = await fetch(`/api/history/traces?${paramsDesc}`);
const dataDesc = await responseDesc.json();
```

### TypeScript Helper Function
```typescript
async function getTraces(
  callsign: string,
  options: {
    limit?: number;
    sortOrder?: 'asc' | 'desc';
    from?: Date;
    to?: Date;
  } = {}
): Promise<PagedResult> {
  const params = new URLSearchParams({
    reportFrom: callsign,
    limit: (options.limit || 10).toString(),
    sortOrder: options.sortOrder || 'asc'
  });

  if (options.from) {
    params.append('from', options.from.toISOString());
  }
  if (options.to) {
    params.append('to', options.to.toISOString());
  }

  const response = await fetch(`/api/history/traces?${params}`);
  return await response.json();
}

// Usage - get oldest traces
const oldestFirst = await getTraces('G8PZT', { sortOrder: 'asc', limit: 50 });

// Usage - get newest traces
const newestFirst = await getTraces('G8PZT', { sortOrder: 'desc', limit: 50 });
```

## Python Examples

```python
import requests

# Ascending order (oldest first)
response = requests.get(
    'https://api.example.com/api/history/traces',
    params={
        'reportFrom': 'G8PZT',
        'limit': 10,
        'sortOrder': 'asc'
    }
)
traces_asc = response.json()

# Descending order (newest first)
response = requests.get(
    'https://api.example.com/api/history/traces',
    params={
        'reportFrom': 'G8PZT',
        'limit': 10,
        'sortOrder': 'desc'
    }
)
traces_desc = response.json()
```

## C# Examples

```csharp
using System.Net.Http;
using System.Web;

public async Task<PagedResult> GetTracesAsync(
    string callsign,
    int limit = 10,
    string sortOrder = "asc")
{
    var query = HttpUtility.ParseQueryString(string.Empty);
    query.Add("reportFrom", callsign);
    query.Add("limit", limit.ToString());
    query.Add("sortOrder", sortOrder);
    
    var url = $"/api/history/traces?{query}";
    
    var response = await _httpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();
    
    return await response.Content.ReadFromJsonAsync<PagedResult>();
}

// Usage - get oldest first
var oldestFirst = await GetTracesAsync("G8PZT", sortOrder: "asc");

// Usage - get newest first
var newestFirst = await GetTracesAsync("G8PZT", sortOrder: "desc");
```

## Pagination Behavior

The `sortOrder` parameter affects how pagination works:

### Ascending Order (Default)
- First page contains the **oldest** records matching the query
- Each subsequent page (via `cursor`) contains **progressively newer** records
- Pagination moves **forward in time**

### Descending Order
- First page contains the **newest** records matching the query
- Each subsequent page (via `cursor`) contains **progressively older** records
- Pagination moves **backward in time**

### Example: Paginating Through Results

```javascript
// Ascending - walk forward through time
async function fetchAllTracesOldestFirst(callsign) {
  const allTraces = [];
  let cursor = null;
  
  do {
    const params = new URLSearchParams({
      reportFrom: callsign,
      limit: '100',
      sortOrder: 'asc'
    });
    if (cursor) params.append('cursor', cursor);
    
    const response = await fetch(`/api/history/traces?${params}`);
    const data = await response.json();
    
    allTraces.push(...data.data);
    cursor = data.page.next;
  } while (cursor);
  
  return allTraces; // Ordered oldest to newest
}

// Descending - walk backward through time
async function fetchRecentTraces(callsign, maxPages = 5) {
  const traces = [];
  let cursor = null;
  let pageCount = 0;
  
  do {
    const params = new URLSearchParams({
      reportFrom: callsign,
      limit: '100',
      sortOrder: 'desc'
    });
    if (cursor) params.append('cursor', cursor);
    
    const response = await fetch(`/api/history/traces?${params}`);
    const data = await response.json();
    
    traces.push(...data.data);
    cursor = data.page.next;
    pageCount++;
  } while (cursor && pageCount < maxPages);
  
  return traces; // Ordered newest to oldest
}
```

## Use Cases

### Ascending Order Use Cases

1. **Event Replay**: Replaying events in the order they occurred
   ```javascript
   const events = await getEvents('G8PZT', { 
     sortOrder: 'asc',
     from: new Date('2025-01-20T00:00:00Z')
   });
   // Process events chronologically
   ```

2. **Time-Series Analysis**: Analyzing trends over time
   ```javascript
   const traces = await getTraces('G8PZT', { 
     sortOrder: 'asc',
     from: startDate,
     to: endDate
   });
   // Calculate statistics in chronological order
   ```

3. **Data Export**: Exporting complete history in chronological order
   ```javascript
   const allData = await fetchAllTracesOldestFirst('G8PZT');
   // Export to CSV in chronological order
   ```

### Descending Order Use Cases

1. **Real-Time Monitoring**: Showing most recent activity
   ```javascript
   const recentTraces = await getTraces('G8PZT', { 
     sortOrder: 'desc',
     limit: 20
   });
   // Display in a dashboard
   ```

2. **Recent History View**: "What happened recently?"
   ```javascript
   const last24Hours = await getEvents('G8PZT', {
     sortOrder: 'desc',
     from: new Date(Date.now() - 24 * 60 * 60 * 1000)
   });
   // Show newest events at top
   ```

3. **Debugging Recent Issues**: Finding latest error events
   ```javascript
   const recentErrors = await getEvents('G8PZT', {
     sortOrder: 'desc',
     type: 'LinkDownEvent',
     limit: 10
   });
   // Investigate most recent link failures
   ```

## Performance Considerations

### Database Indexes
Both ascending and descending queries use the same database indexes efficiently:
- `traces` table: `(timestamp, id)` index
- `events` table: `(timestamp, id)` index

### Query Performance
- **Ascending queries**: Typically slightly faster as they align with natural index order
- **Descending queries**: May require index scan reversal but still efficient
- Both orders support efficient keyset pagination

### Recommendations
- Use ascending order when processing complete historical datasets
- Use descending order for monitoring/dashboard views
- Both are equally suitable for pagination
- No significant performance difference for most queries

## Combining with Other Parameters

The `sortOrder` parameter works seamlessly with all other query parameters:

```bash
# Complex query with multiple filters and sort order
curl "https://api.example.com/api/history/traces?\
reportFrom=G8PZT&\
reportFrom=M0LTE&\
source=G8PZT-1&\
dest=M0LTE-1&\
type=UI&\
from=2025-01-01T00:00:00Z&\
to=2025-01-21T23:59:59Z&\
sortOrder=desc&\
limit=50&\
includeCount=true"
```

## API Response

The response format is identical regardless of `sortOrder`:

```json
{
  "page": {
    "limit": 10,
    "next": "base64_encoded_cursor",
    "totalCount": 1523
  },
  "data": [
    {
      "id": 12345,
      "timestamp": "2025-01-20T10:30:00.123Z",
      "report": { /* L2Trace JSON */ }
    },
    // ... more results in specified order
  ]
}
```

The only difference is the ordering of items in the `data` array.

## Migration Notes

### Breaking Changes
**None** - This is a backward-compatible addition.

### Default Behavior Change
Previously, the API returned results in **descending** order (newest first) with no option to change it.

Now:
- **Default is ascending** (oldest first) 
- Add `sortOrder=desc` to maintain previous behavior

### Updating Client Code

If your client code expects newest-first ordering, update your requests:

```javascript
// Before (relied on implicit DESC order)
const response = await fetch('/api/history/traces?reportFrom=G8PZT&limit=10');

// After (explicit DESC order to maintain previous behavior)
const response = await fetch('/api/history/traces?reportFrom=G8PZT&limit=10&sortOrder=desc');
```

## See Also

- [API Restructure Documentation](API_RESTRUCTURE.md)
- [Multiple ReportFrom Feature](MULTIPLE_REPORTFROM_FEATURE.md)
- [Pagination Guide](PAGINATION.md)
- OpenAPI Documentation: `/scalar`
