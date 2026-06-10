namespace Blazix.BaseUI.Drawer;

/// <summary>
/// Provides data for drawer open state changes.
/// </summary>
public sealed class DrawerOpenChangeEventArgs : OpenChangeEventArgs<DrawerOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DrawerOpenChangeEventArgs"/> class.
    /// </summary>
    public DrawerOpenChangeEventArgs(bool open, DrawerOpenChangeReason reason) : base(open, reason) { }
}

/// <summary>
/// Provides data for drawer snap point changes.
/// </summary>
public sealed class DrawerSnapPointChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DrawerSnapPointChangeEventArgs"/> class.
    /// </summary>
    public DrawerSnapPointChangeEventArgs(DrawerSnapPoint? snapPoint, DrawerOpenChangeReason reason)
    {
        SnapPoint = snapPoint;
        Reason = reason;
    }

    /// <summary>
    /// Gets the requested snap point.
    /// </summary>
    public DrawerSnapPoint? SnapPoint { get; }

    /// <summary>
    /// Gets the reason associated with the snap point change.
    /// </summary>
    public DrawerOpenChangeReason Reason { get; }

    /// <summary>
    /// Gets a value indicating whether the snap point change was canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the snap point change.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}
