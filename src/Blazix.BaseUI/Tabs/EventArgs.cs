namespace Blazix.BaseUI.Tabs;

/// <summary>
/// Provides data for the <see cref="TabsRoot{TValue}.OnValueChange"/> event,
/// including the ability to cancel the tab change.
/// </summary>
/// <typeparam name="TValue">The type of value used to identify tabs.</typeparam>
public sealed class TabsValueChangeEventArgs<TValue> : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TabsValueChangeEventArgs{TValue}"/> class.
    /// </summary>
    /// <param name="value">The requested tab value.</param>
    /// <param name="activationDirection">The activation direction relative to the previous tab.</param>
    /// <param name="reason">The reason the value changed.</param>
    /// <param name="sourceEventArgs">The Blazor event args that triggered the change, if available.</param>
    public TabsValueChangeEventArgs(
        TValue? value,
        ActivationDirection activationDirection,
        TabsValueChangeReason reason = TabsValueChangeReason.None,
        EventArgs? sourceEventArgs = null)
    {
        Value = value;
        ActivationDirection = activationDirection;
        Reason = reason;
        SourceEventArgs = sourceEventArgs;
    }

    /// <summary>
    /// Gets the new tab value being activated.
    /// </summary>
    public TValue? Value { get; }

    /// <summary>
    /// Gets the direction of the activation relative to the previously active tab.
    /// </summary>
    public ActivationDirection ActivationDirection { get; }

    /// <summary>
    /// Gets the reason that triggered the value change.
    /// </summary>
    public TabsValueChangeReason Reason { get; }

    /// <summary>
    /// Gets the Blazor event args that triggered the change, if available.
    /// Automatic changes use <see langword="null"/>.
    /// </summary>
    public EventArgs? SourceEventArgs { get; }

    /// <summary>
    /// Gets a value indicating whether the tab change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the tab change, preventing the active tab from updating.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}
