using Microsoft.AspNetCore.Mvc;
using node_api.Services;
using node_api.Models;
using System.Net;
using System.Text.Json;

namespace node_api.Controllers;

/// <summary>
/// HTTP API for ingesting network event datagrams
/// Data submitted here is published to RabbitMQ and processed identically to UDP datagrams
/// </summary>
[ApiController]
[Route("api/ingest")]
public class DatagramIngestController : ControllerBase
{
    private readonly ILogger<DatagramIngestController> _logger;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;

    public DatagramIngestController(
        ILogger<DatagramIngestController> logger,
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
    public async Task<IActionResult> IngestDatagramAsync([FromBody] NetworkEventDatagram datagram)
    {
        try
        {
            if (!_rabbitMqPublisher.IsAvailable)
            {
                _logger.LogWarning("HTTP ingestion rejected - RabbitMQ is not available");
                return StatusCode(503, new { error = "Service unavailable", message = "Message queue is not available" });
            }

            var arrivalTime = DateTime.UtcNow;
            
            // Serialize the datagram to JSON bytes (same format as UDP)
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes<object>(datagram);
            
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
            
            _logger.LogDebug("HTTP ingest from {SourceIp}: {Type}, {Size} bytes", 
                sourceIp, datagram.DatagramType, jsonBytes.Length);
            
            // Publish to RabbitMQ
            await _rabbitMqPublisher.PublishDatagramAsync(jsonBytes, sourceIp);
            _logger.LogDebug("Published HTTP datagram from {SourceIp} to RabbitMQ", sourceIp);
            
            return Accepted(new { 
                status = "queued", 
                message = "Datagram queued for processing via RabbitMQ",
                type = datagram.DatagramType,
                sourceIp = sourceIp,
                receivedAt = arrivalTime
            });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in HTTP datagram ingestion");
            return BadRequest(new { error = "Invalid JSON", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting datagram via HTTP");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Ingest multiple network event datagrams via HTTP in a single request
    /// </summary>
    /// <param name="datagrams">Array of event datagrams</param>
    /// <returns>202 Accepted with count of queued datagrams</returns>
    /// <remarks>
    /// Accepts an array of datagrams in the same format as single ingestion.
    /// All datagrams are published to RabbitMQ for processing.
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

        if (!_rabbitMqPublisher.IsAvailable)
        {
            _logger.LogWarning("HTTP batch ingestion rejected - RabbitMQ is not available");
            return StatusCode(503, new { error = "Service unavailable", message = "Message queue is not available" });
        }

        try
        {
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
                mode = _rabbitMqPublisher.IsAvailable ? "queue-based" : "direct-processing"
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
                singleIngest = "/api/ingest",
                batchIngest = "/api/ingest/batch",
                status = "/api/ingest/status"
            }
        };

        return Ok(status);
    }
}
