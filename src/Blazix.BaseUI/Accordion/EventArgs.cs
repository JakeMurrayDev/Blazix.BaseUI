using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Blazix.BaseUI.Accordion;

/// <summary>
/// Specifies the reason for an accordion value change event.
/// </summary>
public enum AccordionValueChangeReason
{
    /// <summary>No specific reason.</summary>
    None,

    /// <summary>The trigger was pressed.</summary>
    TriggerPress
}

/// <summary>
/// Provides data for the accordion value change event.
/// </summary>
/// <typeparam name="TValue">The type of the value used to identify accordion items.</typeparam>
public sealed class AccordionValueChangeEventArgs<TValue> : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccordionValueChangeEventArgs{TValue}"/> class.
    /// </summary>
    /// <param name="value">The new value of the expanded item(s).</param>
    /// <param name="reason">The reason for the value change.</param>
    /// <param name="triggerEvent">The mouse event that triggered the change, when the change was caused by a trigger press.</param>
    /// <param name="triggerElement">The trigger element that caused the change, when the change was caused by a trigger press.</param>
    public AccordionValueChangeEventArgs(
        TValue[] value,
        AccordionValueChangeReason reason = AccordionValueChangeReason.None,
        MouseEventArgs? triggerEvent = null,
        ElementReference? triggerElement = null)
    {
        Value = value;
        Reason = reason;
        TriggerEvent = triggerEvent;
        TriggerElement = triggerElement;
    }

    /// <summary>
    /// Gets the new value of the expanded item(s).
    /// </summary>
    public TValue[] Value { get; }

    /// <summary>
    /// Gets the reason for the value change.
    /// </summary>
    public AccordionValueChangeReason Reason { get; }

    /// <summary>
    /// Gets the mouse event that triggered the change, when the change was caused by a trigger press.
    /// </summary>
    public MouseEventArgs? TriggerEvent { get; }

    /// <summary>
    /// Gets the trigger element that caused the change, when the change was caused by a trigger press.
    /// </summary>
    public ElementReference? TriggerElement { get; }

    /// <summary>
    /// Gets a value indicating whether the value change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the event is allowed to propagate.
    /// </summary>
    public bool IsPropagationAllowed { get; private set; }

    /// <summary>
    /// Cancels the value change.
    /// </summary>
    public void Cancel() => IsCanceled = true;

    /// <summary>
    /// Allows the event to propagate.
    /// </summary>
    public void AllowPropagation() => IsPropagationAllowed = true;
}
