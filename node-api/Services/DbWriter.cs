using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace node_api.Services;

public class DbWriter : BackgroundService
{
    private IManagedMqttClient? mqttClient;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        mqttClient = factory.CreateManagedMqttClient();
        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithTcpServer("node-api.packet.oarc.uk", 1883)
                .WithCleanSession(true)
                .Build())
            .Build();
        //await mqttClient.SubscribeAsync()
        mqttClient.ApplicationMessageReceivedAsync += MessageReceived;
        await mqttClient.StartAsync(options);
    }

    private async Task MessageReceived(MqttApplicationMessageReceivedEventArgs args)
    {
        throw new NotImplementedException();
    }
}
