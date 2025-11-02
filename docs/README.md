# Documentation Index

Welcome to the node-api documentation. This index provides quick navigation to all available documentation.

## ?? Table of Contents

### Getting Started

- **[Main README](../README.md)** - Project overview, quick start, architecture
- **[Deployment Guide](DEPLOYMENT.md)** - Production deployment instructions
- **[Docker Publishing](DOCKER_PUBLISH.md)** - Building and publishing Docker images

### Core Features

#### Rate Limiting & Protection
- **[Rate Limiting Lifecycle](RATE_LIMITING.md)** - Comprehensive guide to IP-based rate limiting
- **[Rolling Average Rate Limiting](RATE_LIMITING_ROLLING_AVERAGE.md)** - Dual-threshold approach with burst support

#### Network Intelligence
- **[AX.25 Link Inference](AX25_LINK_INFERENCE.md)** - Complete guide to smart link detection with routing heuristics
  - Includes quick reference, scenarios, implementation details, testing, and troubleshooting

#### Data Collection & Privacy
- **[IP & GeoIP Feature](IP_AND_GEOIP_FEATURE.md)** - Location tracking with privacy-preserving obfuscation
- **[Timestamp Tracking](TIMESTAMP_TRACKING.md)** - UDP datagram arrival time tracking
- **[Link Flapping Detection](LINK_FLAPPING.md)** - Tracking unstable connections

### Architecture & Integration

- **[RabbitMQ Integration](RABBITMQ_INTEGRATION.md)** - Message queue support for dual-path ingestion
- **[Phase 2 Summary](PHASE2_SUMMARY.md)** - Full datagram processing from RabbitMQ

### Diagnostics & Monitoring

- **[Query Frequency Diagnostics](QUERY_FREQUENCY_DIAGNOSTICS.md)** - Database query statistics endpoint
- **[Implementation Notes](IMPLEMENTATION_NOTES.md)** - Various implementation details

### Change History

- **[Changelog](CHANGELOG.md)** - Project history, features, and bug fixes

### Troubleshooting & Bug Fixes

- **[Traffic Loop Fix](TRAFFIC_LOOP_FIX.md)** - Resolving duplicate state updates issue
- **[Total Requests Display Fix](FIX_TOTAL_REQUESTS_DISPLAY.md)** - UI display correction

### Testing

- **[Smoke Tests](../SmokeTests/README.md)** - End-to-end testing guide
  - [Quick Start](../SmokeTests/QUICKSTART.md) - Step-by-step testing guide
  - [TEST Callsign Filtering](../SmokeTests/TEST_CALLSIGN_FILTERING.md) - Understanding test data filtering
- **[Unit Tests](../Tests/)** - 1,000+ comprehensive tests

### Developer Resources

- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute to the project
  - Development workflow
  - Pull request process
  - Code review checklist
  - Testing requirements
- **[Copilot Instructions](../.github/copilot-instructions.md)** - AI coding assistant guidelines
  - Code style standards
  - Testing practices
  - Database conventions
  - MQTT patterns

## ?? Documentation by Category

### For New Developers

Start here if you're new to the project:

1. [Main README](../README.md) - Understand what node-api does
2. [Contributing Guide](../CONTRIBUTING.md) - Learn how to contribute
3. [Copilot Instructions](../.github/copilot-instructions.md) - Learn coding standards
4. [Deployment Guide](DEPLOYMENT.md) - Get it running
5. [Smoke Tests](../SmokeTests/README.md) - Verify it works

### For Feature Understanding

Deep dives into specific features:

| Feature | Description | Documentation |
|---------|-------------|---------------|
| Rate Limiting | IP-based throttling with bursts | [RATE_LIMITING.md](RATE_LIMITING.md) |
| Link Inference | AX.25 routing intelligence | [AX25_LINK_INFERENCE.md](AX25_LINK_INFERENCE.md) |
| Flapping Detection | Unstable link tracking | [LINK_FLAPPING.md](LINK_FLAPPING.md) |
| GeoIP | Location with privacy | [IP_AND_GEOIP_FEATURE.md](IP_AND_GEOIP_FEATURE.md) |
| RabbitMQ | Dual-path ingestion | [RABBITMQ_INTEGRATION.md](RABBITMQ_INTEGRATION.md) |

### For Operations

Running and maintaining the service:

| Task | Documentation |
|------|---------------|
| Deploy to production | [DEPLOYMENT.md](DEPLOYMENT.md) |
| Build Docker images | [DOCKER_PUBLISH.md](DOCKER_PUBLISH.md) |
| Monitor query load | [QUERY_FREQUENCY_DIAGNOSTICS.md](QUERY_FREQUENCY_DIAGNOSTICS.md) |
| Troubleshoot loops | [TRAFFIC_LOOP_FIX.md](TRAFFIC_LOOP_FIX.md) |
| Run smoke tests | [Smoke Tests](../SmokeTests/README.md) |
| View change history | [CHANGELOG.md](CHANGELOG.md) |

### For Troubleshooting

| Issue | Solution |
|-------|----------|
| Excessive traffic | [TRAFFIC_LOOP_FIX.md](TRAFFIC_LOOP_FIX.md) |
| Rate limiting problems | [RATE_LIMITING.md](RATE_LIMITING.md) |
| Link inference issues | [AX25_LINK_INFERENCE.md](AX25_LINK_INFERENCE.md) |
| Test data appearing | [TEST_CALLSIGN_FILTERING.md](../SmokeTests/TEST_CALLSIGN_FILTERING.md) |

## ?? Quick Searches

### By Technology

- **MySQL/Database**: [RATE_LIMITING.md](RATE_LIMITING.md), [QUERY_FREQUENCY_DIAGNOSTICS.md](QUERY_FREQUENCY_DIAGNOSTICS.md)
- **MQTT**: [RabbitMQ Integration](RABBITMQ_INTEGRATION.md), [Main README](../README.md)
- **RabbitMQ**: [RABBITMQ_INTEGRATION.md](RABBITMQ_INTEGRATION.md), [PHASE2_SUMMARY.md](PHASE2_SUMMARY.md)
- **Docker**: [DOCKER_PUBLISH.md](DOCKER_PUBLISH.md), [DEPLOYMENT.md](DEPLOYMENT.md)
- **Testing**: [Smoke Tests](../SmokeTests/README.md), [Copilot Instructions](../.github/copilot-instructions.md)

### By Event Type

- **NodeUpEvent/NodeDownEvent**: [Main README](../README.md), [Copilot Instructions](../.github/copilot-instructions.md)
- **LinkUpEvent/LinkStatus**: [LINK_FLAPPING.md](LINK_FLAPPING.md), [AX25_LINK_INFERENCE.md](AX25_LINK_INFERENCE.md)
- **L2Trace**: [AX25_LINK_INFERENCE.md](AX25_LINK_INFERENCE.md)
- **CircuitUpEvent/CircuitStatus**: [Main README](../README.md)

## ?? Documentation Standards

When contributing documentation:

1. **Use Markdown**: All docs use `.md` extension
2. **Naming Convention**: `UPPERCASE_WITH_UNDERSCORES.md` (except for special cases like README.md)
3. **Link to this index**: Reference `docs/README.md` from new documentation
4. **Keep it current**: Update docs when changing features
5. **Add diagrams**: Use ASCII art or mermaid for visuals
6. **Code examples**: Include runnable snippets
7. **Cross-reference**: Link to related documentation

## ??? Documentation Map

```
node-api/
??? README.md ...................... Main project overview
??? CONTRIBUTING.md ................ Contribution guidelines
??? .github/
?   ??? copilot-instructions.md .... Coding standards
??? docs/
?   ??? README.md .................. This file
?   ??? CHANGELOG.md ............... Project history
?   ??? DEPLOYMENT.md .............. Production deployment
?   ??? DOCKER_PUBLISH.md .......... Container publishing
?   ??? RATE_LIMITING.md ........... Rate limiting system
?   ??? AX25_LINK_INFERENCE.md ..... Link detection logic (consolidated)
?   ??? LINK_FLAPPING.md ........... Flapping detection
?   ??? IP_AND_GEOIP_FEATURE.md .... GeoIP tracking
?   ??? RABBITMQ_INTEGRATION.md .... Message queue support
?   ??? TIMESTAMP_TRACKING.md ...... Arrival timestamps
?   ??? ... (other feature docs)
??? SmokeTests/
?   ??? README.md .................. Testing guide
?   ??? QUICKSTART.md .............. Quick test guide
?   ??? TEST_CALLSIGN_FILTERING.md . Test data handling
??? Tests/ ......................... Unit tests
```

## ?? Need Help?

- **Can't find what you need?** Check the [main README](../README.md) or browse this index
- **Found a documentation issue?** Open a GitHub issue or submit a PR
- **Want to contribute?** See [Contributing Guide](../CONTRIBUTING.md) and [Copilot Instructions](../.github/copilot-instructions.md)

---

**Last Updated**: 2025-01-21  
**Maintained by**: node-api contributors
