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

        RuleFor(x => x.NodeCall)
            .NotEmpty()
            .WithMessage("NodeCall is required");

        RuleFor(x => x.NodeAlias)
            .NotEmpty()
            .WithMessage("NodeAlias is required");
    }
}
