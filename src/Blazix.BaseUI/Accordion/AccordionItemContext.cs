using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

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
    /// <param name="owner">The panel instance registering or clearing the ID.</param>
    /// <param name="id">The panel element ID, or <see langword="null"/> to clear it.</param>
    void SetPanelId(object owner, string? id);

    /// <summary>
    /// Sets the ID of the associated trigger element.
    /// </summary>
    /// <param name="owner">The trigger instance registering or clearing the ID.</param>
    /// <param name="id">The trigger element ID.</param>
    void SetTriggerId(object owner, string? id);

    /// <summary>
    /// Sets whether the associated panel is mounted for transition purposes.
    /// </summary>
    /// <param name="mounted">Whether the panel is mounted.</param>
    void SetPanelMounted(bool mounted);

    /// <summary>
    /// Invokes the trigger action to toggle the accordion item.
    /// </summary>
    /// <param name="triggerEvent">The mouse event that triggered the change.</param>
    /// <param name="triggerElement">The trigger element that caused the change.</param>
    Task HandleTrigger(MouseEventArgs? triggerEvent = null, ElementReference? triggerElement = null);
}

/// <summary>
/// Provides the cascading context for the <see cref="AccordionItem{TValue}"/> component.
/// </summary>
/// <typeparam name="TValue">The type of the value used to identify accordion items.</typeparam>
internal sealed class AccordionItemContext<TValue> : IAccordionItemContext
{
    private readonly RegisteredIdOwner panelIdOwner = new();
    private readonly RegisteredIdOwner triggerIdOwner = new();

    /// <summary>The parent root context.</summary>
    public AccordionRootContext<TValue> RootContext { get; set; } = null!;

    /// <summary>The value that identifies this accordion item.</summary>
    public TValue Value { get; set; } = default!;

    /// <summary>The index of the accordion item.</summary>
    public int Index { get; set; }

    /// <summary>Whether the item is disabled.</summary>
    public bool Disabled { get; set; }

    /// <summary>The action invoked when the trigger is activated.</summary>
    public Func<MouseEventArgs?, ElementReference?, Task> TriggerHandler { get; set; } = null!;

    /// <summary>The action invoked to set the panel ID.</summary>
    public Action<object, string?> PanelIdSetter { get; set; } = null!;

    /// <summary>The action invoked to set the trigger ID.</summary>
    public Action<object, string?> TriggerIdSetter { get; set; } = null!;

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
    public void SetPanelId(object owner, string? id)
    {
        if (!panelIdOwner.ShouldApply(owner, id)) return;

        var next = id ?? string.Empty;
        if (PanelId == next) return;

        PanelId = next;
        PanelIdSetter(owner, id);
    }

    /// <inheritdoc />
    public void SetTriggerId(object owner, string? id)
    {
        if (!triggerIdOwner.ShouldApply(owner, id)) return;

        TriggerId = id;
        TriggerIdSetter(owner, id);
    }

    /// <inheritdoc />
    public void SetPanelMounted(bool mounted)
    {
        PanelMountedSetter(mounted);
    }

    /// <inheritdoc />
    public Task HandleTrigger(MouseEventArgs? triggerEvent = null, ElementReference? triggerElement = null) =>
        TriggerHandler(triggerEvent, triggerElement);
}
