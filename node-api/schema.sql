/*M!999999\- enable the sandbox mode */ 
-- MariaDB dump 10.19  Distrib 10.11.14-MariaDB, for debian-linux-gnu (x86_64)
--
-- Host: localhost    Database: node-data
-- ------------------------------------------------------
-- Server version	10.11.14-MariaDB-0+deb12u2

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `circuits`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `circuits` (
  `canonical_key` varchar(100) NOT NULL,
  `endpoint1` varchar(50) NOT NULL,
  `endpoint2` varchar(50) NOT NULL,
  `status` enum('Active','Disconnected') NOT NULL DEFAULT 'Active',
  `connected_at` datetime(6) DEFAULT NULL,
  `disconnected_at` datetime(6) DEFAULT NULL,
  `last_update` datetime(6) NOT NULL,
  `initiator` varchar(50) DEFAULT NULL,
  `ep1_node` varchar(20) DEFAULT NULL,
  `ep1_circuit_id` int(11) DEFAULT NULL,
  `ep1_direction` varchar(10) DEFAULT NULL,
  `ep1_service` int(11) DEFAULT NULL,
  `ep1_remote` varchar(50) DEFAULT NULL,
  `ep1_local` varchar(50) DEFAULT NULL,
  `ep1_segments_sent` int(11) DEFAULT NULL,
  `ep1_segments_received` int(11) DEFAULT NULL,
  `ep1_segments_resent` int(11) DEFAULT NULL,
  `ep1_segments_queued` int(11) DEFAULT NULL,
  `ep1_bytes_sent` bigint(20) DEFAULT NULL,
  `ep1_bytes_received` bigint(20) DEFAULT NULL,
  `ep1_last_update` datetime(6) DEFAULT NULL,
  `ep2_node` varchar(20) DEFAULT NULL,
  `ep2_circuit_id` int(11) DEFAULT NULL,
  `ep2_direction` varchar(10) DEFAULT NULL,
  `ep2_service` int(11) DEFAULT NULL,
  `ep2_remote` varchar(50) DEFAULT NULL,
  `ep2_local` varchar(50) DEFAULT NULL,
  `ep2_segments_sent` int(11) DEFAULT NULL,
  `ep2_segments_received` int(11) DEFAULT NULL,
  `ep2_segments_resent` int(11) DEFAULT NULL,
  `ep2_segments_queued` int(11) DEFAULT NULL,
  `ep2_bytes_sent` bigint(20) DEFAULT NULL,
  `ep2_bytes_received` bigint(20) DEFAULT NULL,
  `ep2_last_update` datetime(6) DEFAULT NULL,
  `updated_at` datetime(6) NOT NULL DEFAULT current_timestamp(6) ON UPDATE current_timestamp(6),
  PRIMARY KEY (`canonical_key`),
  KEY `idx_endpoint1` (`endpoint1`),
  KEY `idx_endpoint2` (`endpoint2`),
  KEY `idx_status` (`status`),
  KEY `idx_last_update` (`last_update`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `errored_messages`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `errored_messages` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `timestamp` timestamp NOT NULL DEFAULT current_timestamp(),
  `reason` varchar(100) NOT NULL,
  `datagram` text DEFAULT NULL,
  `type` varchar(50) DEFAULT NULL,
  `errors` varchar(1024) DEFAULT NULL,
  `json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=7763 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `events`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
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
) ENGINE=InnoDB AUTO_INCREMENT=1119804 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `links`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `links` (
  `canonical_key` varchar(50) NOT NULL,
  `endpoint1` varchar(20) NOT NULL,
  `endpoint2` varchar(20) NOT NULL,
  `status` enum('Active','Disconnected') NOT NULL DEFAULT 'Active',
  `connected_at` datetime(6) DEFAULT NULL,
  `disconnected_at` datetime(6) DEFAULT NULL,
  `last_update` datetime(6) NOT NULL,
  `initiator` varchar(20) DEFAULT NULL,
  `is_rf` tinyint(1) DEFAULT NULL COMMENT 'Whether this link uses RF (true), internet (false), or unknown (NULL)',
  `flap_count` int(11) NOT NULL DEFAULT 0 COMMENT 'Number of up/down transitions within the current flap detection window',
  `flap_window_start` datetime(6) DEFAULT NULL COMMENT 'Start time of the current flap detection window',
  `last_flap_time` datetime(6) DEFAULT NULL COMMENT 'Timestamp of the most recent flap (up/down transition)',
  `ep1_node` varchar(20) DEFAULT NULL,
  `ep1_link_id` int(11) DEFAULT NULL,
  `ep1_direction` varchar(10) DEFAULT NULL,
  `ep1_port` varchar(20) DEFAULT NULL,
  `ep1_remote` varchar(20) DEFAULT NULL,
  `ep1_local` varchar(20) DEFAULT NULL,
  `ep1_frames_sent` int(11) DEFAULT NULL,
  `ep1_frames_received` int(11) DEFAULT NULL,
  `ep1_frames_resent` int(11) DEFAULT NULL,
  `ep1_frames_queued` int(11) DEFAULT NULL,
  `ep1_frames_queued_peak` int(11) DEFAULT NULL,
  `ep1_bytes_sent` bigint(20) DEFAULT NULL,
  `ep1_bytes_received` bigint(20) DEFAULT NULL,
  `ep1_bps_tx_mean` int(11) DEFAULT NULL,
  `ep1_bps_rx_mean` int(11) DEFAULT NULL,
  `ep1_frame_queue_max` int(11) DEFAULT NULL,
  `ep1_l2_rtt_ms` int(11) DEFAULT NULL,
  `ep1_up_for_secs` int(11) DEFAULT NULL,
  `ep1_last_update` datetime(6) DEFAULT NULL,
  `ep2_node` varchar(20) DEFAULT NULL,
  `ep2_link_id` int(11) DEFAULT NULL,
  `ep2_direction` varchar(10) DEFAULT NULL,
  `ep2_port` varchar(20) DEFAULT NULL,
  `ep2_remote` varchar(20) DEFAULT NULL,
  `ep2_local` varchar(20) DEFAULT NULL,
  `ep2_frames_sent` int(11) DEFAULT NULL,
  `ep2_frames_received` int(11) DEFAULT NULL,
  `ep2_frames_resent` int(11) DEFAULT NULL,
  `ep2_frames_queued` int(11) DEFAULT NULL,
  `ep2_frames_queued_peak` int(11) DEFAULT NULL,
  `ep2_bytes_sent` bigint(20) DEFAULT NULL,
  `ep2_bytes_received` bigint(20) DEFAULT NULL,
  `ep2_bps_tx_mean` int(11) DEFAULT NULL,
  `ep2_bps_rx_mean` int(11) DEFAULT NULL,
  `ep2_frame_queue_max` int(11) DEFAULT NULL,
  `ep2_l2_rtt_ms` int(11) DEFAULT NULL,
  `ep2_up_for_secs` int(11) DEFAULT NULL,
  `ep2_last_update` datetime(6) DEFAULT NULL,
  `updated_at` datetime(6) NOT NULL DEFAULT current_timestamp(6) ON UPDATE current_timestamp(6),
  PRIMARY KEY (`canonical_key`),
  KEY `idx_endpoint1` (`endpoint1`),
  KEY `idx_endpoint2` (`endpoint2`),
  KEY `idx_status` (`status`),
  KEY `idx_last_update` (`last_update`),
  KEY `idx_is_rf` (`is_rf`),
  KEY `idx_flap_count` (`flap_count`),
  KEY `idx_last_flap_time` (`last_flap_time`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `nodes`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `nodes` (
  `callsign` varchar(20) NOT NULL,
  `alias` varchar(10) DEFAULT NULL,
  `locator` varchar(8) DEFAULT NULL,
  `latitude` decimal(10,6) DEFAULT NULL,
  `longitude` decimal(11,6) DEFAULT NULL,
  `software` varchar(50) DEFAULT NULL,
  `version` varchar(20) DEFAULT NULL,
  `uptime_secs` int(11) DEFAULT NULL,
  `links_in` int(11) DEFAULT NULL,
  `links_out` int(11) DEFAULT NULL,
  `circuits_in` int(11) DEFAULT NULL,
  `circuits_out` int(11) DEFAULT NULL,
  `l3_relayed` int(11) DEFAULT NULL,
  `status` enum('Unknown','Online','Offline') NOT NULL DEFAULT 'Unknown',
  `last_seen` datetime(6) DEFAULT NULL,
  `first_seen` datetime(6) DEFAULT NULL,
  `last_status_update` datetime(6) DEFAULT NULL,
  `last_up_event` datetime(6) DEFAULT NULL,
  `last_down_event` datetime(6) DEFAULT NULL,
  `l2_trace_count` int(11) NOT NULL DEFAULT 0,
  `last_l2_trace` datetime(6) DEFAULT NULL,
  `ip_address_obfuscated` varchar(45) DEFAULT NULL,
  `geoip_country_code` varchar(2) DEFAULT NULL,
  `geoip_country_name` varchar(100) DEFAULT NULL,
  `geoip_city` varchar(100) DEFAULT NULL,
  `last_ip_update` datetime DEFAULT NULL,
  `updated_at` datetime(6) NOT NULL DEFAULT current_timestamp(6) ON UPDATE current_timestamp(6),
  PRIMARY KEY (`callsign`),
  KEY `idx_status` (`status`),
  KEY `idx_last_seen` (`last_seen`),
  KEY `idx_software` (`software`),
  KEY `idx_geoip_country` (`geoip_country_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `traces`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `traces` (
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `timestamp` timestamp(3) NOT NULL DEFAULT current_timestamp(3),
  `json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL CHECK (json_valid(`json`)),
  `srce_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.srce')) STORED,
  `dest_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.dest')) STORED,
  `type_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$."@type"')) STORED,
  `reportFrom_idx` varchar(32) GENERATED ALWAYS AS (json_value(`json`,'$.reportFrom')) STORED,
  PRIMARY KEY (`id`),
  KEY `ix_traces_ts_id` (`timestamp` DESC,`id` DESC),
  KEY `ix_traces_srce_dest_type_ts` (`srce_idx`,`dest_idx`,`type_idx`,`timestamp`),
  KEY `ix_traces_reportFrom_srce_dest_ts` (`reportFrom_idx`,`srce_idx`,`dest_idx`,`timestamp`)
) ENGINE=InnoDB AUTO_INCREMENT=26739688 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-11-08 12:27:37
