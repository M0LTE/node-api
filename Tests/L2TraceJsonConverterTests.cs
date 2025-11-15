using FluentAssertions;
using node_api;
using node_api.Models;
using System.Text.Json;

namespace Tests;

/// <summary>
/// Tests for L2TraceJsonConverter to ensure tseq is stripped from supervisory frames
/// (RR, RNR, REJ, SREJ) which do not have transmit sequence numbers according to AX.25 spec.
/// </summary>
public class L2TraceJsonConverterTests
{
    [Theory]
    [InlineData("RR")]
    [InlineData("RNR")]
    [InlineData("REJ")]
    [InlineData("SREJ")]
    public void Should_Strip_Tseq_From_Supervisory_Frames(string l2Type)
    {
        // Arrange - supervisory frame with invalid tseq field
        var json = $$"""
        {
            "@type": "L2Trace",
            "reportFrom": "G9XXX",
            "port": "2",
            "srce": "G8PZT-9",
            "dest": "KIDDER",
            "ctrl": 193,
            "l2Type": "{{l2Type}}",
            "modulo": 8,
            "rseq": 6,
            "tseq": 4,
            "cr": "R"
        }
        """;

        // Act
        var parsed = NetworkEventDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped as L2Trace;

        // Assert
        parsed.Should().BeTrue();
        frame.Should().NotBeNull();
        frame!.L2Type.Should().Be(l2Type);
        frame.ReceiveSequence.Should().Be(6);
        frame.TransmitSequence.Should().BeNull("tseq should be stripped from {0} frames", l2Type);
    }

    [Theory]
    [InlineData("RR")]
    [InlineData("RNR")]
    [InlineData("REJ")]
    [InlineData("SREJ")]
    public void Should_Allow_Supervisory_Frames_Without_Tseq(string l2Type)
    {
        // Arrange - supervisory frame without tseq (correct format)
        var json = $$"""
        {
            "@type": "L2Trace",
            "reportFrom": "G9XXX",
            "port": "2",
            "srce": "G8PZT-9",
            "dest": "KIDDER",
            "ctrl": 193,
            "l2Type": "{{l2Type}}",
            "modulo": 8,
            "rseq": 6,
            "cr": "R"
        }
        """;

        // Act
        var parsed = NetworkEventDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped as L2Trace;

        // Assert
        parsed.Should().BeTrue();
        frame.Should().NotBeNull();
        frame!.L2Type.Should().Be(l2Type);
        frame.ReceiveSequence.Should().Be(6);
        frame.TransmitSequence.Should().BeNull();
    }

    [Fact]
    public void Should_Preserve_Tseq_For_I_Frames()
    {
        // Arrange - I frame with tseq (correct format)
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "G9XXX",
            "port": "2",
            "srce": "G8PZT",
            "dest": "G8PZT-1",
            "ctrl": 136,
            "l2Type": "I",
            "modulo": 8,
            "rseq": 4,
            "tseq": 4,
            "cr": "C",
            "ilen": 63,
            "pid": 207,
            "ptcl": "NET/ROM"
        }
        """;

        // Act
        var parsed = NetworkEventDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped as L2Trace;

        // Assert
        parsed.Should().BeTrue();
        frame.Should().NotBeNull();
        frame!.L2Type.Should().Be("I");
        frame.ReceiveSequence.Should().Be(4);
        frame.TransmitSequence.Should().Be(4, "tseq should be preserved for I frames");
    }

    [Theory]
    [InlineData("UI")]
    [InlineData("SABME")]
    [InlineData("UA")]
    [InlineData("DM")]
    [InlineData("FRMR")]
    public void Should_Preserve_Tseq_For_Non_Supervisory_Frames_If_Present(string l2Type)
    {
        // Arrange - non-supervisory frame with tseq (may or may not be valid, but should be preserved)
        var json = $$"""
        {
            "@type": "L2Trace",
            "reportFrom": "G9XXX",
            "port": "2",
            "srce": "G8PZT",
            "dest": "G8PZT-1",
            "ctrl": 3,
            "l2Type": "{{l2Type}}",
            "modulo": 8,
            "tseq": 5,
            "cr": "C"
        }
        """;

        // Act
        var parsed = NetworkEventDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped as L2Trace;

        // Assert
        parsed.Should().BeTrue();
        frame.Should().NotBeNull();
        frame!.L2Type.Should().Be(l2Type);
        frame.TransmitSequence.Should().Be(5, "tseq should be preserved for non-supervisory frames");
    }

    [Theory]
    [InlineData("rr")]    // lowercase
    [InlineData("Rr")]    // mixed case
    [InlineData("RnR")]   // mixed case
    [InlineData("rej")]   // lowercase
    [InlineData("SrEj")]  // mixed case
    public void Should_Strip_Tseq_Case_Insensitively(string l2Type)
    {
        // Arrange - supervisory frame with various casings
        var json = $$"""
        {
            "@type": "L2Trace",
            "reportFrom": "G9XXX",
            "port": "2",
            "srce": "G8PZT-9",
            "dest": "KIDDER",
            "ctrl": 193,
            "l2Type": "{{l2Type}}",
            "modulo": 8,
            "rseq": 6,
            "tseq": 7,
            "cr": "R"
        }
        """;

        // Act
        var parsed = NetworkEventDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped as L2Trace;

        // Assert
        parsed.Should().BeTrue();
        frame.Should().NotBeNull();
        frame!.TransmitSequence.Should().BeNull("tseq should be stripped regardless of case");
    }

    [Theory]
    [InlineData("RR")]
    [InlineData("RNR")]
    [InlineData("REJ")]
    [InlineData("SREJ")]
    public void Should_Preserve_Other_Fields_When_Stripping_Tseq(string l2Type)
    {
        // Arrange - supervisory frame with multiple fields including tseq
        var json = $$"""
        {
            "@type": "L2Trace",
            "reportFrom": "G9XXX",
            "time": 1759688220,
            "port": "2",
            "dirn": "rcvd",
            "isRF": true,
            "srce": "G8PZT-9",
            "dest": "KIDDER",
            "ctrl": 193,
            "l2Type": "{{l2Type}}",
            "modulo": 8,
            "rseq": 6,
            "tseq": 99,
            "cr": "R",
            "pf": "P"
        }
        """;

        // Act
        var parsed = NetworkEventDatagramDeserialiser.TryDeserialise(json, out var datagramUntyped, out _);
        var frame = datagramUntyped as L2Trace;

        // Assert
        parsed.Should().BeTrue();
        frame.Should().NotBeNull();
        frame!.DatagramType.Should().Be("L2Trace");
        frame.ReportFrom.Should().Be("G9XXX");
        frame.TimeUnixSeconds.Should().Be(1759688220);
        frame.Port.Should().Be("2");
        frame.Direction.Should().Be("rcvd");
        frame.IsRF.Should().BeTrue();
        frame.Source.Should().Be("G8PZT-9");
        frame.Destination.Should().Be("KIDDER");
        frame.Control.Should().Be(193);
        frame.L2Type.Should().Be(l2Type);
        frame.Modulo.Should().Be(8);
        frame.ReceiveSequence.Should().Be(6);
        frame.CommandResponse.Should().Be("R");
        frame.PollFinal.Should().Be("P");
        frame.TransmitSequence.Should().BeNull("tseq should be stripped");
    }
}
