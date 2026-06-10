namespace Blazix.BaseUI.Select;

/// <summary>
/// Provides data for the <see cref="SelectRoot{TValue}.OnOpenChange"/> event.
/// </summary>
public sealed class SelectOpenChangeEventArgs : OpenChangeEventArgs<SelectOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectOpenChangeEventArgs"/> class.
    /// </summary>
    public SelectOpenChangeEventArgs(bool open, SelectOpenChangeReason reason) : base(open, reason) { }
}

/// <summary>
/// Provides data for the <see cref="SelectRoot{TValue}.OnValueChange"/> event.
/// </summary>
/// <typeparam name="TValue">The type of value used by the select.</typeparam>
public sealed class SelectValueChangeEventArgs<TValue> : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectValueChangeEventArgs{TValue}"/> class.
    /// </summary>
    public SelectValueChangeEventArgs(TValue? value, IReadOnlyList<TValue>? values = null)
    {
        Value = value;
        Values = values;
    }

    /// <summary>
    /// Gets the newly selected value.
    /// </summary>
    public TValue? Value { get; }

    /// <summary>
    /// Gets the next selected values when the select allows multiple selection.
    /// </summary>
    public IReadOnlyList<TValue>? Values { get; }

    /// <summary>
    /// Gets whether the value change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the value change, preventing the selected value from updating.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}
