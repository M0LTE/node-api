using Microsoft.AspNetCore.Mvc;
using node_api.Filters;
using node_api.Services;
using node_api.Models;
using System.Text.Json;

namespace node_api.Controllers;

/// <summary>
/// HTTP API for ingesting network event datagrams
/// Data submitted here is queued through RabbitMQ for the same downstream processing pipeline as UDP ingress
/// </summary>
[ApiController]
[Route("api/ingest")]
public class IngestController : ControllerBase
{
    private readonly ILogger<IngestController> _logger;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;

    public IngestController(
        ILogger<IngestController> logger,
        IRabbitMqPublisher rabbitMqPublisher)
    {
        _logger = logger;
        _rabbitMqPublisher = rabbitMqPublisher;
    }

    /// <summary>
    /// Ingest a single network event datagram via HTTP
    /// </summary>
    /// <param name="datagram">The event datagram (NodeUpEvent, LinkUpEvent, L2Trace, etc.)</param>
    /// <returns>202 Accepted if successfully queued, 503 if RabbitMQ unavailable</returns>
    /// <remarks>
    /// Accepts any datagram type supported by the packet network monitoring system.
    /// The datagram will be published to RabbitMQ and processed through the same pipeline as UDP-received events.
    /// 
    /// Supported datagram types (discriminated by "@type" field):
    /// - **NodeUpEvent**: Node startup notification
    /// - **NodeStatus**: Periodic node status report  
    /// - **NodeDownEvent**: Node shutdown notification
    /// - **LinkUpEvent**: AX.25 link connection established
    /// - **LinkStatus**: Link status report
    /// - **LinkDownEvent**: AX.25 link disconnected
    /// - **CircuitUpEvent**: NetRom circuit established
    /// - **CircuitStatus**: Circuit status report
    /// - **CircuitDownEvent**: NetRom circuit disconnected
    /// - **L2Trace**: Layer 2 frame trace (detailed packet analysis)
    /// 
    /// Example NodeUpEvent:
    /// ```json
    /// {
    ///   "@type": "NodeUpEvent",
    ///   "time": 1234567890,
    ///   "nodeCall": "M0LTE-1",
    ///   "nodeAlias": "MYLTE1",
    ///   "locator": "IO91EC",
    ///   "latitude": 51.5074,
    ///   "longitude": -0.1278,
    ///   "software": "xrlin",
    ///   "version": "v504j"
    /// }
    /// ```
    /// 
    /// Example LinkUpEvent:
    /// ```json
    /// {
    ///   "@type": "LinkUpEvent",
    ///   "time": 1234567890,
    ///   "node": "M0LTE-1",
    ///   "id": 123,
    ///   "direction": "outgoing",
    ///   "port": "1",
    ///   "local": "M0LTE-1",
    ///   "remote": "G0ABC-2"
    /// }
    /// ```
    /// 
    /// Example L2Trace:
    /// ```json
    /// {
    ///   "@type": "L2Trace",
    ///   "reportFrom": "M0LTE-1",
    ///   "time": 1234567890,
    ///   "port": "1",
    ///   "srce": "M0LTE-1",
    ///   "dest": "G0ABC",
    ///   "ctrl": 3,
    ///   "l2Type": "UI",
    ///   "cr": "C",
    ///   "ilen": 64,
    ///   "pid": 240,
    ///   "ptcl": "DATA"
    /// }
    /// ```
    /// </remarks>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> IngestDatagramAsync([FromBody] NetworkEventDatagram datagram)
        => IngestTypedDatagramAsync(datagram);

    /// <summary>
    /// Replace the per-port metadata (band, frequency, mode, bitrate, usage, comment) used to annotate
    /// links. Authenticated (X-Api-Key); intended for a single trusted source that derives it from
    /// operator port config/comments. The full set is sent each time and replaces the previous set.
    /// </summary>
    [HttpPost("port-metadata")]
    [IngestApiKey]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult IngestPortMetadata(
        [FromBody] PortMetadata[] items, [FromServices] IPortMetadataStore store)
    {
        if (items is null) return BadRequest("Body required.");
        store.Replace(items);
        _logger.LogInformation("Ingested {Count} port-metadata records", items.Length);
        return Ok(new { received = items.Length });
    }

    // ========== Typed Endpoints for Better OpenAPI Documentation ==========

    /// <summary>
    /// Ingest a NodeUpEvent datagram
    /// </summary>
    /// <remarks>
    /// Reports a node coming online. Required fields: nodeCall, nodeAlias, locator, software, version.
    /// 
    /// Example:
    /// ```json
    /// {
    ///   "time": 1234567890,
    ///   "nodeCall": "M0LTE-1",
    ///   "nodeAlias": "MYLTE1",
    ///   "locator": "IO91EC",
    ///   "latitude": 51.5074,
    ///   "longitude": -0.1278,
    ///   "software": "xrlin",
    ///   "version": "v504j"
    /// }
    /// ```
    /// </remarks>
    [HttpPost("node-up")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> IngestNodeUpEventAsync([FromBody] NodeUpEvent datagram)
        => IngestTypedDatagramAsync(datagram);

    /// <summary>
    /// Ingest a NodeStatus datagram
    /// </summary>
    /// <remarks>
    /// Periodic status report from an active node. Includes uptime, link counts, circuit counts.
    /// 
    /// Example:
    /// ```json
    /// {
    ///   "time": 1234567890,
    ///   "nodeCall": "M0LTE-1",
    ///   "nodeAlias": "MYLTE1",
    ///   "locator": "IO91EC",
    ///   "software": "xrlin",
    ///   "version": "v504j",
    ///   "uptimeSecs": 12345,
    ///   "linksIn": 2,
    ///   "linksOut": 3,
    ///   "cctsIn": 1,
    ///   "cctsOut": 2
    /// }
    /// ```
    /// </remarks>
    [HttpPost("node-status")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> IngestNodeStatusAsync([FromBody] NodeStatusReportEvent datagram)
        => IngestTypedDatagramAsync(datagram);

    /// <summary>
    /// Ingest a NodeDownEvent datagram
    /// </summary>
    /// <remarks>
    /// Reports a node going offline/shutting down.
    /// 
    /// Example:
    /// ```json
    /// {
    ///   "time": 1234567890,
    ///   "nodeCall": "M0LTE-1"
    /// }
    /// ```
    /// </remarks>
    [HttpPost("node-down")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> IngestNodeDownEventAsync([FromBody] NodeDownEvent datagram)
        => IngestTypedDatagramAsync(datagram);

    /// <summary>
    /// Ingest a LinkUpEvent datagram
    /// </summary>
    /// <remarks>
    /// Reports establishment of an AX.25 link between two stations.
    /// 
    /// Example:
    /// ```json
    /// {
    ///   "time": 1234567890,
    ///   "node": "M0LTE-1",
    ///   "id": 123,
    ///   "direction": "outgoing",
    ///   "port": "1",
    ///   "local": "M0LTE-1",
    ///   "remote": "G0ABC-2"
    /// }
    /// ```
    /// </remarks>
    [HttpPost("link-up")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> IngestLinkUpEventAsync([FromBody] LinkUpEvent datagram)
        => IngestTypedDatagramAsync(datagram);

    /// <summary>
    /// Ingest a LinkStatus datagram
    /// </summary>
    /// <remarks>
    /// Periodic status report for an active AX.25 link.
    /// 
    /// Example:
    /// ```json
    /// {
    ///   "time": 1234567890,
    ///   "node": "M0LTE-1",
    ///   "id": 123,
    ///   "port": "1",
    ///   "local": "M0LTE-1",
    ///   "remote": "G0ABC-2",
    ///   "inPkts": 42,
    ///   "outPkts": 38
    /// }
    /// ```
    /// </remarks>
    [HttpPost("link-status")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> IngestLinkStatusAsync([FromBody] LinkStatus datagram)
        => IngestTypedDatagramAsync(datagram);

    /// <summary>
    /// Ingest a LinkDownEvent datagram
    /// </summary>
    /// <remarks>
    /// Reports disconnection of an AX.25 link.
    /// 
    /// Example:
    /// ```json
    /// {
    ///   "time": 1234567890,
    ///   "node": "M0LTE-1",
    ///   "id": 123,
    ///   "port": "1",
    ///   "local": "M0LTE-1",
    ///   "remote": "G0ABC-2"
    /// }
    /// ```
    /// </remarks>
    [HttpPost("link-down")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> IngestLinkDownEventAsync([FromBody] LinkDisconnectionEvent datagram)
        => IngestTypedDatagramAsync(datagram);

    /// <summary>
    /// Ingest a CircuitUpEvent datagram
    /// </summary>
    /// <remarks>
    /// Reports establishment of a NetROM Layer 4 circuit.
    /// 
    /// Example:
    /// ```json
    /// {
    ///   "time": 1234567890,
    ///   "node": "M0LTE-1",
    ///   "id": 456,
    ///   "direction": "outgoing",
    ///   "port": "1",
    ///   "local": "M0LTE-1",
    ///   "remote": "G0ABC"
    /// }
    /// ```
    /// </remarks>
    [HttpPost("circuit-up")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> IngestCircuitUpEventAsync([FromBody] CircuitUpEvent datagram)
        => IngestTypedDatagramAsync(datagram);

    /// <summary>
    /// Ingest a CircuitStatus datagram
    /// </summary>
    /// <remarks>
    /// Periodic status report for an active NetROM circuit.
    /// 
    /// Example:
    /// ```json
    /// {
    ///   "time": 1234567890,
    ///   "node": "M0LTE-1",
    ///   "id": 456,
    ///   "port": "1",
    ///   "local": "M0LTE-1",
    ///   "remote": "G0ABC",
    ///   "inBytes": 1024,
    ///   "outBytes": 2048
    /// }
    /// ```
    /// </remarks>
    [HttpPost("circuit-status")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> IngestCircuitStatusAsync([FromBody] CircuitStatus datagram)
        => IngestTypedDatagramAsync(datagram);

    /// <summary>
    /// Ingest a CircuitDownEvent datagram
    /// </summary>
    /// <remarks>
    /// Reports disconnection of a NetROM circuit.
    /// 
    /// Example:
    /// ```json
    /// {
    ///   "time": 1234567890,
    ///   "node": "M0LTE-1",
    ///   "id": 456,
    ///   "port": "1",
    ///   "local": "M0LTE-1",
    ///   "remote": "G0ABC"
    /// }
    /// ```
    /// </remarks>
    [HttpPost("circuit-down")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> IngestCircuitDownEventAsync([FromBody] CircuitDisconnectionEvent datagram)
        => IngestTypedDatagramAsync(datagram);

    /// <summary>
    /// Ingest an L2Trace datagram
    /// </summary>
    /// <remarks>
    /// Detailed trace of a Layer 2 AX.25 frame. Contains full packet analysis including digipeaters.
    /// 
    /// Example:
    /// ```json
    /// {
    ///   "reportFrom": "M0LTE-1",
    ///   "time": 1234567890,
    ///   "port": "1",
    ///   "dirn": "sent",
    ///   "srce": "M0LTE-1",
    ///   "dest": "G0ABC",
    ///   "ctrl": 3,
    ///   "l2Type": "UI",
    ///   "cr": "C",
    ///   "ilen": 64,
    ///   "pid": 240,
    ///   "ptcl": "DATA",
    ///   "digis": [
    ///     { "call": "DIGI1", "rptd": true }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    [HttpPost("l2trace")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> IngestL2TraceAsync([FromBody] L2Trace datagram)
        => IngestTypedDatagramAsync(datagram);

    /// <summary>
    /// Ingest multiple network event datagrams via HTTP in a single request
    /// </summary>
    /// <param name="datagrams">Array of event datagrams</param>
    /// <returns>202 Accepted with count of queued datagrams</returns>
    /// <remarks>
    /// Accepts an array of datagrams in the same format as single ingestion.
    /// All datagrams are published to RabbitMQ for downstream processing.
    /// 
    /// Example:
    /// ```json
    /// [
    ///   {
    ///     "@type": "NodeUpEvent",
    ///     "nodeCall": "M0LTE-1",
    ///     "nodeAlias": "MYLTE1",
    ///     "locator": "IO91EC",
    ///     "software": "xrlin",
    ///     "version": "v504j"
    ///   },
    ///   {
    ///     "@type": "LinkUpEvent",
    ///     "node": "M0LTE-1",
    ///     "id": 123,
    ///     "direction": "outgoing",
    ///     "port": "1",
    ///     "local": "M0LTE-1",
    ///     "remote": "G0ABC-2"
    ///   }
    /// ]
    /// ```
    /// </remarks>
    [HttpPost("batch")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> IngestDatagramBatchAsync([FromBody] NetworkEventDatagram[] datagrams)
    {
        if (datagrams == null || datagrams.Length == 0)
        {
            return BadRequest(new { error = "Empty batch", message = "At least one datagram is required" });
        }

        try
        {
            if (!_rabbitMqPublisher.IsAvailable)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    error = "Service unavailable",
                    message = "Message queue is not available"
                });
            }

            var arrivalTime = DateTime.UtcNow;
            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            // Get real IP if behind proxy
            if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                var ips = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (ips.Length > 0)
                {
                    sourceIp = ips[0];
                }
            }

            var successCount = 0;
            var failureCount = 0;
            var errors = new List<string>();

            for (int i = 0; i < datagrams.Length; i++)
            {
                try
                {
                    var jsonBytes = JsonSerializer.SerializeToUtf8Bytes<object>(datagrams[i]);
                    await _rabbitMqPublisher.PublishDatagramAsync(jsonBytes, sourceIp);
                    
                    successCount++;
                }
                catch (Exception ex)
                {
                    failureCount++;
                    errors.Add($"Datagram {i} ({datagrams[i].DatagramType}): {ex.Message}");
                    _logger.LogWarning(ex, "Error processing datagram {Index} ({Type}) in batch from {SourceIp}", 
                        i, datagrams[i].DatagramType, sourceIp);
                }
            }

            _logger.LogInformation(
                "HTTP batch ingest from {SourceIp}: {SuccessCount} succeeded, {FailureCount} failed",
                sourceIp, successCount, failureCount);

            var response = new
            {
                status = failureCount == 0 ? "queued" : "partial",
                totalReceived = datagrams.Length,
                successCount = successCount,
                failureCount = failureCount,
                errors = errors,
                sourceIp = sourceIp,
                receivedAt = arrivalTime,
                processingMode = "rabbitmq"
            };

            return failureCount == 0 
                ? Accepted(response) 
                : StatusCode(207, response); // 207 Multi-Status for partial success
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in HTTP batch datagram ingestion");
            return BadRequest(new { error = "Invalid JSON", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting batch datagrams via HTTP");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Check the status of the ingestion service
    /// </summary>
    /// <returns>Service status including RabbitMQ availability</returns>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        var status = new
        {
            service = "datagram-ingest",
            status = "operational",
            rabbitMq = new
            {
                available = _rabbitMqPublisher.IsAvailable,
                mode = "queue-based"
            },
            supportedTypes = new[]
            {
                "NodeUpEvent",
                "NodeStatus", 
                "NodeDownEvent",
                "LinkUpEvent",
                "LinkStatus",
                "LinkDownEvent",
                "CircuitUpEvent",
                "CircuitStatus",
                "CircuitDownEvent",
                "L2Trace"
            },
            endpoints = new
            {
                generic = "/api/ingest",
                batch = "/api/ingest/batch",
                nodeUp = "/api/ingest/node-up",
                nodeStatus = "/api/ingest/node-status",
                nodeDown = "/api/ingest/node-down",
                linkUp = "/api/ingest/link-up",
                linkStatus = "/api/ingest/link-status",
                linkDown = "/api/ingest/link-down",
                circuitUp = "/api/ingest/circuit-up",
                circuitStatus = "/api/ingest/circuit-status",
                circuitDown = "/api/ingest/circuit-down",
                l2trace = "/api/ingest/l2trace",
                status = "/api/ingest/status"
            }
        };

        return Ok(status);
    }

    // ========== Shared Implementation ==========

    private async Task<IActionResult> IngestTypedDatagramAsync(NetworkEventDatagram datagram)
    {
        try
        {
            if (!_rabbitMqPublisher.IsAvailable)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    error = "Service unavailable",
                    message = "Message queue is not available"
                });
            }

            var arrivalTime = DateTime.UtcNow;
            
            // Get the source IP from the HTTP request
            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            // Get real IP if behind proxy (X-Forwarded-For header)
            if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                var ips = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (ips.Length > 0)
                {
                    sourceIp = ips[0];
                }
            }
            
            _logger.LogDebug("HTTP ingest from {SourceIp}: {Type}", 
                sourceIp, datagram.DatagramType);
            
            // Serialize the datagram to JSON bytes (same format as UDP)
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes<object>(datagram);

            await _rabbitMqPublisher.PublishDatagramAsync(jsonBytes, sourceIp);
            _logger.LogDebug("Published HTTP datagram from {SourceIp} to RabbitMQ", sourceIp);
            
            return Accepted(new { 
                status = "queued", 
                message = "Datagram queued for processing via RabbitMQ",
                type = datagram.DatagramType,
                sourceIp = sourceIp,
                receivedAt = arrivalTime,
                processingMode = "rabbitmq"
            });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in HTTP datagram ingestion");
            return BadRequest(new { error = "Invalid JSON", details = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "RabbitMQ unavailable for HTTP datagram ingestion");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Service unavailable",
                message = "Message queue is not available"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting datagram via HTTP");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }
}
