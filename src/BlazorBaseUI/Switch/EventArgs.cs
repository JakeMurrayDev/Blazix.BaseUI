namespace BlazorBaseUI.Switch;

/// <summary>
/// Provides data for the <see cref="SwitchRoot.OnCheckedChange"/> event.
/// </summary>
public class SwitchCheckedChangeEventArgs : EventArgs
{
    /// <summary>
    /// Gets the new checked state of the switch.
    /// </summary>
    public bool Checked { get; }

    /// <summary>
    /// Gets a value indicating whether the checked change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchCheckedChangeEventArgs"/> class.
    /// </summary>
    /// <param name="isChecked">The new checked state of the switch.</param>
    public SwitchCheckedChangeEventArgs(bool isChecked)
    {
        Checked = isChecked;
    }

    /// <summary>
    /// Cancels the checked change, preventing the state from updating.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}
