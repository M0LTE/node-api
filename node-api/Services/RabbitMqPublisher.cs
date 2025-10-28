using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace node_api.Services;

/// <summary>
/// Publishes raw UDP datagrams to RabbitMQ for durability and future service separation
/// </summary>
public sealed class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly bool _isAvailable;
    private const string ExchangeName = "udp-datagrams";
    private const string QueueName = "udp-datagram-queue";
    private const string RoutingKey = "datagram";

    public bool IsAvailable => _isAvailable;

    public RabbitMqPublisher(ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;

        var rabbitHost = Environment.GetEnvironmentVariable("RABBIT_HOST");
        var rabbitUser = Environment.GetEnvironmentVariable("RABBIT_USER");
        var rabbitPass = Environment.GetEnvironmentVariable("RABBIT_PASS");

        // RabbitMQ is optional - if not configured, we'll just continue without it
        if (string.IsNullOrWhiteSpace(rabbitHost) || 
            string.IsNullOrWhiteSpace(rabbitUser) || 
            string.IsNullOrWhiteSpace(rabbitPass))
        {
            _logger.LogWarning("RabbitMQ not configured (missing RABBIT_HOST, RABBIT_USER, or RABBIT_PASS environment variables). UDP datagrams will be processed directly without RabbitMQ persistence.");
            _isAvailable = false;
            return;
        }

        _logger.LogInformation("RabbitMQ configuration detected. Attempting to connect to {Host}...", rabbitHost);

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = rabbitHost,
                UserName = rabbitUser,
                Password = rabbitPass,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(60)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange and queue (idempotent operations)
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.QueueBind(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: RoutingKey);

            _isAvailable = true;
            _logger.LogInformation("RabbitMQ publisher initialized successfully on host {Host}. Queue: {Queue}, Exchange: {Exchange}", 
                rabbitHost, QueueName, ExchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection to {Host}. UDP datagrams will be processed directly without RabbitMQ persistence.", rabbitHost);
            _isAvailable = false;
            
            // Clean up partial initialization
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }

    public Task PublishDatagramAsync(byte[] datagram, string sourceIp, DateTime receivedAt)
    {
        if (!_isAvailable || _channel == null)
        {
            // Silently skip if RabbitMQ is not available
            return Task.CompletedTask;
        }

        try
        {
            var message = new
            {
                datagram = Convert.ToBase64String(datagram),
                sourceIp,
                receivedAt
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: RoutingKey,
                basicProperties: properties,
                body: body);

            _logger.LogDebug("Published datagram from {SourceIp} to RabbitMQ (received at {ReceivedAt})", sourceIp, receivedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing datagram to RabbitMQ from {SourceIp}. Message will be lost from RabbitMQ but will still be processed.", sourceIp);
            // Don't throw - we don't want to stop UDP processing if RabbitMQ fails
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ publisher");
        }
    }
}
