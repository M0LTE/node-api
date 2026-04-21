# RabbitMQ Integration

## Overview

RabbitMQ is now the explicit boundary between the two runtime services:

1. `node-api-ingester` receives UDP datagrams on port `13579`
2. The ingester publishes raw datagrams to RabbitMQ
3. `node-api` consumes those queued datagrams and runs validation, rate limiting, MQTT publishing, state updates, and persistence

`node-api` also uses RabbitMQ for HTTP-ingested datagrams when queueing is available.

## Runtime Ownership

| Concern | Owner |
|---|---|
| UDP listener | `node-api-ingester` |
| RabbitMQ publisher for UDP | `node-api-ingester` |
| RabbitMQ consumer | `node-api` |
| DatagramProcessor | `node-api` |
| MQTT publishing | `node-api` |
| Network state and MySQL persistence | `node-api` |

## Queue Contract

- **Exchange**: `udp-datagrams`
- **Queue**: `udp-datagram-queue`
- **Routing key**: `datagram`

Published messages have this shape:

```json
{
  "datagram": "base64-encoded-raw-udp-bytes",
  "sourceIp": "192.0.2.1",
  "receivedAt": "2025-01-01T12:00:00.0000000Z"
}
```

## Configuration

RabbitMQ is configured through environment variables:

- `RABBIT_HOST`
- `RABBIT_USER`
- `RABBIT_PASS`

### `node-api-ingester`

RabbitMQ is required for the ingester. Without it, the ingester cannot complete its UDP-to-queue handoff.

### `node-api`

RabbitMQ is required for queue-based ingestion. If the queue is unavailable, HTTP ingest returns `503 Service Unavailable` instead of bypassing RabbitMQ.

## Processing Flow

### UDP path

```text
UDP datagram
  -> node-api-ingester
  -> RabbitMQ
  -> node-api RabbitMqConsumer
  -> DatagramProcessor
  -> MQTT + state + MySQL
```

### HTTP path

```text
HTTP POST /api/ingest*
  -> node-api
  -> RabbitMQ
```

## Why this split is useful

- UDP ingress can be deployed or restarted independently from the API/storage service
- RabbitMQ provides durability and a clear contract between services
- `node-api` stays focused on processing, APIs, and persistence
- Smoke tests can target HTTP and UDP separately

## Verification hints

Healthy split deployment typically shows:

- `node-api-ingester` listening on UDP `13579`
- `node-api` connected to `udp-datagram-queue`
- queued datagrams appearing on MQTT output topics after consumption

## Related files

- `node-api-ingester/Services/UdpNodeInfoListener.cs`
- `node-api-ingester/Services/RabbitMqPublisher.cs`
- `node-api/Services/RabbitMqConsumer.cs`
- `node-api/Services/DatagramProcessor.cs`
