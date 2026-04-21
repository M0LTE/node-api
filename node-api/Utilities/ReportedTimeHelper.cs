using System.Text.Json;
using node_api.Models;

namespace node_api.Utilities;

internal static class ReportedTimeHelper
{
    public static DateTime? GetReportedTime(NetworkEventDatagram datagram)
    {
        return datagram switch
        {
            L2Trace trace => FromUnixSeconds(trace.TimeUnixSeconds),
            L3Trace trace => FromUnixSeconds(trace.TimeUnixSeconds),
            NodeUpEvent nodeUp => FromUnixSeconds(nodeUp.TimeUnixSeconds),
            NodeStatusReportEvent nodeStatus => FromUnixSeconds(nodeStatus.TimeUnixSeconds),
            NodeDownEvent nodeDown => FromUnixSeconds(nodeDown.TimeUnixSeconds),
            LinkUpEvent linkUp => FromUnixSeconds(linkUp.TimeUnixSeconds),
            LinkStatus linkStatus => FromUnixSeconds(linkStatus.TimeUnixSeconds),
            LinkDisconnectionEvent linkDown => FromUnixSeconds(linkDown.TimeUnixSeconds),
            CircuitUpEvent circuitUp => FromUnixSeconds(circuitUp.TimeUnixSeconds),
            CircuitStatus circuitStatus => FromUnixSeconds(circuitStatus.TimeUnixSeconds),
            CircuitDisconnectionEvent circuitDown => FromUnixSeconds(circuitDown.TimeUnixSeconds),
            _ => null
        };
    }

    public static DateTime? ExtractFromJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("time", out var timeElement) &&
                timeElement.ValueKind == JsonValueKind.Number &&
                timeElement.TryGetDecimal(out var unixTime))
            {
                return FromUnixSeconds(unixTime);
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    public static DateTime? FromUnixSeconds(decimal? unixSeconds)
    {
        if (!unixSeconds.HasValue)
        {
            return null;
        }

        var wholeSeconds = decimal.ToInt64(decimal.Truncate(unixSeconds.Value));
        var fractionalPart = unixSeconds.Value - wholeSeconds;
        var milliseconds = decimal.ToInt32(decimal.Truncate(fractionalPart * 1000m));
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(wholeSeconds).UtcDateTime;
        return dateTime.AddMilliseconds(milliseconds);
    }
}
