namespace BlazorBaseUI.Accordion;

/// <summary>
/// Defines the context contract for the <see cref="AccordionRoot{TValue}"/> component.
/// </summary>
internal interface IAccordionRootContext
{
    /// <summary>
    /// Determines whether the component should ignore user interaction.
    /// </summary>
    bool Disabled { get; }

    /// <summary>
    /// Gets the visual orientation of the accordion.
    /// </summary>
    Orientation Orientation { get; }

    /// <summary>
    /// Gets the text directionality.
    /// </summary>
    Direction Direction { get; }

    /// <summary>
    /// Determines whether to loop keyboard focus back to the first item when the end of the list is reached.
    /// </summary>
    bool LoopFocus { get; }

    /// <summary>
    /// Determines whether the browser's built-in page search can find and expand the panel contents.
    /// </summary>
    bool HiddenUntilFound { get; }

    /// <summary>
    /// Determines whether to keep the element in the DOM while the panel is closed.
    /// </summary>
    bool KeepMounted { get; }

    /// <summary>
    /// Determines whether the specified value is currently in the open state.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><see langword="true"/> if the value is open; otherwise, <see langword="false"/>.</returns>
    bool IsValueOpen(object value);

    /// <summary>
    /// Handles the value change when an accordion item is expanded or collapsed.
    /// </summary>
    /// <param name="value">The value of the item being toggled.</param>
    /// <param name="nextOpen">Whether the item should be opened.</param>
    void HandleValueChange(object value, bool nextOpen);

    /// <summary>
    /// Registers an accordion item and returns its index.
    /// </summary>
    /// <param name="itemRef">The object key identifying the item.</param>
    /// <returns>The index assigned to the item.</returns>
    int RegisterItem(object itemRef);

    /// <summary>
    /// Unregisters a previously registered accordion item.
    /// </summary>
    /// <param name="itemRef">The object key identifying the item.</param>
    void UnregisterItem(object itemRef);

    /// <summary>
    /// Gets the current index of a registered accordion item.
    /// </summary>
    /// <param name="itemRef">The object key identifying the item.</param>
    /// <returns>The current index of the item, or -1 if not found.</returns>
    int GetItemIndex(object itemRef);
}

/// <summary>
/// Provides the cascading context for the <see cref="AccordionRoot{TValue}"/> component.
/// </summary>
/// <typeparam name="TValue">The type of the value used to identify accordion items.</typeparam>
internal sealed class AccordionRootContext<TValue> : IAccordionRootContext
{
    private readonly List<object> items = new();

    /// <summary>The current value of the expanded item(s).</summary>
    public TValue[] Value { get; set; } = [];

    /// <summary>Whether the accordion is disabled.</summary>
    public bool Disabled { get; set; }

    /// <summary>The visual orientation of the accordion.</summary>
    public Orientation Orientation { get; set; }

    /// <summary>The text directionality.</summary>
    public Direction Direction { get; set; }

    /// <summary>Whether to loop keyboard focus.</summary>
    public bool LoopFocus { get; set; }

    /// <summary>Whether to use hidden='until-found'.</summary>
    public bool HiddenUntilFound { get; set; }

    /// <summary>Whether to keep the element in the DOM.</summary>
    public bool KeepMounted { get; set; }

    /// <summary>The callback invoked when a value changes.</summary>
    public Action<TValue, bool> OnValueChange { get; set; } = null!;

    /// <inheritdoc />
    public bool IsValueOpen(object value)
    {
        if (value is TValue typedValue)
            return Value.Contains(typedValue);
        return false;
    }

    /// <inheritdoc />
    public void HandleValueChange(object value, bool nextOpen)
    {
        if (value is TValue typedValue)
            OnValueChange(typedValue, nextOpen);
    }

    /// <inheritdoc />
    public int RegisterItem(object itemRef)
    {
        items.Add(itemRef);
        return items.Count - 1;
    }

    /// <inheritdoc />
    public void UnregisterItem(object itemRef)
    {
        items.Remove(itemRef);
    }

    /// <inheritdoc />
    public int GetItemIndex(object itemRef)
    {
        return items.IndexOf(itemRef);
    }
}
