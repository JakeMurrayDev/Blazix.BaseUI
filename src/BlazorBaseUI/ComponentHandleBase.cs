using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI;

/// <summary>
/// Interface for components that subscribe to handle state changes.
/// </summary>
/// <typeparam name="TReason">The open change reason enum type.</typeparam>
internal interface IComponentHandleSubscriberBase<TReason>
{
    /// <summary>
    /// Called when a trigger is registered with the handle.
    /// </summary>
    void OnTriggerRegistered(string triggerId, ElementReference? element);

    /// <summary>
    /// Called when a trigger is unregistered from the handle.
    /// </summary>
    void OnTriggerUnregistered(string triggerId);

    /// <summary>
    /// Called when a trigger's element reference is updated.
    /// </summary>
    void OnTriggerElementUpdated(string triggerId, ElementReference? element);

    /// <summary>
    /// Called when an open/close state change is requested.
    /// </summary>
    void OnOpenChangeRequested(bool open, TReason reason, string? triggerId, string? interactionType = null);

    /// <summary>
    /// Called when the handle state has changed.
    /// </summary>
    void OnStateChanged();
}

/// <summary>
/// Abstract base class for imperative component handles that coordinate between
/// detached Root and Trigger components. Manages trigger registration, subscriber
/// notification, and open/close state.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass between triggers and root.</typeparam>
/// <typeparam name="TReason">The open change reason enum type.</typeparam>
public abstract class ComponentHandleBase<TPayload, TReason>
{
    private readonly Dictionary<string, TriggerData> registeredTriggers = [];
    private readonly List<IComponentHandleSubscriberBase<TReason>> subscribers = [];

    private bool isOpen;
    private string? activeTriggerId;
    private TPayload? payload;

    /// <summary>
    /// Gets the list of subscribers for derived classes that need direct access.
    /// </summary>
    internal IReadOnlyList<IComponentHandleSubscriberBase<TReason>> Subscribers => subscribers;

    /// <summary>
    /// Gets a value indicating whether the component is currently open.
    /// </summary>
    public bool IsOpen => isOpen;

    /// <summary>
    /// Gets the ID of the currently active trigger.
    /// </summary>
    public string? ActiveTriggerId => activeTriggerId;

    /// <summary>
    /// Gets the current payload value.
    /// </summary>
    public TPayload? Payload => payload;

    /// <summary>
    /// Gets the imperative action reason value for this component's reason enum.
    /// Used by <see cref="Open"/> and <see cref="Close"/>.
    /// </summary>
    protected abstract TReason ImperativeActionReason { get; }

    /// <summary>
    /// Gets the component name for error messages.
    /// </summary>
    protected abstract string ComponentName { get; }

    /// <summary>
    /// Opens the component and associates it with the trigger with the given ID.
    /// </summary>
    /// <param name="triggerId">ID of the trigger to associate with the component.</param>
    /// <exception cref="ArgumentException">Thrown when triggerId is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no trigger is found with the given ID.</exception>
    public void Open(string triggerId)
    {
        if (string.IsNullOrEmpty(triggerId))
        {
            throw new ArgumentException("Trigger ID cannot be null or empty.", nameof(triggerId));
        }

        if (!registeredTriggers.ContainsKey(triggerId))
        {
            throw new InvalidOperationException($"{ComponentName}Handle.Open: No trigger found with id \"{triggerId}\".");
        }

        SetOpenInternal(true, ImperativeActionReason, triggerId);
    }

    /// <summary>
    /// Closes the component.
    /// </summary>
    public void Close()
    {
        SetOpenInternal(false, ImperativeActionReason, null);
    }

    /// <summary>
    /// Registers a trigger with this handle.
    /// </summary>
    internal void RegisterTrigger(string triggerId, ElementReference? element, TPayload? triggerPayload)
    {
        registeredTriggers[triggerId] = new TriggerData(element, triggerPayload);

        foreach (var subscriber in subscribers.ToArray())
        {
            subscriber.OnTriggerRegistered(triggerId, element);
        }
    }

    /// <summary>
    /// Unregisters a trigger from this handle.
    /// </summary>
    internal void UnregisterTrigger(string triggerId)
    {
        registeredTriggers.Remove(triggerId);

        foreach (var subscriber in subscribers.ToArray())
        {
            subscriber.OnTriggerUnregistered(triggerId);
        }
    }

    /// <summary>
    /// Updates the element reference for a trigger.
    /// </summary>
    internal void UpdateTriggerElement(string triggerId, ElementReference? element)
    {
        if (registeredTriggers.TryGetValue(triggerId, out var data))
        {
            registeredTriggers[triggerId] = data with { Element = element };

            foreach (var subscriber in subscribers.ToArray())
            {
                subscriber.OnTriggerElementUpdated(triggerId, element);
            }
        }
    }

    /// <summary>
    /// Updates the payload for a trigger.
    /// </summary>
    internal void UpdateTriggerPayload(string triggerId, TPayload? triggerPayload)
    {
        if (registeredTriggers.TryGetValue(triggerId, out var data))
        {
            if (EqualityComparer<TPayload?>.Default.Equals(data.Payload, triggerPayload))
            {
                return;
            }

            registeredTriggers[triggerId] = data with { Payload = triggerPayload };

            if (activeTriggerId == triggerId && isOpen)
            {
                payload = triggerPayload;
                NotifyStateChanged();
            }
        }
    }

    /// <summary>
    /// Gets the element reference for a trigger.
    /// </summary>
    internal ElementReference? GetTriggerElement(string? triggerId)
    {
        if (triggerId is not null && registeredTriggers.TryGetValue(triggerId, out var data))
        {
            return data.Element;
        }

        return null;
    }

    /// <summary>
    /// Gets the payload for a trigger.
    /// </summary>
    internal TPayload? GetTriggerPayload(string? triggerId)
    {
        if (triggerId is not null && registeredTriggers.TryGetValue(triggerId, out var data))
        {
            return data.Payload;
        }

        return default;
    }

    /// <summary>
    /// Tries to get the payload for a trigger.
    /// </summary>
    internal bool TryGetTriggerPayload(string? triggerId, out TPayload? triggerPayload)
    {
        if (triggerId is not null && registeredTriggers.TryGetValue(triggerId, out var data))
        {
            triggerPayload = data.Payload;
            return true;
        }

        triggerPayload = default;
        return false;
    }

    /// <summary>
    /// Subscribes a component to handle state changes.
    /// </summary>
    internal void Subscribe(IComponentHandleSubscriberBase<TReason> subscriber)
    {
        if (!subscribers.Contains(subscriber))
        {
            subscribers.Add(subscriber);
        }
    }

    /// <summary>
    /// Unsubscribes a component from handle state changes.
    /// </summary>
    internal void Unsubscribe(IComponentHandleSubscriberBase<TReason> subscriber)
    {
        subscribers.Remove(subscriber);
    }

    /// <summary>
    /// Called by triggers to request opening the component.
    /// </summary>
    internal virtual void RequestOpen(string triggerId, TReason reason, string? interactionType = null)
    {
        SetOpenInternal(true, reason, triggerId, interactionType);
    }

    /// <summary>
    /// Called by triggers to request closing the component.
    /// </summary>
    internal virtual void RequestClose(TReason reason, string? interactionType = null)
    {
        SetOpenInternal(false, reason, null, interactionType);
    }

    /// <summary>
    /// Called by root to sync state back to handle after processing.
    /// </summary>
    internal void SyncState(bool open, string? triggerId, TPayload? currentPayload)
    {
        var changed = isOpen != open
            || activeTriggerId != triggerId
            || !EqualityComparer<TPayload?>.Default.Equals(payload, currentPayload);

        isOpen = open;
        activeTriggerId = triggerId;
        payload = currentPayload;

        if (changed)
        {
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Core state change method. Validates, updates active trigger, and notifies subscribers.
    /// </summary>
    protected virtual void SetOpenInternal(bool nextOpen, TReason reason, string? triggerId, string? interactionType = null)
    {
        if (isOpen == nextOpen && (nextOpen == false || activeTriggerId == triggerId))
        {
            return;
        }

        if (nextOpen && triggerId is not null)
        {
            activeTriggerId = triggerId;
            payload = GetTriggerPayload(triggerId);
        }

        foreach (var subscriber in subscribers.ToArray())
        {
            subscriber.OnOpenChangeRequested(nextOpen, reason, triggerId, interactionType);
        }
    }

    /// <summary>
    /// Notifies all subscribers that the handle state has changed.
    /// </summary>
    protected void NotifyStateChanged()
    {
        foreach (var subscriber in subscribers.ToArray())
        {
            subscriber.OnStateChanged();
        }
    }

    private readonly record struct TriggerData(ElementReference? Element, TPayload? Payload);
}
