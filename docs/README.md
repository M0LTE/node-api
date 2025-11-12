# node-api Documentation

This directory contains documentation for the node-api packet network monitoring service.

## Table of Contents

### Getting Started
- [Deployment Guide](DEPLOYMENT.md) - How to deploy and run the service
- [Database Configuration](DATABASE_CONFIGURATION.md) - MySQL setup and configuration
- [Docker Publishing](DOCKER_PUBLISH.md) - Building and publishing Docker images

### Configuration
- [MQTT Configuration](MQTT_CONFIGURATION.md) - MQTT broker setup and message publishing
- [RabbitMQ Integration](RABBITMQ_INTEGRATION.md) - RabbitMQ setup and usage
- [Database Configuration](DATABASE_CONFIGURATION.md) - Database connection and schema

### Core Features

#### Data Ingestion
- [HTTP Datagram Ingestion](HTTP_DATAGRAM_INGESTION.md) - HTTP POST endpoint for receiving network events
- [Typed Ingest Endpoints](TYPED_INGEST_ENDPOINTS.md) - Type-specific ingestion endpoints

#### Network State Management
- [Reporting Nodes Feature](REPORTING_NODES_FEATURE.md) - Nodes that actively send UDP telemetry vs discovered nodes
- [AX.25 Link Inference](AX25_LINK_INFERENCE.md) - Automatic link detection from L2 trace data
- [Link Flapping Detection](LINK_FLAPPING.md) - Detection and tracking of unstable links
- [IP and GeoIP Feature](IP_AND_GEOIP_FEATURE.md) - IP address tracking and geolocation
- [Test Nodes Exclusion](TEST_NODES_EXCLUSION.md) - Filtering test/development nodes from production data

#### API Features
- [API Restructure](API_RESTRUCTURE.md) - API endpoint organization and structure
- [Rate Limiting](RATE_LIMITING.md) - Request rate limiting implementation
- [Rate Limiting Rolling Average](RATE_LIMITING_ROLLING_AVERAGE.md) - Rolling average calculations for rate metrics
- [Sort Order Parameter](SORT_ORDER_PARAMETER.md) - Sorting options for API endpoints
- [OpenAPI Schema Enhancement](OPENAPI_SCHEMA_ENHANCEMENT.md) - API documentation and schema improvements

#### Analysis Features
- [L2 Connection Analysis (Technical)](L2-Connection-Analysis-Technical.md) - Technical implementation of Layer 2 connection analysis
- [L2 Connection Analysis (UI)](L2-Connection-Analysis-UI.md) - User interface for connection analysis
- [Multiple ReportFrom Feature](MULTIPLE_REPORTFROM_FEATURE.md) - Support for events reported by multiple nodes

### Reference
- [Changelog](CHANGELOG.md) - Version history and changes

## Project Overview

node-api is a .NET 9.0 ASP.NET Core Web API service that provides packet network monitoring capabilities:

- **Listens for UDP datagrams** on port 13579 containing network event data
- **Validates and processes** various event types (nodes, links, circuits, traces)
- **Publishes events** to MQTT/RabbitMQ topics
- **Persists network state** to MySQL database
- **Exposes REST API** for querying network state
- **Provides OpenAPI documentation** at `/scalar`

## Technology Stack

- .NET 9.0
- ASP.NET Core (Minimal API)
- FluentValidation
- Dapper (MySQL ORM)
- MQTTnet
- xUnit (Testing)
- Docker

## Quick Links

- **API Documentation**: Available at `/scalar` when service is running
- **Source Repository**: https://github.com/M0LTE/node-api
- **Docker Image**: Published via GitHub Actions (see [DOCKER_PUBLISH.md](DOCKER_PUBLISH.md))

## Contributing

When adding new features, please:
1. Create feature documentation in this `docs/` directory
2. Update this README with a link to your new documentation
3. Add entries to [CHANGELOG.md](CHANGELOG.md)
4. Update relevant configuration documentation if needed
