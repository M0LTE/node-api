using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace node_api.Services;

/// <summary>
/// Consumes datagrams from RabbitMQ and processes them in the API/storage service
/// </summary>
public sealed class RabbitMqConsumer : BackgroundService, IAsyncDisposable
{
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly IDatagramProcessor _datagramProcessor;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly bool _isEnabled;
    private const string QueueName = "udp-datagram-queue";
    private readonly Channel<DatagramMessage> _processingChannel;
    private Task? _processingTask;
    private readonly int _maxConcurrentProcessing = 10;
    private int _disposeState;

    public RabbitMqConsumer(
        ILogger<RabbitMqConsumer> logger,
        IDatagramProcessor datagramProcessor)
    {
        _logger = logger;
        _datagramProcessor = datagramProcessor;

        var rabbitHost = Environment.GetEnvironmentVariable("RABBIT_HOST");
        var rabbitUser = Environment.GetEnvironmentVariable("RABBIT_USER");
        var rabbitPass = Environment.GetEnvironmentVariable("RABBIT_PASS");

        _isEnabled = !string.IsNullOrWhiteSpace(rabbitHost) && 
                     !string.IsNullOrWhiteSpace(rabbitUser) && 
                     !string.IsNullOrWhiteSpace(rabbitPass);

        var channelOptions = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _processingChannel = Channel.CreateBounded<DatagramMessage>(channelOptions);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_isEnabled)
        {
            _logger.LogInformation("RabbitMQ consumer is disabled (RabbitMQ not configured)");
            return;
        }

        _logger.LogInformation("Starting RabbitMQ consumer...");

        // Start processing task
        _processingTask = ProcessMessagesAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndConsumeAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "RabbitMQ consumer failed, reconnecting in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _processingChannel.Writer.Complete();
        
        if (_processingTask != null)
        {
            await _processingTask;
        }
    }

    private async Task ConnectAndConsumeAsync(CancellationToken stoppingToken)
    {
        var rabbitHost = Environment.GetEnvironmentVariable("RABBIT_HOST")!;
        var rabbitUser = Environment.GetEnvironmentVariable("RABBIT_USER")!;
        var rabbitPass = Environment.GetEnvironmentVariable("RABBIT_PASS")!;

        var factory = new ConnectionFactory
        {
            HostName = rabbitHost,
            UserName = rabbitUser,
            Password = rabbitPass,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(60),
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Set QoS to process messages with prefetch
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.Received += async (sender, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                
                var message = JsonSerializer.Deserialize<DatagramMessage>(messageJson);
                
                if (message != null)
                {
                    await _processingChannel.Writer.WriteAsync(message, stoppingToken);
                }
                
                // Acknowledge the message
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RabbitMQ message");
                // Reject and requeue the message on error
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.QueueDeclare(QueueName, true, false, false);

        _channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("RabbitMQ consumer connected and listening on queue {QueueName}", QueueName);

        // Keep the connection alive until cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessagesAsync(CancellationToken stoppingToken)
    {
        // Create tasks for concurrent processing
        var processingTasks = new List<Task>();
        var semaphore = new SemaphoreSlim(_maxConcurrentProcessing, _maxConcurrentProcessing);

        await foreach (var message in _processingChannel.Reader.ReadAllAsync(stoppingToken))
        {
            await semaphore.WaitAsync(stoppingToken);
            
            var task = Task.Run(async () =>
            {
                try
                {
                    // Decode the datagram
                    var datagram = Convert.FromBase64String(message.Datagram);
                    
                    // Parse IP address
                    if (!IPAddress.TryParse(message.SourceIp, out var ipAddress))
                    {
                        _logger.LogWarning("Invalid source IP in RabbitMQ message: {SourceIp}", message.SourceIp);
                        return;
                    }

                    _logger.LogDebug("Processing datagram from RabbitMQ: {SourceIp}, received at {ReceivedAt}", 
                        message.SourceIp, message.ReceivedAt);
                    
                    // Use the original arrival time from when it was first received
                    var arrivalTime = message.ReceivedAt.Kind == DateTimeKind.Utc 
                        ? message.ReceivedAt 
                        : DateTime.SpecifyKind(message.ReceivedAt, DateTimeKind.Utc);
                    
                    // Process using the shared datagram processor
                    await _datagramProcessor.ProcessDatagramAsync(datagram, ipAddress, arrivalTime, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from RabbitMQ queue");
                }
                finally
                {
                    semaphore.Release();
                }
            }, stoppingToken);

            processingTasks.Add(task);

            // Clean up completed tasks periodically
            processingTasks.RemoveAll(t => t.IsCompleted);
        }

        // Wait for all remaining tasks to complete
        await Task.WhenAll(processingTasks);
        semaphore.Dispose();
    }

    public override void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            return;
        }

        DisposeRabbitMqResources();
        base.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private void DisposeRabbitMqResources()
    {
        var channel = Interlocked.Exchange(ref _channel, null);
        if (channel != null)
        {
            TryCloseChannel(channel);
            TryDisposeChannel(channel);
        }

        var connection = Interlocked.Exchange(ref _connection, null);
        if (connection != null)
        {
            TryCloseConnection(connection);
            TryDisposeConnection(connection);
        }
    }

    private static void TryCloseChannel(IModel channel)
    {
        try
        {
            if (channel.IsOpen)
            {
                channel.Close();
            }
        }
        catch (AlreadyClosedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static void TryDisposeChannel(IModel channel)
    {
        try
        {
            channel.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static void TryCloseConnection(IConnection connection)
    {
        try
        {
            if (connection.IsOpen)
            {
                connection.Close();
            }
        }
        catch (AlreadyClosedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static void TryDisposeConnection(IConnection connection)
    {
        try
        {
            connection.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private class DatagramMessage
    {
        [JsonPropertyName("datagram")]
        public required string Datagram { get; set; }
        
        [JsonPropertyName("sourceIp")]
        public required string SourceIp { get; set; }
        
        [JsonPropertyName("receivedAt")]
        public DateTime ReceivedAt { get; set; }
    }
}
