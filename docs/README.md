# node-api Documentation

This repository now documents a split architecture:

| Service | Purpose |
|---|---|
| `node-api-ingester` | UDP ingress and RabbitMQ publishing |
| `node-api` | HTTP API, RabbitMQ consumption, MQTT publishing, state management, and persistence |

## Getting Started

- [Deployment Guide](DEPLOYMENT.md)
- [Database Configuration](DATABASE_CONFIGURATION.md)
- [Docker Publishing](DOCKER_PUBLISH.md)
- [RabbitMQ Integration](RABBITMQ_INTEGRATION.md)

## Data Ingestion

- [HTTP Datagram Ingestion](HTTP_DATAGRAM_INGESTION.md)
- [Typed Ingest Endpoints](TYPED_INGEST_ENDPOINTS.md)
- [Port Metadata Ingest & Band-Annotated Links](PORT_METADATA_INGEST.md)
- [Rate Limiting](RATE_LIMITING.md)

## Network State and Analysis

- [Reporting Nodes Feature](REPORTING_NODES_FEATURE.md)
- [AX.25 Link Inference](AX25_LINK_INFERENCE.md)
- [Link Flapping Detection](LINK_FLAPPING.md)
- [IP and GeoIP Feature](IP_AND_GEOIP_FEATURE.md)
- [Test Nodes Exclusion](TEST_NODES_EXCLUSION.md)
- [L2 Connection Analysis (Technical)](L2-Connection-Analysis-Technical.md)
- [L2 Connection Analysis (UI)](L2-Connection-Analysis-UI.md)
- [Multiple ReportFrom Feature](MULTIPLE_REPORTFROM_FEATURE.md)

## API and UI

- [API Restructure](API_RESTRUCTURE.md)
- [Sort Order Parameter](SORT_ORDER_PARAMETER.md)
- [OpenAPI Schema Enhancement](OPENAPI_SCHEMA_ENHANCEMENT.md)

## Reference

- [Changelog](CHANGELOG.md)

## Project Overview

The monitoring pipeline is:

1. UDP traffic arrives at `node-api-ingester` on port `13579`
2. The ingester publishes raw datagrams to RabbitMQ
3. `node-api` consumes, validates, rate-limits, enriches, and processes those datagrams
4. `node-api` publishes MQTT events, updates in-memory state, and persists to MySQL
5. `node-api` serves REST and OpenAPI/Scalar endpoints

## Technology Stack

- .NET 10
- ASP.NET Core
- FluentValidation
- Dapper
- MQTTnet
- RabbitMQ.Client
- xUnit
- Docker
