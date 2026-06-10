namespace Blazix.BaseUI.CheckboxGroup;

/// <summary>
/// Describes why a checkbox group value changed.
/// </summary>
public enum CheckboxGroupChangeReason
{
    /// <summary>
    /// No specific interaction reason was supplied.
    /// </summary>
    None
}

/// <summary>
/// Represents the cycling status of a parent checkbox within a <see cref="CheckboxGroup"/>.
/// </summary>
internal enum ParentCheckboxStatus
{
    /// <summary>
    /// All child checkboxes are checked.
    /// </summary>
    On,

    /// <summary>
    /// No child checkboxes are checked.
    /// </summary>
    Off,

    /// <summary>
    /// Some child checkboxes are checked.
    /// </summary>
    Mixed
}
