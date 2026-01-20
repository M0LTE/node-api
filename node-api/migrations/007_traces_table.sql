-- `node-data`.traces definition

CREATE TABLE `traces` (
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `timestamp` timestamp(3) NOT NULL DEFAULT current_timestamp(3),
  `json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL CHECK (json_valid(`json`)),
  `srce_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.srce')) STORED,
  `dest_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.dest')) STORED,
  `type_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$."@type"')) STORED,
  `reportFrom_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.reportFrom')) STORED,
  `l2type` varchar(10) GENERATED ALWAYS AS (json_value(`json`,'$.l2Type')) STORED,
  `dirn` varchar(4) GENERATED ALWAYS AS (json_value(`json`,'$.dirn')) STORED,
  PRIMARY KEY (`id`),
  KEY `ix_traces_ts_id` (`timestamp` DESC,`id` DESC),
  KEY `ix_traces_srce_dest_type_ts` (`srce_idx`,`dest_idx`,`type_idx`,`timestamp`),
  KEY `ix_traces_reportFrom_srce_dest_ts` (`reportFrom_idx`,`srce_idx`,`dest_idx`,`timestamp`)
) ENGINE=InnoDB AUTO_INCREMENT=275630221 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;