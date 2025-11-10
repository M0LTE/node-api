using MySql.Data.MySqlClient;

namespace node_api.Services;

public static class Database
{
    private static IConfiguration? _configuration;
    
    public static void Initialize(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public static readonly Lazy<string> ConnectionStringBuilder = new(static () =>
    {
        // First try configuration (includes User Secrets in Development)
        // Falls back to environment variables for production
        var host = _configuration?["DB_HOST"] ?? Environment.GetEnvironmentVariable("DB_HOST");
        var port = _configuration?["DB_PORT"] ?? Environment.GetEnvironmentVariable("DB_PORT");
        var user = _configuration?["DB_USER"] ?? Environment.GetEnvironmentVariable("DB_USER");
        var password = _configuration?["DB_PASSWORD"] ?? Environment.GetEnvironmentVariable("DB_PASSWORD");
        var database = _configuration?["DB_NAME"] ?? Environment.GetEnvironmentVariable("DB_NAME");
        
        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port) || 
            string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password) || 
            string.IsNullOrEmpty(database))
        {
            throw new InvalidOperationException(
                "Database configuration is missing. Set DB_HOST, DB_PORT, DB_USER, DB_PASSWORD, and DB_NAME " +
                "either in User Secrets (dotnet user-secrets set <key> <value>) or environment variables.");
        }
        
        return $"server={host};" +
            $"port={port};" +
            $"username={user};" +
            $"password={password};" +
            $"database={database};" +
            // Connection pooling (enabled by default, but explicit for clarity)
            "Pooling=true;" +
            "Min Pool Size=0;" +
            "Max Pool Size=100;" +
            // Connection lifetime - force connections to be recreated every 5 minutes
            // This ensures stale connections from a DB outage are replaced
            "Connection Lifetime=300;" +
            // Connection timeout - fail fast if DB is unreachable (10 seconds)
            "Connection Timeout=10;" +
            // Command timeout - prevent indefinite hangs (30 seconds)
            "Default Command Timeout=30;" +
            // Automatically retry commands that failed due to transient errors
            "Connection Reset=true;" +
            // Allow user variables (needed for some queries)
            "Allow User Variables=true";
    });

    public static MySqlConnection GetConnection(bool open = true)
    {
        var connection = new MySqlConnection(ConnectionStringBuilder.Value);
        if (open)
        {
            connection.Open();
        }
        return connection;
    }
}
