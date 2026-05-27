namespace BlazorBaseUI.Drawer;

/// <summary>
/// Provides imperative drawer actions.
/// </summary>
public sealed class DrawerRootActions
{
    /// <summary>
    /// Gets or sets an action that unmounts a closed drawer when unmounting was prevented.
    /// </summary>
    public Action? Unmount { get; set; }

    /// <summary>
    /// Gets or sets an action that closes the drawer imperatively.
    /// </summary>
    public Action? Close { get; set; }
}
