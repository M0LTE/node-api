using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Console;
using node_api.Models;
using node_api.Services;
using node_api.Validators;
using Scalar.AspNetCore;
using System.Net;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.FormatterName = ConsoleFormatterNames.Systemd;
});

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register UDP rate limiting
var rateLimitSettings = builder.Configuration.GetSection("UdpRateLimit").Get<UdpRateLimitSettings>() ?? new UdpRateLimitSettings();
builder.Services.AddSingleton(rateLimitSettings);
builder.Services.AddSingleton<IUdpRateLimitService, UdpRateLimitService>();

// Register GeoIP service
builder.Services.AddSingleton<IGeoIpService, GeoIpService>();

// Register MQTT client provider (must be initialized before DatagramProcessor)
builder.Services.AddSingleton<IMqttClientProvider, MqttClientProvider>();

// Register shared datagram processor
builder.Services.AddSingleton<IDatagramProcessor>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<DatagramProcessor>>();
    var validationService = serviceProvider.GetRequiredService<DatagramValidationService>();
    var rateLimitService = serviceProvider.GetRequiredService<IUdpRateLimitService>();
    var geoIpService = serviceProvider.GetRequiredService<IGeoIpService>();
    var mqttProvider = serviceProvider.GetRequiredService<IMqttClientProvider>();
    
    // Initialize MQTT client synchronously (it will be used immediately)
    mqttProvider.InitializeAsync().GetAwaiter().GetResult();
    
    var mqttClient = mqttProvider.GetClient();
    
    // Pass MQTT client to rate limit service so it can publish events
    rateLimitService.SetMqttClient(mqttClient);
    
    return new DatagramProcessor(logger, validationService, rateLimitService, geoIpService, mqttClient);
});

// Register RabbitMQ services for UDP datagram persistence
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<RabbitMqConsumer>();

// Register network state services
builder.Services.AddSingleton<INetworkStateService, NetworkStateService>();
builder.Services.AddSingleton<NetworkStateUpdater>();

// Register MySQL persistence for network state
builder.Services.AddSingleton<MySqlNetworkStateRepository>();
builder.Services.AddHostedService<NetworkStatePersistenceService>();

// Register MQTT subscriber to populate network state from MQTT events
builder.Services.AddHostedService<MqttStateSubscriber>();

// Register system metrics publisher
builder.Services.AddHostedService<SystemMetricsPublisher>();

// Register query frequency tracker for diagnostics
builder.Services.AddSingleton<QueryFrequencyTracker>();

// Register repositories
builder.Services.AddSingleton<ITraceRepository, MySqlTraceRepository>();
builder.Services.AddSingleton<IEventRepository, MySqlEventRepository>();
builder.Services.AddSingleton<MySqlErroredMessageRepository>();

// Register FluentValidation validators
builder.Services.AddSingleton<IValidator<L2Trace>, L2TraceValidator>();
builder.Services.AddSingleton<IValidator<NodeUpEvent>, NodeUpEventValidator>();
builder.Services.AddSingleton<IValidator<NodeDownEvent>, NodeDownEventValidator>();
builder.Services.AddSingleton<IValidator<NodeStatusReportEvent>, NodeStatusReportEventValidator>();
builder.Services.AddSingleton<IValidator<LinkUpEvent>, LinkUpEventValidator>();
builder.Services.AddSingleton<IValidator<LinkDisconnectionEvent>, LinkDisconnectionEventValidator>();
builder.Services.AddSingleton<IValidator<LinkStatus>, LinkStatusValidator>();
builder.Services.AddSingleton<IValidator<CircuitUpEvent>, CircuitUpEventValidator>();
builder.Services.AddSingleton<IValidator<CircuitDisconnectionEvent>, CircuitDisconnectionEventValidator>();
builder.Services.AddSingleton<IValidator<CircuitStatus>, CircuitStatusValidator>();

// Register validation service
builder.Services.AddSingleton<DatagramValidationService>();

builder.Services.AddHostedService<UdpNodeInfoListener>();

var app = builder.Build();

var options = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                      | ForwardedHeaders.XForwardedProto
                      | ForwardedHeaders.XForwardedHost
};
options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("172.17.0.0"), 16));

app.UseForwardedHeaders(options);

// Add no-cache headers to all responses
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});

// Enable CORS middleware
app.UseCors();

// Enable static files and default files
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi();
app.MapScalarApiReference();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }
