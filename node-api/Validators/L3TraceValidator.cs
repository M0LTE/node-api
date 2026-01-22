using FluentValidation;
using node_api.Models;

namespace node_api.Validators;

public class L3TraceValidator : AbstractValidator<L3Trace>
{
    private static readonly string[] ValidDirections = ["sent", "rcvd"];
    private static readonly string[] ValidProtocolNames = ["NET/ROM"];
    private static readonly string[] ValidL3Types = ["NetRom", "Routing info", "Routing poll", "Unknown"];
    private static readonly string[] ValidL4Types = ["CONN REQ", "CONN REQX", "CONN ACK", "CONN NAK", "DISC REQ", "DISC ACK", "INFO", "INFO ACK", "RSET", "PROT EXT", "unknown"];

    public L3TraceValidator()
    {
        // Always required fields
        RuleFor(x => x.DatagramType)
            .Equal("L3Trace")
            .WithMessage("DatagramType must be 'L3Trace'");

        RuleFor(x => x.Serial)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Serial number cannot be negative");

        RuleFor(x => x.TimeUnixSeconds)
            .GreaterThanOrEqualTo(0)
            .WithMessage("TimeUnixSeconds cannot be negative")
            .LessThanOrEqualTo(DateTimeOffset.MaxValue.ToUnixTimeSeconds())
            .WithMessage("TimeUnixSeconds exceeds maximum valid Unix timestamp");

        RuleFor(x => x.Direction)
            .Must(d => ValidDirections.Contains(d))
            .WithMessage($"Direction must be one of: {string.Join(", ", ValidDirections)}");

        RuleFor(x => x.ReportFrom)
            .NotEmpty()
            .WithMessage("ReportFrom is required")
            .MustBeValidCallsign();

        RuleFor(x => x.Port)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Port cannot be negative");

        RuleFor(x => x.IFieldLength)
            .GreaterThanOrEqualTo(0)
            .WithMessage("IFieldLength cannot be negative");

        RuleFor(x => x.ProtocolId)
            .GreaterThanOrEqualTo(0)
            .WithMessage("ProtocolId cannot be negative");

        RuleFor(x => x.ProtocolName)
            .Must(p => ValidProtocolNames.Contains(p))
            .WithMessage($"ProtocolName must be one of: {string.Join(", ", ValidProtocolNames)}");

        RuleFor(x => x.L3Type)
            .Must(t => ValidL3Types.Contains(t))
            .WithMessage($"L3Type must be one of: {string.Join(", ", ValidL3Types)}");

        // Now optional - validate when present
        RuleFor(x => x.L3Source!)
            .MustBeValidCallsign()
            .When(x => !string.IsNullOrEmpty(x.L3Source));

        RuleFor(x => x.L3Destination!)
            .MustBeValidCallsign()
            .When(x => !string.IsNullOrEmpty(x.L3Destination));

        RuleFor(x => x.TimeToLive)
            .GreaterThan(0)
            .When(x => x.TimeToLive.HasValue)
            .WithMessage("TimeToLive must be greater than 0");

        RuleFor(x => x.L4Type)
            .Must(t => ValidL4Types.Contains(t))
            .When(x => !string.IsNullOrEmpty(x.L4Type))
            .WithMessage($"L4Type must be one of: {string.Join(", ", ValidL4Types)}");

        // Conditional validations for specific L4 types
        When(x => x.L4Type is "CONN ACK" or "INFO" or "INFO ACK" or "DISC REQ" or "DISC ACK", () =>
        {
            RuleFor(x => x.ToCircuit)
                .NotNull()
                .WithMessage("ToCircuit is required for L4 connection frames");
        });

        When(x => x.L4Type == "INFO", () =>
        {
            RuleFor(x => x.TransmitSequenceNumber)
                .NotNull()
                .WithMessage("TransmitSequenceNumber is required for INFO frames");

            RuleFor(x => x.ReceiveSequenceNumber)
                .NotNull()
                .WithMessage("ReceiveSequenceNumber is required for INFO frames");

            RuleFor(x => x.PayloadLength)
                .GreaterThanOrEqualTo(0)
                .When(x => x.PayloadLength.HasValue)
                .WithMessage("PayloadLength cannot be negative");
        });

        When(x => x.L4Type == "INFO ACK", () =>
        {
            RuleFor(x => x.ReceiveSequenceNumber)
                .NotNull()
                .WithMessage("ReceiveSequenceNumber is required for INFO ACK frames");
        });

        // Optional fields validation
        RuleFor(x => x.ToCircuit)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ToCircuit.HasValue)
            .WithMessage("ToCircuit cannot be negative");

        RuleFor(x => x.TransmitSequenceNumber)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TransmitSequenceNumber.HasValue)
            .WithMessage("TransmitSequenceNumber cannot be negative");

        RuleFor(x => x.ReceiveSequenceNumber)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ReceiveSequenceNumber.HasValue)
            .WithMessage("ReceiveSequenceNumber cannot be negative");
    }
}
