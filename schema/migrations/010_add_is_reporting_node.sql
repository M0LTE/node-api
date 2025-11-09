-- Migration: Add is_reporting_node column to nodes table
-- Date: 2025-01-21
-- Description: Track which nodes send UDP telemetry vs discovered only via other nodes' events

-- Add column (defaults to FALSE for existing nodes)
ALTER TABLE `nodes` 
ADD COLUMN `is_reporting_node` BOOLEAN NOT NULL DEFAULT FALSE
AFTER `last_ip_update`;

-- Add index for efficient filtering
CREATE INDEX `idx_is_reporting_node` ON `nodes` (`is_reporting_node`);

-- Optional: Mark existing nodes with recent telemetry as reporting
-- (Assumes nodes with recent last_up_event or last_status_update are reporting)
UPDATE `nodes` 
SET `is_reporting_node` = TRUE 
WHERE `last_up_event` IS NOT NULL OR `last_status_update` IS NOT NULL;

-- Verify
SELECT 
    COUNT(*) AS total_nodes,
    SUM(is_reporting_node) AS reporting_nodes,
    SUM(NOT is_reporting_node) AS discovered_only_nodes
FROM `nodes`;
