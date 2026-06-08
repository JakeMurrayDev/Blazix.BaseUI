using Microsoft.AspNetCore.Components.Web;

namespace Blazix.BaseUI.Switch;

/// <summary>
/// Provides data for the <see cref="SwitchRoot.OnCheckedChange"/> event.
/// </summary>
public class SwitchCheckedChangeEventArgs : EventArgs
{
    /// <summary>
    /// Gets the new checked state of the switch.
    /// </summary>
    public bool Checked { get; }

    /// <summary>
    /// Gets the reason the checked state changed.
    /// </summary>
    public SwitchChangeReason Reason { get; }

    /// <summary>
    /// Gets whether the Shift key was pressed for the input event that changed the switch, when available.
    /// </summary>
    public bool ShiftKey { get; }

    /// <summary>
    /// Gets whether the Ctrl key was pressed for the input event that changed the switch, when available.
    /// </summary>
    public bool CtrlKey { get; }

    /// <summary>
    /// Gets whether the Alt key was pressed for the input event that changed the switch, when available.
    /// </summary>
    public bool AltKey { get; }

    /// <summary>
    /// Gets whether the Meta key was pressed for the input event that changed the switch, when available.
    /// </summary>
    public bool MetaKey { get; }

    /// <summary>
    /// Gets a value indicating whether the checked change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchCheckedChangeEventArgs"/> class.
    /// </summary>
    /// <param name="isChecked">The new checked state of the switch.</param>
    public SwitchCheckedChangeEventArgs(bool isChecked)
        : this(isChecked, SwitchChangeReason.None, null)
    {
    }

    internal SwitchCheckedChangeEventArgs(
        bool isChecked,
        SwitchChangeReason reason,
        MouseEventArgs? mouseEventArgs)
    {
        Checked = isChecked;
        Reason = reason;
        ShiftKey = mouseEventArgs?.ShiftKey ?? false;
        CtrlKey = mouseEventArgs?.CtrlKey ?? false;
        AltKey = mouseEventArgs?.AltKey ?? false;
        MetaKey = mouseEventArgs?.MetaKey ?? false;
    }

    /// <summary>
    /// Cancels the checked change, preventing the state from updating.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}
