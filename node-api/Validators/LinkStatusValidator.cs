using FluentValidation;
using node_api.Models;

namespace node_api.Validators;

public class LinkStatusValidator : AbstractValidator<LinkStatus>
{
    public LinkStatusValidator()
    {
        RuleFor(x => x.DatagramType)
            .Equal("LinkStatus")
            .WithMessage("DatagramType must be 'LinkStatus'");

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

        RuleFor(x => x.FramesSent)
            .GreaterThanOrEqualTo(0)
            .WithMessage("FramesSent cannot be negative");

        RuleFor(x => x.FramesReceived)
            .GreaterThanOrEqualTo(0)
            .WithMessage("FramesReceived cannot be negative");

        RuleFor(x => x.FramesResent)
            .GreaterThanOrEqualTo(0)
            .WithMessage("FramesResent cannot be negative");

        RuleFor(x => x.FramesQueued)
            .GreaterThanOrEqualTo(0)
            .WithMessage("FramesQueued cannot be negative");
    }
}
