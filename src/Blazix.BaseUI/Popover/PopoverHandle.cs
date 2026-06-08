using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Popover;

/// <summary>
/// Non-generic interface for PopoverHandle that allows PopoverRoot to interact with handles
/// without knowing the payload type at compile time.
/// </summary>
public interface IPopoverHandle
{
    /// <summary>
    /// Gets a value indicating whether the popover is currently open.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Gets the ID of the currently active trigger.
    /// </summary>
    string? ActiveTriggerId { get; }

    /// <summary>
    /// Opens the popover and associates it with the trigger with the given ID.
    /// </summary>
    /// <param name="triggerId">ID of the trigger to associate with the popover.</param>
    void Open(string triggerId);

    /// <summary>
    /// Closes the popover.
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
    internal void Subscribe(IPopoverHandleSubscriber subscriber);

    /// <summary>
    /// Unsubscribes a component from handle state changes.
    /// </summary>
    internal void Unsubscribe(IPopoverHandleSubscriber subscriber);

    /// <summary>
    /// Called by root to sync state back to handle after processing.
    /// </summary>
    internal void SyncState(bool open, string? triggerId, object? payload);
}

/// <summary>
/// A handle to control a popover imperatively and to associate detached triggers with it.
/// The handle owns the popover state and coordinates between detached Root and Trigger components.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the popover.</typeparam>
public class PopoverHandle<TPayload> : ComponentHandleBase<TPayload, PopoverOpenChangeReason>, IPopoverHandle
{
    /// <inheritdoc />
    protected override PopoverOpenChangeReason ImperativeActionReason => PopoverOpenChangeReason.ImperativeAction;

    /// <inheritdoc />
    protected override string ComponentName => "Popover";

    /// <inheritdoc />
    ElementReference? IPopoverHandle.GetTriggerElement(string? triggerId) => GetTriggerElement(triggerId);

    /// <inheritdoc />
    object? IPopoverHandle.GetTriggerPayloadAsObject(string? triggerId) => GetTriggerPayload(triggerId);

    /// <inheritdoc />
    void IPopoverHandle.Subscribe(IPopoverHandleSubscriber subscriber) => Subscribe(subscriber);

    /// <inheritdoc />
    void IPopoverHandle.Unsubscribe(IPopoverHandleSubscriber subscriber) => Unsubscribe(subscriber);

    /// <inheritdoc />
    void IPopoverHandle.SyncState(bool open, string? triggerId, object? payload)
        => SyncState(open, triggerId, payload is TPayload typedPayload ? typedPayload : default);
}

/// <summary>
/// Non-generic version of PopoverHandle for scenarios where payload type is not needed.
/// </summary>
public sealed class PopoverHandle : PopoverHandle<object?>;

/// <summary>
/// Interface for components that subscribe to PopoverHandle state changes.
/// </summary>
internal interface IPopoverHandleSubscriber : IComponentHandleSubscriberBase<PopoverOpenChangeReason>;

/// <summary>
/// Factory methods for creating popover handles.
/// </summary>
public static class PopoverHandleFactory
{
    /// <summary>
    /// Creates a new handle to connect a Popover.Root with detached Popover.Trigger components.
    /// </summary>
    /// <typeparam name="TPayload">The type of payload to pass to the popover.</typeparam>
    /// <returns>A new PopoverHandle instance.</returns>
    public static PopoverHandle<TPayload> CreateHandle<TPayload>()
    {
        return new PopoverHandle<TPayload>();
    }

    /// <summary>
    /// Creates a new handle to connect a Popover.Root with detached Popover.Trigger components.
    /// </summary>
    /// <returns>A new PopoverHandle instance.</returns>
    public static PopoverHandle CreateHandle()
    {
        return new PopoverHandle();
    }
}
