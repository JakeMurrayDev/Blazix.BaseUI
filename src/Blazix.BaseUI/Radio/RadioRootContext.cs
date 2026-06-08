namespace Blazix.BaseUI.Radio;

/// <summary>
/// Provides cascading state shared between radio sub-components.
/// </summary>
internal sealed class RadioRootContext
{
    /// <summary>
    /// Gets or sets whether the radio button is currently selected.
    /// </summary>
    public bool Checked { get; set; }

    /// <summary>
    /// Gets or sets whether the radio button is disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets whether the radio button is read-only.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets whether the radio button is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the current state of the radio button.
    /// </summary>
    public RadioRootState State { get; set; } = RadioRootState.Default;
}
