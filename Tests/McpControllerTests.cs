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

    [Fact]
    public void HandleMcpJsonRpc_Notification_Initialized_ReturnsOk()
    {
        // Arrange - notification has no id field
        var requestJson = """{"jsonrpc":"2.0","method":"notifications/initialized","params":{}}""";
        var request = JsonDocument.Parse(requestJson).RootElement;

        // Act
        var result = _controller.HandleMcpJsonRpc(request);

        // Assert - Notifications should return 200 OK with no body
        var okResult = Assert.IsType<OkResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public void HandleMcpJsonRpc_Notification_Cancelled_ReturnsOk()
    {
        // Arrange
        var requestJson = """{"jsonrpc":"2.0","method":"notifications/cancelled","params":{}}""";
        var request = JsonDocument.Parse(requestJson).RootElement;

        // Act
        var result = _controller.HandleMcpJsonRpc(request);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public void HandleMcpJsonRpc_Notification_Unknown_ReturnsOk()
    {
        // Arrange
        var requestJson = """{"jsonrpc":"2.0","method":"notifications/unknown","params":{}}""";
        var request = JsonDocument.Parse(requestJson).RootElement;

        // Act
        var result = _controller.HandleMcpJsonRpc(request);

        // Assert - Unknown notifications should still return OK
        var okResult = Assert.IsType<OkResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    #endregion
}
