ALTER TABLE `traces`
ADD COLUMN `reported_time` datetime(3) NULL
AFTER `json`;

ALTER TABLE `l3traces`
ADD COLUMN `reported_time` datetime(3) NULL
AFTER `json`;

ALTER TABLE `events`
ADD COLUMN `reported_time` datetime(3) NULL
AFTER `json`;

ALTER TABLE `traces` ADD KEY `ix_traces_reported_time` (`reported_time`);
ALTER TABLE `l3traces` ADD KEY `ix_l3traces_reported_time` (`reported_time`);
ALTER TABLE `events` ADD KEY `ix_events_reported_time` (`reported_time`);
