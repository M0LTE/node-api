# L2 Connection Analysis UI

## Overview
A web-based interface for analyzing bidirectional L2 (link layer) communication between two packet radio callsigns.

## Location
`/l2-connection.html` or accessible from the main dashboard at `/`

## Features

### ?? Query Form
- **Callsign Selection**: Enter two callsigns (with optional SSIDs) to analyze
- **Date Range**: Select start and end times (UTC) for analysis
- **Filtering**: Optional filtering by reporting station
- **Options**:
  - Include/exclude metrics (session statistics)
  - Include/exclude traces (frame-level data)
  - Configurable trace limit (1-500 per page)

### ?? Analysis Results

#### Connection Overview
- Visual header showing the two callsigns being analyzed
- Time range of the analysis

#### Overall Metrics
- Total number of sessions
- Total connection duration
- Frame counts (bidirectional)
- I-Frame counts (information frames)
- **Frame Type Breakdown** (expandable):
  - Per-direction statistics for each frame type (I, RR, RNR, etc.)

#### Session Details
For each L2 session, displays:

**Link Up Information:**
- Timestamp
- Node that initiated the connection
- Port number
- Connection direction (incoming/outgoing)

**Link Down Information:**
- Timestamp
- Session duration
- Frames sent/received/resent
- Disconnect reason (if available)

**Session Metrics:**
- Retransmission rate
- Average RTT (Round Trip Time)
- Throughput in bps
- Total frames

**Status Reports** (expandable):
- Periodic link status during the session
- Timestamps, frame counts, RTT measurements

#### Frame Traces
If enabled, displays:
- Individual frames exchanged
- Direction indicators (?/?)
- Frame types (badged for visibility)
- Session association
- Reporting station(s)
- **Pagination** for large result sets

## API Integration

The UI calls the REST API endpoint:
```
GET /api/history/connections/l2
```

### Query Parameters
- `callsign1` (required): First callsign
- `callsign2` (required): Second callsign  
- `from` (required): ISO 8601 timestamp
- `to` (required): ISO 8601 timestamp
- `reportFrom` (optional): Filter by reporting station
- `includeMetrics` (default: true): Include aggregated statistics
- `includeTraces` (default: true): Include frame-level data
- `tracesLimit` (default: 100, max: 500): Traces per page
- `tracesCursor` (optional): Pagination cursor for next page

### Response Structure
```json
{
  "connection": {
    "callsign1": "CALL1-1",
    "callsign2": "CALL2-5",
    "timeRange": {
      "from": "2025-01-01T00:00:00Z",
      "to": "2025-01-02T00:00:00Z"
    }
  },
  "sessions": [
    {
      "sessionId": 1,
      "linkUp": { ... },
      "linkDown": { ... },
      "statusReports": [ ... ],
      "metrics": { ... }
    }
  ],
  "metrics": {
    "totalSessions": 5,
    "totalDurationSecs": 7200,
    "totalFrames": 1500,
    "direction1To2": { ... },
    "direction2To1": { ... }
  },
  "traces": {
    "page": {
      "limit": 100,
      "next": "base64cursor"
    },
    "data": [ ... ]
  }
}
```

## Design

The UI follows the existing design system:
- **Color Scheme**: Blue (#0066cc) primary, purple gradients for highlights
- **Typography**: Segoe UI font family
- **Components**: Consistent cards, badges, tables from other pages
- **Responsive**: Grid-based layouts that adapt to screen size
- **Interactive**: Expandable sections for detailed information

## Usage Example

1. Navigate to `/l2-connection.html`
2. Enter two callsigns (e.g., `CALL1-1` and `CALL2-5`)
3. Select a date range (default: last 24 hours)
4. Optionally filter by reporting station
5. Choose whether to include metrics and/or traces
6. Click "Analyze Connection"
7. Results appear with expandable sections for details
8. Use pagination controls to browse traces if needed

## Performance Considerations

- **Metrics Only**: Fast queries, suitable for overview analysis
- **With Traces**: Slower but provides frame-level detail
- **Pagination**: Large trace sets are paginated to maintain performance
- **Limit Control**: Users can reduce trace limit for faster results

## Future Enhancements

Potential improvements:
- Export results to CSV/JSON
- Real-time updates for active sessions
- Visual timeline of session activity
- Link to individual node pages
- Download frame data for offline analysis
- Comparison between multiple time ranges
