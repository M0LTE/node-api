# CB Nodes Feature

Implemented: 2025-01-21

Separates CB (Citizens Band) stations from amateur radio nodes.

## Database Migration

Run: `schema/migrations/011_add_is_cb_column.sql`

## Manual Marking

```sql
UPDATE `nodes` SET `is_cb` = TRUE WHERE `callsign` = 'CB123';
```

## Frontend

- Amateur radio nodes: Main grid
- CB nodes: Collapsed section below

See index.html for implementation details.
