-- Migration: Add is_cb column to nodes table
-- Date: 2025-01-21
-- Purpose: Distinguish between CB (Citizens Band) stations and amateur radio nodes

ALTER TABLE `nodes` 
ADD COLUMN `is_cb` BOOLEAN NOT NULL DEFAULT FALSE
AFTER `is_reporting_node`;

CREATE INDEX `idx_is_cb` ON `nodes` (`is_cb`);

SELECT 'Migration complete: is_cb column added to nodes table' AS Status;
