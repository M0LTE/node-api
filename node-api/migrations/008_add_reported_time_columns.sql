ALTER TABLE `traces` 
ADD COLUMN `reported_time` datetime(3) GENERATED ALWAYS AS (FROM_UNIXTIME(json_value(`json`,'$.time'))) STORED
AFTER `json`;

ALTER TABLE `l3traces` 
ADD COLUMN `reported_time` datetime(3) GENERATED ALWAYS AS (FROM_UNIXTIME(json_value(`json`,'$.time'))) STORED
AFTER `json`;

ALTER TABLE `events` 
ADD COLUMN `reported_time` datetime(3) GENERATED ALWAYS AS (FROM_UNIXTIME(json_value(`json`,'$.time'))) STORED
AFTER `json`;

ALTER TABLE `traces` ADD KEY `ix_traces_event_time` (`event_time`);
ALTER TABLE `l3traces` ADD KEY `ix_l3traces_event_time` (`event_time`);
ALTER TABLE `events` ADD KEY `ix_events_event_time` (`event_time`);
