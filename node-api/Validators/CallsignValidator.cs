using System.Text.RegularExpressions;
using FluentValidation;

namespace node_api.Validators;

/// <summary>
/// Validator for packet radio callsigns.
/// Packet radio callsigns consist of:
/// - Base callsign: 1-6 alphanumeric characters (A-Z, 0-9)
/// - Optional SSID: hyphen followed by a number 0-15
/// Examples: G8PZT, M0LTE-15, GB7BBS, KIDDER-1, ID, QST, NODES
/// 
/// This validator accepts both standard amateur radio callsigns and commonly used
/// special/generic callsigns in packet radio networks (e.g., ID, QST, NODES, CQ, etc.)
/// </summary>
public static class CallsignValidator
{
    /// <summary>
    /// Regular expression pattern for validating packet radio callsigns.
    /// - Base part: 1-6 alphanumeric characters (uppercase letters and digits)
    /// - Optional SSID: hyphen followed by a number from 0 to 15
    /// </summary>
    private static readonly Regex CallsignPattern = new(
        @"^[A-Z0-9]{1,6}(-([0-9]|1[0-5]))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Validates that a string is a valid packet radio callsign.
    /// Accepts both standard amateur radio callsigns and commonly used special callsigns.
    /// </summary>
    /// <param name="callsign">The callsign to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(string? callsign)
    {
        if (string.IsNullOrWhiteSpace(callsign))
        {
            return false;
        }

        return CallsignPattern.IsMatch(callsign.Trim());
    }

    /// <summary>
    /// FluentValidation rule for validating callsigns.
    /// Usage: RuleFor(x => x.Callsign).Must(CallsignValidator.IsValid)
    /// </summary>
    public static IRuleBuilderOptions<T, string> MustBeValidCallsign<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(IsValid)
            .WithMessage("'{PropertyName}' must be a valid packet radio callsign (1-6 alphanumeric characters, optionally followed by -SSID where SSID is 0-15)");
    }

    /// <summary>
    /// FluentValidation rule for validating optional callsigns (nullable).
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidCallsignWhenNotNull<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(callsign => callsign == null || IsValid(callsign))
            .WithMessage("'{PropertyName}' must be a valid packet radio callsign (1-6 alphanumeric characters, optionally followed by -SSID where SSID is 0-15)");
    }
}
