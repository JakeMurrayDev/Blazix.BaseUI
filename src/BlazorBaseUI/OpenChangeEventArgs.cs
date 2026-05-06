namespace BlazorBaseUI;

/// <summary>
/// Abstract base class for open/close state change event args.
/// Provides shared <see cref="Open"/>, <see cref="Reason"/>, <see cref="IsCanceled"/>,
/// and <see cref="PreventUnmount"/> properties.
/// </summary>
/// <typeparam name="TReason">The open change reason enum type.</typeparam>
public abstract class OpenChangeEventArgs<TReason> : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenChangeEventArgs{TReason}"/> class.
    /// </summary>
    /// <param name="open">The new open state.</param>
    /// <param name="reason">The reason for the state change.</param>
    protected OpenChangeEventArgs(bool open, TReason reason)
    {
        Open = open;
        Reason = reason;
    }

    /// <summary>
    /// Gets the new open state.
    /// </summary>
    public bool Open { get; }

    /// <summary>
    /// Gets the reason for the state change.
    /// </summary>
    public TReason Reason { get; }

    /// <summary>
    /// Gets a value indicating whether the open state change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether unmounting on close should be prevented.
    /// </summary>
    public virtual bool PreventUnmount { get; private set; }

    /// <summary>
    /// Cancels the open state change, preventing the component from opening or closing.
    /// </summary>
    public void Cancel() => IsCanceled = true;

    /// <summary>
    /// Prevents the component from unmounting when it closes.
    /// The popup remains in the DOM after the close transition completes.
    /// </summary>
    public virtual void PreventUnmountOnClose() => PreventUnmount = true;
}
