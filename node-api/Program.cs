using Microsoft.AspNetCore.HttpOverrides;
using node_api.Services;
using Scalar.AspNetCore;
using System.Net;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
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
