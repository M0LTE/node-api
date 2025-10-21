using FluentValidation;
using node_api.Models;

namespace node_api.Validators;

public class LinkDisconnectionEventValidator : AbstractValidator<LinkDisconnectionEvent>
{
    public LinkDisconnectionEventValidator()
    {
        RuleFor(x => x.DatagramType)
            .Equal("LinkDownEvent")
            .WithMessage("DatagramType must be 'LinkDownEvent'");

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

        RuleFor(x => x.UpForSecs)
            .GreaterThanOrEqualTo(0)
            .WithMessage("UpForSecs cannot be negative");

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

        // Optional fields validation
        When(x => x.FramesQueuedPeak.HasValue, () =>
        {
            RuleFor(x => x.FramesQueuedPeak!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("FramesQueuedPeak cannot be negative");
        });

        When(x => x.BytesSent.HasValue, () =>
        {
            RuleFor(x => x.BytesSent!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("BytesSent cannot be negative");
        });

        When(x => x.BytesReceived.HasValue, () =>
        {
            RuleFor(x => x.BytesReceived!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("BytesReceived cannot be negative");
        });
    }
}
