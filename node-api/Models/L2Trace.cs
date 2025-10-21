using System.Text.Json.Serialization;

namespace node_api.Models;

/// <summary>
/// AX25 Layer 2 Trace - Section 1
/// Each message or "report" is sent by XRouter upon transmitting or receiving an AX25 frame of any type.
/// Reports have a variable format, depending on the AX25 frame contents.
/// </summary>
public record L2Trace : UdpNodeInfoJsonDatagram
{
    /// <summary>
    /// Reporter's callsign, e.g. nodecall (Required)
    /// </summary>
    [JsonPropertyName("reportFrom")]
    public required string ReportFrom { get; init; }

    [JsonPropertyName("time")]
    public long? TimeUnixSeconds { get; init; }

    /// <summary>
    /// Port ID, e.g. "3" or "4mlink" (Required)
    /// </summary>
    [JsonPropertyName("port")]
    public required string Port { get; init; }
    
    /// <summary>
    /// Source callsign, e.g. "G8PZT-1" (Required)
    /// </summary>
    [JsonPropertyName("srce")]
    public required string Source { get; init; }

    /// <summary>
    /// Destination callsign, e.g "M0AHN" (Required)
    /// </summary>
    [JsonPropertyName("dest")]
    public required string Destination { get; init; }

    /// <summary>
    /// AX25 control field numeric value (Required)
    /// Applications may wish to decode the control field themselves instead of using the decoded "l2type" field.
    /// </summary>
    [JsonPropertyName("ctrl")]
    public required int Control { get; init; }

    /// <summary>
    /// Frame type mnemonic, e.g. "I", "RR", "UI", "SABME", "C", "D", "DM", "UA", "FRMR", "RNR", "REJ", "?" (Required)
    /// </summary>
    [JsonPropertyName("l2Type")]
    public required string L2Type { get; init; }

    /// <summary>
    /// Command/Response bit: "C" (Command), "R" (Response), or "V1" (AX25 version 1) (Required)
    /// </summary>
    [JsonPropertyName("cr")]
    public required string CommandResponse { get; init; }

    /// <summary>
    /// AX.25 modulo (8 or 128)
    /// </summary>
    [JsonPropertyName("modulo")]
    public int? Modulo { get; init; }

    /// <summary>
    /// List of digipeater calls (Optional)
    /// Only present if there are digipeaters in the destination path.
    /// </summary>
    [JsonPropertyName("digis")]
    public Digipeater[]? Digipeaters { get; init; }

    /// <summary>
    /// Receive sequence number (i.e. expected) (Optional)
    /// </summary>
    [JsonPropertyName("rseq")]
    public int? ReceiveSequence { get; init; }

    /// <summary>
    /// Transmit (send) sequence number (Optional)
    /// </summary>
    [JsonPropertyName("tseq")]
    public int? TransmitSequence { get; init; }

    /// <summary>
    /// Poll/Final bit, either "P" or "F" (Optional)
    /// </summary>
    [JsonPropertyName("pf")]
    public string? PollFinal { get; init; }

    /// <summary>
    /// Next layer protocol number in decimal (Optional)
    /// The pid value includes the two high order bits which are usually set to one,
    /// but are sometimes used for layer 2 fragmentation.
    /// </summary>
    [JsonPropertyName("pid")]
    public int? ProtocolId { get; init; }

    /// <summary>
    /// Next layer protocol in words: "SEG", "DATA", "NET/ROM", "IP", "ARP", "FLEXNET", "?" (Optional)
    /// </summary>
    [JsonPropertyName("ptcl")]
    public string? ProtocolName { get; init; }

    /// <summary>
    /// Length of information field (Optional)
    /// Only present in frame types "I" and "UI".
    /// </summary>
    [JsonPropertyName("ilen")]
    public int? IFieldLength { get; init; }

    /// <summary>
    /// Layer 3 frame type: "NetRom", "Routing info", "Routing poll", "Unknown" (Optional)
    /// Only present if the "ptcl" value is "NET/ROM".
    /// </summary>
    [JsonPropertyName("l3type")]
    public string? L3Type { get; init; }

    /// <summary>
    /// Layer 3 source callsign (Optional - NetRom)
    /// </summary>
    [JsonPropertyName("l3src")]
    public string? L3Source { get; init; }
    
    /// <summary>
    /// Layer 3 destination callsign (Optional - NetRom)
    /// </summary>
    [JsonPropertyName("l3dst")]
    public string? L3Destination { get; init; }

    /// <summary>
    /// Layer 3 Time To Live (Optional - NetRom)
    /// </summary>
    [JsonPropertyName("ttl")]
    public int? TimeToLive { get; init; }

    /// <summary>
    /// NetRom L4 Frame Type: "CONN REQ", "CONN REQX", "CONN ACK", "CONN NAK", "DISC REQ", "DISC ACK", "INFO", "INFO ACK", "RSET", "PROT EXT", "unknown" (Optional - NetRom)
    /// </summary>
    [JsonPropertyName("l4type")]
    public string? L4Type { get; init; }

    /// <summary>
    /// Source circuit number (Optional - NetRom)
    /// Only present in L4 frame types "CONN REQ" and "CONN REQX".
    /// </summary>
    [JsonPropertyName("fromCct")]
    public int? FromCircuit { get; init; }

    /// <summary>
    /// Destination circuit number (Optional - NetRom)
    /// Only present in L4 frame types "CONN ACK", "INFO", "INFO ACK", "DISC REQ" and "DISC ACK".
    /// </summary>
    [JsonPropertyName("toCct")]
    public int? ToCircuit { get; init; }

    /// <summary>
    /// Transmit sequence number (Optional - NetRom)
    /// Only present in "INFO" frames.
    /// </summary>
    [JsonPropertyName("txSeq")]
    public int? TransmitSequenceNumber { get; init; }

    /// <summary>
    /// Receive sequence number (Optional - NetRom)
    /// Only present in "INFO" and "INFO ACK" frames.
    /// </summary>
    [JsonPropertyName("rxSeq")]
    public int? ReceiveSequenceNumber { get; init; }

    /// <summary>
    /// Payload length (Optional - NetRom)
    /// Only present in INFO frames.
    /// </summary>
    [JsonPropertyName("payLen")]
    public int? PayloadLength { get; init; }

    /// <summary>
    /// Callsign of originating user (Optional - NetRom)
    /// Only present in L4 frame types "CONN REQ" and "CONN REQX".
    /// </summary>
    [JsonPropertyName("srcUser")]
    public string? OriginatingUserCallsign { get; init; }

    /// <summary>
    /// Callsign of originating user's node (Optional - NetRom)
    /// Only present in L4 frame types "CONN REQ" and "CONN REQX".
    /// </summary>
    [JsonPropertyName("srcNode")]
    public string? OriginatingNodeCallsign { get; init; }

    /// <summary>
    /// NetRomX service number (Optional - NetRom)
    /// Only present in L4 frame type "CONN REQX".
    /// </summary>
    [JsonPropertyName("service")]
    public int? NetRomXServiceNumber { get; init; }

    /// <summary>
    /// Proposed window (Optional - NetRom)
    /// Only present in L4 frame types "CONN REQ" and "CONN REQX".
    /// </summary>
    [JsonPropertyName("window")]
    public int? ProposedWindow { get; init; }

    /// <summary>
    /// Acceptable window (Optional - NetRom)
    /// Only present in "CONN ACK" frame type.
    /// </summary>
    [JsonPropertyName("accWin")]
    public int? AcceptableWindow { get; init; }

    /// <summary>
    /// Layer 4 T1 timer in seconds (Optional - NetRom)
    /// Only present in L4 frame types "CONN REQ" and "CONN REQX".
    /// </summary>
    [JsonPropertyName("l4t1")]
    public int? Layer4T1Timer { get; init; }

    /// <summary>
    /// BPQ extension (Optional - NetRom)
    /// Only present in L4 frame types "CONN REQ" and "CONN REQX".
    /// </summary>
    [JsonPropertyName("bpqSpy")]
    public int? BpqExtension { get; init; }

    /// <summary>
    /// True if CHOKE flag is set (Optional - NetRom)
    /// </summary>
    [JsonPropertyName("chokeFlag")]
    public bool? ChokeFlag { get; init; }

    /// <summary>
    /// True if NAK flag is set (Optional - NetRom)
    /// </summary>
    [JsonPropertyName("nakFlag")]
    public bool? NakFlag { get; init; }

    /// <summary>
    /// True if MORE flag is set (Optional - NetRom)
    /// </summary>
    [JsonPropertyName("moreFlag")]
    public bool? MoreFlag { get; init; }

    /// <summary>
    /// ID for the NRR request
    /// </summary>
    [JsonPropertyName("nrrId")]
    public int? NrrId { get; init; }

    /// <summary>
    /// List of calls traversed by NRR
    /// </summary>
    [JsonPropertyName("nrrRoute")]
    public string? NrrRoute { get; init; }

    /// <summary>
    /// Routing info type: "NETROM" or "INP3" (Optional - Routing)
    /// Only present if l3type is "Routing info".
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>
    /// Sender's alias (Optional - NetRom Routing)
    /// Only present for NETROM routing info frames.
    /// </summary>
    [JsonPropertyName("fromAlias")]
    public string? FromAlias { get; init; }

    /// <summary>
    /// One or more destination nodes (Optional - Routing)
    /// Only present if l3type is "Routing info".
    /// </summary>
    [JsonPropertyName("nodes")]
    public Node[]? Nodes { get; init; }

    /// <summary>
    /// Frame data content (Optional)
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; init; }

    /// <summary>
    /// Frame payload content (Optional)
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; init; }

    /// <summary>
    /// Represents a node in routing information broadcasts (NetRom or INP3)
    /// </summary>
    public record Node
    {
        /// <summary>
        /// Destination node callsign (Required)
        /// </summary>
        [JsonPropertyName("call")]
        public required string Callsign { get; init; }

        /// <summary>
        /// Destination node's alias (Required for NETROM, Optional for INP3)
        /// </summary>
        [JsonPropertyName("alias")]
        public string? Alias { get; init; }

        /// <summary>
        /// Callsign of next hop (Required for NetRom routing)
        /// </summary>
        [JsonPropertyName("via")]
        public string? Via { get; init; }

        /// <summary>
        /// Route "quality" (0-255) (Required for NetRom routing)
        /// </summary>
        [JsonPropertyName("qual")]
        public int? Quality { get; init; }

        /// <summary>
        /// Number of hops to the destination (Required for INP3 routing)
        /// </summary>
        [JsonPropertyName("hops")]
        public int? Hops { get; init; }

        /// <summary>
        /// One way trip time in 10ms increments (Required for INP3 routing)
        /// </summary>
        [JsonPropertyName("tt")]
        public int? OneWayTripTimeIn10msIncrements { get; init; }

        /// <summary>
        /// Amprnet IP address of the node (Optional - INP3 routing)
        /// </summary>
        [JsonPropertyName("ipAddr")]
        public string? IpAddress { get; init; }

        /// <summary>
        /// Netmask expressed as number 0-32 (Optional - INP3 routing)
        /// </summary>
        [JsonPropertyName("bitMask")]
        public int? BitMask { get; init; }

        /// <summary>
        /// TCP port for Amprnet Telnet (Optional - INP3 routing)
        /// </summary>
        [JsonPropertyName("tcpPort")]
        public int? TcpPort { get; init; }

        /// <summary>
        /// In decimal degrees, e.g. 49.407 (Optional - INP3 routing)
        /// </summary>
        [JsonPropertyName("latitude")]
        public decimal? Latitude { get; init; }

        /// <summary>
        /// In decimal degrees, e.g. -87.5730 (Optional - INP3 routing)
        /// </summary>
        [JsonPropertyName("longitude")]
        public decimal? Longitude { get; init; }

        /// <summary>
        /// Destination node software e.g. "XRLin" (Optional - INP3 routing)
        /// </summary>
        [JsonPropertyName("software")]
        public string? Software { get; init; }

        /// <summary>
        /// Software version (Optional - INP3 routing)
        /// </summary>
        [JsonPropertyName("version")]
        public string? Version { get; init; }

        /// <summary>
        /// True if "call" has normal command line (Optional - INP3 routing)
        /// "isNode" means that "call" applies to a conventional node, not a direct connect to an application.
        /// </summary>
        [JsonPropertyName("isNode")]
        public bool? IsNode { get; init; }

        /// <summary>
        /// True if "call" is that of a full BBS (Optional - INP3 routing)
        /// "isBBS" means that the callsign connects *directly* to a BBS.
        /// </summary>
        [JsonPropertyName("isBBS")]
        public bool? IsBbs { get; init; }

        /// <summary>
        /// True if "call" is that of a PMS (Optional - INP3 routing)
        /// </summary>
        [JsonPropertyName("isPMS")]
        public bool? IsPms { get; init; }

        /// <summary>
        /// True if "call" is an XRChat server (Optional - INP3 routing)
        /// </summary>
        [JsonPropertyName("isXRCHAT")]
        public bool? IsXrchat { get; init; }

        /// <summary>
        /// True if "call" is a RoundTable chat (Optional - INP3 routing)
        /// </summary>
        [JsonPropertyName("isRTCHAT")]
        public bool? IsRtchat { get; init; }

        /// <summary>
        /// True if "call" is Radio Mail Server (Optional - INP3 routing)
        /// </summary>
        [JsonPropertyName("isRMS")]
        public bool? IsRms { get; init; }

        /// <summary>
        /// True if "call" is a DX Cluster (Optional - INP3 routing)
        /// </summary>
        [JsonPropertyName("isDXCLUS")]
        public bool? IsDxcluster { get; init; }

        /// <summary>
        /// Timezone offset from GMT in minutes (Optional - INP3 routing)
        /// </summary>
        [JsonPropertyName("tzMins")]
        public int? TimeZoneMinutesOffsetFromGmt { get; init; }

        /// <summary>
        /// Timestamp indicating when the data was last updated by the destination node (Optional - INP3 routing)
        /// Unix timestamp in seconds
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long? Timestamp { get; init; }
    }

    /// <summary>
    /// Represents a digipeater in the destination path
    /// </summary>
    public record Digipeater
    {
        /// <summary>
        /// Digipeater's callsign+SSID (Required)
        /// </summary>
        [JsonPropertyName("call")]
        public required string Callsign { get; init; }

        /// <summary>
        /// True if digi repeated the packet (Required)
        /// </summary>
        [JsonPropertyName("rptd")]
        public bool? Repeated { get; init; }
    }
}
