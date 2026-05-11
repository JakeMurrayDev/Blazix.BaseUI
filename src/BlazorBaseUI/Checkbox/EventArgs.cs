namespace BlazorBaseUI.Checkbox;

/// <summary>
/// Provides data for the <see cref="CheckboxRoot.OnCheckedChange"/> event.
/// </summary>
public class CheckboxCheckedChangeEventArgs : EventArgs
{
    /// <summary>
    /// Gets the new checked state of the checkbox.
    /// </summary>
    public bool Checked { get; }

    /// <summary>
    /// Gets a value indicating whether the checked change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckboxCheckedChangeEventArgs"/> class.
    /// </summary>
    /// <param name="isChecked">The new checked state of the checkbox.</param>
    public CheckboxCheckedChangeEventArgs(bool isChecked)
    {
        Checked = isChecked;
    }

    /// <summary>
    /// Cancels the checked change, preventing the state from updating.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}
