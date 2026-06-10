namespace Blazix.BaseUI;

/// <summary>
/// Provides helper methods for applying transition-related data attributes to elements.
/// Centralizes the data-open/data-closed/data-starting-style/data-ending-style pattern
/// used across all popup and panel components.
/// Mirrors the attribute output of React's useTransitionStatus hook.
/// </summary>
internal static class TransitionAttributeHelper
{
    /// <summary>
    /// Applies the standard set of open/close and transition data attributes.
    /// </summary>
    /// <param name="attributes">The attribute dictionary to modify.</param>
    /// <param name="open">Whether the component is currently in the open state.</param>
    /// <param name="transitionStatus">The current transition status.</param>
    public static void ApplyTransitionAttributes(
        IDictionary<string, object> attributes,
        bool open,
        TransitionStatus transitionStatus)
    {
        attributes["data-open"] = open;
        attributes["data-closed"] = !open;
        attributes["data-starting-style"] = transitionStatus == TransitionStatus.Starting;
        attributes["data-ending-style"] = transitionStatus == TransitionStatus.Ending;
    }

    /// <summary>
    /// Applies only the transition style data attributes (data-starting-style and data-ending-style).
    /// Used when the component already sets data-open/data-closed separately.
    /// </summary>
    /// <param name="attributes">The attribute dictionary to modify.</param>
    /// <param name="transitionStatus">The current transition status.</param>
    public static void ApplyTransitionStyleAttributes(
        IDictionary<string, object> attributes,
        TransitionStatus transitionStatus)
    {
        attributes["data-starting-style"] = transitionStatus == TransitionStatus.Starting;
        attributes["data-ending-style"] = transitionStatus == TransitionStatus.Ending;
    }
}
