# node-api

A .NET 10 packet-network monitoring system split into two services:

| Service | Responsibility |
|---|---|
| `node-api-ingester` | Owns UDP ingress on port `13579` and publishes raw datagrams to RabbitMQ |
| `node-api` | Owns HTTP APIs, RabbitMQ consumption, validation/processing, MQTT publishing, in-memory state, and MySQL persistence |

## Overview

The split is now clean at the service boundary:

- UDP traffic enters through **`node-api-ingester`** only
- RabbitMQ is the handoff between ingress and processing
- **`node-api`** provides query APIs plus HTTP ingestion endpoints
- MQTT, GeoIP, rate limiting, network-state updates, and persistence all run in **`node-api`**

## Quick Start

### Prerequisites

- .NET 10 SDK
- MySQL 8.0+ (or MariaDB)
- MQTT broker
- RabbitMQ

### Run locally

```bash
# Terminal 1 - processing/API service
cd node-api
dotnet run

# Terminal 2 - UDP ingress service
cd node-api-ingester
dotnet run
```

By default:

- `node-api` serves HTTP on `http://localhost:5000`
- `node-api-ingester` listens for UDP on port `13579`

For local UDP end-to-end testing, RabbitMQ must be configured because the ingester publishes to the queue and the API service consumes from it.

## Architecture

```text
UDP:13579
XRouter nodes
    |
    v
node-api-ingester
    |
    |  AMQP
    v
 RabbitMQ queue
    |
    v
  node-api
    |
    +--> MQTT topics
    +--> MySQL persistence
    +--> REST API /api/*
    +--> OpenAPI/Scalar /scalar
```

## Key Features

- UDP ingress microservice on port `13579`
- HTTP ingestion API (`/api/ingest*`)
- RabbitMQ-backed decoupling between ingress and processing
- MQTT publishing for raw input, validation errors, and processed events
- MySQL-backed traces, events, errored-message storage, and network state
- REST endpoints for nodes, links, circuits, traces, events, and diagnostics
- Rate limiting, blacklist support, GeoIP enrichment, and link-analysis features

## Main HTTP API surfaces

- `POST /api/ingest`
- `POST /api/ingest/batch`
- `GET /api/ingest/status`
- `GET /api/nodes`
- `GET /api/links`
- `GET /api/circuits`
- `GET /api/traces`
- `GET /api/events`
- `GET /scalar`

## Testing

```bash
# Build everything
dotnet build node-api.sln

# Run repository tests that do not require DB credentials
dotnet test Tests/ --filter "Category!=DatabaseIntegration"

# Run smoke tests against deployed or local services
dotnet test SmokeTests/
```

Smoke tests can target HTTP and UDP separately through `BaseUrl` and `UdpHost`, which is important now that the two responsibilities live in different services.

## Documentation

- [Documentation index](docs/README.md)
- [RabbitMQ integration](docs/RABBITMQ_INTEGRATION.md)
- [HTTP datagram ingestion](docs/HTTP_DATAGRAM_INGESTION.md)
- [Rate limiting](docs/RATE_LIMITING.md)
- [Smoke tests](SmokeTests/README.md)
- [Contributing](CONTRIBUTING.md)

## Troubleshooting

### UDP port not accessible

Check the **ingester**, not `node-api`:

```bash
netstat -an | grep 13579
sudo systemctl status node-api-ingester --no-pager
```

### Database integration tests fail locally

Those tests require DB secrets or environment variables:

- `DB_HOST`
- `DB_PORT`
- `DB_USER`
- `DB_PASSWORD`
- `DB_NAME`

## License
