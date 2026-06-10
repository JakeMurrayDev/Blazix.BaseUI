namespace Blazix.BaseUI.Accordion;

/// <summary>
/// Defines the context contract for the <see cref="AccordionItem{TValue}"/> component.
/// </summary>
internal interface IAccordionItemContext
{
    /// <summary>
    /// Determines whether the accordion item is open.
    /// </summary>
    bool Open { get; }

    /// <summary>
    /// Determines whether the accordion item is disabled.
    /// </summary>
    bool Disabled { get; }

    /// <summary>
    /// Gets the index of the accordion item.
    /// </summary>
    int Index { get; }

    /// <summary>
    /// Gets the ID of the associated panel element.
    /// </summary>
    string PanelId { get; }

    /// <summary>
    /// Gets the ID of the associated trigger element.
    /// </summary>
    string? TriggerId { get; }

    /// <summary>
    /// Gets the string representation of the item's value.
    /// </summary>
    string StringValue { get; }

    /// <summary>
    /// Determines whether the accordion item panel is hidden.
    /// </summary>
    bool Hidden { get; }

    /// <summary>
    /// Gets the visual orientation of the accordion.
    /// </summary>
    Orientation Orientation { get; }

    /// <summary>
    /// Sets the ID of the associated panel element.
    /// </summary>
    /// <param name="id">The panel element ID.</param>
    void SetPanelId(string id);

    /// <summary>
    /// Sets the ID of the associated trigger element.
    /// </summary>
    /// <param name="id">The trigger element ID.</param>
    void SetTriggerId(string? id);

    /// <summary>
    /// Sets whether the associated panel is mounted for transition purposes.
    /// </summary>
    /// <param name="mounted">Whether the panel is mounted.</param>
    void SetPanelMounted(bool mounted);

    /// <summary>
    /// Invokes the trigger action to toggle the accordion item.
    /// </summary>
    Task HandleTrigger();
}

/// <summary>
/// Provides the cascading context for the <see cref="AccordionItem{TValue}"/> component.
/// </summary>
/// <typeparam name="TValue">The type of the value used to identify accordion items.</typeparam>
internal sealed class AccordionItemContext<TValue> : IAccordionItemContext
{
    /// <summary>The parent root context.</summary>
    public AccordionRootContext<TValue> RootContext { get; set; } = null!;

    /// <summary>The value that identifies this accordion item.</summary>
    public TValue Value { get; set; } = default!;

    /// <summary>The index of the accordion item.</summary>
    public int Index { get; set; }

    /// <summary>Whether the item is disabled.</summary>
    public bool Disabled { get; set; }

    /// <summary>The action invoked when the trigger is activated.</summary>
    public Func<Task> TriggerHandler { get; set; } = null!;

    /// <summary>The action invoked to set the panel ID.</summary>
    public Action<string> PanelIdSetter { get; set; } = null!;

    /// <summary>The action invoked to set the panel mounted state.</summary>
    public Action<bool> PanelMountedSetter { get; set; } = null!;

    /// <inheritdoc />
    public bool Open => RootContext.IsValueOpen(Value!);

    /// <inheritdoc />
    public string PanelId { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? TriggerId { get; set; }

    /// <inheritdoc />
    public string StringValue => Value?.ToString() ?? string.Empty;

    /// <inheritdoc />
    public Orientation Orientation => RootContext.Orientation;

    /// <inheritdoc />
    public bool Hidden { get; set; }

    /// <inheritdoc />
    public void SetPanelId(string id)
    {
        PanelId = id;
        PanelIdSetter(id);
    }

    /// <inheritdoc />
    public void SetTriggerId(string? id)
    {
        TriggerId = id;
    }

    /// <inheritdoc />
    public void SetPanelMounted(bool mounted)
    {
        PanelMountedSetter(mounted);
    }

    /// <inheritdoc />
    public Task HandleTrigger() => TriggerHandler();
}
