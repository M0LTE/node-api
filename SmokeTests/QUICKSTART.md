# Smoke Tests - Quick Start Guide

## What Are These Tests?

The smoke tests are a **separate test suite** that you can run locally to verify that a **deployed instance** of the node-api service is working correctly. Unlike the unit/integration tests that test the code, these tests verify an actual running service.

?? **Important**: These tests require a running instance of the service. They will fail if the service is not running.

## Quick Start

### 1. Start the Service (Required!)

**Before running smoke tests**, you must have a running instance of the service:

```bash
# In one terminal, start the service
cd node-api
dotnet run
```

Wait for the service to start (you should see "Now listening on: http://localhost:5000" or similar).

### 2. Edit Configuration

**Simply edit `appsettings.json`** to point to your target:

**For local testing:**
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

**For production testing:**
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

### 3. Run the Tests (in a separate terminal)

**Direct approach (recommended):**
```bash
cd SmokeTests
dotnet test
```

**Or use convenience scripts:**
```bash
# Windows
cd SmokeTests
run-smoke-tests.bat

# Linux/Mac
cd SmokeTests
./run-smoke-tests.sh
```

The scripts just show your config and run `dotnet test` - nothing magical!

## ?? Common Issues

### "Connection refused" or "No connection could be made"

**This means the service is not running!**

Solution:
1. Open a terminal
2. Navigate to the `node-api` directory
3. Run `dotnet run`
4. Wait for "Now listening on..." message
5. Keep that terminal open
6. Run smoke tests in a **different terminal**

### Tests show wrong target (localhost vs production)

**Check your `appsettings.json` file!** The tests use whatever is in that file. Simply edit it to point to your target.

## What Gets Tested?

### ? HTTP API Tests (Require Running Service)
- Service is accessible
- OpenAPI documentation works
- Validation endpoint accepts valid datagrams
- Validation endpoint rejects invalid datagrams
- CORS is configured correctly

### ? UDP Tests (Require Running Service)
- UDP port 13579 is accessible
- Service accepts L2Trace datagrams
- Service accepts event datagrams
- Service handles burst traffic
- Service handles malformed JSON gracefully

**Note on TEST Callsigns**: The UDP tests use TEST callsigns (e.g., "TEST", "TEST-1"). These are:
- ? **Still processed** by the UDP listener, validation, MQTT publishing, and database storage
- ? **Available via explicit API requests** (e.g., `/api/nodes/TEST`, `/api/traces?reportFrom=TEST`)
- ? **Filtered from general listings** (e.g., `/api/nodes`, `/api/links`, `/api/circuits` won't include TEST by default)

This filtering is **intentional** to keep production monitoring clean while allowing testing to work normally.

### ? MQTT Tests (Only test MQTT broker)
- MQTT broker is accessible (doesn't require running service)
- Can subscribe to input topics (`in/udp`)
- Can subscribe to error topics (`in/udp/errored/#`)
- Can subscribe to output topics (`out/#`)

### ?? End-to-End Tests (Skipped by Default)
- Complete UDP ? Service ? MQTT flow
- Invalid datagrams appear on error topics

These are skipped because they require:
- A running service instance
- MQTT write credentials (for UDP listener)
- Time to process messages through the pipeline

## Common Scenarios

### Test Local Development Instance

```bash
# 1. Terminal 1: Start your service
cd node-api
dotnet run

# 2. Edit SmokeTests/appsettings.json to use localhost

# 3. Terminal 2: Run smoke tests (once service is running)
cd SmokeTests
dotnet test
```

### Test Production Deployment

```bash
# 1. Edit SmokeTests/appsettings.json to use production URL

# 2. Run tests
cd SmokeTests
dotnet test
```

### Test After Deployment

Add to your CI/CD pipeline:

```yaml
- name: Run Smoke Tests
  run: |
    cd SmokeTests
    # Create/update config for your environment
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

## Interpreting Results

### ? All Tests Pass
Your service is working correctly:
- HTTP API is accessible
- UDP listener is running and processing TEST datagrams
- MQTT broker is reachable
- Service handles valid and invalid input correctly
- TEST data is properly filtered from general API responses

### ? HTTP Tests Fail with "Connection refused"
**The service is not running!**
- Start the service with `dotnet run` in the node-api directory
- Verify `BaseUrl` in config points to the running service
- Check firewall/network rules if testing remote service

### ? UDP Tests Fail
- Verify UDP port 13579 is open
- Check `UdpHost` in config
- Look at service logs for errors
- Ensure service is actually running
- Verify TEST callsigns are still being processed (check logs/MQTT)

### ? MQTT Tests Fail
- Verify MQTT broker is accessible
- Check `MqttHost` and `MqttPort` in config
- Test MQTT connectivity: `mosquitto_sub -h node-api.packet.oarc.uk -t "out/#" -v`

## Running Individual Test Classes

```bash
# Only HTTP tests (requires running service)
dotnet test --filter "FullyQualifiedName~HttpApiSmokeTests"

# Only UDP tests (requires running service)
dotnet test --filter "FullyQualifiedName~UdpSmokeTests"

# Only MQTT tests (no service required)
dotnet test --filter "FullyQualifiedName~MqttSmokeTests"
```

## Manual Verification

### Send Test UDP Datagram

**Using netcat:**
```bash
echo '{"@type":"NodeUpEvent","nodeCall":"TEST","nodeAlias":"TST","locator":"IO82VJ","software":"Test","version":"1.0"}' | nc -u localhost 13579
```

**Using PowerShell:**
```powershell
$udp = New-Object System.Net.Sockets.UdpClient
$bytes = [System.Text.Encoding]::UTF8.GetBytes('{"@type":"NodeUpEvent","nodeCall":"TEST","nodeAlias":"TST","locator":"IO82VJ","software":"Test","version":"1.0"}')
$udp.Send($bytes, $bytes.Length, "localhost", 13579)
$udp.Close()
```

### Verify TEST Data Processing

After sending a TEST datagram, you can verify it was processed:

```bash
# Check MQTT for the TEST event (shows UDP listener received it)
mosquitto_sub -h node-api.packet.oarc.uk -t "out/NodeUpEvent" -v -C 1

# Query API for TEST node explicitly (shows it was stored)
curl http://localhost:5000/api/nodes/TEST

# Query traces from TEST (shows traces were stored)
curl "http://localhost:5000/api/traces?reportFrom=TEST&limit=5"

# General node list won't include TEST (this is expected)
curl http://localhost:5000/api/nodes  # TEST filtered out
```

### Monitor MQTT Messages

```bash
# Subscribe to all output messages
mosquitto_sub -h node-api.packet.oarc.uk -t "out/#" -v

# Subscribe to errors
mosquitto_sub -h node-api.packet.oarc.uk -t "in/udp/errored/#" -v

# Subscribe to raw UDP input
mosquitto_sub -h node-api.packet.oarc.uk -t "in/udp" -v
```

## Differences from Unit Tests

| Feature | Unit/Integration Tests | Smoke Tests |
|---------|----------------------|-------------|
| **Purpose** | Test code correctness | Verify deployed service |
| **Location** | `Tests/` directory | `SmokeTests/` directory |
| **Target** | In-memory/mocked | Real running service |
| **Configuration** | Hardcoded/mocked | `appsettings.json` |
| **Requires Service Running** | ? No | ? Yes (except MQTT tests) |
| **When to Run** | During development | After deployment |
| **Network Required** | No | Yes |
| **External Services** | Mocked | Real (MQTT, UDP) |
| **Speed** | Fast (seconds) | Slower (network calls) |
| **TEST Callsigns** | Used internally | Used and verified |

## TEST Callsign Behavior

The smoke tests use TEST callsigns extensively. Here's how they behave:

### ? What Works
- **UDP datagrams**: TEST callsigns accepted and processed normally
- **Validation**: TEST datagrams validated like any other
- **MQTT publishing**: TEST events published to MQTT topics
- **Database storage**: TEST data persisted to database
- **Explicit queries**: Can request TEST data directly:
  - `/api/nodes/TEST` - Get specific TEST node
  - `/api/nodes/base/TEST` - Get all TEST-X nodes
  - `/api/traces?reportFrom=TEST` - Get TEST traces
  - `/api/links/node/TEST` - Get TEST links
  - `/api/circuits/node/TEST` - Get TEST circuits

### ? What's Filtered
- **General listings**: TEST excluded from default responses:
  - `/api/nodes` - Won't include TEST nodes
  - `/api/links` - Won't include links involving TEST
  - `/api/circuits` - Won't include circuits involving TEST
  - `/api/traces` - Won't include TEST traces unless `reportFrom=TEST` specified

This design allows:
- ? **Testing to work normally** - Send TEST datagrams, verify processing
- ? **Production monitoring to stay clean** - No TEST clutter in dashboards
- ? **Explicit verification** - Can still check TEST data when needed

## Pro Tips

### Keep Multiple Configs

```bash
# Copy for different environments
cp appsettings.json appsettings.local.json
cp appsettings.json appsettings.production.json

# Quick switch:
cp appsettings.production.json appsettings.json
dotnet test
```

### Use .gitignore

Add to `.gitignore` to avoid committing your local config:
```
SmokeTests/appsettings.json
```

Then keep a template:
```
SmokeTests/appsettings.template.json
```

## Troubleshooting

### Tests timeout
- Increase `TestTimeoutSeconds` in `appsettings.json`
- Check network connectivity
- Verify service is not under heavy load

### "Cannot connect to service"
**First, make sure the service is running:**
```bash
# Start the service
cd node-api
dotnet run
```

Then test connectivity:
```bash
# Test HTTP connectivity (should work if service is running)
curl http://localhost:5000/

# Test with deployed instance
curl https://node-api.packet.oarc.uk/
```

### Wrong target being used
**Check your `appsettings.json`!** The file content is what matters, not any script arguments.

### "UDP send fails"
- Check if port 13579 is blocked by firewall
- Verify service logs show UDP listener started
- Try `netstat -an | grep 13579` to see if port is open
- **Make sure the service is actually running!**

### "MQTT connection refused"
```bash
# Test MQTT connectivity
mosquitto_sub -h node-api.packet.oarc.uk -t "test" -d
```

### "TEST data not appearing"
This is expected behavior:
- ? **TEST data IS processed** - Check MQTT topics, service logs, or explicit API queries
- ? **TEST data NOT in general listings** - This is intentional to keep production clean
- ?? **To verify TEST data**: Use explicit queries like `/api/nodes/TEST` or `/api/traces?reportFrom=TEST`

## Support

- Check service logs for detailed error information
- Review the main project README for service configuration
- See the individual test files for what each test validates
- **Remember: The service must be running for most tests to pass!**
- **Configuration is in `appsettings.json` - that's the only place to look!**
- **TEST callsigns are processed but filtered from general API responses by design**
