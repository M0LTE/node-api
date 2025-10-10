using System.Text.Json.Serialization;

namespace node_api.Models;

public record L2Trace : UdpNodeInfoJsonDatagram
{
    [JsonPropertyName("from")]
    public required string FromCallsign { get; init; }

    [JsonPropertyName("port")]
    public required string Port { get; init; }
    
    [JsonPropertyName("srce")]
    public required string Source { get; init; }

    [JsonPropertyName("dest")]
    public required string Destination { get; init; }

    [JsonPropertyName("ctrl")]
    public required int Control { get; init; }

    [JsonPropertyName("l2type")]
    public required string L2Type { get; init; }

    [JsonPropertyName("cr")]
    public required string CommandResponse { get; init; }

    [JsonPropertyName("modulo")]
    public required int? Modulo { get; init; }

    [JsonPropertyName("digis")]
    public Digipeater[]? Digipeaters { get; init; }

    [JsonPropertyName("rseq")]
    public int? ReceiveSequence { get; init; }

    [JsonPropertyName("tseq")]
    public int? TransmitSequence { get; init; }

    [JsonPropertyName("pf")]
    public string? PollFinal { get; init; }

    [JsonPropertyName("pid")]
    public int? ProtocolId { get; init; }

    [JsonPropertyName("ptcl")]
    public string? ProtocolName { get; init; }

    [JsonPropertyName("ilen")]
    public int? IFieldLength { get; init; }

    [JsonPropertyName("l3type")]
    public string? L3Type { get; init; }

    [JsonPropertyName("l3src")]
    public string? L3Source { get; init; }
    
    [JsonPropertyName("l3dst")]
    public string? L3Destination { get; init; }

    [JsonPropertyName("ttl")]
    public int? TimeToLive { get; init; }

    [JsonPropertyName("l4type")]
    public string? L4Type { get; init; }

    [JsonPropertyName("fromCct")]
    public int? FromCircuit { get; init; }

    [JsonPropertyName("toCct")]
    public int? ToCircuit { get; init; }

    [JsonPropertyName("txSeq")]
    public int? TransmitSequenceNumber { get; init; } // MAYBE DUPLICATE OF TSEQ?

    [JsonPropertyName("rxSeq")]
    public int? ReceiveSequenceNumber { get; init; } // MAYBE DUPLICATE OF RSEQ?

    [JsonPropertyName("payLen")]
    public int? PayloadLength { get; init; }

    [JsonPropertyName("srcUser")]
    public string? OriginatingUserCallsign { get; init; }

    [JsonPropertyName("srcNode")]
    public string? OriginatingNodeCallsign { get; init; }

    [JsonPropertyName("service")]
    public int? NetRomXServiceNumber { get; init; }

    [JsonPropertyName("window")]
    public int? ProposedWindow { get; init; }

    [JsonPropertyName("accWin")]
    public int? AcceptableWindow { get; init; }

    [JsonPropertyName("l4t1")]
    public int? Layer4T1Timer { get; init; }

    [JsonPropertyName("bpqSpy")]
    public int? BpqExtension { get; init; }

    [JsonPropertyName("chokeFlag")]
    public bool? ChokeFlag { get; init; }

    [JsonPropertyName("nakFlag")]
    public bool? NakFlag { get; init; }

    [JsonPropertyName("moreFlag")]
    public bool? MoreFlag { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("fromAlias")]
    public string? FromAlias { get; init; }

    [JsonPropertyName("nodes")]
    public Node[]? Nodes { get; init; }

    [JsonPropertyName("data")]
    public string? Data { get; init; }

    [JsonPropertyName("payload")]
    public string? Payload { get; init; }

    public record Node
    {
        [JsonPropertyName("call")]
        public required string Callsign { get; init; }

        [JsonPropertyName("alias")]
        public string? Alias { get; init; }

        [JsonPropertyName("via")]
        public string? Via { get; init; }

        [JsonPropertyName("qual")]
        public int? Quality { get; init; }

        [JsonPropertyName("hops")]
        public int? Hops { get; init; }

        [JsonPropertyName("tt")]
        public int? OneWayTripTimeIn10msIncrements { get; init; }

        [JsonPropertyName("ipAddr")]
        public string? IpAddress { get; init; }

        [JsonPropertyName("bitMask")]
        public int? BitMask { get; init; }

        [JsonPropertyName("tcpPort")]
        public int? TcpPort { get; init; }

        [JsonPropertyName("latitude")]
        public decimal? Latitude { get; init; }

        [JsonPropertyName("longitude")]
        public decimal? Longitude { get; init; }

        [JsonPropertyName("software")]
        public string? Software { get; init; }

        [JsonPropertyName("version")]
        public string? Version { get; init; }

        [JsonPropertyName("isNode")]
        public bool? IsNode { get; init; }

        [JsonPropertyName("isBBS")]
        public bool? IsBbs { get; init; }

        [JsonPropertyName("isPMS")]
        public bool? IsPms { get; init; }

        [JsonPropertyName("isXRCHAT")]
        public bool? IsXrchat { get; init; }

        [JsonPropertyName("isRTCHAT")]
        public bool? IsRtchat { get; init; }

        [JsonPropertyName("isRMS")]
        public bool? IsRms { get; init; }

        [JsonPropertyName("isDXCLUS")]
        public bool? IsDxcluster { get; init; }

        [JsonPropertyName("tzMins")]
        public int? TimeZoneMinutesOffsetFromGmt { get; init; }

        [JsonPropertyName("timestamp")]
        public DateTimeOffset? Timestamp { get; init; }
    }

    public record Digipeater
    {
        [JsonPropertyName("call")]
        public required string Callsign { get; init; }

        [JsonPropertyName("rptd")]
        public required bool Repeated { get; init; }
    }
}
