using Microsoft.AspNetCore.Mvc;
using node_api.Validators;
using node_api.Constants;
using System.Text.Json;

namespace node_api.Controllers;

/// <summary>
/// Controller for diagnosing and validating events and traces before sending them via UDP
/// </summary>
[ApiController]
[Route("api/diagnostics")]
public class DiagnosticsController : ControllerBase
{
    private readonly DatagramValidationService _validationService;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        DatagramValidationService validationService,
        ILogger<DiagnosticsController> logger)
    {
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// Validates any JSON datagram (event or trace) and returns parsing/validation errors
    /// </summary>
    /// <returns>Validation result with detailed error information</returns>
    [HttpPost("validate")]
    [Consumes("application/json", "text/plain")]
    [Produces("application/json")]
    public async Task<IActionResult> ValidateDatagram()
    {
        string? json = null;
        
        try
        {
            // Read the raw request body to handle any malformed JSON
            using var reader = new StreamReader(Request.Body);
            json = await reader.ReadToEndAsync();
            
            _logger.LogDebug("Received validation request: {Json}", json);

            if (string.IsNullOrWhiteSpace(json))
            {
                return Ok(new ValidationResponse
                {
                    IsValid = false,
                    Stage = "json_parsing",
                    Error = "Request body is empty",
                    ReceivedJson = json
                });
            }

            // Try to deserialize using the same logic as UDP processing
            if (UdpNodeInfoJsonDatagramDeserialiser.TryDeserialise(json, out var datagram, out var jsonException))
            {
                if (datagram == null)
                {
                    return Ok(new ValidationResponse
                    {
                        IsValid = false,
                        Stage = "deserialization",
                        Error = "Deserialization succeeded but returned null datagram",
                        ReceivedJson = json
                    });
                }

                // Validate the deserialized datagram
                var validationResult = _validationService.Validate(datagram);

                if (validationResult.IsValid)
                {
                    return Ok(new ValidationResponse
                    {
                        IsValid = true,
                        Stage = "complete",
                        DatagramType = datagram.DatagramType,
                        Message = "Datagram is valid",
                        ReceivedJson = json
                    });
                }
                else
                {
                    return Ok(new ValidationResponse
                    {
                        IsValid = false,
                        Stage = "validation",
                        DatagramType = datagram.DatagramType,
                        ValidationErrors = validationResult.Errors.Select(e => new ValidationError
                        {
                            PropertyName = e.PropertyName,
                            ErrorMessage = e.ErrorMessage,
                            AttemptedValue = e.AttemptedValue?.ToString()
                        }).ToList(),
                        ReceivedJson = json
                    });
                }
            }
            else
            {
                // Deserialization failed
                if (jsonException != null)
                {
                    return Ok(new ValidationResponse
                    {
                        IsValid = false,
                        Stage = "json_parsing",
                        Error = jsonException.Message,
                        ErrorDetails = new
                        {
                            exceptionType = jsonException.GetType().Name,
                            path = jsonException.Path,
                            lineNumber = jsonException.LineNumber,
                            bytePositionInLine = jsonException.BytePositionInLine
                        },
                        ReceivedJson = json
                    });
                }
                else
                {
                    // Unknown datagram type
                    string? typeString = null;
                    try
                    {
                        var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("@type", out var typeElement))
                        {
                            typeString = typeElement.GetString();
                        }
                    }
                    catch { }

                    return Ok(new ValidationResponse
                    {
                        IsValid = false,
                        Stage = "type_recognition",
                        Error = typeString != null 
                            ? $"Unknown datagram type: {typeString}" 
                            : "Missing or invalid @type property",
                        DatagramType = typeString,
                        ReceivedJson = json,
                        SupportedTypes = DatagramTypes.All
                    });
                }
            }
        }
        catch (JsonException ex)
        {
            // Catch any JSON parsing errors that weren't caught by TryDeserialise
            return Ok(new ValidationResponse
            {
                IsValid = false,
                Stage = "json_parsing",
                Error = ex.Message,
                ErrorDetails = new
                {
                    exceptionType = ex.GetType().Name,
                    path = ex.Path,
                    lineNumber = ex.LineNumber,
                    bytePositionInLine = ex.BytePositionInLine
                },
                ReceivedJson = json
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during validation");
            return Ok(new ValidationResponse
            {
                IsValid = false,
                Stage = "unexpected_error",
                Error = ex.Message,
                ErrorDetails = new
                {
                    exceptionType = ex.GetType().Name,
                    stackTrace = ex.StackTrace
                },
                ReceivedJson = json
            });
        }
    }

    /// <summary>
    /// Response model for validation results
    /// </summary>
    public class ValidationResponse
    {
        /// <summary>
        /// Whether the datagram is valid and ready for transmission
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// The stage at which validation stopped (json_parsing, type_recognition, deserialization, validation, complete)
        /// </summary>
        public required string Stage { get; set; }

        /// <summary>
        /// The type of datagram (@type field value)
        /// </summary>
        public string? DatagramType { get; set; }

        /// <summary>
        /// Success message if valid
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Additional error details (exception information, etc.)
        /// </summary>
        public object? ErrorDetails { get; set; }

        /// <summary>
        /// List of validation errors from FluentValidation
        /// </summary>
        public List<ValidationError>? ValidationErrors { get; set; }

        /// <summary>
        /// List of supported datagram types (shown when type is not recognized)
        /// </summary>
        public string[]? SupportedTypes { get; set; }

        /// <summary>
        /// The raw JSON that was received (for debugging)
        /// </summary>
        public string? ReceivedJson { get; set; }
    }

    /// <summary>
    /// Individual validation error
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// The property that failed validation
        /// </summary>
        public required string PropertyName { get; set; }

        /// <summary>
        /// Description of the validation error
        /// </summary>
        public required string ErrorMessage { get; set; }

        /// <summary>
        /// The value that was attempted (if applicable)
        /// </summary>
        public string? AttemptedValue { get; set; }
    }
}
