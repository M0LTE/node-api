using node_api.Models;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace node_api.Services;

public sealed class XrouterUdpService : BackgroundService, IAsyncDisposable
{
    private readonly ILogger<XrouterUdpService> _logger;
    private readonly FramesRepo _framesRepo;
    private readonly Channel<(XrouterUdpJsonFrame Frame, IPEndPoint RemoteEndPoint)> _processingChannel;
    private readonly ChannelWriter<(XrouterUdpJsonFrame Frame, IPEndPoint RemoteEndPoint)> _channelWriter;
    private readonly SemaphoreSlim _processingSemaphore;
    
    private UdpClient? _udpClient;
    private Task? _processingTask;

    public int Port { get; set; } = 13579;
    public int MaxConcurrentProcessing { get; set; } = 100;
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

    public XrouterUdpService(ILogger<XrouterUdpService> logger, FramesRepo framesRepo)
    {
        _logger = logger;
        _framesRepo = framesRepo;
        var channelOptions = new BoundedChannelOptions(MaxConcurrentProcessing * 2)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        };
        
        _processingChannel = Channel.CreateBounded<(XrouterUdpJsonFrame, IPEndPoint)>(channelOptions);
        _channelWriter = _processingChannel.Writer;
        _processingSemaphore = new SemaphoreSlim(MaxConcurrentProcessing, MaxConcurrentProcessing);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start the frame processing task
        _processingTask = ProcessFramesAsync(stoppingToken);

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

        // Signal that we're done writing to the channel
        _channelWriter.Complete();
        
        // Wait for processing to complete
        if (_processingTask is not null)
        {
            await _processingTask.ConfigureAwait(false);
        }
    }

    private async Task StartUdpListenerAsync(CancellationToken stoppingToken)
    {
        _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, Port));
        _logger.LogInformation("Xrouter UDP service started listening on port {Port}", Port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await _udpClient.ReceiveAsync(stoppingToken).ConfigureAwait(false);
                await ProcessDatagramAsync(result, stoppingToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
        }
    }

    private async Task ProcessDatagramAsync(UdpReceiveResult result, CancellationToken stoppingToken)
    {
        try
        {
            var json = Encoding.UTF8.GetString(result.Buffer);
            _logger.LogDebug("Received UDP datagram from {Endpoint}: {Json}", result.RemoteEndPoint, json);

            var frame = JsonSerializer.Deserialize<XrouterUdpJsonFrame>(json);
            
            _logger.LogInformation("Received Xrouter frame - Port: {Port}, Source: {Source}, Destination: {Destination}, Type: {Type}",
                frame.Port, frame.Source, frame.Destination, frame.Type);

            // Queue the frame for processing
            await _channelWriter.WriteAsync((frame, result.RemoteEndPoint), stoppingToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize JSON from {Endpoint}: {Json}",
                result.RemoteEndPoint, Encoding.UTF8.GetString(result.Buffer));
        }
        catch (InvalidOperationException) when (stoppingToken.IsCancellationRequested)
        {
            // Channel is closed, ignore
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing datagram from {Endpoint}", result.RemoteEndPoint);
        }
    }

    private async Task ProcessFramesAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var (frame, remoteEndPoint) in _processingChannel.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                // Process frames with controlled concurrency
                _ = Task.Run(async () =>
                {
                    await _processingSemaphore.WaitAsync(stoppingToken).ConfigureAwait(false);
                    try
                    {
                        await HandleXrouterFrameAsync(frame, remoteEndPoint, stoppingToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        _processingSemaphore.Release();
                    }
                }, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in frame processing loop");
        }
    }

    private async Task HandleXrouterFrameAsync(XrouterUdpJsonFrame frame, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        try
        {
            // Implement your business logic here
            // For example:
            // - Route the frame based on destination
            // - Store in database  
            // - Forward to other services
            // - Validate sequence numbers
            
            _logger.LogInformation("Processing frame from {RemoteEndPoint} - Control: {Control}, Sequences: R={RSeq}, T={TSeq}",
                remoteEndPoint, frame.Control, frame.ReceiveSequence, frame.TransmitSequence);

            _framesRepo.Frames.Add(frame);

            // Simulate async processing
            //await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling frame from {RemoteEndPoint}", remoteEndPoint);
        }
    }

    public override void Dispose()
    {
        _udpClient?.Close();
        _udpClient?.Dispose();
        _processingSemaphore?.Dispose();
        base.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        _channelWriter?.Complete();
        
        if (_processingTask is not null)
        {
            await _processingTask.ConfigureAwait(false);
        }
        
        _udpClient?.Close();
        _udpClient?.Dispose();
        _processingSemaphore?.Dispose();
        
        Dispose();
        GC.SuppressFinalize(this);
    }
}