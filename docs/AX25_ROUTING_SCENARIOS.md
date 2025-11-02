# AX.25 Routing Scenarios - Visual Guide

## Scenario 1: Direct Connection (No Impersonation)

```
???????????                          ???????????
? M0LTE   ?  ???????????????????????>? G8PZT   ?
???????????      RF transmission     ???????????
                                         ?
                                         ? Reports L2Trace:
                                         ? reportFrom: "G8PZT"
                                         ? dirn: "rcvd"
                                         ? source: "M0LTE"
                                         ? dest: "G8PZT"
                                         ? isRF: true
                                         ?
                                    ? CAN INFER LINK
                                    M0LTE <-> G8PZT
                                    isRF: true
```

**Analysis**: 
- G8PZT received (`rcvd`) a frame
- Source is M0LTE (the actual transmitter)
- Link can be reliably inferred

---

## Scenario 2: Node Transmitting as Itself (No Impersonation)

```
                   G8PZT Reports:
                   reportFrom: "G8PZT-1"
                   dirn: "sent"
                   source: "G8PZT-1"
                   dest: "M0LTE"
                   isRF: true
                        ?
                        ? RF transmission
                        ?
???????????      ???????????????????>      ???????????
? G8PZT-1 ?                                ? M0LTE   ?
???????????                                ???????????

? CAN INFER LINK
G8PZT-1 <-> M0LTE
isRF: true
```

**Analysis**:
- G8PZT-1 sent (`sent`) a frame
- Source is G8PZT-1 (same as reporter)
- Base callsigns match: G8PZT == G8PZT
- Node is transmitting as itself
- Link can be reliably inferred

---

## Scenario 3: Different SSID, Same Base (No Impersonation)

```
                   G8PZT-1 Reports:
                   reportFrom: "G8PZT-1"
                   dirn: "sent"
                   source: "G8PZT-2"
                   dest: "M0LTE"
                   isRF: false
                        ?
                        ? Internet/Ethernet
                        ?
???????????      ?????????????????????>    ???????????
? G8PZT-2 ?                                ? M0LTE   ?
???????????                                ???????????

? CAN INFER LINK
G8PZT-2 <-> M0LTE
isRF: false
```

**Analysis**:
- G8PZT-1 sent (`sent`) a frame
- Source is G8PZT-2 (different SSID)
- Base callsigns match: G8PZT == G8PZT
- Same sysop, different SSID - legitimate
- Link can be reliably inferred

---

## Scenario 4: Intermediate Node Forwarding (IMPERSONATION DETECTED)

```
???????????                                   ???????????
? M0LTE   ?  ????> (L2 connection) ????????> ? G8PZT   ?
???????????         through G8PZT             ???????????
                                                   ?
                                                   ? G8PZT forwards traffic
                                                   ? but transmits using
                                                   ? M0LTE's callsign!
                                                   ?
                                                   ? G8PZT Reports:
                                                   ? reportFrom: "G8PZT"
                                                   ? dirn: "sent"
                                                   ? source: "M0LTE"    ??
                                                   ? dest: "M0ABC"
                                                   ? isRF: true
                                                   ?
                                                   ? RF transmission
                                                   ?
                                              ???????????
                                              ? M0ABC   ?
                                              ???????????

? CANNOT INFER DIRECT LINK between M0LTE <-> M0ABC
Base callsigns differ: M0LTE != G8PZT
Source M0LTE is being IMPERSONATED by G8PZT

ACTUAL TOPOLOGY:
  M0LTE <?(some link)?> G8PZT <?(RF)?> M0ABC
```

**Analysis**:
- G8PZT sent (`sent`) a frame
- Source is M0LTE (different from reporter)
- Base callsigns differ: M0LTE != G8PZT
- **IMPERSONATION DETECTED**: G8PZT is forwarding for M0LTE
- The AX.25 frame source is M0LTE, but the actual transmitter is G8PZT
- We cannot infer a direct RF link between M0LTE and M0ABC
- The actual RF link is G8PZT <-> M0ABC

---

## Scenario 5: Overheard Forwarded Traffic (Limitation)

```
???????????                                   ???????????
? M0LTE   ?  ????> (L2 connection) ????????> ? G8PZT   ?
???????????         through G8PZT             ???????????
                                                   ?
                                                   ? RF transmission
                                                   ? (M0LTE impersonated)
                                                   ?
                                                   ?
                                              ???????????
                                              ? M0ABC   ?
                                              ???????????
                                                   ?
                                                   ? M0XYZ overhears
                                                   ?
???????????                                        ?
? M0XYZ   ?  <???????? (overhears RF) ?????????????
???????????
     ?
     ? M0XYZ Reports:
     ? reportFrom: "M0XYZ"
     ? dirn: "rcvd"
     ? source: "M0LTE"      ?? Actually from G8PZT!
     ? dest: "M0ABC"
     ? isRF: true
     ?

?? LIMITATION: M0XYZ cannot detect impersonation
dirn: "rcvd" means M0XYZ received the frame
Heuristic allows link inference (conservative)
Result: Incorrectly infers M0LTE <-> M0ABC RF link
```

**Analysis**:
- M0XYZ received (`rcvd`) a frame
- Source appears to be M0LTE
- M0XYZ has no way to know M0LTE was impersonated by G8PZT
- Our heuristic cannot detect this scenario
- **LIMITATION**: Passive observation of forwarded frames can create false links

---

## Summary Decision Tree

```
L2Trace received
      ?
      ?? dirn is null or empty?
      ?      ?? YES ? ? Allow link inference (conservative)
      ?
      ?? dirn == "rcvd"?
      ?      ?? YES ? ? Allow link inference (source is genuine)
      ?
      ?? dirn == "sent"?
             ?
             ?? base(source) == base(reportFrom)?
             ?      ?? YES ? ? Allow (reporter transmitting as itself)
             ?
             ?? base(source) != base(reportFrom)?
                    ?? YES ? ? DENY (impersonation detected)
                              Reporter is forwarding for someone else
                              Do not update link properties
```

## Code Implementation

```csharp
private bool CanInferLinkFromTrace(L2Trace trace)
{
    // If direction is not specified or is "rcvd", we can infer
    if (string.IsNullOrEmpty(trace.Direction) || 
        trace.Direction.Equals("rcvd", StringComparison.OrdinalIgnoreCase))
    {
        return true;  // ?
    }

    // If direction is "sent", check if source matches reporter
    if (trace.Direction.Equals("sent", StringComparison.OrdinalIgnoreCase))
    {
        var sourceBase = GetBaseCallsign(trace.Source);
        var reporterBase = GetBaseCallsign(trace.ReportFrom);

        // If base callsigns match, reporter transmitting as itself
        if (sourceBase?.Equals(reporterBase, StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;  // ?
        }

        // Base callsigns don't match - impersonation detected
        return false;  // ?
    }

    return true;  // ? Conservative default
}
```

## Testing Scenarios

| Scenario | reportFrom | dirn | source | dest | Result | Reason |
|----------|------------|------|---------|------|--------|---------|
| 1 | G8PZT | rcvd | M0LTE | G8PZT | ? Allow | Received frame |
| 2 | G8PZT-1 | sent | G8PZT-1 | M0LTE | ? Allow | Same call |
| 3 | G8PZT-1 | sent | G8PZT-2 | M0LTE | ? Allow | Same base |
| 4 | G8PZT | sent | M0LTE | M0ABC | ? Deny | Different base (impersonation) |
| 5 | G8PZT | null | M0LTE | M0ABC | ? Allow | No direction (conservative) |
| 6 | M0XYZ | rcvd | M0LTE | M0ABC | ? Allow | Received (limitation) |

## Real-World Impact

### Before Fix
```
Observed L2Traces ? All inferred as direct links
Result: Network map shows many false "RF" links
Example: M0LTE ?(RF)? M0ABC (actually via G8PZT)
```

### After Fix
```
Observed L2Traces ? Heuristic applied
Result: Only genuine direct links shown
Example: M0LTE ?(?)? G8PZT ?(RF)? M0ABC
         (M0LTE?M0ABC link exists but isRF remains uncertain)
```

## Monitoring

Watch for these log messages indicating the heuristic is working:

```
DEBUG: Not inferring link from L2Trace: 
       reporter=G8PZT is forwarding for source=M0LTE 
       (direction=sent, base callsigns differ)

TRACE: Skipping link RF update: 
       source M0LTE appears to be impersonated by G8PZT
```
