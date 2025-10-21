using FluentValidation;
using node_api.Models;

namespace node_api.Validators;

public class NodeUpEventValidator : AbstractValidator<NodeUpEvent>
{
    public NodeUpEventValidator()
    {
        RuleFor(x => x.DatagramType)
            .Equal("NodeUpEvent")
            .WithMessage("DatagramType must be 'NodeUpEvent'");

        RuleFor(x => x.TimeUnixSeconds)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TimeUnixSeconds.HasValue)
            .WithMessage("TimeUnixSeconds cannot be negative")
            .LessThanOrEqualTo(DateTimeOffset.MaxValue.ToUnixTimeSeconds())
            .When(x => x.TimeUnixSeconds.HasValue)
            .WithMessage("TimeUnixSeconds exceeds maximum valid Unix timestamp");

        RuleFor(x => x.NodeCall)
            .NotEmpty()
            .WithMessage("NodeCall is required");

        RuleFor(x => x.NodeAlias)
            .NotEmpty()
            .WithMessage("NodeAlias is required");

        RuleFor(x => x.Locator)
            .NotEmpty()
            .WithMessage("Locator is required")
            .Matches(@"^[A-R]{2}\d{2}[A-Xa-x]{2}$")
            .WithMessage("Locator must be a valid Maidenhead locator (e.g., IO82VJ)");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90 degrees");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180 degrees");

        RuleFor(x => x.Software)
            .NotEmpty()
            .WithMessage("Software is required");

        RuleFor(x => x.Version)
            .NotEmpty()
            .WithMessage("Version is required");
    }
}
