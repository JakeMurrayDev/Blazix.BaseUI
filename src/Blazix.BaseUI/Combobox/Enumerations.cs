namespace Blazix.BaseUI.Combobox;

/// <summary>
/// Controls how combobox filtering and inline completion behave.
/// </summary>
public enum ComboboxMode
{
    /// <summary>Filter the list by the typed value without modifying the input from the active item.</summary>
    List,

    /// <summary>Filter the list by the typed value and temporarily show the active item in the input.</summary>
    Both,

    /// <summary>Keep the list static and temporarily show the active item in the input.</summary>
    Inline,

    /// <summary>Keep the list static and do not temporarily show the active item in the input.</summary>
    None
}

/// <summary>
/// Controls automatic item highlighting.
/// </summary>
public enum ComboboxAutoHighlight
{
    /// <summary>Do not automatically highlight items.</summary>
    False,

    /// <summary>Highlight after input changes and keep the highlight while the query changes.</summary>
    True,

    /// <summary>Always highlight the first available item.</summary>
    Always
}

/// <summary>
/// Describes why the combobox input value changed.
/// </summary>
public enum ComboboxChangeReason
{
    TriggerPress,
    OutsidePress,
    ItemPress,
    ChipRemovePress,
    ClosePress,
    EscapeKey,
    ListNavigation,
    FocusOut,
    InputChange,
    InputClear,
    ClearPress,
    None
}

/// <summary>
/// Describes why the combobox highlight changed.
/// </summary>
public enum ComboboxHighlightReason
{
    Keyboard,
    Pointer,
    None
}
