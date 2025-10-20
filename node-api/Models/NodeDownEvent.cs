using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// 3.2. Node Down Event
/// This report is sent when a node is in the process of going offline.
/// It is unlikely to be sent when a node crashes, unless the error handler is hooked.
/// However a crash may be inferred if a node fails to report within a reasonable interval,
/// or if a subsequent NodeUpEvent is received with the same callsign and alias.
/// </summary>
public record NodeDownEvent : UdpNodeInfoJsonDatagram
{
    /// <summary>
    /// Node Callsign (Required)
    /// </summary>
    [JsonPropertyName("nodeCall")]
    public required string NodeCall { get; init; }

    /// <summary>
    /// Node Alias (Required)
    /// </summary>
    [JsonPropertyName("nodeAlias")]
    public required string NodeAlias { get; init; }

    /// <summary>
    /// Reason for the shutdown, e.g. "reboot" (Optional)
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>
    /// Total number of incoming AX25 connections since node startup (Optional)
    /// </summary>
    [JsonPropertyName("linksIn")]
    public int? LinksIn { get; init; }

    /// <summary>
    /// Total number of outgoing AX25 connections since node startup (Optional)
    /// </summary>
    [JsonPropertyName("linksOut")]
    public int? LinksOut { get; init; }

    /// <summary>
    /// Total NetRom layer 4 connections in since node startup (Optional)
    /// </summary>
    [JsonPropertyName("cctsIn")]
    public int? CircuitsIn { get; init; }

    /// <summary>
    /// Total NetRom layer 4 connections out since node startup (Optional)
    /// </summary>
    [JsonPropertyName("cctsOut")]
    public int? CircuitsOut { get; init; }

    /// <summary>
    /// Total number of NetRom layer 3 frames that were relayed, i.e. not originating or destinating at the node since node startup (Optional)
    /// </summary>
    [JsonPropertyName("l3Relayed")]
    public int? L3Relayed { get; init; }
}