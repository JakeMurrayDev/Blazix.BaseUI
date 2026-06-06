using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.ToggleGroup;

/// <summary>
/// Provides data for the <see cref="ToggleGroup.OnValueChange"/> event.
/// </summary>
public class ToggleGroupValueChangeEventArgs : EventArgs
{
    /// <summary>
    /// Gets the Base UI reason for the change.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets the new group value represented by the values of all pressed toggle buttons.
    /// </summary>
    public IReadOnlyList<string> Value { get; }

    /// <summary>
    /// Gets the Blazor mouse event associated with the change, when available.
    /// </summary>
    public MouseEventArgs? Event { get; }

    /// <summary>
    /// Gets whether the value change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Gets whether internal event propagation has been explicitly allowed.
    /// </summary>
    public bool IsPropagationAllowed { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToggleGroupValueChangeEventArgs"/> class.
    /// </summary>
    /// <param name="value">The new group value.</param>
    public ToggleGroupValueChangeEventArgs(IReadOnlyList<string> value)
        : this(value, eventArgs: null)
    {
    }

    internal ToggleGroupValueChangeEventArgs(IReadOnlyList<string> value, MouseEventArgs? eventArgs)
    {
        Reason = "none";
        Value = value;
        Event = eventArgs;
    }

    /// <summary>
    /// Cancels the value change, preventing the toggle group from updating.
    /// </summary>
    public void Cancel() => IsCanceled = true;

    /// <summary>
    /// Allows event propagation for Base UI handlers that would otherwise stop propagation.
    /// </summary>
    public void AllowPropagation() => IsPropagationAllowed = true;
}
