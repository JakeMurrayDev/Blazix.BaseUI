using Blazix.BaseUI.Field;
using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Combobox;

internal interface IComboboxItemRegistration
{
    object? ValueBoxed { get; }
    string? Label { get; }
    bool Disabled { get; }
    int? Index { get; }
    ElementReference? Element { get; }
    void RefreshFromRoot();
}

internal interface IComboboxRootContext
{
    event Action? StateChanged;
    event Action? ItemMapChanged;

    string RootId { get; }
    string? ListId { get; set; }
    string? PopupId { get; set; }
    string? LabelId { get; set; }
    string? Name { get; }
    string? Form { get; }
    bool Disabled { get; }
    bool ReadOnly { get; }
    bool Required { get; }
    bool Grid { get; }
    bool Inline { get; }
    bool Modal { get; }
    bool OpenOnInputClick { get; }
    bool HighlightItemOnHover { get; }
    bool KeepHighlight { get; }
    bool LoopFocus { get; }
    bool Multiple { get; }
    bool Mounted { get; }
    bool ForceMounted { get; set; }
    bool InputInsidePopup { get; set; }
    bool InputOwnsFormValue { get; set; }
    bool KeyboardActive { get; set; }
    bool ListEmpty { get; }
    bool HasSelectedValue { get; }
    bool HasInputValue { get; }
    bool HasInputElement { get; }
    int ActiveIndex { get; }
    Side PopupSide { get; set; }
    Align PopupAlign { get; set; }
    bool AnchorHidden { get; set; }
    TransitionStatus TransitionStatus { get; }
    ComboboxMode Mode { get; }
    ComboboxAutoHighlight AutoHighlight { get; }
    ComboboxChangeReason OpenChangeReason { get; }
    FieldRootState FieldState { get; }
    ElementReference? InputElement { get; }
    ElementReference? TriggerElement { get; }
    ElementReference? ListElement { get; }
    ElementReference? PopupElement { get; }
    ElementReference? PositionerElement { get; }

    string GetInputValue();
    string GetTypedInputValue();
    string? GetItemLabel(object? value, string? label);
    IReadOnlyList<object?> GetSelectedValuesBoxed();
    bool IsItemVisible(IComboboxItemRegistration item);
    bool IsItemHighlighted(IComboboxItemRegistration item);
    bool IsItemSelected(IComboboxItemRegistration item);
    int RegisterItem(IComboboxItemRegistration item);
    void UnregisterItem(IComboboxItemRegistration item);
    int GetItemIndex(IComboboxItemRegistration item);
    int GetVisibleItemCount();
    void SetInputElement(ElementReference? element);
    void SetTriggerElement(ElementReference? element);
    void SetListElement(ElementReference? element);
    void SetPopupElement(ElementReference? element);
    void SetPositionerElement(ElementReference? element);
    Task SetOpenAsync(bool open, ComboboxChangeReason reason);
    Task SetInputValueAsync(string value, ComboboxChangeReason reason);
    Task HighlightIndexAsync(int index, ComboboxHighlightReason reason);
    Task HighlightRelativeAsync(int delta, ComboboxHighlightReason reason);
    Task CommitActiveItemAsync();
    Task CommitItemAsync(object? value, ComboboxChangeReason reason);
    Task ClearAsync(ComboboxChangeReason reason);
    Task RemoveSelectedValueAtAsync(int index, ComboboxChangeReason reason);
    ValueTask FocusInputAsync();
    void NotifyStateChanged();
    void NotifyItemMapChanged();
}
