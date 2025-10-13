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

builder.Services.AddControllers();
builder.Services.AddOpenApi();

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

if (Environment.MachineName == "node-api")
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
app.MapOpenApi();
app.MapScalarApiReference();
app.MapGet("/", () => Results.Redirect("/scalar"));
app.UseAuthorization();
app.MapControllers();

app.Run();
