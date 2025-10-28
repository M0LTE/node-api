using node_api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using node_api.Models.NetworkState;

namespace Tests;

public class TestCallsignFilteringTests
{
    private readonly NetworkStateService _networkState;

    public TestCallsignFilteringTests()
    {
        _networkState = new NetworkStateService(NullLogger<NetworkStateService>.Instance);
    }

    #region IsTestCallsign Tests

    [Theory]
    [InlineData("TEST")]
    [InlineData("test")]
    [InlineData("TeSt")]
    [InlineData("TEST-0")]
    [InlineData("TEST-1")]
    [InlineData("TEST-15")]
    [InlineData("test-0")]
    [InlineData("test-15")]
    public void IsTestCallsign_Should_Return_True_For_Test_Callsigns(string callsign)
    {
        var result = _networkState.IsTestCallsign(callsign);
        Assert.True(result, $"Expected {callsign} to be identified as a test callsign");
    }

    [Theory]
    [InlineData("TEST-16")]
    [InlineData("TEST-99")]
    [InlineData("TEST1")]
    [InlineData("TESTING")]
    [InlineData("M0LTE")]
    [InlineData("G8PZT-1")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("TEST-")]
    [InlineData("TEST-A")]
    public void IsTestCallsign_Should_Return_False_For_Non_Test_Callsigns(string? callsign)
    {
        var result = _networkState.IsTestCallsign(callsign!);
        Assert.False(result, $"Expected {callsign ?? "(null)"} to NOT be identified as a test callsign");
    }

    #endregion

    #region Node Filtering Tests

    [Fact]
    public void GetAllNodes_Should_Include_Test_Nodes()
    {
        // Create test nodes
        _networkState.GetOrCreateNode("M0LTE");
        _networkState.GetOrCreateNode("TEST");
        _networkState.GetOrCreateNode("TEST-1");
        _networkState.GetOrCreateNode("G8PZT-2");

        var allNodes = _networkState.GetAllNodes();
        
        Assert.Equal(4, allNodes.Count);
        Assert.Contains("TEST", allNodes.Keys);
        Assert.Contains("TEST-1", allNodes.Keys);
    }

    [Fact]
    public void GetNodesByBaseCallsign_Should_Return_Test_Nodes_When_Requested()
    {
        // Create test nodes with various SSIDs
        _networkState.GetOrCreateNode("TEST");
        _networkState.GetOrCreateNode("TEST-1");
        _networkState.GetOrCreateNode("TEST-5");
        _networkState.GetOrCreateNode("M0LTE");

        var testNodes = _networkState.GetNodesByBaseCallsign("TEST").ToList();
        
        Assert.Equal(3, testNodes.Count);
        Assert.Contains(testNodes, n => n.Callsign == "TEST");
        Assert.Contains(testNodes, n => n.Callsign == "TEST-1");
        Assert.Contains(testNodes, n => n.Callsign == "TEST-5");
    }

    #endregion

    #region Link Filtering Tests

    [Fact]
    public void GetAllLinks_Should_Include_Test_Links()
    {
        // Create links involving TEST callsigns
        _networkState.GetOrCreateLink("M0LTE", "G8PZT-1");
        _networkState.GetOrCreateLink("TEST", "G8PZT-2");
        _networkState.GetOrCreateLink("M0ABC", "TEST-1");

        var allLinks = _networkState.GetAllLinks();
        
        Assert.Equal(3, allLinks.Count);
    }

    [Fact]
    public void GetLinksForNode_Should_Return_Links_For_Test_Nodes()
    {
        // Create links involving TEST
        _networkState.GetOrCreateLink("TEST", "G8PZT-1");
        _networkState.GetOrCreateLink("TEST", "M0LTE");

        var testLinks = _networkState.GetLinksForNode("TEST").ToList();
        
        Assert.Equal(2, testLinks.Count);
    }

    #endregion

    #region Circuit Filtering Tests

    [Fact]
    public void GetAllCircuits_Should_Include_Test_Circuits()
    {
        // Create circuits involving TEST callsigns
        _networkState.GetOrCreateCircuit("M0LTE-4:0001", "G8PZT@G8PZT:14c0");
        _networkState.GetOrCreateCircuit("TEST@TEST:0001", "G8PZT@G8PZT:14c0");
        _networkState.GetOrCreateCircuit("M0ABC@M0ABC:0001", "TEST-1@TEST-1:0002");

        var allCircuits = _networkState.GetAllCircuits();
        
        Assert.Equal(3, allCircuits.Count);
    }

    [Fact]
    public void GetCircuitsForNode_Should_Return_Circuits_For_Test_Nodes()
    {
        // Create circuits involving TEST addresses
        _networkState.GetOrCreateCircuit("TEST@TEST:0001", "G8PZT@G8PZT:14c0");
        _networkState.GetOrCreateCircuit("TEST-1@TEST-1:0002", "M0LTE@M0LTE:0003");

        var testCircuits = _networkState.GetCircuitsForNode("TEST@TEST:0001").ToList();
        
        Assert.Single(testCircuits);
    }

    #endregion

    #region Case Sensitivity Tests

    [Fact]
    public void IsTestCallsign_Should_Be_Case_Insensitive()
    {
        Assert.True(_networkState.IsTestCallsign("TEST"));
        Assert.True(_networkState.IsTestCallsign("test"));
        Assert.True(_networkState.IsTestCallsign("TeSt"));
        Assert.True(_networkState.IsTestCallsign("TEST-5"));
        Assert.True(_networkState.IsTestCallsign("test-5"));
        Assert.True(_networkState.IsTestCallsign("TeSt-5"));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void IsTestCallsign_Should_Handle_Boundary_SSIDs()
    {
        // Valid SSIDs
        Assert.True(_networkState.IsTestCallsign("TEST-0"));
        Assert.True(_networkState.IsTestCallsign("TEST-15"));
        
        // Invalid SSIDs
        Assert.False(_networkState.IsTestCallsign("TEST-16"));
        Assert.False(_networkState.IsTestCallsign("TEST--1"));
        Assert.False(_networkState.IsTestCallsign("TEST-"));
    }

    [Fact]
    public void IsTestCallsign_Should_Not_Match_Similar_Callsigns()
    {
        Assert.False(_networkState.IsTestCallsign("TESTING"));
        Assert.False(_networkState.IsTestCallsign("TEST1"));
        Assert.False(_networkState.IsTestCallsign("ATEST"));
        Assert.False(_networkState.IsTestCallsign("TESTA"));
        Assert.False(_networkState.IsTestCallsign("2TEST"));
    }

    #endregion
}
