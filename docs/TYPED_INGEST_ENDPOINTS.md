# Typed Ingestion Endpoints

**Date**: 2025-01-21  
**Status**: ? Implemented  
**Version**: 3.0

## Overview

Added **individual typed endpoints** for each event type to provide perfect OpenAPI/Scalar documentation while maintaining the existing generic polymorphic endpoint for backward compatibility.

## Problem

The generic `/api/ingest` endpoint accepts a polymorphic `NetworkEventDatagram` with JSON discriminator (`@type` field). While this works perfectly at runtime, **OpenAPI/Scalar cannot automatically detect the derived types** from System.Text.Json's `[JsonPolymorphic]` attributes.

### What Users Saw in Scalar

```yaml
POST /api/ingest
  requestBody:
    schema:
      type: object  # ? Generic object with no structure!
      additionalProperties: true
```

**Problems**:
- ? No schema documentation
- ? No field descriptions
- ? No examples
- ? No IntelliSense in API clients
- ? Hard to discover what fields are needed

## Solution

Created **individual typed endpoints** for each event type alongside the generic endpoint:

### New Endpoints

| Endpoint | Accepts | Schema in Scalar |
|----------|---------|------------------|
| `POST /api/ingest/node-up` | `NodeUpEvent` | ? Full schema |
| `POST /api/ingest/node-status` | `NodeStatusReportEvent` | ? Full schema |
| `POST /api/ingest/node-down` | `NodeDownEvent` | ? Full schema |
| `POST /api/ingest/link-up` | `LinkUpEvent` | ? Full schema |
| `POST /api/ingest/link-status` | `LinkStatus` | ? Full schema |
| `POST /api/ingest/link-down` | `LinkDisconnectionEvent` | ? Full schema |
| `POST /api/ingest/circuit-up` | `CircuitUpEvent` | ? Full schema |
| `POST /api/ingest/circuit-status` | `CircuitStatus` | ? Full schema |
| `POST /api/ingest/circuit-down` | `CircuitDisconnectionEvent` | ? Full schema |
| `POST /api/ingest/l2trace` | `L2Trace` | ? Full schema |

### Existing Endpoints (Unchanged)

| Endpoint | Description |
|----------|-------------|
| `POST /api/ingest` | Generic polymorphic endpoint (requires `@type` field) |
| `POST /api/ingest/batch` | Batch ingestion (requires `@type` for each item) |
| `GET /api/ingest/status` | Service status |

## Implementation

### Controller Changes

All typed endpoints share a single private implementation method:

```csharp
[HttpPost("node-up")]
public Task<IActionResult> IngestNodeUpEventAsync([FromBody] NodeUpEvent datagram)
    => IngestTypedDatagramAsync(datagram);

[HttpPost("link-up")]
public Task<IActionResult> IngestLinkUpEventAsync([FromBody] LinkUpEvent datagram)
    => IngestTypedDatagramAsync(datagram);

// ... etc for all event types

private async Task<IActionResult> IngestTypedDatagramAsync(NetworkEventDatagram datagram)
{
    // Shared implementation - same as before
    // Validates, serializes, publishes to RabbitMQ
}
```

**Benefits**:
- ? **No code duplication** - Single implementation method
- ? **Type-safe** - Each endpoint accepts specific type
- ? **Consistent** - Same validation, same processing
- ? **Maintainable** - Change logic in one place

### What Users Now See in Scalar

```yaml
POST /api/ingest/node-up
  requestBody:
    schema:
      type: object
      required:
        - nodeCall
        - nodeAlias
        - locator
        - software
        - version
      properties:
        time:
          type: integer
          format: int64
          description: Unix timestamp (seconds since 1970-01-01)
        nodeCall:
          type: string
          description: Node callsign (e.g., M0LTE-1)
        nodeAlias:
          type: string
          description: Node alias (e.g., MYLTE1)
        # ... full schema with descriptions
```

## Usage Examples

### Typed Endpoint (Recommended)

```bash
# NodeUpEvent - no @type field needed
curl -X POST https://node-api.packet.oarc.uk/api/ingest/node-up \
  -H "Content-Type: application/json" \
  -d '{
    "nodeCall": "M0LTE-1",
    "nodeAlias": "MYLTE1",
    "locator": "IO91EC",
    "software": "xrlin",
    "version": "v504j"
  }'

# LinkUpEvent - no @type field needed
curl -X POST https://node-api.packet.oarc.uk/api/ingest/link-up \
  -H "Content-Type: application/json" \
  -d '{
    "node": "M0LTE-1",
    "id": 123,
    "direction": "outgoing",
    "port": "1",
    "local": "M0LTE-1",
    "remote": "G0ABC-2"
  }'
```

**Note**: The `@type` field is NOT required for typed endpoints.

### Generic Endpoint (Still Works)

```bash
# Generic endpoint - @type field required
curl -X POST https://node-api.packet.oarc.uk/api/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "@type": "NodeUpEvent",
    "nodeCall": "M0LTE-1",
    "nodeAlias": "MYLTE1",
    "locator": "IO91EC",
    "software": "xrlin",
    "version": "v504j"
  }'
```

## Benefits

### For API Consumers

? **Perfect Documentation** - Scalar shows exact schema per endpoint  
? **Discoverable** - Browse endpoints, see what each accepts  
? **Type-Specific Examples** - Every endpoint has relevant examples  
? **Better Validation Errors** - Know immediately what's missing  
? **Client Generation** - Generate strongly-typed clients  
? **IntelliSense Support** - IDEs can autocomplete based on schema  
? **No @type Confusion** - Type implied by URL

### For the Service

? **No Code Duplication** - Shared implementation  
? **Backward Compatible** - Generic endpoint unchanged  
? **Consistent Processing** - Same validation pipeline  
? **Easy to Extend** - Add new types by following pattern  
? **Type Safety** - Controller-level type checking

## When to Use Which Endpoint

### Use Typed Endpoints (`/api/ingest/node-up`, etc.)

? **Building a UI/Dashboard** - Clear, discoverable API  
? **Generating Client Code** - Perfect schema per endpoint  
? **Learning the API** - Scalar shows exact requirements  
? **Single Event Type** - Sending one type at a time  
? **Type Safety** - Want compile-time validation

### Use Generic Endpoint (`/api/ingest`)

? **Dynamic Client** - Event type determined at runtime  
? **UDP-to-HTTP Proxy** - Forwarding without inspection  
? **Legacy Compatibility** - Already using `@type` field  
? **Runtime Type Selection** - Type not known at compile time

### Use Batch Endpoint (`/api/ingest/batch`)

? **Multiple Events** - Sending many events at once  
? **Mixed Types** - Different event types in one request  
? **Performance** - Reduce HTTP request overhead

## API Status Endpoint

The `/api/ingest/status` endpoint now lists all available endpoints:

```json
{
  "service": "datagram-ingest",
  "status": "operational",
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

## Backward Compatibility

### Breaking Changes

**None** - This is a **fully backward-compatible addition**.

### Existing Clients

All existing clients using `/api/ingest` continue to work exactly as before:

```python
# This still works - no changes needed
requests.post('/api/ingest', json={
    '@type': 'NodeUpEvent',
    'nodeCall': 'M0LTE-1',
    # ...
})
```

### Migration Path

Optional migration for better documentation:

**Before**:
```python
# Generic endpoint with @type
requests.post('/api/ingest', json={
    '@type': 'NodeUpEvent',
    'nodeCall': 'M0LTE-1',
    'nodeAlias': 'MYLTE1',
    'locator': 'IO91EC',
    'software': 'xrlin',
    'version': 'v504j'
})
```

**After** (optional):
```python
# Typed endpoint without @type
requests.post('/api/ingest/node-up', json={
    'nodeCall': 'M0LTE-1',  # No @type field
    'nodeAlias': 'MYLTE1',
    'locator': 'IO91EC',
    'software': 'xrlin',
    'version': 'v504j'
})
```

## Testing

### Manual Testing

```bash
# Test typed endpoint
curl -X POST http://localhost:5000/api/ingest/node-up \
  -H "Content-Type: application/json" \
  -d '{
    "nodeCall": "TEST",
    "nodeAlias": "TST",
    "locator": "IO91EC",
    "software": "test",
    "version": "1.0"
  }'

# Should return 202 Accepted with:
{
  "status": "queued",
  "message": "Datagram queued for processing via RabbitMQ",
  "type": "NodeUpEvent",
  "sourceIp": "...",
  "receivedAt": "..."
}
```

### Check Scalar Documentation

1. Visit `http://localhost:5000/scalar`
2. Browse to `/api/ingest` section
3. Verify all typed endpoints show full schemas
4. Try "Execute" with example payloads

### Integration Tests

Existing integration tests continue to work:
- `Tests/Integration/HttpDatagramIngestionTests.cs`

New tests can be added for typed endpoints if desired.

## Implementation Details

### Files Modified

- **`node-api/Controllers/DatagramIngestController.cs`**
  - Added 10 new typed endpoint methods
  - Extracted shared logic to `IngestTypedDatagramAsync` private method
  - Updated status endpoint to list all endpoints
  - Added comprehensive XML documentation with examples

- **`docs/HTTP_DATAGRAM_INGESTION.md`**
  - Updated with typed endpoint documentation
  - Added comparison table (Typed vs Generic vs UDP)
  - Added usage examples for all typed endpoints
  - Added migration guide

- **`docs/TYPED_INGEST_ENDPOINTS.md`** (this file)
  - Implementation summary
  - Design rationale
  - Usage guidelines

### No Breaking Changes

- Generic `/api/ingest` endpoint unchanged
- Batch `/api/ingest/batch` endpoint unchanged
- Status `/api/ingest/status` endpoint enhanced (lists all endpoints)
- All existing tests pass
- All existing clients continue to work

## Future Enhancements

### Possible Additions

1. **Batch Typed Endpoints** (if needed):
   ```csharp
   POST /api/ingest/batch/node-up
   POST /api/ingest/batch/link-up
   ```

2. **Async Validation Endpoints**:
   ```csharp
   POST /api/ingest/node-up/validate  // Validate without queueing
   ```

3. **Schema Export**:
   ```csharp
   GET /api/ingest/node-up/schema     // Get JSON schema
   ```

### Not Needed (Already Available)

- ? OpenAPI schema generation (automatic)
- ? Scalar documentation (automatic)
- ? Type validation (automatic via ASP.NET Core model binding)
- ? XML documentation (added in implementation)

## Comparison: Approaches Considered

### Approach 1: Manual OpenAPI Configuration

**Pros**:
- Single polymorphic endpoint
- Keeps API surface small

**Cons**:
- ? Requires Swashbuckle or similar library
- ? Complex manual schema configuration
- ? Hard to maintain (schema separate from models)
- ? Limited IntelliSense support

### Approach 2: Typed Endpoints (? Chosen)

**Pros**:
- ? Perfect OpenAPI schema automatically
- ? No additional libraries needed
- ? Clear, discoverable API
- ? Easy to maintain (follows model structure)
- ? Excellent IntelliSense support
- ? Backward compatible

**Cons**:
- More endpoints (but shared implementation)

## Conclusion

The typed endpoint approach provides **the best developer experience** for API consumers while maintaining **simplicity and consistency** in the implementation.

### Key Achievements

? **Perfect Scalar Documentation** - Each endpoint shows exact schema  
? **No Code Duplication** - Shared implementation method  
? **Backward Compatible** - Generic endpoint still works  
? **Type Safe** - Compile-time and runtime validation  
? **Discoverable** - Easy to explore in Scalar  
? **Maintainable** - Simple pattern to follow  

### Recommendation

**For new development**: Use typed endpoints (`/api/ingest/node-up`, etc.)  
**For existing code**: No changes needed (generic endpoint still works)  
**For batch operations**: Use `/api/ingest/batch` with `@type` fields

---

**Status**: ? **Implemented and Tested**  
**Version**: 3.0  
**Breaking Changes**: None  
**Build**: ? Passing  
**Tests**: ? Existing tests pass  
**Documentation**: ? Complete  
**Scalar**: ? Perfect schema documentation
