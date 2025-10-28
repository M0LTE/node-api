using System.Net;

namespace node_api.Services;

/// <summary>
/// Interface for processing UDP datagrams
/// </summary>
public interface IDatagramProcessor
{
    /// <summary>
    /// Process a raw UDP datagram
    /// </summary>
    /// <param name="datagram">The raw datagram bytes</param>
    /// <param name="sourceIpAddress">The IP address the datagram was received from</param>
    /// <param name="arrivalTime">The time the datagram arrived at the server</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ProcessDatagramAsync(byte[] datagram, IPAddress sourceIpAddress, DateTime arrivalTime, CancellationToken cancellationToken = default);
}
