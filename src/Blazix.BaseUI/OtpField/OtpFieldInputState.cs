namespace Blazix.BaseUI.OtpField;

/// <summary>
/// Represents the state of an <see cref="OtpFieldInput"/> slot.
/// </summary>
public readonly record struct OtpFieldInputState(
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
    int Index,
    string Value)
{
    internal static OtpFieldInputState FromRoot(OtpFieldRootState rootState, int index, string value) => new(
        Complete: rootState.Complete,
        Disabled: rootState.Disabled,
        Valid: rootState.Valid,
        Touched: rootState.Touched,
        Dirty: rootState.Dirty,
        Filled: value.Length > 0,
        Focused: rootState.Focused,
        Length: rootState.Length,
        ReadOnly: rootState.ReadOnly,
        Required: rootState.Required,
        Index: index,
        Value: value);
}
