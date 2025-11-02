# Fix: Total Requests Display in Active IP Request Rates

## Problem

The "Total Requests" column in the "Active IP Request Rates" table on the web UI (`index.html`) was showing very low numbers that didn't match expectations. For example, an IP that had been sending requests for hours would show only a few requests in the total column.

## Root Cause

In `UdpRateLimitService.GetStats()`, the `TotalRequests` value was calculated as:

```csharp
total = bucket.RequestTimestamps.Count;
```

The `bucket.RequestTimestamps` queue only contains timestamps within the **rolling window** (10 seconds). Older timestamps are automatically removed during rate limit checks to keep the queue small and efficient.

This meant "Total Requests" was actually showing "Requests in the last 10 seconds" rather than a true cumulative total.

## Solution

Added a new `TotalRequestCount` field to the `RateLimitBucket` class that tracks the **lifetime total** of allowed requests for each IP address.

### Code Changes

**File: `node-api/Services/UdpRateLimitService.cs`**

1. **Added field to RateLimitBucket:**
   ```csharp
   private class RateLimitBucket
   {
       public Queue<DateTimeOffset> RequestTimestamps { get; } = new();
       public DateTimeOffset LastAccess { get; set; }
       public string? ReportingCallsign { get; set; }
       public long TotalRequestCount { get; set; }  // NEW
   }
   ```

2. **Increment counter when requests are allowed:**
   ```csharp
   else
   {
       // Add this request
       bucket.RequestTimestamps.Enqueue(now);
       bucket.TotalRequestCount++;  // NEW
   }
   ```

3. **Use the counter in GetStats():**
   ```csharp
   total = (int)bucket.TotalRequestCount;  // CHANGED from bucket.RequestTimestamps.Count
   ```

## Behavior

- **TotalRequestCount** tracks only **allowed** requests (not blocked ones)
- Counter persists for the lifetime of the bucket (until cleanup after 5 minutes of inactivity)
- Each IP address has its own independent counter
- Counter resets when the IP bucket is cleaned up due to inactivity

## Example

**Before the fix:**
- IP sends 1000 requests over 10 minutes
- "Total Requests" shows: ~100 (only those in last 10-second window)

**After the fix:**
- IP sends 1000 requests over 10 minutes
- "Total Requests" shows: 1000 (all allowed requests)

## Testing

Created new test file `Tests/UdpRateLimitTotalRequestsTests.cs` with 3 tests:

1. **GetStats_TotalRequests_Should_Show_Cumulative_Count**
   - Verifies total count persists even after rolling window clears
   
2. **GetStats_TotalRequests_Should_Not_Count_Blocked_Requests**
   - Verifies only allowed requests are counted
   
3. **GetStats_TotalRequests_Should_Be_Separate_Per_IP**
   - Verifies each IP has independent counters

All tests pass. ?

## Impact

- Web UI now shows meaningful total request counts
- No impact on rate limiting behavior itself
- No breaking changes to existing functionality
- Minimal memory overhead (8 bytes per active IP)

## Related Files

- `node-api/Services/UdpRateLimitService.cs` - Core fix
- `node-api/wwwroot/index.html` - UI that displays the data
- `Tests/UdpRateLimitTotalRequestsTests.cs` - New tests
