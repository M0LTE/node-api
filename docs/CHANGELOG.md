# Changelog

Notable changes, bug fixes, and feature implementations in node-api.

## Format

Each entry includes:
- **Date**: When the change was made
- **Type**: Feature, Fix, Enhancement, Refactor
- **Description**: What changed and why
- **Impact**: What this affects
- **References**: Related documentation or issues

---

## 2025-01-21 - AX.25 Link Inference Heuristic

**Type**: Feature  
**Status**: ? Complete and validated

### Summary
Implemented intelligent link inference to prevent false link detection when AX.25 intermediate nodes forward traffic using impersonated callsigns.

### Changes
- Added `CanInferLinkFromTrace()` heuristic in `NetworkStateUpdater.cs`
- Added `GetBaseCallsign()` helper for SSID extraction
- Modified `UpdateFromL2Trace()` to check heuristic before updating link properties
- Added 15 comprehensive tests in `NetworkStateUpdaterL2TraceTests.cs`

### Impact
- **Network topology accuracy**: Improved - fewer spurious links
- **Link RF status**: Only updated when reliable
- **Backwards compatibility**: Fully compatible with conservative defaults

### Documentation
- [AX25_LINK_INFERENCE.md](AX25_LINK_INFERENCE.md)
- [AX25_ROUTING_SCENARIOS.md](AX25_ROUTING_SCENARIOS.md)
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md)
- [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)
- [FINAL_VALIDATION.md](FINAL_VALIDATION.md)

### Test Results
- New tests: 15/15 passed
- Full suite: 1,009/1,009 passed
- No regressions

---

## 2025-01-XX - Total Requests Display Fix

**Type**: Fix  
**Component**: UI/Frontend

### Summary
Corrected the total requests display to show accurate request counts.

### Documentation
- [FIX_TOTAL_REQUESTS_DISPLAY.md](FIX_TOTAL_REQUESTS_DISPLAY.md)

---

## 2025-01-XX - Traffic Loop Resolution

**Type**: Fix  
**Component**: IP & GeoIP Feature

### Summary
Fixed excessive traffic loop (1.59 MB/s) caused by duplicate state updates in IP address and GeoIP tracking feature.

### Root Cause
Duplicate state updates in data flow causing continuous frontend-database traffic.

### Documentation
- [TRAFFIC_LOOP_FIX.md](TRAFFIC_LOOP_FIX.md)

---

## 2024-XX-XX - Link Flapping Detection

**Type**: Feature  
**Component**: Network State

### Summary
Added link flapping detection to identify unstable connections that repeatedly go up and down.

### Features
- Tracks connection/disconnection transitions
- Configurable time windows and thresholds
- Warning logs for flapping links
- API query support

### Documentation
- [LINK_FLAPPING.md](LINK_FLAPPING.md)

---

## 2024-XX-XX - IP Address & GeoIP Tracking

**Type**: Feature  
**Component**: Privacy & Location

### Summary
Added IP address tracking with privacy-preserving obfuscation and GeoIP location lookup.

### Features
- Server-side IP obfuscation (last 2 octets only)
- GeoIP country and city lookup
- Privacy-first design
- No PII stored

### Documentation
- [IP_AND_GEOIP_FEATURE.md](IP_AND_GEOIP_FEATURE.md)
- [TRAFFIC_LOOP_FIX.md](TRAFFIC_LOOP_FIX.md)

---

## 2024-XX-XX - RabbitMQ Integration (Phase 2)

**Type**: Feature  
**Component**: Message Queue

### Summary
Implemented full datagram processing from RabbitMQ, creating a dual-path ingestion system.

### Features
- UDP listener continues to work
- RabbitMQ subscriber processes same events
- Prepares for future microservice separation
- Resilience through redundancy

### Documentation
- [RABBITMQ_INTEGRATION.md](RABBITMQ_INTEGRATION.md)
- [PHASE2_SUMMARY.md](PHASE2_SUMMARY.md)

---

## 2024-XX-XX - Rolling Average Rate Limiting

**Type**: Enhancement  
**Component**: Rate Limiting

### Summary
Enhanced rate limiting system with rolling average and burst support.

### Features
- Dual-threshold approach
- Sustained average tracking
- Short burst allowance
- Configurable windows

### Documentation
- [RATE_LIMITING_ROLLING_AVERAGE.md](RATE_LIMITING_ROLLING_AVERAGE.md)
- [RATE_LIMITING.md](RATE_LIMITING.md)

---

## 2024-XX-XX - UDP Rate Limiting System

**Type**: Feature  
**Component**: Security & Protection

### Summary
Implemented IP-based rate limiting for UDP datagrams to protect against excessive traffic and abuse.

### Features
- Sliding window algorithm
- CIDR-based blacklisting
- Automatic un-rate-limiting
- Per-IP tracking

### Documentation
- [RATE_LIMITING.md](RATE_LIMITING.md)

---

## 2024-XX-XX - Timestamp Tracking

**Type**: Feature  
**Component**: Data Collection

### Summary
Added exact arrival time tracking for UDP datagrams, persisted to database.

### Features
- Server-side timestamp capture
- Stored in `traces` and `events` tables
- Microsecond precision
- Separate from event-reported timestamps

### Documentation
- [TIMESTAMP_TRACKING.md](TIMESTAMP_TRACKING.md)

---

## 2024-XX-XX - Query Frequency Diagnostics

**Type**: Feature  
**Component**: Diagnostics

### Summary
Added diagnostics endpoint to track database query frequency.

### Features
- Real-time query statistics
- Helps diagnose traffic issues
- Identifies natural growth vs bugs
- REST API endpoint

### Documentation
- [QUERY_FREQUENCY_DIAGNOSTICS.md](QUERY_FREQUENCY_DIAGNOSTICS.md)

---

## Legend

**Types**:
- **Feature**: New functionality
- **Fix**: Bug fix or issue resolution
- **Enhancement**: Improvement to existing feature
- **Refactor**: Code reorganization without behavior change
- **Security**: Security-related change
- **Performance**: Performance optimization

**Status**:
- ✅ Complete and validated
- 🚧 In progress
- 📋 Planned
- ⚠️ Known issues

---

**Maintained by**: node-api contributors  
**Format**: [Keep a Changelog](https://keepachangelog.com/)
