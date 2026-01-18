using System.Net;
using System.Net.Sockets;

namespace node_api_ingester.Services;

public sealed class UdpNodeInfoListener : BackgroundService, IAsyncDisposable
{
    private readonly ILogger<UdpNodeInfoListener> _logger;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;
    private UdpClient? _udpClient;

    public int Port { get; set; } = 13579;
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

    public UdpNodeInfoListener(
        ILogger<UdpNodeInfoListener> logger,
        IRabbitMqPublisher rabbitMqPublisher)
    {
        _logger = logger;
        _rabbitMqPublisher = rabbitMqPublisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await StartUdpListenerAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "UDP listener failed, restarting in {Delay}", ReconnectDelay);
                await Task.Delay(ReconnectDelay, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task StartUdpListenerAsync(CancellationToken stoppingToken)
    {
        _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, Port));
        
        _logger.LogInformation("UDP service started listening on port {Port}. Publishing to RabbitMQ queue.", Port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await _udpClient.ReceiveAsync(stoppingToken).ConfigureAwait(false);
                
                _logger.LogDebug("Received datagram from {ip}", result.RemoteEndPoint);
                
                // Fire-and-forget processing
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Publish to RabbitMQ if available
                        await _rabbitMqPublisher.PublishDatagramAsync(result.Buffer, result.RemoteEndPoint.Address.ToString());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process datagram from {Endpoint}", result.RemoteEndPoint);
                    }
                }, stoppingToken);
            }
        }
        finally
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
        }
    }

    public override void Dispose()
    {
        _udpClient?.Close();
        _udpClient?.Dispose();
        base.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        _udpClient?.Close();
        _udpClient?.Dispose();
        
        Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
