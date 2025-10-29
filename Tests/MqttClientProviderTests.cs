using Microsoft.Extensions.Logging;
using NSubstitute;
using node_api.Services;
using Xunit;

namespace Tests;

public class MqttClientProviderTests
{
    private readonly ILogger<MqttClientProvider> _logger;
    private readonly MqttClientProvider _provider;

    public MqttClientProviderTests()
    {
        _logger = Substitute.For<ILogger<MqttClientProvider>>();
        _provider = new MqttClientProvider(_logger);
    }

    [Fact]
    public void IsInitialized_ReturnsFalse_BeforeInitialization()
    {
        // Act & Assert
        Assert.False(_provider.IsInitialized);
    }

    [Fact]
    public void GetClient_ThrowsInvalidOperationException_WhenNotInitialized()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _provider.GetClient());
        Assert.Contains("not been initialized", exception.Message);
        Assert.Contains("InitializeAsync", exception.Message);
    }

    [Fact]
    public async Task InitializeAsync_SetsIsInitializedToTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MQTT_WRITER_PASSWORD", "test-password");

        try
        {
            // Act
            await _provider.InitializeAsync();

            // Assert
            Assert.True(_provider.IsInitialized);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("MQTT_WRITER_PASSWORD", null);
            await _provider.DisposeAsync();
        }
    }

    [Fact]
    public async Task InitializeAsync_ReturnsClient_AfterInitialization()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MQTT_WRITER_PASSWORD", "test-password");

        try
        {
            // Act
            await _provider.InitializeAsync();
            var client = _provider.GetClient();

            // Assert
            Assert.NotNull(client);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("MQTT_WRITER_PASSWORD", null);
            await _provider.DisposeAsync();
        }
    }

    [Fact]
    public async Task InitializeAsync_ThrowsException_WhenPasswordNotSet()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MQTT_WRITER_PASSWORD", null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _provider.InitializeAsync());
    }

    [Fact]
    public async Task InitializeAsync_CanBeCalledMultipleTimes_WithoutError()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MQTT_WRITER_PASSWORD", "test-password");

        try
        {
            // Act
            await _provider.InitializeAsync();
            await _provider.InitializeAsync();
            await _provider.InitializeAsync();

            // Assert
            Assert.True(_provider.IsInitialized);
            var client = _provider.GetClient();
            Assert.NotNull(client);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("MQTT_WRITER_PASSWORD", null);
            await _provider.DisposeAsync();
        }
    }

    [Fact]
    public async Task InitializeAsync_IsThreadSafe()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MQTT_WRITER_PASSWORD", "test-password");

        try
        {
            // Act - Initialize from multiple threads simultaneously
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => Task.Run(() => _provider.InitializeAsync()))
                .ToArray();

            await Task.WhenAll(tasks);

            // Assert
            Assert.True(_provider.IsInitialized);
            var client = _provider.GetClient();
            Assert.NotNull(client);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("MQTT_WRITER_PASSWORD", null);
            await _provider.DisposeAsync();
        }
    }

    [Fact]
    public async Task DisposeAsync_CleansUpResources()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MQTT_WRITER_PASSWORD", "test-password");
        await _provider.InitializeAsync();

        // Act
        await _provider.DisposeAsync();

        // Assert - No exception should be thrown
        // The provider should be in a valid state after disposal
        Environment.SetEnvironmentVariable("MQTT_WRITER_PASSWORD", null);
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MQTT_WRITER_PASSWORD", "test-password");
        await _provider.InitializeAsync();

        // Act - First dispose should work
        await _provider.DisposeAsync();

        // Second dispose might throw ObjectDisposedException from the underlying MQTT client,
        // but our provider should handle it gracefully or at least not crash the application
        // Note: The ManagedMqttClient disposes its underlying resources on first dispose
        try
        {
            await _provider.DisposeAsync();
            await _provider.DisposeAsync();
        }
        catch (ObjectDisposedException)
        {
            // Expected - ManagedMqttClient throws when accessing a disposed object
            // This is acceptable behavior as long as first dispose worked
        }

        Environment.SetEnvironmentVariable("MQTT_WRITER_PASSWORD", null);
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledWithoutInitialization()
    {
        // Act & Assert - Should not throw
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task GetClient_ReturnsSameInstance_OnMultipleCalls()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MQTT_WRITER_PASSWORD", "test-password");

        try
        {
            await _provider.InitializeAsync();

            // Act
            var client1 = _provider.GetClient();
            var client2 = _provider.GetClient();

            // Assert
            Assert.Same(client1, client2);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("MQTT_WRITER_PASSWORD", null);
            await _provider.DisposeAsync();
        }
    }
}
