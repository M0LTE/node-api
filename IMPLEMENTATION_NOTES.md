# TEST Callsign Filtering Implementation

## Overview
This implementation filters out reports from the "TEST" callsign (and its SSIDs TEST-0 through TEST-15) from HTML pages and API responses. The TEST callsign is used by the SmokeTest 1.0 test suite and should be excluded from normal network monitoring displays.

## Changes Made

### 1. **NetworkStateService.cs**
- Added `IsTestCallsign(string callsign)` method to the `INetworkStateService` interface
- Implemented regex-based matching for TEST callsigns: `^TEST(-([0-9]|1[0-5]))?$`
- Supports case-insensitive matching (TEST, test, TeSt, etc.)
- Matches TEST with no SSID and TEST-0 through TEST-15

### 2. **NodesController.cs**
- **`GetAllNodes()`**: Filters out TEST callsigns from the list
- **`GetNode(callsign)`**: No filtering - allows explicit retrieval of TEST nodes
- **`GetNodesByBaseCallsign(baseCallsign)`**: Filters out TEST unless explicitly requesting "TEST" base

### 3. **LinksController.cs**
- **`GetAllLinks()`**: Filters out links where either endpoint is a TEST callsign
- **`GetLink(canonicalKey)`**: No filtering - allows explicit retrieval
- **`GetLinksForNode(callsign)`**: Filters out TEST links unless explicitly requesting TEST
- **`GetLinksForBaseCallsign(baseCallsign)`**: Filters out TEST unless requesting TEST base

### 4. **CircuitsController.cs**
- **`GetAllCircuits()`**: Filters out circuits where either endpoint contains a TEST callsign
- **`GetCircuit(canonicalKey)`**: No filtering - allows explicit retrieval
- **`GetCircuitsForNode(callsign)`**: Filters out TEST circuits unless explicitly requesting TEST
- **`GetCircuitsForBaseCallsign(baseCallsign)`**: Filters out TEST unless requesting TEST base
- Added `ContainsTestCallsign(string address)` helper to parse circuit addresses (e.g., "G8PZT@G8PZT:14c0")

### 5. **MySqlTraceRepository.cs**
- Added MySQL regex filtering to exclude TEST callsigns from `reportFrom_idx` field
- Uses pattern: `^TEST(-([0-9]|1[0-5]))?$`
- Filtering is bypassed when explicitly searching for a specific `reportFrom` value
- Implemented `IsTestCallsign()` helper method for consistency

### 6. **HTML Pages**
No changes needed! The HTML pages (`index.html`, `node.html`, `links.html`, `circuits.html`) automatically benefit from the API filtering since they fetch data from:
- `/api/nodes` and `/api/nodes/base/{baseCallsign}`
- `/api/links` and `/api/links/base/{baseCallsign}`
- `/api/circuits` and `/api/circuits/base/{baseCallsign}`

## Behavior

### What Gets Filtered:
- TEST (no SSID)
- TEST-0 through TEST-15
- Case-insensitive (test, TeSt, TEST, etc.)

### What Does NOT Get Filtered:
- TEST-16 and higher (not valid test SSIDs)
- TESTING, TEST1, or other variations
- Explicit API requests by callsign/canonical key
- Requests specifically for TEST base callsign

### API Endpoints:

| Endpoint | Filtering Behavior |
|----------|-------------------|
| `GET /api/nodes` | Excludes TEST callsigns |
| `GET /api/nodes/TEST` | Returns TEST node (explicit request) |
| `GET /api/nodes/base/TEST` | Returns all TEST SSIDs (explicit request) |
| `GET /api/nodes/base/M0LTE` | Returns M0LTE nodes, excludes TEST |
| `GET /api/links` | Excludes links with TEST endpoints |
| `GET /api/links/node/TEST` | Returns TEST links (explicit request) |
| `GET /api/circuits` | Excludes circuits with TEST endpoints |
| `GET /api/traces` | Excludes traces from TEST reportFrom |
| `GET /api/traces?reportFrom=TEST` | Returns TEST traces (explicit request) |

## Testing

Created comprehensive test suite in `Tests/TestCallsignFilteringTests.cs`:
- ? IsTestCallsign recognition (28 test cases)
- ? Node filtering behavior
- ? Link filtering behavior
- ? Circuit filtering behavior
- ? Case insensitivity
- ? Edge cases and boundary conditions

All tests pass successfully.

## Impact on HTML Pages

### index.html
- Reporting Nodes section will not show TEST nodes
- Neighbor Nodes section will not show TEST nodes
- Real-time MQTT Statistics will still show TEST messages but users won't see them in the node lists

### node.html
- When viewing a specific node page, TEST nodes won't appear in "neighbor nodes" lists
- Direct navigation to `/node.html?callsign=TEST` will still work (explicit request)

### links.html
- Links involving TEST callsigns will not appear in the table
- Direct API requests for specific link keys will still work

### circuits.html
- Circuits involving TEST callsigns will not appear in the table
- Direct API requests for specific circuit keys will still work

## Migration Notes

- No database changes required
- Existing data remains intact
- TEST callsign data is preserved in the database and can still be accessed via explicit API requests
- MQTT messages from TEST are still processed and stored, just filtered from default views
- Rate limiting and blacklisting still applies to TEST callsigns
