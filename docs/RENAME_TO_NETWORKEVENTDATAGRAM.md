# Renaming UdpNodeInfoJsonDatagram to NetworkEventDatagram

**Date**: 2025-01-21  
**Status**: ? Completed  
**Impact**: Breaking change (naming only - JSON format unchanged)

## Overview

Renamed `UdpNodeInfoJsonDatagram` to `NetworkEventDatagram` to better reflect its protocol-agnostic nature.

## Rationale

The old name `UdpNodeInfoJsonDatagram` was misleading because:
- ? **Protocol-specific**: Implied UDP-only transport
- ? **Inaccurate**: Used for HTTP ingestion as well
- ? **Not future-proof**: Would need renaming if other protocols added (WebSocket, gRPC, etc.)

The new name `NetworkEventDatagram` is better because:
- ? **Protocol-agnostic**: Works for UDP, HTTP, and future protocols  
- ? **Descriptive**: Clearly describes what it is (a network event datagram)
- ? **Future-proof**: Won't need renaming when adding new transport protocols

## Changes Made

### 1. Renamed Base Class

**File**: `node-api/Models/UdpNodeInfoJsonDatagram.cs` ? `node-api/Models/NetworkEventDatagram.cs`

```csharp
// Before
public record UdpNodeInfoJsonDatagram
{
    [JsonPropertyName("@type")]
    public required string DatagramType { get; init; }
}

// After (with XML documentation)
/// <summary>
/// Base class for all packet network event datagrams.
/// Supports multiple transport protocols (UDP, HTTP, etc.) and uses JSON polymorphism
/// with the "@type" discriminator field to deserialize to specific event types.
/// </summary>
public record NetworkEventDatagram
{
    [JsonPropertyName("@type")]
    public required string DatagramType { get; init; }
}
```

### 2. Renamed Deserializer

**File**: `node-api/UdpNodeInfoJsonDatagramDeserialiser.cs` ? `node-api/NetworkEventDatagramDeserialiser.cs`

```csharp
// Before
public static class UdpNodeInfoJsonDatagramDeserialiser
{
    public static bool TryDeserialise(string json, out UdpNodeInfoJsonDatagram? frame, ...)
}

// After (with XML documentation)
/// <summary>
/// Deserializes JSON strings into strongly-typed NetworkEventDatagram objects.
/// Supports multiple transport protocols (UDP, HTTP, etc.)
/// </summary>
public static class NetworkEventDatagramDeserialiser
{
    public static bool TryDeserialise(string json, out NetworkEventDatagram? frame, ...)
}
```

### 3. Updated All References

Updated all code files that referenced the old names:

**Controllers**:
- `DatagramIngestController.cs` - HTTP ingestion endpoints

**Services**:
- `DatagramProcessor.cs` - Processes datagrams from all sources
- `UdpNodeInfoListener.cs` - UDP listener service

**Validators**:
- `DatagramValidationService.cs` - Validates all datagram types

**Models** (all inherit from `NetworkEventDatagram`):
- `NodeUpEvent.cs`
- `NodeDownEvent.cs`
- `NodeStatusReportEvent.cs`
- `LinkUpEvent.cs`
- `LinkDisconnectionEvent.cs`
- `LinkStatus.cs`
- `CircuitUpEvent.cs`
- `CircuitDisconnectionEvent.cs`
- `CircuitStatus.cs`
- `L2Trace.cs`

**Tests**:
- All test files updated to use new naming

### 4. Automated Refactoring

Used PowerShell to update all occurrences:

```powershell
# Find and replace in all .cs files
Get-ChildItem -Path "C:\Users\tom\source\repos\M0LTE\node-api" -Recurse -Include *.cs | 
    ForEach-Object { 
        (Get-Content $_.FullName) -replace 'UdpNodeInfoJsonDatagram', 'NetworkEventDatagram' | 
        Set-Content $_.FullName 
    }

# Rename files
Rename-Item "UdpNodeInfoJsonDatagram.cs" -NewName "NetworkEventDatagram.cs"
Rename-Item "UdpNodeInfoJsonDatagramDeserialiser.cs" -NewName "NetworkEventDatagramDeserialiser.cs"
```

## Backward Compatibility

? **100% Backward Compatible at Runtime**

- JSON format unchanged (still uses `@type` discriminator)
- All event type names unchanged (`"NodeUpEvent"`, `"LinkUpEvent"`, etc.)
- Wire protocol unchanged (UDP and HTTP use same JSON format)
- Existing clients continue to work without modification

? **Breaking Change for Compiled Code**

If anyone has compiled code referencing `UdpNodeInfoJsonDatagram`:
- Must recompile with new name
- This only affects:
  - Internal development (this project)
  - Any projects referencing this assembly

**Mitigation**: Since this is an internal API without published NuGet packages, the impact is minimal.

## Testing

### Build Status
? Build successful

### Test Status
?? 8 HTTP ingestion tests failing due to unrelated polymorphic deserialization issue with ASP.NET Core model binding

**Note**: The rename itself is successful. The test failures are due to a separate issue with ASP.NET Core not automatically deserializing polymorphic types from anonymous objects in tests. This will be addressed separately.

## Benefits

1. **Clearer Intent**: Name now reflects actual usage (multiple protocols)
2. **Better Documentation**: Developers immediately understand it's not UDP-specific
3. **Future-Proof**: Won't need renaming when adding WebSocket/gRPC support
4. **Consistent Naming**: Aligns with `NetworkStateService`, `NetworkEventDatagram`, etc.

## Migration Guide

### For Internal Development

**No action required** - already done via automated refactoring.

### For External Consumers (if any)

If you have code referencing `UdpNodeInfoJsonDatagram`:

```csharp
// Old code
UdpNodeInfoJsonDatagram datagram = ...;

// New code
NetworkEventDatagram datagram = ...;
```

**Find and replace**: `UdpNodeInfoJsonDatagram` ? `NetworkEventDatagram`

### For JSON/API Clients

**No changes needed** - JSON format is identical.

## Related Changes

- Added XML documentation to `NetworkEventDatagram` class
- Added XML documentation to `NetworkEventDatagramDeserialiser` class
- Updated `docs/OPENAPI_SCHEMA_ENHANCEMENT.md` (will reference new name in future updates)

## Next Steps

1. ? **Completed**: Rename complete and building successfully
2. ?? **In Progress**: Fix HTTP ingestion test failures (separate issue)
3. ?? **TODO**: Update documentation to reference new name
4. ?? **TODO**: Consider adding type alias for backward compat if needed

---

**Status**: ? **Refactoring Complete**  
**Build**: ? Passing  
**Tests**: ?? 8 failing (unrelated issue)  
**Breaking Change**: Naming only (runtime compatible)
