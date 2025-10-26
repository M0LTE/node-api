using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Console;
using node_api.Models;
using node_api.Services;
using node_api.Validators;
using node_api.Middleware;
using Scalar.AspNetCore;
using System.Net;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.FormatterName = ConsoleFormatterNames.Systemd;
});

// Load security settings
var securitySettings = builder.Configuration.GetSection("Security").Get<SecuritySettings>() ?? new SecuritySettings();

// Add CORS services with configurable origins
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = securitySettings.CorsAllowedOrigins;
        
        if (allowedOrigins.Contains("*"))
        {
            // Allow any origin (development mode)
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // Restrict to specific origins (production mode)
            policy.WithOrigins(allowedOrigins.ToArray())
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

builder.Services.AddControllers();

// Add request body size limit
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB limit
});

builder.Services.AddOpenApi();

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

// Register repositories
builder.Services.AddSingleton<ITraceRepository, MySqlTraceRepository>();
builder.Services.AddSingleton<IEventRepository, MySqlEventRepository>();

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

if (Environment.MachineName != "PRECISION3660")
{
    builder.Services.AddHostedService<DbWriter>();
}
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

// Add security headers middleware
app.UseMiddleware<SecurityHeadersMiddleware>();

// Add API rate limiting middleware
app.UseMiddleware<ApiRateLimitingMiddleware>();

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
