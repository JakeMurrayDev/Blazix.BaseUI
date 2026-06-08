namespace Blazix.BaseUI.Switch;

/// <summary>
/// Provides cascading state from <see cref="SwitchRoot"/> to its child components.
/// </summary>
internal sealed class SwitchRootContext
{
    /// <summary>
    /// Gets or sets whether the switch is checked.
    /// </summary>
    public bool Checked { get; set; }

    /// <summary>
    /// Gets or sets whether the switch is disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets whether the switch is read-only.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets whether the switch is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the current state of the switch.
    /// </summary>
    public SwitchRootState State { get; set; } = null!;
}
