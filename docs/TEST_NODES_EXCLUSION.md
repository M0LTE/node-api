# Test Nodes Exclusion Feature

Implemented: 2025-01-21

## Overview

Test nodes (marked with `is_test = TRUE`) are automatically excluded from all frontend displays.

## Database Migration

Run: `schema/migrations/012_add_is_test_column.sql`

## Marking Test Nodes

```sql
UPDATE `nodes` SET `is_test` = TRUE WHERE `callsign` = 'TEST';
UPDATE `nodes` SET `is_test` = TRUE WHERE `callsign` LIKE 'TEST-%';
```

## Frontend Behavior

- Test nodes are excluded from both amateur radio and CB sections
- Test nodes do not appear in node counts
- Test nodes are filtered at the data aggregation level

## Backend Changes

- Added `IsTest` property to `NodeState` model
- Updated `MySqlNetworkStateRepository` to persist `is_test` field
- Updated `NetworkStatePersistenceService` to copy `IsTest` field

## Frontend Changes

- `NodeTracker.getAggregatedNodes()` filters out nodes where `isTest = true`
- API loading extracts `isTest` from response
- MQTT handlers extract `isTest` from events (if provided)`

## Use Cases

- Development and testing without polluting production data
- Smoke tests that shouldn't appear in live dashboards
- QA validation nodes

---

See `NodeTracker.getAggregatedNodes()` in `index.html` for implementation.
