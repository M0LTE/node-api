# TEST Callsign Filtering and SmokeTests

## Overview

The node-api service implements filtering of TEST callsigns from general API responses while still fully processing TEST data. This document explains how this affects the smoke tests and how to work with TEST data.

## What Changed?

### TEST Filtering Implementation

TEST callsigns (matching pattern `^TEST(-[0-9]|1[0-5])?$`) are now filtered from:

- `GET /api/nodes` - General node listing
- `GET /api/links` - General link listing  
- `GET /api/circuits` - General circuit listing
- `GET /api/traces` - General trace listing (unless explicitly requested)

### What Didn't Change?

? **All internal processing unchanged**:
- UDP listener accepts and validates TEST datagrams
- MQTT publishing includes TEST events
- Database persistence stores TEST data
- Network state tracking includes TEST nodes/links/circuits

## Impact on SmokeTests

### ? SmokeTests Still Work

The smoke tests **continue to work perfectly** because they:

1. **Test UDP transmission** - Not API retrieval
   - Tests send UDP datagrams with TEST callsigns
   - Verify datagrams were sent successfully
   - Don't query API expecting TEST in general listings

2. **Test validation** - Using the diagnostics endpoint
   - `POST /api/diagnostics/validate` with TEST data
   - Validates TEST datagrams correctly
   - Returns validation results

3. **Test MQTT connectivity** - Not TEST-specific data
   - Subscribe to topics
   - Verify broker connectivity
   - Don't look for specific TEST messages

### What the SmokeTests Verify

| Test Category | What It Tests | Uses TEST? | Still Works? |
|---------------|---------------|------------|--------------|
| **HttpApiSmokeTests** | API endpoints, validation, CORS | Yes | ? Yes |
| **UdpSmokeTests** | UDP datagram transmission | Yes | ? Yes |
| **MqttSmokeTests** | MQTT broker connectivity | No | ? Yes |
| **EndToEndSmokeTests** | Complete flow (mostly skipped) | Yes | ? Yes |

## How to Verify TEST Data

After running smoke tests, you can verify TEST data was processed:

### ? Methods That Work

```bash
# 1. Check MQTT topics (TEST events are published)
mosquitto_sub -h node-api.packet.oarc.uk -t "out/NodeUpEvent" -v -C 1

# 2. Query specific TEST node (explicit request)
curl http://localhost:5000/api/nodes/TEST

# 3. Query TEST base callsign (explicit request)
curl http://localhost:5000/api/nodes/base/TEST

# 4. Query traces from TEST (explicit filter)
curl "http://localhost:5000/api/traces?reportFrom=TEST&limit=5"

# 5. Query links for TEST (explicit request)
curl http://localhost:5000/api/links/node/TEST

# 6. Query circuits for TEST (explicit request)
curl http://localhost:5000/api/circuits/node/TEST

# 7. Check service logs (TEST processing logged)
# Logs will show TEST datagrams being received, validated, and processed
```

### ? Methods That Won't Show TEST (By Design)

```bash
# General node listing (TEST filtered)
curl http://localhost:5000/api/nodes

# General link listing (TEST links filtered)
curl http://localhost:5000/api/links

# General circuit listing (TEST circuits filtered)
curl http://localhost:5000/api/circuits

# Traces without reportFrom filter (TEST filtered)
curl http://localhost:5000/api/traces?limit=10
```

## Why This Design?

### ? Benefits

1. **Testing Works Normally**
   - Smoke tests send TEST datagrams
   - Service processes them completely
   - Can verify processing via explicit queries

2. **Production Stays Clean**
   - Production monitoring dashboards don't show TEST
   - API responses focused on real network data
   - No TEST clutter in general listings

3. **Debugging Remains Possible**
   - Can still query TEST data explicitly
   - MQTT shows TEST events in real-time
   - Service logs show TEST processing

### ?? Use Cases

| Use Case | Solution |
|----------|----------|
| Running smoke tests | Works out of the box - no changes needed |
| Verifying TEST was processed | Use explicit queries or MQTT subscription |
| Production monitoring | General API calls automatically exclude TEST |
| Debugging TEST issues | Service logs + explicit API queries |
| CI/CD pipeline testing | Smoke tests work normally |

## Example: Complete TEST Verification Flow

```bash
# 1. Send TEST datagram via UDP
echo '{"@type":"NodeUpEvent","nodeCall":"TEST","nodeAlias":"TST","locator":"IO82VJ","software":"Test","version":"1.0"}' | nc -u localhost 13579

# 2. Verify on MQTT (proves UDP listener received it)
mosquitto_sub -h node-api.packet.oarc.uk -t "out/NodeUpEvent" -v -C 1
# Should see: out/NodeUpEvent {"@type":"NodeUpEvent","nodeCall":"TEST",...}

# 3. Query API explicitly (proves it was stored)
curl http://localhost:5000/api/nodes/TEST
# Should return: {"callsign":"TEST","alias":"TST",...}

# 4. Check general node list (proves filtering works)
curl http://localhost:5000/api/nodes
# Should NOT include TEST

# 5. Query traces from TEST (proves traces stored)
curl "http://localhost:5000/api/traces?reportFrom=TEST&limit=5"
# Should return traces from TEST
```

## Updating SmokeTests (If Needed)

The current smoke tests **don't need updates** because they only test transmission, not retrieval. However, if you want to add verification tests:

```csharp
[Fact]
public async Task TEST_Data_Should_Be_Processed_But_Filtered()
{
    // Send TEST datagram
    var datagram = """
    {
        "@type": "NodeUpEvent",
        "nodeCall": "TEST",
        "nodeAlias": "TST",
        "locator": "IO82VJ",
        "software": "Test",
        "version": "1.0"
    }
    """;
    
    using var udpClient = new UdpClient();
    var bytes = Encoding.UTF8.GetBytes(datagram);
    await udpClient.SendAsync(bytes, bytes.Length, GetUdpEndpoint());
    
    await Task.Delay(1000); // Allow processing time
    
    // Explicit query should work
    var testNode = await _fixture.HttpClient.GetAsync("/api/nodes/TEST");
    testNode.StatusCode.Should().Be(HttpStatusCode.OK);
    
    // General listing should not include TEST
    var allNodes = await _fixture.HttpClient.GetFromJsonAsync<List<Node>>("/api/nodes");
    allNodes.Should().NotContain(n => n.Callsign == "TEST");
}
```

## Troubleshooting

### "TEST data not appearing"

**This is expected for general queries!**

? **Correct behavior**:
- `GET /api/nodes` doesn't include TEST
- `GET /api/links` doesn't include TEST links
- `GET /api/circuits` doesn't include TEST circuits

? **Not a bug** - this is intentional filtering

?? **Solution**: Use explicit queries:
- `GET /api/nodes/TEST`
- `GET /api/traces?reportFrom=TEST`
- `GET /api/links/node/TEST`

### "Smoke tests failing"

If smoke tests fail, check:

1. **Service is running** - Most tests require it
2. **UDP port accessible** - Check firewall rules
3. **MQTT broker reachable** - Test with mosquitto_sub
4. **Not expecting TEST in general listings** - This is filtered by design

### "How do I know TEST was processed?"

Multiple verification methods:

1. **Service logs** - Look for TEST processing messages
2. **MQTT topics** - Subscribe to `out/#` and watch for TEST events
3. **Explicit API queries** - `GET /api/nodes/TEST`
4. **Database** - Query MySQL directly for TEST data

## Summary

| Aspect | Status | Notes |
|--------|--------|-------|
| **Smoke tests** | ? Work unchanged | Only test transmission, not retrieval |
| **UDP processing** | ? Unchanged | TEST datagrams fully processed |
| **MQTT publishing** | ? Unchanged | TEST events published normally |
| **Database storage** | ? Unchanged | TEST data persisted |
| **Explicit queries** | ? Work | Can request TEST data directly |
| **General listings** | ? Filtered | TEST excluded by design |

**Bottom line**: The smoke tests work perfectly because they test the processing pipeline (which is unchanged), not the API filtering (which is new). TEST data flows through the entire system but is simply hidden from general API responses to keep production monitoring clean.
