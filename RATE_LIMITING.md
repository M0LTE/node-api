# UDP Rate Limiting Lifecycle

This document describes how IP addresses are rate limited and un-rate limited in the node-api service.

## Overview

The node-api service implements IP-based rate limiting for UDP datagrams to protect against excessive traffic and abuse. The system uses a **sliding window algorithm** for rate limiting and supports **CIDR-based blacklisting** for permanent blocks.

## Architecture

Rate limiting is implemented by the `UdpRateLimitService` class which provides:
- Per-IP rate limiting using a sliding 1-second window
- CIDR-based blacklisting (permanent blocks)
- MQTT event publishing for monitoring
- Statistics tracking and HTTP API for diagnostics

## Configuration

Rate limiting is configured via `appsettings.json`:

```json
{
  "UdpRateLimitSettings": {
    "RequestsPerSecondPerIp": 10,
    "Blacklist": [
      "192.168.1.0/24",
      "10.0.0.1",
      "2001:db8::/32"
    ]
  }
}
```

- **RequestsPerSecondPerIp**: Maximum requests allowed per IP per second (default: 10)
- **Blacklist**: List of IP addresses or CIDR ranges to permanently block

## How an IP Becomes Rate Limited

### 1. Request Arrives

When a UDP datagram arrives at the service (port 13579), the `UdpNodeInfoListener` extracts:
- The source IP address from the UDP packet
- The optional `reportFrom` callsign from the datagram payload

### 2. Blacklist Check (First)

**Before** rate limiting, the system checks if the IP is in the blacklist:

```
IsBlacklisted(ipAddress)
  ↓
  Check each blacklisted network/IP
  ↓
  If match found → Block (permanent)
```

- Blacklist checks use CIDR matching (e.g., `192.168.1.0/24` matches all IPs in that subnet)
- Blacklist blocks are **permanent** for the lifetime of the service
- Both IPv4 and IPv6 are supported
- If blacklisted, the request is blocked immediately (no rate limit check occurs)

### 3. Rate Limit Check (Second)

If not blacklisted, the system checks the rate limit using a **sliding window**:

```
ShouldAllowRequestAsync(ipAddress)
  ↓
  Get or create bucket for this IP
  ↓
  Remove timestamps older than 1 second
  ↓
  Count remaining timestamps
  ↓
  If count >= RequestsPerSecondPerIp
    → Block (temporary, automatic expiry)
  Else
    → Allow and add current timestamp
```

#### Sliding Window Mechanism

Each IP has a "bucket" containing timestamps of recent requests:

```
Time:     0s    0.2s   0.5s   0.8s   1.0s   1.2s   1.5s
Request:  ✓     ✓      ✓      ✓      ✓      ?
          
At 1.2s: Remove requests before 0.2s
  → Only 4 requests in last second
  → Allow request

At 1.2s: If we had 10 requests in last second
  → Block request
```

Key characteristics:
- **Window size**: Exactly 1 second
- **Window type**: Sliding (continuously updating)
- **Granularity**: Individual requests tracked with high precision
- **Cleanup**: Automatic removal of timestamps older than 1 second on each check

### 4. Block Recording

When a request is blocked, the service records the event:

```json
{
  "ip": "***.***. 1.100",
  "callsign": "G0ABC",
  "reason": "rate_limit",
  "timestamp": "2025-10-27T20:00:00Z",
  "block_count": 5,
  "expires_at": "2025-10-27T20:01:00Z"
}
```

This information is:
- Stored in memory (last 100 blocked IPs)
- Published to MQTT topic `metrics/ratelimit`
- Exposed via diagnostics API at `/api/diagnostics/ratelimit/stats`

### 5. Logging

All blocks are logged with appropriate severity:

```
[WARNING] Rate limit exceeded for IP: 192.168.1.100 (Callsign: G0ABC) (12 req/s)
[WARNING] Blocked blacklisted IP: 10.0.0.1 (Callsign: M0XYZ)
```

IP addresses are **redacted** in logs and MQTT messages:
- IPv4: First two octets replaced (e.g., `192.168.1.100` → `***.***. 1.100`)
- IPv6: First two segments replaced (e.g., `2001:db8::1` → `****:****::1`)

## How an IP Becomes Un-Rate Limited

### Automatic Expiry (Rate Limits)

Rate limits are **automatically removed** through the sliding window mechanism:

```
Initial state (10 req/s limit):
  0.0s: Request 1-10 → All allowed
  0.1s: Request 11 → BLOCKED
  0.5s: Request 12 → BLOCKED
  1.0s: Request 13 → BLOCKED (timestamps from 0.0s still count)
  1.1s: Request 14 → ALLOWED (timestamps from 0.0s removed, only 9 in window)
```

**Key Point**: There is no "un-rate limiting" event. As soon as old timestamps age out (after 1 second), new requests are automatically allowed.

Timeline example with 10 req/s limit:
```
T=0.0s:  Make 10 requests → All allowed
T=0.5s:  Make 1 request   → BLOCKED (10 requests in last second)
T=1.0s:  Make 1 request   → BLOCKED (10 requests from 0.0-1.0s)
T=1.01s: Make 1 request   → ALLOWED (only 9 requests from 0.01-1.01s)
```

### No Manual Un-Rate Limiting

There is **no manual intervention** to un-rate limit an IP:
- No API endpoint to clear rate limits
- No configuration change needed
- No service restart required

The sliding window automatically handles this.

### Blacklist (Permanent)

Blacklisted IPs are **never** automatically un-blocked:
- Blacklist is loaded at service startup
- Changes require configuration update and service restart
- No automatic expiry for blacklist entries

## Rate Limit Types Comparison

| Feature | Rate Limit | Blacklist |
|---------|-----------|-----------|
| **Duration** | Automatic (1 second sliding window) | Permanent (until config change) |
| **Trigger** | Too many requests in 1 second | IP/CIDR in blacklist config |
| **Check Order** | Second | First |
| **Expiry** | Automatic (as timestamps age) | Never (config-based) |
| **Reason Code** | `rate_limit` | `blacklist` |
| **Use Case** | Protect against bursts | Block known bad actors |

## Statistics and Monitoring

### HTTP API

Get current rate limiting statistics:

```bash
GET /api/diagnostics/ratelimit/stats
```

Response:
```json
{
  "totalBlacklisted": 150,
  "totalRateLimited": 42,
  "activeIpAddresses": 5,
  "recentlyBlockedIps": [
    {
      "ipAddress": "***.***. 1.100",
      "reason": "rate_limit",
      "blockedAt": "2025-10-27T20:00:00Z",
      "blockCount": 3,
      "expiresAt": "2025-10-27T20:01:00Z",
      "reportingCallsign": "G0ABC"
    }
  ],
  "activeIpRates": [
    {
      "ipAddress": "***.***. 1.50",
      "requestsPerSecond": 8.0,
      "totalRequests": 8,
      "lastRequest": "2025-10-27T20:00:05Z",
      "reportingCallsign": "M0XYZ"
    }
  ]
}
```

### MQTT Events

Rate limit events are published to `metrics/ratelimit`:

```json
{
  "ip": "***.***. 1.100",
  "callsign": "G0ABC",
  "reason": "rate_limit",
  "timestamp": "2025-10-27T20:00:00Z",
  "block_count": 1,
  "expires_at": "2025-10-27T20:01:00Z"
}
```

### Metrics

- **totalBlacklisted**: Counter of all blacklist blocks since service start
- **totalRateLimited**: Counter of all rate limit blocks since service start
- **activeIpAddresses**: Number of IPs with rate limit buckets in memory
- **recentlyBlockedIps**: Last 100 blocked IPs (blacklist or rate limit)
- **activeIpRates**: Top 20 most active IPs in last 60 seconds

## Implementation Details

### Data Structures

Each IP has a `RateLimitBucket`:
```csharp
class RateLimitBucket
{
    Queue<DateTimeOffset> RequestTimestamps;  // Sliding window
    DateTimeOffset LastAccess;                // For cleanup
    string? ReportingCallsign;                // From datagram
}
```

### Thread Safety

- Uses `ConcurrentDictionary` for thread-safe bucket storage
- Per-bucket locks for timestamp queue operations
- `Interlocked` for counter updates

### Memory Management

#### Cleanup of Stale Buckets

A background timer runs every 60 seconds to remove inactive IPs:

```csharp
CleanupStaleBuckets()
  ↓
  Find buckets with LastAccess > 5 minutes ago
  ↓
  Remove from dictionary
```

This prevents memory leaks from one-time senders.

#### Blocked IP History

- Limited to last 100 blocked IPs
- Oldest entries removed when limit exceeded
- Separate from active rate limit buckets

### Request Flow

Complete flow from UDP packet to rate limit decision:

```
1. UDP Datagram arrives at port 13579
   ↓
2. UdpNodeInfoListener receives packet
   ↓
3. Extract IP address from endpoint
   ↓
4. Parse datagram for reportFrom callsign
   ↓
5. Call UdpRateLimitService.ShouldAllowRequestAsync()
   ↓
6. Check blacklist first
   ├─ Blacklisted → BLOCK (log, record, publish MQTT)
   └─ Not blacklisted → Continue to step 7
   ↓
7. Get/create rate limit bucket for IP
   ↓
8. Lock bucket and remove old timestamps
   ↓
9. Count remaining timestamps
   ├─ Count >= limit → BLOCK (log, record, publish MQTT)
   └─ Count < limit → ALLOW (add timestamp)
   ↓
10. If ALLOWED: Process datagram normally
    If BLOCKED: Drop packet, log debug message
```

### Code Location

- **Service**: `/node-api/Services/UdpRateLimitService.cs`
- **Settings**: `/node-api/Models/UdpRateLimitSettings.cs`
- **Integration**: `/node-api/Services/UdpNodeInfoListener.cs` (lines ~140-148)
- **API**: `/node-api/Controllers/DiagnosticsController.cs` (lines 264-270)
- **Tests**: `/Tests/UdpRateLimitServiceTests.cs` and `/Tests/UdpRateLimitIntegrationTests.cs`

## Common Scenarios

### Scenario 1: Burst Traffic

A station sends 15 datagrams in quick succession:

```
T=0.00s: Requests 1-10  → All ALLOWED
T=0.05s: Requests 11-15 → All BLOCKED (10/s limit)
T=1.10s: Request 16     → ALLOWED (all timestamps from T=0 expired)
```

### Scenario 2: Sustained Traffic

A station sends 8 requests per second continuously:

```
T=0.0s:  8 requests → All ALLOWED
T=1.0s:  8 requests → All ALLOWED (previous 8 expired)
T=2.0s:  8 requests → All ALLOWED (continues indefinitely)
```

### Scenario 3: Gradual Recovery

A station exceeds the limit, then slows down:

```
T=0.0s:  10 requests → All ALLOWED (bucket full)
T=0.5s:  1 request   → BLOCKED
T=1.0s:  1 request   → BLOCKED (still 10 in window from 0.0-1.0s)
T=1.1s:  1 request   → ALLOWED (only 1 from 0.1-1.1s, 9 expired)
```

### Scenario 4: Blacklisted IP

A blacklisted IP tries to send:

```
Every request: BLOCKED (no rate limit check, immediate block)
No automatic unblock (requires config change + restart)
```

## Performance Considerations

### Time Complexity

- **ShouldAllowRequestAsync**: O(n) where n = requests in last second (typically < 20)
- **Cleanup**: O(m) where m = stale buckets (runs every 60s)
- **GetStats**: O(k) where k = active IPs (limited to top 20)

### Memory Usage

- Each bucket: ~200 bytes + (timestamp count × 16 bytes)
- 1000 active IPs @ 10 req/s = ~360 KB
- Blocked history (100 entries): ~20 KB

### Scalability

Tested with:
- 100 simultaneous IPs
- 500 total requests
- Completes in < 1 second

## Security Considerations

### IP Redaction

All IP addresses are **partially redacted** in:
- Logs
- MQTT messages
- API responses

This prevents exposure of complete IP addresses while still allowing pattern detection.

### CIDR Blacklisting

Supports flexible blocking:
- Single IP: `192.168.1.50`
- Subnet: `192.168.1.0/24`
- Large range: `10.0.0.0/8`
- IPv6: `2001:db8::/32`

### Protection Against

- **DoS attacks**: Rate limiting prevents single IP from overwhelming service
- **Distributed attacks**: Each IP tracked independently
- **Known bad actors**: Blacklist provides permanent blocks

## Troubleshooting

### "Why is my IP blocked?"

Check the diagnostics endpoint:

```bash
curl http://localhost:5000/api/diagnostics/ratelimit/stats
```

Look for:
- Your IP in `recentlyBlockedIps`
- Reason: `rate_limit` or `blacklist`
- If rate_limit: Wait 1 second for automatic recovery
- If blacklist: Contact administrator

### "Rate limiting seems inconsistent"

Rate limiting uses a **sliding window**, not fixed intervals:
- Requests are allowed based on the last 1 second
- Not based on fixed time buckets (e.g., "00:00:00-00:00:01")

Example:
```
✗ Wrong thinking: "I can send 10 at :00 and 10 at :01"
✓ Correct: "I can send 10 in any 1-second period"
```

### "How do I unblock an IP?"

**Rate limits**: Wait 1 second (automatic)
**Blacklist**: Edit configuration and restart service

## Future Enhancements

Potential improvements (not currently implemented):

- Configurable window size (currently fixed at 1 second)
- Per-callsign rate limiting (currently per-IP only)
- Dynamic blacklist via API (currently config-only)
- Rate limit exceptions/whitelisting
- Exponential backoff for repeat offenders
- Metrics export to Prometheus

## References

- Implementation: `UdpRateLimitService.cs`
- Configuration: `UdpRateLimitSettings.cs`
- Tests: `UdpRateLimitServiceTests.cs`, `UdpRateLimitIntegrationTests.cs`
- API: `DiagnosticsController.cs`
