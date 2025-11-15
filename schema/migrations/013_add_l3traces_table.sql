-- Migration: Add l3traces table
-- Date: 2025-01-21
-- Purpose: Create dedicated table for Layer 3 (NET/ROM) trace messages

CREATE TABLE IF NOT EXISTS `l3traces` (
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `timestamp` timestamp(3) NOT NULL DEFAULT current_timestamp(3),
  `json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL CHECK (json_valid(`json`)),
  `l3src_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.l3src')) STORED,
  `l3dst_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.l3dst')) STORED,
  `reportFrom_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.reportFrom')) STORED,
  `l4Type_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.l4Type')) STORED,
  PRIMARY KEY (`id`),
  KEY `ix_l3traces_ts_id` (`timestamp` DESC,`id` DESC),
  KEY `ix_l3traces_l3src_l3dst_ts` (`l3src_idx`,`l3dst_idx`,`timestamp`),
  KEY `ix_l3traces_reportFrom_ts` (`reportFrom_idx`,`timestamp`),
  KEY `ix_l3traces_l4Type_ts` (`l4Type_idx`,`timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

SELECT 'Migration complete: l3traces table created' AS Status;
