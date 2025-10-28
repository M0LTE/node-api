-- Migration: Add IP address and GeoIP columns to nodes table
-- Date: 2025-01-02
-- Description: Adds columns to store obfuscated IP address and GeoIP information for each node

-- Add columns for IP address and GeoIP data
ALTER TABLE `nodes`
ADD COLUMN `ip_address_obfuscated` VARCHAR(45) NULL AFTER `last_l2_trace`,
ADD COLUMN `geoip_country_code` VARCHAR(2) NULL AFTER `ip_address_obfuscated`,
ADD COLUMN `geoip_country_name` VARCHAR(100) NULL AFTER `geoip_country_code`,
ADD COLUMN `geoip_city` VARCHAR(100) NULL AFTER `geoip_country_name`,
ADD COLUMN `last_ip_update` DATETIME NULL AFTER `geoip_city`;

-- Add index for faster GeoIP queries
CREATE INDEX `idx_geoip_country` ON `nodes` (`geoip_country_code`);

-- To rollback this migration, run:
-- ALTER TABLE `nodes` 
-- DROP COLUMN `last_ip_update`,
-- DROP COLUMN `geoip_city`,
-- DROP COLUMN `geoip_country_name`,
-- DROP COLUMN `geoip_country_code`,
-- DROP COLUMN `ip_address_obfuscated`;
-- DROP INDEX `idx_geoip_country` ON `nodes`;
