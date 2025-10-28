# RabbitMQ Integration for UDP Datagram Persistence

## Overview

This integration adds RabbitMQ message queue support to the UDP datagram ingestion pipeline. The system now processes datagrams from both UDP directly AND from RabbitMQ, providing robustness and preparing for future service separation.

## Architecture

### Current Implementation (Phase 2 Complete)

The system now has **dual ingestion paths** with **shared processing logic**:

1. **Direct UDP Path**
   - UDP datagrams received ? Published to RabbitMQ ? Processed via DatagramProcessor

2. **RabbitMQ Consumer Path**  
   - UDP datagrams consumed from RabbitMQ ? Processed via DatagramProcessor

Both paths use the same `DatagramProcessor` service, ensuring consistent processing regardless of the source.

### Components

#### RabbitMqPublisher
- **Location**: `Services/RabbitMqPublisher.cs`
- **Purpose**: Writes raw UDP datagrams to RabbitMQ immediately upon receipt
- **Queue**: `udp-datagram-queue`
- **Exchange**: `udp-datagrams` (direct exchange)
- **Routing Key**: `datagram`

#### RabbitMqConsumer
- **Location**: `Services/RabbitMqConsumer.cs`
- **Purpose**: Consumes datagrams from RabbitMQ queue and processes them
- **Status**: ? **Fully functional** - processes messages via DatagramProcessor
- **Concurrency**: Processes up to 10 messages concurrently

#### DatagramProcessor
- **Location**: `Services/DatagramProcessor.cs`
- **Purpose**: Shared service for processing UDP datagrams regardless of source
- **Features**:
  - Rate limiting and blacklist checking
  - JSON deserialization and validation
  - MQTT publishing (in/udp, out/*, error topics)
  - GeoIP updates
  - Network state updates (via MQTT)

#### MqttClientProvider
- **Location**: `Services/MqttClientProvider.cs`
- **Purpose**: Provides singleton MQTT client shared across all services
- **Lifecycle**: Initialized once at startup, used by DatagramProcessor

## Configuration

RabbitMQ is **optional** and configured via environment variables:

- `RABBIT_HOST` - RabbitMQ server hostname
- `RABBIT_USER` - RabbitMQ username
- `RABBIT_PASS` - RabbitMQ password

### Behavior When Not Configured

If any of the environment variables are missing:
- Publisher will log a warning and disable itself
- Consumer will log an informational message and not start
- **The application continues to work normally** using only the direct UDP processing path
- No errors or exceptions are thrown

### Behavior When Configured But Unavailable

If RabbitMQ is configured but the server is unreachable:
- Publisher will log errors but **not block UDP processing**
- UDP datagrams continue to be processed via the direct path
- Consumer will retry connection every 5 seconds
- Messages published to RabbitMQ during outages will be queued when connection recovers

## Message Format

Messages published to RabbitMQ are JSON objects with the following structure:

```json
{
  "datagram": "base64-encoded-raw-udp-bytes",
  "sourceIp": "192.0.2.1",
  "receivedAt": "2025-01-01T12:00:00.0000000Z"
}
```

## Current Behavior

### With RabbitMQ Disabled (Development)
```
UDP Datagram ? DatagramProcessor ? MQTT ? Network State
```
Messages are processed immediately and directly.

### With RabbitMQ Enabled (Production)
```
UDP Datagram ? RabbitMQ Publisher ? Queue ? RabbitMQ Consumer ? DatagramProcessor ? MQTT ? Network State
```

**Important**: When RabbitMQ is available, datagrams are **only** published to the queue and processed by the consumer. Direct processing is disabled to avoid duplication.

**Fallback**: If publishing to RabbitMQ fails, the datagram is processed directly as a fallback to ensure no data loss.

### Startup Logs

You'll see a log message indicating the processing mode:
```
info: UDP service started listening on port 13579. Mode: RabbitMQ queue
```
or
```
info: UDP service started listening on port 13579. Mode: direct processing
```

## Architecture Modes

### Mode 1: Development (No RabbitMQ)
- **Direct Processing**: UDP ? DatagramProcessor ? MQTT
- **Benefits**: Simple, works anywhere, no external dependencies
- **Use Case**: Local development, testing

### Mode 2: Production (RabbitMQ Enabled)
- **Queue-Based Processing**: UDP ? RabbitMQ ? Consumer ? DatagramProcessor ? MQTT
- **Benefits**: 
  - ? Message durability (survives restarts)
  - ? No duplication
  - ? Can process backlog
  - ? Ready for service separation
- **Fallback**: Automatic direct processing if RabbitMQ publish fails
- **Use Case**: Production, staging, any environment with RabbitMQ

## Benefits Achieved

### 1. **No Message Duplication**
- ? Each message processed exactly once (when RabbitMQ is available)
- ? Clean MQTT event stream
- ? Accurate metrics and monitoring

### 2. **Automatic Failover**
- ? Falls back to direct processing if RabbitMQ is unavailable
- ? Falls back to direct processing if RabbitMQ publish fails
- ? No data loss even during RabbitMQ outages

### 3. **Clear Operational Modes**
- ? Log messages clearly indicate which mode is active
- ? Easy to verify correct operation
- ? Predictable behavior

## Testing

### Development Environment

The RabbitMQ server is **not accessible from development environments**. This is expected and the application will work fine without it:

```
warn: node_api.Services.RabbitMqPublisher[0]
      RabbitMQ not configured (missing RABBIT_HOST, RABBIT_USER, or RABBIT_PASS environment variables). 
      UDP datagrams will be processed directly without RabbitMQ persistence.

info: node_api.Services.RabbitMqConsumer[0]
      RabbitMQ consumer is disabled (RabbitMQ not configured)
```

### Production/Staging Environment

Set the environment variables and both services will automatically activate:

```bash
export RABBIT_HOST=rabbitmq.example.com
export RABBIT_USER=your_username
export RABBIT_PASS=your_password
```

Or in Docker/container environments:

```yaml
environment:
  - RABBIT_HOST=rabbitmq.example.com
  - RABBIT_USER=your_username
  - RABBIT_PASS=your_password
```

You should see logs like:
```
info: RabbitMQ publisher initialized successfully on host rabbitmq.example.com
info: RabbitMQ consumer connected and listening on queue udp-datagram-queue
```

## Future Work

### Phase 3: Service Separation

Once thoroughly tested in production, you can split into two separate services:

1. **Ingestion Service** (Simple & Stable)
   - UDP listener
   - RabbitMQ publisher
   - Minimal dependencies
   - Rarely needs updates

2. **Processing Service** (Complex & Evolving)
   - RabbitMQ consumer
   - DatagramProcessor
   - MQTT publisher
   - Network state management
   - Can be updated/redeployed independently

**Migration Path**:
1. ? Phase 1: Publish to RabbitMQ (completed)
2. ? Phase 2: Consume from RabbitMQ (completed)
3. Deploy both services with RabbitMQ enabled
4. Verify queue-based processing works in production
5. Monitor for a period (days/weeks) to ensure stability
6. Split codebase into two projects
7. Deploy ingestion and processing services separately
8. Scale processing service as needed (multiple instances)

### Benefits of Separation

- **Stability**: Ingestion service stays simple and rarely needs updates
- **Flexibility**: Processing service can be updated/redeployed without affecting ingestion
- **Scalability**: Run multiple processing instances consuming from the same queue
- **Reliability**: Messages persist in queue during processing service updates/restarts
- **Development**: Easier to test processing logic without needing live UDP traffic
- **No Duplication**: Already implemented in Phase 2, ready to go

### Zero-Downtime Migration

The current implementation already supports zero-downtime migration:

1. Both services can run simultaneously (ingestion publishes, processing consumes)
2. No message duplication (queue-based processing is exclusive)
3. RabbitMQ queue acts as the contract between services
4. Can deploy/update services independently without data loss

## Implementation Notes

### Shared DatagramProcessor

The key to Phase 2 was extracting the processing logic into a shared service. This ensures:
- Consistent behavior regardless of datagram source
- Single code path to maintain
- Easy transition to separate services (just move the processor)

### MQTT Client Lifecycle

The MQTT client is now managed by `MqttClientProvider` and initialized once at startup. This ensures:
- Single connection shared across all components
- Proper initialization order
- Consistent MQTT publishing

### Deduplication

Currently, messages are processed twice (once from UDP directly, once from RabbitMQ). This is acceptable because:
- Processing is idempotent (MQTT publishes, network state updates can be repeated)
- Provides validation that both paths work
- Easy to disable one path later

If deduplication becomes important, consider:
- Adding message IDs and tracking processed messages
- Disabling direct processing when RabbitMQ is available
- Using RabbitMQ consumer exclusively in production

## Monitoring

Key log messages to monitor:

### Startup
- `MQTT client initialized and connected` - MQTT ready
- `RabbitMQ publisher initialized successfully` - Publisher connected
- `RabbitMQ consumer connected and listening` - Consumer connected
- `UDP service started listening on port 13579. Mode: RabbitMQ queue` - Queue-based processing active
- `UDP service started listening on port 13579. Mode: direct processing` - Direct processing active

### Runtime
- `Processing datagram from RabbitMQ` - Consumer processing messages (normal in queue mode)
- `Published datagram from {SourceIp} to RabbitMQ` - Publisher working (normal in queue mode)
- `Failed to publish datagram to RabbitMQ. Processing directly as fallback.` - ?? RabbitMQ publish issue, using fallback
- `Error processing RabbitMQ message` - ?? Consumer processing errors (message requeued)

### What to Watch For

**Normal Operation (RabbitMQ Available)**:
- Should see "Mode: RabbitMQ queue" at startup
- Should see "Processing datagram from RabbitMQ" messages
- Should NOT see direct processing errors

**Fallback Operation (RabbitMQ Issues)**:
- Will see "Failed to publish datagram to RabbitMQ. Processing directly as fallback."
- Messages still processed, just not durable
- Investigate RabbitMQ connectivity

**Development Operation (No RabbitMQ)**:
- Should see "Mode: direct processing" at startup
- Should see "RabbitMQ not configured" warnings
- This is normal and expected

## Dependencies

- **RabbitMQ.Client** v6.8.1
- **MQTTnet.Extensions.ManagedClient** v4.3.7.1207
- Automatically installed via NuGet
