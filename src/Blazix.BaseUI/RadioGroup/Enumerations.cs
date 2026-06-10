namespace Blazix.BaseUI.RadioGroup;

/// <summary>
/// Identifies the data attributes rendered on the radio group root element.
/// </summary>
public enum RadioGroupDataAttribute
{
    /// <summary>Present when the radio group is disabled.</summary>
    Disabled,

    /// <summary>Present when the radio group is read-only.</summary>
    ReadOnly,

    /// <summary>Present when the radio group is required.</summary>
    Required,

    /// <summary>Present when the radio group is valid.</summary>
    Valid,

    /// <summary>Present when the radio group is invalid.</summary>
    Invalid,

    /// <summary>Present when the radio group has been touched.</summary>
    Touched,

    /// <summary>Present when the radio group value has changed from its initial value.</summary>
    Dirty,

    /// <summary>Present when a radio option is selected.</summary>
    Filled,

    /// <summary>Present when the radio group has focus.</summary>
    Focused
}
