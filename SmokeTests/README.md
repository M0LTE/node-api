# Smoke Tests

These smoke tests validate the split deployment, where HTTP and UDP may be served by different processes.

## What they target

| Setting | Expected target |
|---|---|
| `BaseUrl` | `node-api` HTTP service |
| `UdpHost` / `UdpPort` | `node-api-ingester` UDP listener |
| `MqttHost` / `MqttPort` | MQTT broker used by `node-api` |

That means the smoke tests can verify the full split even when the API and ingester run separately.

## Local setup

Start both services before running UDP-related smoke tests:

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

## Example configuration

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

If your ingester runs on a different machine or container host, point `UdpHost` at that location.

## Test groups

- **HTTP API**: OpenAPI/Scalar, validation endpoints, CORS, and general API availability
- **UDP**: datagram submission to the ingester on port `13579`
- **MQTT**: broker connectivity and topic subscription
- **End-to-end**: UDP -> ingester -> RabbitMQ -> node-api -> MQTT

## Running subsets

```bash
dotnet test --filter "FullyQualifiedName~HttpApiSmokeTests"
dotnet test --filter "FullyQualifiedName~UdpSmokeTests"
dotnet test --filter "FullyQualifiedName~MqttSmokeTests"
```

## TEST callsigns

The suite uses TEST callsigns intentionally:

- they are still accepted by the UDP ingester and processing pipeline
- they are still published to MQTT and stored
- they are filtered from general listing endpoints unless explicitly requested

Useful checks after a run:

```bash
curl http://localhost:5000/api/nodes/TEST
curl "http://localhost:5000/api/traces?reportFrom=TEST&limit=5"
mosquitto_sub -h node-api.packet.oarc.uk -t "out/NodeUpEvent" -v -C 1
```

## Common failures

### HTTP tests fail

- verify `node-api` is running
- verify `BaseUrl` points to the right service

### UDP tests fail

- verify `node-api-ingester` is running
- verify UDP `13579` is open
- verify `UdpHost` and `UdpPort` point to the ingester

### MQTT tests fail

- verify broker connectivity and subscription access
