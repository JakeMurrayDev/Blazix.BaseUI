namespace BlazorBaseUI.Popover;

/// <summary>
/// Provides data for the popover open state change event.
/// </summary>
public sealed class PopoverOpenChangeEventArgs : OpenChangeEventArgs<PopoverOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PopoverOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">The requested open state of the popover.</param>
    /// <param name="reason">The reason for the open state change.</param>
    public PopoverOpenChangeEventArgs(bool open, PopoverOpenChangeReason reason) : base(open, reason) { }
}
