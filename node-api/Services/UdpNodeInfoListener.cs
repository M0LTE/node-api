using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using node_api.Models;
using node_api.Validators;
using node_api.Utilities;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace node_api.Services;

public sealed class UdpNodeInfoListener : BackgroundService, IAsyncDisposable
{
    private readonly ILogger<UdpNodeInfoListener> _logger;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;
    private readonly IDatagramProcessor _datagramProcessor;
    private UdpClient? _udpClient;

    public int Port { get; set; } = int.Parse(Environment.GetEnvironmentVariable("UDP_PORT")!);
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

    public UdpNodeInfoListener(
        ILogger<UdpNodeInfoListener> logger,
        IRabbitMqPublisher rabbitMqPublisher,
        IDatagramProcessor datagramProcessor)
    {
        _logger = logger;
        _rabbitMqPublisher = rabbitMqPublisher;
        _datagramProcessor = datagramProcessor;
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
        
        if (_rabbitMqPublisher.IsAvailable)
        {
            _logger.LogInformation("UDP service started listening on port {Port}. Publishing to RabbitMQ queue.", Port);
        }
        else
        {
            _logger.LogInformation("UDP service started listening on port {Port}. Processing directly (RabbitMQ unavailable).", Port);
        }

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
                        if (_rabbitMqPublisher.IsAvailable)
                        {
                            // Publish to RabbitMQ if available
                            await _rabbitMqPublisher.PublishDatagramAsync(result.Buffer, result.RemoteEndPoint.Address.ToString());
                        }
                        else
                        {
                            // Process directly if RabbitMQ unavailable
                            await _datagramProcessor.ProcessDatagramAsync(
                                result.Buffer, 
                                result.RemoteEndPoint.Address, 
                                DateTime.UtcNow, 
                                stoppingToken);
                        }
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
