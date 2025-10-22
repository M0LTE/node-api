using Microsoft.Extensions.Configuration;

namespace SmokeTests;

public class SmokeTestSettings
{
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public string UdpHost { get; set; } = "localhost";
    public int UdpPort { get; set; } = 13579;
    public string MqttHost { get; set; } = "node-api.packet.oarc.uk";
    public int MqttPort { get; set; } = 1883;
    public int TestTimeoutSeconds { get; set; } = 30;
}

public class SmokeTestFixture : IDisposable
{
    public SmokeTestSettings Settings { get; }
    public HttpClient HttpClient { get; }

    public SmokeTestFixture()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        Settings = new SmokeTestSettings();
        configuration.GetSection("SmokeTestSettings").Bind(Settings);

        HttpClient = new HttpClient
        {
            BaseAddress = new Uri(Settings.BaseUrl),
            Timeout = TimeSpan.FromSeconds(Settings.TestTimeoutSeconds)
        };
    }

    public void Dispose()
    {
        HttpClient?.Dispose();
    }
}
