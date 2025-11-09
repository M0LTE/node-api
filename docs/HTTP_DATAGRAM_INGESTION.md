# HTTP Datagram Ingestion API

**Date**: 2025-01-21  
**Status**: ? Implemented

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

## API Endpoints

### 1. Single Datagram Ingestion

**Endpoint**: `POST /api/ingest`  
**Content-Type**: `application/json`

Ingest a single network event datagram.

#### Request Body

Any valid network event datagram type:

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

#### Response

**202 Accepted** (RabbitMQ available):
```json
{
  "status": "queued",
  "message": "Datagram queued for processing via RabbitMQ",
  "sourceIp": "192.0.2.1",
  "receivedAt": "2025-01-21T12:00:00.0000000Z"
}
```

**202 Accepted** (RabbitMQ unavailable - direct processing):
```json
{
  "status": "processed",
  "message": "Datagram processed directly (RabbitMQ unavailable)",
  "sourceIp": "192.0.2.1",
  "receivedAt": "2025-01-21T12:00:00.0000000Z"
}
```

**400 Bad Request** (Invalid JSON):
```json
{
  "error": "Invalid JSON",
  "details": "..."
}
```

#### Example Usage

```bash
# Using curl
curl -X POST https://node-api.packet.oarc.uk/api/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "@type": "NodeStatusReportEvent",
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

# Using Python
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

Array of datagram objects:

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
    "l2Type": "I",
    "srce": "M0LTE-1",
    "dest": "G0ABC-2"
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
    "Datagram 1: Invalid JSON format"
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

## Supported Event Types

All existing UDP datagram types are supported:

### Node Events
- `NodeUpEvent` - Node comes online
- `NodeStatusReportEvent` - Periodic node status
- `NodeDownEvent` - Node goes offline

### Link Events
- `LinkUpEvent` - Layer 2 link established
- `LinkStatus` - Periodic link status
- `LinkDisconnectionEvent` - Link disconnected

### Circuit Events
- `CircuitUpEvent` - NetROM Layer 4 circuit established
- `CircuitStatus` - Periodic circuit status
- `CircuitDisconnectionEvent` - Circuit disconnected

### Trace Events
- `L2Trace` - Layer 2 frame trace

For detailed schemas, see the [Event Types Documentation](../README.md#event-types).

## Processing Pipeline

### With RabbitMQ Available (Default)

```
HTTP POST ? DatagramIngestController
              ?
          RabbitMQ Publisher
              ? (queue: udp-datagram-queue)
          RabbitMQ Queue
              ?
          RabbitMQ Consumer
              ?
          DatagramProcessor
              ?
          Rate Limiting & Validation
              ?
          MQTT Publisher
              ?
          MqttStateSubscriber
              ?
          Network State Updated
```

### Without RabbitMQ (Fallback)

```
HTTP POST ? DatagramIngestController
              ?
          DatagramProcessor
              ?
          Rate Limiting & Validation
              ?
          MQTT Publisher
              ?
          MqttStateSubscriber
              ?
          Network State Updated
```

## Rate Limiting

HTTP ingestion uses the **same rate limiting** as UDP ingestion:

- Per-IP rate limiting (default: 25 requests/second with burst support)
- CIDR blacklist for malicious sources
- Automatic temporary blocks for excessive rates
- All rate limiting is handled in `DatagramProcessor`

Source IP is extracted from:
1. `X-Forwarded-For` header (if present, uses first IP)
2. `HttpContext.Connection.RemoteIpAddress` (direct connection)

## Security Considerations

### IP Address Handling

- Source IP is extracted from HTTP headers for rate limiting
- GeoIP tracking uses the same obfuscation as UDP (last 2 octets only)
- Supports `X-Forwarded-For` header for proxy/load balancer scenarios

### Authentication

Currently **no authentication** is required. Consider adding:

1. **API Key Authentication**:
```csharp
[Authorize(AuthenticationSchemes = "ApiKey")]
[HttpPost]
public async Task<IActionResult> IngestDatagramAsync(...)
```

2. **Rate Limiting per API Key**:
- Track usage by API key instead of IP
- Enforce different rate limits per key

3. **IP Whitelist**:
- Only allow submissions from known nodes
- Reject unknown IPs at ingress

### CORS

If accessed from web browsers, configure CORS in `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("IngestPolicy", policy =>
    {
        policy.WithOrigins("https://trusted-source.example.com")
              .AllowAnyHeader()
              .WithMethods("POST");
    });
});

// ...

app.UseCors("IngestPolicy");
```

## Use Cases

### 1. Node Software Integration

XRouter nodes can submit telemetry via HTTP in addition to UDP:

```python
# Python example for XRouter node
import requests
import json

def send_node_status(callsign, alias, locator, software, version, uptime_secs):
    datagram = {
        "@type": "NodeStatusReportEvent",
        "time": int(time.time()),
        "nodeCall": callsign,
        "nodeAlias": alias,
        "locator": locator,
        "software": software,
        "version": version,
        "uptimeSecs": uptime_secs
    }
    
    response = requests.post(
        "https://node-api.packet.oarc.uk/api/ingest",
        headers={"Content-Type": "application/json"},
        data=json.dumps(datagram)
    )
    
    return response.status_code == 202

# Send status every 60 seconds
while True:
    send_node_status("M0LTE-1", "MYLTE1", "IO91EC", "xrlin", "v504j", 12345)
    time.sleep(60)
```

### 2. Bulk Historical Import

Import historical data from logs:

```bash
# Convert log file to JSON array
cat historical_events.log | jq -s '.' > batch.json

# Submit as batch
curl -X POST https://node-api.packet.oarc.uk/api/ingest/batch \
  -H "Content-Type: application/json" \
  --data @batch.json
```

### 3. External Monitoring Tools

Integrate with existing monitoring systems:

```javascript
// Node.js example
const axios = require('axios');

async function reportNodeUp(nodeData) {
  try {
    const response = await axios.post(
      'https://node-api.packet.oarc.uk/api/ingest',
      {
        "@type": "NodeUpEvent",
        ...nodeData
      },
      {
        headers: { 'Content-Type': 'application/json' }
      }
    );
    
    console.log('Status:', response.data.status);
    return true;
  } catch (error) {
    console.error('Error:', error.message);
    return false;
  }
}
```

### 4. Testing & Development

Easy testing without UDP client:

```bash
# Quick test of a NodeUpEvent
curl -X POST http://localhost:5000/api/ingest \
  -H "Content-Type: application/json" \
  -d '{"@type":"NodeUpEvent","nodeCall":"TEST-1","nodeAlias":"TEST","locator":"IO91EC","software":"test","version":"v1"}'

# Verify in MQTT
mosquitto_sub -h localhost -t "out/#" -v
```

## Monitoring

### Logs

HTTP ingestion logs to the same logger as UDP:

```
info: DatagramIngestController[0]
      HTTP ingest from 192.0.2.1: 256 bytes

debug: DatagramIngestController[0]
       Published HTTP datagram from 192.0.2.1 to RabbitMQ

debug: DatagramProcessor[0]
       Processing datagram from RabbitMQ: 192.0.2.1
```

### Metrics

Same metrics as UDP ingestion:
- MQTT topic: `metrics/system/{hostname}`
- Rate limit blocks: `metrics/ratelimit`
- Network state updates: MQTT `out/#` topics

## Performance

### Single Datagram Ingestion

- **Latency**: ~10-50ms (RabbitMQ queue)
- **Throughput**: Limited by rate limiting (25/sec per IP by default)
- **Overhead**: Similar to UDP ingestion + HTTP overhead

### Batch Ingestion

- **Latency**: ~50-200ms (depends on batch size)
- **Throughput**: Much higher than single ingestion
- **Recommended**: Use batch endpoint for bulk imports

### Scalability

- HTTP ingestion scales horizontally (multiple instances)
- RabbitMQ queue ensures messages aren't lost during scaling
- Same concurrency controls as UDP (100 concurrent processing by default)

## Comparison: HTTP vs UDP

| Feature | UDP Ingestion | HTTP Ingestion |
|---------|--------------|----------------|
| **Protocol** | UDP datagrams (port 13579) | HTTP POST (port 443/80) |
| **Reliability** | Fire-and-forget | Acknowledged (202 response) |
| **Firewall** | May be blocked | Usually allowed |
| **Authentication** | None | Can add API keys |
| **Batch Support** | No | Yes (`/api/ingest/batch`) |
| **Processing** | Identical (via DatagramProcessor) | Identical (via DatagramProcessor) |
| **Rate Limiting** | Yes | Yes (same limits) |
| **RabbitMQ** | Publishes to queue | Publishes to same queue |
| **Use Case** | Real-time node telemetry | External tools, testing, bulk import |

## Troubleshooting

### "503 Service Unavailable" Error

**Cause**: RabbitMQ is configured but unavailable  
**Solution**: Check RabbitMQ connection, or remove RabbitMQ environment variables to use direct processing

### Datagrams Not Appearing in Network State

**Cause**: Validation failure  
**Solution**: Check MQTT topic `in/udp/errored/validation` for validation errors

### Rate Limited

**Cause**: Exceeding 25 requests/second from single IP  
**Solution**: Use batch endpoint, or increase rate limit in configuration

### "Invalid JSON" Error

**Cause**: Malformed JSON in request  
**Solution**: Validate JSON format, check for syntax errors

## Future Enhancements

1. **Authentication**:
   - API key authentication
   - OAuth2 / JWT tokens
   - Per-key rate limiting

2. **Compression**:
   - Accept gzip/deflate compressed payloads
   - Reduce bandwidth for large batches

3. **WebSocket Support**:
   - Bidirectional real-time streaming
   - Receive acknowledgments instantly

4. **GraphQL Endpoint**:
   - Flexible ingestion with schema validation
   - Query capabilities alongside ingestion

5. **Async Processing Status**:
   - Return job ID for batch operations
   - Query processing status via `/api/ingest/jobs/{id}`

## Related Documentation

- [RabbitMQ Integration](RABBITMQ_INTEGRATION.md)
- [Event Types](../README.md#event-types)
- [Rate Limiting](RATE_LIMITING.md)
- [API Documentation](../README.md#api-endpoints)

---

**Status**: ? **Production Ready**  
**Version**: 1.0  
**Compatibility**: All existing event types supported
