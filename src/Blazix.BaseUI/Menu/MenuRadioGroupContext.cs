namespace Blazix.BaseUI.Menu;

/// <summary>
/// Defines the contract for a radio group context that manages single-selection state.
/// </summary>
internal interface IMenuRadioGroupContext
{
    /// <summary>
    /// Gets the currently selected value.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Gets whether the radio group is disabled.
    /// </summary>
    bool Disabled { get; }

    /// <summary>
    /// Sets the selected value asynchronously.
    /// </summary>
    Task SetValueAsync(object? newValue, MenuRadioGroupChangeEventArgs eventArgs);
}

/// <summary>
/// Provides shared state for a <see cref="MenuRadioGroup"/> and its descendant <see cref="MenuRadioItem"/> components.
/// </summary>
internal sealed class MenuRadioGroupContext : IMenuRadioGroupContext
{
    private Func<object?> getValue = null!;
    private Func<object?, MenuRadioGroupChangeEventArgs, Task> setValue = null!;

    /// <summary>
    /// Gets or sets whether the radio group is disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the delegate that retrieves the current value.
    /// </summary>
    public Func<object?> GetValue { get => getValue; init => getValue = value; }

    /// <summary>
    /// Gets or sets the delegate that sets the current value.
    /// </summary>
    public Func<object?, MenuRadioGroupChangeEventArgs, Task> SetValue { get => setValue; init => setValue = value; }

    /// <inheritdoc />
    public object? Value => getValue();

    /// <inheritdoc />
    public async Task SetValueAsync(object? newValue, MenuRadioGroupChangeEventArgs eventArgs)
    {
        await setValue(newValue, eventArgs);
    }
}
