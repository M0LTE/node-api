using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using node_api.Models;
using node_api.Services;
using node_api.Validators;
using Scalar.AspNetCore;
using System.Net;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register FluentValidation validators
builder.Services.AddScoped<IValidator<L2Trace>, L2TraceValidator>();
builder.Services.AddScoped<IValidator<NodeUpEvent>, NodeUpEventValidator>();
builder.Services.AddScoped<IValidator<NodeDownEvent>, NodeDownEventValidator>();
builder.Services.AddScoped<IValidator<NodeStatusReportEvent>, NodeStatusReportEventValidator>();
builder.Services.AddScoped<IValidator<LinkUpEvent>, LinkUpEventValidator>();
builder.Services.AddScoped<IValidator<LinkDisconnectionEvent>, LinkDisconnectionEventValidator>();
builder.Services.AddScoped<IValidator<LinkStatus>, LinkStatusValidator>();
builder.Services.AddScoped<IValidator<CircuitUpEvent>, CircuitUpEventValidator>();
builder.Services.AddScoped<IValidator<CircuitDisconnectionEvent>, CircuitDisconnectionEventValidator>();
builder.Services.AddScoped<IValidator<CircuitStatus>, CircuitStatusValidator>();

// Register validation service
builder.Services.AddScoped<DatagramValidationService>();

builder.Services.AddHostedService<DbWriter>();
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
