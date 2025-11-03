# Multiple ReportFrom Callsigns - Usage Examples

This document provides practical examples for using the multiple `reportFrom` callsigns feature in various programming languages and tools.

## Table of Contents

- [cURL](#curl)
- [JavaScript/TypeScript](#javascripttypescript)
- [Python](#python)
- [C#](#c)
- [PowerShell](#powershell)
- [HTTP/REST Clients](#httprest-clients)

---

## cURL

### Single Callsign

```bash
curl "https://api.example.com/api/history/traces?reportFrom=G8PZT&limit=10"
```

### Multiple Callsigns

```bash
curl "https://api.example.com/api/history/traces?reportFrom=G8PZT&reportFrom=M0LTE&reportFrom=G8ABC&limit=10"
```

### With Date Range

```bash
curl "https://api.example.com/api/history/traces?reportFrom=G8PZT&reportFrom=M0LTE&from=2025-01-01T00:00:00Z&to=2025-01-21T23:59:59Z&limit=10"
```

### With All Filters

```bash
curl "https://api.example.com/api/history/traces?\
reportFrom=G8PZT&\
reportFrom=M0LTE&\
source=G8PZT-1&\
dest=M0LTE-1&\
type=UI&\
from=2025-01-01T00:00:00Z&\
to=2025-01-21T23:59:59Z&\
limit=50&\
includeCount=true"
```

---

## JavaScript/TypeScript

### Using URLSearchParams (Recommended)

```javascript
// Build URL with multiple reportFrom callsigns
const callsigns = ['G8PZT', 'M0LTE', 'G8ABC'];
const params = new URLSearchParams();

callsigns.forEach(cs => params.append('reportFrom', cs));
params.append('limit', '10');

const url = `/api/history/traces?${params.toString()}`;
const response = await fetch(url);
const data = await response.json();

console.log(`Found ${data.data.length} traces`);
```

### With Additional Filters

```javascript
const callsigns = ['G8PZT', 'M0LTE'];
const params = new URLSearchParams();

callsigns.forEach(cs => params.append('reportFrom', cs));
params.append('source', 'G8PZT-1');
params.append('type', 'UI');
params.append('limit', '50');
params.append('includeCount', 'true');

const response = await fetch(`/api/history/traces?${params}`);
const data = await response.json();

console.log(`Total count: ${data.page.totalCount}`);
console.log(`Returned: ${data.data.length} traces`);
```

### TypeScript with Type Safety

```typescript
interface TraceDto {
  id: number;
  timestamp: string;
  report: any;
}

interface PageInfo {
  limit: number;
  next?: string;
  totalCount?: number;
}

interface PagedResult {
  page: PageInfo;
  data: TraceDto[];
}

async function getTraces(callsigns: string[], limit: number = 10): Promise<PagedResult> {
  const params = new URLSearchParams();
  
  callsigns.forEach(cs => params.append('reportFrom', cs));
  params.append('limit', limit.toString());
  
  const response = await fetch(`/api/history/traces?${params}`);
  
  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }
  
  return await response.json();
}

// Usage
const traces = await getTraces(['G8PZT', 'M0LTE', 'G8ABC'], 50);
console.log(`Retrieved ${traces.data.length} traces`);
```

### React Hook Example

```typescript
import { useState, useEffect } from 'react';

function useTraces(callsigns: string[], limit: number = 10) {
  const [data, setData] = useState<PagedResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    const fetchTraces = async () => {
      try {
        setLoading(true);
        const params = new URLSearchParams();
        callsigns.forEach(cs => params.append('reportFrom', cs));
        params.append('limit', limit.toString());
        
        const response = await fetch(`/api/history/traces?${params}`);
        const result = await response.json();
        setData(result);
      } catch (err) {
        setError(err as Error);
      } finally {
        setLoading(false);
      }
    };

    fetchTraces();
  }, [callsigns.join(','), limit]);

  return { data, loading, error };
}

// Usage in component
function TracesView() {
  const { data, loading, error } = useTraces(['G8PZT', 'M0LTE']);
  
  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error.message}</div>;
  
  return (
    <div>
      <h2>Traces from G8PZT and M0LTE</h2>
      <p>Found {data?.data.length} traces</p>
      {/* Render traces */}
    </div>
  );
}
```

---

## Python

### Using requests Library

```python
import requests
from datetime import datetime, timedelta

# Single callsign
response = requests.get(
    'https://api.example.com/api/history/traces',
    params={'reportFrom': 'G8PZT', 'limit': 10}
)
data = response.json()

# Multiple callsigns
callsigns = ['G8PZT', 'M0LTE', 'G8ABC']
response = requests.get(
    'https://api.example.com/api/history/traces',
    params=[('reportFrom', cs) for cs in callsigns] + [('limit', 10)]
)
data = response.json()

print(f"Found {len(data['data'])} traces")
```

### With Date Range

```python
from datetime import datetime, timedelta
import requests

callsigns = ['G8PZT', 'M0LTE']
from_date = (datetime.utcnow() - timedelta(days=7)).isoformat() + 'Z'
to_date = datetime.utcnow().isoformat() + 'Z'

params = [
    ('reportFrom', cs) for cs in callsigns
] + [
    ('from', from_date),
    ('to', to_date),
    ('limit', 50),
    ('includeCount', 'true')
]

response = requests.get(
    'https://api.example.com/api/history/traces',
    params=params
)

data = response.json()
print(f"Total count: {data['page'].get('totalCount', 'unknown')}")
print(f"Retrieved: {len(data['data'])} traces")
```

### Helper Function

```python
from typing import List, Optional
from datetime import datetime
import requests

def get_traces(
    callsigns: List[str],
    limit: int = 10,
    source: Optional[str] = None,
    dest: Optional[str] = None,
    from_date: Optional[datetime] = None,
    to_date: Optional[datetime] = None,
    include_count: bool = False
) -> dict:
    """
    Fetch traces from multiple reporting stations.
    
    Args:
        callsigns: List of reporter callsigns to filter by
        limit: Maximum number of traces to return (default: 10)
        source: Optional source callsign filter
        dest: Optional destination callsign filter
        from_date: Optional start date for filtering
        to_date: Optional end date for filtering
        include_count: Whether to include total count (default: False)
    
    Returns:
        Dict containing 'page' and 'data' keys
    """
    params = [('reportFrom', cs) for cs in callsigns]
    params.append(('limit', limit))
    
    if source:
        params.append(('source', source))
    if dest:
        params.append(('dest', dest))
    if from_date:
        params.append(('from', from_date.isoformat() + 'Z'))
    if to_date:
        params.append(('to', to_date.isoformat() + 'Z'))
    if include_count:
        params.append(('includeCount', 'true'))
    
    response = requests.get(
        'https://api.example.com/api/history/traces',
        params=params
    )
    response.raise_for_status()
    return response.json()

# Usage
traces = get_traces(
    callsigns=['G8PZT', 'M0LTE', 'G8ABC'],
    limit=50,
    source='G8PZT-1',
    include_count=True
)

print(f"Found {len(traces['data'])} traces")
```

---

## C#

### Using HttpClient

```csharp
using System.Net.Http;
using System.Text.Json;

public class TracesApiClient
{
    private readonly HttpClient _httpClient;
    
    public TracesApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<PagedResult> GetTracesAsync(
        string[] callsigns,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        
        foreach (var callsign in callsigns)
        {
            queryParams.Add($"reportFrom={Uri.EscapeDataString(callsign)}");
        }
        queryParams.Add($"limit={limit}");
        
        var url = $"/api/history/traces?{string.Join("&", queryParams)}";
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<PagedResult>(json)!;
    }
}

// Usage
var client = new TracesApiClient(httpClient);
var result = await client.GetTracesAsync(
    new[] { "G8PZT", "M0LTE", "G8ABC" },
    limit: 50
);

Console.WriteLine($"Found {result.Data.Count} traces");
```

### With Query String Builder

```csharp
using System.Web;

public static class TracesQueryBuilder
{
    public static string BuildTracesUrl(
        string[] callsigns,
        int limit = 10,
        string? source = null,
        string? dest = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        bool includeCount = false)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        
        foreach (var callsign in callsigns)
        {
            query.Add("reportFrom", callsign);
        }
        
        query.Add("limit", limit.ToString());
        
        if (source != null)
            query.Add("source", source);
        if (dest != null)
            query.Add("dest", dest);
        if (from.HasValue)
            query.Add("from", from.Value.ToString("O"));
        if (to.HasValue)
            query.Add("to", to.Value.ToString("O"));
        if (includeCount)
            query.Add("includeCount", "true");
        
        return $"/api/history/traces?{query}";
    }
}

// Usage
var url = TracesQueryBuilder.BuildTracesUrl(
    callsigns: new[] { "G8PZT", "M0LTE" },
    limit: 50,
    source: "G8PZT-1",
    from: DateTimeOffset.UtcNow.AddDays(-7),
    includeCount: true
);

var response = await httpClient.GetAsync(url);
```

### Strongly Typed Model

```csharp
public record TraceDto(long Id, DateTime Timestamp, JsonElement Report);

public record PageInfo(int Limit, string? Next, long? TotalCount);

public record PagedResult(PageInfo Page, IReadOnlyList<TraceDto> Data);
```

---

## PowerShell

### Basic Query

```powershell
# Single callsign
$response = Invoke-RestMethod -Uri "https://api.example.com/api/history/traces?reportFrom=G8PZT&limit=10"
Write-Host "Found $($response.data.Count) traces"

# Multiple callsigns
$callsigns = @('G8PZT', 'M0LTE', 'G8ABC')
$params = $callsigns | ForEach-Object { "reportFrom=$_" }
$params += "limit=10"
$queryString = $params -join '&'

$response = Invoke-RestMethod -Uri "https://api.example.com/api/history/traces?$queryString"
Write-Host "Found $($response.data.Count) traces"
```

### With Parameters Object

```powershell
function Get-Traces {
    param(
        [Parameter(Mandatory=$true)]
        [string[]]$Callsigns,
        
        [int]$Limit = 10,
        [string]$Source,
        [string]$Dest,
        [datetime]$From,
        [datetime]$To,
        [switch]$IncludeCount
    )
    
    $queryParams = @()
    
    foreach ($callsign in $Callsigns) {
        $queryParams += "reportFrom=$([uri]::EscapeDataString($callsign))"
    }
    
    $queryParams += "limit=$Limit"
    
    if ($Source) { $queryParams += "source=$([uri]::EscapeDataString($Source))" }
    if ($Dest) { $queryParams += "dest=$([uri]::EscapeDataString($Dest))" }
    if ($From) { $queryParams += "from=$($From.ToString('o'))" }
    if ($To) { $queryParams += "to=$($To.ToString('o'))" }
    if ($IncludeCount) { $queryParams += "includeCount=true" }
    
    $queryString = $queryParams -join '&'
    $uri = "https://api.example.com/api/history/traces?$queryString"
    
    Invoke-RestMethod -Uri $uri
}

# Usage
$traces = Get-Traces -Callsigns 'G8PZT','M0LTE' -Limit 50 -IncludeCount
Write-Host "Total count: $($traces.page.totalCount)"
Write-Host "Retrieved: $($traces.data.Count) traces"
```

---

## HTTP/REST Clients

### Postman

**GET** `https://api.example.com/api/history/traces`

**Query Params:**
```
reportFrom: G8PZT
reportFrom: M0LTE
reportFrom: G8ABC
limit: 10
includeCount: true
```

### HTTPie

```bash
# Multiple callsigns
http GET https://api.example.com/api/history/traces \
  reportFrom==G8PZT \
  reportFrom==M0LTE \
  reportFrom==G8ABC \
  limit==10

# With filters
http GET https://api.example.com/api/history/traces \
  reportFrom==G8PZT \
  reportFrom==M0LTE \
  source==G8PZT-1 \
  type==UI \
  limit==50 \
  includeCount==true
```

### REST Client (VS Code Extension)

```http
### Get traces from multiple reporters
GET https://api.example.com/api/history/traces?reportFrom=G8PZT&reportFrom=M0LTE&limit=10

### With all filters
GET https://api.example.com/api/history/traces
  ?reportFrom=G8PZT
  &reportFrom=M0LTE
  &source=G8PZT-1
  &dest=M0LTE-1
  &type=UI
  &from=2025-01-01T00:00:00Z
  &to=2025-01-21T23:59:59Z
  &limit=50
  &includeCount=true
```

---

## Common Patterns

### Pagination with Multiple Callsigns

```javascript
async function fetchAllTraces(callsigns) {
  const allTraces = [];
  let cursor = null;
  
  do {
    const params = new URLSearchParams();
    callsigns.forEach(cs => params.append('reportFrom', cs));
    params.append('limit', '100');
    if (cursor) params.append('cursor', cursor);
    
    const response = await fetch(`/api/history/traces?${params}`);
    const data = await response.json();
    
    allTraces.push(...data.data);
    cursor = data.page.next;
  } while (cursor);
  
  return allTraces;
}
```

### Error Handling

```typescript
async function getTracesWithErrorHandling(callsigns: string[]) {
  try {
    const params = new URLSearchParams();
    callsigns.forEach(cs => params.append('reportFrom', cs));
    params.append('limit', '10');
    
    const response = await fetch(`/api/history/traces?${params}`);
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    
    return await response.json();
  } catch (error) {
    console.error('Failed to fetch traces:', error);
    throw error;
  }
}
```

### Rate Limiting

```javascript
class TracesClient {
  constructor(baseUrl, maxRequestsPerSecond = 10) {
    this.baseUrl = baseUrl;
    this.minInterval = 1000 / maxRequestsPerSecond;
    this.lastRequestTime = 0;
  }
  
  async getTraces(callsigns, options = {}) {
    // Rate limiting
    const now = Date.now();
    const timeSinceLastRequest = now - this.lastRequestTime;
    if (timeSinceLastRequest < this.minInterval) {
      await new Promise(resolve => 
        setTimeout(resolve, this.minInterval - timeSinceLastRequest)
      );
    }
    
    const params = new URLSearchParams();
    callsigns.forEach(cs => params.append('reportFrom', cs));
    Object.entries(options).forEach(([key, value]) => {
      params.append(key, String(value));
    });
    
    this.lastRequestTime = Date.now();
    const response = await fetch(`${this.baseUrl}/api/history/traces?${params}`);
    return await response.json();
  }
}
```

---

## Best Practices

1. **Always escape callsigns** when building URLs manually
2. **Use URLSearchParams** or equivalent library for query string building
3. **Handle pagination** for large result sets
4. **Implement error handling** and retry logic
5. **Respect rate limits** if implemented
6. **Cache results** when appropriate
7. **Validate callsigns** before making requests
8. **Use TypeScript** for type safety in JavaScript projects

---

## See Also

- [MULTIPLE_REPORTFROM_FEATURE.md](MULTIPLE_REPORTFROM_FEATURE.md) - Complete feature documentation
- [API_RESTRUCTURE.md](API_RESTRUCTURE.md) - API endpoint structure
- OpenAPI/Scalar docs - `/scalar` endpoint for interactive API documentation
