namespace Blazix.BaseUI.CheckboxGroup;

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
    /// Gets the reason the value changed.
    /// </summary>
    public CheckboxGroupChangeReason Reason { get; }

    /// <summary>
    /// Gets a value indicating whether the value change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the event is allowed to propagate.
    /// </summary>
    public bool IsPropagationAllowed { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckboxGroupValueChangeEventArgs"/> class.
    /// </summary>
    /// <param name="value">The new value of the checkbox group.</param>
    /// <param name="reason">The reason the value changed.</param>
    public CheckboxGroupValueChangeEventArgs(
        string[] value,
        CheckboxGroupChangeReason reason = CheckboxGroupChangeReason.None)
    {
        Value = value;
        Reason = reason;
    }

    /// <summary>
    /// Cancels the value change, preventing the state from updating.
    /// </summary>
    public void Cancel() => IsCanceled = true;

    /// <summary>
    /// Allows the event to propagate in cases where Base UI would otherwise stop propagation.
    /// </summary>
    public void AllowPropagation() => IsPropagationAllowed = true;
}
