namespace BlazorBaseUI.ToggleGroup;

/// <summary>
/// Provides data for the <see cref="ToggleGroup.OnValueChange"/> event.
/// </summary>
public class ToggleGroupValueChangeEventArgs : EventArgs
{
    /// <summary>
    /// Gets the new group value represented by the values of all pressed toggle buttons.
    /// </summary>
    public IReadOnlyList<string> Value { get; }

    /// <summary>
    /// Gets whether the value change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToggleGroupValueChangeEventArgs"/> class.
    /// </summary>
    /// <param name="value">The new group value.</param>
    public ToggleGroupValueChangeEventArgs(IReadOnlyList<string> value)
    {
        Value = value;
    }

    /// <summary>
    /// Cancels the value change, preventing the toggle group from updating.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}
