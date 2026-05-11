namespace BlazorBaseUI.Slider;

/// <summary>
/// Provides data for the slider value change event. Can be canceled to prevent the value update.
/// </summary>
/// <typeparam name="TValue">The type of the slider value (<see cref="double"/> or <see cref="T:double[]"/>).</typeparam>
public class SliderValueChangeEventArgs<TValue> : EventArgs where TValue : notnull
{
    /// <summary>
    /// Gets the new slider value.
    /// </summary>
    public TValue Value { get; }

    /// <summary>
    /// Gets the reason that triggered the value change.
    /// </summary>
    public SliderChangeReason Reason { get; }

    /// <summary>
    /// Gets the index of the thumb that initiated the change.
    /// </summary>
    public int ActiveThumbIndex { get; }

    /// <summary>
    /// Gets whether the value change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SliderValueChangeEventArgs{TValue}"/> class.
    /// </summary>
    public SliderValueChangeEventArgs(TValue value, SliderChangeReason reason, int activeThumbIndex)
    {
        Value = value;
        Reason = reason;
        ActiveThumbIndex = activeThumbIndex;
    }

    /// <summary>
    /// Cancels the value change, preventing the slider from updating.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}

/// <summary>
/// Provides data for the slider value committed event, fired when the user finishes an interaction (e.g., pointer up or keyboard input).
/// </summary>
/// <typeparam name="TValue">The type of the slider value (<see cref="double"/> or <see cref="T:double[]"/>).</typeparam>
public class SliderValueCommittedEventArgs<TValue> : EventArgs where TValue : notnull
{
    /// <summary>
    /// Gets the committed slider value.
    /// </summary>
    public TValue Value { get; }

    /// <summary>
    /// Gets the reason that triggered the commit.
    /// </summary>
    public SliderChangeReason Reason { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SliderValueCommittedEventArgs{TValue}"/> class.
    /// </summary>
    public SliderValueCommittedEventArgs(TValue value, SliderChangeReason reason)
    {
        Value = value;
        Reason = reason;
    }
}
