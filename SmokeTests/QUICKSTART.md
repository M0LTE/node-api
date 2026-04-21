# Smoke Tests - Quick Start

These tests assume the repository is split into:

- `node-api` for HTTP APIs and processing
- `node-api-ingester` for UDP ingress on `13579`

## Local run

```bash
# Terminal 1
cd node-api
dotnet run

# Terminal 2
cd node-api-ingester
dotnet run

# Terminal 3
cd SmokeTests
dotnet test
```

## Local config

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

`BaseUrl` should point to `node-api`.

`UdpHost` and `UdpPort` should point to `node-api-ingester`.

## Production config

If both services are exposed from the same host, `BaseUrl` and `UdpHost` can still point at the same machine. If they are deployed separately, set them independently.

## What the tests cover

- HTTP API availability and OpenAPI docs
- UDP datagram submission to the ingester
- MQTT broker connectivity
- split end-to-end flow from UDP through MQTT

## Common failures

### HTTP failures

- start `node-api`
- verify `BaseUrl`

### UDP failures

- start `node-api-ingester`
- verify UDP `13579` is open
- verify `UdpHost`

### MQTT failures

- verify the configured broker is reachable

## Handy commands

```bash
dotnet test --filter "FullyQualifiedName~HttpApiSmokeTests"
dotnet test --filter "FullyQualifiedName~UdpSmokeTests"
dotnet test --filter "FullyQualifiedName~MqttSmokeTests"
```
