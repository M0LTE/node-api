using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using node_api.Services;

namespace Tests;

/// <summary>
/// Tests for MySqlNetworkStateRepository batch deletion methods.
/// These tests use a mock repository since we don't have a real database in tests.
/// </summary>
public class MySqlNetworkStateRepositoryBatchTests
{
    private class TestableRepository : MySqlNetworkStateRepository
    {
        public List<string> DeletedLinks { get; } = new();
        public List<string> DeletedCircuits { get; } = new();
        public bool ThrowOnDelete { get; set; }

        public TestableRepository() 
            : base(Substitute.For<ILogger<MySqlNetworkStateRepository>>())
        {
        }

        public new async Task<int> BatchDeleteLinksAsync(IEnumerable<string> canonicalKeys, CancellationToken ct = default)
        {
            if (ThrowOnDelete) throw new Exception("Simulated database error");
            
            var keys = canonicalKeys.ToList();
            DeletedLinks.AddRange(keys);
            return keys.Count;
        }

        public new async Task<int> BatchDeleteCircuitsAsync(IEnumerable<string> canonicalKeys, CancellationToken ct = default)
        {
            if (ThrowOnDelete) throw new Exception("Simulated database error");
            
            var keys = canonicalKeys.ToList();
            DeletedCircuits.AddRange(keys);
            return keys.Count;
        }
    }

    [Fact]
    public async Task BatchDeleteLinks_DeletesMultipleLinks()
    {
        // Arrange
        var repository = new TestableRepository();
        var keys = new[] { "LINK1", "LINK2", "LINK3" };

        // Act
        var result = await repository.BatchDeleteLinksAsync(keys);

        // Assert
        result.Should().Be(3);
        repository.DeletedLinks.Should().HaveCount(3);
        repository.DeletedLinks.Should().Contain(keys);
    }

    [Fact]
    public async Task BatchDeleteLinks_ReturnsZeroForEmptyList()
    {
        // Arrange
        var repository = new TestableRepository();
        var keys = Array.Empty<string>();

        // Act
        var result = await repository.BatchDeleteLinksAsync(keys);

        // Assert
        result.Should().Be(0);
        repository.DeletedLinks.Should().BeEmpty();
    }

    [Fact]
    public async Task BatchDeleteCircuits_DeletesMultipleCircuits()
    {
        // Arrange
        var repository = new TestableRepository();
        var keys = new[] { "CIRCUIT1", "CIRCUIT2", "CIRCUIT3", "CIRCUIT4", "CIRCUIT5" };

        // Act
        var result = await repository.BatchDeleteCircuitsAsync(keys);

        // Assert
        result.Should().Be(5);
        repository.DeletedCircuits.Should().HaveCount(5);
        repository.DeletedCircuits.Should().Contain(keys);
    }

    [Fact]
    public async Task BatchDeleteCircuits_ReturnsZeroForEmptyList()
    {
        // Arrange
        var repository = new TestableRepository();
        var keys = Array.Empty<string>();

        // Act
        var result = await repository.BatchDeleteCircuitsAsync(keys);

        // Assert
        result.Should().Be(0);
        repository.DeletedCircuits.Should().BeEmpty();
    }

    [Fact]
    public async Task BatchDeleteCircuits_HandlesLargeList()
    {
        // Arrange
        var repository = new TestableRepository();
        var keys = Enumerable.Range(1, 1000)
            .Select(i => $"CIRCUIT{i}")
            .ToList();

        // Act
        var result = await repository.BatchDeleteCircuitsAsync(keys);

        // Assert
        result.Should().Be(1000);
        repository.DeletedCircuits.Should().HaveCount(1000);
    }

    [Fact]
    public async Task BatchDeleteLinks_PropagatesException()
    {
        // Arrange
        var repository = new TestableRepository { ThrowOnDelete = true };
        var keys = new[] { "LINK1", "LINK2" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await repository.BatchDeleteLinksAsync(keys));
    }

    [Fact]
    public async Task BatchDeleteCircuits_PropagatesException()
    {
        // Arrange
        var repository = new TestableRepository { ThrowOnDelete = true };
        var keys = new[] { "CIRCUIT1", "CIRCUIT2" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await repository.BatchDeleteCircuitsAsync(keys));
    }
}
