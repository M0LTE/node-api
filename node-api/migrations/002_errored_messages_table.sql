-- `node-data`.errored_messages definition

CREATE TABLE `errored_messages` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `timestamp` timestamp NOT NULL DEFAULT current_timestamp(),
  `reason` varchar(100) NOT NULL,
  `datagram` text DEFAULT NULL,
  `type` varchar(50) DEFAULT NULL,
  `errors` varchar(1024) DEFAULT NULL,
  `json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=62733 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;