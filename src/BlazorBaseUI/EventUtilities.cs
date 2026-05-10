using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI;

/// <summary>
/// Utilities for handling event forwarding when components are used via RenderAs.
/// Components that support being RenderAs targets should use these utilities to invoke
/// any event handlers passed through AdditionalAttributes.
/// </summary>
internal static class EventUtilities
{
    /// <summary>
    /// Invokes an onclick handler from AdditionalAttributes if present.
    /// Use this in components that have their own click handling but may be used as RenderAs targets.
    /// </summary>
    public static Task InvokeOnClickAsync(IReadOnlyDictionary<string, object>? additionalAttributes, MouseEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onclick", e);
    }

    /// <summary>
    /// Invokes an onkeydown handler from AdditionalAttributes if present.
    /// </summary>
    public static Task InvokeOnKeyDownAsync(IReadOnlyDictionary<string, object>? additionalAttributes, KeyboardEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onkeydown", e);
    }

    public static Task InvokeOnMouseEnterAsync(IReadOnlyDictionary<string, object>? additionalAttributes, MouseEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onmouseenter", e);
    }

    public static Task InvokeOnMouseLeaveAsync(IReadOnlyDictionary<string, object>? additionalAttributes, MouseEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onmouseleave", e);
    }

    public static Task InvokeOnMouseMoveAsync(IReadOnlyDictionary<string, object>? additionalAttributes, MouseEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onmousemove", e);
    }

    public static Task InvokeOnFocusAsync(IReadOnlyDictionary<string, object>? additionalAttributes, FocusEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onfocus", e);
    }

    public static Task InvokeOnBlurAsync(IReadOnlyDictionary<string, object>? additionalAttributes, FocusEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onblur", e);
    }

    public static Task InvokeOnPointerDownAsync(IReadOnlyDictionary<string, object>? additionalAttributes, PointerEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onpointerdown", e);
    }

    public static Task InvokeOnPointerEnterAsync(IReadOnlyDictionary<string, object>? additionalAttributes, PointerEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onpointerenter", e);
    }

    public static Task InvokeOnPointerMoveAsync(IReadOnlyDictionary<string, object>? additionalAttributes, PointerEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onpointermove", e);
    }

    public static Task InvokeOnMouseUpAsync(IReadOnlyDictionary<string, object>? additionalAttributes, MouseEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "onmouseup", e);
    }

    public static Task InvokeOnTouchStartAsync(IReadOnlyDictionary<string, object>? additionalAttributes, TouchEventArgs e)
    {
        return InvokeEventAsync(additionalAttributes, "ontouchstart", e);
    }

    private static async Task InvokeEventAsync<TEvent>(IReadOnlyDictionary<string, object>? additionalAttributes, string attribute, TEvent e) where TEvent : EventArgs
    {
        if (additionalAttributes is null)
        {
            return;
        }

        // Try exact match first, then case-insensitive
        if (!additionalAttributes.TryGetValue(attribute, out var value))
        {
            // Try to find case-insensitive match (e.g., "OnClick" vs "onclick")
            foreach (var kvp in additionalAttributes)
            {
                if (string.Equals(kvp.Key, attribute, StringComparison.OrdinalIgnoreCase))
                {
                    value = kvp.Value;
                    break;
                }
            }

            if (value is null)
            {
                return;
            }
        }

        switch (value)
        {
            case EventCallback<TEvent> callback:
                await callback.InvokeAsync(e);
                break;
            case EventCallback nonGenericCallback:
                await nonGenericCallback.InvokeAsync();
                break;
            case Action action:
                action();
                break;
            case Action<TEvent> actionWithArgs:
                actionWithArgs(e);
                break;
            case Func<Task> asyncAction:
                await asyncAction();
                break;
            case Func<TEvent, Task> asyncActionWithArgs:
                await asyncActionWithArgs(e);
                break;
            case MulticastDelegate del:
                var result = del.DynamicInvoke(e);
                if (result is Task task)
                {
                    await task;
                }
                break;
        }
    }
}
