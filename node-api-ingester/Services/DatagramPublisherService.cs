namespace node_api_ingester.Services;

/// <summary>
/// Background service that reads datagrams from the buffer and publishes them to RabbitMQ.
/// Handles RabbitMQ unavailability by retrying with exponential backoff.
/// </summary>
public sealed class DatagramPublisherService(
    ILogger<DatagramPublisherService> logger,
    IDatagramBuffer buffer,
    IRabbitMqPublisher rabbitMqPublisher) : BackgroundService
{
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Datagram publisher service started");

        await foreach (var message in buffer.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            await PublishWithRetryAsync(message, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task PublishWithRetryAsync(DatagramMessage message, CancellationToken stoppingToken)
    {
        var retryDelay = InitialRetryDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await rabbitMqPublisher.PublishDatagramAsync(message.Datagram, message.SourceIp, message.ReceivedAt).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var bufferedCount = buffer.Count;
                logger.LogWarning(
                    ex,
                    "Failed to publish datagram to RabbitMQ. Retrying in {Delay}. Buffer contains {Count} messages.",
                    retryDelay,
                    bufferedCount);

                await Task.Delay(retryDelay, stoppingToken).ConfigureAwait(false);

                // Exponential backoff with cap
                retryDelay = TimeSpan.FromTicks(Math.Min(retryDelay.Ticks * 2, MaxRetryDelay.Ticks));
            }
        }
    }
}
