using node_api.Services;

namespace Tests.Mocks;

/// <summary>
/// Mock RabbitMQ publisher for testing without requiring a real RabbitMQ instance
/// </summary>
public class MockRabbitMqPublisher : IRabbitMqPublisher
{
    public bool IsAvailable => true;

    public List<(byte[] Datagram, string SourceIp, DateTime Timestamp)> PublishedDatagrams { get; } = new();

    public Task PublishDatagramAsync(byte[] datagram, string sourceIp)
    {
        PublishedDatagrams.Add((datagram, sourceIp, DateTime.UtcNow));
        return Task.CompletedTask;
    }

    public void ClearPublishedDatagrams()
    {
        PublishedDatagrams.Clear();
    }
}
