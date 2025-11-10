-- Migration: Add is_test column to nodes table
-- Date: 2025-01-21
-- Purpose: Mark test nodes to exclude them from production displays

ALTER TABLE `nodes` 
ADD COLUMN `is_test` BOOLEAN NOT NULL DEFAULT FALSE
AFTER `is_cb`;

CREATE INDEX `idx_is_test` ON `nodes` (`is_test`);

SELECT 'Migration complete: is_test column added to nodes table' AS Status;
