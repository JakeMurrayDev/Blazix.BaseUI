using System.ComponentModel;

namespace Blazix.BaseUI.OtpField;

/// <summary>
/// Specifies the validation rule applied to an OTP field value.
/// </summary>
public enum OtpFieldValidationType
{
    /// <summary>Only decimal digits are accepted.</summary>
    Numeric,

    /// <summary>Only alphabetic characters are accepted.</summary>
    Alpha,

    /// <summary>Only alphabetic characters and decimal digits are accepted.</summary>
    Alphanumeric,

    /// <summary>No built-in character validation is applied.</summary>
    None
}

/// <summary>
/// Specifies why the OTP field value changed.
/// </summary>
public enum OtpFieldChangeReason
{
    /// <summary>The value changed because a slot input or hidden validation input changed.</summary>
    InputChange,

    /// <summary>The value changed because a slot was cleared by input.</summary>
    InputClear,

    /// <summary>The value changed because text was pasted.</summary>
    InputPaste,

    /// <summary>The value changed because of a keyboard command.</summary>
    Keyboard
}

/// <summary>
/// Specifies why an OTP value became complete.
/// </summary>
public enum OtpFieldCompleteReason
{
    /// <summary>The value became complete through input.</summary>
    InputChange,

    /// <summary>The value became complete through paste.</summary>
    InputPaste
}

/// <summary>
/// Specifies why entered text was rejected before committing an OTP value.
/// </summary>
public enum OtpFieldInvalidReason
{
    /// <summary>Characters were rejected during input.</summary>
    InputChange,

    /// <summary>Characters were rejected during paste.</summary>
    InputPaste
}

internal static class OtpFieldEnumExtensions
{
    extension(OtpFieldValidationType validationType)
    {
        public string ToDataString() =>
            validationType switch
            {
                OtpFieldValidationType.Numeric => "numeric",
                OtpFieldValidationType.Alpha => "alpha",
                OtpFieldValidationType.Alphanumeric => "alphanumeric",
                OtpFieldValidationType.None => "none",
                _ => throw new InvalidEnumArgumentException(nameof(validationType), (int)validationType, typeof(OtpFieldValidationType))
            };
    }

    extension(OtpFieldChangeReason reason)
    {
        public string ToDataString() =>
            reason switch
            {
                OtpFieldChangeReason.InputChange => "input-change",
                OtpFieldChangeReason.InputClear => "input-clear",
                OtpFieldChangeReason.InputPaste => "input-paste",
                OtpFieldChangeReason.Keyboard => "keyboard",
                _ => throw new InvalidEnumArgumentException(nameof(reason), (int)reason, typeof(OtpFieldChangeReason))
            };
    }
}
