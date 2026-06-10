using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Dialog;

/// <summary>
/// Non-generic interface for DialogHandle that allows DialogRoot to interact with handles
/// without knowing the payload type at compile time.
/// </summary>
public interface IDialogHandle
{
    /// <summary>
    /// Gets a value indicating whether the dialog is currently open.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Gets the ID of the currently active trigger.
    /// </summary>
    string? ActiveTriggerId { get; }

    /// <summary>
    /// Gets the ID of the popup controlled by this handle.
    /// </summary>
    internal string? PopupId { get; }

    /// <summary>
    /// Opens the dialog and associates it with the trigger with the given ID.
    /// </summary>
    /// <param name="triggerId">ID of the trigger to associate with the dialog.</param>
    void Open(string? triggerId);

    /// <summary>
    /// Opens the dialog with a payload without associating a trigger.
    /// </summary>
    /// <param name="payload">The payload to pass to the dialog.</param>
    void OpenWithPayload(object? payload);

    /// <summary>
    /// Closes the dialog.
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
    internal void Subscribe(IDialogHandleSubscriber subscriber);

    /// <summary>
    /// Unsubscribes a component from handle state changes.
    /// </summary>
    internal void Unsubscribe(IDialogHandleSubscriber subscriber);

    /// <summary>
    /// Called by root to sync state back to handle after processing.
    /// </summary>
    internal void SyncState(bool open, string? triggerId, object? payload);

    /// <summary>
    /// Called by root to sync the popup ID back to detached triggers.
    /// </summary>
    internal void SyncPopupId(string? popupId);
}

/// <summary>
/// A handle to control a dialog imperatively and to associate detached triggers with it.
/// The handle owns the dialog state and coordinates between detached Root and Trigger components.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the dialog.</typeparam>
public class DialogHandle<TPayload> : ComponentHandleBase<TPayload, DialogOpenChangeReason>, IDialogHandle
{
    /// <inheritdoc />
    protected override DialogOpenChangeReason ImperativeActionReason => DialogOpenChangeReason.ImperativeAction;

    /// <inheritdoc />
    protected override string ComponentName => "Dialog";

    /// <summary>
    /// Gets the ID of the popup controlled by this handle.
    /// </summary>
    internal string? PopupId => HandledPopupId;

    /// <summary>
    /// Opens the dialog and optionally associates it with the trigger with the given ID.
    /// </summary>
    /// <param name="triggerId">ID of the trigger to associate with the dialog, or <see langword="null"/>.</param>
    public new void Open(string? triggerId)
    {
        SetOpenInternal(true, DialogOpenChangeReason.ImperativeAction, triggerId);
    }

    /// <summary>
    /// Opens the dialog with a payload without associating a trigger.
    /// </summary>
    /// <param name="payload">The payload to pass to the dialog.</param>
    public void OpenWithPayload(TPayload? payload)
    {
        foreach (var subscriber in Subscribers.ToArray())
        {
            if (subscriber is IDialogHandleSubscriber dialogSubscriber)
            {
                dialogSubscriber.OnOpenWithPayloadRequested(payload, DialogOpenChangeReason.ImperativeAction);
            }
        }
    }

    /// <inheritdoc />
    ElementReference? IDialogHandle.GetTriggerElement(string? triggerId) => GetTriggerElement(triggerId);

    /// <inheritdoc />
    object? IDialogHandle.GetTriggerPayloadAsObject(string? triggerId) => GetTriggerPayload(triggerId);

    /// <inheritdoc />
    void IDialogHandle.Subscribe(IDialogHandleSubscriber subscriber) => Subscribe(subscriber);

    /// <inheritdoc />
    void IDialogHandle.Unsubscribe(IDialogHandleSubscriber subscriber) => Unsubscribe(subscriber);

    /// <inheritdoc />
    void IDialogHandle.OpenWithPayload(object? payload)
        => OpenWithPayload(payload is TPayload typed ? typed : default);

    /// <inheritdoc />
    void IDialogHandle.SyncState(bool open, string? triggerId, object? payload)
        => SyncState(open, triggerId, payload is TPayload typedPayload ? typedPayload : default);

    /// <inheritdoc />
    string? IDialogHandle.PopupId => HandledPopupId;

    /// <inheritdoc />
    void IDialogHandle.SyncPopupId(string? popupId) => SyncPopupId(popupId);
}

/// <summary>
/// Non-generic version of DialogHandle for scenarios where payload type is not needed.
/// </summary>
public sealed class DialogHandle : DialogHandle<object?>;

/// <summary>
/// Interface for components that subscribe to DialogHandle state changes.
/// </summary>
internal interface IDialogHandleSubscriber : IComponentHandleSubscriberBase<DialogOpenChangeReason>
{
    /// <summary>
    /// Called when the dialog should open with a payload and no trigger association.
    /// </summary>
    void OnOpenWithPayloadRequested(object? payload, DialogOpenChangeReason reason);
}

/// <summary>
/// Factory methods for creating dialog handles.
/// </summary>
public static class DialogHandleFactory
{
    /// <summary>
    /// Creates a new handle to connect a Dialog.Root with detached Dialog.Trigger components.
    /// </summary>
    /// <typeparam name="TPayload">The type of payload to pass to the dialog.</typeparam>
    /// <returns>A new DialogHandle instance.</returns>
    public static DialogHandle<TPayload> CreateHandle<TPayload>()
    {
        return new DialogHandle<TPayload>();
    }

    /// <summary>
    /// Creates a new handle to connect a Dialog.Root with detached Dialog.Trigger components.
    /// </summary>
    /// <returns>A new DialogHandle instance.</returns>
    public static DialogHandle CreateHandle()
    {
        return new DialogHandle();
    }
}
