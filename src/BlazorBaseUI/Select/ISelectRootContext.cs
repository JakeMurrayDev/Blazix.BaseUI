using BlazorBaseUI.Field;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Select;

/// <summary>
/// Provides non-generic context for the select root, enabling cross-component
/// communication between the root and its descendant components.
/// </summary>
internal interface ISelectRootContext
{
    /// <summary>
    /// Gets the unique identifier for this select root instance.
    /// </summary>
    string RootId { get; }

    /// <summary>
    /// Gets or sets whether the select popup is open.
    /// </summary>
    bool Open { get; set; }

    /// <summary>
    /// Gets or sets whether the select popup is mounted in the DOM.
    /// </summary>
    bool Mounted { get; set; }

    /// <summary>
    /// Gets or sets whether the select is disabled.
    /// </summary>
    bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets whether the select is read-only.
    /// </summary>
    bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets whether the select is required.
    /// </summary>
    bool Required { get; set; }

    /// <summary>
    /// Gets or sets whether the select supports multiple selection.
    /// </summary>
    bool Multiple { get; set; }

    /// <summary>
    /// Gets or sets whether items should be highlighted when the pointer moves over them.
    /// </summary>
    bool HighlightItemOnHover { get; set; }

    /// <summary>
    /// Gets or sets the reason the select's open state last changed.
    /// </summary>
    SelectOpenChangeReason OpenChangeReason { get; set; }

    /// <summary>
    /// Gets or sets the current transition animation status.
    /// </summary>
    TransitionStatus TransitionStatus { get; set; }

    /// <summary>
    /// Gets or sets the index of the currently highlighted item.
    /// </summary>
    int ActiveIndex { get; set; }

    /// <summary>
    /// Gets or sets the ID of the list element (when SelectList is used).
    /// </summary>
    string? ListId { get; set; }

    /// <summary>
    /// Gets whether a SelectList is present.
    /// </summary>
    bool HasList { get; }

    /// <summary>
    /// Gets or sets whether the scroll-up arrow should be visible.
    /// </summary>
    bool ScrollUpArrowVisible { get; set; }

    /// <summary>
    /// Gets or sets whether the scroll-down arrow should be visible.
    /// </summary>
    bool ScrollDownArrowVisible { get; set; }

    /// <summary>
    /// Gets the delegate that returns the current open state.
    /// </summary>
    Func<bool> GetOpen { get; }

    /// <summary>
    /// Gets the delegate that returns whether the select popup is mounted.
    /// </summary>
    Func<bool> GetMounted { get; }

    /// <summary>
    /// Gets the delegate that returns the trigger element reference.
    /// </summary>
    Func<ElementReference?> GetTriggerElement { get; }

    /// <summary>
    /// Gets the delegate that sets the trigger element reference.
    /// </summary>
    Action<ElementReference?> SetTriggerElement { get; }

    /// <summary>
    /// Gets the delegate that returns the popup element reference.
    /// </summary>
    Func<ElementReference?> GetPopupElement { get; }

    /// <summary>
    /// Gets the delegate that sets the popup element reference.
    /// </summary>
    Action<ElementReference?> SetPopupElement { get; }

    /// <summary>
    /// Gets the delegate that returns the positioner element reference.
    /// Distinct from the popup element; used for <c>relatedTarget</c> containment
    /// checks and the mouseup-outside-bounds cancel-open logic on the trigger.
    /// </summary>
    Func<ElementReference?> GetPositionerElement { get; }

    /// <summary>
    /// Gets the delegate that sets the positioner element reference.
    /// </summary>
    Action<ElementReference?> SetPositionerElement { get; }

    /// <summary>
    /// Gets the delegate that returns the value element reference (the span
    /// rendered by <see cref="SelectValue{TValue}"/>). Mirrors the React
    /// <c>valueRef</c> exposed from <c>useSelectRootContext</c>.
    /// </summary>
    Func<ElementReference?> GetValueElement { get; }

    /// <summary>
    /// Gets the delegate that sets the value element reference.
    /// </summary>
    Action<ElementReference?> SetValueElement { get; }

    /// <summary>
    /// Gets the delegate that returns the currently-selected item's
    /// <see cref="SelectItemText"/> element reference, used by the
    /// <c>alignItemWithTrigger</c> placement pipeline to align the popup so
    /// the selected item's text sits over the trigger's value text.
    /// Mirrors the React <c>selectedItemTextRef</c>.
    /// </summary>
    Func<ElementReference?> GetSelectedItemTextElement { get; }

    /// <summary>
    /// Gets the delegate that registers/clears the currently-selected item's
    /// <see cref="SelectItemText"/> element reference. Pass <see langword="null"/>
    /// to clear when the previously-selected item unmounts or is no longer selected.
    /// </summary>
    Action<ElementReference?> SetSelectedItemTextElement { get; }

    /// <summary>
    /// Gets whether an element has been registered as the current selected-item-text
    /// element (i.e., <see cref="GetSelectedItemTextElement"/> returns a non-null value).
    /// Mirrors the React <c>selectedItemTextRef.current !== null</c> check used by
    /// <see cref="SelectItemText"/>'s index-0 fallback branch.
    /// </summary>
    bool HasSelectedItemTextElement { get; }

    /// <summary>
    /// Gets the delegate that sets the list element reference.
    /// </summary>
    Action<ElementReference?> SetListElement { get; }

    /// <summary>
    /// Gets the delegate that sets the active item index.
    /// </summary>
    Action<int> SetActiveIndex { get; }

    /// <summary>
    /// Gets the delegate that sets the open state asynchronously with a reason.
    /// </summary>
    Func<bool, SelectOpenChangeReason, Task> SetOpenAsync { get; }

    /// <summary>
    /// Gets or sets the ID of the popup element (when no SelectList is present).
    /// </summary>
    string? PopupId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the trigger element. Set by
    /// <see cref="SelectTrigger"/> so that descendants (e.g., <see cref="SelectLabel"/>)
    /// can resolve a focus target without the trigger's <see cref="ElementReference"/>.
    /// Mirrors the React store's <c>triggerElement?.id</c> fallback used by <c>useLabel</c>.
    /// </summary>
    string? TriggerId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the label element associated with this select.
    /// Set by <see cref="SelectLabel"/> and consumed by <see cref="SelectTrigger"/>
    /// when no outer <c>LabelableContext</c> supplies a label id.
    /// Mirrors the React store's <c>labelId</c> slot.
    /// </summary>
    string? LabelId { get; set; }

    /// <summary>
    /// Raises the <see cref="StateChanged"/> event so descendants (e.g., the trigger)
    /// re-render after context properties like <see cref="LabelId"/> change.
    /// </summary>
    void NotifyStateChanged();

    /// <summary>
    /// Gets the delegate that clears keyboard-set highlights from the DOM.
    /// </summary>
    Func<Task> ClearHighlightsAsync { get; }

    /// <summary>
    /// Gets whether the current value represents a placeholder state (no selection).
    /// </summary>
    bool IsPlaceholder { get; }

    /// <summary>
    /// Gets the current single-selection value as a boxed object, or the first value
    /// of a multi-selection (or <see langword="null"/> when empty). Used by
    /// <see cref="SelectTrigger"/> to surface the value in its state record so
    /// <see cref="SelectTrigger.Render"/> consumers can branch on it.
    /// </summary>
    object? CurrentValueBoxed { get; }

    /// <summary>
    /// Checks whether a given value (boxed) is currently selected.
    /// </summary>
    bool IsValueSelected(object? value);

    /// <summary>
    /// Raised when the select's state changes (value, open, etc.),
    /// allowing child components to re-render.
    /// </summary>
    event Action? StateChanged;

    /// <summary>
    /// Raised when keyboard navigation takes over highlighting,
    /// signaling items to clear their mouse-set highlights.
    /// </summary>
    event Action? HighlightCleared;

    /// <summary>
    /// Gets the current field state from the parent FieldRoot, if any.
    /// </summary>
    FieldRootState FieldState { get; }

    /// <summary>
    /// Gets or sets the interaction type that opened the select (click, keyboard, touch).
    /// </summary>
    InteractionType OpenInteractionType { get; set; }

    /// <summary>
    /// Gets the index of the currently selected item.
    /// </summary>
    int SelectedIndex { get; }

    /// <summary>
    /// Gets or sets whether drag-to-select is allowed on selected items via mouse-up.
    /// </summary>
    bool AllowSelectedMouseUp { get; set; }

    /// <summary>
    /// Gets or sets whether drag-to-select is allowed on unselected items via mouse-up.
    /// </summary>
    bool AllowUnselectedMouseUp { get; set; }

    /// <summary>
    /// Gets or sets the count of mounted scroll arrow components.
    /// </summary>
    int ScrollArrowsMountedCount { get; set; }

    /// <summary>
    /// Gets whether any scroll arrows are currently mounted.
    /// </summary>
    bool HasScrollArrows { get; }

    /// <summary>
    /// Gets or sets whether keyboard navigation is currently active (vs mouse).
    /// </summary>
    bool KeyboardActive { get; set; }

    /// <summary>
    /// Gets or sets whether items should be force-mounted in the DOM even when closed.
    /// Set by the trigger on first focus so the first click avoids an extra render cycle
    /// while items mount. Mirrors the React <c>store.forceMount</c> slot.
    /// </summary>
    bool ForceMount { get; set; }

    /// <summary>
    /// Gets or sets whether the positioner is in align-item-with-trigger mode.
    /// Set by <see cref="SelectPositioner"/>; the trigger uses it to auto-close on
    /// focus so the popup does not obscure the focused trigger.
    /// Mirrors the React <c>alignItemWithTriggerActiveRef</c>.
    /// </summary>
    bool AlignItemWithTriggerActive { get; set; }

    /// <summary>
    /// Gets whether the select is in modal mode. Mirrors the React store's
    /// <c>modal</c> selector. Consumed by <see cref="SelectPositioner"/> to
    /// decide whether to render an <c>InternalBackdrop</c> and acquire a body
    /// scroll lock.
    /// </summary>
    bool Modal { get; set; }

    /// <summary>
    /// Gets the ordered list of currently-mounted item values. Mirrors the
    /// React <c>valuesRef.current</c>. Values are boxed so the positioner
    /// (non-generic) can reason about membership independent of the root's
    /// generic type parameter.
    /// </summary>
    IReadOnlyList<object?> RegisteredValues { get; }

    /// <summary>
    /// Gets the initial value captured by the root when the select first mounted.
    /// Used by <see cref="SelectPositioner"/> as a fallback target when the
    /// currently-selected value disappears from the visible item set.
    /// Mirrors the React <c>initialValueRef.current</c>. <see langword="null"/>
    /// means "no initial selection".
    /// </summary>
    object? InitialValueBoxed { get; }

    /// <summary>
    /// Gets the delegate used to compare two boxed item values for equality.
    /// Honors the user-supplied <c>IsItemEqualToValue</c> callback on the root
    /// (or falls back to <see cref="object.Equals(object?, object?)"/>).
    /// Mirrors the React <c>isItemEqualToValue</c> store slot.
    /// </summary>
    Func<object?, object?, bool> AreValuesEqual { get; }

    /// <summary>
    /// Gets the current multi-select value list, boxed. Empty for single-select.
    /// Used by the positioner's item-map prune handler to filter visible values.
    /// </summary>
    IReadOnlyList<object?> MultiValueBoxed { get; }

    /// <summary>
    /// Replaces the current selection with <paramref name="nextValue"/>.
    /// For single-select this is the boxed value (or <see langword="null"/>);
    /// for multi-select it must be an <see cref="IEnumerable{Object}"/> or
    /// <see langword="null"/>. Mirrors the React <c>setValue(value, eventDetails)</c>
    /// call used by the positioner's prune logic.
    /// </summary>
    Task SetValueBoxedAsync(object? nextValue, SelectOpenChangeReason reason);

    /// <summary>
    /// Registers an item value as mounted. Triggers <see cref="ItemMapChanged"/>
    /// so the positioner can react. Safe to call multiple times with the same
    /// value — the registration is ref-counted internally.
    /// </summary>
    void RegisterItemValue(object? value);

    /// <summary>
    /// Un-registers a previously-registered item value. Triggers
    /// <see cref="ItemMapChanged"/> when the last reference is removed.
    /// </summary>
    void UnregisterItemValue(object? value);

    /// <summary>
    /// Clears the currently-selected item text ref (used when pruning resets
    /// the selection to null). Mirrors the React
    /// <c>selectedItemTextRef.current = null</c> assignment.
    /// </summary>
    void ClearSelectedItemText();

    /// <summary>
    /// Raised whenever an item mounts or unmounts and the registered value set
    /// changes. Consumed by <see cref="SelectPositioner"/> to run the prune
    /// logic (mirrors React <c>CompositeList.onMapChange</c>).
    /// </summary>
    event Action? ItemMapChanged;

    /// <summary>
    /// Gets whether the Items list contains a null-valued entry with a label.
    /// </summary>
    bool HasNullItemLabel { get; }

    /// <summary>
    /// Checks whether the given index is the selected index (selected by focus).
    /// </summary>
    bool IsSelectedByFocus(int index);

    /// <summary>
    /// Handles typeahead character input when the select is closed (single-select only).
    /// </summary>
    Task HandleClosedTypeaheadAsync(string character);

    /// <summary>
    /// Notifies the root that the trigger has received focus.
    /// </summary>
    void OnTriggerFocus();

    /// <summary>
    /// Notifies the root that the trigger has lost focus.
    /// </summary>
    void OnTriggerBlur();

    /// <summary>
    /// Returns the ordinal index of a registered boxed value, or <c>-1</c> when not registered.
    /// Uses <see cref="AreValuesEqual"/> so custom equality is honored. O(n) over the
    /// registered item set. Mirrors the index returned by React <c>useCompositeListItem</c>.
    /// </summary>
    int IndexOfValue(object? boxedValue);

    /// <summary>
    /// Sets <see cref="ActiveIndex"/> in response to pointer hover without invoking
    /// the JS <c>setActiveItem</c> side effects (focus, scroll). Pushes a lightweight
    /// sync message to JS so arrow-key navigation starts from the hovered item.
    /// Mirrors React's onMouseEnter/onMouseMove paths that call
    /// <c>store.set('activeIndex', index)</c> without imperative focus.
    /// </summary>
    Task SetHoverActiveIndexAsync(int index);

    /// <summary>
    /// Clears <see cref="ActiveIndex"/> only when it currently equals
    /// <paramref name="expectedIndex"/>. Mirrors React's onMouseLeave compare-and-clear
    /// (the item only retracts its own highlight).
    /// </summary>
    Task ClearHoverActiveIndexAsync(int expectedIndex);

    /// <summary>
    /// Gets whether a typeahead query is currently in progress. Items consume this to
    /// decide whether <c>Space</c> should be treated as a commit key or appended to the
    /// typeahead buffer. Mirrors React <c>typingRef.current</c>.
    /// </summary>
    bool IsTyping { get; }

    /// <summary>
    /// Registers a <see cref="SelectItem{TValue}"/> with the root by instance identity and
    /// returns the ordinal index it has been assigned. Appends to the ordered registration
    /// list so each item — even ones sharing the same <c>Value</c> — receives a unique index.
    /// Mirrors React <c>useCompositeListItem</c>'s DOM-order registration.
    /// </summary>
    int RegisterItem(object item);

    /// <summary>
    /// Unregisters a previously-registered <see cref="SelectItem{TValue}"/>. Subsequent
    /// sibling registrations shift down in index; callers should re-query their index
    /// via <see cref="GetItemIndex"/> on each render.
    /// </summary>
    void UnregisterItem(object item);

    /// <summary>
    /// Returns the current ordinal index of a previously-registered item, or <c>-1</c>
    /// when the item is not registered. O(n) over the registered item list.
    /// </summary>
    int GetItemIndex(object item);

    /// <summary>
    /// Registers a lazy typeahead label resolver for the given boxed value. Used as a
    /// fallback when the parent <see cref="SelectItem{TValue}"/> has no <c>Label</c>
    /// prop but a <see cref="SelectItemText"/> is present; the resolver reads
    /// <c>textContent</c> from the text element on demand. Mirrors the React
    /// <c>textRef</c> path through <c>useCompositeListItem</c>.
    /// </summary>
    void RegisterItemLabelResolver(object? boxedValue, Func<Task<string?>> resolver);

    /// <summary>
    /// Removes a previously-registered lazy typeahead label resolver.
    /// </summary>
    void UnregisterItemLabelResolver(object? boxedValue);
}
