-- `node-data`.l3traces definition

CREATE TABLE `l3traces` (
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `timestamp` timestamp(3) NOT NULL DEFAULT current_timestamp(3),
  `json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL CHECK (json_valid(`json`)),
  `l3src_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.l3src')) STORED,
  `l3dst_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.l3dst')) STORED,
  `reportFrom_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.reportFrom')) STORED,
  `l4Type_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.l4Type')) STORED,
  `l3type_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.l3Type')) STORED,
  PRIMARY KEY (`id`),
  KEY `ix_l3traces_ts_id` (`timestamp` DESC,`id` DESC),
  KEY `ix_l3traces_l3src_l3dst_ts` (`l3src_idx`,`l3dst_idx`,`timestamp`),
  KEY `ix_l3traces_reportFrom_ts` (`reportFrom_idx`,`timestamp`),
  KEY `ix_l3traces_l4Type_ts` (`l4Type_idx`,`timestamp`)
) ENGINE=InnoDB AUTO_INCREMENT=5138293 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;