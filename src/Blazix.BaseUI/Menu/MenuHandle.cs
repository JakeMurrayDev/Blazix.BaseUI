using Blazix.BaseUI.MenuBar;
using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Menu;

/// <summary>
/// Non-generic interface for MenuHandle that allows MenuRoot to interact with handles
/// without knowing the payload type at compile time.
/// </summary>
public interface IMenuHandle
{
    /// <summary>
    /// Gets a value indicating whether the menu is currently open.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Gets the ID of the currently active trigger.
    /// </summary>
    string? ActiveTriggerId { get; }

    /// <summary>
    /// Gets the unique identifier for the popup element.
    /// </summary>
    string? PopupId { get; }

    /// <summary>
    /// Gets the menubar context associated with the active trigger.
    /// </summary>
    internal MenuBarRootContext? MenubarContext { get; }

    /// <summary>
    /// Opens the menu and associates it with the trigger with the given ID.
    /// </summary>
    /// <param name="triggerId">ID of the trigger to associate with the menu.</param>
    void Open(string triggerId);

    /// <summary>
    /// Closes the menu.
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
    /// Tries to get the payload for a trigger as an object.
    /// </summary>
    internal bool TryGetTriggerPayloadAsObject(string? triggerId, out object? triggerPayload);

    /// <summary>
    /// Registers a trigger with this handle.
    /// </summary>
    internal void RegisterTrigger(string triggerId, ElementReference? element, object? triggerPayload);

    /// <summary>
    /// Registers the menubar context associated with a trigger.
    /// </summary>
    internal void RegisterMenubarContext(string triggerId, MenuBarRootContext context);

    /// <summary>
    /// Unregisters a trigger from this handle.
    /// </summary>
    internal void UnregisterTrigger(string triggerId);

    /// <summary>
    /// Unregisters the menubar context associated with a trigger.
    /// </summary>
    internal void UnregisterMenubarContext(string triggerId);

    /// <summary>
    /// Updates the element reference for a trigger.
    /// </summary>
    internal void UpdateTriggerElement(string triggerId, ElementReference? element);

    /// <summary>
    /// Updates the payload for a trigger.
    /// </summary>
    internal void UpdateTriggerPayload(string triggerId, object? triggerPayload);

    /// <summary>
    /// Requests that the menu opens for the specified trigger.
    /// </summary>
    internal void RequestOpen(string triggerId, MenuOpenChangeReason reason, string? interactionType = null);

    /// <summary>
    /// Requests that the menu closes.
    /// </summary>
    internal void RequestClose(MenuOpenChangeReason reason, string? interactionType = null);

    /// <summary>
    /// Subscribes a component to handle state changes.
    /// </summary>
    internal void Subscribe(IMenuHandleSubscriber subscriber);

    /// <summary>
    /// Unsubscribes a component from handle state changes.
    /// </summary>
    internal void Unsubscribe(IMenuHandleSubscriber subscriber);

    /// <summary>
    /// Called by root to sync state back to handle after processing.
    /// </summary>
    internal void SyncState(bool open, string? triggerId, object? payload);

    /// <summary>
    /// Sets the unique identifier for the popup element.
    /// </summary>
    internal void SetPopupId(string? popupId);
}

/// <summary>
/// A handle to control a menu imperatively and to associate detached triggers with it.
/// The handle owns the menu state and coordinates between detached Root and Trigger components.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the menu.</typeparam>
public class MenuHandle<TPayload> : ComponentHandleBase<TPayload, MenuOpenChangeReason>, IMenuHandle
{
    private readonly Dictionary<string, MenuBarRootContext> menuBarContexts = [];
    private string? popupId;

    /// <inheritdoc />
    protected override MenuOpenChangeReason ImperativeActionReason => MenuOpenChangeReason.ImperativeAction;

    /// <inheritdoc />
    protected override string ComponentName => "Menu";

    /// <summary>
    /// Gets the unique identifier for the popup element.
    /// </summary>
    public string? PopupId => popupId;

    /// <inheritdoc />
    MenuBarRootContext? IMenuHandle.MenubarContext => MenubarContext;

    /// <summary>
    /// Gets the menubar context associated with the active trigger.
    /// </summary>
    internal MenuBarRootContext? MenubarContext
    {
        get
        {
            if (ActiveTriggerId is not null && menuBarContexts.TryGetValue(ActiveTriggerId, out var activeContext))
            {
                return activeContext;
            }

            return menuBarContexts.Values.FirstOrDefault();
        }
    }

    /// <inheritdoc />
    ElementReference? IMenuHandle.GetTriggerElement(string? triggerId) => GetTriggerElement(triggerId);

    /// <inheritdoc />
    object? IMenuHandle.GetTriggerPayloadAsObject(string? triggerId) => GetTriggerPayload(triggerId);

    /// <inheritdoc />
    bool IMenuHandle.TryGetTriggerPayloadAsObject(string? triggerId, out object? triggerPayload)
    {
        var found = TryGetTriggerPayload(triggerId, out var typedPayload);
        triggerPayload = typedPayload;
        return found;
    }

    /// <inheritdoc />
    void IMenuHandle.RegisterTrigger(string triggerId, ElementReference? element, object? triggerPayload)
        => RegisterTrigger(triggerId, element, triggerPayload is TPayload typedPayload ? typedPayload : default);

    /// <inheritdoc />
    void IMenuHandle.RegisterMenubarContext(string triggerId, MenuBarRootContext context)
        => RegisterMenubarContext(triggerId, context);

    /// <inheritdoc />
    void IMenuHandle.UnregisterTrigger(string triggerId)
    {
        UnregisterMenubarContext(triggerId);
        UnregisterTrigger(triggerId);
    }

    /// <inheritdoc />
    void IMenuHandle.UnregisterMenubarContext(string triggerId) => UnregisterMenubarContext(triggerId);

    /// <inheritdoc />
    void IMenuHandle.UpdateTriggerElement(string triggerId, ElementReference? element) => UpdateTriggerElement(triggerId, element);

    /// <inheritdoc />
    void IMenuHandle.UpdateTriggerPayload(string triggerId, object? triggerPayload)
        => UpdateTriggerPayload(triggerId, triggerPayload is TPayload typedPayload ? typedPayload : default);

    /// <inheritdoc />
    void IMenuHandle.RequestOpen(string triggerId, MenuOpenChangeReason reason, string? interactionType)
        => RequestOpen(triggerId, reason, interactionType);

    /// <inheritdoc />
    void IMenuHandle.RequestClose(MenuOpenChangeReason reason, string? interactionType)
        => RequestClose(reason, interactionType);

    /// <inheritdoc />
    void IMenuHandle.Subscribe(IMenuHandleSubscriber subscriber) => Subscribe(subscriber);

    /// <inheritdoc />
    void IMenuHandle.Unsubscribe(IMenuHandleSubscriber subscriber) => Unsubscribe(subscriber);

    /// <inheritdoc />
    void IMenuHandle.SyncState(bool open, string? triggerId, object? payload)
        => SyncState(open, triggerId, payload is TPayload typedPayload ? typedPayload : default);

    /// <inheritdoc />
    void IMenuHandle.SetPopupId(string? value) { popupId = value; }

    /// <summary>
    /// Registers the menubar context associated with a trigger.
    /// </summary>
    internal void RegisterMenubarContext(string triggerId, MenuBarRootContext context)
    {
        var previousContext = MenubarContext;
        menuBarContexts[triggerId] = context;

        if (!ReferenceEquals(previousContext, MenubarContext))
        {
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Unregisters the menubar context associated with a trigger.
    /// </summary>
    internal void UnregisterMenubarContext(string triggerId)
    {
        var previousContext = MenubarContext;

        if (menuBarContexts.Remove(triggerId) && !ReferenceEquals(previousContext, MenubarContext))
        {
            NotifyStateChanged();
        }
    }
}

/// <summary>
/// Non-generic version of MenuHandle for scenarios where payload type is not needed.
/// </summary>
public sealed class MenuHandle : MenuHandle<object?>;

/// <summary>
/// Interface for components that subscribe to MenuHandle state changes.
/// </summary>
internal interface IMenuHandleSubscriber : IComponentHandleSubscriberBase<MenuOpenChangeReason>;

/// <summary>
/// Factory methods for creating menu handles.
/// </summary>
public static class MenuHandleFactory
{
    /// <summary>
    /// Creates a new handle to connect a Menu.Root with detached Menu.Trigger components.
    /// </summary>
    /// <typeparam name="TPayload">The type of payload to pass to the menu.</typeparam>
    /// <returns>A new MenuHandle instance.</returns>
    public static MenuHandle<TPayload> CreateHandle<TPayload>()
    {
        return new MenuHandle<TPayload>();
    }

    /// <summary>
    /// Creates a new handle to connect a Menu.Root with detached Menu.Trigger components.
    /// </summary>
    /// <returns>A new MenuHandle instance.</returns>
    public static MenuHandle CreateHandle()
    {
        return new MenuHandle();
    }
}
