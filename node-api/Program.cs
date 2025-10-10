using node_api;
using node_api.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddHostedService<DbWriter>();
builder.Services.AddHostedService<UdpNodeInfoListener>();
builder.Services.AddSingleton<FramesRepo>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseAuthorization();

app.MapControllers();

app.Run();
