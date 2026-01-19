using node_api_ingester.Configuration;
using node_api_ingester.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure MQTT settings from configuration with fallback to environment variable
builder.Services.Configure<MqttSettings>(options =>
{
    builder.Configuration.GetSection("MqttSettings").Bind(options);

    // Fallback to environment variable if password not set in config
    if (string.IsNullOrWhiteSpace(options.Password))
    {
        options.Password = Environment.GetEnvironmentVariable("MQTT_WRITER_PASSWORD");
    }
});

builder.Services.Configure<UdpNodeInfoListenerSettings>(options =>
{
    var port = Environment.GetEnvironmentVariable("UDP_PORT");
    if (string.IsNullOrWhiteSpace(port))
    {
        builder.Configuration.GetSection("UdpNodeInfoListenerSettings").Bind(options);
    }
    else
    {
        options.UdpPort = int.Parse(port);
    }
});

// Register in-memory datagram buffer (survives RabbitMQ outages)
builder.Services.AddSingleton<IDatagramBuffer, DatagramBuffer>();

// Register RabbitMQ services for UDP datagram persistence
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

// Register hosted services
builder.Services.AddHostedService<UdpNodeInfoListener>();
builder.Services.AddHostedService<DatagramPublisherService>();

var host = builder.Build();
host.Run();
