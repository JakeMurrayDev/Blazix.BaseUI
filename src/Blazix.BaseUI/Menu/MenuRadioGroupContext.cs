namespace Blazix.BaseUI.Menu;

/// <summary>
/// Defines the contract for a radio group context that manages single-selection state.
/// </summary>
internal interface IMenuRadioGroupContext<TValue>
{
    /// <summary>
    /// Gets the currently selected value.
    /// </summary>
    TValue? Value { get; }

    /// <summary>
    /// Gets whether the radio group is disabled.
    /// </summary>
    bool Disabled { get; }

    /// <summary>
    /// Sets the selected value asynchronously.
    /// </summary>
    Task SetValueAsync(TValue? newValue, MenuRadioGroupChangeEventArgs<TValue> eventArgs);
}

/// <summary>
/// Provides shared state for a <see cref="MenuRadioGroup{TValue}"/> and its descendant <see cref="MenuRadioItem{TValue}"/> components.
/// </summary>
/// <typeparam name="TValue">The type used to identify radio items.</typeparam>
internal sealed class MenuRadioGroupContext<TValue> : IMenuRadioGroupContext<TValue>
{
    private Func<TValue?> getValue = null!;
    private Func<TValue?, MenuRadioGroupChangeEventArgs<TValue>, Task> setValue = null!;

    /// <summary>
    /// Gets or sets whether the radio group is disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the delegate that retrieves the current value.
    /// </summary>
    public Func<TValue?> GetValue { get => getValue; init => getValue = value; }

    /// <summary>
    /// Gets or sets the delegate that sets the current value.
    /// </summary>
    public Func<TValue?, MenuRadioGroupChangeEventArgs<TValue>, Task> SetValue { get => setValue; init => setValue = value; }

    /// <inheritdoc />
    public TValue? Value => getValue();

    /// <inheritdoc />
    public async Task SetValueAsync(TValue? newValue, MenuRadioGroupChangeEventArgs<TValue> eventArgs)
    {
        await setValue(newValue, eventArgs);
    }
}
