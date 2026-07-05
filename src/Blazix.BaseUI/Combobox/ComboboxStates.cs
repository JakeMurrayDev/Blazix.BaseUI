using Blazix.BaseUI.Field;

namespace Blazix.BaseUI.Combobox;

/// <summary>
/// Represents the root combobox state.
/// </summary>
/// <param name="Open">Whether the popup is open.</param>
public readonly record struct ComboboxRootState(bool Open);

/// <summary>
/// Represents the combobox input state.
/// </summary>
public readonly record struct ComboboxInputState(
    bool Disabled,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused,
    bool Open,
    Side? PopupSide,
    bool ListEmpty,
    bool ReadOnly)
{
    internal static ComboboxInputState FromFieldState(
        FieldRootState fieldState,
        bool open,
        bool disabled,
        bool readOnly,
        Side? popupSide,
        bool listEmpty)
    {
        return new ComboboxInputState(
            disabled,
            fieldState.Valid,
            fieldState.Touched,
            fieldState.Dirty,
            fieldState.Filled,
            fieldState.Focused,
            open,
            popupSide,
            listEmpty,
            readOnly);
    }
}

/// <summary>
/// Represents the combobox trigger state.
/// </summary>
public readonly record struct ComboboxTriggerState(
    bool Disabled,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused,
    bool Open,
    Side? PopupSide,
    bool ListEmpty,
    bool ReadOnly,
    bool Required)
{
    internal static ComboboxTriggerState FromFieldState(
        FieldRootState fieldState,
        bool open,
        bool disabled,
        bool readOnly,
        bool required,
        Side? popupSide,
        bool listEmpty)
    {
        return new ComboboxTriggerState(
            disabled,
            fieldState.Valid,
            fieldState.Touched,
            fieldState.Dirty,
            fieldState.Filled,
            fieldState.Focused,
            open,
            popupSide,
            listEmpty,
            readOnly,
            required);
    }
}

/// <summary>
/// Represents the combobox input group state.
/// </summary>
public readonly record struct ComboboxInputGroupState(
    bool Disabled,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused,
    bool Open,
    Side? PopupSide,
    bool ListEmpty,
    bool ReadOnly)
{
    internal static ComboboxInputGroupState FromFieldState(
        FieldRootState fieldState,
        bool open,
        bool disabled,
        bool readOnly,
        Side? popupSide,
        bool listEmpty)
    {
        return new ComboboxInputGroupState(
            disabled,
            fieldState.Valid,
            fieldState.Touched,
            fieldState.Dirty,
            fieldState.Filled,
            fieldState.Focused,
            open,
            popupSide,
            listEmpty,
            readOnly);
    }
}

/// <summary>
/// Represents the combobox list state.
/// </summary>
/// <param name="Empty">Whether the list is empty.</param>
public readonly record struct ComboboxListState(bool Empty);

/// <summary>
/// Represents the combobox popup state.
/// </summary>
public readonly record struct ComboboxPopupState(
    bool Open,
    Side Side,
    Align Align,
    bool AnchorHidden,
    TransitionStatus TransitionStatus,
    bool Empty);

/// <summary>
/// Represents the combobox positioner state.
/// </summary>
public readonly record struct ComboboxPositionerState(
    bool Open,
    Side Side,
    Align Align,
    bool AnchorHidden);

/// <summary>
/// Represents the combobox arrow state.
/// </summary>
public readonly record struct ComboboxArrowState(
    bool Open,
    Side Side,
    Align Align,
    bool Uncentered);

/// <summary>
/// Represents the combobox item state.
/// </summary>
/// <param name="Disabled">Whether the item is disabled.</param>
/// <param name="Highlighted">Whether the item is highlighted.</param>
/// <param name="Selected">Whether the item is selected.</param>
public readonly record struct ComboboxItemState(bool Disabled, bool Highlighted, bool Selected);

/// <summary>
/// Represents the combobox item indicator state.
/// </summary>
public readonly record struct ComboboxItemIndicatorState(
    bool Selected,
    TransitionStatus TransitionStatus);

/// <summary>
/// Represents the combobox chips container state.
/// </summary>
public readonly record struct ComboboxChipsState(bool HasChips);

/// <summary>
/// Represents a combobox selected-value chip state.
/// </summary>
public readonly record struct ComboboxChipState(bool Disabled, bool ReadOnly);

/// <summary>
/// Represents a combobox chip remove button state.
/// </summary>
public readonly record struct ComboboxChipRemoveState(bool Disabled);

/// <summary>
/// Represents the combobox clear button state.
/// </summary>
public readonly record struct ComboboxClearState(
    bool Open,
    bool Disabled,
    bool Visible,
    TransitionStatus TransitionStatus);

/// <summary>
/// Represents the combobox icon state.
/// </summary>
/// <param name="Open">Whether the popup is open.</param>
public readonly record struct ComboboxIconState(bool Open);

/// <summary>
/// Represents the combobox empty-state element.
/// </summary>
public readonly record struct ComboboxEmptyState;

/// <summary>
/// Represents the combobox status element.
/// </summary>
public readonly record struct ComboboxStatusState;

/// <summary>
/// Represents the combobox group element.
/// </summary>
public readonly record struct ComboboxGroupState;

/// <summary>
/// Represents the combobox group label element.
/// </summary>
public readonly record struct ComboboxGroupLabelState;

/// <summary>
/// Represents the combobox row element.
/// </summary>
public readonly record struct ComboboxRowState;

/// <summary>
/// Represents the combobox collection state.
/// </summary>
public readonly record struct ComboboxCollectionState;

/// <summary>
/// Represents the combobox value state.
/// </summary>
public readonly record struct ComboboxValueState;

/// <summary>
/// Represents the combobox backdrop state.
/// </summary>
public readonly record struct ComboboxBackdropState(bool Open, TransitionStatus TransitionStatus);

/// <summary>
/// Represents the combobox portal state.
/// </summary>
public readonly record struct ComboboxPortalState;

/// <summary>
/// Represents the combobox separator state.
/// </summary>
/// <param name="Orientation">The separator orientation.</param>
public readonly record struct ComboboxSeparatorState(Orientation Orientation);
