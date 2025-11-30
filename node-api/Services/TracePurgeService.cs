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
        var queries = new[] 
        { 
            "delete low_priority FROM `traces` WHERE timestamp < NOW() - INTERVAL 7 DAY limit 25000;",
            "delete low_priority FROM `l3traces` WHERE timestamp < NOW() - INTERVAL 7 DAY limit 25000;"
        };

        while (true)
        {
            try
            {
                using var conn = Database.GetConnection(open: false);
                await conn.OpenAsync(cancellationToken);
                foreach (var query in queries)
                {
                    await conn.ExecuteAsync(query);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
            }
            finally
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
