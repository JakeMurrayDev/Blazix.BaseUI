namespace Blazix.BaseUI.NumberField;

/// <summary>
/// Provides data for the <see cref="NumberFieldRoot.OnValueChange"/> callback,
/// fired when the numeric value changes.
/// </summary>
public sealed class NumberFieldValueChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NumberFieldValueChangeEventArgs"/> class.
    /// </summary>
    /// <param name="value">The new numeric value.</param>
    /// <param name="reason">The reason the value changed.</param>
    /// <param name="direction">The direction of the change, if applicable.</param>
    public NumberFieldValueChangeEventArgs(
        double? value,
        NumberFieldChangeReason reason,
        int? direction = null)
    {
        Value = value;
        Reason = reason;
        Direction = direction;
    }

    /// <summary>
    /// Gets the new numeric value of the field.
    /// </summary>
    public double? Value { get; }

    /// <summary>
    /// Gets the reason that triggered the value change.
    /// </summary>
    public NumberFieldChangeReason Reason { get; }

    /// <summary>
    /// Gets the direction of the change, where <c>1</c> is increment and <c>-1</c> is decrement.
    /// </summary>
    public int? Direction { get; }

    /// <summary>
    /// Gets whether the value change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the value change, preventing it from being applied.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}

/// <summary>
/// Provides data for the <see cref="NumberFieldRoot.OnValueCommitted"/> callback,
/// fired when a value is committed after interaction completes.
/// </summary>
public sealed class NumberFieldValueCommittedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NumberFieldValueCommittedEventArgs"/> class.
    /// </summary>
    /// <param name="value">The committed numeric value.</param>
    /// <param name="reason">The reason the value was committed.</param>
    public NumberFieldValueCommittedEventArgs(
        double? value,
        NumberFieldChangeReason reason)
    {
        Value = value;
        Reason = reason;
    }

    /// <summary>
    /// Gets the committed numeric value of the field.
    /// </summary>
    public double? Value { get; }

    /// <summary>
    /// Gets the reason that triggered the value commit.
    /// </summary>
    public NumberFieldChangeReason Reason { get; }
}
