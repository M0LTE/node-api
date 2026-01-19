using Microsoft.Extensions.Options;
using node_api_ingester.Configuration;
using System.Net;
using System.Net.Sockets;

namespace node_api_ingester.Services;

public sealed class UdpNodeInfoListener(
    ILogger<UdpNodeInfoListener> logger,
    IDatagramBuffer datagramBuffer,
    IOptions<UdpNodeInfoListenerSettings> settings) : BackgroundService, IAsyncDisposable
{
    private UdpClient? _udpClient;

    public int Port { get; set; } = settings.Value.UdpPort;
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

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
                logger.LogError(ex, "UDP listener failed, restarting in {Delay}", ReconnectDelay);
                await Task.Delay(ReconnectDelay, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task StartUdpListenerAsync(CancellationToken stoppingToken)
    {
        _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, Port));
        
        logger.LogInformation("UDP service started listening on port {Port}. Buffering datagrams for RabbitMQ.", Port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await _udpClient.ReceiveAsync(stoppingToken).ConfigureAwait(false);

                logger.LogDebug("Received datagram from {ip}", result.RemoteEndPoint);
                
                var message = new DatagramMessage(
                    result.Buffer,
                    result.RemoteEndPoint.Address.ToString(),
                    DateTime.UtcNow);

                await datagramBuffer.WriteAsync(message, stoppingToken).ConfigureAwait(false);
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
