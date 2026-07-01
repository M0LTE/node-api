namespace node_api.Models;

/// <summary>
/// Full metadata for one node port, pushed in by an external source (packetnodes) that derives it from
/// operator port config/comments (LinBPQ M0LTEMapInfo / PortFreq and free-text). node-api doesn't
/// receive port config itself, so this is how a link's band, frequency, mode etc. become known.
/// Held in memory and refreshed by the periodic push.
/// </summary>
public sealed record PortMetadata(
    string Node,
    string Port,
    string? LinkType,
    long? FreqHz,
    string? Band,
    string? FreqSource,
    string? Mode,
    string? Modulation,
    int? Baud,
    int? Bitrate,
    string? Usage,
    string? Comment);
