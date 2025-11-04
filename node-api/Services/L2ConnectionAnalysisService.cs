using System.Text.Json;

namespace node_api.Services;

public class L2ConnectionAnalysisService(
    IEventRepository eventRepository,
    ITraceRepository traceRepository,
    ILogger<L2ConnectionAnalysisService> logger) : IL2ConnectionAnalysisService
{
    public async Task<L2ConnectionAnalysis> AnalyzeConnectionAsync(
        string callsign1,
        string callsign2,
        DateTimeOffset from,
        DateTimeOffset to,
        string[]? reportFrom,
        bool includeMetrics,
        bool includeTraces,
        int tracesLimit,
        string? tracesCursor,
        CancellationToken ct)
    {
        // Normalize callsigns to canonical order (alphabetically)
        var (endpoint1, endpoint2) = string.Compare(callsign1, callsign2, StringComparison.OrdinalIgnoreCase) < 0
            ? (callsign1, callsign2)
            : (callsign2, callsign1);

        var connection = new ConnectionInfo(
            endpoint1,
            endpoint2,
            new TimeRange(from, to)
        );

        // Get link events and build sessions
        var eventData = await eventRepository.GetLinkEventsBetweenEndpointsAsync(
            endpoint1, endpoint2, from, to, ct);
        var sessions = BuildSessionsFromEvents(eventData);

        // Get overall metrics if requested
        OverallMetrics? metrics = null;
        if (includeMetrics)
        {
            metrics = await BuildOverallMetricsAsync(endpoint1, endpoint2, from, to, ct);
        }

        // Get traces if requested
        PagedTraces? traces = null;
        if (includeTraces)
        {
            traces = await BuildPagedTracesAsync(
                endpoint1, endpoint2, from, to, reportFrom, tracesLimit, tracesCursor, sessions, ct);
        }

        return new L2ConnectionAnalysis(connection, sessions, metrics, traces);
    }

    private List<ConnectionSession> BuildSessionsFromEvents(IReadOnlyList<Controllers.EventsController.EventDto> events)
    {
        var sessions = new List<ConnectionSession>();
        SessionBuilder? currentSession = null;
        var sessionIdCounter = 1;

        foreach (var eventDto in events)
        {
            var root = eventDto.Event;
            
            if (!root.TryGetProperty("@type", out var typeElement))
                continue;

            var type = typeElement.GetString();
            var timestamp = eventDto.Timestamp;

            switch (type)
            {
                case "LinkUpEvent":
                    // Start new session
                    currentSession = new SessionBuilder
                    {
                        SessionId = sessionIdCounter++,
                        LinkUp = ParseLinkUpEvent(root, timestamp)
                    };
                    break;

                case "LinkStatus":
                    if (currentSession != null)
                    {
                        currentSession.StatusReports.Add(ParseLinkStatusEvent(root, timestamp));
                    }
                    break;

                case "LinkDownEvent":
                    if (currentSession != null)
                    {
                        currentSession.LinkDown = ParseLinkDownEvent(root, timestamp);
                        
                        // Calculate session metrics
                        currentSession.Metrics = CalculateSessionMetrics(currentSession);
                        
                        // Complete session
                        sessions.Add(currentSession.Build());
                        currentSession = null;
                    }
                    break;
            }
        }

        // If there's an unclosed session, add it anyway
        if (currentSession != null)
        {
            currentSession.Metrics = CalculateSessionMetrics(currentSession);
            sessions.Add(currentSession.Build());
        }

        return sessions;
    }

    private LinkUpEventInfo ParseLinkUpEvent(JsonElement root, DateTime timestamp)
    {
        return new LinkUpEventInfo(
            timestamp,
            root.GetProperty("node").GetString() ?? "",
            root.GetProperty("port").GetString() ?? "",
            root.GetProperty("direction").GetString() ?? "",
            root.GetProperty("id").GetInt32()
        );
    }

    private LinkStatusInfo ParseLinkStatusEvent(JsonElement root, DateTime timestamp)
    {
        return new LinkStatusInfo(
            timestamp,
            root.TryGetProperty("upForSecs", out var upForSecs) ? upForSecs.GetInt32() : null,
            root.TryGetProperty("frmsSent", out var frmsSent) ? frmsSent.GetInt32() : null,
            root.TryGetProperty("frmsRcvd", out var frmsRcvd) ? frmsRcvd.GetInt32() : null,
            root.TryGetProperty("frmsResent", out var frmsResent) ? frmsResent.GetInt32() : null,
            root.TryGetProperty("l2rttMs", out var l2rttMs) ? l2rttMs.GetInt32() : null,
            root.TryGetProperty("bpsTxMean", out var bpsTxMean) ? bpsTxMean.GetInt32() : null,
            root.TryGetProperty("bpsRxMean", out var bpsRxMean) ? bpsRxMean.GetInt32() : null
        );
    }

    private LinkDownEventInfo ParseLinkDownEvent(JsonElement root, DateTime timestamp)
    {
        return new LinkDownEventInfo(
            timestamp,
            root.TryGetProperty("upForSecs", out var upForSecs) ? upForSecs.GetInt32() : null,
            root.TryGetProperty("frmsSent", out var frmsSent) ? frmsSent.GetInt32() : null,
            root.TryGetProperty("frmsRcvd", out var frmsRcvd) ? frmsRcvd.GetInt32() : null,
            root.TryGetProperty("frmsResent", out var frmsResent) ? frmsResent.GetInt32() : null,
            root.TryGetProperty("bytesSent", out var bytesSent) ? bytesSent.GetInt32() : null,
            root.TryGetProperty("bytesRcvd", out var bytesRcvd) ? bytesRcvd.GetInt32() : null,
            root.TryGetProperty("reason", out var reason) ? reason.GetString() : null
        );
    }

    private SessionMetrics? CalculateSessionMetrics(SessionBuilder session)
    {
        if (session.LinkDown == null)
            return null;

        var totalFrames = (session.LinkDown.FramesSent ?? 0) + (session.LinkDown.FramesReceived ?? 0);
        decimal? retransmissionRate = totalFrames > 0
            ? (decimal)(session.LinkDown.FramesResent ?? 0) / totalFrames
            : null;

        var avgRttMs = session.StatusReports.Count > 0
            ? (int?)session.StatusReports.Where(s => s.L2RttMs.HasValue).Select(s => s.L2RttMs!.Value).DefaultIfEmpty(0).Average()
            : null;

        var throughputBps = session.LinkDown.UpForSecs > 0 && session.LinkDown.BytesSent.HasValue && session.LinkDown.BytesReceived.HasValue
            ? (int?)(((session.LinkDown.BytesSent.Value + session.LinkDown.BytesReceived.Value) * 8) / session.LinkDown.UpForSecs.Value)
            : null;

        return new SessionMetrics(
            session.LinkDown.UpForSecs,
            totalFrames > 0 ? totalFrames : null,
            retransmissionRate,
            avgRttMs,
            throughputBps
        );
    }

    private async Task<OverallMetrics> BuildOverallMetricsAsync(
        string endpoint1,
        string endpoint2,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        // Get frame statistics from trace repository
        var stats = await traceRepository.GetFrameStatisticsBetweenEndpointsAsync(
            endpoint1, endpoint2, from, to, ct);

        // Build directional metrics
        var direction1To2Stats = stats.Where(s => 
            s.Source.Equals(endpoint1, StringComparison.OrdinalIgnoreCase) &&
            s.Dest.Equals(endpoint2, StringComparison.OrdinalIgnoreCase)).ToList();

        var direction2To1Stats = stats.Where(s =>
            s.Source.Equals(endpoint2, StringComparison.OrdinalIgnoreCase) &&
            s.Dest.Equals(endpoint1, StringComparison.OrdinalIgnoreCase)).ToList();

        var dir1To2 = BuildDirectionalMetrics(direction1To2Stats);
        var dir2To1 = BuildDirectionalMetrics(direction2To1Stats);

        // Get session count from events
        var eventData = await eventRepository.GetEventsAsync(
            node: null,
            type: "LinkDownEvent",
            direction: null,
            remote: null,
            local: null,
            port: null,
            from: from,
            to: to,
            limit: 10000, // Large limit to get all sessions
            cursor: null,
            includeTotalCount: false,
            sortOrder: "ASC",
            ct: ct);

        // Count sessions and calculate total duration
        var sessionCount = 0;
        var totalDuration = 0;

        foreach (var evt in eventData.Data)
        {
            var root = evt.Event;
            
            // Check if this is a link between our two endpoints
            if (root.TryGetProperty("local", out var local) && 
                root.TryGetProperty("remote", out var remote))
            {
                var localVal = local.GetString() ?? "";
                var remoteVal = remote.GetString() ?? "";
                
                if ((localVal.Equals(endpoint1, StringComparison.OrdinalIgnoreCase) && 
                     remoteVal.Equals(endpoint2, StringComparison.OrdinalIgnoreCase)) ||
                    (localVal.Equals(endpoint2, StringComparison.OrdinalIgnoreCase) && 
                     remoteVal.Equals(endpoint1, StringComparison.OrdinalIgnoreCase)))
                {
                    sessionCount++;
                    if (root.TryGetProperty("upForSecs", out var upForSecs))
                    {
                        totalDuration += upForSecs.GetInt32();
                    }
                }
            }
        }

        return new OverallMetrics(
            sessionCount,
            totalDuration > 0 ? totalDuration : null,
            dir1To2.Frames + dir2To1.Frames,
            dir1To2,
            dir2To1
        );
    }

    private DirectionalMetrics BuildDirectionalMetrics(List<FrameStatistic> stats)
    {
        var frameTypes = new Dictionary<string, int>();
        var totalFrames = 0;
        var iFrames = 0;
        var totalBytes = 0;

        foreach (var stat in stats)
        {
            var count = (int)stat.Count;
            totalFrames += count;
            totalBytes += (int)stat.TotalBytes;

            if (stat.FrameType?.Equals("I", StringComparison.OrdinalIgnoreCase) == true)
            {
                iFrames += count;
            }

            if (!string.IsNullOrEmpty(stat.FrameType))
            {
                if (frameTypes.ContainsKey(stat.FrameType))
                    frameTypes[stat.FrameType] += count;
                else
                    frameTypes[stat.FrameType] = count;
            }
        }

        return new DirectionalMetrics(
            totalFrames,
            iFrames,
            totalBytes,
            frameTypes
        );
    }

    private async Task<PagedTraces> BuildPagedTracesAsync(
        string endpoint1,
        string endpoint2,
        DateTimeOffset from,
        DateTimeOffset to,
        string[]? reportFrom,
        int limit,
        string? cursor,
        IReadOnlyList<ConnectionSession> sessions,
        CancellationToken ct)
    {
        var (traces, nextCursor) = await traceRepository.GetBidirectionalTracesAsync(
            endpoint1, endpoint2, from, to, reportFrom, limit, cursor, ct);

        var data = new List<TraceWithContext>();
        foreach (var trace in traces)
        {
            var root = trace.Report;
            
            // Determine direction
            string? source = null;
            string? dest = null;
            string? frameType = null;
            string? reportedBy = null;

            if (root.TryGetProperty("srce", out var srceElement))
                source = srceElement.GetString();
            if (root.TryGetProperty("dest", out var destElement))
                dest = destElement.GetString();
            if (root.TryGetProperty("l2Type", out var l2TypeElement))
                frameType = l2TypeElement.GetString();
            if (root.TryGetProperty("reportFrom", out var reportFromElement))
                reportedBy = reportFromElement.GetString();

            var direction = source != null && source.Equals(endpoint1, StringComparison.OrdinalIgnoreCase)
                ? $"{endpoint1} ? {endpoint2}"
                : $"{endpoint2} ? {endpoint1}";

            // Find which session this trace belongs to
            var sessionId = FindSessionForTrace(trace.Timestamp, sessions);

            data.Add(new TraceWithContext(
                trace.Id,
                trace.Timestamp,
                sessionId,
                direction,
                frameType ?? "?",
                reportedBy != null ? new[] { reportedBy } : Array.Empty<string>(),
                trace.Report
            ));
        }

        return new PagedTraces(
            new TracesPageInfo(limit, nextCursor),
            data
        );
    }

    private int? FindSessionForTrace(DateTime timestamp, IReadOnlyList<ConnectionSession> sessions)
    {
        foreach (var session in sessions)
        {
            if (session.LinkUp == null)
                continue;

            var sessionStart = session.LinkUp.Timestamp;
            var sessionEnd = session.LinkDown?.Timestamp ?? DateTime.MaxValue;

            if (timestamp >= sessionStart && timestamp <= sessionEnd)
            {
                return session.SessionId;
            }
        }

        return null;
    }

    private class SessionBuilder
    {
        public int SessionId { get; init; }
        public LinkUpEventInfo? LinkUp { get; set; }
        public LinkDownEventInfo? LinkDown { get; set; }
        public List<LinkStatusInfo> StatusReports { get; } = new();
        public SessionMetrics? Metrics { get; set; }

        public ConnectionSession Build()
        {
            return new ConnectionSession(
                SessionId,
                LinkUp,
                LinkDown,
                StatusReports,
                Metrics
            );
        }
    }
}
