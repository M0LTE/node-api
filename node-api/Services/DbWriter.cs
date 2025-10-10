using Dapper;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;

namespace node_api.Services;

public class DbWriter(ILogger<DbWriter> logger) : IHostedService
{
    private IManagedMqttClient? mqttClient;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new MqttFactory();
        mqttClient = factory.CreateManagedMqttClient();
        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithTcpServer("node-api.packet.oarc.uk", 1883)
                .WithCleanSession(true)
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .Build())
            .Build();
        await mqttClient.SubscribeAsync("out/#");
        mqttClient.ApplicationMessageReceivedAsync += MessageReceived;
        await mqttClient.StartAsync(options);

        logger.LogInformation("DbWriter started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await mqttClient.UnsubscribeAsync("out/#");
    }



    private async Task MessageReceived(MqttApplicationMessageReceivedEventArgs args)
    {
        var type = args.ApplicationMessage.UserProperties.SingleOrDefault(p => p.Name == "type");

        logger.LogInformation("{type}: {payload}", type?.Value ?? "unknown", args.ApplicationMessage.ConvertPayloadToString());

        if (type!.Value == "L2Trace")
        {
            using var connection = Database.GetConnection();
            await connection.ExecuteAsync(
                "INSERT INTO traces (json) VALUES (@json)",
                new
                {
                    json = args.ApplicationMessage.ConvertPayloadToString()
                });

            logger.LogInformation("Inserted trace into database");
        }
        else if (type!.Value != "")
        {
            using var connection = Database.GetConnection();
            await connection.ExecuteAsync(
                "INSERT INTO events (json) VALUES (@json)",
                new
                {
                    json = args.ApplicationMessage.ConvertPayloadToString()
                });

            logger.LogInformation("Inserted event into database");
        }
    }
}
