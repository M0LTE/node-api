# HTTP Datagram Ingestion API

**Date**: 2025-01-21  
**Status**: ? Implemented with **OpenAPI Schema Support**

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
- ? **Same Event Types**: Accepts all existing datagram types (NodeUpEvent, LinkStatus, etc.)
- ? **Batch Support**: Can ingest multiple datagrams in a single request
- ? **Fallback**: Automatically processes directly if RabbitMQ unavailable
- ? **Rate Limiting**: Uses the same rate limiting as UDP (via DatagramProcessor)
- ? **IP Tracking**: Preserves source IP for GeoIP and security
- ? **OpenAPI Schema**: Full OpenAPI/Swagger documentation with polymorphic discriminator support
- ? **Strongly Typed**: Validates against specific event type schemas

## OpenAPI Documentation

The API provides **full OpenAPI 3.0 schema documentation** with support for polymorphic types using JSON discriminators.

### Access OpenAPI Documentation

- **Scalar UI**: `https://node-api.packet.oarc.uk/scalar/v1`
- **Swagger JSON**: `https://node-api.packet.oarc.uk/swagger/v1/swagger.json`
- **Swagger UI** (if enabled): `https://node-api.packet.oarc.uk/swagger`

### Polymorphic Type Support

The API uses **JSON polymorphism** with the `@type` discriminator field to support multiple event types:

```json
{
  "@type": "NodeUpEvent",  // Discriminator field
  "nodeCall": "M0LTE-1",
  // ... other NodeUpEvent-specific fields
}
```

OpenAPI automatically generates:
- **Separate schemas** for each event type (NodeUpEvent, LinkUpEvent, L2Trace, etc.)
- **Union type** at the endpoint level (oneOf discriminator)
- **Type-specific validation** based on the `@type` field
- **IntelliSense/autocomplete** in API clients

### Supported Event Types (Discriminated by `@type`)

| @type | Schema | Description |
|-------|--------|-------------|
| `NodeUpEvent` | [NodeUpEvent](#nodeupevent-schema) | Node comes online |
| `NodeStatus` | [NodeStatusReportEvent](#nodestatusreportevent-schema) | Periodic node status |
| `NodeDownEvent` | [NodeDownEvent](#nodedownevent-schema) | Node goes offline |
| `LinkUpEvent` | [LinkUpEvent](#linkupevent-schema) | Layer 2 link established |
| `LinkStatus` | [LinkStatus](#linkstatus-schema) | Periodic link status |
| `LinkDownEvent` | [LinkDisconnectionEvent](#linkdisconnectionevent-schema) | Link disconnected |
| `CircuitUpEvent` | [CircuitUpEvent](#circuitupevent-schema) | Layer 4 circuit established |
| `CircuitStatus` | [CircuitStatus](#circuitstatus-schema) | Periodic circuit status |
| `CircuitDownEvent` | [CircuitDisconnectionEvent](#circuitdisconnectionevent-schema) | Circuit disconnected |
| `L2Trace` | [L2Trace](#l2trace-schema) | Layer 2 frame trace |

## API Endpoints

### 1. Single Datagram Ingestion

**Endpoint**: `POST /api/ingest`  
**Content-Type**: `application/json`

Ingest a single network event datagram.

#### Request Body

Accepts `UdpNodeInfoJsonDatagram` (polymorphic type discriminated by `@type` field).

**Example: NodeUpEvent**
```json
{
  "@type": "NodeUpEvent",
  "time": 1234567890,
  "nodeCall": "M0LTE-1",
  "nodeAlias": "MYLTE1",
  "locator": "IO91EC",
  "latitude": 51.5074,
  "longitude": -0.1278,
  "software": "xrlin",
  "version": "v504j"
}
```

**Example: LinkUpEvent**
```json
{
  "@type": "LinkUpEvent",
  "time": 1234567890,
  "node": "M0LTE-1",
  "id": 123,
  "direction": "outgoing",
  "port": "1",
  "local": "M0LTE-1",
  "remote": "G0ABC-2"
}
```

**Example: L2Trace**
```json
{
  "@type": "L2Trace",
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
}
```

#### Response

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

#### Example Usage

```bash
# Using curl
curl -X POST https://node-api.packet.oarc.uk/api/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "@type": "NodeStatus",
    "time": 1737465600,
    "nodeCall": "M0LTE-1",
    "nodeAlias": "MYLTE1",
    "locator": "IO91EC",
    "latitude": 51.5074,
    "longitude": -0.1278,
    "software": "xrlin",
    "version": "v504j",
    "uptimeSecs": 12345,
    "linksIn": 2,
    "linksOut": 3,
    "cctsIn": 1,
    "cctsOut": 2,
    "l3Relayed": 150
  }'

# Using PowerShell
$body = @{
    "@type" = "NodeUpEvent"
    "nodeCall" = "M0LTE-1"
    "nodeAlias" = "MYLTE1"
    "locator" = "IO91EC"
    "latitude" = 51.5074
    "longitude" = -0.1278
    "software" = "xrlin"
    "version" = "v504j"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://node-api.packet.oarc.uk/api/ingest" `
  -Method Post `
  -ContentType "application/json" `
  -Body $body

# Using Python with requests
import requests
import json

datagram = {
    "@type": "NodeUpEvent",
    "nodeCall": "M0LTE-1",
    "nodeAlias": "MYLTE1",
    "locator": "IO91EC",
    "latitude": 51.5074,
    "longitude": -0.1278,
    "software": "xrlin",
    "version": "v504j"
}

response = requests.post(
    "https://node-api.packet.oarc.uk/api/ingest",
    headers={"Content-Type": "application/json"},
    data=json.dumps(datagram)
)

print(response.status_code)
print(response.json())
```

### 2. Batch Datagram Ingestion

**Endpoint**: `POST /api/ingest/batch`  
**Content-Type**: `application/json`

Ingest multiple network event datagrams in a single request.

#### Request Body

Array of `UdpNodeInfoJsonDatagram` objects (each discriminated by `@type`):

```json
[
  {
    "@type": "NodeUpEvent",
    "nodeCall": "M0LTE-1",
    "nodeAlias": "MYLTE1",
    "locator": "IO91EC",
    "latitude": 51.5074,
    "longitude": -0.1278,
    "software": "xrlin",
    "version": "v504j"
  },
  {
    "@type": "LinkUpEvent",
    "time": 1234567890,
    "node": "M0LTE-1",
    "id": 123,
    "direction": "outgoing",
    "port": "1",
    "local": "M0LTE-1",
    "remote": "G0ABC-2"
  },
  {
    "@type": "L2Trace",
    "time": 1234567890,
    "reportFrom": "M0LTE-1",
    "port": "1",
    "srce": "M0LTE-1",
    "dest": "G0ABC-2",
    "ctrl": 0,
    "l2Type": "I",
    "cr": "C"
  }
]
```

#### Response

**202 Accepted** (All successful):
```json
{
  "status": "queued",
  "totalReceived": 3,
  "successCount": 3,
  "failureCount": 0,
  "errors": [],
  "sourceIp": "192.0.2.1",
  "receivedAt": "2025-01-21T12:00:00.0000000Z",
  "processingMode": "rabbitmq"
}
```

**207 Multi-Status** (Partial success):
```json
{
  "status": "partial",
  "totalReceived": 3,
  "successCount": 2,
  "failureCount": 1,
  "errors": [
    "Datagram 1 (LinkUpEvent): Invalid JSON format"
  ],
  "sourceIp": "192.0.2.1",
  "receivedAt": "2025-01-21T12:00:00.0000000Z",
  "processingMode": "rabbitmq"
}
```

#### Example Usage

```bash
# Using curl
curl -X POST https://node-api.packet.oarc.uk/api/ingest/batch \
  -H "Content-Type": application/json" \
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

# Using Python
import requests
import json

datagrams = [
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
]

response = requests.post(
    "https://node-api.packet.oarc.uk/api/ingest/batch",
    headers={"Content-Type": "application/json"},
    data=json.dumps(datagrams)
)

print(response.json())
```

### 3. Service Status

**Endpoint**: `GET /api/ingest/status`

Check the status of the ingestion service.

#### Response

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
    "singleIngest": "/api/ingest",
    "batchIngest": "/api/ingest/batch",
    "status": "/api/ingest/status"
  }
}
```

#### Example Usage

```bash
# Check service status
curl https://node-api.packet.oarc.uk/api/ingest/status
```

## Event Type Schemas

### NodeUpEvent Schema

```json
{
  "@type": "NodeUpEvent",
  "time": 1234567890,          // Unix timestamp (optional)
  "nodeCall": "M0LTE-1",       // Required
  "nodeAlias": "MYLTE1",       // Required
  "locator": "IO91EC",         // Required (Maidenhead locator)
  "latitude": 51.5074,         // Optional (decimal degrees)
  "longitude": -0.1278,        // Optional (decimal degrees)
  "software": "xrlin",         // Required
  "version": "v504j"           // Required
}
```

### LinkUpEvent Schema

```json
{
  "@type": "LinkUpEvent",
  "time": 1234567890,          // Unix timestamp (optional)
  "node": "M0LTE-1",           // Required
  "id": 123,                   // Required (link ID > 0)
  "direction": "outgoing",     // Required ("incoming" or "outgoing")
  "port": "1",                 // Required
  "local": "M0LTE-1",          // Required
  "remote": "G0ABC-2"          // Required
}
```

### L2Trace Schema

```json
{
  "@type": "L2Trace",
  "reportFrom": "M0LTE-1",     // Required
  "time": 1234567890,          // Unix timestamp (optional)
  "port": "1",                 // Required
  "dirn": "sent",              // Optional ("sent" or "rcvd")
  "isRF": true,                // Optional (boolean)
  "srce": "M0LTE-1",           // Required
  "dest": "G0ABC",             // Required
  "ctrl": 3,                   // Required (>= 0)
  "l2Type": "UI",              // Required (SABME, C, D, DM, UA, UI, I, FRMR, RR, RNR, REJ, TEST, XID, SREJ, ?)
  "cr": "C",                   // Required (C, R, or V1)
  "modulo": 8,                 // Optional (8 or 128)
  "ilen": 64,                  // Optional (>= 0, for I and UI frames)
  "pid": 240,                  // Optional
  "ptcl": "DATA",              // Optional (SEG, DATA, NET/ROM, IP, ARP, FLEXNET, ?)
  "digis": [                   // Optional
    {
      "call": "DIGI1",
      "rptd": true
    }
  ]
  // ... many more optional fields for NET/ROM, routing, etc.
}
```

For complete schemas, refer to the OpenAPI documentation at `/scalar/v1`.

## Benefits of Strong Typing

### 1. **Better Developer Experience**

- **IntelliSense**: IDEs can autocomplete fields based on the `@type`
- **Type safety**: Client libraries can generate strongly-typed classes
- **Validation**: Errors caught at serialization time, not processing time

### 2. **Automatic Documentation**

- **OpenAPI/Swagger**: Full schema documentation generated automatically
- **Examples**: Each event type has example payloads
- **Field descriptions**: Every field documented with types and constraints

### 3. **Client Generation**

Generate strongly-typed clients in any language:

```bash
# Generate TypeScript client
npx @openapitools/openapi-generator-cli generate \
  -i https://node-api.packet.oarc.uk/swagger/v1/swagger.json \
  -g typescript-fetch \
  -o ./src/api

# Generate C# client
dotnet swagger tofile --output swagger.json node-api.dll v1
NSwag openapi2csclient /input:swagger.json /classname:NodeApiClient /namespace:NodeApi.Client
```

### 4. **Validation at Multiple Layers**

1. **JSON Schema Validation**: ASP.NET Core validates against OpenAPI schema
2. **FluentValidation**: Business rule validation (callsign format, ranges, etc.)
3. **Type Safety**: Compiler ensures correct field types

## Processing Pipeline

### With RabbitMQ Available (Default)

```
HTTP POST ? DatagramIngestController
              ? (deserialize to strongly-typed model)
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

## Comparison: HTTP vs UDP

| Feature | UDP Ingestion | HTTP Ingestion |
|---------|--------------|----------------|
| **Protocol** | UDP datagrams (port 13579) | HTTP POST (port 443/80) |
| **Reliability** | Fire-and-forget | Acknowledged (202 response) |
| **Firewall** | May be blocked | Usually allowed |
| **Schema Documentation** | None | Full OpenAPI/Swagger |
| **Type Safety** | Runtime only | Compile-time + Runtime |
| **Batch Support** | No | Yes (`/api/ingest/batch`) |
| **Processing** | Identical (via DatagramProcessor) | Identical (via DatagramProcessor) |
| **Rate Limiting** | Yes | Yes (same limits) |
| **Client Generation** | Manual | Automatic (from OpenAPI) |

## Related Documentation

- [RabbitMQ Integration](RABBITMQ_REFACTORING.md)
- [Packet Network Monitoring Spec](../Tests/Packet_Network_Monitoring_Project_v0.8.txt)
- [OpenAPI Specification](https://node-api.packet.oarc.uk/swagger/v1/swagger.json)

---

**Status**: ? **Production Ready with Full OpenAPI Schema Support**  
**Version**: 2.0  
**Breaking Change**: None (backward compatible)
**New Feature**: Strongly-typed schemas with OpenAPI polymorphism
