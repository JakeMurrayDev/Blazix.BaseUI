using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.PreviewCard;

/// <summary>
/// Non-generic interface for PreviewCardHandle that allows PreviewCardRoot to interact with handles
/// without knowing the payload type at compile time.
/// </summary>
public interface IPreviewCardHandle
{
    /// <summary>
    /// Gets a value indicating whether the preview card is currently open.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Gets the ID of the currently active trigger.
    /// </summary>
    string? ActiveTriggerId { get; }

    /// <summary>
    /// Gets the root ID associated with the subscribed preview card.
    /// </summary>
    internal string? RootId { get; }

    /// <summary>
    /// Opens the preview card and associates it with the trigger with the given ID.
    /// </summary>
    /// <param name="triggerId">ID of the trigger to associate with the preview card.</param>
    void Open(string triggerId);

    /// <summary>
    /// Closes the preview card.
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
    internal void Subscribe(IPreviewCardHandleSubscriber subscriber);

    /// <summary>
    /// Unsubscribes a component from handle state changes.
    /// </summary>
    internal void Unsubscribe(IPreviewCardHandleSubscriber subscriber);

    /// <summary>
    /// Called by root to sync state back to handle after processing.
    /// </summary>
    internal void SyncState(bool open, string? triggerId, object? payload);

    /// <summary>
    /// Called by root to expose its JavaScript root ID to detached triggers.
    /// </summary>
    internal void SyncRootId(string? rootId);
}

/// <summary>
/// A handle to control a preview card imperatively and to associate detached triggers with it.
/// The handle owns the preview card state and coordinates between detached Root and Trigger components.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the preview card.</typeparam>
public class PreviewCardHandle<TPayload> : ComponentHandleBase<TPayload, PreviewCardOpenChangeReason>, IPreviewCardHandle
{
    /// <summary>
    /// Occurs when the subscribed root ID changes.
    /// </summary>
    internal event Action? RootIdChanged;

    /// <summary>
    /// Gets the root ID associated with the subscribed preview card.
    /// </summary>
    internal string? RootId { get; private set; }

    /// <inheritdoc />
    string? IPreviewCardHandle.RootId => RootId;

    /// <inheritdoc />
    protected override PreviewCardOpenChangeReason ImperativeActionReason => PreviewCardOpenChangeReason.ImperativeAction;

    /// <inheritdoc />
    protected override string ComponentName => "PreviewCard";

    /// <inheritdoc />
    ElementReference? IPreviewCardHandle.GetTriggerElement(string? triggerId) => GetTriggerElement(triggerId);

    /// <inheritdoc />
    object? IPreviewCardHandle.GetTriggerPayloadAsObject(string? triggerId) => GetTriggerPayload(triggerId);

    /// <inheritdoc />
    void IPreviewCardHandle.Subscribe(IPreviewCardHandleSubscriber subscriber) => Subscribe(subscriber);

    /// <inheritdoc />
    void IPreviewCardHandle.Unsubscribe(IPreviewCardHandleSubscriber subscriber) => Unsubscribe(subscriber);

    /// <inheritdoc />
    void IPreviewCardHandle.SyncState(bool open, string? triggerId, object? payload)
        => SyncState(open, triggerId, payload is TPayload typedPayload ? typedPayload : default);

    /// <inheritdoc />
    void IPreviewCardHandle.SyncRootId(string? rootId)
    {
        if (RootId == rootId)
        {
            return;
        }

        RootId = rootId;
        RootIdChanged?.Invoke();
        NotifyStateChanged();
    }
}

/// <summary>
/// Non-generic version of PreviewCardHandle for scenarios where payload type is not needed.
/// </summary>
public sealed class PreviewCardHandle : PreviewCardHandle<object?>;

/// <summary>
/// Interface for components that subscribe to PreviewCardHandle state changes.
/// </summary>
internal interface IPreviewCardHandleSubscriber : IComponentHandleSubscriberBase<PreviewCardOpenChangeReason>;

/// <summary>
/// Factory methods for creating preview card handles.
/// </summary>
public static class PreviewCardHandleFactory
{
    /// <summary>
    /// Creates a new handle to connect a PreviewCard.Root with detached PreviewCard.Trigger components.
    /// </summary>
    /// <typeparam name="TPayload">The type of payload to pass to the preview card.</typeparam>
    /// <returns>A new PreviewCardHandle instance.</returns>
    public static PreviewCardHandle<TPayload> CreateHandle<TPayload>()
    {
        return new PreviewCardHandle<TPayload>();
    }

    /// <summary>
    /// Creates a new handle to connect a PreviewCard.Root with detached PreviewCard.Trigger components.
    /// </summary>
    /// <returns>A new PreviewCardHandle instance.</returns>
    public static PreviewCardHandle CreateHandle()
    {
        return new PreviewCardHandle();
    }
}
