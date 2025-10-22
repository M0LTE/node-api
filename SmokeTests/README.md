# Smoke Tests for node-api

This project contains smoke tests for the node-api service that can be run against a deployed instance to verify it's working correctly.

## Overview

The smoke tests verify:

1. **HTTP API** - Validation endpoint, OpenAPI docs, CORS support
2. **UDP Listener** - Datagram reception on port 13579
3. **MQTT Integration** - Message publishing to MQTT broker
4. **End-to-End Flow** - Complete UDP ? Service ? MQTT flow

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

### Direct Approach (Recommended)

```bash
# 1. Edit appsettings.json to point to your target
# 2. Run tests
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

# Only MQTT tests
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

### UdpSmokeTests

- ? UDP port accessibility
- ? L2Trace datagram acceptance
- ? CircuitUpEvent datagram acceptance
- ? Multiple sequential datagrams
- ? Large datagrams with routing info
- ? Invalid JSON graceful handling
- ? Unknown datagram type handling

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

## Interpreting Results

### Success Indicators

- All non-skipped tests pass ?
- UDP port is accessible and accepting datagrams
- HTTP endpoints respond correctly
- MQTT broker is accessible for subscriptions

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

## Pro Tips

1. **Keep a template**: Copy `appsettings.json` to `appsettings.local.json` and `appsettings.production.json` for quick switching
2. **Use .gitignore**: Add `appsettings.json` to `.gitignore` to avoid committing your local config
3. **Environment variables**: You can override settings using environment variables in CI/CD
4. **Quick switch**: Just swap the JSON file contents to switch between targets
