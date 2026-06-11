# PreviewCard Parity Matrix

## Root / Store

| React source | Blazor equivalent | Status |
|---|---|---|
| `PreviewCardStore.useStore` with `defaultOpen`, `openProp`, `activeTriggerId`, `triggerIdProp` | `PreviewCardRoot` `DefaultOpen`, `Open`, `DefaultTriggerId`, `TriggerId`, controlled lifecycle sync | Verified |
| `useOnFirstRender` default-open support | `InitializeJsAsync` syncs default-open root state to JS | Verified |
| `useControlledProp('openProp')` | `CurrentOpen`, `ApplyOpenParameterChange`, `CompleteOpenParameterChange` | Verified |
| `useControlledProp('triggerIdProp')` | `TriggerId` updates via `SetActiveTrigger` | Verified |
| `onOpenChange`, cancel, `preventUnmountOnClose` | `PreviewCardOpenChangeEventArgs`, `IsCanceled`, `PreventUnmount`, transition close preservation | Verified |
| `onOpenChangeComplete` | `OnOpenChangeComplete` invoked from transition completion | Verified |
| `actionsRef.unmount`, `actionsRef.close` | `PreviewCardRootActions.Unmount`, `Close`, existing `Open/OpenWithTriggerId` extension | Verified |
| `handle` external store | `PreviewCardHandle<TPayload>`, root subscription, root ID sync for detached triggers | Verified |
| render-function children with payload | `ChildContentWithPayload(PreviewCardRootPayloadContext)` | Verified |
| `useImplicitActiveTrigger` | `TrySetImplicitActiveTrigger` | Verified |
| `useOpenStateTransitions` | `TransitionLifecycleManager` + JS starting/end callbacks | Verified |
| `useDismiss` | JS `createDismissInteraction` outside press + global Escape callback | Verified |
| `usePopupInteractionProps` / `FOCUSABLE_POPUP_PROPS` | JS-driven trigger/popup props + popup `tabindex=-1`, `data-floating-ui-focusable=""` | Verified |
| `instantType`: focus open, dismiss close for trigger press/escape only | `PreviewCardInstantType` mapping in `SetOpenAsync` | Verified |
| hover inline rect coords | JS stores active trigger and passes trigger ID; source inline-rect cursor details remain DOM-owned in floating module | Accounted |

## Trigger

| React source | Blazor equivalent | Status |
|---|---|---|
| Renders `<a>` | `RenderElement` default tag `a` | Verified |
| `id`, generated via Base UI id hook | `Id` or generated `Guid.ToIdString()` | Verified |
| `payload` forwarding | root/handle trigger payload maps | Verified |
| `handle` detached trigger support | `PreviewCardHandle<TPayload>` root-id event and trigger JS hover binding | Verified |
| default `delay=600`, `closeDelay=300` | `Delay = 600`, `CloseDelay = 300` | Verified |
| `useHoverReferenceInteraction` mouse-only, `move:false`, `safePolygon` | component-specific JS `initializeHoverInteraction` with shared `createHoverInteraction`, mouse-only and safe polygon | Verified |
| `useFocus` with delay | Blazor focus fallback uses `Delay`; Playwright verifies focus behavior | Verified |
| `data-popup-open` only on active open trigger | C# render and JS `syncTriggerOpenAttributes` use empty marker only when active | Verified |

## Portal

| React source | Blazor equivalent | Status |
|---|---|---|
| renders children when `mounted || keepMounted` | `PreviewCardPortal` existing mounted/keep-mounted behavior | Verified |

## Positioner

| React source | Blazor equivalent | Status |
|---|---|---|
| default side bottom, align center | `Side.Bottom`, `Align.Center` | Verified |
| default position method absolute | `PositionMethod.Absolute` | Verified |
| collision boundary clipping ancestors, padding 5, arrow padding 5 | matching parameters/defaults | Verified |
| sticky false, disable anchor tracking false | matching parameters/defaults and JS args | Verified |
| collision avoidance default | `CollisionAvoidance ?? new CollisionAvoidance()` | Verified |
| `adaptiveOrigin` when viewport exists | JS receives `hasViewport` and shared floating positioning uses viewport path | Verified |
| computed side/align/anchorHidden/arrowUncentered | JS `OnPositionUpdated` callback updates context/state | Verified |
| hidden when not mounted | `hidden` attribute | Verified |
| inert closed positioner pointer-events | resolved style adds `pointer-events: none` when closed | Verified |
| data attrs: `data-open`, `data-closed`, `data-anchor-hidden`, `data-side`, `data-align` | present; no `data-instant` | Verified |

## Popup

| React source | Blazor equivalent | Status |
|---|---|---|
| renders `<div>` | `RenderElement` default tag `div` | Verified |
| popup state includes open/side/align/instant/transitionStatus | `PreviewCardPopupState` | Verified |
| focusable popup props | `tabindex="-1"`, `data-floating-ui-focusable=""` | Verified |
| hover floating close delay from store | JS hover interaction owns popup floating element and close delay | Verified |
| transition attributes | `TransitionAttributeHelper.ApplyTransitionStyleAttributes` | Verified |
| data attrs: open/closed/starting-style/ending-style/side/align | present; no `data-instant` | Verified |

## Viewport

| React source | Blazor equivalent | Status |
|---|---|---|
| `usePopupViewport` | `PreviewCardViewport` with JS popup-viewport wrappers | Verified |
| child wrapper `data-current` | `WrappedChildContent` direct child | Verified |
| previous clone `data-previous`, `inert`, absolute, CSS size vars | shared `blazor-baseui-popup-viewport` module | Verified |
| `data-activation-direction` | JS trigger-change callback + `OnViewportTransitionStart` | Verified |
| `data-transitioning` | `PreviewCardViewportState.Transitioning` | Verified |
| `data-instant` for dismiss/focus | `PreviewCardViewportState.Instant` mapping | Verified |

## Backdrop / Arrow

| React source | Blazor equivalent | Status |
|---|---|---|
| Backdrop role presentation | `role="presentation"` | Verified |
| Backdrop forced noninteractive styles | pointer events and user-select forced styles | Verified |
| Backdrop transition attrs | `TransitionAttributeHelper.ApplyTransitionAttributes` | Verified |
| Arrow aria-hidden true | `aria-hidden="true"` | Verified |
| Arrow data attrs open/closed/side/align/uncentered | present | Verified |
| Arrow no `data-instant` | removed and tested | Verified |
