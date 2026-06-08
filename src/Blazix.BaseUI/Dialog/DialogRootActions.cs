namespace Blazix.BaseUI.Dialog;

/// <summary>
/// Provides imperative actions for controlling a dialog programmatically.
/// </summary>
public sealed class DialogRootActions
{
    /// <summary>
    /// Gets the action that forces the dialog to unmount from the DOM.
    /// Useful when the dialog's animation is controlled by an external library.
    /// </summary>
    public Action? Unmount { get; internal set; }

    /// <summary>
    /// Gets the action that closes the dialog imperatively.
    /// </summary>
    public Action? Close { get; internal set; }
}
