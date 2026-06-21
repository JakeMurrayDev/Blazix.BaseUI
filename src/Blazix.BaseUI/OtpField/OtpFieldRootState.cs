namespace Blazix.BaseUI.OtpField;

/// <summary>
/// Represents the state of an <see cref="OtpFieldRoot"/> component.
/// </summary>
public readonly record struct OtpFieldRootState(
    bool Complete,
    bool Disabled,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused,
    int Length,
    bool ReadOnly,
    bool Required,
    string Value)
{
    internal static OtpFieldRootState Default { get; } = new(
        Complete: false,
        Disabled: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false,
        Length: 0,
        ReadOnly: false,
        Required: false,
        Value: string.Empty);
}
