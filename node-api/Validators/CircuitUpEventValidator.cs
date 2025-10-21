using FluentValidation;
using node_api.Models;

namespace node_api.Validators;

public class CircuitUpEventValidator : AbstractValidator<CircuitUpEvent>
{
    public CircuitUpEventValidator()
    {
        RuleFor(x => x.DatagramType)
            .Equal("CircuitUpEvent")
            .WithMessage("DatagramType must be 'CircuitUpEvent'");

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
            .Must(d => d == "incoming" || d == "outgoing")
            .WithMessage("Direction must be 'incoming' or 'outgoing'");

        RuleFor(x => x.Remote)
            .NotEmpty()
            .WithMessage("Remote address is required");

        RuleFor(x => x.Local)
            .NotEmpty()
            .WithMessage("Local address is required");
    }
}
