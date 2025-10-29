# Database Query Frequency Diagnostics

This feature tracks database query frequency to help diagnose increasing database traffic and determine if it's natural growth or a potential bug.

## Endpoint

```
GET /api/diagnostics/db/query-frequency
```

## Response Format

Returns a JSON array of query statistics, sorted by total count (descending):

```json
[
  {
    "methodName": "GetTracesAsync",
    "queryText": "SELECT `id`, `timestamp`, `json` as report FROM `traces` WHERE 1=1 AND `reportFrom_idx` NOT REGEXP @testPattern...",
    "totalCount": 1523,
    "lastSeen": "2025-10-29T11:45:23.123Z",
    "hourlyData": [
      {
        "hour": "2025-10-29T11:00:00Z",
        "count": 1523
      }
    ]
  },
  {
    "methodName": "GetTotalCountAsync",
    "queryText": "SELECT COUNT(*) FROM `traces` WHERE 1=1 AND `reportFrom_idx` NOT REGEXP @testPattern",
    "totalCount": 45,
    "lastSeen": "2025-10-29T11:43:12.456Z",
    "hourlyData": [
      {
        "hour": "2025-10-29T11:00:00Z",
        "count": 45
      }
    ]
  }
]
```

## Fields

- **methodName**: The repository method that executed the query (e.g., "GetTracesAsync", "UpsertNodeAsync")
- **queryText**: The sanitized SQL query text (newlines removed, first 100 chars used for grouping)
- **totalCount**: Total number of times this query has been executed (since server start or last 24 hours)
- **lastSeen**: UTC timestamp of when this query was last executed
- **hourlyData**: Array of hourly counts for the last 24 hours
  - **hour**: UTC timestamp truncated to the hour (YYYY-MM-DDTHH:00:00Z)
  - **count**: Number of queries in that hour

## Usage Examples

### Check current query frequency

```bash
curl http://localhost:5000/api/diagnostics/db/query-frequency
```

### Find the most frequently called queries

The results are sorted by `totalCount` descending, so the first items are the most frequently executed queries.

### Monitor query patterns over time

Use the `hourlyData` to see if query frequency is increasing linearly, exponentially, or staying constant.

### Identify potential issues

Look for:
- Queries with very high `totalCount` relative to expected traffic
- Linear or exponential growth in `hourlyData` counts
- Unexpected queries being called frequently
- COUNT queries that might indicate inefficient pagination

## Implementation Details

- Query tracking is enabled for all repository methods that use `QueryLogger`
- Data is retained for the last 24 hours only (older data is automatically cleaned up)
- The tracker uses `CallerMemberName` to automatically capture the calling method name
- SQL queries are sanitized (newlines and special characters removed) before tracking
- Queries are grouped by method name + first 100 characters of the SQL
- All timestamps are in UTC

## Performance Impact

The tracking overhead is minimal:
- Uses `ConcurrentDictionary` for thread-safe updates
- In-memory only (no database writes for tracking)
- Automatic cleanup runs once per hour
- Query sanitization happens at execution time (already done by `QueryLogger`)
