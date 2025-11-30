using Dapper;

namespace node_api.Services;

public class TracePurgeService(ILogger<TracePurgeService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () => await Runner(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    private async Task Runner(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                using var conn = Database.GetConnection(open: false);
                await conn.OpenAsync(cancellationToken);
                await conn.ExecuteAsync("delete low_priority FROM `traces` WHERE timestamp < NOW() - INTERVAL 30 DAY limit 100000;");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
            }
            finally
            {
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
