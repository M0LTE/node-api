namespace node_api.Services;

/// <summary>
/// Represents the result of an optional count operation.
/// </summary>
public sealed class CountResult
{
    private CountResult(long? value, string? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>
    /// The count value if successful, null otherwise.
    /// </summary>
    public long? Value { get; }

    /// <summary>
    /// Error message if the count operation failed.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// True if the count was successful.
    /// </summary>
    public bool IsSuccess => Value.HasValue && Error == null;

    /// <summary>
    /// Count was not requested.
    /// </summary>
    public static CountResult NotRequested => new(null, null);

    /// <summary>
    /// Count completed successfully.
    /// </summary>
    public static CountResult Success(long count) => new(count, null);

    /// <summary>
    /// Count operation failed with an error.
    /// </summary>
    public static CountResult Failed(string error) => new(null, error);
}
