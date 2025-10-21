using FluentValidation;
using node_api.Models;

namespace node_api.Validators;

public class CircuitDisconnectionEventValidator : AbstractValidator<CircuitDisconnectionEvent>
{
    public CircuitDisconnectionEventValidator()
    {
        RuleFor(x => x.DatagramType)
            .Equal("CircuitDownEvent")
            .WithMessage("DatagramType must be 'CircuitDownEvent'");

        RuleFor(x => x.TimeUnixSeconds)
            .GreaterThanOrEqualTo(0)
            .WithMessage("TimeUnixSeconds cannot be negative")
            .LessThanOrEqualTo(DateTimeOffset.MaxValue.ToUnixTimeSeconds())
            .WithMessage("TimeUnixSeconds exceeds maximum valid Unix timestamp");

        RuleFor(x => x.Node)
            .NotEmpty()
            .WithMessage("Node callsign is required");

        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Circuit ID must be greater than 0");

        RuleFor(x => x.Direction)
            .Must(d => d == "incoming" || d == "outgoing")
            .WithMessage("Direction must be 'incoming' or 'outgoing'");

        RuleFor(x => x.Remote)
            .NotEmpty()
            .WithMessage("Remote address is required");

        RuleFor(x => x.Local)
            .NotEmpty()
            .WithMessage("Local address is required");

        RuleFor(x => x.SegsSent)
            .GreaterThanOrEqualTo(0)
            .WithMessage("SegsSent cannot be negative");

        RuleFor(x => x.SegsRcvd)
            .GreaterThanOrEqualTo(0)
            .WithMessage("SegsRcvd cannot be negative");

        RuleFor(x => x.SegsResent)
            .GreaterThanOrEqualTo(0)
            .WithMessage("SegsResent cannot be negative");

        RuleFor(x => x.SegsQueued)
            .GreaterThanOrEqualTo(0)
            .WithMessage("SegsQueued cannot be negative");

        RuleFor(x => x.BytesReceived)
            .GreaterThanOrEqualTo(0)
            .When(x => x.BytesReceived.HasValue)
            .WithMessage("BytesReceived cannot be negative");

        RuleFor(x => x.BytesSent)
            .GreaterThanOrEqualTo(0)
            .When(x => x.BytesSent.HasValue)
            .WithMessage("BytesSent cannot be negative");
    }
}
