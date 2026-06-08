using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Tooltip;

/// <summary>
/// Non-generic interface for TooltipHandle that allows TooltipRoot to interact with handles
/// without knowing the payload type at compile time.
/// </summary>
public interface ITooltipHandle
{
    /// <summary>
    /// Gets a value indicating whether the tooltip is currently open.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Gets the ID of the currently active trigger.
    /// </summary>
    string? ActiveTriggerId { get; }

    /// <summary>
    /// Opens the tooltip and associates it with the trigger with the given ID.
    /// </summary>
    /// <param name="triggerId">ID of the trigger to associate with the tooltip.</param>
    void Open(string triggerId);

    /// <summary>
    /// Closes the tooltip.
    /// </summary>
    void Close();

    /// <summary>
    /// Gets the element reference for a trigger.
    /// </summary>
    internal ElementReference? GetTriggerElement(string? triggerId);

    /// <summary>
    /// Gets the payload for a trigger as an object.
    /// </summary>
    internal object? GetTriggerPayloadAsObject(string? triggerId);

    /// <summary>
    /// Subscribes a component to handle state changes.
    /// </summary>
    internal void Subscribe(ITooltipHandleSubscriber subscriber);

    /// <summary>
    /// Unsubscribes a component from handle state changes.
    /// </summary>
    internal void Unsubscribe(ITooltipHandleSubscriber subscriber);

    /// <summary>
    /// Called by root to sync state back to handle after processing.
    /// </summary>
    internal void SyncState(bool open, string? triggerId, object? payload);
}

/// <summary>
/// A handle to control a tooltip imperatively and to associate detached triggers with it.
/// The handle owns the tooltip state and coordinates between detached Root and Trigger components.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the tooltip.</typeparam>
public class TooltipHandle<TPayload> : ComponentHandleBase<TPayload, TooltipOpenChangeReason>, ITooltipHandle
{
    /// <inheritdoc />
    protected override TooltipOpenChangeReason ImperativeActionReason => TooltipOpenChangeReason.ImperativeAction;

    /// <inheritdoc />
    protected override string ComponentName => "Tooltip";

    /// <inheritdoc />
    ElementReference? ITooltipHandle.GetTriggerElement(string? triggerId) => GetTriggerElement(triggerId);

    /// <inheritdoc />
    object? ITooltipHandle.GetTriggerPayloadAsObject(string? triggerId) => GetTriggerPayload(triggerId);

    /// <inheritdoc />
    void ITooltipHandle.Subscribe(ITooltipHandleSubscriber subscriber) => Subscribe(subscriber);

    /// <inheritdoc />
    void ITooltipHandle.Unsubscribe(ITooltipHandleSubscriber subscriber) => Unsubscribe(subscriber);

    /// <inheritdoc />
    void ITooltipHandle.SyncState(bool open, string? triggerId, object? payload)
        => SyncState(open, triggerId, payload is TPayload typedPayload ? typedPayload : default);
}

/// <summary>
/// Non-generic version of TooltipHandle for scenarios where payload type is not needed.
/// </summary>
public sealed class TooltipHandle : TooltipHandle<object?>;

/// <summary>
/// Interface for components that subscribe to TooltipHandle state changes.
/// </summary>
internal interface ITooltipHandleSubscriber : IComponentHandleSubscriberBase<TooltipOpenChangeReason>;

/// <summary>
/// Factory methods for creating tooltip handles.
/// </summary>
public static class TooltipHandleFactory
{
    /// <summary>
    /// Creates a new handle to connect a Tooltip.Root with detached Tooltip.Trigger components.
    /// </summary>
    /// <typeparam name="TPayload">The type of payload to pass to the tooltip.</typeparam>
    /// <returns>A new TooltipHandle instance.</returns>
    public static TooltipHandle<TPayload> CreateHandle<TPayload>()
    {
        return new TooltipHandle<TPayload>();
    }

    /// <summary>
    /// Creates a new handle to connect a Tooltip.Root with detached Tooltip.Trigger components.
    /// </summary>
    /// <returns>A new TooltipHandle instance.</returns>
    public static TooltipHandle CreateHandle()
    {
        return new TooltipHandle();
    }
}
