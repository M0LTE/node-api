# node-api

A .NET 9.0 ASP.NET Core Web API service for real-time packet radio network monitoring and analysis.

## 📚 Overview

**node-api** is a comprehensive monitoring solution for AX.25 packet radio networks. It ingests network event data via UDP datagrams, validates and processes various event types, maintains network state, and provides both REST API and MQTT interfaces for real-time monitoring.

### Key Features

- 📡 **UDP Datagram Ingestion** - Listens on port 13579 for network events
- ✅ **Comprehensive Validation** - FluentValidation for all event types
- 📤 **Real-time MQTT Publishing** - Events published to MQTT topics
- 💾 **Persistent State** - MySQL database for network state and history
- 🌐 **REST API** - Query nodes, links, circuits, traces, and diagnostics
- ⚡ **Rate Limiting** - Rolling average with burst support
- 🌍 **GeoIP Integration** - Location tracking with privacy-preserving obfuscation
- 🧠 **Link Intelligence** - Flapping detection and AX.25 routing heuristics
- 🐰 **RabbitMQ Support** - Dual-path ingestion for resilience
- 📖 **OpenAPI/Scalar** - Interactive API documentation at `/scalar`

## 🚀 Quick Start

### Prerequisites

- .NET 9.0 SDK
- MySQL 8.0+ (or MariaDB)
- MQTT broker (e.g., Mosquitto)
- Optional: RabbitMQ for dual-path ingestion

### Running Locally

```bash
# Clone the repository
git clone https://github.com/M0LTE/node-api.git
cd node-api

# Configure connection strings (edit appsettings.json)
cd node-api
# Edit ConnectionStrings:DefaultConnection and MqttSettings

# Run the service
dotnet run

# Service will be available at:
# - http://localhost:5000
# - OpenAPI docs: http://localhost:5000/scalar
```

### Running with Docker

```bash
docker build -t node-api .
docker run -p 5000:8080 -p 13579:13579/udp node-api
```

See [Deployment Guide](docs/DEPLOYMENT.md) for production deployment instructions.

## 📚 Documentation

### Getting Started
- 📖 [Documentation Index](docs/README.md) - Complete documentation navigation
- 🚀 [Deployment Guide](docs/DEPLOYMENT.md) - Production deployment
- 🐳 [Docker Publishing](docs/DOCKER_PUBLISH.md) - Container build and publish

### Core Features
- ⚡ [Rate Limiting](docs/RATE_LIMITING.md) - UDP rate limiting lifecycle
- 🌍 [IP & GeoIP Tracking](docs/IP_AND_GEOIP_FEATURE.md) - Location with privacy
- 📊 [Link Flapping Detection](docs/LINK_FLAPPING.md) - Unstable connection tracking
- 🧠 [AX.25 Link Inference](docs/AX25_LINK_INFERENCE.md) - Routing heuristics
- ⏰ [Timestamp Tracking](docs/TIMESTAMP_TRACKING.md) - Datagram arrival times

### Architecture
- 🐰 [RabbitMQ Integration](docs/RABBITMQ_INTEGRATION.md) - Message queue support
- 📋 [Phase 2 Summary](docs/PHASE2_SUMMARY.md) - Dual-path ingestion

### Testing
- 🧪 [Smoke Tests](SmokeTests/README.md) - End-to-end testing guide
- ✅ [Unit Tests](Tests/) - 1,000+ comprehensive tests

### Developer Resources
- 📝 [Contributing Guide](CONTRIBUTING.md) - How to contribute to the project
- 💻 [Copilot Instructions](.github/copilot-instructions.md) - Coding standards and guidelines

## 🏗️ Architecture

```
┌─────────┐     UDP:13579      ┌──────────────┐
│   XRouter   │ ────────────────>│ UDP Listener │
│   Nodes     │                    │              │
└─────────┘                    └──────────────┘
                                          │
┌─────────┐     AMQP          ┌─────────────────┐
│  RabbitMQ   │ ────────────────>│  Validation   │
│   Queue     │                    │   Service     │
└─────────┘                    └─────────────────┘
                                          │
                                   ┌───────────────────┐
                                   │ Network State   │
                                   │    Updater      │
                                   └───────────────────┘
                                          │
                    ┌─────────────────────┼─────────────────────┐
                    │                     │                     │
             ┌──────────┐        ┌──────────┐       ┌──────────┐
             │   MySQL    │        │    MQTT    │       │  REST API  │
             │  Database  │        │  Publisher │       │   /api/*   │
             └──────────┘        └──────────┘       └──────────┘
```

### Technology Stack

- **Runtime**: .NET 9.0
- **Framework**: ASP.NET Core with minimal API
- **Validation**: FluentValidation
- **Database**: MySQL 8.0+ with Dapper ORM
- **Messaging**: MQTTnet client, RabbitMQ.Client
- **Testing**: xUnit (1,000+ tests)
- **Containerization**: Docker

## 📋 Event Types

The service processes the following AX.25 network events:

| Event Type | Description |
|------------|-------------|
| `NodeUpEvent` | Node comes online |
| `NodeStatusReportEvent` | Periodic node status |
| `NodeDownEvent` | Node goes offline |
| `LinkUpEvent` | Layer 2 link established |
| `LinkStatus` | Periodic link status |
| `LinkDisconnectionEvent` | Link disconnected |
| `CircuitUpEvent` | NetROM Layer 4 circuit established |
| `CircuitStatus` | Periodic circuit status |
| `CircuitDisconnectionEvent` | Circuit disconnected |
| `L2Trace` | Layer 2 frame trace |

## 🌐 API Endpoints

### Nodes
- `GET /api/nodes` - List all nodes
- `GET /api/nodes/{callsign}` - Get specific node
- `GET /api/nodes/base/{baseCallsign}` - Get nodes by base callsign

### Links
- `GET /api/links` - List all links
- `GET /api/links/{key}` - Get specific link
- `GET /api/links/node/{callsign}` - Get links for a node

### Circuits
- `GET /api/circuits` - List all circuits
- `GET /api/circuits/{key}` - Get specific circuit
- `GET /api/circuits/node/{callsign}` - Get circuits for a node

### Traces & Events
- `GET /api/traces` - Query L2 traces
- `GET /api/events` - Query network events

### Diagnostics
- `GET /api/diagnostics/db/query-frequency` - Database query statistics
- `GET /api/diagnostics/udp/rate-limit-status` - Rate limiting status

### API Documentation
- `GET /scalar` - Interactive API documentation
- `GET /openapi/v1.json` - OpenAPI specification

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test Tests/

# Run smoke tests (requires running service)
cd SmokeTests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~NetworkStateUpdaterL2TraceTests"
```

**Test Coverage**: 1,000+ tests covering:
- Validators (all event types)
- Network state updates
- API endpoints
- Rate limiting
- AX.25 routing logic
- End-to-end flows

See [Smoke Tests Documentation](SmokeTests/README.md) for detailed testing guide.

## 🔒 Security & Privacy

- **IP Obfuscation**: Only last 2 octets of IPv4 addresses stored
- **Rate Limiting**: Prevents abuse with rolling average + burst detection
- **CIDR Blacklisting**: Permanent blocks for malicious sources
- **Input Validation**: FluentValidation on all incoming data
- **SQL Injection Protection**: Parameterized queries throughout

## 🔧 Troubleshooting

### UDP Port Not Accessible
```bash
# Check if port is listening
netstat -an | grep 13579

# Check firewall rules (Linux)
sudo ufw status
sudo ufw allow 13579/udp

# Check firewall rules (Windows)
netsh advfirewall firewall show rule name="Node API UDP"
```

### Database Connection Issues
- Verify connection string in `appsettings.json`
- Ensure MySQL is running: `systemctl status mysql`
- Check database exists and schema is up to date

### MQTT Connection Issues
- Verify broker address and port
- Check broker allows connections: `mosquitto_sub -h <broker> -t "#" -v`
- Review MQTT logs in application output

See individual feature documentation for specific troubleshooting guides.

## ⚡ Performance

- **UDP Throughput**: Handles 100+ datagrams/second
- **Rate Limiting**: Configurable per-IP limits with burst support
- **Database**: Optimized queries with indexing
- **Memory**: Efficient in-memory network state tracking
- **Latency**: Sub-millisecond event processing

## 🤝 Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

Quick checklist:

1. Review [Copilot Instructions](.github/copilot-instructions.md) for coding standards
2. Write tests for new features (xUnit)
3. Ensure all tests pass: `dotnet test`
4. Follow existing code patterns and naming conventions
5. Update documentation for new features

For comprehensive contributor guidelines, workflow, and best practices, see [CONTRIBUTING.md](CONTRIBUTING.md).

## 📄 License

[Add license information here]

## 🙏 Acknowledgments

This project monitors AX.25 packet radio networks and processes data from XRouter nodes. Thanks to the amateur radio community for the protocols and specifications.

## 💬 Support

- **Issues**: [GitHub Issues](https://github.com/M0LTE/node-api/issues)
- **Documentation**: [docs/](docs/)
- **API Docs**: `/scalar` endpoint when service is running

---

**Built with ❤️ for the packet radio community**
