using FluentValidation;
using node_api.Models;

namespace node_api.Validators;

public class LinkDisconnectionEventValidator : AbstractValidator<LinkDisconnectionEvent>
{
    private static readonly string[] ValidDirections = ["incoming", "outgoing"];

    public LinkDisconnectionEventValidator()
    {
        RuleFor(x => x.DatagramType)
            .Equal("LinkDownEvent")
            .WithMessage("DatagramType must be 'LinkDownEvent'");

        RuleFor(x => x.TimeUnixSeconds)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TimeUnixSeconds.HasValue)
            .WithMessage("TimeUnixSeconds cannot be negative")
            .LessThanOrEqualTo(DateTimeOffset.MaxValue.ToUnixTimeSeconds())
            .When(x => x.TimeUnixSeconds.HasValue)
            .WithMessage("TimeUnixSeconds exceeds maximum valid Unix timestamp");

        RuleFor(x => x.Node)
            .NotEmpty()
            .WithMessage("Node callsign is required");

        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Id must be greater than 0");

        RuleFor(x => x.Direction)
            .Must(d => ValidDirections.Contains(d.ToLower()))
            .WithMessage($"Direction must be one of: {string.Join(", ", ValidDirections)}");

        RuleFor(x => x.Port)
            .NotEmpty()
            .WithMessage("Port is required");

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

        RuleFor(x => x.FramesQueuedPeak)
            .GreaterThanOrEqualTo(0)
            .When(x => x.FramesQueuedPeak.HasValue)
            .WithMessage("FramesQueuedPeak cannot be negative");

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
