namespace Blazix.BaseUI.NavigationMenu;

/// <summary>
/// Provides data for the <see cref="NavigationMenuRoot.OnValueChange"/> event.
/// </summary>
public sealed class NavigationMenuValueChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationMenuValueChangeEventArgs"/> class.
    /// </summary>
    public NavigationMenuValueChangeEventArgs(string? value, NavigationMenuCloseReason reason = NavigationMenuCloseReason.None)
    {
        Value = value;
        Reason = reason;
    }

    /// <summary>
    /// Gets the new value indicating which item's content is displayed, or <see langword="null"/> if the menu is closing.
    /// </summary>
    public string? Value { get; }

    /// <summary>
    /// Gets the interaction reason for the value change.
    /// </summary>
    public NavigationMenuCloseReason Reason { get; }

    /// <summary>
    /// Gets whether the value change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the value change, preventing the navigation menu from switching items.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}
