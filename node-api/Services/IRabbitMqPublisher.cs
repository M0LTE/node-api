namespace node_api.Services;

/// <summary>
/// Interface for publishing raw UDP datagrams to RabbitMQ
/// </summary>
public interface IRabbitMqPublisher
{
    /// <summary>
    /// Publishes a raw UDP datagram to RabbitMQ
    /// </summary>
    /// <param name="datagram">The raw datagram bytes</param>
    /// <param name="sourceIp">The IP address the datagram was received from</param>
    Task PublishDatagramAsync(byte[] datagram, string sourceIp);
    
    /// <summary>
    /// Check if RabbitMQ is available/configured
    /// </summary>
    bool IsAvailable { get; }
}
