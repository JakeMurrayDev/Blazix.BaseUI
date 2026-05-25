using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tabs;

/// <summary>
/// Provides non-generic context for the tabs root, enabling cross-component
/// communication between tabs and panels.
/// </summary>
internal interface ITabsRootContext
{
    /// <summary>
    /// Gets the orientation of the tabs.
    /// </summary>
    Orientation Orientation { get; }

    /// <summary>
    /// Gets the direction of the most recent tab activation.
    /// </summary>
    ActivationDirection ActivationDirection { get; }

    /// <summary>
    /// Gets the text direction used for horizontal keyboard navigation.
    /// </summary>
    Direction Direction { get; }

    /// <summary>
    /// Gets the panel ID associated with the given tab value.
    /// </summary>
    /// <param name="tabValue">The tab value to look up.</param>
    /// <returns>The panel ID, or <see langword="null"/> if not found.</returns>
    string? GetTabPanelIdByValue(object? tabValue);

    /// <summary>
    /// Gets the tab ID associated with the given panel value.
    /// </summary>
    /// <param name="panelValue">The panel value to look up.</param>
    /// <returns>The tab ID, or <see langword="null"/> if not found.</returns>
    string? GetTabIdByPanelValue(object? panelValue);

    /// <summary>
    /// Gets the <see cref="ElementReference"/> of the tab associated with the given value.
    /// </summary>
    /// <param name="value">The value to look up.</param>
    /// <returns>The tab element reference, or <see langword="null"/> if not found.</returns>
    ElementReference? GetTabElementByValue(object? value);

    /// <summary>
    /// Gets the tab registration order associated with the given value.
    /// </summary>
    /// <param name="value">The value to look up.</param>
    /// <returns>The tab order, or <see langword="null"/> if not found.</returns>
    int? GetTabOrderByValue(object? value);

    /// <summary>
    /// Gets the panel index associated with the given value.
    /// </summary>
    /// <param name="panelValue">The panel value to look up.</param>
    /// <returns>The panel index, or <see langword="null"/> if not found.</returns>
    int? GetPanelIndexByValue(object? panelValue);

    /// <summary>
    /// Registers a panel with the given value and ID.
    /// </summary>
    /// <param name="panelValue">The panel value.</param>
    /// <param name="panelId">The panel element ID.</param>
    void RegisterPanel(object? panelValue, string panelId);

    /// <summary>
    /// Registers panel metadata with the given value and ID.
    /// </summary>
    /// <param name="panelValue">The panel value.</param>
    /// <param name="panelId">The panel element ID.</param>
    void RegisterPanelInfo(object? panelValue, string panelId);

    /// <summary>
    /// Unregisters panel metadata with the given value and ID.
    /// </summary>
    /// <param name="panelValue">The panel value.</param>
    /// <param name="panelId">The panel element ID.</param>
    void UnregisterPanelInfo(object? panelValue, string panelId);

    /// <summary>
    /// Unregisters a panel with the given value and ID.
    /// </summary>
    /// <param name="panelValue">The panel value.</param>
    /// <param name="panelId">The panel element ID.</param>
    void UnregisterPanel(object? panelValue, string panelId);
}

/// <summary>
/// Provides strongly-typed context for the tabs root, extending <see cref="ITabsRootContext"/>
/// with value-based tab registration and activation.
/// </summary>
/// <typeparam name="TValue">The type of value used to identify tabs.</typeparam>
internal interface ITabsRootContext<TValue> : ITabsRootContext
{
    /// <summary>
    /// Gets the currently active tab value.
    /// </summary>
    TValue? Value { get; }

    /// <summary>
    /// Requests a tab value change with the specified activation direction.
    /// </summary>
    /// <param name="value">The new tab value.</param>
    /// <param name="direction">The direction of activation.</param>
    /// <param name="reason">The reason for the value change.</param>
    /// <param name="sourceEventArgs">The event args that triggered the change, if available.</param>
    Task OnValueChangeAsync(
        TValue? value,
        ActivationDirection direction,
        TabsValueChangeReason reason,
        EventArgs? sourceEventArgs = null);

    /// <summary>
    /// Registers tab information including its element reference, ID, and disabled state.
    /// </summary>
    /// <param name="value">The tab value.</param>
    /// <param name="element">The tab's element reference.</param>
    /// <param name="id">The tab's element ID.</param>
    /// <param name="disabled">Whether the tab is disabled.</param>
    void RegisterTabInfo(TValue? value, ElementReference element, string? id, bool disabled);

    /// <summary>
    /// Unregisters tab information for the given value.
    /// </summary>
    /// <param name="value">The tab value to unregister.</param>
    void UnregisterTabInfo(TValue? value);
}

/// <summary>
/// Stores registration information for a single tab.
/// </summary>
/// <typeparam name="TValue">The type of value used to identify tabs.</typeparam>
internal sealed class TabInfo<TValue>(TValue? value, ElementReference element, string? id, bool disabled, int order)
{
    /// <summary>
    /// Gets the tab's identifying value.
    /// </summary>
    public TValue? Value { get; } = value;

    /// <summary>
    /// Gets or sets the tab's element reference.
    /// </summary>
    public ElementReference Element { get; set; } = element;

    /// <summary>
    /// Gets or sets the tab's element ID.
    /// </summary>
    public string? Id { get; set; } = id;

    /// <summary>
    /// Gets or sets a value indicating whether the tab is disabled.
    /// </summary>
    public bool Disabled { get; set; } = disabled;

    /// <summary>
    /// Gets the registration order of the tab.
    /// </summary>
    public int Order { get; } = order;
}

/// <summary>
/// Stores registration information for a single panel.
/// </summary>
internal sealed class PanelInfo(string id, int order)
{
    /// <summary>
    /// Gets or sets the panel's element ID.
    /// </summary>
    public string Id { get; set; } = id;

    /// <summary>
    /// Gets the registration order of the panel.
    /// </summary>
    public int Order { get; } = order;
}

/// <summary>
/// Default implementation of <see cref="ITabsRootContext{TValue}"/> that manages tab and panel registrations.
/// </summary>
/// <typeparam name="TValue">The type of value used to identify tabs.</typeparam>
internal sealed class TabsRootContext<TValue> : ITabsRootContext<TValue>
{
    private readonly Dictionary<object, TabInfo<TValue>> tabsByValue = [];
    private readonly Dictionary<object, string> panelIdsByValue = [];
    private readonly Dictionary<object, PanelInfo> panelInfoByValue = [];
    private int nextOrder;
    private int nextPanelOrder;

    /// <summary>
    /// Initializes a new instance of the <see cref="TabsRootContext{TValue}"/> class.
    /// </summary>
    /// <param name="orientation">The tabs orientation.</param>
    /// <param name="activationDirection">The initial activation direction.</param>
    /// <param name="direction">The text direction.</param>
    /// <param name="getValue">A function that returns the current active tab value.</param>
    /// <param name="onValueChange">A callback invoked when the active tab value changes.</param>
    public TabsRootContext(
        Orientation orientation,
        ActivationDirection activationDirection,
        Direction direction,
        Func<TValue?> getValue,
        Func<TValue?, ActivationDirection, TabsValueChangeReason, EventArgs?, Task> onValueChange)
    {
        Orientation = orientation;
        ActivationDirection = activationDirection;
        Direction = direction;
        GetValue = getValue;
        OnValueChange = onValueChange;
    }

    /// <inheritdoc />
    public Orientation Orientation { get; set; }

    /// <inheritdoc />
    public ActivationDirection ActivationDirection { get; set; }

    /// <inheritdoc />
    public Direction Direction { get; set; }

    /// <summary>
    /// Gets a value indicating whether any tabs are registered.
    /// </summary>
    public bool HasTabs => tabsByValue.Count > 0;

    /// <summary>
    /// Gets or sets the callback invoked when a tab is registered.
    /// </summary>
    public Action? OnTabRegistered { get; set; }

    private Func<TValue?> GetValue { get; }
    private Func<TValue?, ActivationDirection, TabsValueChangeReason, EventArgs?, Task> OnValueChange { get; }

    /// <inheritdoc />
    public TValue? Value => GetValue();

    /// <inheritdoc />
    public async Task OnValueChangeAsync(
        TValue? value,
        ActivationDirection direction,
        TabsValueChangeReason reason,
        EventArgs? sourceEventArgs = null)
    {
        await OnValueChange(value, direction, reason, sourceEventArgs);
    }

    /// <inheritdoc />
    public void RegisterTabInfo(TValue? value, ElementReference element, string? id, bool disabled)
    {
        if (value is null)
            return;

        if (tabsByValue.TryGetValue(value, out var existing))
        {
            existing.Element = element;
            existing.Id = id;
            existing.Disabled = disabled;
        }
        else
        {
            tabsByValue[value] = new TabInfo<TValue>(value, element, id, disabled, nextOrder++);
        }

        OnTabRegistered?.Invoke();
    }

    /// <inheritdoc />
    public void UnregisterTabInfo(TValue? value)
    {
        if (value is null)
            return;

        if (tabsByValue.Remove(value!))
        {
            OnTabRegistered?.Invoke();
        }
    }

    /// <summary>
    /// Determines whether the tab with the specified value is disabled.
    /// </summary>
    /// <param name="value">The tab value to check.</param>
    /// <returns><see langword="true"/> if the tab is disabled; otherwise, <see langword="false"/>.</returns>
    public bool IsTabDisabled(TValue? value)
    {
        if (value is null)
            return false;

        return tabsByValue.TryGetValue(value, out var info) && info.Disabled;
    }

    /// <summary>
    /// Determines whether the tab with the specified value is registered.
    /// </summary>
    /// <param name="value">The tab value to check.</param>
    /// <returns><see langword="true"/> if the tab is registered; otherwise, <see langword="false"/>.</returns>
    public bool HasTab(TValue? value)
    {
        if (value is null)
            return false;

        return tabsByValue.ContainsKey(value);
    }

    /// <summary>
    /// Gets the value of the first enabled tab in registration order.
    /// </summary>
    /// <returns>The first enabled tab value, or <see langword="default"/> if none are enabled.</returns>
    public TValue? GetFirstEnabledTabValue()
    {
        TabInfo<TValue>? best = null;
        foreach (var info in tabsByValue.Values)
        {
            if (!info.Disabled && (best is null || info.Order < best.Order))
            {
                best = info;
            }
        }
        return best is not null ? best.Value : default;
    }

    /// <inheritdoc />
    public void RegisterPanel(object? panelValue, string panelId)
    {
        if (panelValue is null)
            return;

        panelIdsByValue[panelValue] = panelId;
    }

    /// <inheritdoc />
    public void RegisterPanelInfo(object? panelValue, string panelId)
    {
        if (panelValue is null)
            return;

        if (panelInfoByValue.TryGetValue(panelValue, out var existing))
        {
            existing.Id = panelId;
        }
        else
        {
            panelInfoByValue[panelValue] = new PanelInfo(panelId, nextPanelOrder++);
        }
    }

    /// <inheritdoc />
    public void UnregisterPanelInfo(object? panelValue, string panelId)
    {
        if (panelValue is null)
            return;

        if (panelInfoByValue.TryGetValue(panelValue, out var existing) && existing.Id == panelId)
        {
            panelInfoByValue.Remove(panelValue);
        }
    }

    /// <inheritdoc />
    public void UnregisterPanel(object? panelValue, string panelId)
    {
        if (panelValue is null)
            return;

        if (panelIdsByValue.TryGetValue(panelValue, out var existingId) && existingId == panelId)
        {
            panelIdsByValue.Remove(panelValue);
        }
    }

    /// <inheritdoc />
    public string? GetTabPanelIdByValue(object? tabValue)
    {
        if (tabValue is null)
            return null;

        return panelIdsByValue.TryGetValue(tabValue, out var panelId) ? panelId : null;
    }

    /// <inheritdoc />
    public int? GetPanelIndexByValue(object? panelValue)
    {
        if (panelValue is null)
            return null;

        if (!panelInfoByValue.TryGetValue(panelValue, out var target))
        {
            return null;
        }

        return panelInfoByValue.Values
            .OrderBy(static info => info.Order)
            .TakeWhile(info => !ReferenceEquals(info, target))
            .Count();
    }

    /// <inheritdoc />
    public string? GetTabIdByPanelValue(object? panelValue)
    {
        if (panelValue is not TValue typedValue)
            return null;

        return tabsByValue.TryGetValue(typedValue, out var info) ? info.Id : null;
    }

    /// <inheritdoc />
    public ElementReference? GetTabElementByValue(object? value)
    {
        if (value is not TValue typedValue)
            return null;

        return tabsByValue.TryGetValue(typedValue, out var info) ? info.Element : null;
    }

    /// <inheritdoc />
    public int? GetTabOrderByValue(object? value)
    {
        if (value is not TValue typedValue)
            return null;

        return tabsByValue.TryGetValue(typedValue, out var info) ? info.Order : null;
    }
}
