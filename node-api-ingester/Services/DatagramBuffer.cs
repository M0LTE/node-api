using System.Threading.Channels;

namespace node_api_ingester.Services;

/// <summary>
/// Interface for buffering datagrams in memory when RabbitMQ is unavailable
/// </summary>
public interface IDatagramBuffer
{
    /// <summary>
    /// Writes a datagram to the buffer
    /// </summary>
    ValueTask WriteAsync(DatagramMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads datagrams from the buffer as they become available
    /// </summary>
    IAsyncEnumerable<DatagramMessage> ReadAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current number of items in the buffer
    /// </summary>
    int Count { get; }
}

/// <summary>
/// In-memory buffer for datagrams using System.Threading.Channels
/// </summary>
public sealed class DatagramBuffer : IDatagramBuffer
{
    private readonly Channel<DatagramMessage> _channel;
    private int _count;

    public DatagramBuffer(int capacity = 100_000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<DatagramMessage>(options);
    }

    public int Count => _count;

    public async ValueTask WriteAsync(DatagramMessage message, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
        Interlocked.Increment(ref _count);
    }

    public async IAsyncEnumerable<DatagramMessage> ReadAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            Interlocked.Decrement(ref _count);
            yield return message;
        }
    }
}
