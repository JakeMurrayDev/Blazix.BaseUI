namespace BlazorBaseUI.Accordion;

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
    public AccordionValueChangeEventArgs(TValue[] value, AccordionValueChangeReason reason = AccordionValueChangeReason.None)
    {
        Value = value;
        Reason = reason;
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
