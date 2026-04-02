namespace BlazorBaseUI.Dialog;

/// <summary>
/// Provides data for the dialog open change event.
/// </summary>
public sealed class DialogOpenChangeEventArgs : OpenChangeEventArgs<DialogOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DialogOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">The new open state.</param>
    /// <param name="reason">The reason for the state change.</param>
    public DialogOpenChangeEventArgs(bool open, DialogOpenChangeReason reason) : base(open, reason) { }
}
