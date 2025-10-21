using Dapper;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace node_api.Services;

/// <summary>
/// Helper for logging slow database queries.
/// </summary>
internal static partial class QueryLogger
{
    /// <summary>
    /// Executes a query and logs if it exceeds the slow query threshold.
    /// </summary>
    public static async Task<IEnumerable<T>> QueryWithLoggingAsync<T>(
        IDbConnection connection,
        CommandDefinition command,
        ILogger logger,
        int slowQueryThresholdMs)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await connection.QueryAsync<T>(command);
            sw.Stop();

            if (sw.ElapsedMilliseconds >= slowQueryThresholdMs)
            {
                LogSlowQuery(logger, command.CommandText, sw.ElapsedMilliseconds, command.Parameters);
            }

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            var sanitizedQuery = SanitizeSql(command.CommandText);
            logger.LogError(ex, "Query failed after {ElapsedMs}ms: {Query}",
                sw.ElapsedMilliseconds, sanitizedQuery);
            throw;
        }
    }

    /// <summary>
    /// Executes a scalar query and logs if it exceeds the slow query threshold.
    /// </summary>
    public static async Task<T> ExecuteScalarWithLoggingAsync<T>(
        IDbConnection connection,
        CommandDefinition command,
        ILogger logger,
        int slowQueryThresholdMs)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await connection.ExecuteScalarAsync<T>(command);
            sw.Stop();

            if (sw.ElapsedMilliseconds >= slowQueryThresholdMs)
            {
                LogSlowQuery(logger, command.CommandText, sw.ElapsedMilliseconds, command.Parameters);
            }

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            var sanitizedQuery = SanitizeSql(command.CommandText);
            logger.LogError(ex, "Query failed after {ElapsedMs}ms: {Query}",
                sw.ElapsedMilliseconds, sanitizedQuery);
            throw;
        }
    }

    private static void LogSlowQuery(ILogger logger, string query, long elapsedMs, object? parameters)
    {
        var sanitizedQuery = SanitizeSql(query);

        if (parameters is DynamicParameters dp)
        {
            var paramNames = dp.ParameterNames.ToList();
            var sanitizedParams = new Dictionary<string, string>();

            foreach (var name in paramNames)
            {
                var value = dp.Get<object>(name);
                sanitizedParams[name] = SanitizeParameterValue(value);
            }

            // Use structured logging with separate properties
            logger.LogWarning(
                "Slow query detected: {ElapsedMs}ms | Query: {Query} | Params: {ParamCount} | {ParameterDetails}",
                elapsedMs,
                sanitizedQuery,
                paramNames.Count,
                string.Join(", ", sanitizedParams.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        }
        else
        {
            logger.LogWarning(
                "Slow query detected: {ElapsedMs}ms | Query: {Query}",
                elapsedMs,
                sanitizedQuery);
        }
    }

    /// <summary>
    /// Sanitizes SQL query to a single line, removing extra whitespace and special characters that may cause issues with systemd journal.
    /// </summary>
    private static string SanitizeSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return string.Empty;

        // Replace newlines and multiple spaces with single space
        var sanitized = WhitespaceRegex().Replace(sql, " ");

        // Trim and remove any other control characters
        sanitized = sanitized.Trim();
        sanitized = ControlCharacterRegex().Replace(sanitized, string.Empty);

        return sanitized;
    }

    /// <summary>
    /// Sanitizes parameter values to avoid special characters in logs.
    /// </summary>
    private static string SanitizeParameterValue(object? value)
    {
        if (value == null)
            return "null";

        var stringValue = value.ToString() ?? "null";

        // Limit length to prevent huge log entries
        if (stringValue.Length > 100)
            stringValue = stringValue[..97] + "...";

        // Remove control characters and newlines
        stringValue = ControlCharacterRegex().Replace(stringValue, string.Empty);

        return stringValue;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"[\x00-\x1F\x7F]")]
    private static partial Regex ControlCharacterRegex();
}
