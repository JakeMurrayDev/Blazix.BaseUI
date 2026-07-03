namespace Blazix.BaseUI.ContextMenu;

using Microsoft.AspNetCore.Components;

/// <summary>
/// Provides shared state and callbacks for the <see cref="ContextMenuRoot"/> and its descendant components.
/// </summary>
internal sealed class ContextMenuRootContext
{
    /// <summary>
    /// Gets the unique identifier for this context menu root instance.
    /// </summary>
    public string RootId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the delegate that returns the virtual anchor element reference used for positioning.
    /// </summary>
    public Func<ElementReference?> GetVirtualAnchorElement { get; init; } = null!;

    /// <summary>
    /// Gets the current positioner element reference, if one has been rendered.
    /// </summary>
    public ElementReference? PositionerElement { get; private set; }

    /// <summary>
    /// Gets or sets the callback to register the backdrop element with the context menu JS,
    /// enabling native context menu suppression when right-clicking the backdrop.
    /// </summary>
    public Func<ElementReference, Task>? RegisterBackdropElement { get; set; }

    /// <summary>
    /// Gets or sets the callback to register the positioner element with the context menu JS,
    /// enabling mouseup handling to distinguish menu chrome from outside presses.
    /// </summary>
    public Func<ElementReference, Task>? RegisterPositionerElement { get; set; }

    /// <summary>
    /// Gets or sets the callback that recomputes the popup position against the current virtual
    /// anchor. Invoked when an already-open context menu receives a second right-click so the
    /// popup follows the cursor (the anchor is moved by JS before the callback runs).
    /// </summary>
    public Func<Task>? RequestReposition { get; set; }

    /// <summary>
    /// Stores and forwards the current positioner element.
    /// </summary>
    /// <param name="element">The current positioner element.</param>
    /// <returns>A task that completes after the JS registration callback runs.</returns>
    public async Task SetPositionerElementAsync(ElementReference? element)
    {
        PositionerElement = element;

        if (element.HasValue && RegisterPositionerElement is not null)
        {
            await RegisterPositionerElement(element.Value);
        }
    }
}
