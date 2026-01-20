-- `node-data`.events definition

CREATE TABLE `events` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `timestamp` timestamp(3) NOT NULL DEFAULT current_timestamp(3),
  `json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL CHECK (json_valid(`json`)),
  `node_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.node')) STORED,
  `nodeCall_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.nodeCall')) STORED,
  `type_idx` varchar(64) GENERATED ALWAYS AS (json_value(`json`,'$."@type"')) STORED,
  `direction_idx` varchar(16) GENERATED ALWAYS AS (json_value(`json`,'$.direction')) STORED,
  `remote_idx` varchar(64) GENERATED ALWAYS AS (json_value(`json`,'$.remote')) STORED,
  `local_idx` varchar(64) GENERATED ALWAYS AS (json_value(`json`,'$.local')) STORED,
  `port_idx` varchar(16) GENERATED ALWAYS AS (json_value(`json`,'$.port')) STORED,
  `software_idx` varchar(16) GENERATED ALWAYS AS (json_value(`json`,'$.software')) STORED,
  PRIMARY KEY (`id`),
  KEY `ix_events_ts_id` (`timestamp` DESC,`id` DESC),
  KEY `ix_events_node_ts` (`node_idx`,`timestamp` DESC,`id` DESC),
  KEY `ix_events_nodeCall_ts` (`nodeCall_idx`,`timestamp` DESC,`id` DESC),
  KEY `ix_events_type_ts` (`type_idx`,`timestamp` DESC,`id` DESC),
  KEY `ix_events_direction_ts` (`direction_idx`,`timestamp` DESC,`id` DESC),
  KEY `ix_events_remote_ts` (`remote_idx`,`timestamp` DESC,`id` DESC),
  KEY `ix_events_local_ts` (`local_idx`,`timestamp` DESC,`id` DESC),
  KEY `ix_events_port_ts` (`port_idx`,`timestamp` DESC,`id` DESC)
) ENGINE=InnoDB AUTO_INCREMENT=14077639 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;