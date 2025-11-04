namespace node_api.Services;

/// <summary>
/// Service for analyzing bidirectional L2 connections between two callsigns
/// </summary>
public interface IL2ConnectionAnalysisService
{
    Task<L2ConnectionAnalysis> AnalyzeConnectionAsync(
        string callsign1,
        string callsign2,
        DateTimeOffset from,
        DateTimeOffset to,
        string[]? reportFrom,
        bool includeMetrics,
        bool includeTraces,
        int tracesLimit,
        string? tracesCursor,
        CancellationToken ct);
}

public record L2ConnectionAnalysis(
    ConnectionInfo Connection,
    IReadOnlyList<ConnectionSession> Sessions,
    OverallMetrics? Metrics,
    PagedTraces? Traces
);

public record ConnectionInfo(
    string Callsign1,
    string Callsign2,
    TimeRange TimeRange
);

public record TimeRange(
    DateTimeOffset From,
    DateTimeOffset To
);

public record ConnectionSession(
    int SessionId,
    LinkUpEventInfo? LinkUp,
    LinkDownEventInfo? LinkDown,
    IReadOnlyList<LinkStatusInfo> StatusReports,
    SessionMetrics? Metrics
);

public record LinkUpEventInfo(
    DateTime Timestamp,
    string Node,
    string Port,
    string Direction,
    int LinkId
);

public record LinkDownEventInfo(
    DateTime Timestamp,
    int? UpForSecs,
    int? FramesSent,
    int? FramesReceived,
    int? FramesResent,
    int? BytesSent,
    int? BytesReceived,
    string? Reason
);

public record LinkStatusInfo(
    DateTime Timestamp,
    int? UpForSecs,
    int? FramesSent,
    int? FramesReceived,
    int? FramesResent,
    int? L2RttMs,
    int? BpsTxMean,
    int? BpsRxMean
);

public record SessionMetrics(
    int? DurationSecs,
    int? TotalFrames,
    decimal? RetransmissionRate,
    int? AvgRttMs,
    int? ThroughputBps
);

public record OverallMetrics(
    int TotalSessions,
    int? TotalDurationSecs,
    int TotalFrames,
    DirectionalMetrics Direction1To2,
    DirectionalMetrics Direction2To1
);

public record DirectionalMetrics(
    int Frames,
    int IFrames,
    int Bytes,
    IReadOnlyDictionary<string, int> FrameTypes
);

public record PagedTraces(
    TracesPageInfo Page,
    IReadOnlyList<TraceWithContext> Data
);

public record TracesPageInfo(
    int Limit,
    string? Next
);

public record TraceWithContext(
    long Id,
    DateTime Timestamp,
    int? SessionId,
    string Direction,
    string FrameType,
    IReadOnlyList<string> ReportedBy,
    System.Text.Json.JsonElement Report
);
