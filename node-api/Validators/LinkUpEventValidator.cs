using FluentValidation;
using node_api.Models;

namespace node_api.Validators;

public class LinkUpEventValidator : AbstractValidator<LinkUpEvent>
{
    public LinkUpEventValidator()
    {
        RuleFor(x => x.DatagramType)
            .Equal("LinkUpEvent")
            .WithMessage("DatagramType must be 'LinkUpEvent'");

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
            .WithMessage("Link ID must be greater than 0");

        RuleFor(x => x.Direction)
            .Must(d => d == "incoming" || d == "outgoing")
            .WithMessage("Direction must be 'incoming' or 'outgoing'");

        RuleFor(x => x.Port)
            .NotEmpty()
            .WithMessage("Port identifier is required");

        RuleFor(x => x.Remote)
            .NotEmpty()
            .WithMessage("Remote callsign is required");

        RuleFor(x => x.Local)
            .NotEmpty()
            .WithMessage("Local callsign is required");
    }
}
