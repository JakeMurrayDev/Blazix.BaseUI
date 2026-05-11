namespace BlazorBaseUI.RadioGroup;

/// <summary>
/// Provides data for the radio group value change event. Can be canceled to prevent the value update.
/// </summary>
/// <typeparam name="TValue">The type of the radio group value.</typeparam>
public class RadioGroupValueChangeEventArgs<TValue> : EventArgs
{
    /// <summary>
    /// Gets the new radio group value.
    /// </summary>
    public TValue? Value { get; }

    /// <summary>
    /// Gets whether the value change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RadioGroupValueChangeEventArgs{TValue}"/> class.
    /// </summary>
    /// <param name="value">The new value.</param>
    public RadioGroupValueChangeEventArgs(TValue? value)
    {
        Value = value;
    }

    /// <summary>
    /// Cancels the value change, preventing the radio group from updating.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}
