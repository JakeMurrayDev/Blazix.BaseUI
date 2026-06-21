namespace Blazix.BaseUI.OtpField;

/// <summary>
/// Provides data for an OTP field value change.
/// </summary>
/// <param name="Value">The next normalized OTP value.</param>
/// <param name="Reason">The reason the value changed.</param>
public sealed class OtpFieldValueChangeEventArgs(string value, OtpFieldChangeReason reason) : EventArgs
{
    /// <summary>Gets the next normalized OTP value.</summary>
    public string Value { get; } = value;

    /// <summary>Gets the reason the value changed.</summary>
    public OtpFieldChangeReason Reason { get; } = reason;

    /// <summary>Gets whether the component's default value handling has been canceled.</summary>
    public bool IsCanceled { get; private set; }

    /// <summary>Gets whether the native event is allowed to propagate.</summary>
    public bool IsPropagationAllowed { get; private set; }

    /// <summary>Cancels the component's default value handling for this event.</summary>
    public void Cancel() => IsCanceled = true;

    /// <summary>Allows the native event to propagate in cases where Base UI would stop propagation.</summary>
    public void AllowPropagation() => IsPropagationAllowed = true;
}

/// <summary>
/// Provides data for rejected OTP input text.
/// </summary>
/// <param name="Value">The raw attempted text before normalization.</param>
/// <param name="Reason">The reason the text was processed.</param>
public sealed class OtpFieldValueInvalidEventArgs(string value, OtpFieldInvalidReason reason) : EventArgs
{
    /// <summary>Gets the raw attempted text before normalization.</summary>
    public string Value { get; } = value;

    /// <summary>Gets the reason the text was processed.</summary>
    public OtpFieldInvalidReason Reason { get; } = reason;
}

/// <summary>
/// Provides data for OTP completion.
/// </summary>
/// <param name="Value">The completed normalized OTP value.</param>
/// <param name="Reason">The reason the value became complete.</param>
public sealed class OtpFieldValueCompleteEventArgs(string value, OtpFieldCompleteReason reason) : EventArgs
{
    /// <summary>Gets the completed normalized OTP value.</summary>
    public string Value { get; } = value;

    /// <summary>Gets the reason the value became complete.</summary>
    public OtpFieldCompleteReason Reason { get; } = reason;
}

/// <summary>
/// Describes the result of an OTP field value commit.
/// </summary>
/// <param name="Committed">Whether a new value was accepted.</param>
/// <param name="Value">The current normalized OTP value after handling.</param>
/// <param name="FocusIndex">The slot index that should receive focus, if any.</param>
public sealed record OtpFieldCommitResult(bool Committed, string Value, int? FocusIndex);
