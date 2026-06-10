namespace Blazix.BaseUI.Checkbox;

/// <summary>
/// Provides cascading state from <see cref="CheckboxRoot"/> to its child components.
/// </summary>
internal sealed class CheckboxRootContext
{
    /// <summary>
    /// Gets or sets whether the checkbox is checked.
    /// </summary>
    public bool Checked { get; set; }

    /// <summary>
    /// Gets or sets whether the checkbox is disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets whether the checkbox is read-only.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets whether the checkbox is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets whether the checkbox is in an indeterminate state.
    /// </summary>
    public bool Indeterminate { get; set; }

    /// <summary>
    /// Gets or sets the current state of the checkbox.
    /// </summary>
    public CheckboxRootState State { get; set; }
}
