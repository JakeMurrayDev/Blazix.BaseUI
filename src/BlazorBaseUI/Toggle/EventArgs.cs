namespace BlazorBaseUI.Toggle;

/// <summary>
/// Provides data for the <see cref="Toggle.OnPressedChange"/> event.
/// </summary>
public class TogglePressedChangeEventArgs : EventArgs
{
    /// <summary>
    /// Gets the new pressed state of the toggle.
    /// </summary>
    public bool Pressed { get; }

    /// <summary>
    /// Gets whether the state change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TogglePressedChangeEventArgs"/> class.
    /// </summary>
    /// <param name="pressed">The new pressed state of the toggle.</param>
    public TogglePressedChangeEventArgs(bool pressed)
    {
        Pressed = pressed;
    }

    /// <summary>
    /// Cancels the pressed state change, preventing the toggle from updating.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}
