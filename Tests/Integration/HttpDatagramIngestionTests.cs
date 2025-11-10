using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration;

/// <summary>
/// Integration tests for HTTP datagram ingestion API
/// These tests verify that datagrams submitted via HTTP are processed
/// identically to UDP datagrams through the same pipeline.
/// </summary>
public class HttpDatagramIngestionTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public HttpDatagramIngestionTests(TestWebApplicationFactory factory, ITestOutputHelper output)
    {
        _client = factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task IngestSingleDatagram_NodeUpEvent_ReturnsAccepted()
    {
        // Arrange
        var datagram = new
        {
            @type = "NodeUpEvent",
            time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            nodeCall = "TEST-1",
            nodeAlias = "TESTNODE",
            locator = "IO91EC",
            latitude = 51.5074m,
            longitude = -0.1278m,
            software = "test-client",
            version = "v1.0"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ingest", datagram);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotNull(result);
        
        var status = result.GetProperty("status").GetString();
        Assert.True(status == "queued" || status == "processed");
        
        _output.WriteLine($"Status: {status}");
    }

    [Fact]
    public async Task IngestSingleDatagram_NodeStatusEvent_ReturnsAccepted()
    {
        // Arrange
        var datagram = new
        {
            @type = "NodeStatus",  // Fixed: use "NodeStatus" not "NodeStatusReportEvent"
            time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            nodeCall = "TEST-2",
            nodeAlias = "TESTNODE2",
            locator = "IO91EC",
            latitude = 51.5074m,
            longitude = -0.1278m,
            software = "test-client",
            version = "v1.0",
            uptimeSecs = 12345,
            linksIn = 2,
            linksOut = 3,
            cctsIn = 1,
            cctsOut = 2,
            l3Relayed = 150
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ingest", datagram);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task IngestSingleDatagram_LinkUpEvent_ReturnsAccepted()
    {
        // Arrange
        var datagram = new
        {
            @type = "LinkUpEvent",
            time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            node = "TEST-3",
            id = 123,
            direction = "outgoing",
            port = "1",
            local = "TEST-3",
            remote = "TEST-4"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ingest", datagram);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task IngestSingleDatagram_L2Trace_ReturnsAccepted()
    {
        // Arrange
        var datagram = new
        {
            @type = "L2Trace",
            reportFrom = "TEST-5",
            time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            port = "1",
            srce = "TEST-5",
            dest = "TEST-6",
            ctrl = 3,
            l2Type = "UI",
            cr = "C"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ingest", datagram);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task IngestBatchDatagrams_ValidDatagrams_ReturnsAccepted()
    {
        // Arrange
        var datagrams = new object[]
        {
            new
            {
                @type = "NodeUpEvent",
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                nodeCall = "BATCH-1",
                nodeAlias = "BATCH1",
                locator = "IO91EC",
                latitude = 51.5074m,
                longitude = -0.1278m,
                software = "test-client",
                version = "v1.0"
            },
            new
            {
                @type = "LinkUpEvent",
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                node = "BATCH-1",
                id = 123,
                direction = "outgoing",
                port = "1",
                local = "BATCH-1",
                remote = "BATCH-2"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ingest/batch", datagrams);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Accepted || 
                   response.StatusCode == HttpStatusCode.MultiStatus);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotNull(result);
        
        var successCount = result.GetProperty("successCount").GetInt32();
        var failureCount = result.GetProperty("failureCount").GetInt32();
        
        Assert.Equal(2, successCount + failureCount);
        _output.WriteLine($"Success: {successCount}, Failures: {failureCount}");
    }

    [Fact]
    public async Task IngestBatchDatagrams_EmptyBatch_ReturnsBadRequest()
    {
        // Arrange
        var emptyBatch = Array.Empty<object>();

        // Act
        var response = await _client.PostAsJsonAsync("/api/ingest/batch", emptyBatch);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task IngestSingleDatagram_InvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var invalidJson = new StringContent("{invalid json", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/ingest", invalidJson);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetIngestStatus_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/ingest/status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotNull(result);
        
        var service = result.GetProperty("service").GetString();
        var status = result.GetProperty("status").GetString();
        
        Assert.Equal("datagram-ingest", service);
        Assert.Equal("operational", status);
        
        _output.WriteLine($"Service: {service}, Status: {status}");
    }

    [Fact]
    public async Task IngestSingleDatagram_ResponseContainsSourceIp()
    {
        // Arrange
        var datagram = new
        {
            @type = "NodeUpEvent",
            time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            nodeCall = "IPTEST-1",
            nodeAlias = "IPTEST1",
            locator = "IO91EC",
            software = "test",
            version = "v1"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ingest", datagram);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotNull(result);
        
        var sourceIp = result.GetProperty("sourceIp").GetString();
        Assert.NotNull(sourceIp);
        Assert.NotEmpty(sourceIp);
        
        _output.WriteLine($"Source IP: {sourceIp}");
    }

    [Fact]
    public async Task IngestSingleDatagram_ResponseContainsReceivedAt()
    {
        // Arrange
        var datagram = new
        {
            @type = "NodeUpEvent",
            time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            nodeCall = "TIMETEST-1",
            nodeAlias = "TIMETEST1",
            locator = "IO91EC",
            software = "test",
            version = "v1"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ingest", datagram);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotNull(result);
        
        var receivedAt = result.GetProperty("receivedAt").GetString();
        Assert.NotNull(receivedAt);
        
        // Should be parseable as DateTime
        var timestamp = DateTime.Parse(receivedAt);
        Assert.True(timestamp > DateTime.UtcNow.AddMinutes(-1));
        
        _output.WriteLine($"Received At: {receivedAt}");
    }

    [Fact]
    public async Task IngestBatchDatagrams_LargeBatch_ReturnsAccepted()
    {
        // Arrange - Create a batch of 100 datagrams
        var datagrams = Enumerable.Range(1, 100).Select(i => new
        {
            @type = "L2Trace",
            reportFrom = $"LARGE-{i}",
            time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            port = "1",
            srce = $"LARGE-{i}",
            dest = $"LARGE-{i + 1}",
            ctrl = 3,
            l2Type = "UI",
            cr = "C"
        }).ToArray();

        // Act
        var response = await _client.PostAsJsonAsync("/api/ingest/batch", datagrams);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Accepted || 
                   response.StatusCode == HttpStatusCode.MultiStatus);
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotNull(result);
        
        var totalReceived = result.GetProperty("totalReceived").GetInt32();
        Assert.Equal(100, totalReceived);
        
        _output.WriteLine($"Large batch processed: {totalReceived} datagrams");
    }
}
