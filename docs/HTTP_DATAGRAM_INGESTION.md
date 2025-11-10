# HTTP Datagram Ingestion API

**Date**: 2025-01-21  
**Status**: ? Implemented with **Full OpenAPI Schema Support via Typed Endpoints**

## Overview

The **HTTP Datagram Ingestion API** provides RESTful endpoints for submitting network event datagrams via HTTP POST requests. Data submitted through these endpoints follows the **exact same processing pipeline** as UDP datagrams:

```
HTTP POST ? RabbitMQ Queue ? Consumer ? DatagramProcessor ? MQTT ? Network State
     ?                                         ?
  (same path as UDP datagrams)
```

## Key Features

- ? **Identical Processing**: Uses the same `DatagramProcessor` as UDP ingestion
- ? **RabbitMQ Integration**: Publishes to the same queue as UDP datagrams
- ? **Typed Endpoints**: Individual endpoints per event type for perfect OpenAPI documentation
- ? **Generic Endpoint**: Polymorphic endpoint that accepts any event type
- ? **Batch Support**: Can ingest multiple datagrams in a single request
- ? **Fallback**: Automatically processes directly if RabbitMQ unavailable
- ? **Rate Limiting**: Uses the same rate limiting as UDP (via DatagramProcessor)
- ? **IP Tracking**: Preserves source IP for GeoIP and security
- ? **Full OpenAPI Schema**: Comprehensive documentation with examples in Scalar
- ? **Strongly Typed**: Each endpoint validates against its specific schema

## OpenAPI Documentation

The API provides **full OpenAPI 3.0 schema documentation** with individual schemas for each event type.

### Access OpenAPI Documentation

- **Scalar UI**: `https://node-api.packet.oarc.uk/scalar/v1`
- **Swagger JSON**: `https://node-api.packet.oarc.uk/swagger/v1/swagger.json`
- **Swagger UI** (if enabled): `https://node-api.packet.oarc.uk/swagger`

### Endpoint Design

The API provides **two ways** to submit datagrams:

1. **Typed Endpoints** (? Recommended for Scalar):
   - `/api/ingest/node-up` - POST NodeUpEvent
   - `/api/ingest/link-up` - POST LinkUpEvent
   - `/api/ingest/l2trace` - POST L2Trace
   - etc.
   - **Benefits**: Perfect OpenAPI schema, clear documentation, type-specific examples

2. **Generic Polymorphic Endpoint**:
   - `/api/ingest` - POST any NetworkEventDatagram (discriminated by `@type` field)
   - **Benefits**: Single endpoint for all types, flexible for generic clients

Both approaches use the exact same processing logic internally.

## API Endpoints

### Typed Endpoints (Recommended)

Each event type has its own dedicated endpoint with perfect OpenAPI documentation:

| Endpoint | Event Type | Description |
|----------|------------|-------------|
| `POST /api/ingest/node-up` | NodeUpEvent | Node comes online |
| `POST /api/ingest/node-status` | NodeStatus | Periodic node status |
| `POST /api/ingest/node-down` | NodeDownEvent | Node goes offline |
| `POST /api/ingest/link-up` | LinkUpEvent | Layer 2 link established |
| `POST /api/ingest/link-status` | LinkStatus | Periodic link status |
| `POST /api/ingest/link-down` | LinkDownEvent | Link disconnected |
| `POST /api/ingest/circuit-up` | CircuitUpEvent | Layer 4 circuit established |
| `POST /api/ingest/circuit-status` | CircuitStatus | Periodic circuit status |
| `POST /api/ingest/circuit-down` | CircuitDownEvent | Circuit disconnected |
| `POST /api/ingest/l2trace` | L2Trace | Layer 2 frame trace |

#### Example: POST /api/ingest/node-up

```bash
curl -X POST https://node-api.packet.oarc.uk/api/ingest/node-up \
  -H "Content-Type: application/json" \
  -d '{
    "time": 1234567890,
    "nodeCall": "M0LTE-1",
    "nodeAlias": "MYLTE1",
    "locator": "IO91EC",
    "latitude": 51.5074,
    "longitude": -0.1278,
    "software": "xrlin",
    "version": "v504j"
  }'
```

**Note**: The `@type` discriminator field is NOT required for typed endpoints (it's implied by the endpoint URL).

#### Example: POST /api/ingest/link-up

```bash
curl -X POST https://node-api.packet.oarc.uk/api/ingest/link-up \
  -H "Content-Type: application/json" \
  -d '{
    "time": 1234567890,
    "node": "M0LTE-1",
    "id": 123,
    "direction": "outgoing",
    "port": "1",
    "local": "M0LTE-1",
    "remote": "G0ABC-2"
  }'
```

#### Example: POST /api/ingest/l2trace

```bash
curl -X POST https://node-api.packet.oarc.uk/api/ingest/l2trace \
  -H "Content-Type: application/json" \
  -d '{
    "reportFrom": "M0LTE-1",
    "time": 1234567890,
    "port": "1",
    "srce": "M0LTE-1",
    "dest": "G0ABC",
    "ctrl": 3,
    "l2Type": "UI",
    "cr": "C",
    "ilen": 64,
    "pid": 240,
    "ptcl": "DATA"
  }'
```

### Generic Polymorphic Endpoint

**Endpoint**: `POST /api/ingest`  
**Content-Type**: `application/json`

Accepts any datagram type, discriminated by the `@type` field.

#### Example with @type discriminator:

```bash
curl -X POST https://node-api.packet.oarc.uk/api/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "@type": "NodeUpEvent",
    "time": 1234567890,
    "nodeCall": "M0LTE-1",
    "nodeAlias": "MYLTE1",
    "locator": "IO91EC",
    "latitude": 51.5074,
    "longitude": -0.1278,
    "software": "xrlin",
    "version": "v504j"
  }'
```

**Note**: When using the generic endpoint, the `@type` field is REQUIRED.

### Response Format

All ingestion endpoints return the same response format:

**202 Accepted**:
```json
{
  "status": "queued",
  "message": "Datagram queued for processing via RabbitMQ",
  "type": "NodeUpEvent",
  "sourceIp": "192.0.2.1",
  "receivedAt": "2025-01-21T12:00:00.0000000Z"
}
```

**400 Bad Request** (Invalid JSON or failed validation):
```json
{
  "error": "Invalid JSON",
  "details": "..."
}
```

**503 Service Unavailable** (RabbitMQ unavailable):
```json
{
  "error": "Service unavailable",
  "message": "Message queue is not available"
}
```

### Batch Ingestion

**Endpoint**: `POST /api/ingest/batch`  
**Content-Type**: `application/json`

Accepts an array of datagrams. When using batch, each datagram **must** include the `@type` field.

```bash
curl -X POST https://node-api.packet.oarc.uk/api/ingest/batch \
  -H "Content-Type: application/json" \
  -d '[
    {
      "@type": "NodeUpEvent",
      "nodeCall": "M0LTE-1",
      "nodeAlias": "MYLTE1",
      "locator": "IO91EC",
      "software": "xrlin",
      "version": "v504j"
    },
    {
      "@type": "LinkUpEvent",
      "node": "M0LTE-1",
      "id": 123,
      "direction": "outgoing",
      "port": "1",
      "local": "M0LTE-1",
      "remote": "G0ABC-2"
    }
  ]'
```

**Response** (202 Accepted):
```json
{
  "status": "queued",
  "totalReceived": 2,
  "successCount": 2,
  "failureCount": 0,
  "errors": [],
  "sourceIp": "192.0.2.1",
  "receivedAt": "2025-01-21T12:00:00.0000000Z",
  "processingMode": "rabbitmq"
}
```

### Service Status

**Endpoint**: `GET /api/ingest/status`

Returns service status and lists all available endpoints.

```bash
curl https://node-api.packet.oarc.uk/api/ingest/status
```

**Response**:
```json
{
  "service": "datagram-ingest",
  "status": "operational",
  "rabbitMq": {
    "available": true,
    "mode": "queue-based"
  },
  "supportedTypes": [
    "NodeUpEvent",
    "NodeStatus",
    "NodeDownEvent",
    "LinkUpEvent",
    "LinkStatus",
    "LinkDownEvent",
    "CircuitUpEvent",
    "CircuitStatus",
    "CircuitDownEvent",
    "L2Trace"
  ],
  "endpoints": {
    "generic": "/api/ingest",
    "batch": "/api/ingest/batch",
    "nodeUp": "/api/ingest/node-up",
    "nodeStatus": "/api/ingest/node-status",
    "nodeDown": "/api/ingest/node-down",
    "linkUp": "/api/ingest/link-up",
    "linkStatus": "/api/ingest/link-status",
    "linkDown": "/api/ingest/link-down",
    "circuitUp": "/api/ingest/circuit-up",
    "circuitStatus": "/api/ingest/circuit-status",
    "circuitDown": "/api/ingest/circuit-down",
    "l2trace": "/api/ingest/l2trace",
    "status": "/api/ingest/status"
  }
}
```

## Choosing Between Typed and Generic Endpoints

### Use Typed Endpoints When:

? **Building a UI/Dashboard** - Clear, discoverable API  
? **Generating Client Code** - Perfect schema per endpoint  
? **Learning the API** - Scalar shows exact schema with examples  
? **Single Event Type** - Sending one type at a time  
? **Type Safety Matters** - Compile-time validation in clients

### Use Generic Endpoint When:

? **Dynamic Client** - Event type determined at runtime  
? **Forwarding Proxy** - Forwarding UDP to HTTP without inspection  
? **Legacy Compatibility** - Existing code using `@type` discriminator  
? **Batch Mixed Types** - Already have the array with `@type` fields

## Client Examples

### Python with Typed Endpoints

```python
import requests

# NodeUpEvent
response = requests.post(
    'https://node-api.packet.oarc.uk/api/ingest/node-up',
    json={
        'nodeCall': 'M0LTE-1',
        'nodeAlias': 'MYLTE1',
        'locator': 'IO91EC',
        'software': 'xrlin',
        'version': 'v504j'
    }
)

# LinkUpEvent
response = requests.post(
    'https://node-api.packet.oarc.uk/api/ingest/link-up',
    json={
        'node': 'M0LTE-1',
        'id': 123,
        'direction': 'outgoing',
        'port': '1',
        'local': 'M0LTE-1',
        'remote': 'G0ABC-2'
    }
)
```

### PowerShell with Typed Endpoints

```powershell
# NodeUpEvent
$body = @{
    nodeCall = "M0LTE-1"
    nodeAlias = "MYLTE1"
    locator = "IO91EC"
    software = "xrlin"
    version = "v504j"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://node-api.packet.oarc.uk/api/ingest/node-up" `
  -Method Post `
  -ContentType "application/json" `
  -Body $body
```

### JavaScript/TypeScript with Typed Endpoints

```typescript
// NodeUpEvent
const response = await fetch('https://node-api.packet.oarc.uk/api/ingest/node-up', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    nodeCall: 'M0LTE-1',
    nodeAlias: 'MYLTE1',
    locator: 'IO91EC',
    software: 'xrlin',
    version: 'v504j'
  })
});

// LinkUpEvent
const response = await fetch('https://node-api.packet.oarc.uk/api/ingest/link-up', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    node: 'M0LTE-1',
    id: 123,
    direction: 'outgoing',
    port: '1',
    local: 'M0LTE-1',
    remote: 'G0ABC-2'
  })
});
```

### Generic Endpoint (with @type)

```python
import requests

# Using generic endpoint with @type discriminator
response = requests.post(
    'https://node-api.packet.oarc.uk/api/ingest',
    json={
        '@type': 'NodeUpEvent',  # Required for generic endpoint
        'nodeCall': 'M0LTE-1',
        'nodeAlias': 'MYLTE1',
        'locator': 'IO91EC',
        'software': 'xrlin',
        'version': 'v504j'
    }
)
```

## Event Type Schemas

All event type schemas are fully documented in the OpenAPI specification at `/scalar/v1`. Here are the key fields for each type:

### NodeUpEvent
```json
{
  "time": 1234567890,          // Unix timestamp (optional)
  "nodeCall": "M0LTE-1",       // Required
  "nodeAlias": "MYLTE1",       // Required
  "locator": "IO91EC",         // Required (Maidenhead)
  "latitude": 51.5074,         // Optional
  "longitude": -0.1278,        // Optional
  "software": "xrlin",         // Required
  "version": "v504j"           // Required
}
```

### LinkUpEvent
```json
{
  "time": 1234567890,          // Unix timestamp (optional)
  "node": "M0LTE-1",           // Required
  "id": 123,                   // Required (>0)
  "direction": "outgoing",     // Required ("incoming"/"outgoing")
  "port": "1",                 // Required
  "local": "M0LTE-1",          // Required
  "remote": "G0ABC-2"          // Required
}
```

### L2Trace
```json
{
  "reportFrom": "M0LTE-1",     // Required
  "time": 1234567890,          // Unix timestamp (optional)
  "port": "1",                 // Required
  "srce": "M0LTE-1",           // Required
  "dest": "G0ABC",             // Required
  "ctrl": 3,                   // Required (>=0)
  "l2Type": "UI",              // Required
  "cr": "C",                   // Required
  "ilen": 64,                  // Optional (for I/UI frames)
  "pid": 240,                  // Optional
  "ptcl": "DATA"               // Optional
  // ...many more optional fields
}
```

See `/scalar/v1` for complete schemas with all fields and validation rules.

## Benefits of This Design

### For API Consumers

? **Perfect Scalar Documentation** - Each typed endpoint shows exact schema  
? **Clear API Surface** - Browse endpoints, see what each accepts  
? **Type-Specific Examples** - Every endpoint has relevant examples  
? **Better Validation Errors** - Know immediately what's wrong  
? **Client Generation** - Generate type-safe clients from OpenAPI

### For the Service

? **No Code Duplication** - All endpoints share same implementation  
? **Consistent Processing** - Same validation, same pipeline  
? **Backward Compatible** - Generic endpoint still works  
? **Flexible** - Use typed or generic based on your needs

## Processing Pipeline

### With RabbitMQ Available (Default)

```
HTTP POST ? DatagramIngestController
              ? (typed or generic endpoint)
          RabbitMQ Publisher
              ? (serialize to JSON, queue: udp-datagram-queue)
          RabbitMQ Queue
              ?
          RabbitMQ Consumer
              ? (deserialize to strongly-typed model)
          DatagramProcessor
              ?
          FluentValidation
              ?
          MQTT Publisher
              ?
          MqttStateSubscriber
              ?
          Network State Updated
```

## Comparison: Typed vs Generic vs UDP

| Feature | UDP Ingestion | Generic HTTP | Typed HTTP |
|---------|--------------|--------------|-----------|
| **Protocol** | UDP (port 13579) | HTTP POST | HTTP POST |
| **Endpoint** | N/A | `/api/ingest` | `/api/ingest/node-up`, etc. |
| **@type Required** | Yes | Yes | No (implied by URL) |
| **Firewall** | May be blocked | Usually allowed | Usually allowed |
| **Schema Docs** | None | Generic `object` | Perfect per-type schema |
| **Discoverability** | None | Low | High |
| **Client Gen** | Manual | Generic | Type-safe per endpoint |
| **Processing** | Identical | Identical | Identical |

## Migration Guide

### From UDP to HTTP

**Before (UDP)**:
```python
import socket
import json

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.sendto(json.dumps({
    '@type': 'NodeUpEvent',
    'nodeCall': 'M0LTE-1',
    # ...
}).encode(), ('node-api.packet.oarc.uk', 13579))
```

**After (HTTP - Typed Endpoint)**:
```python
import requests

requests.post(
    'https://node-api.packet.oarc.uk/api/ingest/node-up',
    json={
        'nodeCall': 'M0LTE-1',  # No @type needed!
        # ...
    }
)
```

**After (HTTP - Generic Endpoint)**:
```python
import requests

requests.post(
    'https://node-api.packet.oarc.uk/api/ingest',
    json={
        '@type': 'NodeUpEvent',  # @type still required
        'nodeCall': 'M0LTE-1',
        # ...
    }
)
```

### Updating Existing HTTP Clients

If you're already using the generic `/api/ingest` endpoint, **no changes required**. The generic endpoint continues to work exactly as before.

To benefit from better documentation and type safety, consider migrating to typed endpoints:

**Before**:
```javascript
fetch('/api/ingest', {
  method: 'POST',
  body: JSON.stringify({ '@type': 'NodeUpEvent', nodeCall: 'M0LTE-1', ... })
})
```

**After**:
```javascript
fetch('/api/ingest/node-up', {  // Specific endpoint
  method: 'POST',
  body: JSON.stringify({ nodeCall: 'M0LTE-1', ... })  // No @type
})
```

## Related Documentation

- [RabbitMQ Integration](RABBITMQ_INTEGRATION.md)
- [Packet Network Monitoring Spec](../Tests/Packet_Network_Monitoring_Project_v0.8.txt)
- [OpenAPI Specification](https://node-api.packet.oarc.uk/swagger/v1/swagger.json)
- [Scalar Interactive Docs](https://node-api.packet.oarc.uk/scalar/v1)

---

**Status**: ? **Production Ready with Typed Endpoints**  
**Version**: 3.0  
**Breaking Change**: None (backward compatible)  
**New Feature**: Individual typed endpoints with perfect OpenAPI documentation  
**Recommendation**: Use typed endpoints (`/api/ingest/node-up`, etc.) for best Scalar experience
