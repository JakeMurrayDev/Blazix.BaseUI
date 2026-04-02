namespace BlazorBaseUI.PreviewCard;

/// <summary>
/// Provides data for the preview card open state change event.
/// </summary>
public sealed class PreviewCardOpenChangeEventArgs : OpenChangeEventArgs<PreviewCardOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PreviewCardOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">The requested open state of the preview card.</param>
    /// <param name="reason">The reason for the open state change.</param>
    public PreviewCardOpenChangeEventArgs(bool open, PreviewCardOpenChangeReason reason) : base(open, reason) { }
}
