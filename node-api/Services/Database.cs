using MySql.Data.MySqlClient;

namespace node_api.Services;

internal static class Database
{
    public static readonly Lazy<string> ConnectionStringBuilder = new(static () =>
    {
        return $"server={Environment.GetEnvironmentVariable("DB_HOST")};" +
            $"port={Environment.GetEnvironmentVariable("DB_PORT")};" +
            $"username={Environment.GetEnvironmentVariable("DB_USER")};" +
            $"password={Environment.GetEnvironmentVariable("DB_PASSWORD")};" +
            $"database={Environment.GetEnvironmentVariable("DB_NAME")}";
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