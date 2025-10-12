using Dapper;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        await mqttClient.SubscribeAsync("in/udp/errored/#");
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
        if (args.ApplicationMessage.Topic.StartsWith("out/"))
        {
            await SaveOutputMessage(args);
        }

        if (args.ApplicationMessage.Topic.StartsWith("in/"))
        {
            await SaveInputMessage(args);
        }
    }

    private class ValidationError
    {
        [JsonPropertyName("datagram")]
        public required string Datagram { get; init; }

        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("errors")]
        public required List<ValidationErrorDetail> Errors { get; init; }

        public record ValidationErrorDetail
        {
            [JsonPropertyName("property")]
            public required string Property { get; init; }

            [JsonPropertyName("error")]
            public required string Error { get; init; }
        }
    }

    private async Task SaveInputMessage(MqttApplicationMessageReceivedEventArgs args)
    {
        if (!args.ApplicationMessage.Topic.StartsWith("in/udp/errored/"))
        {
            return;
        }

        try
        {
            var reason = args.ApplicationMessage.Topic.Split('/').Last();

            if (reason == "validation")
            {
                var obj = JsonSerializer.Deserialize<ValidationError>(args.ApplicationMessage.ConvertPayloadToString())
                    ?? throw new Exception("Failed to deserialize validation error");

                using var connection = Database.GetConnection();
                await connection.ExecuteAsync(
                    "INSERT INTO errored_messages (reason, datagram, type, errors, json) VALUES (@reason, @datagram, @type, @errors, @json)",
                    new
                    {
                        reason,
                        obj.Datagram,
                        obj.Type,
                        errors = string.Join("; ", obj.Errors.Select(e => $"{e.Property}: {e.Error}")),
                        type = "validation",
                        json = args.ApplicationMessage.ConvertPayloadToString()
                    });
            }
            else
            {
                using var connection = Database.GetConnection();
                await connection.ExecuteAsync(
                    "INSERT INTO errored_messages (reason, json) VALUES (@reason, @json)",
                    new
                    {
                        reason,
                        json = args.ApplicationMessage.ConvertPayloadToString()
                    });
            }

            logger.LogInformation("Saved errored message to database: {reason}", reason);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save errored message to database");
            return;
        }
    }

    private async Task SaveOutputMessage(MqttApplicationMessageReceivedEventArgs args)
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
