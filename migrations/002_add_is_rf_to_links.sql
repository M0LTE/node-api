-- Migration: Add is_rf column to links table
-- This column tracks whether a link uses RF (true), internet (false), or unknown (NULL)
-- We only update this field when we receive definitive information from L2Trace events

ALTER TABLE `links` 
ADD COLUMN `is_rf` BOOLEAN NULL COMMENT 'Whether this link uses RF (true), internet (false), or unknown (NULL)' 
AFTER `initiator`;

-- Add index for filtering by RF status
ALTER TABLE `links`
ADD INDEX `idx_is_rf` (`is_rf`);
