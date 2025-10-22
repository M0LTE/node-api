namespace node_api.Constants;

/// <summary>
/// Constants for datagram type strings
/// </summary>
public static class DatagramTypes
{
    public const string L2Trace = "L2Trace";
    public const string NodeUpEvent = "NodeUpEvent";
    public const string NodeDownEvent = "NodeDownEvent";
    public const string NodeStatus = "NodeStatus";
    public const string LinkUpEvent = "LinkUpEvent";
    public const string LinkDownEvent = "LinkDownEvent";
    public const string LinkStatus = "LinkStatus";
    public const string CircuitUpEvent = "CircuitUpEvent";
    public const string CircuitDownEvent = "CircuitDownEvent";
    public const string CircuitStatus = "CircuitStatus";

    /// <summary>
    /// All supported datagram types
    /// </summary>
    public static readonly string[] All =
    [
        L2Trace,
        NodeUpEvent,
        NodeDownEvent,
        NodeStatus,
        LinkUpEvent,
        LinkDownEvent,
        LinkStatus,
        CircuitUpEvent,
        CircuitDownEvent,
        CircuitStatus
    ];
}
