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

    public int Port { get; set; } = 13579;
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
        
        var processingMode = _rabbitMqPublisher.IsAvailable ? "RabbitMQ queue" : "direct processing";
        _logger.LogInformation("UDP service started listening on port {Port}. Mode: {Mode}", Port, processingMode);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await _udpClient.ReceiveAsync(stoppingToken).ConfigureAwait(false);
                var receivedAt = DateTime.UtcNow; // Capture arrival time immediately
                
                _logger.LogDebug("Received datagram from {ip}", result.RemoteEndPoint);
                
                if (_rabbitMqPublisher.IsAvailable)
                {
                    // RabbitMQ available: publish to queue only (consumer will process)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _rabbitMqPublisher.PublishDatagramAsync(result.Buffer, result.RemoteEndPoint.Address.ToString(), receivedAt);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to publish datagram to RabbitMQ. Processing directly as fallback.");
                            // Fallback to direct processing if RabbitMQ publish fails
                            try
                            {
                                await _datagramProcessor.ProcessDatagramAsync(result.Buffer, result.RemoteEndPoint.Address, receivedAt, stoppingToken);
                            }
                            catch (Exception processingEx)
                            {
                                _logger.LogError(processingEx, "Error in fallback processing of datagram from {Endpoint}", result.RemoteEndPoint);
                            }
                        }
                    }, stoppingToken);
                }
                else
                {
                    // RabbitMQ not available: process directly
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _datagramProcessor.ProcessDatagramAsync(result.Buffer, result.RemoteEndPoint.Address, receivedAt, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing datagram from {Endpoint}", result.RemoteEndPoint);
                        }
                    }, stoppingToken);
                }
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