using FluentValidation;
using node_api.Models;

namespace node_api.Validators;

public class NodeStatusReportEventValidator : AbstractValidator<NodeStatusReportEvent>
{
    public NodeStatusReportEventValidator()
    {
        RuleFor(x => x.DatagramType)
            .Equal("NodeStatus")
            .WithMessage("DatagramType must be 'NodeStatus'");

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

        RuleFor(x => x.UptimeSecs)
            .GreaterThanOrEqualTo(0)
            .WithMessage("UptimeSecs cannot be negative");

        RuleFor(x => x.LinksIn)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LinksIn.HasValue)
            .WithMessage("LinksIn cannot be negative");

        RuleFor(x => x.LinksOut)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LinksOut.HasValue)
            .WithMessage("LinksOut cannot be negative");

        RuleFor(x => x.CircuitsIn)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CircuitsIn.HasValue)
            .WithMessage("CircuitsIn cannot be negative");

        RuleFor(x => x.CircuitsOut)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CircuitsOut.HasValue)
            .WithMessage("CircuitsOut cannot be negative");

        RuleFor(x => x.L3Relayed)
            .GreaterThanOrEqualTo(0)
            .When(x => x.L3Relayed.HasValue)
            .WithMessage("L3Relayed cannot be negative");
    }
}
