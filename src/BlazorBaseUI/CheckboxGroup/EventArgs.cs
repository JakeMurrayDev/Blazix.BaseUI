namespace BlazorBaseUI.CheckboxGroup;

/// <summary>
/// Provides data for the <see cref="CheckboxGroup.OnValueChange"/> event.
/// </summary>
public class CheckboxGroupValueChangeEventArgs : EventArgs
{
    /// <summary>
    /// Gets the new value of the checkbox group.
    /// </summary>
    public string[] Value { get; }

    /// <summary>
    /// Gets a value indicating whether the value change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckboxGroupValueChangeEventArgs"/> class.
    /// </summary>
    /// <param name="value">The new value of the checkbox group.</param>
    public CheckboxGroupValueChangeEventArgs(string[] value)
    {
        Value = value;
    }

    /// <summary>
    /// Cancels the value change, preventing the state from updating.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}
