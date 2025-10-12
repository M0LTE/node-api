using FluentValidation;
using FluentValidation.Results;
using node_api.Models;

namespace node_api.Validators;

/// <summary>
/// Provides validation services for UDP node info JSON datagrams
/// </summary>
public class DatagramValidationService
{
    private readonly IValidator<L2Trace> _l2TraceValidator;
    private readonly IValidator<NodeUpEvent> _nodeUpEventValidator;
    private readonly IValidator<NodeDownEvent> _nodeDownEventValidator;
    private readonly IValidator<NodeStatusReportEvent> _nodeStatusReportEventValidator;
    private readonly IValidator<LinkUpEvent> _linkUpEventValidator;
    private readonly IValidator<LinkDisconnectionEvent> _linkDisconnectionEventValidator;
    private readonly IValidator<LinkStatus> _linkStatusValidator;
    private readonly IValidator<CircuitUpEvent> _circuitUpEventValidator;
    private readonly IValidator<CircuitDisconnectionEvent> _circuitDisconnectionEventValidator;
    private readonly IValidator<CircuitStatus> _circuitStatusValidator;

    public DatagramValidationService(
        IValidator<L2Trace> l2TraceValidator,
        IValidator<NodeUpEvent> nodeUpEventValidator,
        IValidator<NodeDownEvent> nodeDownEventValidator,
        IValidator<NodeStatusReportEvent> nodeStatusReportEventValidator,
        IValidator<LinkUpEvent> linkUpEventValidator,
        IValidator<LinkDisconnectionEvent> linkDisconnectionEventValidator,
        IValidator<LinkStatus> linkStatusValidator,
        IValidator<CircuitUpEvent> circuitUpEventValidator,
        IValidator<CircuitDisconnectionEvent> circuitDisconnectionEventValidator,
        IValidator<CircuitStatus> circuitStatusValidator)
    {
        _l2TraceValidator = l2TraceValidator;
        _nodeUpEventValidator = nodeUpEventValidator;
        _nodeDownEventValidator = nodeDownEventValidator;
        _nodeStatusReportEventValidator = nodeStatusReportEventValidator;
        _linkUpEventValidator = linkUpEventValidator;
        _linkDisconnectionEventValidator = linkDisconnectionEventValidator;
        _linkStatusValidator = linkStatusValidator;
        _circuitUpEventValidator = circuitUpEventValidator;
        _circuitDisconnectionEventValidator = circuitDisconnectionEventValidator;
        _circuitStatusValidator = circuitStatusValidator;
    }

    /// <summary>
    /// Validates a datagram and returns the validation result
    /// </summary>
    public ValidationResult Validate(UdpNodeInfoJsonDatagram datagram)
    {
        return datagram switch
        {
            L2Trace l2Trace => _l2TraceValidator.Validate(l2Trace),
            NodeUpEvent nodeUpEvent => _nodeUpEventValidator.Validate(nodeUpEvent),
            NodeDownEvent nodeDownEvent => _nodeDownEventValidator.Validate(nodeDownEvent),
            NodeStatusReportEvent nodeStatusReportEvent => _nodeStatusReportEventValidator.Validate(nodeStatusReportEvent),
            LinkUpEvent linkUpEvent => _linkUpEventValidator.Validate(linkUpEvent),
            LinkDisconnectionEvent linkDisconnectionEvent => _linkDisconnectionEventValidator.Validate(linkDisconnectionEvent),
            LinkStatus linkStatus => _linkStatusValidator.Validate(linkStatus),
            CircuitUpEvent circuitUpEvent => _circuitUpEventValidator.Validate(circuitUpEvent),
            CircuitDisconnectionEvent circuitDisconnectionEvent => _circuitDisconnectionEventValidator.Validate(circuitDisconnectionEvent),
            CircuitStatus circuitStatus => _circuitStatusValidator.Validate(circuitStatus),
            _ => new ValidationResult(new[] { new ValidationFailure("DatagramType", "Unknown datagram type") })
        };
    }

    /// <summary>
    /// Validates a datagram and returns whether it's valid
    /// </summary>
    public bool IsValid(UdpNodeInfoJsonDatagram datagram, out ValidationResult validationResult)
    {
        validationResult = Validate(datagram);
        return validationResult.IsValid;
    }
}
