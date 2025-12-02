using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using node_api.Controllers;
using node_api.Models.NetworkState;
using node_api.Services;
using System.Text.Json;
using Xunit;

namespace Tests;

public class McpControllerTests
{
    private readonly INetworkStateService _networkState;
    private readonly ILogger<McpController> _logger;
    private readonly McpController _controller;

    public McpControllerTests()
    {
        _logger = Substitute.For<ILogger<McpController>>();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        
        var networkStateLogger = Substitute.For<ILogger<NetworkStateService>>();
        _networkState = new NetworkStateService(networkStateLogger, configuration);
        
        _controller = new McpController(_networkState, _logger);
    }

    #region Server Info Tests

    [Fact]
    public void GetServerInfo_ReturnsServerInformation()
    {
        // Act
        var result = _controller.GetServerInfo() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        
        var value = result.Value;
        Assert.NotNull(value);
        
        // Verify response structure using reflection
        var nameProperty = value.GetType().GetProperty("name");
        Assert.NotNull(nameProperty);
        Assert.Equal("node-api-mcp-server", nameProperty.GetValue(value));
    }

    [Fact]
    public void GetServerInfo_ReturnsProtocolVersion()
    {
        // Act
        var result = _controller.GetServerInfo() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        Assert.NotNull(value);
        
        var protocolProperty = value.GetType().GetProperty("protocol");
        Assert.NotNull(protocolProperty);
        var protocol = protocolProperty.GetValue(value);
        Assert.NotNull(protocol);
        
        var versionProperty = protocol.GetType().GetProperty("version");
        Assert.NotNull(versionProperty);
        Assert.Equal("2024-11-05", versionProperty.GetValue(protocol));
    }

    #endregion

    #region JSON-RPC POST Tests

    [Fact]
    public void HandleMcpJsonRpc_Initialize_ReturnsServerInfo()
    {
        // Arrange
        var requestJson = """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}""";
        var request = JsonDocument.Parse(requestJson).RootElement;

        // Act
        var result = _controller.HandleMcpJsonRpc(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        Assert.NotNull(value);
        
        var jsonrpcProperty = value.GetType().GetProperty("jsonrpc");
        Assert.NotNull(jsonrpcProperty);
        Assert.Equal("2.0", jsonrpcProperty.GetValue(value));
        
        var resultProperty = value.GetType().GetProperty("result");
        Assert.NotNull(resultProperty);
        var resultValue = resultProperty.GetValue(value);
        Assert.NotNull(resultValue);
    }

    [Fact]
    public void HandleMcpJsonRpc_ToolsList_ReturnsAllTools()
    {
        // Arrange
        var requestJson = """{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}""";
        var request = JsonDocument.Parse(requestJson).RootElement;

        // Act
        var result = _controller.HandleMcpJsonRpc(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        Assert.NotNull(value);
        
        var resultProperty = value.GetType().GetProperty("result");
        Assert.NotNull(resultProperty);
        var resultValue = resultProperty.GetValue(value);
        Assert.NotNull(resultValue);
        
        var toolsProperty = resultValue.GetType().GetProperty("tools");
        Assert.NotNull(toolsProperty);
        var tools = toolsProperty.GetValue(resultValue) as Array;
        Assert.NotNull(tools);
        Assert.Equal(4, tools.Length);
    }

    [Fact]
    public void HandleMcpJsonRpc_ToolsCall_GetAllNodes_ReturnsNodes()
    {
        // Arrange
        _networkState.GetOrCreateNode("M0LTE");
        _networkState.GetOrCreateNode("G8PZT");
        
        var requestJson = """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"get_all_nodes","arguments":{}}}""";
        var request = JsonDocument.Parse(requestJson).RootElement;

        // Act
        var result = _controller.HandleMcpJsonRpc(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        Assert.NotNull(value);
        
        var resultProperty = value.GetType().GetProperty("result");
        Assert.NotNull(resultProperty);
        var resultValue = resultProperty.GetValue(value);
        Assert.NotNull(resultValue);
        
        // Should have content array with text
        var contentProperty = resultValue.GetType().GetProperty("content");
        Assert.NotNull(contentProperty);
        var content = contentProperty.GetValue(resultValue) as Array;
        Assert.NotNull(content);
        Assert.NotEmpty(content);
    }

    [Fact]
    public void HandleMcpJsonRpc_ToolsCall_GetLinksForCallsign_ReturnsResponse()
    {
        // Arrange
        _networkState.GetOrCreateNode("M0LTE");
        _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        
        var requestJson = """{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"get_links_for_callsign","arguments":{"callsign":"M0LTE"}}}""";
        var request = JsonDocument.Parse(requestJson).RootElement;

        // Act
        var result = _controller.HandleMcpJsonRpc(request);

        // Assert - Just verify it returns something and doesn't throw
        Assert.NotNull(result);
    }

    [Fact]
    public void HandleMcpJsonRpc_MissingMethod_ReturnsBadRequest()
    {
        // Arrange
        var requestJson = """{"jsonrpc":"2.0","id":5}""";
        var request = JsonDocument.Parse(requestJson).RootElement;

        // Act
        var result = _controller.HandleMcpJsonRpc(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public void HandleMcpJsonRpc_UnknownMethod_ReturnsError()
    {
        // Arrange
        var requestJson = """{"jsonrpc":"2.0","id":6,"method":"unknown_method","params":{}}""";
        var request = JsonDocument.Parse(requestJson).RootElement;

        // Act
        var result = _controller.HandleMcpJsonRpc(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
        
        var value = objectResult.Value;
        Assert.NotNull(value);
        
        var errorProperty = value.GetType().GetProperty("error");
        Assert.NotNull(errorProperty);
    }

    [Fact]
    public void HandleMcpJsonRpc_ToolsCall_MissingToolName_ReturnsError()
    {
        // Arrange
        var requestJson = """{"jsonrpc":"2.0","id":7,"method":"tools/call","params":{}}""";
        var request = JsonDocument.Parse(requestJson).RootElement;

        // Act
        var result = _controller.HandleMcpJsonRpc(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public void HandleMcpJsonRpc_ToolsCall_UnknownTool_ReturnsError()
    {
        // Arrange
        var requestJson = """{"jsonrpc":"2.0","id":8,"method":"tools/call","params":{"name":"unknown_tool","arguments":{}}}""";
        var request = JsonDocument.Parse(requestJson).RootElement;

        // Act
        var result = _controller.HandleMcpJsonRpc(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    [Fact]
    public void HandleMcpJsonRpc_ToolsCall_GetNodeDetails_ReturnsResponse()
    {
        // Arrange
        var node = _networkState.GetOrCreateNode("M0LTE");
        node.Alias = "TEST";
        _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        
        var requestJson = """{"jsonrpc":"2.0","id":9,"method":"tools/call","params":{"name":"get_node_details","arguments":{"callsign":"M0LTE"}}}""";
        var request = JsonDocument.Parse(requestJson).RootElement;

        // Act
        var result = _controller.HandleMcpJsonRpc(request);

        // Assert - Just verify it returns something and doesn't throw
        Assert.NotNull(result);
    }

    [Fact]
    public void HandleMcpJsonRpc_ToolsCall_GetAllLinks_ExcludesTestCallsigns()
    {
        // Arrange
        _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        _networkState.GetOrCreateLink("TEST", "M0ABC");
        
        var requestJson = """{"jsonrpc":"2.0","id":10,"method":"tools/call","params":{"name":"get_all_links","arguments":{}}}""";
        var request = JsonDocument.Parse(requestJson).RootElement;

        // Act
        var result = _controller.HandleMcpJsonRpc(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        
        // Extract the result and parse the JSON text to verify only 1 link
        var value = result.Value;
        var resultProperty = value!.GetType().GetProperty("result");
        var resultValue = resultProperty!.GetValue(value);
        var contentProperty = resultValue!.GetType().GetProperty("content");
        var content = contentProperty!.GetValue(resultValue) as Array;
        Assert.NotNull(content);
        Assert.NotEmpty(content);
        
        var firstContent = content.GetValue(0);
        var textProperty = firstContent!.GetType().GetProperty("text");
        var textValue = textProperty!.GetValue(firstContent) as string;
        Assert.NotNull(textValue);
        Assert.Contains("\"totalLinks\": 1", textValue);
    }

    #endregion

    #region List Tools Tests

    [Fact]
    public void ListTools_ReturnsAllTools()
    {
        // Act
        var result = _controller.ListTools() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        
        var value = result.Value;
        Assert.NotNull(value);
        
        var toolsProperty = value.GetType().GetProperty("tools");
        Assert.NotNull(toolsProperty);
        var tools = toolsProperty.GetValue(value) as Array;
        Assert.NotNull(tools);
        Assert.Equal(4, tools.Length);
    }

    [Fact]
    public void ListTools_ContainsGetAllLinksTool()
    {
        // Act
        var result = _controller.ListTools() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        var toolsProperty = value!.GetType().GetProperty("tools");
        Assert.NotNull(toolsProperty);
        var toolsValue = toolsProperty.GetValue(value);
        
        // Convert to enumerable and check
        Assert.NotNull(toolsValue);
        var toolsEnumerable = toolsValue as System.Collections.IEnumerable;
        Assert.NotNull(toolsEnumerable);
        
        var toolNames = new List<string>();
        foreach (var tool in toolsEnumerable)
        {
            if (tool != null)
            {
                var nameProperty = tool.GetType().GetProperty("Name");
                if (nameProperty != null)
                {
                    var name = nameProperty.GetValue(tool)?.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        toolNames.Add(name);
                    }
                }
            }
        }
        
        Assert.Contains("get_all_links", toolNames);
    }

    [Fact]
    public void ListTools_ContainsAllRequiredTools()
    {
        // Act
        var result = _controller.ListTools() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        var toolsProperty = value!.GetType().GetProperty("tools");
        Assert.NotNull(toolsProperty);
        var toolsValue = toolsProperty.GetValue(value);
        
        // Convert to enumerable and check
        Assert.NotNull(toolsValue);
        var toolsEnumerable = toolsValue as System.Collections.IEnumerable;
        Assert.NotNull(toolsEnumerable);
        
        var toolNames = new List<string>();
        foreach (var tool in toolsEnumerable)
        {
            if (tool != null)
            {
                var nameProperty = tool.GetType().GetProperty("Name");
                if (nameProperty != null)
                {
                    var name = nameProperty.GetValue(tool)?.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        toolNames.Add(name);
                    }
                }
            }
        }
        
        Assert.Contains("get_all_links", toolNames);
        Assert.Contains("get_links_for_callsign", toolNames);
        Assert.Contains("get_all_nodes", toolNames);
        Assert.Contains("get_node_details", toolNames);
    }

    #endregion

    #region Get All Links Tests

    [Fact]
    public void ExecuteTool_GetAllLinks_ReturnsAllLinks()
    {
        // Arrange
        _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        _networkState.GetOrCreateLink("M0ABC", "G8XYZ");
        
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>()
        };

        // Act
        var result = _controller.ExecuteTool("get_all_links", request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        Assert.NotNull(value);
        
        var totalLinksProperty = value.GetType().GetProperty("totalLinks");
        Assert.NotNull(totalLinksProperty);
        Assert.Equal(2, totalLinksProperty.GetValue(value));
    }

    [Fact]
    public void ExecuteTool_GetAllLinks_ExcludesTestCallsigns()
    {
        // Arrange
        _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        _networkState.GetOrCreateLink("TEST", "M0ABC");
        _networkState.GetOrCreateLink("M0XYZ", "TEST-1");
        
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>()
        };

        // Act
        var result = _controller.ExecuteTool("get_all_links", request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        var totalLinksProperty = value!.GetType().GetProperty("totalLinks");
        Assert.NotNull(totalLinksProperty);
        Assert.Equal(1, totalLinksProperty.GetValue(value));
    }

    [Fact]
    public void ExecuteTool_GetAllLinks_WithIncludeDisconnectedFalse_ExcludesDisconnectedLinks()
    {
        // Arrange
        var link1 = _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        link1.Status = LinkStatus.Active;
        
        var link2 = _networkState.GetOrCreateLink("M0ABC", "G8XYZ");
        link2.Status = LinkStatus.Disconnected;
        
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>
            {
                { "includeDisconnected", false }
            }
        };

        // Act
        var result = _controller.ExecuteTool("get_all_links", request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        var totalLinksProperty = value!.GetType().GetProperty("totalLinks");
        Assert.NotNull(totalLinksProperty);
        Assert.Equal(1, totalLinksProperty.GetValue(value));
    }

    [Fact]
    public void ExecuteTool_GetAllLinks_WithIncludeDisconnectedTrue_IncludesDisconnectedLinks()
    {
        // Arrange
        var link1 = _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        link1.Status = LinkStatus.Active;
        
        var link2 = _networkState.GetOrCreateLink("M0ABC", "G8XYZ");
        link2.Status = LinkStatus.Disconnected;
        
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>
            {
                { "includeDisconnected", true }
            }
        };

        // Act
        var result = _controller.ExecuteTool("get_all_links", request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        var totalLinksProperty = value!.GetType().GetProperty("totalLinks");
        Assert.NotNull(totalLinksProperty);
        Assert.Equal(2, totalLinksProperty.GetValue(value));
    }

    #endregion

    #region Get Links For Callsign Tests

    [Fact]
    public void ExecuteTool_GetLinksForCallsign_ReturnsLinksForCallsign()
    {
        // Arrange
        _networkState.GetOrCreateNode("M0LTE");
        _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        _networkState.GetOrCreateLink("M0LTE", "M0ABC");
        _networkState.GetOrCreateLink("G8XYZ", "M0DEF");
        
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>
            {
                { "callsign", "M0LTE" }
            }
        };

        // Act
        var result = _controller.ExecuteTool("get_links_for_callsign", request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        var totalLinksProperty = value!.GetType().GetProperty("totalLinks");
        Assert.NotNull(totalLinksProperty);
        Assert.Equal(2, totalLinksProperty.GetValue(value));
    }

    [Fact]
    public void ExecuteTool_GetLinksForCallsign_HandlesSSIDs()
    {
        // Arrange
        _networkState.GetOrCreateNode("M0LTE");
        _networkState.GetOrCreateNode("M0LTE-1");
        _networkState.GetOrCreateNode("M0LTE-2");
        
        _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        _networkState.GetOrCreateLink("M0LTE-1", "M0ABC");
        _networkState.GetOrCreateLink("M0LTE-2", "M0DEF");
        
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>
            {
                { "callsign", "M0LTE" }
            }
        };

        // Act
        var result = _controller.ExecuteTool("get_links_for_callsign", request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        var totalLinksProperty = value!.GetType().GetProperty("totalLinks");
        Assert.NotNull(totalLinksProperty);
        Assert.Equal(3, totalLinksProperty.GetValue(value));
    }

    [Fact]
    public void ExecuteTool_GetLinksForCallsign_IsCaseInsensitive()
    {
        // Arrange
        _networkState.GetOrCreateNode("M0LTE");
        _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>
            {
                { "callsign", "m0lte" }
            }
        };

        // Act
        var result = _controller.ExecuteTool("get_links_for_callsign", request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        var totalLinksProperty = value!.GetType().GetProperty("totalLinks");
        Assert.NotNull(totalLinksProperty);
        Assert.Equal(1, totalLinksProperty.GetValue(value));
    }

    [Fact]
    public void ExecuteTool_GetLinksForCallsign_WithoutCallsign_ReturnsError()
    {
        // Arrange
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>()
        };

        // Act
        var result = _controller.ExecuteTool("get_links_for_callsign", request);

        // Assert
        Assert.IsType<ObjectResult>(result);
        var objectResult = result as ObjectResult;
        Assert.NotNull(objectResult);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public void ExecuteTool_GetLinksForCallsign_WithEmptyCallsign_ReturnsError()
    {
        // Arrange
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>
            {
                { "callsign", "" }
            }
        };

        // Act
        var result = _controller.ExecuteTool("get_links_for_callsign", request);

        // Assert
        Assert.IsType<ObjectResult>(result);
        var objectResult = result as ObjectResult;
        Assert.NotNull(objectResult);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public void ExecuteTool_GetLinksForCallsign_ExcludesTestCallsigns()
    {
        // Arrange
        _networkState.GetOrCreateNode("M0LTE");
        _networkState.GetOrCreateLink("M0LTE", "TEST");
        _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>
            {
                { "callsign", "M0LTE" }
            }
        };

        // Act
        var result = _controller.ExecuteTool("get_links_for_callsign", request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        var totalLinksProperty = value!.GetType().GetProperty("totalLinks");
        Assert.NotNull(totalLinksProperty);
        Assert.Equal(1, totalLinksProperty.GetValue(value));
    }

    #endregion

    #region Get All Nodes Tests

    [Fact]
    public void ExecuteTool_GetAllNodes_ReturnsAllNodes()
    {
        // Arrange
        _networkState.GetOrCreateNode("M0LTE");
        _networkState.GetOrCreateNode("G8PZT");
        _networkState.GetOrCreateNode("M0ABC");
        
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>()
        };

        // Act
        var result = _controller.ExecuteTool("get_all_nodes", request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        var totalNodesProperty = value!.GetType().GetProperty("totalNodes");
        Assert.NotNull(totalNodesProperty);
        Assert.Equal(3, totalNodesProperty.GetValue(value));
    }

    [Fact]
    public void ExecuteTool_GetAllNodes_ExcludesTestCallsigns()
    {
        // Arrange
        _networkState.GetOrCreateNode("M0LTE");
        _networkState.GetOrCreateNode("TEST");
        _networkState.GetOrCreateNode("TEST-1");
        
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>()
        };

        // Act
        var result = _controller.ExecuteTool("get_all_nodes", request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        var totalNodesProperty = value!.GetType().GetProperty("totalNodes");
        Assert.NotNull(totalNodesProperty);
        Assert.Equal(1, totalNodesProperty.GetValue(value));
    }

    [Fact]
    public void ExecuteTool_GetAllNodes_WithIncludeOfflineFalse_ExcludesOfflineNodes()
    {
        // Arrange
        var node1 = _networkState.GetOrCreateNode("M0LTE");
        node1.Status = NodeStatus.Online;
        
        var node2 = _networkState.GetOrCreateNode("G8PZT");
        node2.Status = NodeStatus.Offline;
        
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>
            {
                { "includeOffline", false }
            }
        };

        // Act
        var result = _controller.ExecuteTool("get_all_nodes", request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        var totalNodesProperty = value!.GetType().GetProperty("totalNodes");
        Assert.NotNull(totalNodesProperty);
        Assert.Equal(1, totalNodesProperty.GetValue(value));
    }

    [Fact]
    public void ExecuteTool_GetAllNodes_WithIncludeOfflineTrue_IncludesOfflineNodes()
    {
        // Arrange
        var node1 = _networkState.GetOrCreateNode("M0LTE");
        node1.Status = NodeStatus.Online;
        
        var node2 = _networkState.GetOrCreateNode("G8PZT");
        node2.Status = NodeStatus.Offline;
        
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>
            {
                { "includeOffline", true }
            }
        };

        // Act
        var result = _controller.ExecuteTool("get_all_nodes", request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        var totalNodesProperty = value!.GetType().GetProperty("totalNodes");
        Assert.NotNull(totalNodesProperty);
        Assert.Equal(2, totalNodesProperty.GetValue(value));
    }

    #endregion

    #region Get Node Details Tests

    [Fact]
    public void ExecuteTool_GetNodeDetails_ReturnsNodeWithLinksAndCircuits()
    {
        // Arrange
        var node = _networkState.GetOrCreateNode("M0LTE");
        node.Alias = "TEST";
        node.Status = NodeStatus.Online;
        
        _networkState.GetOrCreateLink("M0LTE", "G8PZT");
        _networkState.GetOrCreateCircuit("M0LTE@M0LTE:0001", "G8PZT@G8PZT:0002");
        
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>
            {
                { "callsign", "M0LTE" }
            }
        };

        // Act
        var result = _controller.ExecuteTool("get_node_details", request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        
        var nodeProperty = value!.GetType().GetProperty("node");
        Assert.NotNull(nodeProperty);
        
        var linksProperty = value.GetType().GetProperty("links");
        Assert.NotNull(linksProperty);
        
        var circuitsProperty = value.GetType().GetProperty("circuits");
        Assert.NotNull(circuitsProperty);
    }

    [Fact]
    public void ExecuteTool_GetNodeDetails_WithoutCallsign_ReturnsError()
    {
        // Arrange
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>()
        };

        // Act
        var result = _controller.ExecuteTool("get_node_details", request);

        // Assert
        Assert.IsType<ObjectResult>(result);
        var objectResult = result as ObjectResult;
        Assert.NotNull(objectResult);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public void ExecuteTool_GetNodeDetails_WithNonExistentNode_ReturnsError()
    {
        // Arrange
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>
            {
                { "callsign", "NONEXISTENT" }
            }
        };

        // Act
        var result = _controller.ExecuteTool("get_node_details", request);

        // Assert
        Assert.IsType<ObjectResult>(result);
        var objectResult = result as ObjectResult;
        Assert.NotNull(objectResult);
        Assert.Equal(404, objectResult.StatusCode);
    }

    [Fact]
    public void ExecuteTool_GetNodeDetails_WithEmptyCallsign_ReturnsError()
    {
        // Arrange
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>
            {
                { "callsign", "" }
            }
        };

        // Act
        var result = _controller.ExecuteTool("get_node_details", request);

        // Assert
        Assert.IsType<ObjectResult>(result);
        var objectResult = result as ObjectResult;
        Assert.NotNull(objectResult);
        Assert.Equal(400, objectResult.StatusCode);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void ExecuteTool_WithUnknownTool_ReturnsNotFound()
    {
        // Arrange
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>()
        };

        // Act
        var result = _controller.ExecuteTool("unknown_tool", request);

        // Assert
        Assert.IsType<ObjectResult>(result);
        var objectResult = result as ObjectResult;
        Assert.NotNull(objectResult);
        Assert.Equal(404, objectResult.StatusCode);
    }

    [Fact]
    public void ExecuteTool_WithNullArguments_HandlesGracefully()
    {
        // Arrange
        var request = new McpController.McpToolRequest
        {
            Arguments = null
        };

        // Act
        var result = _controller.ExecuteTool("get_all_links", request) as OkObjectResult;

        // Assert - Should still work with default values
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    #endregion

    #region Hidden Callsign Tests

    [Fact]
    public void ExecuteTool_GetAllNodes_ExcludesHiddenCallsigns()
    {
        // Arrange - Create configuration with hidden callsigns
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "HiddenCallsigns:0", "HIDDEN" }
            })
            .Build();
        
        var networkStateLogger = Substitute.For<ILogger<NetworkStateService>>();
        var networkState = new NetworkStateService(networkStateLogger, configuration);
        var controller = new McpController(networkState, _logger);
        
        networkState.GetOrCreateNode("M0LTE");
        networkState.GetOrCreateNode("HIDDEN");
        networkState.GetOrCreateNode("HIDDEN-1");
        
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>()
        };

        // Act
        var result = controller.ExecuteTool("get_all_nodes", request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        var totalNodesProperty = value!.GetType().GetProperty("totalNodes");
        Assert.NotNull(totalNodesProperty);
        Assert.Equal(1, totalNodesProperty.GetValue(value));
    }

    [Fact]
    public void ExecuteTool_GetAllLinks_ExcludesHiddenCallsigns()
    {
        // Arrange - Create configuration with hidden callsigns
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "HiddenCallsigns:0", "HIDDEN" }
            })
            .Build();
        
        var networkStateLogger = Substitute.For<ILogger<NetworkStateService>>();
        var networkState = new NetworkStateService(networkStateLogger, configuration);
        var controller = new McpController(networkState, _logger);
        
        networkState.GetOrCreateLink("M0LTE", "G8PZT");
        networkState.GetOrCreateLink("HIDDEN", "M0ABC");
        networkState.GetOrCreateLink("M0XYZ", "HIDDEN-1");
        
        var request = new McpController.McpToolRequest
        {
            Arguments = new Dictionary<string, object>()
        };

        // Act
        var result = controller.ExecuteTool("get_all_links", request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        var totalLinksProperty = value!.GetType().GetProperty("totalLinks");
        Assert.NotNull(totalLinksProperty);
        Assert.Equal(1, totalLinksProperty.GetValue(value));
    }

    #endregion
}
