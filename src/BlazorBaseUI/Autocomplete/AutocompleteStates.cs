using BlazorBaseUI.Field;

namespace BlazorBaseUI.Autocomplete;

/// <summary>
/// Represents the root autocomplete state.
/// </summary>
/// <param name="Open">Whether the popup is open.</param>
public readonly record struct AutocompleteRootState(bool Open);

/// <summary>
/// Represents the autocomplete input state.
/// </summary>
public readonly record struct AutocompleteInputState(
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
    internal static AutocompleteInputState FromFieldState(
        FieldRootState fieldState,
        bool open,
        bool disabled,
        bool readOnly,
        Side? popupSide,
        bool listEmpty)
    {
        return new AutocompleteInputState(
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
/// Represents the autocomplete trigger state.
/// </summary>
public readonly record struct AutocompleteTriggerState(
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
    internal static AutocompleteTriggerState FromFieldState(
        FieldRootState fieldState,
        bool open,
        bool disabled,
        bool readOnly,
        bool required,
        Side? popupSide,
        bool listEmpty)
    {
        return new AutocompleteTriggerState(
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
/// Represents the autocomplete input group state.
/// </summary>
public readonly record struct AutocompleteInputGroupState(
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
    internal static AutocompleteInputGroupState FromFieldState(
        FieldRootState fieldState,
        bool open,
        bool disabled,
        bool readOnly,
        Side? popupSide,
        bool listEmpty)
    {
        return new AutocompleteInputGroupState(
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
/// Represents the autocomplete list state.
/// </summary>
/// <param name="Empty">Whether the list is empty.</param>
public readonly record struct AutocompleteListState(bool Empty);

/// <summary>
/// Represents the autocomplete popup state.
/// </summary>
public readonly record struct AutocompletePopupState(
    bool Open,
    Side Side,
    Align Align,
    bool AnchorHidden,
    TransitionStatus TransitionStatus,
    bool Empty);

/// <summary>
/// Represents the autocomplete positioner state.
/// </summary>
public readonly record struct AutocompletePositionerState(
    bool Open,
    Side Side,
    Align Align,
    bool AnchorHidden);

/// <summary>
/// Represents the autocomplete arrow state.
/// </summary>
public readonly record struct AutocompleteArrowState(
    bool Open,
    Side Side,
    Align Align,
    bool Uncentered);

/// <summary>
/// Represents the autocomplete item state.
/// </summary>
/// <param name="Disabled">Whether the item is disabled.</param>
/// <param name="Highlighted">Whether the item is highlighted.</param>
public readonly record struct AutocompleteItemState(bool Disabled, bool Highlighted);

/// <summary>
/// Represents the autocomplete clear button state.
/// </summary>
public readonly record struct AutocompleteClearState(
    bool Open,
    bool Disabled,
    bool Visible,
    TransitionStatus TransitionStatus);

/// <summary>
/// Represents the autocomplete icon state.
/// </summary>
/// <param name="Open">Whether the popup is open.</param>
public readonly record struct AutocompleteIconState(bool Open);

/// <summary>
/// Represents the autocomplete empty-state element.
/// </summary>
public readonly record struct AutocompleteEmptyState;

/// <summary>
/// Represents the autocomplete status element.
/// </summary>
public readonly record struct AutocompleteStatusState;

/// <summary>
/// Represents the autocomplete group element.
/// </summary>
public readonly record struct AutocompleteGroupState;

/// <summary>
/// Represents the autocomplete group label element.
/// </summary>
public readonly record struct AutocompleteGroupLabelState;

/// <summary>
/// Represents the autocomplete row element.
/// </summary>
public readonly record struct AutocompleteRowState;

/// <summary>
/// Represents the autocomplete collection state.
/// </summary>
public readonly record struct AutocompleteCollectionState;

/// <summary>
/// Represents the autocomplete value state.
/// </summary>
public readonly record struct AutocompleteValueState;

/// <summary>
/// Represents the autocomplete backdrop state.
/// </summary>
public readonly record struct AutocompleteBackdropState(bool Open, TransitionStatus TransitionStatus);

/// <summary>
/// Represents the autocomplete portal state.
/// </summary>
public readonly record struct AutocompletePortalState;

/// <summary>
/// Represents the autocomplete separator state.
/// </summary>
/// <param name="Orientation">The separator orientation.</param>
public readonly record struct AutocompleteSeparatorState(Orientation Orientation);
