using node_api_ingester.Configuration;
using node_api_ingester.Services;

var builder = Host.CreateApplicationBuilder(args);

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
