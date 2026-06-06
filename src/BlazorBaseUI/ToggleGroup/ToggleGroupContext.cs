using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.ToggleGroup;

/// <summary>
/// Defines the contract for cascading state shared between a <see cref="ToggleGroup"/>
/// and its child <see cref="Toggle.Toggle"/> components.
/// </summary>
internal interface IToggleGroupContext
{
    /// <summary>
    /// Gets the values of all currently pressed toggle buttons in the group.
    /// </summary>
    IReadOnlyList<string> Value { get; }

    /// <summary>
    /// Gets whether the toggle group should ignore user interaction.
    /// </summary>
    bool Disabled { get; }

    /// <summary>
    /// Gets the orientation of the toggle group.
    /// </summary>
    Orientation Orientation { get; }

    /// <summary>
    /// Gets whether keyboard focus loops back to the first item when the end of the list is reached.
    /// </summary>
    bool LoopFocus { get; }

    /// <summary>
    /// Gets the current text direction used by keyboard navigation.
    /// </summary>
    Direction Direction { get; }

    /// <summary>
    /// Gets whether the group value was initialized with a value or default value parameter.
    /// </summary>
    bool IsValueInitialized { get; }

    /// <summary>
    /// Gets whether the group is rendered inside a toolbar root.
    /// </summary>
    bool IsInToolbar { get; }

    /// <summary>
    /// Gets the associated <see cref="ElementReference"/> of the group element.
    /// </summary>
    ElementReference? GroupElement { get; }

    /// <summary>
    /// Updates the pressed state of a toggle within the group.
    /// </summary>
    /// <param name="toggleValue">The value identifier of the toggle to update.</param>
    /// <param name="nextPressed">The new pressed state for the toggle.</param>
    /// <param name="eventArgs">The originating mouse event.</param>
    Task<ToggleGroupValueChangeEventArgs> SetGroupValueAsync(
        string toggleValue,
        bool nextPressed,
        MouseEventArgs? eventArgs);

    /// <summary>
    /// Registers a toggle item for render-time roving tabindex calculation.
    /// </summary>
    /// <param name="itemId">The stable item identifier.</param>
    /// <param name="disabled">Whether the item is disabled.</param>
    void RegisterItem(string itemId, bool disabled);

    /// <summary>
    /// Updates a registered toggle item.
    /// </summary>
    /// <param name="itemId">The stable item identifier.</param>
    /// <param name="disabled">Whether the item is disabled.</param>
    void UpdateItem(string itemId, bool disabled);

    /// <summary>
    /// Unregisters a toggle item.
    /// </summary>
    /// <param name="itemId">The stable item identifier.</param>
    void UnregisterItem(string itemId);

    /// <summary>
    /// Gets the current render-time tabindex for a toggle item.
    /// </summary>
    /// <param name="itemId">The stable item identifier.</param>
    int GetTabIndex(string itemId);
}

/// <summary>
/// Provides cascading state shared between a <see cref="ToggleGroup"/>
/// and its child <see cref="Toggle.Toggle"/> components.
/// </summary>
internal sealed class ToggleGroupContext : IToggleGroupContext
{
    private readonly List<ToggleGroupItemRegistration> itemRegistrations = [];

    /// <summary>
    /// Gets or sets whether the toggle group should ignore user interaction.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the orientation of the toggle group.
    /// </summary>
    public Orientation Orientation { get; set; }

    /// <summary>
    /// Gets or sets whether keyboard focus loops back to the first item when the end of the list is reached.
    /// </summary>
    public bool LoopFocus { get; set; }

    /// <summary>
    /// Gets or sets the current text direction used by keyboard navigation.
    /// </summary>
    public Direction Direction { get; set; } = Direction.Ltr;

    /// <summary>
    /// Gets or sets whether the group value was initialized with a value or default value parameter.
    /// </summary>
    public bool IsValueInitialized { get; set; }

    /// <summary>
    /// Gets or sets whether the group is rendered inside a toolbar root.
    /// </summary>
    public bool IsInToolbar { get; set; }

    /// <summary>
    /// Gets or sets a delegate that returns the values of all currently pressed toggle buttons.
    /// </summary>
    public Func<IReadOnlyList<string>> GetValueFunc { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that updates the pressed state of a toggle within the group.
    /// </summary>
    public Func<string, bool, MouseEventArgs?, Task<ToggleGroupValueChangeEventArgs>> SetGroupValueFunc { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that returns the associated <see cref="ElementReference"/> of the group element.
    /// </summary>
    public Func<ElementReference?> GetGroupElementFunc { get; set; } = null!;

    /// <inheritdoc />
    public IReadOnlyList<string> Value => GetValueFunc();

    /// <inheritdoc />
    public ElementReference? GroupElement => GetGroupElementFunc();

    /// <inheritdoc />
    public Task<ToggleGroupValueChangeEventArgs> SetGroupValueAsync(
        string toggleValue,
        bool nextPressed,
        MouseEventArgs? eventArgs) =>
        SetGroupValueFunc(toggleValue, nextPressed, eventArgs);

    /// <inheritdoc />
    public void RegisterItem(string itemId, bool disabled)
    {
        if (itemRegistrations.Any(item => item.ItemId == itemId))
        {
            UpdateItem(itemId, disabled);
            return;
        }

        itemRegistrations.Add(new ToggleGroupItemRegistration(itemId, disabled));
    }

    /// <inheritdoc />
    public void UpdateItem(string itemId, bool disabled)
    {
        var index = itemRegistrations.FindIndex(item => item.ItemId == itemId);
        if (index >= 0)
        {
            itemRegistrations[index] = itemRegistrations[index] with { Disabled = disabled };
        }
    }

    /// <inheritdoc />
    public void UnregisterItem(string itemId)
    {
        var index = itemRegistrations.FindIndex(item => item.ItemId == itemId);
        if (index >= 0)
        {
            itemRegistrations.RemoveAt(index);
        }
    }

    /// <inheritdoc />
    public int GetTabIndex(string itemId)
    {
        var firstEnabled = itemRegistrations.FirstOrDefault(item => !item.Disabled);
        return firstEnabled is not null && firstEnabled.ItemId == itemId ? 0 : -1;
    }
}

internal sealed record ToggleGroupItemRegistration(string ItemId, bool Disabled);
