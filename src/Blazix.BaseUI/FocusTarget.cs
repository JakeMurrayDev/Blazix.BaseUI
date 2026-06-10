using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI;

/// <summary>
/// Represents a focus target for initial or final focus placement.
/// </summary>
public abstract record FocusTarget
{
    private FocusTarget() { }

    /// <summary>Don't move focus when the dialog opens/closes.</summary>
    public sealed record None : FocusTarget;

    /// <summary>Focus the first tabbable element (or the popup itself).</summary>
    public sealed record Default : FocusTarget;

    /// <summary>Focus a specific element.</summary>
    public sealed record Element(ElementReference Ref) : FocusTarget;

    /// <summary>Focus an element determined by the interaction type that opened the dialog.</summary>
    public sealed record Callback(Func<string, ElementReference?> Fn) : FocusTarget;

    /// <summary>
    /// Implicit conversion from <see cref="ElementReference"/> to <see cref="FocusTarget.Element"/>,
    /// preserving backward compatibility with the previous <c>ElementReference?</c> parameter API.
    /// </summary>
    public static implicit operator FocusTarget(ElementReference elementRef) =>
        new Element(elementRef);
}
