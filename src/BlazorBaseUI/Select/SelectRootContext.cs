using BlazorBaseUI.Field;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Select;

/// <summary>
/// Provides strongly-typed context for the select root, implementing <see cref="ISelectRootContext"/>
/// with value-based selection management.
/// </summary>
/// <typeparam name="TValue">The type of value used to identify items.</typeparam>
internal sealed class SelectRootContext<TValue> : ISelectRootContext
{
    #pragma warning disable CS8714 // TValue may be nullable but Dictionary requires notnull key
    private readonly Dictionary<TValue, string> _itemLabels = new();
    #pragma warning restore CS8714

    private readonly List<object?> _registeredValues = new();
    private readonly List<object> _registeredItems = new();
    private readonly List<KeyValuePair<object?, Func<Task<string?>>>> _itemLabelResolvers = new();
    private Func<object?, object?, bool>? _areValuesEqual;

    private string _typeaheadBuffer = string.Empty;
    private CancellationTokenSource? _typeaheadCts;

    /// <inheritdoc />
    public string RootId { get; init; } = string.Empty;

    /// <inheritdoc />
    public bool Open { get; set; }

    /// <inheritdoc />
    public bool Mounted { get; set; }

    /// <inheritdoc />
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public bool ReadOnly { get; set; }

    /// <inheritdoc />
    public bool Required { get; set; }

    /// <inheritdoc />
    public bool Multiple { get; set; }

    /// <inheritdoc />
    public bool HighlightItemOnHover { get; set; }

    /// <inheritdoc />
    public SelectOpenChangeReason OpenChangeReason { get; set; }

    /// <inheritdoc />
    public TransitionStatus TransitionStatus { get; set; }

    /// <inheritdoc />
    public int ActiveIndex { get; set; }

    /// <inheritdoc />
    public string? ListId { get; set; }

    /// <inheritdoc />
    public bool HasList => !string.IsNullOrEmpty(ListId);

    /// <inheritdoc />
    public bool ScrollUpArrowVisible { get; set; }

    /// <inheritdoc />
    public bool ScrollDownArrowVisible { get; set; }

    /// <inheritdoc />
    public Func<bool> GetOpen { get; init; } = null!;

    /// <inheritdoc />
    public Func<bool> GetMounted { get; init; } = null!;

    /// <inheritdoc />
    public Func<ElementReference?> GetTriggerElement { get; init; } = null!;

    /// <inheritdoc />
    public Action<ElementReference?> SetTriggerElement { get; init; } = null!;

    /// <inheritdoc />
    public Func<ElementReference?> GetPopupElement { get; init; } = null!;

    /// <inheritdoc />
    public Action<ElementReference?> SetPopupElement { get; init; } = null!;

    /// <inheritdoc />
    public Func<ElementReference?> GetPositionerElement { get; init; } = null!;

    /// <inheritdoc />
    public Action<ElementReference?> SetPositionerElement { get; init; } = null!;

    /// <inheritdoc />
    public Func<ElementReference?> GetValueElement { get; init; } = null!;

    /// <inheritdoc />
    public Action<ElementReference?> SetValueElement { get; init; } = null!;

    /// <inheritdoc />
    public Func<ElementReference?> GetSelectedItemTextElement { get; init; } = null!;

    /// <inheritdoc />
    public Action<ElementReference?> SetSelectedItemTextElement { get; init; } = null!;

    /// <inheritdoc />
    public bool HasSelectedItemTextElement => GetSelectedItemTextElement() is not null;

    /// <inheritdoc />
    public Action<ElementReference?> SetListElement { get; init; } = null!;

    /// <inheritdoc />
    public Action<int> SetActiveIndex { get; init; } = null!;

    /// <inheritdoc />
    public Func<bool, SelectOpenChangeReason, Task> SetOpenAsync { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that returns the current value.
    /// </summary>
    public Func<TValue?> GetValue { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that returns the current values for multi-select.
    /// </summary>
    public Func<IReadOnlyList<TValue>> GetValues { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that selects a value.
    /// </summary>
    public Func<TValue?, Task> SelectValueAsync { get; init; } = null!;

    /// <summary>
    /// Gets or sets the custom equality comparer for item values.
    /// </summary>
    public Func<TValue, TValue, bool>? IsItemEqualToValue { get; set; }

    /// <summary>
    /// Gets or sets the function to convert a value to its string label.
    /// </summary>
    public Func<TValue?, string?>? ItemToStringLabel { get; set; }

    /// <summary>
    /// Gets or sets the function to convert a value to its form submission string.
    /// </summary>
    public Func<TValue?, string?>? ItemToStringValue { get; set; }

    /// <summary>
    /// Gets or sets the static list of options for pre-mount label resolution.
    /// When provided, labels can be resolved before <see cref="SelectItem{TValue}"/>
    /// components mount (e.g., for default value display).
    /// </summary>
    public IReadOnlyList<SelectOption<TValue>>? Items { get; set; }

    /// <summary>
    /// Gets or sets the grouped options for pre-mount label resolution.
    /// Mirrors React Base UI's grouped <c>{ items: [...] }</c> shape accepted by
    /// <c>resolveSelectedLabel</c>. Scanned after <see cref="Items"/> in <see cref="GetLabel"/>.
    /// </summary>
    public IReadOnlyList<SelectOptionGroup<TValue>>? ItemGroups { get; set; }

    /// <inheritdoc />
    public string? PopupId { get; set; }

    /// <inheritdoc />
    public string? TriggerId { get; set; }

    /// <inheritdoc />
    public string? LabelId { get; set; }

    /// <inheritdoc />
    public Func<Task> ClearHighlightsAsync { get; init; } = null!;

    /// <inheritdoc />
    public FieldRootState FieldState { get; set; } = FieldRootState.Default;

    /// <inheritdoc />
    public InteractionType OpenInteractionType { get; set; }

    /// <inheritdoc />
    public int SelectedIndex { get; set; } = -1;

    /// <inheritdoc />
    public bool AllowSelectedMouseUp { get; set; }

    /// <inheritdoc />
    public bool AllowUnselectedMouseUp { get; set; }

    /// <inheritdoc />
    public int ScrollArrowsMountedCount { get; set; }

    /// <inheritdoc />
    public bool HasScrollArrows => ScrollArrowsMountedCount > 0;

    /// <inheritdoc />
    public bool KeyboardActive { get; set; }

    /// <inheritdoc />
    public bool ForceMount { get; set; }

    /// <inheritdoc />
    public bool AlignItemWithTriggerActive { get; set; }

    /// <inheritdoc />
    public bool Modal { get; set; }

    /// <summary>
    /// Gets or sets the boxed initial value captured by <see cref="SelectRoot{TValue}"/>
    /// at first mount. Used as a fallback target by the positioner's prune logic.
    /// </summary>
    public object? InitialValueBoxed { get; set; }

    /// <summary>
    /// Gets or sets the delegate that dispatches a prune-time value replacement
    /// to the root. Supplied by <see cref="SelectRoot{TValue}"/> at construction.
    /// </summary>
    public Func<object?, SelectOpenChangeReason, Task>? SetValueBoxedFunc { get; init; }

    /// <summary>
    /// Gets or sets the delegate that pushes a silent <c>activeIndex</c> update to JS
    /// so keyboard navigation resumes from the hovered item. No focus or scroll side
    /// effects — purely a state sync. Supplied by <see cref="SelectRoot{TValue}"/>.
    /// </summary>
    public Func<int, Task>? SyncActiveIndexSilentFunc { get; init; }

    /// <inheritdoc />
    public IReadOnlyList<object?> RegisteredValues => _registeredValues;

    /// <inheritdoc />
    public IReadOnlyList<object?> MultiValueBoxed
    {
        get
        {
            if (!Multiple)
            {
                return Array.Empty<object?>();
            }

            var values = GetValues();
            if (values.Count == 0)
            {
                return Array.Empty<object?>();
            }

            var boxed = new object?[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                boxed[i] = values[i];
            }
            return boxed;
        }
    }

    /// <inheritdoc />
    public Func<object?, object?, bool> AreValuesEqual =>
        _areValuesEqual ??= (a, b) =>
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a is null || b is null)
            {
                return false;
            }

            if (IsItemEqualToValue is not null && a is TValue ta && b is TValue tb)
            {
                return IsItemEqualToValue(ta, tb);
            }

            return EqualityComparer<object>.Default.Equals(a, b);
        };

    /// <inheritdoc />
    public Task SetValueBoxedAsync(object? nextValue, SelectOpenChangeReason reason) =>
        SetValueBoxedFunc is null ? Task.CompletedTask : SetValueBoxedFunc(nextValue, reason);

    /// <inheritdoc />
    public void RegisterItemValue(object? value)
    {
        _registeredValues.Add(value);
        ItemMapChanged?.Invoke();
    }

    /// <inheritdoc />
    public void UnregisterItemValue(object? value)
    {
        var comparer = AreValuesEqual;
        for (var i = 0; i < _registeredValues.Count; i++)
        {
            if (comparer(_registeredValues[i], value))
            {
                _registeredValues.RemoveAt(i);
                ItemMapChanged?.Invoke();
                return;
            }
        }
    }

    /// <inheritdoc />
    public int RegisterItem(object item)
    {
        _registeredItems.Add(item);
        return _registeredItems.Count - 1;
    }

    /// <inheritdoc />
    public void UnregisterItem(object item)
    {
        _registeredItems.Remove(item);
    }

    /// <inheritdoc />
    public int GetItemIndex(object item)
    {
        return _registeredItems.IndexOf(item);
    }

    /// <inheritdoc />
    public void RegisterItemLabelResolver(object? boxedValue, Func<Task<string?>> resolver)
    {
        var comparer = AreValuesEqual;
        for (var i = 0; i < _itemLabelResolvers.Count; i++)
        {
            if (comparer(_itemLabelResolvers[i].Key, boxedValue))
            {
                _itemLabelResolvers[i] = new KeyValuePair<object?, Func<Task<string?>>>(boxedValue, resolver);
                return;
            }
        }

        _itemLabelResolvers.Add(new KeyValuePair<object?, Func<Task<string?>>>(boxedValue, resolver));
    }

    /// <inheritdoc />
    public void UnregisterItemLabelResolver(object? boxedValue)
    {
        var comparer = AreValuesEqual;
        for (var i = 0; i < _itemLabelResolvers.Count; i++)
        {
            if (comparer(_itemLabelResolvers[i].Key, boxedValue))
            {
                _itemLabelResolvers.RemoveAt(i);
                return;
            }
        }
    }

    /// <inheritdoc />
    public int IndexOfValue(object? boxedValue)
    {
        var comparer = AreValuesEqual;
        for (var i = 0; i < _registeredValues.Count; i++)
        {
            if (comparer(_registeredValues[i], boxedValue))
            {
                return i;
            }
        }
        return -1;
    }

    /// <inheritdoc />
    public async Task SetHoverActiveIndexAsync(int index)
    {
        if (ActiveIndex == index)
        {
            return;
        }

        ActiveIndex = index;
        NotifyStateChanged();

        if (SyncActiveIndexSilentFunc is not null)
        {
            await SyncActiveIndexSilentFunc(index);
        }
    }

    /// <inheritdoc />
    public async Task ClearHoverActiveIndexAsync(int expectedIndex)
    {
        if (ActiveIndex != expectedIndex)
        {
            return;
        }

        ActiveIndex = -1;
        NotifyStateChanged();

        if (SyncActiveIndexSilentFunc is not null)
        {
            await SyncActiveIndexSilentFunc(-1);
        }
    }

    /// <inheritdoc />
    public bool IsTyping => _typeaheadBuffer.Length > 0;

    /// <inheritdoc />
    public void ClearSelectedItemText()
    {
        // Blazor resolves display text via GetLabel() on every render, so there is
        // no separate ref to null out. The positioner re-renders via StateChanged
        // after prune, which re-runs label resolution with the updated value.
    }

    /// <inheritdoc />
    public event Action? ItemMapChanged;

    /// <inheritdoc />
    public bool HasNullItemLabel
    {
        get
        {
            if (Items is not null)
            {
                foreach (var item in Items)
                {
                    if (item.Value is null && !string.IsNullOrEmpty(item.Label))
                    {
                        return true;
                    }
                }
            }

            if (ItemGroups is not null)
            {
                foreach (var group in ItemGroups)
                {
                    foreach (var item in group.Items)
                    {
                        if (item.Value is null && !string.IsNullOrEmpty(item.Label))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }

    /// <inheritdoc />
    public bool IsSelectedByFocus(int index) => SelectedIndex == index;

    /// <summary>
    /// Gets or sets the floating root context adapter for use with
    /// <see cref="FloatingFocusManager.FloatingFocusManager"/>.
    /// </summary>
    public IFloatingRootContext? FloatingRootContext { get; set; }

    /// <summary>
    /// Gets or sets the action delegate invoked when the trigger receives focus.
    /// </summary>
    public Action? OnTriggerFocusAction { get; init; }

    /// <summary>
    /// Gets or sets the action delegate invoked when the trigger loses focus.
    /// </summary>
    public Action? OnTriggerBlurAction { get; init; }

    /// <inheritdoc />
    public void OnTriggerFocus() => OnTriggerFocusAction?.Invoke();

    /// <inheritdoc />
    public void OnTriggerBlur() => OnTriggerBlurAction?.Invoke();

    /// <inheritdoc />
    public event Action? StateChanged;

    /// <inheritdoc />
    public event Action? HighlightCleared;

    /// <summary>
    /// Raises the <see cref="StateChanged"/> event.
    /// </summary>
    public void NotifyStateChanged() => StateChanged?.Invoke();

    /// <summary>
    /// Raises the <see cref="HighlightCleared"/> event.
    /// </summary>
    public void NotifyHighlightCleared() => HighlightCleared?.Invoke();

    /// <inheritdoc />
    public bool IsPlaceholder
    {
        get
        {
            if (Multiple)
            {
                return GetValues().Count == 0;
            }

            // Mirror React's `stringifyAsValue(value, itemToStringValue) !== ''`:
            // both null and a serialized empty string mean "no real selection".
            var value = GetValue();
            if (value is null)
            {
                return true;
            }

            var serialized = GetFormValue(value);
            return string.IsNullOrEmpty(serialized);
        }
    }

    /// <inheritdoc />
    public object? CurrentValueBoxed
    {
        get
        {
            // Mirror React's state shape: in Multiple mode, expose the whole value
            // array (boxed) so custom Render/state-keyed CSS receive the array as
            // React does. Single mode stays scalar.
            if (Multiple)
            {
                return MultiValueBoxed;
            }

            return GetValue();
        }
    }

    /// <inheritdoc />
    public bool IsValueSelected(object? value)
    {
        // React allows null item values (default `value` prop on SelectItem). The
        // `is TValue` pattern fails on null, so handle null explicitly: when TValue
        // is a reference type or Nullable<T> (`default(TValue)` is null), null is a
        // legal candidate; otherwise it cannot match.
        TValue? typedValue;
        if (value is null)
        {
            if (default(TValue) is not null)
            {
                return false;
            }

            typedValue = default;
        }
        else if (value is TValue tv)
        {
            typedValue = tv;
        }
        else
        {
            return false;
        }

        if (Multiple)
        {
            var values = GetValues();
            foreach (var v in values)
            {
                if (AreEqual(v, typedValue))
                {
                    return true;
                }
            }
            return false;
        }

        return AreEqual(GetValue(), typedValue);
    }

    /// <summary>
    /// Registers a display label for the given value, used by <see cref="GetLabel"/> when
    /// <see cref="ItemToStringLabel"/> is not provided.
    /// Only notifies subscribers when the registration would change the resolved label
    /// for a currently selected value.
    /// </summary>
    public void RegisterItemLabel(TValue value, string label)
    {
        if (_itemLabels.TryGetValue(value, out var existing) && existing == label)
        {
            return;
        }

        _itemLabels[value] = label;

        if (ItemToStringLabel is not null)
        {
            return;
        }

        if (Items is not null)
        {
            return;
        }

        if (Multiple)
        {
            var values = GetValues();
            foreach (var v in values)
            {
                if (AreEqual(v, value))
                {
                    NotifyStateChanged();
                    return;
                }
            }
        }
        else if (AreEqual(GetValue(), value))
        {
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Called when a <see cref="SelectItem{TValue}"/> disposes. Labels are intentionally
    /// retained so <see cref="GetLabel"/> can resolve display text after popup close
    /// without requiring items to re-register on reopen.
    /// </summary>
    public void UnregisterItemLabel(TValue value)
    {
    }

    /// <summary>
    /// Gets the label for a given value. Resolution order mirrors the React Base UI
    /// <c>resolveSelectedLabel</c> cascade:
    /// <list type="number">
    ///   <item><description><see cref="ItemToStringLabel"/> user callback</description></item>
    ///   <item><description><see cref="ISelectItemLabel"/> on the value itself (object with explicit label)</description></item>
    ///   <item><description>Registered live <see cref="SelectItem{TValue}"/> labels</description></item>
    ///   <item><description>Static <see cref="Items"/> scan</description></item>
    ///   <item><description>Grouped <see cref="ItemGroups"/> scan</description></item>
    ///   <item><description><see cref="object.ToString"/> fallback</description></item>
    /// </list>
    /// </summary>
    public string? GetLabel(TValue? value)
    {
        if (ItemToStringLabel is not null)
        {
            return ItemToStringLabel(value);
        }

        if (value is ISelectItemLabel labeled && labeled.Label is not null)
        {
            return labeled.Label;
        }

        if (value is not null && _itemLabels.TryGetValue(value, out var label))
        {
            return label;
        }

        if (Items is not null)
        {
            foreach (var item in Items)
            {
                if (AreEqual(item.Value, value))
                {
                    return item.Label;
                }
            }
        }

        if (ItemGroups is not null)
        {
            foreach (var group in ItemGroups)
            {
                foreach (var item in group.Items)
                {
                    if (AreEqual(item.Value, value))
                    {
                        return item.Label;
                    }
                }
            }
        }

        return value?.ToString();
    }

    /// <summary>
    /// Gets the form submission string for a given value using <see cref="ItemToStringValue"/> if available,
    /// otherwise falls back to <see cref="object.ToString"/>.
    /// </summary>
    public string? GetFormValue(TValue? value)
    {
        if (ItemToStringValue is not null)
        {
            return ItemToStringValue(value);
        }

        return value?.ToString();
    }

    /// <summary>
    /// Finds a value by its form submission string, matching case-insensitively
    /// against registered items. Used for browser autofill handling.
    /// Falls back to label matching because browsers often autofill displayed
    /// text (e.g. "United States") rather than the underlying value ("US").
    /// </summary>
    public (bool Found, TValue? Value) FindValueByFormString(string inputValue)
    {
        foreach (var kvp in _itemLabels)
        {
            var candidate = GetFormValue(kvp.Key);
            if (candidate is not null &&
                candidate.Equals(inputValue, StringComparison.OrdinalIgnoreCase))
            {
                return (true, kvp.Key);
            }
        }

        if (Items is not null)
        {
            foreach (var item in Items)
            {
                var candidate = GetFormValue(item.Value);
                if (candidate is not null &&
                    candidate.Equals(inputValue, StringComparison.OrdinalIgnoreCase))
                {
                    return (true, item.Value);
                }
            }
        }

        if (ItemGroups is not null)
        {
            foreach (var group in ItemGroups)
            {
                foreach (var item in group.Items)
                {
                    var candidate = GetFormValue(item.Value);
                    if (candidate is not null &&
                        candidate.Equals(inputValue, StringComparison.OrdinalIgnoreCase))
                    {
                        return (true, item.Value);
                    }
                }
            }
        }

        // Label fallback for browser autofill compatibility.
        foreach (var kvp in _itemLabels)
        {
            var candidate = GetLabel(kvp.Key);
            if (candidate is not null &&
                candidate.Equals(inputValue, StringComparison.OrdinalIgnoreCase))
            {
                return (true, kvp.Key);
            }
        }

        if (Items is not null)
        {
            foreach (var item in Items)
            {
                var candidate = GetLabel(item.Value);
                if (candidate is not null &&
                    candidate.Equals(inputValue, StringComparison.OrdinalIgnoreCase))
                {
                    return (true, item.Value);
                }
            }
        }

        if (ItemGroups is not null)
        {
            foreach (var group in ItemGroups)
            {
                foreach (var item in group.Items)
                {
                    var candidate = GetLabel(item.Value);
                    if (candidate is not null &&
                        candidate.Equals(inputValue, StringComparison.OrdinalIgnoreCase))
                    {
                        return (true, item.Value);
                    }
                }
            }
        }

        return (false, default);
    }

    /// <summary>
    /// Compares two values for equality using <see cref="IsItemEqualToValue"/> if available,
    /// otherwise falls back to <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    internal bool AreEqual(TValue? a, TValue? b)
    {
        if (IsItemEqualToValue is not null && a is not null && b is not null)
        {
            return IsItemEqualToValue(a, b);
        }

        return EqualityComparer<TValue>.Default.Equals(a, b);
    }

    /// <inheritdoc />
    public async Task HandleClosedTypeaheadAsync(string character)
    {
        if (Multiple)
        {
            return;
        }

        _typeaheadCts?.Cancel();
        _typeaheadCts = new CancellationTokenSource();
        var token = _typeaheadCts.Token;

        _typeaheadBuffer += character.ToLowerInvariant();

        TValue? matchValue = default;
        var found = false;
        var currentValue = GetValue();

        bool TryFindMatch<TCandidate>(
            IReadOnlyList<TCandidate> candidates,
            Func<TCandidate, TValue> getValue,
            Func<TCandidate, string?> getLabel,
            out TValue? value)
        {
            value = default;
            if (candidates.Count == 0)
            {
                return false;
            }

            var startIndex = 0;
            for (var i = 0; i < candidates.Count; i++)
            {
                if (AreEqual(getValue(candidates[i]), currentValue))
                {
                    startIndex = i + 1;
                    break;
                }
            }

            for (var offset = 0; offset < candidates.Count; offset++)
            {
                var index = (startIndex + offset) % candidates.Count;
                var candidate = candidates[index];
                var label = getLabel(candidate);
                if (!string.IsNullOrEmpty(label) &&
                    label.StartsWith(_typeaheadBuffer, StringComparison.OrdinalIgnoreCase))
                {
                    value = getValue(candidate);
                    return true;
                }
            }

            return false;
        }

        if (Items is not null && Items.Count > 0)
        {
            found = TryFindMatch(
                Items,
                item => item.Value,
                item => item.Label,
                out matchValue);
        }

        if (!found && ItemGroups is not null)
        {
            var groupedItems = new List<SelectOption<TValue>>();
            foreach (var group in ItemGroups)
            {
                foreach (var item in group.Items)
                {
                    groupedItems.Add(item);
                }
            }

            found = TryFindMatch(
                groupedItems,
                item => item.Value,
                item => item.Label,
                out matchValue);
        }

        if (!found)
        {
            var labels = _itemLabels.ToList();
            found = TryFindMatch(
                labels,
                kvp => kvp.Key,
                kvp => kvp.Value,
                out matchValue);
        }

        if (!found && _itemLabelResolvers.Count > 0)
        {
            var resolvedLabels = new List<KeyValuePair<TValue, string?>>();
            foreach (var kvp in _itemLabelResolvers)
            {
                var label = await kvp.Value();
                if (kvp.Key is TValue typedKey)
                {
                    resolvedLabels.Add(new KeyValuePair<TValue, string?>(typedKey, label));
                }
            }

            found = TryFindMatch(
                resolvedLabels,
                kvp => kvp.Key,
                kvp => kvp.Value,
                out matchValue);
        }

        if (found)
        {
            await SelectValueAsync(matchValue);
        }

        _ = Task.Delay(500, token).ContinueWith(
            _ => _typeaheadBuffer = string.Empty,
            token,
            TaskContinuationOptions.OnlyOnRanToCompletion,
            TaskScheduler.Default);
    }
}
