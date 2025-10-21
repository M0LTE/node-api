using FluentValidation;
using node_api.Models;

namespace node_api.Validators;

public class CircuitStatusValidator : AbstractValidator<CircuitStatus>
{
    private static readonly string[] ValidDirections = ["incoming", "outgoing"];

    public CircuitStatusValidator()
    {
        RuleFor(x => x.DatagramType)
            .Equal("CircuitStatus")
            .WithMessage("DatagramType must be 'CircuitStatus'");

        RuleFor(x => x.TimeUnixSeconds)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TimeUnixSeconds.HasValue)
            .WithMessage("TimeUnixSeconds cannot be negative")
            .LessThanOrEqualTo(DateTimeOffset.MaxValue.ToUnixTimeSeconds())
            .When(x => x.TimeUnixSeconds.HasValue)
            .WithMessage("TimeUnixSeconds exceeds maximum valid Unix timestamp");

        RuleFor(x => x.Node)
            .NotEmpty()
            .WithMessage("Node callsign is required")
            .MustBeValidCallsign();

        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Circuit ID must be greater than 0");

        RuleFor(x => x.Direction)
            .Must(d => ValidDirections.Contains(d.ToLower()))
            .WithMessage("Direction must be 'incoming' or 'outgoing'");

        RuleFor(x => x.Remote)
            .NotEmpty()
            .WithMessage("Remote address is required");

        RuleFor(x => x.Local)
            .NotEmpty()
            .WithMessage("Local address is required");

        RuleFor(x => x.SegmentsSent)
            .GreaterThanOrEqualTo(0)
            .WithMessage("SegmentsSent cannot be negative");

        RuleFor(x => x.SegmentsReceived)
            .GreaterThanOrEqualTo(0)
            .WithMessage("SegmentsReceived cannot be negative");

        RuleFor(x => x.SegmentsResent)
            .GreaterThanOrEqualTo(0)
            .WithMessage("SegmentsResent cannot be negative");

        RuleFor(x => x.SegmentsQueued)
            .GreaterThanOrEqualTo(0)
            .WithMessage("SegmentsQueued cannot be negative");

        RuleFor(x => x.Service)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Service.HasValue)
            .WithMessage("Service cannot be negative");

        RuleFor(x => x.BytesSent)
            .GreaterThanOrEqualTo(0)
            .When(x => x.BytesSent.HasValue)
            .WithMessage("BytesSent cannot be negative");

        RuleFor(x => x.BytesReceived)
            .GreaterThanOrEqualTo(0)
            .When(x => x.BytesReceived.HasValue)
            .WithMessage("BytesReceived cannot be negative");
    }
}
