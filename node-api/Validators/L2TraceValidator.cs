using FluentValidation;
using node_api.Models;

namespace node_api.Validators;

public class L2TraceValidator : AbstractValidator<L2Trace>
{
    private static readonly string[] ValidL2Types = ["SABME", "C", "D", "DM", "UA", "UI", "I", "FRMR", "RR", "RNR", "REJ", "?", "XID", "TEST", "SREJ"];
    private static readonly string[] ValidCommandResponses = ["C", "R", "V1"];
    private static readonly string[] ValidPollFinal = ["P", "F"];
    private static readonly string[] ValidProtocolNames = ["SEG", "DATA", "NET/ROM", "IP", "ARP", "FLEXNET", "?"];
    private static readonly string[] ValidL3Types = ["NetRom", "Routing info", "Routing poll", "Unknown"];
    private static readonly string[] ValidL4Types = ["NRR Request", "NRR Reply", "CONN REQ", "CONN REQX", "CONN ACK", "CONN NAK", "DISC REQ", "DISC ACK", "INFO", "INFO ACK", "RSET", "PROT EXT", "unknown"];
    private static readonly string[] ValidRoutingTypes = ["NODES", "INP3"];
    private static readonly string[] ValidDirections = ["sent", "rcvd"];

    public L2TraceValidator()
    {
        // Always required fields
        RuleFor(x => x.DatagramType)
            .Equal("L2Trace")
            .WithMessage("DatagramType must be 'L2Trace'");

        RuleFor(x => x.ReportFrom)
            .NotEmpty()
            .WithMessage("Reporter's callsign is required")
            .MustBeValidCallsign();

        // TimeUnixSeconds validation - should be a valid Unix timestamp
        RuleFor(x => x.TimeUnixSeconds)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TimeUnixSeconds.HasValue)
            .WithMessage("TimeUnixSeconds cannot be negative")
            .LessThanOrEqualTo(DateTimeOffset.MaxValue.ToUnixTimeSeconds())
            .When(x => x.TimeUnixSeconds.HasValue)
            .WithMessage("TimeUnixSeconds exceeds maximum valid Unix timestamp");

        RuleFor(x => x.Port)
            .NotEmpty()
            .WithMessage("Port ID is required");

        // Direction validation (optional field)
        RuleFor(x => x.Direction)
            .Must(d => d == null || ValidDirections.Contains(d))
            .WithMessage($"Direction must be null or one of: {string.Join(", ", ValidDirections)}");

        RuleFor(x => x.Source)
            .NotEmpty()
            .WithMessage("Source callsign is required")
            .MustBeValidCallsign();

        RuleFor(x => x.Destination)
            .NotEmpty()
            .WithMessage("Destination callsign is required")
            .MustBeValidCallsign();

        RuleFor(x => x.Control)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Control field cannot be negative");

        RuleFor(x => x.L2Type)
            .Must(t => ValidL2Types.Contains(t))
            .WithMessage($"L2Type must be one of: {string.Join(", ", ValidL2Types)}");

        RuleFor(x => x.CommandResponse)
            .Must(cr => ValidCommandResponses.Contains(cr))
            .WithMessage($"CommandResponse must be one of: {string.Join(", ", ValidCommandResponses)}");

        RuleFor(x => x.Modulo)
            .Must(m => m == 8 || m == 128)
            .When(x => x.Modulo.HasValue)
            .WithMessage("Modulo must be 8 or 128");

        // Optional fields validation
        RuleFor(x => x.PollFinal)
            .Must(pf => pf == null || ValidPollFinal.Contains(pf))
            .WithMessage($"PollFinal must be null or one of: {string.Join(", ", ValidPollFinal)}");

        RuleFor(x => x.ProtocolName)
            .Must(p => p == null || ValidProtocolNames.Contains(p))
            .WithMessage($"ProtocolName must be null or one of: {string.Join(", ", ValidProtocolNames)}");

        RuleFor(x => x.IFieldLength)
            .GreaterThanOrEqualTo(0)
            .When(x => x.IFieldLength.HasValue)
            .WithMessage("IFieldLength cannot be negative");

        // ilen field only present in frame types "I" and "UI"
        RuleFor(x => x.IFieldLength)
            .Null()
            .When(x => x.L2Type != "I" && x.L2Type != "UI")
            .WithMessage("IFieldLength should only be present for 'I' and 'UI' frame types");

        // Digipeater validation
        RuleForEach(x => x.Digipeaters)
            .SetValidator(new DigipeaterValidator())
            .When(x => x.Digipeaters != null);

        // NET/ROM protocol conditional validation
        When(x => x.ProtocolName == "NET/ROM", () =>
        {
            RuleFor(x => x.L3Type)
                .NotEmpty()
                .WithMessage("L3Type is required when ProtocolName is 'NET/ROM'")
                .Must(t => ValidL3Types.Contains(t!))
                .WithMessage($"L3Type must be one of: {string.Join(", ", ValidL3Types)}");
        });

        // NetRom Layer 3 validation
        When(x => x.L3Type == "NetRom", () =>
        {
            RuleFor(x => x.L3Source)
                .NotEmpty()
                .WithMessage("L3Source is required when L3Type is 'NetRom'")
                .MustBeValidCallsign();

            RuleFor(x => x.L3Destination)
                .NotEmpty()
                .WithMessage("L3Destination is required when L3Type is 'NetRom'")
                .MustBeValidCallsign();

            RuleFor(x => x.TimeToLive)
                .NotNull()
                .WithMessage("TimeToLive is required when L3Type is 'NetRom'")
                .GreaterThan(0)
                .WithMessage("TimeToLive must be greater than 0");

            RuleFor(x => x.L4Type)
                .NotEmpty()
                .WithMessage("L4Type is required when L3Type is 'NetRom'")
                .Must(t => ValidL4Types.Contains(t!))
                .WithMessage($"L4Type must be one of: {string.Join(", ", ValidL4Types)}");

            // CONN REQ and CONN REQX validation
            When(x => x.L4Type == "CONN REQ" || x.L4Type == "CONN REQX", () =>
            {
                RuleFor(x => x.FromCircuit)
                    .NotNull()
                    .WithMessage("FromCircuit is required for CONN REQ/CONN REQX");

                RuleFor(x => x.OriginatingUserCallsign)
                    .NotEmpty()
                    .WithMessage("OriginatingUserCallsign is required for CONN REQ/CONN REQX")
                    .MustBeValidCallsign();

                RuleFor(x => x.OriginatingNodeCallsign)
                    .NotEmpty()
                    .WithMessage("OriginatingNodeCallsign is required for CONN REQ/CONN REQX")
                    .MustBeValidCallsign();

                RuleFor(x => x.ProposedWindow)
                    .NotNull()
                    .WithMessage("ProposedWindow is required for CONN REQ/CONN REQX");
            });

            // CONN REQX specific validation
            When(x => x.L4Type == "CONN REQX", () =>
            {
                RuleFor(x => x.NetRomXServiceNumber)
                    .NotNull()
                    .WithMessage("NetRomXServiceNumber is required for CONN REQX");
            });

            // CONN ACK, INFO, INFO ACK, DISC REQ, DISC ACK validation
            When(x => x.L4Type is "CONN ACK" or "INFO" or "INFO ACK" or "DISC REQ" or "DISC ACK", () =>
            {
                RuleFor(x => x.ToCircuit)
                    .NotNull()
                    .WithMessage("ToCircuit is required for L4 connection frames");
            });

            // INFO frame validation
            When(x => x.L4Type == "INFO", () =>
            {
                RuleFor(x => x.TransmitSequenceNumber)
                    .NotNull()
                    .WithMessage("TransmitSequenceNumber is required for INFO frames");

                RuleFor(x => x.ReceiveSequenceNumber)
                    .NotNull()
                    .WithMessage("ReceiveSequenceNumber is required for INFO frames");
            });

            // INFO ACK frame validation
            When(x => x.L4Type == "INFO ACK", () =>
            {
                RuleFor(x => x.ReceiveSequenceNumber)
                    .NotNull()
                    .WithMessage("ReceiveSequenceNumber is required for INFO ACK frames");
            });

            // CONN ACK validation
            When(x => x.L4Type == "CONN ACK", () =>
            {
                RuleFor(x => x.AcceptableWindow)
                    .NotNull()
                    .WithMessage("AcceptableWindow is required for CONN ACK");
            });

            // NRR Request and Reply validation
            When(x => x.L4Type is "NRR Request" or "NRR Reply", () =>
            {
                RuleFor(x => x.NrrId)
                    .NotNull()
                    .WithMessage("NrrId is required for NRR Request/Reply");

                RuleFor(x => x.NrrRoute)
                    .NotEmpty()
                    .WithMessage("NrrRoute is required for NRR Request/Reply");
            });
        });

        // Routing info validation
        When(x => x.L3Type == "Routing info", () =>
        {
            RuleFor(x => x.Type)
                .NotEmpty()
                .WithMessage("Type is required when L3Type is 'Routing info'")
                .Must(t => ValidRoutingTypes.Contains(t!))
                .WithMessage($"Type must be one of: {string.Join(", ", ValidRoutingTypes)}");

            // NODES routing validation (NetRom routing broadcast)
            When(x => x.Type == "NODES", () =>
            {
                // FromAlias is marked as required in spec v0.8a section 2.2.1
                // but real-world implementations don't include it, so making it optional
                
                RuleForEach(x => x.Nodes)
                    .SetValidator(new NetRomNodeValidator())
                    .When(x => x.Nodes != null);
            });

            // INP3 routing validation
            When(x => x.Type == "INP3", () =>
            {
                RuleForEach(x => x.Nodes)
                    .SetValidator(new Inp3NodeValidator())
                    .When(x => x.Nodes != null);
            });
        });
    }
}

public class DigipeaterValidator : AbstractValidator<L2Trace.Digipeater>
{
    public DigipeaterValidator()
    {
        RuleFor(x => x.Callsign)
            .NotEmpty()
            .WithMessage("Digipeater callsign is required")
            .MustBeValidCallsign();
    }
}

public class NetRomNodeValidator : AbstractValidator<L2Trace.Node>
{
    public NetRomNodeValidator()
    {
        RuleFor(x => x.Callsign)
            .NotEmpty()
            .WithMessage("Node callsign is required")
            .MustBeValidCallsign();

        RuleFor(x => x.Alias)
            .NotNull()
            .WithMessage("Alias is required for NETROM routing");

        RuleFor(x => x.Via)
            .NotEmpty()
            .WithMessage("Via is required for NETROM routing")
            .MustBeValidCallsign();

        RuleFor(x => x.Quality)
            .NotNull()
            .WithMessage("Quality is required for NETROM routing")
            .InclusiveBetween(0, 255)
            .WithMessage("Quality must be between 0 and 255");
    }
}

public class Inp3NodeValidator : AbstractValidator<L2Trace.Node>
{
    public Inp3NodeValidator()
    {
        RuleFor(x => x.Callsign)
            .NotEmpty()
            .WithMessage("Node callsign is required")
            .MustBeValidCallsign();

        RuleFor(x => x.Hops)
            .NotNull()
            .WithMessage("Hops is required for INP3 routing")
            .GreaterThanOrEqualTo(0)
            .WithMessage("Hops cannot be negative");

        RuleFor(x => x.OneWayTripTimeIn10msIncrements)
            .NotNull()
            .WithMessage("OneWayTripTime is required for INP3 routing")
            .GreaterThanOrEqualTo(0)
            .WithMessage("OneWayTripTime cannot be negative");

        // Optional fields validation
        RuleFor(x => x.BitMask)
            .InclusiveBetween(0, 32)
            .When(x => x.BitMask.HasValue)
            .WithMessage("BitMask must be between 0 and 32");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180");

        // Add validation for Timestamp field
        RuleFor(x => x.Timestamp)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Timestamp.HasValue)
            .WithMessage("Timestamp cannot be negative")
            .LessThanOrEqualTo(DateTimeOffset.MaxValue.ToUnixTimeSeconds())
            .When(x => x.Timestamp.HasValue)
            .WithMessage("Timestamp exceeds maximum valid Unix timestamp");
    }
}
