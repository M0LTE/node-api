# Smoke Tests for node-api

This project contains smoke tests for the node-api service that can be run against a deployed instance to verify it's working correctly.

## Overview

The smoke tests verify:

1. **HTTP API** - Validation endpoint, OpenAPI docs, CORS support
2. **UDP Listener** - Datagram reception on port 13579
3. **MQTT Integration** - Message publishing to MQTT broker
4. **End-to-End Flow** - Complete UDP ? Service ? MQTT flow

**Note on TEST Callsigns**: The smoke tests use TEST callsigns (e.g., "TEST", "TEST-1"). These are processed normally by the service but filtered from general API responses to keep production monitoring clean. See [Configuration](#test-callsign-behavior) for details.

## Configuration

**Simply edit `appsettings.json`** to point to your target instance:

### For Local Testing
```json
{
  "SmokeTestSettings": {
    "BaseUrl": "http://localhost:5000",
    "UdpHost": "localhost",
    "UdpPort": 13579,
    "MqttHost": "node-api.packet.oarc.uk",
    "MqttPort": 1883,
    "TestTimeoutSeconds": 30
  }
}
```

### For Production Testing
```json
{
  "SmokeTestSettings": {
    "BaseUrl": "https://node-api.packet.oarc.uk",
    "UdpHost": "node-api.packet.oarc.uk",
    "UdpPort": 13579,
    "MqttHost": "node-api.packet.oarc.uk",
    "MqttPort": 1883,
    "TestTimeoutSeconds": 30
  }
}
```

**That's it!** Just edit the file and run the tests. No scripts needed.

## Running the Tests

### Prerequisites

?? **Important**: Most tests require the service to be running!

```bash
# Start the service in one terminal
cd node-api
dotnet run

# Run smoke tests in another terminal
cd SmokeTests
dotnet test
```

### Direct Approach (Recommended)

```bash
# 1. Edit appsettings.json to point to your target
# 2. Make sure service is running
# 3. Run tests
cd SmokeTests
dotnet test
```

### Using Convenience Scripts

The runner scripts just display your config and run `dotnet test`:

**Windows:**
```cmd
cd SmokeTests
run-smoke-tests.bat
```

**Linux/Mac:**
```bash
cd SmokeTests
chmod +x run-smoke-tests.sh
./run-smoke-tests.sh
```

### Run Specific Test Categories

```bash
# Only HTTP tests
dotnet test --filter "FullyQualifiedName~HttpApiSmokeTests"

# Only UDP tests
dotnet test --filter "FullyQualifiedName~UdpSmokeTests"

# Only MQTT tests (no running service required)
dotnet test --filter "FullyQualifiedName~MqttSmokeTests"
```

## Test Categories

### HttpApiSmokeTests

- ? Health check endpoint
- ? OpenAPI specification accessibility
- ? Scalar documentation accessibility
- ? Validation endpoint with valid data
- ? Validation endpoint with invalid data
- ? Malformed JSON handling
- ? CORS support

**Note**: Uses TEST callsigns for validation testing

### UdpSmokeTests

- ? UDP port accessibility
- ? L2Trace datagram acceptance
- ? CircuitUpEvent datagram acceptance
- ? Multiple sequential datagrams
- ? Large datagrams with routing info
- ? Invalid JSON graceful handling
- ? Unknown datagram type handling

**Note**: Sends TEST callsigns which are processed but filtered from general API responses

### MqttSmokeTests

- ? MQTT broker connectivity
- ? Subscription to input topics
- ? Subscription to error topics
- ? Subscription to output topics
- ? Clean session support
- ?? **Skipped**: End-to-end message verification (requires write credentials)

### EndToEndSmokeTests

- ?? **Skipped**: Complete UDP ? MQTT flow (requires MQTT access)
- ?? **Skipped**: Invalid datagram error topic verification
- ? Burst message handling
- ? Diagnostics endpoint availability during load
- ? Multiple datagram types processing

## TEST Callsign Behavior

The smoke tests use TEST callsigns extensively. Understanding how they work is important:

### ? What Works
- **UDP Processing**: TEST datagrams are accepted, validated, and processed
- **MQTT Publishing**: TEST events published to MQTT topics (e.g., `out/NodeUpEvent`)
- **Database Storage**: TEST data persisted to database
- **Explicit API Queries**: Can request TEST data directly:
  - `GET /api/nodes/TEST` - Get specific TEST node
  - `GET /api/nodes/base/TEST` - Get all TEST-X nodes
  - `GET /api/traces?reportFrom=TEST` - Get traces from TEST
  - `GET /api/links/node/TEST` - Get links involving TEST
  - `GET /api/circuits/node/TEST` - Get circuits involving TEST

### ? What's Filtered
- **General API Listings**: TEST excluded from default responses:
  - `GET /api/nodes` - Won't include TEST nodes
  - `GET /api/links` - Won't include links with TEST endpoints
  - `GET /api/circuits` - Won't include circuits with TEST endpoints
  - `GET /api/traces` - Won't include TEST traces (unless `reportFrom=TEST`)

### Why This Design?
- ? **Testing works normally** - Send TEST datagrams, verify processing
- ? **Production stays clean** - No TEST clutter in dashboards and monitors
- ? **Explicit verification possible** - Can still query TEST data when needed

### Verifying TEST Data

After running smoke tests, you can verify TEST data was processed:

```bash
# Check MQTT for TEST events
mosquitto_sub -h node-api.packet.oarc.uk -t "out/NodeUpEvent" -v -C 1

# Query API for TEST node (works - explicit request)
curl http://localhost:5000/api/nodes/TEST

# Query traces from TEST (works - explicit filter)
curl "http://localhost:5000/api/traces?reportFrom=TEST&limit=5"

# General node list (TEST filtered out - this is expected)
curl http://localhost:5000/api/nodes  # No TEST nodes
```

## Interpreting Results

### Success Indicators

- All non-skipped tests pass ?
- UDP port is accessible and accepting TEST datagrams
- HTTP endpoints respond correctly with proper TEST filtering
- MQTT broker is accessible for subscriptions
- TEST data processed but filtered from general listings

### Common Issues

#### "Service may not be running"
- Make sure service is running: `dotnet run` (in node-api directory)
- Check that `BaseUrl` in config points to running instance
- Verify service is running: `curl http://your-service:5000/`

#### "UDP port not accessible"
- Check firewall rules allow UDP traffic on port 13579
- Verify `UdpHost` and `UdpPort` in config are correct
- Check service logs to confirm UDP listener started

#### "MQTT broker connection failed"
- Verify `MqttHost` and `MqttPort` in config
- Check network connectivity to MQTT broker
- Confirm MQTT broker allows anonymous connections for read-only subscriptions

#### "TEST data not found"
**This is expected for general queries!**
- ? TEST data IS processed - check MQTT, logs, or use explicit queries
- ? TEST data NOT in `/api/nodes`, `/api/links`, etc. - this is by design
- ?? To verify: Use `/api/nodes/TEST` or `/api/traces?reportFrom=TEST`

## Manual Testing

For manual verification of the complete flow:

1. Start monitoring MQTT topics:
   ```bash
   # Using mosquitto_sub
   mosquitto_sub -h node-api.packet.oarc.uk -t "out/#" -v
   ```

2. Send a test UDP datagram:
   ```bash
   # Using netcat
   echo '{"@type":"NodeUpEvent","nodeCall":"TEST","nodeAlias":"TST","locator":"IO82VJ","software":"Test","version":"1.0"}' | nc -u your-service 13579
   ```

3. Verify message appears on MQTT topic `out/NodeUpEvent`

4. Query for TEST node explicitly:
   ```bash
   curl http://your-service:5000/api/nodes/TEST
   ```

5. Verify TEST is NOT in general node list (expected):
   ```bash
   curl http://your-service:5000/api/nodes  # Should not include TEST
   ```

## CI/CD Integration

These smoke tests are designed to run in CI/CD pipelines:

```yaml
# Example GitHub Actions
- name: Run Smoke Tests
  run: |
    cd SmokeTests
    # Update config for deployment environment
    cat > appsettings.json << EOF
    {
      "SmokeTestSettings": {
        "BaseUrl": "${{ env.SERVICE_URL }}",
        "UdpHost": "${{ env.SERVICE_HOST }}",
        "UdpPort": 13579,
        "MqttHost": "node-api.packet.oarc.uk",
        "MqttPort": 1883,
        "TestTimeoutSeconds": 30
      }
    }
    EOF
    dotnet test --logger "console;verbosity=detailed"
```

## Troubleshooting

Enable detailed logging:
```bash
dotnet test --logger "console;verbosity=detailed"
```

Check service health:
```bash
curl http://your-service:5000/scalar
```

Verify UDP listener:
```bash
netstat -an | grep 13579
```

Test MQTT connectivity:
```bash
mosquitto_sub -h node-api.packet.oarc.uk -t "in/udp" -v -d
```

Check TEST data processing:
```bash
# This should work (explicit request)
curl http://your-service:5000/api/nodes/TEST

# This should NOT include TEST (filtered)
curl http://your-service:5000/api/nodes

# Check traces from TEST
curl "http://your-service:5000/api/traces?reportFrom=TEST&limit=5"
```

## Pro Tips

1. **Keep a template**: Copy `appsettings.json` to `appsettings.local.json` and `appsettings.production.json` for quick switching
2. **Use .gitignore**: Add `appsettings.json` to `.gitignore` to avoid committing your local config
3. **Environment variables**: You can override settings using environment variables in CI/CD
4. **Quick switch**: Just swap the JSON file contents to switch between targets
5. **TEST verification**: Remember TEST data exists but is filtered from general listings - use explicit queries to verify

## Quick Reference

| What You Want | How to Get It |
|---------------|---------------|
| All nodes (production) | `GET /api/nodes` (TEST filtered) |
| Specific TEST node | `GET /api/nodes/TEST` |
| All TEST SSIDs | `GET /api/nodes/base/TEST` |
| Traces from TEST | `GET /api/traces?reportFrom=TEST` |
| Links involving TEST | `GET /api/links/node/TEST` |
| Circuits involving TEST | `GET /api/circuits/node/TEST` |
| Check TEST on MQTT | `mosquitto_sub -t "out/#"` |

## Further Reading

- See `QUICKSTART.md` for step-by-step guide
- See test files for what each test validates
- Main project README for service configuration
