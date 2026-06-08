namespace Blazix.BaseUI.Autocomplete;

/// <summary>
/// Provides details for an autocomplete input value change.
/// </summary>
public sealed class AutocompleteValueChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AutocompleteValueChangeEventArgs"/> class.
    /// </summary>
    /// <param name="value">The new input value.</param>
    /// <param name="reason">The reason the value changed.</param>
    public AutocompleteValueChangeEventArgs(string value, AutocompleteChangeReason reason)
    {
        Value = value;
        Reason = reason;
    }

    /// <summary>
    /// Gets the new input value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the reason the value changed.
    /// </summary>
    public AutocompleteChangeReason Reason { get; }

    /// <summary>
    /// Gets whether the consumer canceled the internal state update.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the value change, preventing the input value from being applied.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}

/// <summary>
/// Provides details for an autocomplete popup open state change.
/// </summary>
public sealed class AutocompleteOpenChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AutocompleteOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">Whether the popup is open.</param>
    /// <param name="reason">The reason the open state changed.</param>
    public AutocompleteOpenChangeEventArgs(bool open, AutocompleteChangeReason reason)
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
    public AutocompleteChangeReason Reason { get; }

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
/// Provides details for an autocomplete item highlight change.
/// </summary>
/// <typeparam name="TValue">The autocomplete item value type.</typeparam>
/// <param name="Value">The highlighted item value, or <see langword="default"/> when no item is highlighted.</param>
/// <param name="Index">The highlighted item index, or <c>-1</c> when no item is highlighted.</param>
/// <param name="Reason">The reason the highlight changed.</param>
public readonly record struct AutocompleteHighlightEventArgs<TValue>(
    TValue? Value,
    int Index,
    AutocompleteHighlightReason Reason);
