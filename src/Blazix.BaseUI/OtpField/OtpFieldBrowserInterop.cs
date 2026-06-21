using System.Text.Json.Serialization;
using Blazix.BaseUI.Field;

namespace Blazix.BaseUI.OtpField;

/// <summary>
/// Provides keydown details passed from the OTP field browser interop module.
/// </summary>
public sealed class OtpFieldKeyDownDetails
{
    public string Key { get; set; } = string.Empty;
    public bool CtrlKey { get; set; }
    public bool MetaKey { get; set; }
    public bool AltKey { get; set; }
    public int? SelectionStart { get; set; }
    public int? SelectionEnd { get; set; }
    public string Value { get; set; } = string.Empty;
}

internal sealed record OtpFieldBrowserResult(
    [property: JsonPropertyName("handled")] bool Handled,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("focusIndex")] int? FocusIndex);

internal sealed record OtpFieldBrowserConfig(
    [property: JsonPropertyName("length")] int Length,
    [property: JsonPropertyName("disabled")] bool Disabled,
    [property: JsonPropertyName("readOnly")] bool ReadOnly);

internal sealed record OtpFieldJsValiditySnapshot(
    [property: JsonPropertyName("state")] FieldValidityState State,
    [property: JsonPropertyName("validationMessage")] string ValidationMessage)
{
    public FieldNativeValiditySnapshot ToNativeSnapshot() => new(State, ValidationMessage);
}
