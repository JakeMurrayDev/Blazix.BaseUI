using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Toggle;

/// <summary>
/// Provides data for the <see cref="Toggle.OnPressedChange"/> event.
/// </summary>
public class TogglePressedChangeEventArgs : EventArgs
{
    /// <summary>
    /// Gets the Base UI reason for the change.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets the new pressed state of the toggle.
    /// </summary>
    public bool Pressed { get; }

    /// <summary>
    /// Gets the Blazor mouse event associated with the change, when available.
    /// </summary>
    public MouseEventArgs? Event { get; }

    /// <summary>
    /// Gets whether the state change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Gets whether internal event propagation has been explicitly allowed.
    /// </summary>
    public bool IsPropagationAllowed { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TogglePressedChangeEventArgs"/> class.
    /// </summary>
    /// <param name="pressed">The new pressed state of the toggle.</param>
    public TogglePressedChangeEventArgs(bool pressed)
        : this(pressed, eventArgs: null, isCanceled: false, isPropagationAllowed: false)
    {
    }

    internal TogglePressedChangeEventArgs(
        bool pressed,
        MouseEventArgs? eventArgs,
        bool isCanceled,
        bool isPropagationAllowed)
    {
        Reason = "none";
        Pressed = pressed;
        Event = eventArgs;
        IsCanceled = isCanceled;
        IsPropagationAllowed = isPropagationAllowed;
    }

    /// <summary>
    /// Cancels the pressed state change, preventing the toggle from updating.
    /// </summary>
    public void Cancel() => IsCanceled = true;

    /// <summary>
    /// Allows event propagation for Base UI handlers that would otherwise stop propagation.
    /// </summary>
    public void AllowPropagation() => IsPropagationAllowed = true;
}
