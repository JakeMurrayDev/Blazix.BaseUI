namespace Blazix.BaseUI.Menu;

/// <summary>
/// Provides data for the <see cref="MenuRoot.OnOpenChange"/> event.
/// </summary>
public sealed class MenuOpenChangeEventArgs : OpenChangeEventArgs<MenuOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MenuOpenChangeEventArgs"/> class.
    /// </summary>
    public MenuOpenChangeEventArgs(bool open, MenuOpenChangeReason reason, object? payload = null) : base(open, reason)
    {
        Payload = payload;
    }

    /// <summary>
    /// Gets the optional payload associated with the state change.
    /// </summary>
    public object? Payload { get; }
}

/// <summary>
/// Provides data for the <see cref="MenuRadioGroup.OnValueChange"/> event.
/// </summary>
public sealed class MenuRadioGroupChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MenuRadioGroupChangeEventArgs"/> class.
    /// </summary>
    public MenuRadioGroupChangeEventArgs(object? value, MenuRadioGroupChangeReason reason)
    {
        Value = value;
        Reason = reason;
    }

    /// <summary>
    /// Gets the newly selected value.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the reason the selected value changed.
    /// </summary>
    public MenuRadioGroupChangeReason Reason { get; }

    /// <summary>
    /// Gets whether the value change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the value change, preventing the selection from updating.
    /// </summary>
    public void Cancel()
    {
        IsCanceled = true;
    }
}

/// <summary>
/// Provides data for the <see cref="MenuCheckboxItem.OnCheckedChange"/> event.
/// </summary>
public sealed class MenuCheckboxItemChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MenuCheckboxItemChangeEventArgs"/> class.
    /// </summary>
    public MenuCheckboxItemChangeEventArgs(bool @checked, MenuCheckboxItemChangeReason reason)
    {
        Checked = @checked;
        Reason = reason;
    }

    /// <summary>
    /// Gets the new checked state.
    /// </summary>
    public bool Checked { get; }

    /// <summary>
    /// Gets the reason the checked state changed.
    /// </summary>
    public MenuCheckboxItemChangeReason Reason { get; }

    /// <summary>
    /// Gets whether the checked state change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the checked state change, preventing the checkbox from toggling.
    /// </summary>
    public void Cancel()
    {
        IsCanceled = true;
    }
}
