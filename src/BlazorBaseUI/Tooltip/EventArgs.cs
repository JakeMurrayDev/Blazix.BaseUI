namespace BlazorBaseUI.Tooltip;

/// <summary>
/// Provides data for the tooltip open state change event.
/// </summary>
public sealed class TooltipOpenChangeEventArgs : OpenChangeEventArgs<TooltipOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TooltipOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">The requested open state of the tooltip.</param>
    /// <param name="reason">The reason for the open state change.</param>
    public TooltipOpenChangeEventArgs(bool open, TooltipOpenChangeReason reason) : base(open, reason) { }
}
