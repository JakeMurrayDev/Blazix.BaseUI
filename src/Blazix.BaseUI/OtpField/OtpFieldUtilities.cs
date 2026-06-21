using System.Text;
using System.Text.RegularExpressions;

namespace Blazix.BaseUI.OtpField;

/// <summary>
/// Utility methods used by OTP field normalization and slot editing.
/// </summary>
public static partial class OtpFieldUtilities
{
    private sealed record ValidationConfig(
        string SlotPattern,
        Func<int, string> GetRootPattern,
        Regex RejectedCharacters,
        string InputMode);

    private static readonly ValidationConfig NumericConfig = new(
        SlotPattern: @"\d{1}",
        GetRootPattern: length => @$"\d{{{length}}}",
        RejectedCharacters: NumericRejectedRegex(),
        InputMode: "numeric");

    private static readonly ValidationConfig AlphaConfig = new(
        SlotPattern: "[a-zA-Z]{1}",
        GetRootPattern: length => $"[a-zA-Z]{{{length}}}",
        RejectedCharacters: AlphaRejectedRegex(),
        InputMode: "text");

    private static readonly ValidationConfig AlphanumericConfig = new(
        SlotPattern: "[a-zA-Z0-9]{1}",
        GetRootPattern: length => $"[a-zA-Z0-9]{{{length}}}",
        RejectedCharacters: AlphanumericRejectedRegex(),
        InputMode: "text");

    /// <summary>
    /// Removes all Unicode whitespace from an OTP value.
    /// </summary>
    /// <param name="value">The value to strip.</param>
    /// <returns>The value with all whitespace removed.</returns>
    public static string StripWhitespace(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        foreach (var rune in value.EnumerateRunes())
        {
            if (!Rune.IsWhiteSpace(rune))
                builder.Append(rune);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Normalizes an OTP value.
    /// </summary>
    public static string Normalize(
        string? value,
        int length,
        OtpFieldValidationType validationType,
        Func<string, string>? normalizeValue = null) =>
        NormalizeWithDetails(value, length, validationType, normalizeValue).Value;

    /// <summary>
    /// Normalizes an OTP value and reports whether characters were rejected.
    /// </summary>
    public static OtpFieldNormalizeResult NormalizeWithDetails(
        string? value,
        int length,
        OtpFieldValidationType validationType,
        Func<string, string>? normalizeValue = null)
    {
        var strippedValue = StripWhitespace(value);
        var validation = GetValidationConfig(validationType);
        var normalizedValue = ApplyValidation(strippedValue, validation);
        var didRejectCharacters = strippedValue.Length > normalizedValue.Length;

        if (normalizeValue is not null)
        {
            var customNormalizedValue = normalizeValue(normalizedValue) ?? string.Empty;
            didRejectCharacters |= normalizedValue.Length > customNormalizedValue.Length;
            normalizedValue = ApplyValidation(customNormalizedValue, validation);
            didRejectCharacters |= customNormalizedValue.Length > normalizedValue.Length;
        }

        var maxLength = Math.Max(length, 0);
        var normalizedRunes = normalizedValue.EnumerateRunes().ToArray();
        var clampedValue = JoinRunes(normalizedRunes.Take(maxLength));

        return new OtpFieldNormalizeResult(
            clampedValue,
            didRejectCharacters || normalizedRunes.Length > maxLength);
    }

    /// <summary>
    /// Replaces characters starting at the provided slot index and re-normalizes the final OTP value.
    /// </summary>
    public static string ReplaceValue(
        string currentValue,
        int index,
        string nextValue,
        int length,
        OtpFieldValidationType validationType,
        Func<string, string>? normalizeValue = null)
    {
        var normalizedValue = Normalize(nextValue, length, validationType, normalizeValue);
        var currentRunes = currentValue.EnumerateRunes().ToArray();
        var normalizedLength = GetCharacterLength(normalizedValue);
        var safeIndex = Math.Clamp(index, 0, currentRunes.Length);
        var prefix = JoinRunes(currentRunes.Take(safeIndex));
        var suffixStart = Math.Min(safeIndex + normalizedLength, currentRunes.Length);
        var suffix = JoinRunes(currentRunes.Skip(suffixStart));

        return Normalize($"{prefix}{normalizedValue}{suffix}", length, validationType, normalizeValue);
    }

    /// <summary>
    /// Removes one character from the OTP value.
    /// </summary>
    public static string RemoveCharacter(string currentValue, int index)
    {
        var currentRunes = currentValue.EnumerateRunes().ToArray();
        if (index < 0 || index >= currentRunes.Length)
            return currentValue;

        return $"{JoinRunes(currentRunes.Take(index))}{JoinRunes(currentRunes.Skip(index + 1))}";
    }

    internal static int GetCharacterLength(string? value) =>
        string.IsNullOrEmpty(value) ? 0 : value.EnumerateRunes().Count();

    internal static string GetCharacterAt(string value, int index)
    {
        if (index < 0)
            return string.Empty;

        var currentIndex = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            if (currentIndex == index)
                return rune.ToString();

            currentIndex++;
        }

        return string.Empty;
    }

    internal static string? GetSlotPattern(OtpFieldValidationType validationType) =>
        GetValidationConfig(validationType)?.SlotPattern;

    internal static string? GetRootPattern(OtpFieldValidationType validationType, int length) =>
        GetValidationConfig(validationType)?.GetRootPattern(length);

    internal static string? GetDefaultInputMode(OtpFieldValidationType validationType) =>
        GetValidationConfig(validationType)?.InputMode;

    private static string ApplyValidation(string value, ValidationConfig? validation) =>
        validation is null ? value : validation.RejectedCharacters.Replace(value, string.Empty);

    private static ValidationConfig? GetValidationConfig(OtpFieldValidationType validationType) =>
        validationType switch
        {
            OtpFieldValidationType.Numeric => NumericConfig,
            OtpFieldValidationType.Alpha => AlphaConfig,
            OtpFieldValidationType.Alphanumeric => AlphanumericConfig,
            OtpFieldValidationType.None => null,
            _ => NumericConfig
        };

    private static string JoinRunes(IEnumerable<Rune> runes)
    {
        var builder = new StringBuilder();
        foreach (var rune in runes)
            builder.Append(rune);

        return builder.ToString();
    }

    [GeneratedRegex(@"[^\d]", RegexOptions.CultureInvariant)]
    private static partial Regex NumericRejectedRegex();

    [GeneratedRegex(@"[^a-zA-Z]", RegexOptions.CultureInvariant)]
    private static partial Regex AlphaRejectedRegex();

    [GeneratedRegex(@"[^a-zA-Z0-9]", RegexOptions.CultureInvariant)]
    private static partial Regex AlphanumericRejectedRegex();
}

/// <summary>
/// Represents the result of OTP value normalization.
/// </summary>
/// <param name="Value">The normalized value.</param>
/// <param name="DidRejectCharacters">Whether any characters were rejected.</param>
public sealed record OtpFieldNormalizeResult(string Value, bool DidRejectCharacters);
