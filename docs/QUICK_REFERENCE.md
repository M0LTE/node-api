# Quick Reference: AX.25 Link Inference Heuristic

## The Problem in One Sentence
**Intermediate AX.25 nodes transmit using the user's callsign, not their own, causing false link inference.**

## The Solution in One Rule
```
IF (direction == "sent") AND (base_callsign(source) ? base_callsign(reportFrom))
THEN
    ? Source is impersonated ? DO NOT infer link
ELSE
    ? Can reliably infer link
```

## Quick Examples

### ? VALID - Can Infer Link
| Scenario | reportFrom | dirn | source | Reason |
|----------|------------|------|---------|---------|
| Received frame | G8PZT | `rcvd` | M0LTE | Source is genuine |
| Same callsign | G8PZT-1 | `sent` | G8PZT-1 | Reporter = Source |
| Same base, different SSID | G8PZT-1 | `sent` | G8PZT-2 | Base G8PZT = G8PZT |
| Missing direction | G8PZT | `null` | M0LTE | Conservative default |

### ? INVALID - Cannot Infer (Impersonation)
| Scenario | reportFrom | dirn | source | Reason |
|----------|------------|------|---------|---------|
| Forwarded traffic | G8PZT | `sent` | M0LTE | Base G8PZT ? M0LTE |
| With SSIDs | G8PZT-1 | `sent` | M0LTE-5 | Base G8PZT ? M0LTE |

## Code Snippet

```csharp
// In NetworkStateUpdater.cs
public void UpdateFromL2Trace(L2Trace trace)
{
    // ... track nodes ...
    
    // Only update link if we can reliably infer it
    if (trace.IsRF.HasValue && CanInferLinkFromTrace(trace))
    {
        var link = _networkState.GetLink(canonicalKey);
        if (link != null)
        {
            link.IsRF = trace.IsRF;  // Update link property
        }
    }
}

private bool CanInferLinkFromTrace(L2Trace trace)
{
    if (dirn is null or "rcvd") return true;
    
    if (dirn == "sent")
    {
        return base(source) == base(reportFrom);
    }
    
    return true;  // Conservative
}
```

## What This Affects

| Component | Impact |
|-----------|--------|
| Node tracking | ? **No change** - All nodes still tracked |
| Link creation | ? **No change** - Only LinkUpEvent creates links |
| Link properties | ?? **Changed** - `IsRF` only updated when reliable |
| Network map | ?? **Improved** - Shows accurate topology |

## Testing

```bash
# Run the specific tests
dotnet test --filter "FullyQualifiedName~NetworkStateUpdaterL2TraceTests"

# Result: 15 tests, all should pass
```

## Common Scenarios

### Scenario 1: User M0LTE connects to BBS M0ABC through node G8PZT

```
BEFORE FIX:
  L2Trace: G8PZT sent from M0LTE to M0ABC
  Result: Incorrectly inferred M0LTE ?(RF)? M0ABC

AFTER FIX:
  L2Trace: G8PZT sent from M0LTE to M0ABC
  Detection: base(M0LTE) ? base(G8PZT) ? Impersonation!
  Result: Link M0LTE ?? M0ABC exists, but IsRF not updated
```

### Scenario 2: Normal traffic between nodes

```
L2Trace: G8PZT-1 received from M0LTE to G8PZT-2
Result: ? Can infer M0LTE ?(RF)? G8PZT-2
```

## Debugging

### Log Messages

```
? Normal operation:
DEBUG: Updated link RF status from L2Trace: M0LTE<->G8PZT is RF

? Impersonation detected:
DEBUG: Not inferring link from L2Trace: reporter=G8PZT 
       is forwarding for source=M0LTE 
       (direction=sent, base callsigns differ)
TRACE: Skipping link RF update: source M0LTE 
       appears to be impersonated by G8PZT
```

### Troubleshooting

| Issue | Check | Fix |
|-------|-------|-----|
| Too many false links | Review logs for impersonation detections | Working as designed |
| Missing expected links | Verify LinkUpEvent received | L2Trace doesn't create links |
| Incorrect RF status | Check direction field in L2Trace | May need more data |

## Files to Review

| Purpose | File |
|---------|------|
| Implementation | `node-api/Services/NetworkStateUpdater.cs` |
| Tests | `Tests/NetworkStateUpdaterL2TraceTests.cs` |
| Documentation | `docs/AX25_ROUTING_AND_LINK_INFERENCE.md` |
| Visual guide | `docs/AX25_ROUTING_SCENARIOS.md` |
| This card | `docs/QUICK_REFERENCE.md` |

## Key Takeaways

1. ? **Nodes are always tracked** - No change in node discovery
2. ?? **Link properties conditionally updated** - Only when reliable
3. ? **L2Trace never creates links** - Only LinkUpEvent does
4. ?? **Heuristic is conservative** - When in doubt, allow inference
5. ?? **Improves accuracy** - Network map shows real topology
6. ?? **Well tested** - 15 comprehensive tests

## When to Use This Knowledge

- Investigating link inference issues
- Understanding network topology visualization
- Debugging false positive links
- Reviewing L2Trace processing logic
- Explaining RF link detection to users

## Quick Decision Guide

```
You see L2Trace data:
  ?? Question 1: What's the direction?
  ?    ?? "rcvd" ? ? Trust it
  ?    ?? "sent" ? Go to Question 2
  ?    ?? null/unknown ? ? Trust it (conservative)
  ?
  ?? Question 2: Do the base callsigns match?
       ?? YES ? ? Trust it (reporter transmitting as itself)
       ?? NO ? ? Don't trust it (impersonation!)
```

---

**Last Updated**: 2025-01-21  
**Version**: 1.0  
**Author**: GitHub Copilot
