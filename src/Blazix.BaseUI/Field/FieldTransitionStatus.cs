namespace Blazix.BaseUI.Field;

/// <summary>
/// Represents the transition status of a conditionally rendered field part.
/// </summary>
public enum FieldTransitionStatus
{
    /// <summary>The element is entering.</summary>
    Starting,

    /// <summary>The element is exiting.</summary>
    Ending,

    /// <summary>The element is mounted without an active transition.</summary>
    Idle
}
