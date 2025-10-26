namespace node_api.Utilities;

/// <summary>
/// Validation utilities for common data formats
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Checks if a string is valid Base64 encoding
    /// </summary>
    /// <param name="s">The string to validate</param>
    /// <returns>True if the string is valid Base64, false otherwise</returns>
    public static bool IsValidBase64String(string s)
    {
        if (string.IsNullOrEmpty(s))
            return false;

        // Base64 should only contain A-Z, a-z, 0-9, +, /, and = for padding
        return s.All(c => char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '=');
    }
}
