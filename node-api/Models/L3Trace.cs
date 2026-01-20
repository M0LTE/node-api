using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// Layer 3 Trace - NET/ROM level packet trace
/// Each message represents a NET/ROM layer packet transmitted or received
/// </summary>
public record L3Trace : NetworkEventDatagram
{
    /// <summary>
    /// Serial number of this trace message (Required)
    /// </summary>
    [JsonPropertyName("serial")]
    public required int Serial { get; init; }

    /// <summary>
    /// Unix timestamp in seconds (Required)
    /// </summary>
    [JsonPropertyName("time")]
    public required decimal TimeUnixSeconds { get; init; }

    /// <summary>
    /// Direction: "sent" or "rcvd" (Required)
    /// </summary>
    [JsonPropertyName("dirn")]
    public required string Direction { get; init; }

    /// <summary>
    /// Indicates whether the packet was transmitted/received over RF (Required)
    /// </summary>
    [JsonPropertyName("isRF")]
    public required bool IsRF { get; init; }

    /// <summary>
    /// Reporter's callsign (Required)
    /// </summary>
    [JsonPropertyName("reportFrom")]
    public required string ReportFrom { get; init; }

    /// <summary>
    /// Port ID (Required)
    /// </summary>
    [JsonPropertyName("port")]
    public required int Port { get; init; }

    /// <summary>
    /// Information field length (Required)
    /// </summary>
    [JsonPropertyName("ilen")]
    public required int IFieldLength { get; init; }

    /// <summary>
    /// Protocol ID in decimal (Required)
    /// </summary>
    [JsonPropertyName("pid")]
    public required int ProtocolId { get; init; }

    /// <summary>
    /// Protocol name, e.g. "NET/ROM" (Required)
    /// </summary>
    [JsonPropertyName("ptcl")]
    public required string ProtocolName { get; init; }

    /// <summary>
    /// Layer 3 frame type: "NetRom", "Routing info", "Routing poll", "Unknown" (Required)
    /// </summary>
    [JsonPropertyName("l3Type")]
    public required string L3Type { get; init; }

    /// <summary>
    /// Layer 3 source callsign (Optional)
    /// </summary>
    [JsonPropertyName("l3src")]
    public string? L3Source { get; init; }

    /// <summary>
    /// Layer 3 destination callsign (Optional)
    /// </summary>
    [JsonPropertyName("l3dst")]
    public string? L3Destination { get; init; }

    /// <summary>
    /// Time To Live (Optional)
    /// </summary>
    [JsonPropertyName("ttl")]
    public int? TimeToLive { get; init; }

    /// <summary>
    /// NetRom L4 Frame Type: "CONN REQ", "CONN REQX", "CONN ACK", "CONN NAK", "DISC REQ", "DISC ACK", "INFO", "INFO ACK", "RSET", "PROT EXT", "unknown" (Optional)
    /// </summary>
    [JsonPropertyName("l4Type")]
    public string? L4Type { get; init; }

    /// <summary>
    /// Destination circuit number (Optional)
    /// Present in L4 frame types "CONN ACK", "INFO", "INFO ACK", "DISC REQ" and "DISC ACK"
    /// </summary>
    [JsonPropertyName("toCct")]
    public int? ToCircuit { get; init; }

    /// <summary>
    /// Transmit sequence number (Optional)
    /// Only present in "INFO" frames
    /// </summary>
    [JsonPropertyName("txSeq")]
    public int? TransmitSequenceNumber { get; init; }

    /// <summary>
    /// Receive sequence number (Optional)
    /// Present in "INFO" and "INFO ACK" frames
    /// </summary>
    [JsonPropertyName("rxSeq")]
    public int? ReceiveSequenceNumber { get; init; }

    /// <summary>
    /// Payload length (Optional)
    /// Only present in INFO frames
    /// </summary>
    [JsonPropertyName("paylen")]
    public int? PayloadLength { get; init; }
}
