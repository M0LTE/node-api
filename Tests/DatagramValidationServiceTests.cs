using FluentValidation;
using node_api.Models;
using node_api.Validators;

namespace Tests;

public class DatagramValidationServiceTests
{
    private readonly DatagramValidationService _service;

    public DatagramValidationServiceTests()
    {
        _service = new DatagramValidationService(
            new L2TraceValidator(),
            new NodeUpEventValidator(),
            new NodeDownEventValidator(),
            new NodeStatusReportEventValidator(),
            new LinkUpEventValidator(),
            new LinkDisconnectionEventValidator(),
            new LinkStatusValidator(),
            new CircuitUpEventValidator(),
            new CircuitDisconnectionEventValidator(),
            new CircuitStatusValidator()
        );
    }

    [Fact]
    public void Should_Validate_L2Trace()
    {
        var datagram = new L2Trace
        {
            DatagramType = "L2Trace",
            ReportFrom = "G9XXX",
            Port = "1",
            Source = "G8PZT-1",
            Destination = "ID",
            Control = 3,
            L2Type = "UI",
            Modulo = 8,
            CommandResponse = "C"
        };

        var result = _service.Validate(datagram);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Should_Validate_CircuitStatus()
    {
        var datagram = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "G8PZT-1",
            Id = 1,
            Direction = "incoming",
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = 5,
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
        };

        var result = _service.Validate(datagram);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Should_Validate_LinkUpEvent()
    {
        var datagram = new LinkUpEvent
        {
            DatagramType = "LinkUpEvent",
            TimeUnixSeconds = 1729512000,
            Node = "G8PZT-1",
            Id = 3,
            Direction = "outgoing",
            Port = "2",
            Remote = "KIDDER-1",
            Local = "G8PZT-11"
        };

        var result = _service.Validate(datagram);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Should_Validate_NodeUpEvent()
    {
        var datagram = new NodeUpEvent
        {
            DatagramType = "NodeUpEvent",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64",
            Locator = "IO70KD",
            Software = "XrLin",
            Version = "504j"
        };

        var result = _service.Validate(datagram);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Should_Detect_Invalid_Datagram()
    {
        var datagram = new CircuitStatus
        {
            DatagramType = "CircuitStatus",
            TimeUnixSeconds = 1759688220,
            Node = "",  // Invalid
            Id = 0,     // Invalid
            Direction = "sideways",  // Invalid
            Remote = "G8PZT@G8PZT:14c0",
            Local = "G8PZT-4:0001",
            SegsSent = -1,  // Invalid
            SegsRcvd = 27,
            SegsResent = 0,
            SegsQueued = 0
        };

        var result = _service.Validate(datagram);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Should_Return_IsValid_Through_Helper_Method()
    {
        var validDatagram = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            NodeCall = "G8PZT-1",
            NodeAlias = "XRLN64"
        };

        var isValid = _service.IsValid(validDatagram, out var validationResult);
        
        Assert.True(isValid);
        Assert.True(validationResult.IsValid);
    }

    [Fact]
    public void Should_Return_Errors_Through_Helper_Method()
    {
        var invalidDatagram = new NodeDownEvent
        {
            DatagramType = "NodeDownEvent",
            NodeCall = "",  // Invalid
            NodeAlias = "XRLN64"
        };

        var isValid = _service.IsValid(invalidDatagram, out var validationResult);
        
        Assert.False(isValid);
        Assert.False(validationResult.IsValid);
        Assert.NotEmpty(validationResult.Errors);
    }

    [Fact]
    public void Should_Validate_All_Datagram_Types()
    {
        var datagrams = new UdpNodeInfoJsonDatagram[]
        {
            new L2Trace
            {
                DatagramType = "L2Trace",
                ReportFrom = "G9XXX",
                Port = "1",
                Source = "G8PZT-1",
                Destination = "ID",
                Control = 3,
                L2Type = "UI",
                Modulo = 8,
                CommandResponse = "C"
            },
            new CircuitStatus
            {
                DatagramType = "CircuitStatus",
                TimeUnixSeconds = 1759688220,
                Node = "G8PZT-1",
                Id = 1,
                Direction = "incoming",
                Remote = "G8PZT@G8PZT:14c0",
                Local = "G8PZT-4:0001",
                SegsSent = 5,
                SegsRcvd = 27,
                SegsResent = 0,
                SegsQueued = 0
            },
            new CircuitUpEvent
            {
                DatagramType = "CircuitUpEvent",
                TimeUnixSeconds = 1759688220,
                Node = "G8PZT",
                Id = 1,
                Direction = "incoming",
                Remote = "G8PZT@G8PZT:14c0",
                Local = "G8PZT-4:0001"
            },
            new CircuitDisconnectionEvent
            {
                DatagramType = "CircuitDownEvent",
                TimeUnixSeconds = 1759688220,
                Node = "G8PZT",
                Id = 1,
                Direction = "incoming",
                Remote = "G8PZT@G8PZT:14c0",
                Local = "G8PZT-4:0001",
                SegsSent = 5,
                SegsRcvd = 27,
                SegsResent = 0,
                SegsQueued = 0
            },
            new LinkUpEvent
            {
                DatagramType = "LinkUpEvent",
                TimeUnixSeconds = 1729512000,
                Node = "G8PZT-1",
                Id = 3,
                Direction = "outgoing",
                Port = "2",
                Remote = "KIDDER-1",
                Local = "G8PZT-11"
            },
            new LinkDisconnectionEvent
            {
                DatagramType = "LinkDownEvent",
                TimeUnixSeconds = 1761053424,
                Node = "G8PZT-1",
                Id = 3,
                Direction = "outgoing",
                Port = "2",
                Remote = "KIDDER-1",
                Local = "G8PZT-11",
                UpForSecs = 78,
                FramesSent = 100,
                FramesReceived = 50,
                FramesResent = 5,
                FramesQueued = 0
            },
            new LinkStatus
            {
                DatagramType = "LinkStatus",
                TimeUnixSeconds = 1729512000,
                Node = "G8PZT-1",
                Id = 3,
                Direction = "incoming",
                Port = "2",
                Remote = "KIDDER-1",
                Local = "G8PZT-11",
                UpForSecs = 300,
                FramesSent = 100,
                FramesReceived = 50,
                FramesResent = 5,
                FramesQueued = 2
            },
            new NodeUpEvent
            {
                DatagramType = "NodeUpEvent",
                NodeCall = "G8PZT-1",
                NodeAlias = "XRLN64",
                Locator = "IO70KD",
                Software = "XrLin",
                Version = "504j"
            },
            new NodeDownEvent
            {
                DatagramType = "NodeDownEvent",
                NodeCall = "G8PZT-1",
                NodeAlias = "XRLN64"
            },
            new NodeStatusReportEvent
            {
                DatagramType = "NodeStatus",
                NodeCall = "G8PZT-1",
                NodeAlias = "XRLN64",
                Locator = "IO70KD",
                Software = "XrLin",
                Version = "504j",
                UptimeSecs = 86400
            }
        };

        foreach (var datagram in datagrams)
        {
            var result = _service.Validate(datagram);
            Assert.True(result.IsValid, $"Failed to validate {datagram.DatagramType}");
        }
    }
}
