# L2 Connection Analysis - Technical Notes

## Frame Statistics Calculation

The L2 Connection Analysis feature calculates frame statistics (including byte counts) **at runtime in C#** rather than using pre-indexed database columns.

### Why Runtime Calculation?

For modest dataset sizes (typical for amateur packet radio networks), calculating statistics from JSON at query time is:
- **Simpler**: No database schema changes needed
- **Cleaner**: Single source of truth (the JSON data)
- **Flexible**: Easy to add new statistics without migrations
- **Sufficient**: Performance is acceptable for expected data volumes

### Implementation

See `MySqlTraceRepository.GetFrameStatisticsBetweenEndpointsAsync()`:

1. Query fetches raw JSON for traces between two endpoints
2. C# code parses each JSON document
3. Statistics are aggregated in-memory:
   - Frame count per direction/type
   - Total bytes from `ilen` field (information length)
4. Results returned as `FrameStatistic` records

### Performance Considerations

**Typical Query**:
- Date range: 24-48 hours
- Callsign pair: 2 stations
- Expected traces: 100-10,000 frames
- Parse time: <100ms for 10K frames

**Optimization if needed**:
If performance becomes an issue with larger datasets:
- Add database index columns (e.g., `ilen_idx`)
- Use SQL aggregation instead of in-memory
- Implement caching for frequently-queried pairs

### Related Code

- **Repository**: `node-api/Services/MySqlTraceRepository.cs`
- **Service**: `node-api/Services/L2ConnectionAnalysisService.cs`
- **Controller**: `node-api/Controllers/L2ConnectionsController.cs`
- **Tests**: `Tests/L2ConnectionAnalysisServiceTests.cs`

---

**Design Decision**: Keep it simple for now. Optimize if usage patterns show the need.
