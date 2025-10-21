using FluentValidation;
using node_api.Models;

namespace node_api.Validators;

public class NodeDownEventValidator : AbstractValidator<NodeDownEvent>
{
    public NodeDownEventValidator()
    {
        RuleFor(x => x.DatagramType)
            .Equal("NodeDownEvent")
            .WithMessage("DatagramType must be 'NodeDownEvent'");

        RuleFor(x => x.TimeUnixSeconds)
            .GreaterThanOrEqualTo(0)
            .WithMessage("TimeUnixSeconds cannot be negative")
            .LessThanOrEqualTo(DateTimeOffset.MaxValue.ToUnixTimeSeconds())
            .WithMessage("TimeUnixSeconds exceeds maximum valid Unix timestamp");

        RuleFor(x => x.NodeCall)
            .NotEmpty()
            .WithMessage("NodeCall is required");

        RuleFor(x => x.NodeAlias)
            .NotEmpty()
            .WithMessage("NodeAlias is required");

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

        RuleFor(x => x.UptimeSecs)
            .GreaterThanOrEqualTo(0)
            .When(x => x.UptimeSecs.HasValue)
            .WithMessage("UptimeSecs cannot be negative");
    }
}
