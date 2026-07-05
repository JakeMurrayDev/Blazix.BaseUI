namespace Blazix.BaseUI.Combobox;

/// <summary>
/// Provides details for a combobox selected value change.
/// </summary>
/// <typeparam name="TValue">The combobox item value type.</typeparam>
public sealed class ComboboxValueChangeEventArgs<TValue> : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComboboxValueChangeEventArgs{TValue}"/> class.
    /// </summary>
    /// <param name="value">The next single selected value.</param>
    /// <param name="values">The next multiple selected values.</param>
    /// <param name="reason">The reason the selected value changed.</param>
    public ComboboxValueChangeEventArgs(
        TValue? value,
        IReadOnlyList<TValue>? values,
        ComboboxChangeReason reason)
    {
        Value = value;
        Values = values;
        Reason = reason;
    }

    /// <summary>
    /// Gets the next single selected value.
    /// </summary>
    public TValue? Value { get; }

    /// <summary>
    /// Gets the next multiple selected values.
    /// </summary>
    public IReadOnlyList<TValue>? Values { get; }

    /// <summary>
    /// Gets the reason the selected value changed.
    /// </summary>
    public ComboboxChangeReason Reason { get; }

    /// <summary>
    /// Gets whether the consumer canceled the internal state update.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the selected value change, preventing the selected value from being applied.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}

/// <summary>
/// Provides details for a combobox input value change.
/// </summary>
public sealed class ComboboxInputValueChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComboboxInputValueChangeEventArgs"/> class.
    /// </summary>
    /// <param name="value">The new input value.</param>
    /// <param name="reason">The reason the input value changed.</param>
    public ComboboxInputValueChangeEventArgs(string value, ComboboxChangeReason reason)
    {
        Value = value;
        Reason = reason;
    }

    /// <summary>
    /// Gets the new input value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the reason the input value changed.
    /// </summary>
    public ComboboxChangeReason Reason { get; }

    /// <summary>
    /// Gets whether the consumer canceled the internal state update.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the input value change, preventing the input value from being applied.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}

/// <summary>
/// Provides details for an combobox popup open state change.
/// </summary>
public sealed class ComboboxOpenChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComboboxOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">Whether the popup is open.</param>
    /// <param name="reason">The reason the open state changed.</param>
    public ComboboxOpenChangeEventArgs(bool open, ComboboxChangeReason reason)
    {
        Open = open;
        Reason = reason;
    }

    /// <summary>
    /// Gets whether the popup is open.
    /// </summary>
    public bool Open { get; }

    /// <summary>
    /// Gets the reason the open state changed.
    /// </summary>
    public ComboboxChangeReason Reason { get; }

    /// <summary>
    /// Gets whether the consumer canceled the internal state update.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the open state change, preventing the popup state from being applied.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}

/// <summary>
/// Provides details for an combobox item highlight change.
/// </summary>
/// <typeparam name="TValue">The combobox item value type.</typeparam>
/// <param name="Value">The highlighted item value, or <see langword="default"/> when no item is highlighted.</param>
/// <param name="Index">The highlighted item index, or <c>-1</c> when no item is highlighted.</param>
/// <param name="Reason">The reason the highlight changed.</param>
public readonly record struct ComboboxHighlightEventArgs<TValue>(
    TValue? Value,
    int Index,
    ComboboxHighlightReason Reason);
