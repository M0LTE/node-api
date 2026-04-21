using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using node_api.Configuration;
using node_api.Services;
using Tests.Mocks;

namespace Tests.Integration;

/// <summary>
/// Test factory for integration tests
/// Creates a test version of the web application with in-memory dependencies
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Provide mock MQTT settings for tests
            services.Configure<MqttSettings>(options =>
            {
                options.Host = "test-broker";
                options.Port = 1883;
                options.Username = "test-user";
                options.Password = "test-password"; // Mock password for tests
                options.ClientIdPrefix = "test";
                options.AutoReconnectDelaySeconds = 5;
                options.CleanSession = true;
            });

            // Remove hosted services that create their own MQTT clients
            // These would try to connect during tests and cause disposal issues
            var hostedServicesToRemove = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .Where(d => d.ImplementationType == typeof(MqttStateSubscriber) ||
                           d.ImplementationType == typeof(SystemMetricsPublisher))
                .ToList();
            
            foreach (var service in hostedServicesToRemove)
            {
                services.Remove(service);
            }

            // Replace RabbitMQ publisher with mock for testing
            // This allows tests to run without a real RabbitMQ instance
            services.RemoveAll<IRabbitMqPublisher>();
            services.AddSingleton<IRabbitMqPublisher, MockRabbitMqPublisher>();
            
            // Note: RabbitMQ consumer is a hosted service, we don't need to mock it
            // since it only runs when RabbitMQ is configured via environment variables
            // (which won't be set in test environment)
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        builder.UseEnvironment("Testing");
    }
}
