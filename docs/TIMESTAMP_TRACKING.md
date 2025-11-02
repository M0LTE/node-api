# UDP Datagram Arrival Timestamp Tracking

## Overview

The system now tracks the exact arrival time of UDP datagrams at the server and persists this timestamp to the database `traces` and `events` tables.

## Implementation

### 1. Timestamp Capture

**Location**: `UdpNodeInfoListener.StartUdpListenerAsync()`

The arrival timestamp is captured immediately when the UDP datagram is received:

```csharp
var result = await _udpClient.ReceiveAsync(stoppingToken);
var arrivalTime = DateTime.UtcNow; // Captured immediately
```

### 2. Timestamp Propagation

The arrival time flows through the entire processing pipeline:

#### Via RabbitMQ
- **Publisher**: Stores `receivedAt` in the RabbitMQ message JSON
- **Consumer**: Extracts `receivedAt` and passes it to `DatagramProcessor`

#### Via Direct Processing  
- **UDP Listener**: Passes `arrivalTime` directly to `DatagramProcessor`

#### Through DatagramProcessor
- Passed as parameter to `ProcessDatagramAsync(datagram, sourceIp, arrivalTime)`
- Added to MQTT messages as user property: `arrivalTime` (ISO 8601 format)
- Flows through processing channel to `HandleFrame`

#### To MQTT
All MQTT messages (in/udp, out/*, error topics) include:
- **User Property**: `arrivalTime` in ISO 8601 format (`"O"` format string)

### 3. Database Storage

**Location**: `DbWriter.SaveOutputMessage()`

The `DbWriter` extracts the arrival time from MQTT user properties and uses it for database inserts:

```csharp
// Extract from MQTT user property
var arrivalTimeStr = args.ApplicationMessage.UserProperties
    .SingleOrDefault(p => p.Name == "arrivalTime")?.Value;

// Parse and use for insert
INSERT INTO traces (json, timestamp) VALUES (@json, @timestamp)
INSERT INTO events (json, timestamp) VALUES (@json, @timestamp)
```

## Database Schema

The `timestamp` field is of type `timestamp(3)` in MariaDB (millisecond precision).

- **Before**: Database set timestamp on insert (using `DEFAULT CURRENT_TIMESTAMP(3)`)
- **After**: Application sets timestamp explicitly using UDP datagram arrival time

## Benefits

1. **Accurate Timing**: Timestamp reflects when the datagram actually arrived at the server, not when it was processed or inserted into the database
2. **Consistent Across Paths**: Same timestamp whether processed directly or via RabbitMQ queue
3. **Analysis**: Enables accurate latency analysis and event ordering
4. **Durability**: Timestamp preserved even if processing is delayed (e.g., when consuming from RabbitMQ backlog)

## Fallback Behavior

If for any reason the arrival time is not available (shouldn't happen in normal operation):
- Database insert omits the `timestamp` column
- Database uses its default `CURRENT_TIMESTAMP(3)`
- Ensures no data loss even in edge cases

## Timestamp Format

- **In Memory**: `DateTime` (UTC)
- **In MQTT**: ISO 8601 string (`"O"` format, e.g., `"2025-01-16T10:30:00.1234567Z"`)
- **In RabbitMQ**: ISO 8601 string (via JSON `receivedAt` field)
- **In Database**: `timestamp(3)` (millisecond precision)

## Testing

To verify the implementation:

1. Send a UDP datagram
2. Check MQTT messages for `arrivalTime` user property
3. Query database to see timestamp matches arrival time (not insert time)
4. Publish to RabbitMQ, stop consumer, restart later - verify timestamp still reflects original arrival time

## Files Modified

- `node-api/Services/IDatagramProcessor.cs` - Added `arrivalTime` parameter
- `node-api/Services/DatagramProcessor.cs` - Thread arrival time through processing
- `node-api/Services/UdpNodeInfoListener.cs` - Capture arrival time on receive
- `node-api/Services/RabbitMqConsumer.cs` - Use `receivedAt` from queue message  
- `node-api/Services/DbWriter.cs` - Extract from MQTT and insert to database
