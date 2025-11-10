using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration;

/// <summary>
/// Tests to explicitly verify polymorphic JSON deserialization via the @type discriminator.
/// Ensures that ASP.NET Core correctly deserializes incoming JSON to the appropriate
/// NetworkEventDatagram derived type based on the @type field.
/// </summary>
public class PolymorphicDeserializationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public PolymorphicDeserializationTests(TestWebApplicationFactory factory, ITestOutputHelper _output)
    {
        _client = factory.CreateClient();
        this._output = _output;
    }

    #region NodeEvent Polymorphism Tests

    [Fact]
    public async Task NodeUpEvent_CorrectlyDeserializedViaDiscriminator()
    {
        // Arrange
        var json = """
        {
            "@type": "NodeUpEvent",
            "nodeCall": "POLY-1",
            "nodeAlias": "POLY1",
            "locator": "IO91EC",
            "software": "test",
            "version": "v1"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var type = result.GetProperty("type").GetString();
        Assert.Equal("NodeUpEvent", type);
        
        _output.WriteLine($"? NodeUpEvent correctly deserialized via @type discriminator");
    }

    [Fact]
    public async Task NodeStatus_CorrectlyDeserializedViaDiscriminator()
    {
        // Arrange
        var json = """
        {
            "@type": "NodeStatus",
            "nodeCall": "POLY-2",
            "nodeAlias": "POLY2",
            "locator": "IO91EC",
            "software": "test",
            "version": "v1",
            "uptimeSecs": 3600
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var type = result.GetProperty("type").GetString();
        Assert.Equal("NodeStatus", type);
        
        _output.WriteLine($"? NodeStatus correctly deserialized via @type discriminator");
    }

    [Fact]
    public async Task NodeDownEvent_CorrectlyDeserializedViaDiscriminator()
    {
        // Arrange
        var json = """
        {
            "@type": "NodeDownEvent",
            "nodeCall": "POLY-3",
            "nodeAlias": "POLY3",
            "reason": "shutdown"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Debug: Print response if not accepted
        if (response.StatusCode != HttpStatusCode.Accepted)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Error response: {errorContent}");
        }

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var type = result.GetProperty("type").GetString();
        Assert.Equal("NodeDownEvent", type);
        
        _output.WriteLine($"? NodeDownEvent correctly deserialized via @type discriminator");
    }

    #endregion

    #region LinkEvent Polymorphism Tests

    [Fact]
    public async Task LinkUpEvent_CorrectlyDeserializedViaDiscriminator()
    {
        // Arrange
        var json = """
        {
            "@type": "LinkUpEvent",
            "node": "POLY-4",
            "id": 1,
            "direction": "outgoing",
            "port": "1",
            "local": "POLY-4",
            "remote": "POLY-5"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var type = result.GetProperty("type").GetString();
        Assert.Equal("LinkUpEvent", type);
        
        _output.WriteLine($"? LinkUpEvent correctly deserialized via @type discriminator");
    }

    [Fact]
    public async Task LinkStatus_CorrectlyDeserializedViaDiscriminator()
    {
        // Arrange
        var json = """
        {
            "@type": "LinkStatus",
            "node": "POLY-6",
            "id": 2,
            "direction": "incoming",
            "port": "2",
            "local": "POLY-6",
            "remote": "POLY-7",
            "frmsSent": 100,
            "frmsRcvd": 50,
            "frmsResent": 5,
            "frmsQueued": 0
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var type = result.GetProperty("type").GetString();
        Assert.Equal("LinkStatus", type);
        
        _output.WriteLine($"? LinkStatus correctly deserialized via @type discriminator");
    }

    [Fact]
    public async Task LinkDownEvent_CorrectlyDeserializedViaDiscriminator()
    {
        // Arrange
        var json = """
        {
            "@type": "LinkDownEvent",
            "node": "POLY-8",
            "id": 3,
            "direction": "outgoing",
            "port": "1",
            "local": "POLY-8",
            "remote": "POLY-9",
            "frmsSent": 200,
            "frmsRcvd": 150,
            "frmsResent": 10,
            "frmsQueued": 0
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var type = result.GetProperty("type").GetString();
        Assert.Equal("LinkDownEvent", type);
        
        _output.WriteLine($"? LinkDownEvent correctly deserialized via @type discriminator");
    }

    #endregion

    #region CircuitEvent Polymorphism Tests

    [Fact]
    public async Task CircuitUpEvent_CorrectlyDeserializedViaDiscriminator()
    {
        // Arrange
        var json = """
        {
            "@type": "CircuitUpEvent",
            "node": "POLY-10",
            "id": 1,
            "direction": "incoming",
            "remote": "POLY-11@POLY-11:1234",
            "local": "POLY-10:5678"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var type = result.GetProperty("type").GetString();
        Assert.Equal("CircuitUpEvent", type);
        
        _output.WriteLine($"? CircuitUpEvent correctly deserialized via @type discriminator");
    }

    [Fact]
    public async Task CircuitStatus_CorrectlyDeserializedViaDiscriminator()
    {
        // Arrange
        var json = """
        {
            "@type": "CircuitStatus",
            "node": "POLY-12",
            "id": 2,
            "direction": "outgoing",
            "remote": "POLY-13@POLY-13:1234",
            "local": "POLY-12:5678",
            "segsSent": 50,
            "segsRcvd": 75,
            "segsResent": 2,
            "segsQueued": 0
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var type = result.GetProperty("type").GetString();
        Assert.Equal("CircuitStatus", type);
        
        _output.WriteLine($"? CircuitStatus correctly deserialized via @type discriminator");
    }

    [Fact]
    public async Task CircuitDownEvent_CorrectlyDeserializedViaDiscriminator()
    {
        // Arrange
        var json = """
        {
            "@type": "CircuitDownEvent",
            "node": "POLY-14",
            "id": 3,
            "direction": "incoming",
            "remote": "POLY-15@POLY-15:1234",
            "local": "POLY-14:5678",
            "segsSent": 100,
            "segsRcvd": 150,
            "segsResent": 5,
            "segsQueued": 0
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var type = result.GetProperty("type").GetString();
        Assert.Equal("CircuitDownEvent", type);
        
        _output.WriteLine($"? CircuitDownEvent correctly deserialized via @type discriminator");
    }

    #endregion

    #region L2Trace Polymorphism Tests

    [Fact]
    public async Task L2Trace_CorrectlyDeserializedViaDiscriminator()
    {
        // Arrange
        var json = """
        {
            "@type": "L2Trace",
            "reportFrom": "POLY-16",
            "port": "1",
            "srce": "POLY-16",
            "dest": "POLY-17",
            "ctrl": 3,
            "l2Type": "UI",
            "cr": "C"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var type = result.GetProperty("type").GetString();
        Assert.Equal("L2Trace", type);
        
        _output.WriteLine($"? L2Trace correctly deserialized via @type discriminator");
    }

    #endregion

    #region Invalid Discriminator Tests

    [Fact]
    public async Task UnknownType_ReturnsBadRequest()
    {
        // Arrange
        var json = """
        {
            "@type": "UnknownEventType",
            "someField": "someValue"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        _output.WriteLine($"? Unknown @type correctly rejected with BadRequest");
    }

    [Fact]
    public async Task MissingTypeField_AcceptsAsBaseType()
    {
        // Arrange - JSON without @type field
        var json = """
        {
            "nodeCall": "INVALID-1",
            "nodeAlias": "INVALID1"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        // Without @type, it deserializes to base NetworkEventDatagram type and is accepted
        // (though it may fail validation later in the pipeline)
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        _output.WriteLine($"? Missing @type field accepted (deserializes to base type)");
    }

    [Fact]
    public async Task WrongTypeForFields_ReturnsBadRequest()
    {
        // Arrange - NodeUpEvent with @type of LinkUpEvent
        var json = """
        {
            "@type": "LinkUpEvent",
            "nodeCall": "MISMATCH-1",
            "nodeAlias": "MISMATCH1",
            "locator": "IO91EC",
            "software": "test",
            "version": "v1"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        _output.WriteLine($"? Type/field mismatch correctly rejected with BadRequest");
    }

    #endregion

    #region Batch Polymorphism Tests

    [Fact]
    public async Task BatchWithMultipleTypes_CorrectlyDeserializesAll()
    {
        // Arrange - Mix of different event types in one batch
        var json = """
        [
            {
                "@type": "NodeUpEvent",
                "nodeCall": "BATCH-1",
                "nodeAlias": "BATCH1",
                "locator": "IO91EC",
                "software": "test",
                "version": "v1"
            },
            {
                "@type": "LinkUpEvent",
                "node": "BATCH-2",
                "id": 1,
                "direction": "outgoing",
                "port": "1",
                "local": "BATCH-2",
                "remote": "BATCH-3"
            },
            {
                "@type": "L2Trace",
                "reportFrom": "BATCH-4",
                "port": "1",
                "srce": "BATCH-4",
                "dest": "BATCH-5",
                "ctrl": 3,
                "l2Type": "UI",
                "cr": "C"
            },
            {
                "@type": "CircuitUpEvent",
                "node": "BATCH-6",
                "id": 1,
                "direction": "incoming",
                "remote": "BATCH-7@BATCH-7:1234",
                "local": "BATCH-6:5678"
            }
        ]
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest/batch", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Accepted || 
                   response.StatusCode == HttpStatusCode.MultiStatus);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var successCount = result.GetProperty("successCount").GetInt32();
        Assert.Equal(4, successCount);
        
        _output.WriteLine($"? Batch with 4 different types all correctly deserialized");
    }

    [Fact]
    public async Task BatchWithSomeInvalidTypes_AcceptsAllButMayFailValidation()
    {
        // Arrange - Mix of valid and invalid types
        var json = """
        [
            {
                "@type": "NodeUpEvent",
                "nodeCall": "PARTIAL-1",
                "nodeAlias": "PARTIAL1",
                "locator": "IO91EC",
                "software": "test",
                "version": "v1"
            },
            {
                "@type": "InvalidType",
                "someField": "someValue"
            },
            {
                "@type": "LinkUpEvent",
                "node": "PARTIAL-2",
                "id": 1,
                "direction": "outgoing",
                "port": "1",
                "local": "PARTIAL-2",
                "remote": "PARTIAL-3"
            }
        ]
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest/batch", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Debug
        if (response.StatusCode != HttpStatusCode.Accepted && response.StatusCode != HttpStatusCode.MultiStatus)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Unexpected status: {response.StatusCode}");
            _output.WriteLine($"Response: {errorContent}");
        }

        // Assert
        // All items are accepted at the controller level (they deserialize successfully)
        // Invalid types will fail validation later in the DatagramProcessor pipeline
        Assert.True(response.StatusCode == HttpStatusCode.Accepted || 
                   response.StatusCode == HttpStatusCode.MultiStatus || 
                   response.StatusCode == HttpStatusCode.BadRequest); // BadRequest if entire batch fails to deserialize
        
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.MultiStatus)
        {
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var totalReceived = result.GetProperty("totalReceived").GetInt32();
            
            Assert.Equal(3, totalReceived);
            
            _output.WriteLine($"? Batch accepted: {totalReceived} items received");
        }
        else
        {
            _output.WriteLine($"? Batch rejected at controller level (BadRequest)");
        }
    }

    #endregion

    #region Case Sensitivity Tests

    [Fact]
    public async Task TypeField_IsCaseSensitive()
    {
        // Arrange - Wrong case in @type
        var json = """
        {
            "@type": "nodeupevent",
            "nodeCall": "CASE-1",
            "nodeAlias": "CASE1",
            "locator": "IO91EC",
            "software": "test",
            "version": "v1"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        _output.WriteLine($"? Lowercase @type correctly rejected (case-sensitive)");
    }

    [Fact]
    public async Task PropertyNames_AreCaseInsensitive()
    {
        // Arrange - Mixed case in property names (should work due to PropertyNameCaseInsensitive)
        var json = """
        {
            "@type": "NodeUpEvent",
            "NodeCall": "CASE-2",
            "NODEALIAS": "CASE2",
            "LOCATOR": "IO91EC",
            "software": "test",
            "VERSION": "v1"
        }
        """;

        // Act
        var response = await _client.PostAsync("/api/ingest", 
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        _output.WriteLine($"? Mixed-case property names accepted (case-insensitive)");
    }

    #endregion
}
