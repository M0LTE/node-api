# RabbitMQ Integration for UDP Datagram Persistence

## Overview

This integration adds RabbitMQ message queue support to the UDP datagram ingestion pipeline. The purpose is to increase robustness and prepare for future service separation where the UDP ingestion and processing can be split into two microservices.

## Architecture

### Current Implementation

The system now has two parallel paths for UDP datagrams:

1. **Direct Processing Path** (existing, continues to work)
   - UDP datagrams received ? Validated ? Processed ? Published to MQTT

2. **RabbitMQ Path** (new, preparatory)
   - UDP datagrams received ? **Published to RabbitMQ** ? Consumed from RabbitMQ ? (TODO: Processing)

### Components

#### RabbitMqPublisher
- **Location**: `Services/RabbitMqPublisher.cs`
- **Purpose**: Writes raw UDP datagrams to RabbitMQ immediately upon receipt
- **Queue**: `udp-datagram-queue`
- **Exchange**: `udp-datagrams` (direct exchange)
- **Routing Key**: `datagram`

#### RabbitMqConsumer
- **Location**: `Services/RabbitMqConsumer.cs`
- **Purpose**: Consumes datagrams from RabbitMQ queue
- **Status**: Currently logs received messages; processing integration is TODO

## Configuration

RabbitMQ is **optional** and configured via environment variables:

- `RABBIT_HOST` - RabbitMQ server hostname
- `RABBIT_USER` - RabbitMQ username
- `RABBIT_PASS` - RabbitMQ password

### Behavior When Not Configured

If any of the environment variables are missing:
- Publisher will log a warning and disable itself
- Consumer will log an informational message and not start
- **The application continues to work normally** using the direct processing path
- No errors or exceptions are thrown

### Behavior When Configured But Unavailable

If RabbitMQ is configured but the server is unreachable:
- Publisher will log errors but **not block UDP processing**
- UDP datagrams continue to be processed via the direct path
- Consumer will retry connection every 5 seconds

## Message Format

Messages published to RabbitMQ are JSON objects with the following structure:

```json
{
  "datagram": "base64-encoded-raw-udp-bytes",
  "sourceIp": "192.0.2.1",
  "receivedAt": "2025-01-01T12:00:00.0000000Z"
}
```

## Testing

### Development Environment

The RabbitMQ server is **not accessible from development environments**. This is expected and the application will work fine without it:

```
warn: node_api.Services.RabbitMqPublisher[0]
      RabbitMQ not configured (missing RABBIT_HOST, RABBIT_USER, or RABBIT_PASS environment variables). Running without RabbitMQ.

info: node_api.Services.RabbitMqConsumer[0]
      RabbitMQ consumer is disabled (RabbitMQ not configured)
```

### Production/Staging Environment

Set the environment variables and the services will automatically activate:

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

## Future Work

### Phase 2: Complete Consumer Processing

The RabbitMQ consumer currently receives messages but doesn't fully process them. Next steps:

1. Extract datagram processing logic into a shared service
2. Have both UDP listener and RabbitMQ consumer use this shared service
3. Ensure idempotency in processing (handle duplicate messages)

### Phase 3: Service Separation

Once the consumer is fully functional and tested:

1. Split into two separate services:
   - **Ingestion Service**: UDP listener + RabbitMQ publisher only
   - **Processing Service**: RabbitMQ consumer + processing logic + MQTT publishing

2. Benefits:
   - Ingestion service stays simple and stable (rarely needs updates)
   - Processing service can be updated/redeployed independently
   - Better scalability (can run multiple processing instances)
   - Improved reliability (messages persist in queue during processing service updates)

## Queue Configuration

The RabbitMQ queue is configured as:
- **Durable**: Yes (survives broker restarts)
- **Exclusive**: No (can have multiple consumers)
- **Auto-delete**: No (queue persists when no consumers)
- **QoS Prefetch**: 10 messages per consumer

## Monitoring

Key log messages to monitor:

- `RabbitMQ publisher initialized successfully` - Publisher connected
- `RabbitMQ consumer connected and listening` - Consumer connected
- `Failed to publish datagram to RabbitMQ` - Publish errors (UDP still processed)
- `Error processing RabbitMQ message` - Consumer processing errors

## Dependencies

- **RabbitMQ.Client** v6.8.1
- Automatically installed via NuGet
