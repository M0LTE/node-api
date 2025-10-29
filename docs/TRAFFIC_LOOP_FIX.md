# Traffic Loop Fix - IP Address and GeoIP Feature

## Problem

After implementing the IP address and GeoIP tracking feature (commit `54e50bd`), a traffic loop was observed between the frontend service and the database container, causing excessive network traffic (1.59 MB/s sending, 1634 packets/s).

## Root Cause

The issue was caused by **duplicate state updates** in the data flow:

### Original (Problematic) Flow:

1. **UDP datagram arrives** ? `UdpNodeInfoListener` or `RabbitMqConsumer` ? `DatagramProcessor.ProcessDatagramAsync()`

2. **DatagramProcessor** did TWO things:
   - ? **Called `UpdateNodeIpAddress()`** which **directly modified in-memory network state**
   - ? Published event to MQTT topic `out/{EventType}`

3. **MqttStateSubscriber** listened to `out/#` topics and **also updated in-memory network state** via `NetworkStateUpdater`

4. **NetworkStatePersistenceService** runs every 30 seconds and writes ALL nodes to database

**Result**: Every UDP packet caused TWO state updates:
- Once in `DatagramProcessor.UpdateNodeIpAddress()` 
- Once in `MqttStateSubscriber` via `NetworkStateUpdater`

This meant that for every UDP packet received, the node's IP address, GeoIP country, city, and lastIpUpdate timestamp were being modified in-memory **twice**, causing excessive churn. When `NetworkStatePersistenceService` ran every 30 seconds, it would write all these constantly-changing nodes to the database, generating massive database traffic even if the actual data hadn't meaningfully changed.

## Solution

**Remove direct state modification from `DatagramProcessor`** to maintain a single source of truth for state updates through MQTT event flow.

### New (Fixed) Flow:

1. **UDP datagram arrives** ? `UdpNodeInfoListener` or `RabbitMqConsumer` ? `DatagramProcessor.ProcessDatagramAsync()`

2. **DatagramProcessor** now:
   - ? No longer modifies state directly
   - ? Publishes event to MQTT topic `out/{EventType}` **with IP address info as user properties**

3. **MqttStateSubscriber** listens to `out/#` topics and:
   - ? Updates in-memory network state **once** via `NetworkStateUpdater`
   - ? Extracts IP address and GeoIP data from MQTT user properties
   - ? Calls `NetworkStateUpdater.UpdateNodeIpInfo()` to update IP data

4. **NetworkStatePersistenceService** runs every 30 seconds and writes nodes to database

**Result**: Single state update per event, reducing database churn and network traffic.

## Changes Made

### 1. `node-api/Services/DatagramProcessor.cs`

**Removed**:
- `INetworkStateService _networkState` dependency
- `UpdateNodeIpAddress()` method that directly modified state

**Added**:
- IP address and GeoIP information added as **MQTT user properties** instead:
  - `ipObfuscated` - Obfuscated IP address
  - `geoCountryCode` - ISO country code
  - `geoCountryName` - Country name
  - `geoCity` - City name
- `ExtractReportingCallsign()` helper method

### 2. `node-api/Services/MqttStateSubscriber.cs`

**Modified**:
- `OnMessageReceivedAsync()` now extracts IP/GeoIP user properties from MQTT messages
- Calls new `UpdateNodeIpInfo()` method after processing each event type
- Added `UpdateNodeIpInfo()` helper method to conditionally update IP data

### 3. `node-api/Services/NetworkStateUpdater.cs`

**Added**:
- `UpdateNodeIpInfo()` method to update IP address and GeoIP information

### 4. `node-api/Program.cs`

**Modified**:
- Removed `networkState` parameter from `DatagramProcessor` constructor call

## Benefits

1. **Reduced database traffic**: No more duplicate state updates per UDP packet
2. **Single source of truth**: All state updates flow through `MqttStateSubscriber` ? `NetworkStateUpdater`
3. **Consistent architecture**: Follows the existing pattern where MQTT events drive state changes
4. **Reduced memory churn**: State objects modified once per event instead of twice

## Testing

After deployment:
- Monitor network traffic between frontend and database containers
- Verify IP address and GeoIP data still appears correctly on node details pages
- Confirm `NetworkStatePersistenceService` database writes are reasonable (30-second interval)
- Check that duplicate updates are no longer occurring

## Related Files

- `node-api/Services/DatagramProcessor.cs` - Event processing and MQTT publishing
- `node-api/Services/MqttStateSubscriber.cs` - MQTT event subscriber
- `node-api/Services/NetworkStateUpdater.cs` - In-memory state updates
- `node-api/Services/NetworkStatePersistenceService.cs` - Database persistence (every 30s)
- `node-api/Program.cs` - Service registration

## Future Improvements

Consider adding database update optimization:
- Only persist nodes that have actually changed since last persistence
- Track "dirty" flags on state objects
- Implement change detection before database upsert

This would further reduce database traffic, especially for nodes that report frequently but whose metadata rarely changes.
