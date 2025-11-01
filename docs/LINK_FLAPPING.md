# Link Flapping Detection

## Overview

This feature tracks and identifies "flapping" links in the packet network. A link is considered flapping when it repeatedly goes up and down within a short time period, indicating an unstable connection.

## How It Works

### Detection Logic

1. **Flap Tracking**: When a link transitions from Disconnected to Active status, the system increments a flap counter
2. **Time Windows**: Flaps are tracked within a configurable time window (default: 15 minutes)
3. **Threshold**: A link is considered "flapping" when it has 3 or more flaps within the time window
4. **Auto-Reset**: The flap counter resets when the time window expires

### Data Model

The `LinkState` class now includes:
- `FlapCount` - Number of up/down transitions in the current window
- `FlapWindowStart` - Start time of the current detection window
- `LastFlapTime` - Timestamp of the most recent flap
- `IsFlapping(flapThreshold, windowMinutes)` - Method to check if link is currently flapping

### Database Schema

Migration `003_add_link_flap_tracking.sql` adds three columns to the `links` table:
```sql
flap_count INT NOT NULL DEFAULT 0
flap_window_start DATETIME(6) NULL
last_flap_time DATETIME(6) NULL
```

## API Usage

### Get Flapping Links (Default Settings)

```bash
GET /api/links/flapping
```

Returns all links with 3+ flaps in the last 15 minutes.

### Customize Threshold

```bash
GET /api/links/flapping?flapThreshold=5
```

Returns links with 5+ flaps in the last 15 minutes.

### Customize Time Window

```bash
GET /api/links/flapping?windowMinutes=30
```

Returns links with 3+ flaps in the last 30 minutes.

### Combine Parameters

```bash
GET /api/links/flapping?flapThreshold=4&windowMinutes=20
```

Returns links with 4+ flaps in the last 20 minutes.

## Response Format

The endpoint returns an array of `LinkState` objects, ordered by flap count (descending):

```json
[
  {
    "canonicalKey": "G8PZT<->M0LTE",
    "endpoint1": "G8PZT",
    "endpoint2": "M0LTE",
    "status": "Active",
    "flapCount": 7,
    "flapWindowStart": "2025-11-01T10:30:00Z",
    "lastFlapTime": "2025-11-01T10:44:00Z",
    "connectedAt": "2025-11-01T10:44:00Z",
    "lastUpdate": "2025-11-01T10:44:00Z",
    ...
  }
]
```

## Filtering

The endpoint automatically excludes:
- Links involving TEST callsigns (TEST, TEST-1 through TEST-15)
- Links involving hidden callsigns (configured in `appsettings.json`)

## Logging

When a link reaches the flap threshold, a warning is logged:

```
Link G8PZT<->M0LTE is flapping: 3 transitions in the last 15 minutes
```

## Use Cases

1. **Network Monitoring**: Identify unstable RF links or problematic network segments
2. **Troubleshooting**: Quickly find links that need attention
3. **Performance Analysis**: Track link stability over time
4. **Alerting**: Build alerts based on flapping link detection

## Implementation Notes

- Flap detection only occurs when a link comes **up** after being disconnected
- The first time a link comes up (never disconnected before) is **not** counted as a flap
- Flap tracking persists to the database and survives application restarts
- The detection logic runs in-memory for performance, with periodic database sync
