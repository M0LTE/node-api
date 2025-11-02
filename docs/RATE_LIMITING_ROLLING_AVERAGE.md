# Rolling Average Rate Limiting with Burst Support

## Overview

The UDP rate limiting system now implements a sophisticated dual-threshold approach that:
1. **Tracks rolling average** request rates over a configurable time window
2. **Allows short bursts** above the sustained rate limit
3. **Blocks based on both** sustained average and immediate burst thresholds

## Architecture

### Key Components

#### Rolling Window
- **Duration**: 10 seconds (configurable via `_rollingWindowDuration`)
- **Purpose**: Calculate average request rate over time
- **Algorithm**: Sliding window that removes old timestamps and calculates average

#### Dual Thresholds

1. **Sustained Rate Limit** (Primary)
   - Based on the configured `RequestsPerSecondPerIp` setting
   - Calculated as average over the rolling 10-second window
   - Example: If limit is 2 req/s, an IP can average 2 requests per second over 10 seconds
   - This equals 20 requests in a 10-second period

2. **Burst Limit** (Secondary)
   - Set to **3x the sustained rate** (e.g., 6 req/s if sustained is 2 req/s)
   - Measured over a **1-second window**
   - Allows legitimate short spikes without blocking
   - Example: An IP can send 6 requests in a single second, but must stay under 2 req/s average

## How It Works

### Request Evaluation Flow

```
1. Check blacklist (permanent block)
   ?? If blacklisted ? BLOCK
   ?? If not blacklisted ? Continue

2. Get or create rate limit bucket for IP

3. Clean old timestamps (older than rolling window)

4. Calculate rates:
   ?? Burst rate: requests in last 1 second
   ?? Average rate: requests in last 10 seconds ÷ 10

5. Apply thresholds:
   ?? If burst rate >= burst limit (e.g., 6 req/s) ? BLOCK (burst_limit)
   ?? If average rate >= sustained limit (e.g., 2 req/s) ? BLOCK (sustained_rate_limit)
   ?? Otherwise ? ALLOW and add timestamp
```

### Example Scenarios

#### Scenario 1: Normal Usage
```
IP sends 2 requests per second consistently
- Burst rate: 2 req/s ? (under 6 req/s burst limit)
- Average rate: 2 req/s ? (at 2 req/s sustained limit)
- Result: ALLOWED
```

#### Scenario 2: Short Burst
```
IP sends 5 requests in 1 second, then nothing for 9 seconds
- Burst rate: 5 req/s ? (under 6 req/s burst limit)
- Average rate: 0.5 req/s ? (under 2 req/s sustained limit)
- Result: ALLOWED (burst was acceptable)
```

#### Scenario 3: Excessive Burst
```
IP sends 7 requests in 1 second
- Burst rate: 7 req/s ? (exceeds 6 req/s burst limit)
- Result: BLOCKED (burst_limit)
```

#### Scenario 4: Sustained Overuse
```
IP sends 3 requests per second for 10 seconds (30 total)
- Burst rate: 3 req/s ? (under 6 req/s burst limit)
- Average rate: 3 req/s ? (exceeds 2 req/s sustained limit)
- Result: BLOCKED (sustained_rate_limit)
```

## Configuration

### Settings (in `appsettings.json`)

```json
{
  "UdpRateLimitSettings": {
    "RequestsPerSecondPerIp": 2,  // Sustained average limit
    "Blacklist": []                // CIDR blocks to permanently block
  }
}
```

### Automatic Calculations

- **Burst Limit**: Automatically set to `3 × RequestsPerSecondPerIp`
- **Rolling Window**: Fixed at 10 seconds
- **Cleanup Interval**: Every 1 minute for stale buckets

## Monitoring & Statistics

### Enhanced Stats

The `GetStats()` method now returns:

```csharp
public class IpRateInfo
{
    public string IpAddress { get; set; }              // Redacted IP
    public double RequestsPerSecond { get; set; }      // Current rate (last 1 sec)
    public double AverageRequestsPerSecond { get; set; } // Rolling average
    public int TotalRequests { get; set; }             // Total in window
    public DateTimeOffset LastRequest { get; set; }
    public string? ReportingCallsign { get; set; }     // Ham radio callsign
}
```

### MQTT Events

Blocked IPs publish to `metrics/ratelimit` with:

```json
{
  "ip": "***.***. 1.100",
  "callsign": "G4ABC",
  "reason": "burst_limit" | "sustained_rate_limit" | "blacklist",
  "timestamp": "2024-01-15T10:30:00Z",
  "block_count": 5,
  "expires_at": "2024-01-15T10:31:00Z",
  "average_rate": 2.5,
  "burst_rate": 7,
  "sustained_limit": 2,
  "burst_limit": 6
}
```

## Benefits

### 1. **Flexibility**
- Legitimate users can occasionally send bursts without penalty
- Sustained abuse is caught by the rolling average

### 2. **Fairness**
- Short-term spikes don't permanently block legitimate users
- Malicious sustained traffic is still blocked effectively

### 3. **Responsiveness**
- Immediate burst protection (1-second window)
- Medium-term sustained protection (10-second window)

### 4. **Self-Healing**
- Old timestamps naturally expire from the rolling window
- No permanent blocks for rate limiting (only blacklist)
- IPs can resume normal operation after reducing their rate

## Implementation Details

### Thread Safety
- Uses `ConcurrentDictionary` for bucket storage
- Lock-based synchronization within each bucket
- Async operations performed outside locks to prevent deadlocks

### Memory Management
- Stale buckets cleaned up every minute (IPs inactive for 5+ minutes)
- Request history limited to rolling window duration
- Block history capped at 100 entries

### Performance
- O(1) bucket lookup via dictionary
- O(n) cleanup where n = timestamps in window (typically small)
- Minimal lock contention (per-IP locks, not global)

## Testing Recommendations

### Unit Tests
1. Test burst detection (>6 req/s in 1 second)
2. Test sustained average (>2 req/s over 10 seconds)
3. Test burst allowance (5 req/s in 1 sec, then quiet)
4. Test rolling window cleanup
5. Test thread safety with concurrent requests

### Integration Tests
1. Simulate realistic traffic patterns
2. Test recovery after rate limit expires
3. Test MQTT event publishing
4. Test statistics accuracy

## Future Enhancements

### Possible Improvements
1. **Configurable burst multiplier** (currently hardcoded to 3x)
2. **Configurable rolling window** (currently 10 seconds)
3. **Adaptive limits** based on overall system load
4. **Per-callsign limits** (in addition to per-IP)
5. **Grace periods** for first-time IPs
6. **Progressive penalties** (increasing block duration)

### Metrics to Track
- Average burst sizes across all IPs
- Distribution of rolling averages
- Effectiveness of dual thresholds
- False positive rate (legitimate users blocked)
