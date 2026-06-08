using Microsoft.AspNetCore.Components.Web;

namespace Blazix.BaseUI.Checkbox;

/// <summary>
/// Provides data for the <see cref="CheckboxRoot.OnCheckedChange"/> event.
/// </summary>
public class CheckboxCheckedChangeEventArgs : EventArgs
{
    /// <summary>
    /// Gets the new checked state of the checkbox.
    /// </summary>
    public bool Checked { get; }

    /// <summary>
    /// Gets the reason the checked state changed.
    /// </summary>
    public CheckboxChangeReason Reason { get; }

    /// <summary>
    /// Gets whether the Shift key was pressed for the input event that changed the checkbox, when available.
    /// </summary>
    public bool ShiftKey { get; }

    /// <summary>
    /// Gets whether the Ctrl key was pressed for the input event that changed the checkbox, when available.
    /// </summary>
    public bool CtrlKey { get; }

    /// <summary>
    /// Gets whether the Alt key was pressed for the input event that changed the checkbox, when available.
    /// </summary>
    public bool AltKey { get; }

    /// <summary>
    /// Gets whether the Meta key was pressed for the input event that changed the checkbox, when available.
    /// </summary>
    public bool MetaKey { get; }

    /// <summary>
    /// Gets a value indicating whether the checked change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the event is allowed to propagate.
    /// </summary>
    public bool IsPropagationAllowed { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckboxCheckedChangeEventArgs"/> class.
    /// </summary>
    /// <param name="isChecked">The new checked state of the checkbox.</param>
    /// <param name="reason">The reason the checked state changed.</param>
    public CheckboxCheckedChangeEventArgs(
        bool isChecked,
        CheckboxChangeReason reason = CheckboxChangeReason.None)
        : this(isChecked, reason, null)
    {
    }

    internal CheckboxCheckedChangeEventArgs(
        bool isChecked,
        CheckboxChangeReason reason,
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

    /// <summary>
    /// Allows the event to propagate in cases where Base UI would otherwise stop propagation.
    /// </summary>
    public void AllowPropagation() => IsPropagationAllowed = true;
}
