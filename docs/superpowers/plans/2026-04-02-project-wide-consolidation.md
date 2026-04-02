# Project-Wide Consolidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate ~1,500+ lines of duplication across C# Handle classes, EventArgs, Extensions, and JS modules by extracting shared base classes and wiring up existing shared JS modules.

**Architecture:** C# consolidation uses abstract base classes (`ComponentHandleBase<TPayload, TReason>`, `OpenChangeEventArgs<TReason>`) that component-specific subclasses extend with thin overrides. JS consolidation wires existing shared modules (`composite.js`, `press-and-hold.js`, `popup-viewport.js`) to their consumers, extends `floating.js` with transition/hover helpers, and creates a new `activation.js` module.

**Tech Stack:** .NET 10, Blazor, C# 14 (extension blocks), JavaScript ES modules

**Spec:** `docs/superpowers/specs/2026-04-02-project-wide-consolidation-design.md`

---

## File Map

### New Files
| File | Responsibility |
|---|---|
| `src/BlazorBaseUI/ComponentHandleBase.cs` | Abstract base class for imperative handle pattern + `IComponentHandleSubscriberBase<TReason>` interface |
| `src/BlazorBaseUI/OpenChangeEventArgs.cs` | Abstract base class for open/close event args |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-activation.js` | Shared Space/Enter key activation for non-button elements |

### Modified Files — C#
| File | Change |
|---|---|
| `src/BlazorBaseUI/Dialog/DialogHandle.cs` | Extend `ComponentHandleBase`, keep Dialog-specific `OpenWithPayload` |
| `src/BlazorBaseUI/Popover/PopoverHandle.cs` | Extend `ComponentHandleBase`, keep interactionType on `RequestClose` |
| `src/BlazorBaseUI/Menu/MenuHandle.cs` | Extend `ComponentHandleBase`, keep `PopupId`/`SetPopupId` |
| `src/BlazorBaseUI/Tooltip/TooltipHandle.cs` | Extend `ComponentHandleBase`, straight inheritance |
| `src/BlazorBaseUI/PreviewCard/PreviewCardHandle.cs` | Extend `ComponentHandleBase`, straight inheritance |
| `src/BlazorBaseUI/Dialog/EventArgs.cs` | Extend `OpenChangeEventArgs<DialogOpenChangeReason>` |
| `src/BlazorBaseUI/Popover/EventArgs.cs` | Extend `OpenChangeEventArgs<PopoverOpenChangeReason>` |
| `src/BlazorBaseUI/Menu/EventArgs.cs` | Extend `OpenChangeEventArgs<MenuOpenChangeReason>` |
| `src/BlazorBaseUI/Tooltip/EventArgs.cs` | Extend `OpenChangeEventArgs<TooltipOpenChangeReason>` |
| `src/BlazorBaseUI/PreviewCard/EventArgs.cs` | Extend `OpenChangeEventArgs<PreviewCardOpenChangeReason>` |
| `src/BlazorBaseUI/Select/EventArgs.cs` | Extend `OpenChangeEventArgs<SelectOpenChangeReason>` |
| `src/BlazorBaseUI/Collapsible/EventArgs.cs` | Extend `OpenChangeEventArgs<CollapsibleOpenChangeReason>` |
| `src/BlazorBaseUI/Dialog/DialogRoot.razor` | Update `Canceled` → `IsCanceled`, `PreventUnmountingOnClose` → `PreventUnmount` |
| `src/BlazorBaseUI/Popover/PopoverRoot.razor` | Update `Canceled` → `IsCanceled`, `UnmountPrevented` → `PreventUnmount` |
| `src/BlazorBaseUI/Tooltip/TooltipRoot.razor` | Update `Canceled` → `IsCanceled` |
| `src/BlazorBaseUI/PreviewCard/PreviewCardRoot.razor` | Update `Canceled` → `IsCanceled` |
| `src/BlazorBaseUI/Accordion/AccordionRoot.razor` | Update `Canceled` → `IsCanceled` (if applicable) |
| `src/BlazorBaseUI/Extensions.cs` | Add `ParseSide`/`ParseAlign` methods |
| `src/BlazorBaseUI/Popover/Extensions.cs` | Remove `ParseSide`/`ParseAlign` |
| `src/BlazorBaseUI/Menu/Extensions.cs` | Remove `ParseSide`/`ParseAlign` |
| `AGENTS.md` | Add JS interop file architecture rule |

### Modified Files — JS
| File | Change |
|---|---|
| `src/BlazorBaseUI/wwwroot/blazor-baseui-floating.js` | Add exported transition helpers + hover interaction wrappers |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-dialog.js` | Delete private transition utils, import from floating.js |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-tooltip.js` | Import transition helpers + hover wrappers from floating.js |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-preview-card.js` | Import transition helpers + hover wrappers from floating.js |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-popover.js` | Import hover wrappers from floating.js |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-tabs.js` | Import from composite.js, remove inline nav |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-toggle.js` | Import from composite.js, remove inline nav |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-radio.js` | Import from composite.js, remove inline nav |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-toolbar.js` | Import from composite.js, remove inline nav |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-menubar.js` | Import from composite.js, remove inline nav |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-accordion-trigger.js` | Import from composite.js, remove inline nav |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-button.js` | Import from activation.js, remove inline key handlers |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-toggle.js` | Import from activation.js (in addition to composite.js) |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-switch.js` | Import from activation.js, remove inline key handlers |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-checkbox.js` | Import from activation.js, remove inline key handlers |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-number-field.js` | Import from press-and-hold.js, remove inline auto-change |
| `src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js` | Import from popup-viewport.js, remove inline viewport logic |

---

## Task 1: Create `ComponentHandleBase<TPayload, TReason>`

**Files:**
- Create: `src/BlazorBaseUI/ComponentHandleBase.cs`

- [ ] **Step 1: Create the base class file**

```csharp
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
    protected IReadOnlyList<IComponentHandleSubscriberBase<TReason>> Subscribers => subscribers;

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
        isOpen = open;
        activeTriggerId = triggerId;
        payload = currentPayload;
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
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build src/BlazorBaseUI/BlazorBaseUI.csproj`
Expected: 0 errors (new file, no consumers yet)

- [ ] **Step 3: Commit**

```bash
git add src/BlazorBaseUI/ComponentHandleBase.cs
git commit -m "Add ComponentHandleBase abstract class for shared handle infrastructure"
```

---

## Task 2: Migrate All 5 Handle Classes

**Files:**
- Modify: `src/BlazorBaseUI/Dialog/DialogHandle.cs`
- Modify: `src/BlazorBaseUI/Popover/PopoverHandle.cs`
- Modify: `src/BlazorBaseUI/Menu/MenuHandle.cs`
- Modify: `src/BlazorBaseUI/Tooltip/TooltipHandle.cs`
- Modify: `src/BlazorBaseUI/PreviewCard/PreviewCardHandle.cs`
- Modify: `src/BlazorBaseUI/Dialog/DialogRoot.razor` (subscriber interface change)
- Modify: `src/BlazorBaseUI/Popover/PopoverRoot.razor` (subscriber interface change)
- Modify: `src/BlazorBaseUI/Menu/MenuRoot.razor` (subscriber interface change)
- Modify: `src/BlazorBaseUI/Tooltip/TooltipRoot.razor` (subscriber interface change)
- Modify: `src/BlazorBaseUI/PreviewCard/PreviewCardRoot.razor` (subscriber interface change)

Each handle migration follows the same pattern. The component-specific handle class:
1. Removes all fields/methods that now live in the base
2. Keeps its component-specific `IXxxHandle` interface (unchanged public API)
3. Changes `IXxxHandleSubscriber` to extend `IComponentHandleSubscriberBase<TReason>`
4. Adds only component-specific overrides/additions

- [ ] **Step 1: Migrate TooltipHandle (simplest — straight inheritance, no overrides)**

Replace `src/BlazorBaseUI/Tooltip/TooltipHandle.cs` with:

```csharp
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tooltip;

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
    ElementReference? ITooltipHandle.GetTriggerElement(string? triggerId)
    {
        return GetTriggerElement(triggerId);
    }

    /// <inheritdoc />
    object? ITooltipHandle.GetTriggerPayloadAsObject(string? triggerId)
    {
        return GetTriggerPayload(triggerId);
    }

    /// <inheritdoc />
    void ITooltipHandle.Subscribe(ITooltipHandleSubscriber subscriber)
    {
        Subscribe(subscriber);
    }

    /// <inheritdoc />
    void ITooltipHandle.Unsubscribe(ITooltipHandleSubscriber subscriber)
    {
        Unsubscribe(subscriber);
    }

    /// <inheritdoc />
    void ITooltipHandle.SyncState(bool open, string? triggerId, object? payload)
    {
        SyncState(open, triggerId, payload is TPayload typedPayload ? typedPayload : default);
    }
}

/// <summary>
/// Non-generic version of TooltipHandle for scenarios where payload type is not needed.
/// </summary>
public sealed class TooltipHandle : TooltipHandle<object?>;

/// <summary>
/// Interface for components that subscribe to TooltipHandle state changes.
/// </summary>
internal interface ITooltipHandleSubscriber : IComponentHandleSubscriberBase<TooltipOpenChangeReason>
{
}

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
```

- [ ] **Step 2: Migrate PreviewCardHandle (identical pattern to Tooltip)**

Replace `src/BlazorBaseUI/PreviewCard/PreviewCardHandle.cs`. Same pattern as Tooltip: extend `ComponentHandleBase<TPayload, PreviewCardOpenChangeReason>`, keep `IPreviewCardHandle` interface unchanged, change `IPreviewCardHandleSubscriber` to extend `IComponentHandleSubscriberBase<PreviewCardOpenChangeReason>` with empty body. Set `ImperativeActionReason => PreviewCardOpenChangeReason.ImperativeAction` and `ComponentName => "PreviewCard"`.

- [ ] **Step 3: Migrate MenuHandle (adds `PopupId`/`SetPopupId`)**

Replace `src/BlazorBaseUI/Menu/MenuHandle.cs`. Same base pattern plus:

```csharp
public class MenuHandle<TPayload> : ComponentHandleBase<TPayload, MenuOpenChangeReason>, IMenuHandle
{
    private string? popupId;

    /// <inheritdoc />
    protected override MenuOpenChangeReason ImperativeActionReason => MenuOpenChangeReason.ImperativeAction;

    /// <inheritdoc />
    protected override string ComponentName => "Menu";

    /// <summary>
    /// Gets the unique identifier for the popup element.
    /// </summary>
    public string? PopupId => popupId;

    // ... explicit interface implementations same as Tooltip pattern ...

    /// <inheritdoc />
    void IMenuHandle.SetPopupId(string? value)
    {
        popupId = value;
    }
}
```

Keep `IMenuHandle` interface unchanged (it has `PopupId` property and `SetPopupId` internal method). Change `IMenuHandleSubscriber` to extend `IComponentHandleSubscriberBase<MenuOpenChangeReason>` with empty body.

- [ ] **Step 4: Migrate PopoverHandle (passes `interactionType` through `RequestClose`)**

Replace `src/BlazorBaseUI/Popover/PopoverHandle.cs`. Same base pattern — no override needed since the base `RequestClose` already accepts `string? interactionType = null`. Set `ImperativeActionReason => PopoverOpenChangeReason.ImperativeAction` and `ComponentName => "Popover"`. Change `IPopoverHandleSubscriber` to extend `IComponentHandleSubscriberBase<PopoverOpenChangeReason>` with empty body.

- [ ] **Step 5: Migrate DialogHandle (adds `OpenWithPayload`)**

Replace `src/BlazorBaseUI/Dialog/DialogHandle.cs`. Same base pattern plus the Dialog-specific `OpenWithPayload` method:

```csharp
public class DialogHandle<TPayload> : ComponentHandleBase<TPayload, DialogOpenChangeReason>, IDialogHandle
{
    /// <inheritdoc />
    protected override DialogOpenChangeReason ImperativeActionReason => DialogOpenChangeReason.ImperativeAction;

    /// <inheritdoc />
    protected override string ComponentName => "Dialog";

    /// <summary>
    /// Opens the dialog with a payload without associating a trigger.
    /// </summary>
    /// <param name="payload">The payload to pass to the dialog.</param>
    public void OpenWithPayload(TPayload? payload)
    {
        if (IsOpen)
        {
            return;
        }

        // Use the base's SyncState to set the payload without triggering normal open flow
        // Then notify dialog-specific subscribers
        foreach (var subscriber in Subscribers.ToArray())
        {
            if (subscriber is IDialogHandleSubscriber dialogSubscriber)
            {
                dialogSubscriber.OnOpenWithPayloadRequested(payload, DialogOpenChangeReason.ImperativeAction);
            }
        }
    }

    // ... explicit interface implementations ...

    /// <inheritdoc />
    void IDialogHandle.OpenWithPayload(object? payload)
    {
        OpenWithPayload(payload is TPayload typed ? typed : default);
    }
}
```

Keep `IDialogHandle` interface unchanged (it has `OpenWithPayload(object?)`). Change `IDialogHandleSubscriber` to extend `IComponentHandleSubscriberBase<DialogOpenChangeReason>`:

```csharp
internal interface IDialogHandleSubscriber : IComponentHandleSubscriberBase<DialogOpenChangeReason>
{
    /// <summary>
    /// Called when the dialog should open with a payload and no trigger association.
    /// </summary>
    void OnOpenWithPayloadRequested(object? payload, DialogOpenChangeReason reason);
}
```

- [ ] **Step 6: Update Root components for subscriber interface changes**

Each Root component currently declares standalone subscriber interface methods. Since the subscriber interfaces now extend `IComponentHandleSubscriberBase<TReason>`, the Root components' `OnOpenChangeRequested` signatures must match the base interface (which includes `string? interactionType = null`).

For **TooltipRoot.razor**, **PreviewCardRoot.razor**, **MenuRoot.razor** — their `OnOpenChangeRequested` currently lacks `interactionType`. Add the parameter:

```csharp
// Before (e.g., TooltipRoot):
void ITooltipHandleSubscriber.OnOpenChangeRequested(bool open, TooltipOpenChangeReason reason, string? triggerId)

// After:
void ITooltipHandleSubscriber.OnOpenChangeRequested(bool open, TooltipOpenChangeReason reason, string? triggerId, string? interactionType = null)
```

**DialogRoot.razor** and **PopoverRoot.razor** already have `interactionType` — no signature change needed.

- [ ] **Step 7: Build the full solution**

Run: `dotnet build BlazorBaseUI.slnx`
Expected: 0 errors

- [ ] **Step 8: Run unit tests**

Run: `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj`
Expected: All tests pass

- [ ] **Step 9: Commit**

```bash
git add src/BlazorBaseUI/Dialog/DialogHandle.cs src/BlazorBaseUI/Popover/PopoverHandle.cs src/BlazorBaseUI/Menu/MenuHandle.cs src/BlazorBaseUI/Tooltip/TooltipHandle.cs src/BlazorBaseUI/PreviewCard/PreviewCardHandle.cs src/BlazorBaseUI/Dialog/DialogRoot.razor src/BlazorBaseUI/Popover/PopoverRoot.razor src/BlazorBaseUI/Menu/MenuRoot.razor src/BlazorBaseUI/Tooltip/TooltipRoot.razor src/BlazorBaseUI/PreviewCard/PreviewCardRoot.razor
git commit -m "Migrate 5 handle classes to ComponentHandleBase"
```

---

## Task 3: Create `OpenChangeEventArgs<TReason>`

**Files:**
- Create: `src/BlazorBaseUI/OpenChangeEventArgs.cs`

- [ ] **Step 1: Create the base class file**

```csharp
namespace BlazorBaseUI;

/// <summary>
/// Abstract base class for open/close state change event args.
/// Provides shared <see cref="Open"/>, <see cref="Reason"/>, <see cref="IsCanceled"/>,
/// and <see cref="PreventUnmount"/> properties.
/// </summary>
/// <typeparam name="TReason">The open change reason enum type.</typeparam>
public abstract class OpenChangeEventArgs<TReason> : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenChangeEventArgs{TReason}"/> class.
    /// </summary>
    /// <param name="open">The new open state.</param>
    /// <param name="reason">The reason for the state change.</param>
    protected OpenChangeEventArgs(bool open, TReason reason)
    {
        Open = open;
        Reason = reason;
    }

    /// <summary>
    /// Gets the new open state.
    /// </summary>
    public bool Open { get; }

    /// <summary>
    /// Gets the reason for the state change.
    /// </summary>
    public TReason Reason { get; }

    /// <summary>
    /// Gets a value indicating whether the open state change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether unmounting on close should be prevented.
    /// </summary>
    public virtual bool PreventUnmount { get; private set; }

    /// <summary>
    /// Cancels the open state change, preventing the component from opening or closing.
    /// </summary>
    public void Cancel() => IsCanceled = true;

    /// <summary>
    /// Prevents the component from unmounting when it closes.
    /// The popup remains in the DOM after the close transition completes.
    /// </summary>
    public virtual void PreventUnmountOnClose() => PreventUnmount = true;
}
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build src/BlazorBaseUI/BlazorBaseUI.csproj`
Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git add src/BlazorBaseUI/OpenChangeEventArgs.cs
git commit -m "Add OpenChangeEventArgs abstract base class for shared event args"
```

---

## Task 4: Migrate All EventArgs + Update Consumers

**Files:**
- Modify: All 7 EventArgs.cs files
- Modify: All Root components that reference `Canceled` (normalize to `IsCanceled`)
- Modify: All Root components that reference unmount prevention properties (normalize to `PreventUnmount`)

- [ ] **Step 1: Migrate DialogOpenChangeEventArgs**

Replace the class in `src/BlazorBaseUI/Dialog/EventArgs.cs`:

```csharp
namespace BlazorBaseUI.Dialog;

/// <summary>
/// Provides data for the dialog open change event.
/// </summary>
public sealed class DialogOpenChangeEventArgs : OpenChangeEventArgs<DialogOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DialogOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">The new open state.</param>
    /// <param name="reason">The reason for the state change.</param>
    public DialogOpenChangeEventArgs(bool open, DialogOpenChangeReason reason)
        : base(open, reason)
    {
    }
}
```

- [ ] **Step 2: Update DialogRoot.razor consumers**

In `src/BlazorBaseUI/Dialog/DialogRoot.razor`:
- Replace `args.Canceled` → `args.IsCanceled`
- Replace `args.PreventUnmountingOnClose` → `args.PreventUnmount`

Note: `context.PreventUnmountingOnClose` is a separate context property — do NOT rename that. Only rename the EventArgs property access.

- [ ] **Step 3: Migrate PopoverOpenChangeEventArgs**

Replace the class in `src/BlazorBaseUI/Popover/EventArgs.cs`:

```csharp
namespace BlazorBaseUI.Popover;

/// <summary>
/// Provides data for the popover open state change event.
/// </summary>
public sealed class PopoverOpenChangeEventArgs : OpenChangeEventArgs<PopoverOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PopoverOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">The requested open state of the popover.</param>
    /// <param name="reason">The reason for the open state change.</param>
    public PopoverOpenChangeEventArgs(bool open, PopoverOpenChangeReason reason)
        : base(open, reason)
    {
    }
}
```

- [ ] **Step 4: Update PopoverRoot.razor consumers**

In `src/BlazorBaseUI/Popover/PopoverRoot.razor`:
- Replace `args.Canceled` → `args.IsCanceled`
- Replace `args.UnmountPrevented` → `args.PreventUnmount`

- [ ] **Step 5: Migrate MenuOpenChangeEventArgs**

Replace the `MenuOpenChangeEventArgs` class in `src/BlazorBaseUI/Menu/EventArgs.cs`:

```csharp
/// <summary>
/// Provides data for the <see cref="MenuRoot.OnOpenChange"/> event.
/// </summary>
public sealed class MenuOpenChangeEventArgs : OpenChangeEventArgs<MenuOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MenuOpenChangeEventArgs"/> class.
    /// </summary>
    public MenuOpenChangeEventArgs(bool open, MenuOpenChangeReason reason, object? payload = null)
        : base(open, reason)
    {
        Payload = payload;
    }

    /// <summary>
    /// Gets the optional payload associated with the state change.
    /// </summary>
    public object? Payload { get; }

    /// <summary>
    /// Gets whether the event should propagate to parent menus.
    /// </summary>
    public bool IsPropagationAllowed { get; private set; }

    /// <summary>
    /// Allows the open state change event to propagate to parent menus in nested menu scenarios.
    /// </summary>
    public void AllowPropagation() => IsPropagationAllowed = true;
}
```

Keep `MenuRadioGroupChangeEventArgs` and `MenuCheckboxItemChangeEventArgs` unchanged (they are value-change events, not open-change events).

MenuRoot.razor already uses `IsCanceled` and `PreventUnmount` — no consumer update needed.

- [ ] **Step 6: Migrate TooltipOpenChangeEventArgs**

Replace the class in `src/BlazorBaseUI/Tooltip/EventArgs.cs`:

```csharp
namespace BlazorBaseUI.Tooltip;

/// <summary>
/// Provides data for the tooltip open state change event.
/// </summary>
public sealed class TooltipOpenChangeEventArgs : OpenChangeEventArgs<TooltipOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TooltipOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">The requested open state of the tooltip.</param>
    /// <param name="reason">The reason for the open state change.</param>
    public TooltipOpenChangeEventArgs(bool open, TooltipOpenChangeReason reason)
        : base(open, reason)
    {
    }
}
```

- [ ] **Step 7: Update TooltipRoot.razor consumers**

Replace `args.Canceled` → `args.IsCanceled` in `src/BlazorBaseUI/Tooltip/TooltipRoot.razor`.

- [ ] **Step 8: Migrate PreviewCardOpenChangeEventArgs**

Replace the class in `src/BlazorBaseUI/PreviewCard/EventArgs.cs`:

```csharp
namespace BlazorBaseUI.PreviewCard;

/// <summary>
/// Provides data for the preview card open state change event.
/// </summary>
public sealed class PreviewCardOpenChangeEventArgs : OpenChangeEventArgs<PreviewCardOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PreviewCardOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">The requested open state of the preview card.</param>
    /// <param name="reason">The reason for the open state change.</param>
    public PreviewCardOpenChangeEventArgs(bool open, PreviewCardOpenChangeReason reason)
        : base(open, reason)
    {
    }
}
```

- [ ] **Step 9: Update PreviewCardRoot.razor consumers**

Replace `args.Canceled` → `args.IsCanceled` in `src/BlazorBaseUI/PreviewCard/PreviewCardRoot.razor`.

- [ ] **Step 10: Migrate SelectOpenChangeEventArgs**

Replace the `SelectOpenChangeEventArgs` class in `src/BlazorBaseUI/Select/EventArgs.cs`:

```csharp
/// <summary>
/// Provides data for the <see cref="SelectRoot{TValue}.OnOpenChange"/> event.
/// </summary>
public sealed class SelectOpenChangeEventArgs : OpenChangeEventArgs<SelectOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectOpenChangeEventArgs"/> class.
    /// </summary>
    public SelectOpenChangeEventArgs(bool open, SelectOpenChangeReason reason)
        : base(open, reason)
    {
    }
}
```

Keep `SelectValueChangeEventArgs<TValue>` unchanged. SelectRoot.razor already uses `IsCanceled` — no consumer update needed.

- [ ] **Step 11: Migrate CollapsibleOpenChangeEventArgs**

Replace the `CollapsibleOpenChangeEventArgs` class in `src/BlazorBaseUI/Collapsible/EventArgs.cs`:

```csharp
/// <summary>
/// Provides data for the <see cref="CollapsibleRoot.OnOpenChange"/> event.
/// </summary>
public sealed class CollapsibleOpenChangeEventArgs : OpenChangeEventArgs<CollapsibleOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of <see cref="CollapsibleOpenChangeEventArgs"/>.
    /// </summary>
    /// <param name="open">The new open state of the collapsible.</param>
    /// <param name="reason">The reason the open state changed.</param>
    public CollapsibleOpenChangeEventArgs(bool open, CollapsibleOpenChangeReason reason = CollapsibleOpenChangeReason.None)
        : base(open, reason)
    {
    }

    /// <summary>
    /// Gets whether the event should propagate to parent components.
    /// </summary>
    public bool IsPropagationAllowed { get; private set; }

    /// <summary>
    /// Allows the open state change event to propagate to parent components.
    /// </summary>
    public void AllowPropagation() => IsPropagationAllowed = true;
}
```

Keep the `CollapsibleOpenChangeReason` enum in the same file (it's already defined there). CollapsibleRoot.razor already uses `IsCanceled` — no consumer update needed.

- [ ] **Step 12: Check AccordionRoot.razor**

`AccordionRoot.razor` references `Canceled` at line 216. Check whether this uses a `CollapsibleOpenChangeEventArgs` (which now has `IsCanceled`) or its own EventArgs. If it uses `CollapsibleOpenChangeEventArgs`, then the rename from `Canceled` to `IsCanceled` is already handled by the base class. If it accesses `.Canceled` directly, update to `.IsCanceled`.

- [ ] **Step 13: Build the full solution**

Run: `dotnet build BlazorBaseUI.slnx`
Expected: 0 errors. If there are compilation errors from other consumers referencing old property names (`Canceled`, `PreventUnmountingOnClose`, `UnmountPrevented`), fix them — they should all use `IsCanceled` and `PreventUnmount` now.

- [ ] **Step 14: Run unit tests**

Run: `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj`
Expected: All tests pass

- [ ] **Step 15: Commit**

```bash
git add -A
git commit -m "Migrate 7 EventArgs classes to OpenChangeEventArgs base, normalize property names"
```

---

## Task 5: Move `ParseSide`/`ParseAlign` to Global Extensions

**Files:**
- Modify: `src/BlazorBaseUI/Extensions.cs`
- Modify: `src/BlazorBaseUI/Popover/Extensions.cs`
- Modify: `src/BlazorBaseUI/Menu/Extensions.cs`

- [ ] **Step 1: Add methods to global Extensions.cs**

Add to the `Extensions` class in `src/BlazorBaseUI/Extensions.cs`:

```csharp
extension(Side _)
{
    public static Side ParseSide(string value) => value switch
    {
        "top" => Side.Top,
        "right" => Side.Right,
        "bottom" => Side.Bottom,
        "left" => Side.Left,
        _ => Side.Bottom
    };

    public static Align ParseAlign(string value) => value switch
    {
        "start" => Align.Start,
        "center" => Align.Center,
        "end" => Align.End,
        _ => Align.Center
    };
}
```

Wait — the existing Extensions.cs uses C# 14 `extension` blocks. But `ParseSide`/`ParseAlign` are currently **static methods** (not extension methods). They're called as `Extensions.ParseSide(value)` from Popover and Menu code. Check how they're called in consumer code to determine the right approach.

If called as `Extensions.ParseSide(value)` or `BlazorBaseUI.Popover.Extensions.ParseSide(value)`, then making them static methods on the global `Extensions` class works. However, the global Extensions class uses the C# 14 `extension` block syntax. The simplest approach: add them as regular static methods since they don't extend an instance.

Actually, look at the existing code — the global `Extensions` class is `internal static class Extensions` with `extension(...)` blocks. The Popover/Menu `ParseSide`/`ParseAlign` are regular static methods on their local `Extensions` classes. To move them, add them as regular static methods on the global class.

The calling code in Popover/Menu will need to change from `Extensions.ParseSide(value)` to either `BlazorBaseUI.Extensions.ParseSide(value)` or just `Extensions.ParseSide(value)` if the global namespace is in scope. Since Popover/Menu components are in `BlazorBaseUI.Popover`/`BlazorBaseUI.Menu` namespaces and the global Extensions is in `BlazorBaseUI`, there may be a name collision. We need to check.

**Actually**, since both local and global `Extensions` classes exist, consumers in `BlazorBaseUI.Popover` will resolve `Extensions` to the local one first. After removing `ParseSide`/`ParseAlign` from the local class, the compiler will look up to the parent namespace `BlazorBaseUI` and find them there. This should work transparently.

- [ ] **Step 1 (revised): Add static ParseSide/ParseAlign methods to global Extensions.cs**

Add these methods inside the `Extensions` class body in `src/BlazorBaseUI/Extensions.cs`:

```csharp
    /// <summary>
    /// Parses a side string from FloatingUI into the corresponding <see cref="Side"/> enum value.
    /// </summary>
    public static Side ParseSide(string value) => value switch
    {
        "top" => Side.Top,
        "right" => Side.Right,
        "bottom" => Side.Bottom,
        "left" => Side.Left,
        _ => Side.Bottom
    };

    /// <summary>
    /// Parses an align string from FloatingUI into the corresponding <see cref="Align"/> enum value.
    /// </summary>
    public static Align ParseAlign(string value) => value switch
    {
        "start" => Align.Start,
        "center" => Align.Center,
        "end" => Align.End,
        _ => Align.Center
    };
```

- [ ] **Step 2: Remove ParseSide/ParseAlign from Popover/Extensions.cs**

Remove the `ParseSide` and `ParseAlign` methods from `src/BlazorBaseUI/Popover/Extensions.cs`. Keep the `ToDataAttributeString` method for `PopoverInstantType`.

- [ ] **Step 3: Remove ParseSide/ParseAlign from Menu/Extensions.cs**

Remove the `ParseSide` and `ParseAlign` methods from `src/BlazorBaseUI/Menu/Extensions.cs`. Keep the `ToDataAttributeString` method for `MenuInstantType`.

- [ ] **Step 4: Update callers if needed**

Search all `.razor` and `.cs` files in `src/BlazorBaseUI/Popover/` and `src/BlazorBaseUI/Menu/` for calls to `Extensions.ParseSide` and `Extensions.ParseAlign`. Since the local `Extensions` class still exists (with `ToDataAttributeString`), and both local and global classes are named `Extensions`, the compiler may report ambiguity. If so, fully qualify: `BlazorBaseUI.Extensions.ParseSide(value)`.

Also check `src/BlazorBaseUI/Tooltip/`, `src/BlazorBaseUI/PreviewCard/`, `src/BlazorBaseUI/NavigationMenu/`, and `src/BlazorBaseUI/Select/` for any components that import Popover's or Menu's `ParseSide`/`ParseAlign` via a `using` directive.

- [ ] **Step 5: Build**

Run: `dotnet build BlazorBaseUI.slnx`
Expected: 0 errors

- [ ] **Step 6: Commit**

```bash
git add src/BlazorBaseUI/Extensions.cs src/BlazorBaseUI/Popover/Extensions.cs src/BlazorBaseUI/Menu/Extensions.cs
git commit -m "Move ParseSide/ParseAlign to global Extensions.cs, remove duplicates"
```

---

## Task 6: Wire Up AccessibilityUtilities

**Files:**
- Modify: Components that inline button role/tabindex/aria-disabled patterns

- [ ] **Step 1: Find consumers**

Search for components that inline the patterns `AccessibilityUtilities` consolidates:
- `attributes["role"] = "button"` + `attributes["tabindex"]` patterns
- `attributes["aria-disabled"] = true` with focusable-when-disabled logic

Run grep for these patterns across `src/BlazorBaseUI/**/*.razor` files. For each match, replace the inline logic with the appropriate `AccessibilityUtilities.ApplyButtonAttributes()` or `AccessibilityUtilities.ApplyFocusableWhenDisabled()` call.

- [ ] **Step 2: Apply changes to each component found**

For each component found in Step 1, replace the inline attribute manipulation with the utility call. Example transformation:

```csharp
// Before:
componentAttributes["role"] = "button";
componentAttributes["tabindex"] = 0;
if (Context.Disabled)
{
    componentAttributes["aria-disabled"] = true;
}

// After:
AccessibilityUtilities.ApplyButtonAttributes(componentAttributes, Context.Disabled);
```

- [ ] **Step 3: Build and test**

Run: `dotnet build BlazorBaseUI.slnx`
Run: `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj`
Expected: 0 errors, all tests pass

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Wire up AccessibilityUtilities to components with inline button/label patterns"
```

---

## Task 7: Add AGENTS.md JS Interop File Architecture Rule

**Files:**
- Modify: `AGENTS.md`

- [ ] **Step 1: Add the rule**

In `AGENTS.md`, after the existing rule `### 4. JavaScript Interop Rules`, add:

```markdown
### 4a. JS Interop File Architecture

- Every component requiring JS interop **must** have its own component-specific JS file (e.g., `blazor-baseui-dialog.js`)
- Component JS files import shared behavior from functional JS modules (e.g., `blazor-baseui-floating.js`, `blazor-baseui-composite.js`)
- Shared/functional JS modules must **never** contain component-specific logic
- **Exception:** When the interaction is trivial (a single function call with no component-specific wiring), the C# component may import the shared JS module directly
- When adding new behavior to a component, modify the component's JS file — not the shared module
```

- [ ] **Step 2: Commit**

```bash
git add AGENTS.md
git commit -m "Add JS interop file architecture rule to AGENTS.md"
```

---

## Task 8: Extend `blazor-baseui-floating.js`

**Files:**
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-floating.js`
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-dialog.js`
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-tooltip.js`
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-preview-card.js`
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-popover.js`

- [ ] **Step 1: Add transition helper exports to floating.js**

At the end of `blazor-baseui-floating.js`, add the following exported functions extracted from `tooltip.js` (lines 168–285). These are identical in tooltip and preview-card:

```javascript
/**
 * Sets up a transitionend/animationend listener on the popup element.
 * Calls dotNetRef.OnTransitionStatusChanged when transitions complete.
 */
export function setupTransitionEndListener(rootState, isOpen) { /* copy from tooltip.js lines 238-285 */ }

/**
 * Waits for the popup element to exist in the DOM, then starts the transition.
 * Uses polling with requestAnimationFrame if the element isn't available yet.
 */
export function waitForPopupAndStartTransition(rootState, isOpen) { /* copy from tooltip.js lines 168-199 */ }

/**
 * Starts the open/close transition on the popup element.
 * Removes/adds data-starting-style and data-ending-style attributes
 * to trigger CSS transitions.
 */
export function startTransition(rootState, isOpen) { /* copy from tooltip.js lines 201-236 */ }
```

Copy the exact function bodies from `blazor-baseui-tooltip.js` lines 168–285.

- [ ] **Step 2: Add hover interaction wrapper exports to floating.js**

Add exported wrapper functions that encapsulate the hover interaction lifecycle. These are extracted from `tooltip.js` lines 50–115:

```javascript
/**
 * Initializes a hover interaction for a floating element.
 * @param {Map} stateRoots - The state.roots map for the component
 * @param {string} rootId - The root ID
 * @param {Element} triggerElement - The trigger element
 * @param {object} options - { openDelay, closeDelay, disableHoverablePopup, componentName }
 */
export function initializeHoverInteractionForRoot(stateRoots, rootId, triggerElement, options) { /* extracted from tooltip.js */ }

export function disposeHoverInteractionForRoot(stateRoots, rootId) { /* extracted from tooltip.js */ }

export function updateHoverInteractionFloatingElementForRoot(stateRoots, rootId) { /* extracted from tooltip.js */ }

export function setHoverInteractionOpenForRoot(stateRoots, rootId, isOpen) { /* extracted from tooltip.js */ }
```

Copy the exact function bodies from `blazor-baseui-tooltip.js` lines 50–115, parameterizing the state map and component name.

- [ ] **Step 3: Add shared Escape key handler to floating.js**

```javascript
/**
 * Creates a global Escape key handler that closes the first open root.
 * @param {Map} roots - The state.roots map
 * @param {string} methodName - The .NET method to invoke (e.g., 'OnEscapeKey')
 * @returns {function} The keydown handler function (for addEventListener/removeEventListener)
 */
export function createEscapeKeyHandler(roots, methodName) {
    return function handleGlobalKeyDown(e) {
        if (e.key !== 'Escape') return;
        for (const [id, rootState] of roots) {
            if (rootState.isOpen && rootState.dotNetRef) {
                rootState.dotNetRef.invokeMethodAsync(methodName).catch(() => { });
                break;
            }
        }
    };
}
```

- [ ] **Step 4: Update dialog.js — delete private transition utils, import from floating.js**

In `blazor-baseui-dialog.js`:
- Delete private `checkForTransitionOrAnimation` function (lines 379–390)
- Delete private `parseCssDuration` function (lines 392–411)
- Delete private `getMaxTransitionDuration` function (lines 413–430)
- Add import at top: `import { checkForTransitionOrAnimation, parseCssDuration, getMaxTransitionDuration } from './blazor-baseui-floating.js';`
- Replace inline `requestAnimationFrame(() => { requestAnimationFrame(() => {` (line 175–176) with:
  ```javascript
  import { requestDoubleAnimationFrame } from './blazor-baseui-animations.js';
  ```
  Then use `await requestDoubleAnimationFrame()` in place of the nested RAFs.

- [ ] **Step 5: Update tooltip.js — import shared helpers from floating.js**

In `blazor-baseui-tooltip.js`:
- Delete local `setupTransitionEndListener` (lines 238–285), `waitForPopupAndStartTransition` (lines 168–199), `startTransition` (lines 201–236)
- Delete local `handleGlobalKeyDown` (lines 35–44)
- Delete local `initializeHoverInteraction`, `disposeHoverInteraction`, `updateHoverInteractionFloatingElement`, `setHoverInteractionOpen` (lines 50–115)
- Add imports from `blazor-baseui-floating.js`:
  ```javascript
  import { setupTransitionEndListener, waitForPopupAndStartTransition, startTransition, initializeHoverInteractionForRoot, disposeHoverInteractionForRoot, updateHoverInteractionFloatingElementForRoot, setHoverInteractionOpenForRoot, createEscapeKeyHandler } from './blazor-baseui-floating.js';
  ```
- Replace inline double-RAF with `import { requestDoubleAnimationFrame } from './blazor-baseui-animations.js';`
- Update callers to use the imported functions, passing the required state parameters.

- [ ] **Step 6: Update preview-card.js — same pattern as tooltip.js**

Apply the identical changes as Step 5 to `blazor-baseui-preview-card.js` (which has byte-for-byte identical functions).

- [ ] **Step 7: Update popover.js — import hover interaction wrappers**

In `blazor-baseui-popover.js`:
- Delete local hover interaction functions (the 4 functions near-identical to tooltip's)
- Import from `blazor-baseui-floating.js`:
  ```javascript
  import { initializeHoverInteractionForRoot, disposeHoverInteractionForRoot, updateHoverInteractionFloatingElementForRoot, setHoverInteractionOpenForRoot } from './blazor-baseui-floating.js';
  ```
- Update callers. Note: popover's version has an optional `callbackDotNetRef` parameter — ensure this is accommodated in the shared version or kept as a thin wrapper.

- [ ] **Step 8: Build the demo to verify JS loads correctly**

Run: `dotnet run --project demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.csproj`
Verify no JS console errors on load.

- [ ] **Step 9: Commit**

```bash
git add src/BlazorBaseUI/wwwroot/blazor-baseui-floating.js src/BlazorBaseUI/wwwroot/blazor-baseui-dialog.js src/BlazorBaseUI/wwwroot/blazor-baseui-tooltip.js src/BlazorBaseUI/wwwroot/blazor-baseui-preview-card.js src/BlazorBaseUI/wwwroot/blazor-baseui-popover.js
git commit -m "Consolidate transition helpers and hover interaction into floating.js"
```

---

## Task 9: Wire Up `blazor-baseui-composite.js`

**Files:**
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-tabs.js`
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-toggle.js`
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-radio.js`
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-toolbar.js`
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-menubar.js`
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-accordion-trigger.js`

For each component, the migration pattern is:

1. Add import: `import { initialize as compositeInit, dispose as compositeDispose, updateOptions as compositeUpdate } from './blazor-baseui-composite.js';`
2. In the component's `initialize` function, call `compositeInit(element, { orientation, loop, enableHomeAndEndKeys, direction, itemSelector, highlightOnHover })` with the appropriate options
3. In the component's `dispose` function, call `compositeDispose(element)`
4. Delete the inline functions: `navigateToNext`, `navigateToPrevious`, `navigateToFirst`, `navigateToLast`, `updateTabIndexes`/`updateToggleTabIndexes`, `getOrdered*`, `handleKeyDown` (keyboard navigation portion), `registerItem`/`unregisterItem`, `getFocusableItems`, `isTabDisabled`/`isToggleDisabled`/`isRadioDisabled`
5. Keep component-specific keyboard handling that is NOT navigation (e.g., Enter/Space firing value changes, submenu open logic)

- [ ] **Step 1: Migrate tabs.js**

The tabs component uses `orientation`, `loop`, and `[data-blazor-base-ui-tab]` as item selector. Add composite import, call `compositeInit` in the tab list initialize function, delete `navigateToNext/Previous/First/Last`, `updateTabIndexes`, `getOrderedTabs`, `isTabDisabled`. Keep tab-specific activation logic (Enter/Space selecting a tab).

- [ ] **Step 2: Migrate toggle.js**

Same pattern. Item selector: `[data-blazor-base-ui-toggle-item]`. Delete `navigateToNext/Previous/First/Last`, `updateToggleTabIndexes`, `getOrderedToggles`, `isToggleDisabled`.

- [ ] **Step 3: Migrate radio.js**

Item selector: `[data-blazor-base-ui-radio-item]`. Delete `navigateToNext/Previous`, `updateTabIndexes`, `getOrderedRadios`, `isRadioDisabled`. Note: radio selects on navigation (arrow key = select), so keep the value-change logic triggered by navigation.

- [ ] **Step 4: Migrate toolbar.js**

Item selector: `[data-blazor-base-ui-toolbar-item]`. Delete `handleKeyDown`, `registerItem/unregisterItem`, `getFocusableItems`, the `compareDocumentPosition` sort. The toolbar version omits `scrollIntoView` — set `scroll: false` in composite options if supported, or leave default.

- [ ] **Step 5: Migrate menubar.js**

Item selector: `[data-blazor-base-ui-menubar-item]`. Same deletion pattern as toolbar. Keep submenu open/close logic triggered by keyboard.

- [ ] **Step 6: Migrate accordion-trigger.js**

Item selector: `[data-blazor-base-ui-accordion-trigger]`. Delete `handleKeyDown`, the arrow key navigation, Home/End. Keep accordion-specific behavior (Enter/Space toggling panels).

- [ ] **Step 7: Build demo and test**

Run: `dotnet run --project demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.csproj`
Verify keyboard navigation works for each migrated component.

- [ ] **Step 8: Run Playwright tests if available**

Run: `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~Tabs OR FullyQualifiedName~Toggle OR FullyQualifiedName~Radio OR FullyQualifiedName~Toolbar OR FullyQualifiedName~Menubar OR FullyQualifiedName~Accordion"`

- [ ] **Step 9: Commit**

```bash
git add src/BlazorBaseUI/wwwroot/blazor-baseui-tabs.js src/BlazorBaseUI/wwwroot/blazor-baseui-toggle.js src/BlazorBaseUI/wwwroot/blazor-baseui-radio.js src/BlazorBaseUI/wwwroot/blazor-baseui-toolbar.js src/BlazorBaseUI/wwwroot/blazor-baseui-menubar.js src/BlazorBaseUI/wwwroot/blazor-baseui-accordion-trigger.js
git commit -m "Wire up blazor-baseui-composite.js to 6 component JS files"
```

---

## Task 10: Create `blazor-baseui-activation.js` + Wire Up

**Files:**
- Create: `src/BlazorBaseUI/wwwroot/blazor-baseui-activation.js`
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-button.js`
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-toggle.js`
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-switch.js`
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-checkbox.js`

- [ ] **Step 1: Create the activation module**

```javascript
/**
 * Shared module for Space/Enter key activation on non-button elements.
 * Makes elements behave like native buttons for keyboard interaction.
 *
 * Behavior:
 * - Space keydown: preventDefault (prevents scroll)
 * - Space keyup: fires click event
 * - Enter keydown: fires click event
 */

const STATE_KEY = Symbol.for('BlazorBaseUI.Activation.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = new Map();
}
const state = window[STATE_KEY];

/**
 * Initializes Space/Enter activation on an element.
 * @param {Element} element - The element to make activatable
 * @param {object} [options] - Configuration options
 * @param {boolean} [options.nativeButtonGuard=true] - Skip setup if element is a <button>
 */
export function initialize(element, options = {}) {
    const { nativeButtonGuard = true } = options;

    if (nativeButtonGuard && element.tagName === 'BUTTON') {
        return;
    }

    const handlers = {
        keydown(e) {
            if (e.key === ' ') {
                e.preventDefault();
            } else if (e.key === 'Enter') {
                e.preventDefault();
                element.click();
            }
        },
        keyup(e) {
            if (e.key === ' ') {
                e.preventDefault();
                element.click();
            }
        }
    };

    element.addEventListener('keydown', handlers.keydown);
    element.addEventListener('keyup', handlers.keyup);

    state.set(element, handlers);
}

/**
 * Removes activation handlers from an element.
 * @param {Element} element - The element to clean up
 */
export function dispose(element) {
    const handlers = state.get(element);
    if (handlers) {
        element.removeEventListener('keydown', handlers.keydown);
        element.removeEventListener('keyup', handlers.keyup);
        state.delete(element);
    }
}
```

- [ ] **Step 2: Migrate button.js**

In `blazor-baseui-button.js`:
- Add import: `import { initialize as activationInit, dispose as activationDispose } from './blazor-baseui-activation.js';`
- In `initialize`, call `activationInit(element, { nativeButtonGuard: true })` instead of inline keydown/keyup handlers
- In `dispose`, call `activationDispose(element)`
- Delete inline `keydownHandler` and `keyupHandler` functions

- [ ] **Step 3: Migrate toggle.js, switch.js, checkbox.js**

Apply the same pattern to each:
- Import `initialize`/`dispose` from activation.js
- Replace inline Space/Enter key handling with the import
- Note for **checkbox.js**: the checkbox has an "optimistic state" pattern on Space. If the optimistic state logic is tightly coupled with the keydown handler, keep it as a component-specific wrapper that calls the activation module, or keep checkbox's handler inline if it diverges significantly.

- [ ] **Step 4: Build demo and test**

Run: `dotnet run --project demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.csproj`
Verify Space/Enter activation works for buttons, toggles, switches, checkboxes.

- [ ] **Step 5: Commit**

```bash
git add src/BlazorBaseUI/wwwroot/blazor-baseui-activation.js src/BlazorBaseUI/wwwroot/blazor-baseui-button.js src/BlazorBaseUI/wwwroot/blazor-baseui-toggle.js src/BlazorBaseUI/wwwroot/blazor-baseui-switch.js src/BlazorBaseUI/wwwroot/blazor-baseui-checkbox.js
git commit -m "Add blazor-baseui-activation.js and wire up 4 component JS files"
```

---

## Task 11: Wire Up `blazor-baseui-press-and-hold.js`

**Files:**
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-number-field.js`

- [ ] **Step 1: Migrate number-field.js**

In `blazor-baseui-number-field.js`:
- Add import: `import { initialize as pressAndHoldInit, dispose as pressAndHoldDispose } from './blazor-baseui-press-and-hold.js';`
- Replace inline `startAutoChange`/`stopAutoChange` logic (~45 lines) and the constants (`START_AUTO_CHANGE_DELAY = 400`, `CHANGE_VALUE_TICK_DELAY = 60`, `SCROLLING_POINTER_MOVE_DISTANCE = 8`) with calls to the shared module
- In the step button initialization, call `pressAndHoldInit(stepButton, dotNetRef, { startDelay: 400, tickDelay: 60, scrollDistance: 8, callbackMethod: 'OnTick', stopMethod: 'OnStop' })`
- In dispose, call `pressAndHoldDispose(stepButton)`

- [ ] **Step 2: Build demo and test number field**

Run: `dotnet run --project demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.csproj`
Navigate to number field demo, verify press-and-hold increment/decrement works.

- [ ] **Step 3: Commit**

```bash
git add src/BlazorBaseUI/wwwroot/blazor-baseui-number-field.js
git commit -m "Wire up blazor-baseui-press-and-hold.js to number-field"
```

---

## Task 12: Wire Up `blazor-baseui-popup-viewport.js`

**Files:**
- Modify: `src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js`

- [ ] **Step 1: Migrate navigation-menu.js**

In `blazor-baseui-navigation-menu.js`:
- Add import: `import { initialize as viewportInit, dispose as viewportDispose, onTriggerChange, contentChanged } from './blazor-baseui-popup-viewport.js';`
- Replace inline ResizeObserver setup and viewport morph logic with calls to the shared module
- In viewport initialization, call `viewportInit(viewportElement, { dotNetRef, popupElement, positionerElement, side, direction, cssVars })`
- In trigger change handling, call `onTriggerChange(viewportElement, previousTriggerElement, newTriggerElement)`
- In dispose, call `viewportDispose(viewportElement)`
- Delete inline `setupAutoResize`, morph/clone logic, directional animation code

- [ ] **Step 2: Build demo and test navigation menu**

Run: `dotnet run --project demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.csproj`
Navigate to navigation menu demo, verify viewport transitions work.

- [ ] **Step 3: Commit**

```bash
git add src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js
git commit -m "Wire up blazor-baseui-popup-viewport.js to navigation-menu"
```

---

## Final Verification

- [ ] **Full solution build**

Run: `dotnet build BlazorBaseUI.slnx`
Expected: 0 errors

- [ ] **Full unit test suite**

Run: `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj`
Expected: All tests pass

- [ ] **Playwright tests (if CI available)**

Run: `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj`
Expected: All tests pass
