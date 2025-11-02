# AX.25 Routing and Link Inference

## Background

In AX.25 packet radio networks, when a user makes a Layer 2 connection **through** an intermediate node, that intermediate node will transmit frames using the **user's callsign**, not its own callsign.

### Example Scenario

```
User M0LTE connects to BBS M0ABC through intermediate node G8PZT:

M0LTE -----> G8PZT -----> M0ABC
         (RF link)    (RF link)
```

When G8PZT forwards traffic from M0LTE to M0ABC:
- The **source callsign** in the AX.25 frame is `M0LTE`
- The **actual transmitter** is `G8PZT`
- A station listening to G8PZT's transmissions will "hear" `M0LTE` as the source, even though G8PZT transmitted the frame

This is normal AX.25 behavior, but it creates a challenge for link inference.

## The Problem

If we naively infer links from L2Trace events, we would incorrectly conclude that:
- There is a **direct link** between M0LTE and M0ABC
- This link uses **RF** (because G8PZT transmitted it over RF)

In reality:
- M0LTE and M0ABC are **not directly connected** via RF
- The actual RF links are: M0LTE <-> G8PZT and G8PZT <-> M0ABC
- M0LTE is connected to M0ABC via an **L2 path through G8PZT**

## The Solution: Heuristic for Link Inference

We use the following heuristic to detect when a callsign is being "impersonated" by an intermediate node:

### Rule

```
IF (direction == "sent") AND (base_callsign(source) != base_callsign(reportFrom))
THEN
    The source callsign is being impersonated by the reporter
    DO NOT infer a direct link between source and destination
ELSE
    We can reliably infer the link
END IF
```

### Explanation

1. **`direction == "sent"`**: The reporting node transmitted the frame
   - If the `source` doesn't match the reporter, the reporter is forwarding for someone else
   - The source callsign is being impersonated

2. **`direction == "rcvd"`**: The reporting node received/overheard the frame
   - The source is the actual sender (not impersonated)
   - We can reliably infer the link

3. **Base callsign comparison**: We compare the base callsign (without SSID)
   - `G8PZT-1` and `G8PZT-2` have the same base (`G8PZT`) - legitimate
   - `G8PZT-1` and `M0LTE` have different bases - impersonation

### Edge Cases

- **Missing `direction` field**: Conservative approach - allow link inference
- **Unknown `direction` value**: Conservative approach - allow link inference
- **Case sensitivity**: Base callsign comparison is case-insensitive

## Implementation

### Code Location

- **Logic**: `node-api/Services/NetworkStateUpdater.cs`
  - Method: `CanInferLinkFromTrace(L2Trace trace)`
  - Used by: `UpdateFromL2Trace(L2Trace trace)`

- **Tests**: `Tests/NetworkStateUpdaterL2TraceTests.cs`
  - 15 comprehensive tests covering all scenarios

### Behavior

The `UpdateFromL2Trace` method:

1. **Always tracks node activity**: Source, destination, and reporter nodes are tracked regardless of link inference
2. **Conditionally updates link information**: Only updates link properties (like `IsRF`) when we can reliably infer the link
3. **Does not create links**: L2Trace events do not create links; only `LinkUpEvent` creates links
4. **Updates existing links**: If a link exists (created by LinkUpEvent), we may update its properties based on L2Trace data

### Logging

When impersonation is detected:

```
DEBUG: Not inferring link from L2Trace: reporter=G8PZT-1 is forwarding for source=M0LTE (direction=sent, base callsigns differ)
TRACE: Skipping link RF update: source M0LTE appears to be impersonated by G8PZT-1
```

## Testing

Run the tests with:

```bash
dotnet test Tests/ --filter "FullyQualifiedName~NetworkStateUpdaterL2TraceTests"
```

All 15 tests should pass, covering:
- Basic L2Trace processing
- Link inference with direction "rcvd"
- Link inference with direction "sent" (matching bases)
- Link inference with direction "sent" (different bases - impersonation)
- Edge cases (missing direction, no IsRF, non-existent links)
- Case sensitivity
- Real-world scenarios

## Impact

This change affects:

1. **Link RF status**: The `IsRF` property on links will only be set when we have reliable information
2. **Network visualization**: Links shown in the network map will more accurately reflect physical topology
3. **Link tracking**: Spurious "links" between stations that are actually connected via intermediate nodes will not be inferred

## Further Considerations

### Limitations

This heuristic works for most cases but has limitations:

1. **Port calls**: Some nodes use different callsigns on different ports. If a node uses port call `G8PZT-10` on port 1, but its main callsign is `G8PZT`, our heuristic might incorrectly flag this as impersonation.

2. **Multiple SSIDs**: A sysop might legitimately run multiple SSIDs on the same node (e.g., `G8PZT-1` for digipeating, `G8PZT-2` for NET/ROM). Our heuristic correctly handles this by comparing base callsigns.

3. **Received frames**: When `direction == "rcvd"`, we assume the source is genuine. However, a node might overhear a forwarded frame and incorrectly infer a direct link. This is a fundamental limitation of passive observation.

### Future Enhancements

Possible improvements:

1. **Path analysis**: Analyze digipeater paths to determine actual routing
2. **Correlation**: Cross-reference L2Trace data with LinkUpEvent data to validate links
3. **Confidence scoring**: Assign confidence scores to links based on multiple sources of evidence
4. **Configuration**: Allow sysops to declare port calls and SSID usage patterns

## References

- **AX.25 Specification**: [Link Layer Protocol (AX.25)](http://www.ax25.net/AX25.2.2-Jul%2098-2.pdf)
- **Project Documentation**: `Tests/Packet_Network_Monitoring_Project_v0.8a.txt`
- **Link Flapping**: `docs/LINK_FLAPPING.md`
