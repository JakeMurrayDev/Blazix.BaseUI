using Blazix.BaseUI.Field;
using Microsoft.AspNetCore.Components;
using System.Collections.Concurrent;
using System.Reflection;

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

internal sealed class ComboboxRootContext<TValue> : IComboboxRootContext, IDisposable
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> labelPropertyCache = new();
    private readonly List<IComboboxItemRegistration> registeredItems = new();

    public event Action? StateChanged;
    public event Action? ItemMapChanged;

    public string RootId { get; init; } = null!;
    public string? ListId { get; set; }
    public string? PopupId { get; set; }
    public string? LabelId { get; set; }
    public string? Name { get; set; }
    public string? Form { get; set; }
    public bool Disabled { get; set; }
    public bool ReadOnly { get; set; }
    public bool Required { get; set; }
    public bool Grid { get; set; }
    public bool Virtualized { get; set; }
    public bool Inline { get; set; }
    public bool Modal { get; set; }
    public bool OpenOnInputClick { get; set; }
    public bool HighlightItemOnHover { get; set; }
    public bool KeepHighlight { get; set; }
    public bool LoopFocus { get; set; }
    public bool Multiple { get; set; }
    public bool Mounted { get; set; }
    public bool ForceMounted { get; set; }
    public bool InputInsidePopup { get; set; } = true;
    public bool InputOwnsFormValue { get; set; }
    public bool KeyboardActive { get; set; } = true;
    public bool HasInputValue { get; set; }
    public bool HasInputElement => InputElement.HasValue;
    public bool HasSelectedValue => Multiple ? GetSelectedValues().Count > 0 : GetSelectedValue() is not null;
    public int ActiveIndex { get; set; } = -1;
    public Side PopupSide { get; set; } = Side.Bottom;
    public Align PopupAlign { get; set; } = Align.Center;
    public bool AnchorHidden { get; set; }
    public TransitionStatus TransitionStatus { get; set; } = TransitionStatus.Undefined;
    public ComboboxMode Mode { get; set; } = ComboboxMode.List;
    public ComboboxAutoHighlight AutoHighlight { get; set; }
    public ComboboxChangeReason OpenChangeReason { get; set; } = ComboboxChangeReason.None;
    public FieldRootState FieldState { get; set; } = FieldRootState.Default;
    public ElementReference? InputElement { get; private set; }
    public ElementReference? TriggerElement { get; private set; }
    public ElementReference? ListElement { get; private set; }
    public ElementReference? PopupElement { get; private set; }
    public ElementReference? PositionerElement { get; private set; }
    public IReadOnlyList<TValue>? Items { get; set; }
    public IReadOnlyList<ComboboxOptionGroup<TValue>>? ItemGroups { get; set; }
    public IReadOnlyList<TValue>? FilteredItems { get; set; }
    public int Limit { get; set; } = -1;
    public Func<TValue, string, Func<TValue, string?>?, bool>? Filter { get; set; }
    public bool FilterDisabled { get; set; }
    public Func<TValue?, string?>? ItemToStringLabel { get; set; }
    public Func<TValue?, string?>? ItemToStringValue { get; set; }
    public Func<TValue, TValue, bool>? IsItemEqualToValue { get; set; }
    public Func<string> GetInputValueFunc { get; set; } = () => string.Empty;
    public Func<string> GetTypedInputValueFunc { get; set; } = () => string.Empty;
    public Func<TValue?> GetSelectedValueFunc { get; set; } = () => default;
    public Func<List<TValue>> GetSelectedValuesFunc { get; set; } = () => [];
    public Func<bool, ComboboxChangeReason, Task> SetOpenAsyncFunc { get; set; } = (_, _) => Task.CompletedTask;
    public Func<string, ComboboxChangeReason, Task> SetInputValueAsyncFunc { get; set; } = (_, _) => Task.CompletedTask;
    public Func<TValue?, ComboboxChangeReason, Task> SetSelectedValueAsyncFunc { get; set; } = (_, _) => Task.CompletedTask;
    public Func<IReadOnlyList<TValue>, ComboboxChangeReason, Task> SetSelectedValuesAsyncFunc { get; set; } = (_, _) => Task.CompletedTask;
    public Func<int, ComboboxHighlightReason, Task> HighlightIndexAsyncFunc { get; set; } = (_, _) => Task.CompletedTask;
    public Func<object?, ComboboxChangeReason, Task> CommitItemAsyncFunc { get; set; } = (_, _) => Task.CompletedTask;
    public Func<ComboboxChangeReason, Task> ClearAsyncFunc { get; set; } = _ => Task.CompletedTask;
    public Func<int, ComboboxChangeReason, Task> RemoveSelectedValueAtAsyncFunc { get; set; } = (_, _) => Task.CompletedTask;
    public Func<ValueTask> FocusInputAsyncFunc { get; set; } = () => ValueTask.CompletedTask;

    public bool ListEmpty => GetVisibleItemCount() == 0;

    public string GetInputValue() => GetInputValueFunc();

    public string GetTypedInputValue() => GetTypedInputValueFunc();

    public TValue? GetSelectedValue() => GetSelectedValueFunc();

    public List<TValue> GetSelectedValues() => GetSelectedValuesFunc();

    public IReadOnlyList<object?> GetSelectedValuesBoxed() => GetSelectedValues().Cast<object?>().ToList();

    public string? GetItemLabel(object? value, string? label)
    {
        if (!string.IsNullOrEmpty(label))
        {
            return label;
        }

        if (value is TValue typed)
        {
            return Stringify(typed);
        }

        return value?.ToString();
    }

    public bool IsItemVisible(IComboboxItemRegistration item)
    {
        var filtered = GetFlatFilteredItems();
        if (filtered.Count == 0)
        {
            return false;
        }

        if (item.ValueBoxed is not TValue typedValue)
        {
            return filtered.Any(v => Equals(v, item.ValueBoxed));
        }

        return filtered.Any(v => AreValuesEqual(v, typedValue));
    }

    public bool IsItemHighlighted(IComboboxItemRegistration item)
    {
        return GetItemIndex(item) == ActiveIndex;
    }

    public bool IsItemSelected(IComboboxItemRegistration item)
    {
        if (item.ValueBoxed is not TValue typedValue)
        {
            return false;
        }

        if (Multiple)
        {
            return GetSelectedValues().Any(selectedValue => AreValuesEqual(selectedValue, typedValue));
        }

        var selected = GetSelectedValue();
        return selected is not null && AreValuesEqual(typedValue, selected);
    }

    public int RegisterItem(IComboboxItemRegistration item)
    {
        if (!registeredItems.Contains(item))
        {
            registeredItems.Add(item);
            NotifyItemMapChanged();
        }

        return GetItemIndex(item);
    }

    public void UnregisterItem(IComboboxItemRegistration item)
    {
        if (registeredItems.Remove(item))
        {
                if (!Virtualized && ActiveIndex >= GetVisibleItemCount())
                {
                    ActiveIndex = -1;
                }

            NotifyItemMapChanged();
        }
    }

    public int GetItemIndex(IComboboxItemRegistration item)
    {
        if (item.Index.HasValue)
        {
            return item.Index.Value;
        }

        var visible = GetVisibleRegisteredItems();
        return visible.IndexOf(item);
    }

    public int GetVisibleItemCount()
    {
        if (Items is not null || ItemGroups is not null || FilteredItems is not null)
        {
            return GetFlatFilteredItems().Count;
        }

        return GetVisibleRegisteredItems().Count;
    }

    public void SetInputElement(ElementReference? element)
    {
        InputElement = element;
    }

    public void SetTriggerElement(ElementReference? element)
    {
        TriggerElement = element;
    }

    public void SetListElement(ElementReference? element)
    {
        ListElement = element;
    }

    public void SetPopupElement(ElementReference? element)
    {
        PopupElement = element;
    }

    public void SetPositionerElement(ElementReference? element)
    {
        PositionerElement = element;
    }

    public Task SetOpenAsync(bool open, ComboboxChangeReason reason) => SetOpenAsyncFunc(open, reason);

    public Task SetInputValueAsync(string value, ComboboxChangeReason reason) => SetInputValueAsyncFunc(value, reason);

    public Task SetSelectedValueAsync(TValue? value, ComboboxChangeReason reason) => SetSelectedValueAsyncFunc(value, reason);

    public Task SetSelectedValuesAsync(IReadOnlyList<TValue> values, ComboboxChangeReason reason) => SetSelectedValuesAsyncFunc(values, reason);

    public Task HighlightIndexAsync(int index, ComboboxHighlightReason reason) => HighlightIndexAsyncFunc(index, reason);

    public Task HighlightRelativeAsync(int delta, ComboboxHighlightReason reason)
    {
        var count = GetVisibleItemCount();
        if (count == 0)
        {
            return HighlightIndexAsync(-1, reason);
        }

        var nextIndex = ActiveIndex + delta;
        if (nextIndex < 0)
        {
            nextIndex = LoopFocus ? count - 1 : 0;
        }
        else if (nextIndex >= count)
        {
            nextIndex = LoopFocus ? 0 : count - 1;
        }

        return HighlightIndexAsync(nextIndex, reason);
    }

    public async Task CommitActiveItemAsync()
    {
        if (ActiveIndex < 0)
        {
            return;
        }

        var value = GetValueAtVisibleIndex(ActiveIndex);
        if (value.Found)
        {
            await CommitItemAsync(value.Value, ComboboxChangeReason.ItemPress);
        }
    }

    public Task CommitItemAsync(object? value, ComboboxChangeReason reason) => CommitItemAsyncFunc(value, reason);

    public Task ClearAsync(ComboboxChangeReason reason) => ClearAsyncFunc(reason);

    public Task RemoveSelectedValueAtAsync(int index, ComboboxChangeReason reason) => RemoveSelectedValueAtAsyncFunc(index, reason);

    public ValueTask FocusInputAsync() => FocusInputAsyncFunc();

    public void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }

    public void NotifyItemMapChanged()
    {
        ItemMapChanged?.Invoke();
        StateChanged?.Invoke();
    }

    public void Dispose()
    {
        StateChanged = null;
        ItemMapChanged = null;
        registeredItems.Clear();
    }

    internal List<TValue> GetFlatFilteredItems()
    {
        if (FilteredItems is not null)
        {
            return ApplyLimit(FilteredItems).ToList();
        }

        var source = GetFlatSourceItems();
        if (Mode is ComboboxMode.Inline or ComboboxMode.None || FilterDisabled)
        {
            return ApplyLimit(source).ToList();
        }

        var query = GetTypedInputValue().Trim();
        if (query.Length == 0)
        {
            return ApplyLimit(source).ToList();
        }

        var result = new List<TValue>();
        foreach (var item in source)
        {
            if (Limit > -1 && result.Count >= Limit)
            {
                break;
            }

            if (Matches(item, query))
            {
                result.Add(item);
            }
        }

        return result;
    }

    internal List<TValue> GetAllKnownItems()
    {
        var all = GetFlatSourceItems().ToList();
        foreach (var item in registeredItems)
        {
            if (item.ValueBoxed is TValue typedValue && !all.Any(value => AreValuesEqual(value, typedValue)))
            {
                all.Add(typedValue);
            }
        }

        return all;
    }

    internal (bool Found, object? Value) GetValueAtVisibleIndex(int index)
    {
        if (index < 0)
        {
            return (false, null);
        }

        var source = GetFlatFilteredItems();
        if (index < source.Count)
        {
            return (true, source[index]);
        }

        var visible = GetVisibleRegisteredItems();
        if (index < visible.Count)
        {
            return (true, visible[index].ValueBoxed);
        }

        return (false, null);
    }

    internal string Stringify(TValue? value)
    {
        if (ItemToStringLabel is not null)
        {
            return ItemToStringLabel(value) ?? string.Empty;
        }

        if (value is null)
        {
            return string.Empty;
        }

        var type = value.GetType();
        var labelProperty = labelPropertyCache.GetOrAdd(type, static candidateType =>
            candidateType.GetProperty("Label") ?? candidateType.GetProperty("label"));
        if (labelProperty?.GetValue(value) is { } label)
        {
            return label.ToString() ?? string.Empty;
        }

        return value.ToString() ?? string.Empty;
    }

    private List<IComboboxItemRegistration> GetVisibleRegisteredItems()
    {
        var result = new List<IComboboxItemRegistration>();
        foreach (var item in registeredItems)
        {
            if (!item.Disabled && MatchesRegisteredItem(item))
            {
                result.Add(item);
            }
        }

        return result;
    }

    private IEnumerable<TValue> ApplyLimit(IEnumerable<TValue> values)
    {
        return Limit > -1 ? values.Take(Limit) : values;
    }

    private IReadOnlyList<TValue> GetFlatSourceItems()
    {
        if (Items is not null)
        {
            return Items;
        }

        if (ItemGroups is not null)
        {
            return ItemGroups.SelectMany(group => group.Items).ToList();
        }

        return registeredItems
            .Where(item => item.ValueBoxed is TValue)
            .Select(item => (TValue)item.ValueBoxed!)
            .ToList();
    }

    private bool MatchesRegisteredItem(IComboboxItemRegistration item)
    {
        if (Mode is ComboboxMode.Inline or ComboboxMode.None || FilterDisabled)
        {
            return true;
        }

        var query = GetTypedInputValue().Trim();
        if (query.Length == 0)
        {
            return true;
        }

        if (item.ValueBoxed is TValue typedValue && Filter is not null)
        {
            return Filter(typedValue, query, Stringify);
        }

        var label = GetItemLabel(item.ValueBoxed, item.Label) ?? string.Empty;
        return label.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    private bool Matches(TValue item, string query)
    {
        if (Filter is not null)
        {
            return Filter(item, query, Stringify);
        }

        var label = Stringify(item);
        return label.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    private bool AreValuesEqual(TValue left, TValue right)
    {
        return IsItemEqualToValue?.Invoke(left, right) ?? EqualityComparer<TValue>.Default.Equals(left, right);
    }
}
