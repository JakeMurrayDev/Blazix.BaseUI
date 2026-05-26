using BlazorBaseUI.Field;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Autocomplete;

internal interface IAutocompleteItemRegistration
{
    object? ValueBoxed { get; }
    string? Label { get; }
    bool Disabled { get; }
    int? Index { get; }
    ElementReference? Element { get; }
    void RefreshFromRoot();
}

internal interface IAutocompleteRootContext
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
    bool Mounted { get; }
    bool ForceMounted { get; set; }
    bool InputInsidePopup { get; set; }
    bool InputOwnsFormValue { get; set; }
    bool KeyboardActive { get; set; }
    bool ListEmpty { get; }
    bool HasInputValue { get; }
    bool HasInputElement { get; }
    int ActiveIndex { get; }
    Side PopupSide { get; set; }
    Align PopupAlign { get; set; }
    bool AnchorHidden { get; set; }
    TransitionStatus TransitionStatus { get; }
    AutocompleteMode Mode { get; }
    AutocompleteAutoHighlight AutoHighlight { get; }
    AutocompleteChangeReason OpenChangeReason { get; }
    FieldRootState FieldState { get; }
    ElementReference? InputElement { get; }
    ElementReference? TriggerElement { get; }
    ElementReference? ListElement { get; }
    ElementReference? PopupElement { get; }
    ElementReference? PositionerElement { get; }

    string GetInputValue();
    string GetTypedInputValue();
    string GetFormValue();
    string? GetItemLabel(object? value, string? label);
    bool IsItemVisible(IAutocompleteItemRegistration item);
    bool IsItemHighlighted(IAutocompleteItemRegistration item);
    int RegisterItem(IAutocompleteItemRegistration item);
    void UnregisterItem(IAutocompleteItemRegistration item);
    int GetItemIndex(IAutocompleteItemRegistration item);
    int GetVisibleItemCount();
    void SetInputElement(ElementReference? element);
    void SetTriggerElement(ElementReference? element);
    void SetListElement(ElementReference? element);
    void SetPopupElement(ElementReference? element);
    void SetPositionerElement(ElementReference? element);
    Task SetOpenAsync(bool open, AutocompleteChangeReason reason);
    Task SetInputValueAsync(string value, AutocompleteChangeReason reason);
    Task HighlightIndexAsync(int index, AutocompleteHighlightReason reason);
    Task HighlightRelativeAsync(int delta, AutocompleteHighlightReason reason);
    Task CommitActiveItemAsync();
    Task CommitItemAsync(object? value, AutocompleteChangeReason reason);
    Task ClearAsync(AutocompleteChangeReason reason);
    void NotifyStateChanged();
    void NotifyItemMapChanged();
}
