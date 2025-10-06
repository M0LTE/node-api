using System.Text.Json.Serialization;

namespace node_api.Models;

public readonly record struct XrouterUdpJsonFrame
{
    [JsonPropertyName("port")]
    public required string Port { get; init; }
    
    [JsonPropertyName("srce")]
    public required string Source { get; init; }

    [JsonPropertyName("dest")]
    public required string Destination { get; init; }

    [JsonPropertyName("ctrl")]
    public required int Control { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("modulo")]
    public required int Modulo { get; init; }

    [JsonPropertyName("rseq")]
    public int? ReceiveSequence { get; init; }

    [JsonPropertyName("tseq")]
    public int? TransmitSequence { get; init; }

    [JsonPropertyName("cr")]
    public required string Cr { get; init; }

    [JsonPropertyName("ilen")]
    public int? ILen { get; init; }

    [JsonPropertyName("pid")]
    public int? Pid { get; init; }

    [JsonPropertyName("ptcl")]
    public string? Ptcl { get; init; }
}