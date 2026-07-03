using Blazix.BaseUI.Field;
using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Autocomplete;

internal sealed class AutocompleteRootContext<TValue> : IAutocompleteRootContext, IDisposable
{
    private readonly List<IAutocompleteItemRegistration> registeredItems = new();

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
    public bool Mounted { get; set; }
    public bool ForceMounted { get; set; }
    public bool InputInsidePopup { get; set; } = true;
    public bool InputOwnsFormValue { get; set; }
    public bool KeyboardActive { get; set; } = true;
    public bool HasInputValue { get; set; }
    public bool HasInputElement => InputElement.HasValue;
    public int ActiveIndex { get; set; } = -1;
    public Side PopupSide { get; set; } = Side.Bottom;
    public Align PopupAlign { get; set; } = Align.Center;
    public bool AnchorHidden { get; set; }
    public TransitionStatus TransitionStatus { get; set; } = TransitionStatus.Undefined;
    public AutocompleteMode Mode { get; set; } = AutocompleteMode.List;
    public AutocompleteAutoHighlight AutoHighlight { get; set; }
    public AutocompleteChangeReason OpenChangeReason { get; set; } = AutocompleteChangeReason.None;
    public FieldRootState FieldState { get; set; } = FieldRootState.Default;
    public ElementReference? InputElement { get; private set; }
    public ElementReference? TriggerElement { get; private set; }
    public ElementReference? ListElement { get; private set; }
    public ElementReference? PopupElement { get; private set; }
    public ElementReference? PositionerElement { get; private set; }
    public IReadOnlyList<TValue>? Items { get; set; }
    public IReadOnlyList<AutocompleteOptionGroup<TValue>>? ItemGroups { get; set; }
    public IReadOnlyList<TValue>? FilteredItems { get; set; }
    public int Limit { get; set; } = -1;
    public Func<TValue, string, Func<TValue, string?>?, bool>? Filter { get; set; }
    public bool FilterDisabled { get; set; }
    public Func<TValue?, string?>? ItemToStringValue { get; set; }
    public Func<TValue, TValue, bool>? IsItemEqualToValue { get; set; }
    public Func<string> GetInputValueFunc { get; set; } = () => string.Empty;
    public Func<string> GetTypedInputValueFunc { get; set; } = () => string.Empty;
    public Func<string> GetFormValueFunc { get; set; } = () => string.Empty;
    public Func<bool, AutocompleteChangeReason, Task> SetOpenAsyncFunc { get; set; } = (_, _) => Task.CompletedTask;
    public Func<string, AutocompleteChangeReason, Task> SetInputValueAsyncFunc { get; set; } = (_, _) => Task.CompletedTask;
    public Func<int, AutocompleteHighlightReason, Task> HighlightIndexAsyncFunc { get; set; } = (_, _) => Task.CompletedTask;
    public Func<object?, AutocompleteChangeReason, Task> CommitItemAsyncFunc { get; set; } = (_, _) => Task.CompletedTask;
    public Func<AutocompleteChangeReason, Task> ClearAsyncFunc { get; set; } = _ => Task.CompletedTask;
    public Func<ValueTask> FocusInputAsyncFunc { get; set; } = () => ValueTask.CompletedTask;

    public bool ListEmpty => GetVisibleItemCount() == 0;

    public string GetInputValue() => GetInputValueFunc();

    public string GetTypedInputValue() => GetTypedInputValueFunc();

    public string GetFormValue() => GetFormValueFunc();

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

    public bool IsItemVisible(IAutocompleteItemRegistration item)
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

    public bool IsItemHighlighted(IAutocompleteItemRegistration item)
    {
        return GetItemIndex(item) == ActiveIndex;
    }

    public int RegisterItem(IAutocompleteItemRegistration item)
    {
        if (!registeredItems.Contains(item))
        {
            registeredItems.Add(item);
            NotifyItemMapChanged();
        }

        return GetItemIndex(item);
    }

    public void UnregisterItem(IAutocompleteItemRegistration item)
    {
        if (registeredItems.Remove(item))
        {
            if (!Virtualized && ActiveIndex >= registeredItems.Count)
            {
                ActiveIndex = -1;
            }

            NotifyItemMapChanged();
        }
    }

    public int GetItemIndex(IAutocompleteItemRegistration item)
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

    public Task SetOpenAsync(bool open, AutocompleteChangeReason reason) => SetOpenAsyncFunc(open, reason);

    public Task SetInputValueAsync(string value, AutocompleteChangeReason reason) => SetInputValueAsyncFunc(value, reason);

    public Task HighlightIndexAsync(int index, AutocompleteHighlightReason reason) => HighlightIndexAsyncFunc(index, reason);

    public Task HighlightRelativeAsync(int delta, AutocompleteHighlightReason reason)
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
            await CommitItemAsync(value.Value, AutocompleteChangeReason.ItemPress);
        }
    }

    public Task CommitItemAsync(object? value, AutocompleteChangeReason reason) => CommitItemAsyncFunc(value, reason);

    public Task ClearAsync(AutocompleteChangeReason reason) => ClearAsyncFunc(reason);

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
        if (Mode is AutocompleteMode.Inline or AutocompleteMode.None || FilterDisabled)
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
        if (ItemToStringValue is not null)
        {
            return ItemToStringValue(value) ?? string.Empty;
        }

        if (value is null)
        {
            return string.Empty;
        }

        var type = value.GetType();
        var labelProperty = type.GetProperty("Label") ?? type.GetProperty("label");
        if (labelProperty?.GetValue(value) is { } label)
        {
            return label.ToString() ?? string.Empty;
        }

        return value.ToString() ?? string.Empty;
    }

    private List<IAutocompleteItemRegistration> GetVisibleRegisteredItems()
    {
        var result = new List<IAutocompleteItemRegistration>();
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

    private bool MatchesRegisteredItem(IAutocompleteItemRegistration item)
    {
        if (Mode is AutocompleteMode.Inline or AutocompleteMode.None || FilterDisabled)
        {
            return true;
        }

        var query = GetTypedInputValue().Trim();
        if (query.Length == 0)
        {
            return true;
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
