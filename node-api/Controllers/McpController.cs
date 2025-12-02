using Microsoft.AspNetCore.Mvc;
using node_api.Models.NetworkState;
using node_api.Services;
using System.Text.Json.Serialization;

namespace node_api.Controllers;

/// <summary>
/// MCP (Model Context Protocol) server endpoints
/// Implements the MCP HTTP protocol for exposing network state as tools
/// </summary>
[ApiController]
[Route("mcp")]
public class McpController : ControllerBase
{
    private readonly INetworkStateService _networkState;
    private readonly ILogger<McpController> _logger;

    public McpController(
        INetworkStateService networkState,
        ILogger<McpController> logger)
    {
        _networkState = networkState;
        _logger = logger;
    }

    /// <summary>
    /// MCP server info and capabilities
    /// GET /mcp
    /// </summary>
    [HttpGet]
    public IActionResult GetServerInfo()
    {
        return Ok(new
        {
            name = "node-api-mcp-server",
            version = "1.0.0",
            description = "Packet Radio Network State MCP Server",
            protocol = new
            {
                version = "2024-11-05",
                capabilities = new
                {
                    tools = true,
                    resources = false,
                    prompts = false
                }
            }
        });
    }

    /// <summary>
    /// MCP JSON-RPC endpoint for streaming HTTP transport
    /// POST /mcp
    /// </summary>
    [HttpPost]
    public IActionResult HandleMcpJsonRpc([FromBody] System.Text.Json.JsonElement request)
    {
        try
        {
            // Extract method from JSON-RPC request
            if (!request.TryGetProperty("method", out var methodElement))
            {
                return BadRequest(new
                {
                    jsonrpc = "2.0",
                    error = new
                    {
                        code = -32600,
                        message = "Invalid Request: missing method"
                    },
                    id = request.TryGetProperty("id", out var idProp) ? (object?)idProp : null
                });
            }

            var method = methodElement.GetString();
            
            // Check if this is a notification (no id field means notification)
            var hasId = request.TryGetProperty("id", out var requestId);
            
            _logger.LogInformation("Handling MCP JSON-RPC {Type}: {Method}", hasId ? "method" : "notification", method);

            // Handle notifications (no response needed)
            if (!hasId)
            {
                HandleNotification(method);
                return Ok(); // Notifications don't return anything
            }

            // Handle regular methods (require response)
            var id = (object?)requestId;

            object? result = method switch
            {
                "initialize" => new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new
                    {
                        tools = new { }
                    },
                    serverInfo = new
                    {
                        name = "node-api-mcp-server",
                        version = "1.0.0"
                    }
                },
                "tools/list" => new
                {
                    tools = GetToolsArray()
                },
                "tools/call" => HandleToolCall(request),
                _ => throw new McpException($"Unknown method: {method}", 404)
            };

            return Ok(new
            {
                jsonrpc = "2.0",
                id,
                result
            });
        }
        catch (McpException ex)
        {
            _logger.LogWarning("MCP JSON-RPC error: {Message}", ex.Message);
            return StatusCode(ex.StatusCode, new
            {
                jsonrpc = "2.0",
                error = new
                {
                    code = ex.StatusCode == 404 ? -32601 : -32603,
                    message = ex.Message
                },
                id = request.TryGetProperty("id", out var idProp) ? (object?)idProp : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP JSON-RPC request");
            return StatusCode(500, new
            {
                jsonrpc = "2.0",
                error = new
                {
                    code = -32603,
                    message = "Internal error"
                },
                id = request.TryGetProperty("id", out var idProp) ? (object?)idProp : null
            });
        }
    }

    private void HandleNotification(string? method)
    {
        // MCP notifications - these don't require responses
        switch (method)
        {
            case "notifications/initialized":
                _logger.LogInformation("MCP client initialized");
                break;
            case "notifications/cancelled":
                _logger.LogInformation("MCP client cancelled operation");
                break;
            default:
                _logger.LogWarning("Unknown MCP notification: {Method}", method);
                break;
        }
    }

    private object[] GetToolsArray()
    {
        return new object[]
        {
            new
            {
                name = "get_all_links",
                description = "Get all current packet radio network links with their status, including connection state, endpoints, and performance metrics",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        includeDisconnected = new
                        {
                            type = "boolean",
                            description = "Include disconnected links in the results (default: false)"
                        }
                    }
                }
            },
            new
            {
                name = "get_links_for_callsign",
                description = "Get all links involving a specific callsign or base callsign (e.g., M0LTE will return M0LTE, M0LTE-1, M0LTE-2, etc.)",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        callsign = new
                        {
                            type = "string",
                            description = "The callsign to search for (base or full with SSID)"
                        },
                        includeDisconnected = new
                        {
                            type = "boolean",
                            description = "Include disconnected links in the results (default: false)"
                        }
                    },
                    required = new[] { "callsign" }
                }
            },
            new
            {
                name = "get_all_nodes",
                description = "Get all known packet radio nodes with their status, location, software version, and activity information",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        includeOffline = new
                        {
                            type = "boolean",
                            description = "Include offline nodes in the results (default: true)"
                        }
                    }
                }
            },
            new
            {
                name = "get_node_details",
                description = "Get detailed information about a specific node including all its links and circuits",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        callsign = new
                        {
                            type = "string",
                            description = "The exact callsign to get details for"
                        }
                    },
                    required = new[] { "callsign" }
                }
            }
        };
    }

    private object HandleToolCall(System.Text.Json.JsonElement request)
    {
        if (!request.TryGetProperty("params", out var paramsElement))
        {
            throw new McpException("Missing params", 400);
        }

        if (!paramsElement.TryGetProperty("name", out var nameElement))
        {
            throw new McpException("Missing tool name in params", 400);
        }

        var toolName = nameElement.GetString();
        if (string.IsNullOrEmpty(toolName))
        {
            throw new McpException("Tool name cannot be empty", 400);
        }

        // Extract arguments
        Dictionary<string, object>? arguments = null;
        if (paramsElement.TryGetProperty("arguments", out var argsElement))
        {
            arguments = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(argsElement.GetRawText());
        }

        var toolResult = toolName switch
        {
            "get_all_links" => GetAllLinks(arguments),
            "get_links_for_callsign" => GetLinksForCallsign(arguments),
            "get_all_nodes" => GetAllNodes(arguments),
            "get_node_details" => GetNodeDetails(arguments),
            _ => throw new McpException($"Unknown tool: {toolName}", 404)
        };

        // Return in MCP format with content array
        return new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = System.Text.Json.JsonSerializer.Serialize(toolResult, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
                }
            }
        };
    }

    /// <summary>
    /// List available MCP tools
    /// GET /mcp/tools
    /// </summary>
    [HttpGet("tools")]
    public IActionResult ListTools()
    {
        var tools = new[]
        {
            new McpTool
            {
                Name = "get_all_links",
                Description = "Get all current packet radio network links with their status, including connection state, endpoints, and performance metrics",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        includeDisconnected = new
                        {
                            type = "boolean",
                            description = "Include disconnected links in the results (default: false)"
                        }
                    }
                }
            },
            new McpTool
            {
                Name = "get_links_for_callsign",
                Description = "Get all links involving a specific callsign or base callsign (e.g., M0LTE will return M0LTE, M0LTE-1, M0LTE-2, etc.)",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        callsign = new
                        {
                            type = "string",
                            description = "The callsign to search for (base or full with SSID)"
                        },
                        includeDisconnected = new
                        {
                            type = "boolean",
                            description = "Include disconnected links in the results (default: false)"
                        }
                    },
                    required = new[] { "callsign" }
                }
            },
            new McpTool
            {
                Name = "get_all_nodes",
                Description = "Get all known packet radio nodes with their status, location, software version, and activity information",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        includeOffline = new
                        {
                            type = "boolean",
                            description = "Include offline nodes in the results (default: true)"
                        }
                    }
                }
            },
            new McpTool
            {
                Name = "get_node_details",
                Description = "Get detailed information about a specific node including all its links and circuits",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        callsign = new
                        {
                            type = "string",
                            description = "The exact callsign to get details for"
                        }
                    },
                    required = new[] { "callsign" }
                }
            }
        };

        return Ok(new { tools });
    }

    /// <summary>
    /// Execute an MCP tool
    /// POST /mcp/tools/{toolName}
    /// </summary>
    [HttpPost("tools/{toolName}")]
    public IActionResult ExecuteTool(string toolName, [FromBody] McpToolRequest request)
    {
        _logger.LogInformation("Executing MCP tool: {ToolName}", toolName);

        try
        {
            var result = toolName switch
            {
                "get_all_links" => GetAllLinks(request.Arguments),
                "get_links_for_callsign" => GetLinksForCallsign(request.Arguments),
                "get_all_nodes" => GetAllNodes(request.Arguments),
                "get_node_details" => GetNodeDetails(request.Arguments),
                _ => throw new McpException($"Unknown tool: {toolName}", 404)
            };

            return Ok(result);
        }
        catch (McpException ex)
        {
            _logger.LogWarning("MCP tool error: {Message}", ex.Message);
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing MCP tool: {ToolName}", toolName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private object GetAllLinks(Dictionary<string, object>? arguments)
    {
        var includeDisconnected = false;
        
        if (arguments?.TryGetValue("includeDisconnected", out var value) == true && value is bool boolValue)
        {
            includeDisconnected = boolValue;
        }

        var links = _networkState.GetAllLinks()
            .Values
            .Where(l => !_networkState.IsTestCallsign(l.Endpoint1) && 
                       !_networkState.IsTestCallsign(l.Endpoint2) &&
                       !_networkState.IsHiddenCallsign(l.Endpoint1) &&
                       !_networkState.IsHiddenCallsign(l.Endpoint2))
            .Where(l => includeDisconnected || l.Status == LinkStatus.Active)
            .Select(FormatLink)
            .ToList();

        return new
        {
            totalLinks = links.Count,
            links
        };
    }

    private object GetLinksForCallsign(Dictionary<string, object>? arguments)
    {
        if (arguments?.TryGetValue("callsign", out var callsignObj) != true || callsignObj is not string callsign)
        {
            throw new McpException("Missing required argument: callsign", 400);
        }

        if (string.IsNullOrWhiteSpace(callsign))
        {
            throw new McpException("Callsign cannot be empty", 400);
        }

        var includeDisconnected = false;
        if (arguments.TryGetValue("includeDisconnected", out var value) && value is bool boolValue)
        {
            includeDisconnected = boolValue;
        }

        // Get all SSIDs for this base callsign
        var ssids = _networkState.GetNodesByBaseCallsign(callsign)
            .Select(n => n.Callsign)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!ssids.Any())
        {
            // Try exact match
            if (_networkState.GetNode(callsign) != null)
            {
                ssids.Add(callsign);
            }
        }

        var links = _networkState.GetAllLinks()
            .Values
            .Where(l => ssids.Contains(l.Endpoint1) || ssids.Contains(l.Endpoint2))
            .Where(l => !_networkState.IsTestCallsign(l.Endpoint1) && 
                       !_networkState.IsTestCallsign(l.Endpoint2) &&
                       !_networkState.IsHiddenCallsign(l.Endpoint1) &&
                       !_networkState.IsHiddenCallsign(l.Endpoint2))
            .Where(l => includeDisconnected || l.Status == LinkStatus.Active)
            .Select(FormatLink)
            .ToList();

        return new
        {
            callsign,
            matchedCallsigns = ssids.ToList(),
            totalLinks = links.Count,
            links
        };
    }

    private object GetAllNodes(Dictionary<string, object>? arguments)
    {
        var includeOffline = true;
        
        if (arguments?.TryGetValue("includeOffline", out var value) == true && value is bool boolValue)
        {
            includeOffline = boolValue;
        }

        var nodes = _networkState.GetAllNodes()
            .Values
            .Where(n => !_networkState.IsTestCallsign(n.Callsign) && 
                       !_networkState.IsHiddenCallsign(n.Callsign))
            .Where(n => includeOffline || n.Status == NodeStatus.Online)
            .Select(FormatNode)
            .ToList();

        return new
        {
            totalNodes = nodes.Count,
            nodes
        };
    }

    private object GetNodeDetails(Dictionary<string, object>? arguments)
    {
        if (arguments?.TryGetValue("callsign", out var callsignObj) != true || callsignObj is not string callsign)
        {
            throw new McpException("Missing required argument: callsign", 400);
        }

        if (string.IsNullOrWhiteSpace(callsign))
        {
            throw new McpException("Callsign cannot be empty", 400);
        }

        var node = _networkState.GetNode(callsign);
        
        if (node == null)
        {
            throw new McpException($"Node not found: {callsign}", 404);
        }

        var links = _networkState.GetLinksForNode(callsign)
            .Select(FormatLink)
            .ToList();

        var circuits = _networkState.GetCircuitsForNode(callsign)
            .Select(FormatCircuit)
            .ToList();

        return new
        {
            node = FormatNode(node),
            links,
            circuits
        };
    }

    private static object FormatLink(LinkState link)
    {
        return new
        {
            endpoint1 = link.Endpoint1,
            endpoint2 = link.Endpoint2,
            status = link.Status.ToString(),
            connectedAt = link.ConnectedAt,
            disconnectedAt = link.DisconnectedAt,
            lastUpdate = link.LastUpdate,
            initiator = link.Initiator,
            isRF = link.IsRF,
            isFlapping = link.IsFlapping(),
            flapCount = link.FlapCount > 0 ? (int?)link.FlapCount : null,
            endpoints = link.Endpoints.Values.Select(e => new
            {
                node = e.Node,
                direction = e.Direction,
                port = e.Port,
                upForSecs = e.UpForSecs,
                framesSent = e.FramesSent,
                framesReceived = e.FramesReceived,
                framesResent = e.FramesResent,
                bytesReceived = e.BytesReceived,
                bytesSent = e.BytesSent,
                l2RttMs = e.L2RttMs
            }).ToList()
        };
    }

    private static object FormatNode(NodeState node)
    {
        return new
        {
            callsign = node.Callsign,
            alias = node.Alias,
            status = node.Status.ToString(),
            locator = node.Locator,
            latitude = node.Latitude,
            longitude = node.Longitude,
            software = node.Software,
            version = node.Version,
            uptimeSecs = node.UptimeSecs,
            firstSeen = node.FirstSeen,
            lastSeen = node.LastSeen,
            isReportingNode = node.IsReportingNode,
            linksIn = node.LinksIn,
            linksOut = node.LinksOut,
            circuitsIn = node.CircuitsIn,
            circuitsOut = node.CircuitsOut,
            l3Relayed = node.L3Relayed,
            ipAddressObfuscated = node.IpAddressObfuscated,
            geoIpCountryCode = node.GeoIpCountryCode,
            geoIpCountryName = node.GeoIpCountryName,
            geoIpCity = node.GeoIpCity
        };
    }

    private static object FormatCircuit(CircuitState circuit)
    {
        return new
        {
            endpoint1 = circuit.Endpoint1,
            endpoint2 = circuit.Endpoint2,
            status = circuit.Status.ToString(),
            connectedAt = circuit.ConnectedAt,
            disconnectedAt = circuit.DisconnectedAt,
            lastUpdate = circuit.LastUpdate,
            initiator = circuit.Initiator,
            endpoints = circuit.Endpoints.Values.Select(e => new
            {
                node = e.Node,
                direction = e.Direction,
                service = e.Service,
                segmentsSent = e.SegmentsSent,
                segmentsReceived = e.SegmentsReceived,
                segmentsResent = e.SegmentsResent,
                bytesReceived = e.BytesReceived,
                bytesSent = e.BytesSent
            }).ToList()
        };
    }

    #region Models

    public class McpTool
    {
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("description")]
        public required string Description { get; set; }

        [JsonPropertyName("inputSchema")]
        public required object InputSchema { get; set; }
    }

    public class McpToolRequest
    {
        [JsonPropertyName("arguments")]
        public Dictionary<string, object>? Arguments { get; set; }
    }

    public class McpException : Exception
    {
        public int StatusCode { get; }

        public McpException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }

    #endregion
}
