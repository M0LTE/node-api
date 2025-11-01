-- Migration: Add flap tracking columns to links table
-- These columns track link flapping (repeated up/down transitions in a short time)
-- This enables monitoring of unstable links in the network

ALTER TABLE `links` 
ADD COLUMN `flap_count` INT NOT NULL DEFAULT 0 COMMENT 'Number of up/down transitions within the current flap detection window'
AFTER `is_rf`;

ALTER TABLE `links`
ADD COLUMN `flap_window_start` DATETIME(6) NULL COMMENT 'Start time of the current flap detection window'
AFTER `flap_count`;

ALTER TABLE `links`
ADD COLUMN `last_flap_time` DATETIME(6) NULL COMMENT 'Timestamp of the most recent flap (up/down transition)'
AFTER `flap_window_start`;

-- Add index for querying flapping links
ALTER TABLE `links`
ADD INDEX `idx_flap_count` (`flap_count`);

ALTER TABLE `links`
ADD INDEX `idx_last_flap_time` (`last_flap_time`);
