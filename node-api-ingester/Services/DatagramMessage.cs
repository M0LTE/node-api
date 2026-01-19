namespace node_api_ingester.Services;

/// <summary>
/// Represents a UDP datagram message to be published to RabbitMQ
/// </summary>
/// <param name="Datagram">The raw datagram bytes</param>
/// <param name="SourceIp">The IP address the datagram was received from</param>
/// <param name="ReceivedAt">When the datagram was received</param>
public sealed record DatagramMessage(byte[] Datagram, string SourceIp, DateTime ReceivedAt);
