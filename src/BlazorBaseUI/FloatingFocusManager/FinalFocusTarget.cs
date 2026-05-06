using System;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.FloatingFocusManager;

/// <summary>
/// Result returned by a <see cref="FinalFocusTarget"/> callback, describing where focus should move on close.
/// </summary>
public readonly struct FinalFocusResult
{
    /// <summary>Use the default focus-restoration behavior (focus the trigger or previously focused element).</summary>
    public bool UseDefault { get; init; }

    /// <summary>Do nothing — leave focus wherever it currently is.</summary>
    public bool Suppress { get; init; }

    /// <summary>An explicit element to focus.</summary>
    public ElementReference? Target { get; init; }

    /// <summary>Use the default focus-restoration behavior.</summary>
    public static FinalFocusResult Default => new() { UseDefault = true };

    /// <summary>Suppress focus restoration entirely.</summary>
    public static FinalFocusResult None => new() { Suppress = true };

    /// <summary>Move focus to a specific element.</summary>
    public static FinalFocusResult To(ElementReference element) => new() { Target = element };
}

/// <summary>
/// Describes what should happen to focus when a floating component closes.
/// Mirrors React Base UI's <c>finalFocus</c> prop which accepts
/// <c>bool | RefObject | (closeType: InteractionType) =&gt; HTMLElement | bool | null</c>.
/// </summary>
/// <remarks>
/// <para><see cref="Default"/> — use the default behavior (focus trigger or previously focused element).</para>
/// <para><see cref="None"/> — do not move focus.</para>
/// <para>Use <see cref="From(ElementReference)"/> to move focus to a specific element,
/// or <see cref="From(Func{InteractionType, FinalFocusResult})"/> to decide at close time based on how the component was dismissed.</para>
/// </remarks>
public readonly struct FinalFocusTarget
{
    private readonly bool? boolValue;
    private readonly ElementReference? elementValue;
    private readonly Func<InteractionType, FinalFocusResult>? callbackValue;

    private FinalFocusTarget(
        bool? b,
        ElementReference? e,
        Func<InteractionType, FinalFocusResult>? cb)
    {
        boolValue = b;
        elementValue = e;
        callbackValue = cb;
    }

    /// <summary>Use the default focus-restoration behavior. Equivalent to React's <c>finalFocus={true}</c>.</summary>
    public static FinalFocusTarget Default => new(true, null, null);

    /// <summary>Do not move focus. Equivalent to React's <c>finalFocus={false}</c>.</summary>
    public static FinalFocusTarget None => new(false, null, null);

    /// <summary>Move focus to a specific element.</summary>
    public static FinalFocusTarget From(ElementReference element) => new(null, element, null);

    /// <summary>
    /// Decide focus behavior at close time based on the user's last interaction (mouse, touch, pen, keyboard).
    /// </summary>
    public static FinalFocusTarget From(Func<InteractionType, FinalFocusResult> callback) => new(null, null, callback);

    /// <summary>Implicit conversion from <see cref="bool"/>. <c>true</c> = <see cref="Default"/>, <c>false</c> = <see cref="None"/>.</summary>
    public static implicit operator FinalFocusTarget(bool v) => v ? Default : None;

    /// <summary>Implicit conversion from <see cref="ElementReference"/>.</summary>
    public static implicit operator FinalFocusTarget(ElementReference e) => From(e);

    /// <summary>Implicit conversion from a close-type callback.</summary>
    public static implicit operator FinalFocusTarget(Func<InteractionType, FinalFocusResult> fn) => From(fn);

    /// <summary>Resolves this target to a concrete result for the given close interaction type.</summary>
    public FinalFocusResult Resolve(InteractionType closeType)
    {
        if (boolValue is true) return FinalFocusResult.Default;
        if (boolValue is false) return FinalFocusResult.None;
        if (elementValue is { } el) return FinalFocusResult.To(el);
        if (callbackValue is not null) return callbackValue(closeType);
        return FinalFocusResult.Default;
    }
}
