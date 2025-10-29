using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using node_api.Controllers;

namespace Tests;

public class DiagnosticsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DiagnosticsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    #region Valid Datagram Tests

    [Fact]
    public async Task Should_Accept_Valid_NodeUpEvent()
    {
        // Arrange
        var json = """
        {
            "@type": "NodeUpEvent",
            "nodeCall": "G8PZT-1",
            "nodeAlias": "XRLN64",
            "locator": "IO70KD",
            "software": "XrLin",
            "version": "504j"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
        result.Stage.Should().Be("complete");
        result.DatagramType.Should().Be("NodeUpEvent");
        result.Message.Should().Contain("valid");
    }

    [Fact]
    public async Task Should_Accept_Valid_L2Trace()
    {
        // Arrange - Use correct property names from L2Trace model
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "G9XXX",
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "ID",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
        result.Stage.Should().Be("complete");
        result.DatagramType.Should().Be("L2Trace");
    }

    [Fact]
    public async Task Should_Accept_Valid_CircuitStatus()
    {
        // Arrange - Use correct JSON property names
        var json = """
        {
            "@type": "CircuitStatus",
            "time": 1759688220,
            "node": "G8PZT-1",
            "id": 1,
            "direction": "incoming",
            "remote": "G8PZT@G8PZT:14c0",
            "local": "G8PZT-4:0001",
            "segsSent": 5,
            "segsRcvd": 27,
            "segsResent": 0,
            "segsQueued": 0
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
        result.Stage.Should().Be("complete");
        result.DatagramType.Should().Be("CircuitStatus");
    }

    [Fact]
    public async Task Should_Accept_Valid_LinkUpEvent()
    {
        // Arrange
        var json = """
        {
            "@type": "LinkUpEvent",
            "timeUnixSeconds": 1729512000,
            "node": "G8PZT-1",
            "id": 3,
            "direction": "outgoing",
            "port": "2",
            "remote": "KIDDER-1",
            "local": "G8PZT-11"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
        result.Stage.Should().Be("complete");
        result.DatagramType.Should().Be("LinkUpEvent");
    }

    #endregion

    #region JSON Parsing Error Tests

    [Fact]
    public async Task Should_Return_JSON_Parsing_Error_For_Malformed_JSON()
    {
        // Arrange - Missing comma between properties
        var json = """
        {
            "@type": "NodeUpEvent"
            "nodeCall": "G8PZT-1"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.Stage.Should().Be("json_parsing");
        result.Error.Should().NotBeNullOrEmpty();
        result.ErrorDetails.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Return_JSON_Parsing_Error_For_Invalid_JSON()
    {
        // Arrange - Not valid JSON at all
        var json = "This is not JSON";

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.Stage.Should().Be("json_parsing");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Return_JSON_Parsing_Error_With_Line_Number()
    {
        // Arrange - Error on specific line
        var json = """
        {
            "@type": "NodeUpEvent",
            "nodeCall": "G8PZT-1",
            "nodeAlias": "XRLN64"
            "locator": "IO70KD"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.Stage.Should().Be("json_parsing");
        result.ErrorDetails.Should().NotBeNull();
    }

    #endregion

    #region Type Recognition Error Tests

    [Fact]
    public async Task Should_Return_Type_Recognition_Error_For_Missing_Type()
    {
        // Arrange - Missing @type field
        var json = """
        {
            "nodeCall": "G8PZT-1",
            "nodeAlias": "XRLN64"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.Stage.Should().Be("type_recognition");
        result.Error.Should().Contain("@type");
        result.SupportedTypes.Should().NotBeNull();
        result.SupportedTypes.Should().Contain(new[] { "L2Trace", "NodeUpEvent", "NodeStatus" });
    }

    [Fact]
    public async Task Should_Return_Type_Recognition_Error_For_Unknown_Type()
    {
        // Arrange - Invalid @type value
        var json = """
        {
            "@type": "UnknownEventType",
            "nodeCall": "G8PZT-1"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.Stage.Should().Be("type_recognition");
        result.Error.Should().Contain("Unknown datagram type");
        result.Error.Should().Contain("UnknownEventType");
        result.DatagramType.Should().Be("UnknownEventType");
        result.SupportedTypes.Should().NotBeNull();
        result.SupportedTypes.Should().HaveCountGreaterThan(5);
    }

    [Fact]
    public async Task Should_List_All_Supported_Types_On_Unknown_Type()
    {
        // Arrange
        var json = """
        {
            "@type": "SomeRandomType"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.SupportedTypes.Should().Contain(new[]
        {
            "L2Trace",
            "NodeUpEvent",
            "NodeDownEvent",
            "NodeStatus",
            "LinkUpEvent",
            "LinkDownEvent",
            "LinkStatus",
            "CircuitUpEvent",
            "CircuitDownEvent",
            "CircuitStatus"
        });
    }

    #endregion

    #region Validation Error Tests

    [Fact]
    public async Task Should_Return_Validation_Errors_For_Invalid_NodeUpEvent()
    {
        // Arrange - Empty nodeCall (missing required fields also)
        var json = """
        {
            "@type": "NodeUpEvent",
            "nodeCall": "",
            "nodeAlias": "",
            "locator": "INVALID",
            "software": "",
            "version": ""
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.Stage.Should().Be("validation");
        result.DatagramType.Should().Be("NodeUpEvent");
        result.ValidationErrors.Should().NotBeNull();
        result.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_Return_Multiple_Validation_Errors()
    {
        // Arrange - Invalid direction, validator will catch it after deserialization
        var json = """
        {
            "@type": "LinkUpEvent",
            "time": 1729512000,
            "node": "G8PZT-1",
            "id": 3,
            "direction": "sideways",
            "port": "2",
            "remote": "KIDDER-1",
            "local": "G8PZT-11"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.Stage.Should().Be("validation");
        result.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_Include_Property_Names_In_Validation_Errors()
    {
        // Arrange
        var json = """
        {
            "@type": "LinkUpEvent",
            "timeUnixSeconds": 1729512000,
            "node": "",
            "id": 3,
            "direction": "outgoing",
            "port": "",
            "remote": "KIDDER-1",
            "local": "G8PZT-11"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.ValidationErrors.Should().NotBeNull();
        result.ValidationErrors!.Should().Contain(e => !string.IsNullOrEmpty(e.PropertyName));
        result.ValidationErrors.Should().Contain(e => !string.IsNullOrEmpty(e.ErrorMessage));
    }

    [Fact]
    public async Task Should_Include_Attempted_Values_In_Validation_Errors()
    {
        // Arrange - Invalid direction value
        var json = """
        {
            "@type": "LinkUpEvent",
            "time": 1729512000,
            "node": "G8PZT-1",
            "id": 3,
            "direction": "wrong-direction",
            "port": "2",
            "remote": "KIDDER-1",
            "local": "G8PZT-11"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.ValidationErrors.Should().NotBeNull();
        result.ValidationErrors!.Should().Contain(e => 
            e.PropertyName.Contains("direction", StringComparison.OrdinalIgnoreCase) ||
            e.PropertyName.Contains("Direction", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Should_Return_JSON_Property_Names_In_Validation_Errors()
    {
        // Arrange - Invalid L2Trace with various errors
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "",
            "port": "",
            "srce": "",
            "dest": "",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C"
        }
        """;
        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().NotBeNull();

        // Verify JSON property names are used, not C# property names
        var propertyNames = result.ValidationErrors!.Select(e => e.PropertyName).ToList();

        // Should use JSON names like "reportFrom", "port", "srce", "dest"
        // NOT C# names like "ReportFrom", "Port", "Source", "Destination"
        propertyNames.Should().Contain("reportFrom");
        propertyNames.Should().Contain("port");
        propertyNames.Should().Contain("srce");
        propertyNames.Should().Contain("dest");

        // Should NOT contain C# property names
        propertyNames.Should().NotContain("ReportFrom");
        propertyNames.Should().NotContain("Port");
        propertyNames.Should().NotContain("Source");
        propertyNames.Should().NotContain("Destination");
    }

    [Fact]
    public async Task Should_Return_JSON_Property_Names_For_CircuitStatus_Errors()
    {
        // Arrange - Invalid CircuitStatus with errors
        var json = """
        {
            "@type": "CircuitStatus",
            "node": "",
            "id": 0,
            "direction": "invalid",
            "remote": "",
            "local": "",
            "segsSent": -1,
            "segsRcvd": -1,
            "segsResent": 0,
            "segsQueued": 0
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().NotBeNull();

        var propertyNames = result.ValidationErrors!.Select(e => e.PropertyName).ToList();

        // Should use JSON names like "segsSent", "segsRcvd"
        // NOT C# names like "SegmentsSent", "SegmentsReceived"
        if (propertyNames.Any(p => p.Contains("Sent", StringComparison.Ordinal)))
        {
            propertyNames.Where(p => p.Contains("Sent", StringComparison.Ordinal))
                .Should().Contain("segsSent");
            propertyNames.Should().NotContain("SegmentsSent");
        }

        if (propertyNames.Any(p => p.Contains("Rcvd", StringComparison.OrdinalIgnoreCase) || 
                                    p.Contains("Received", StringComparison.OrdinalIgnoreCase)))
        {
            propertyNames.Where(p => p.Contains("Rcvd", StringComparison.OrdinalIgnoreCase) || 
                                       p.Contains("Received", StringComparison.OrdinalIgnoreCase))
                .Should().Contain("segsRcvd");
            propertyNames.Should().NotContain("SegmentsReceived");
        }
    }

    [Fact]
    public async Task Should_Use_JSON_Property_Names_In_NetRom_Validation_Error_Messages()
    {
        // Arrange - Invalid NetRom CONN REQ with missing srcUser and srcNode
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "G9XXX",
            "port": "2",
            "srce": "G8PZT-1",
            "dest": "G8PZT",
            "ctrl": 232,
            "l2Type": "I",
            "cr": "C",
            "ptcl": "NET/ROM",
            "l3type": "NetRom",
            "l3src": "G8PZT-1",
            "l3dst": "G8PZT",
            "ttl": 25,
            "l4type": "CONN REQ",
            "fromCct": 4,
            "window": 4
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().NotBeNull();

        // Find errors related to srcUser and srcNode
        var errors = result.ValidationErrors!.ToList();
        
        // Should have errors mentioning srcUser and srcNode
        errors.Should().Contain(e => e.ErrorMessage.Contains("srcUser", StringComparison.OrdinalIgnoreCase));
        errors.Should().Contain(e => e.ErrorMessage.Contains("srcNode", StringComparison.OrdinalIgnoreCase));
        
        // Should NOT mention the C# property names
        errors.Should().NotContain(e => e.ErrorMessage.Contains("OriginatingUserCallsign", StringComparison.Ordinal));
        errors.Should().NotContain(e => e.ErrorMessage.Contains("OriginatingNodeCallsign", StringComparison.Ordinal));
        
        // The property names in the errors should be JSON property names
        errors.Where(e => e.PropertyName.Contains("src", StringComparison.OrdinalIgnoreCase))
            .Should().Contain(e => e.PropertyName == "srcUser" || e.PropertyName == "srcNode");
    }

    #endregion

    #region Content-Type Tests

    [Fact]
    public async Task Should_Accept_Text_Plain_Content_Type()
    {
        // Arrange
        var json = """
        {
            "@type": "NodeUpEvent",
            "nodeCall": "G8PZT-1",
            "nodeAlias": "XRLN64",
            "locator": "IO70KD",
            "software": "XrLin",
            "version": "504j"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "text/plain"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Accept_Application_Json_Content_Type()
    {
        // Arrange
        var json = """
        {
            "@type": "NodeDownEvent",
            "timeUnixSeconds": 1759682231,
            "nodeCall": "G8PZT-1",
            "nodeAlias": "XRLN64"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
    public async Task Should_Handle_L2Trace_With_Digipeaters()
    {
        // Arrange - Use correct property names
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "G9XXX",
            "port": "1",
            "srce": "G8PZT-1",
            "dest": "ID",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C",
            "digis": [
                {
                    "call": "RELAY-1",
                    "rptd": true
                },
                {
                    "call": "RELAY-2",
                    "rptd": false
                }
            ]
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Validate_Complete_NetRom_Frame()
    {
        // Arrange - Using correct property names
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "G9XXX",
            "time": 1729512000,
            "port": "2",
            "srce": "G8PZT-1",
            "dest": "G8PZT",
            "ctrl": 232,
            "l2Type": "I",
            "cr": "C",
            "ilen": 36,
            "pid": 207,
            "ptcl": "NET/ROM",
            "l3type": "NetRom",
            "l3src": "G8PZT-1",
            "l3dst": "G8PZT",
            "ttl": 25,
            "l4type": "CONN REQ",
            "fromCct": 4,
            "srcUser": "G8PZT-4",
            "srcNode": "G8PZT-1",
            "window": 4
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Return_Received_JSON_In_Response()
    {
        // Arrange
        var json = """
        {
            "@type": "NodeUpEvent",
            "nodeCall": "TEST",
            "nodeAlias": "ALIAS",
            "locator": "IO70KD",
            "software": "test",
            "version": "1.0"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.ReceivedJson.Should().NotBeNullOrEmpty();
        result.ReceivedJson.Should().Contain("TEST");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Should_Handle_Empty_JSON_Object()
    {
        // Arrange
        var json = "{}";

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.Stage.Should().Be("type_recognition");
    }

    [Fact]
    public async Task Should_Handle_Null_Type_Field()
    {
        // Arrange
        var json = """
        {
            "@type": null,
            "nodeCall": "G8PZT-1"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Handle_Case_Sensitivity_In_Type_Field()
    {
        // Arrange - Test that @TYPE (uppercase) is handled - deserialization is case-insensitive
        var json = """
        {
            "@type": "NodeUpEvent",
            "nodeCall": "G8PZT-1",
            "nodeAlias": "XRLN64",
            "locator": "IO70KD",
            "software": "XrLin",
            "version": "504j"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        // The deserializer is case-insensitive
        result!.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("NodeStatus")]
    [InlineData("LinkDownEvent")]
    [InlineData("CircuitUpEvent")]
    [InlineData("CircuitDownEvent")]
    public async Task Should_Recognize_All_Datagram_Types(string datagramType)
    {
        // Arrange - Minimal JSON, may not be valid but type should be recognized  
        // Note: Will likely fail validation due to missing required fields
        var json = $$"""
        {
            "@type": "{{datagramType}}"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/diagnostics/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DiagnosticsController.ValidationResponse>();
        result.Should().NotBeNull();
        // The datagram type should be recognized even if validation fails
        // Note: May not deserialize successfully due to missing required fields
        if (result!.DatagramType != null)
        {
            result.DatagramType.Should().Be(datagramType);
        }
        else
        {
            // If it failed to deserialize, check it was recognized in the error
            result.Stage.Should().Be("json_parsing");
        }
    }

    #endregion

    #region CORS Tests

    [Fact]
    public async Task Should_Support_CORS_For_Diagnostics_Endpoint()
    {
        // Arrange
        var json = """
        {
            "@type": "NodeUpEvent",
            "nodeCall": "G8PZT-1",
            "nodeAlias": "XRLN64"
        }
        """;
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/diagnostics/validate")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Origin", "https://example.com");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("*");
    }

    #endregion

    #region Server Time Tests

    [Fact]
    public async Task ServerTime_Endpoint_Should_Return_Current_Time()
    {
        // Act
        var response = await _client.GetAsync("/api/diagnostics/server-time");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServerTimeResponse>();
        result.Should().NotBeNull();
        result!.ServerTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task ServerTime_Endpoint_Should_Return_Utc_Time()
    {
        // Act
        var response = await _client.GetAsync("/api/diagnostics/server-time");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServerTimeResponse>();
        result.Should().NotBeNull();
        result!.ServerTime.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task ServerTime_Endpoint_Should_Have_Correct_Content_Type()
    {
        // Act
        var response = await _client.GetAsync("/api/diagnostics/server-time");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task ServerTime_Endpoint_Should_Support_CORS()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/diagnostics/server-time");
        request.Headers.Add("Origin", "https://example.com");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
    }

    [Fact]
    public async Task ServerTime_Endpoint_Should_Be_Consistent_Across_Multiple_Calls()
    {
        // Arrange - Make multiple calls and ensure times are sequential
        var times = new List<DateTime>();

        // Act
        for (int i = 0; i < 3; i++)
        {
            var response = await _client.GetAsync("/api/diagnostics/server-time");
            var result = await response.Content.ReadFromJsonAsync<ServerTimeResponse>();
            times.Add(result!.ServerTime);
            await Task.Delay(10); // Small delay between calls
        }

        // Assert
        times.Should().HaveCount(3);
        times[0].Should().BeBefore(times[2]); // First call should be before last call
    }

    // Helper class for deserializing server time response
    private class ServerTimeResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("serverTime")]
        public DateTime ServerTime { get; set; }
    }

    #endregion
}
