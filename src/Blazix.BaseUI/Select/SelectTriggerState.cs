using Blazix.BaseUI.Field;

namespace Blazix.BaseUI.Select;

/// <summary>
/// Represents the state of a <see cref="SelectTrigger"/> component.
/// </summary>
/// <param name="Open">Whether the corresponding select popup is open.</param>
/// <param name="Disabled">Whether the trigger is disabled.</param>
/// <param name="Placeholder">Whether no value is selected and a placeholder is shown.</param>
/// <param name="ReadOnly">Whether the trigger is read-only.</param>
/// <param name="Valid">Whether the trigger is in a valid state, or <see langword="null"/> if not validated.</param>
/// <param name="Touched">Whether the trigger has been touched.</param>
/// <param name="Dirty">Whether the trigger's value has changed from its initial value.</param>
/// <param name="Filled">Whether the trigger has a value (is filled).</param>
/// <param name="Focused">Whether the trigger is focused.</param>
/// <param name="PopupSide">Which side the popup is positioned relative to the trigger, or <see langword="null"/> when not mounted.</param>
/// <param name="Value">The currently selected value (boxed). Never emitted as a DOM attribute; exposed so
/// <see cref="SelectTrigger.Render"/> consumers can branch on the value.</param>
public readonly record struct SelectTriggerState(
    bool Open,
    bool Disabled,
    bool Placeholder,
    bool ReadOnly,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused,
    Side? PopupSide,
    object? Value)
{
    internal static SelectTriggerState FromFieldState(
        FieldRootState fieldState,
        bool isOpen,
        bool isDisabled,
        bool isPlaceholder,
        bool isReadOnly,
        Side? popupSide,
        object? value) => new(
            Open: isOpen,
            Disabled: isDisabled,
            Placeholder: isPlaceholder,
            ReadOnly: isReadOnly,
            Valid: fieldState.Valid,
            Touched: fieldState.Touched,
            Dirty: fieldState.Dirty,
            Filled: fieldState.Filled,
            Focused: fieldState.Focused,
            PopupSide: popupSide,
            Value: value);
}
