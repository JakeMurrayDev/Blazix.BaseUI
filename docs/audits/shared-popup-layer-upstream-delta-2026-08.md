# Shared popup/positioning/focus layer — upstream delta (cycle 1)

> Sweep ticket [#158](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/158), part of
> upstream sync cycle 1. Classification follows the ratified rubric in
> [METHODOLOGY.md](METHODOLOGY.md): one disposition row per *(commit, component)* pair,
> (b)/(c) port rows name their covering test or record a one-line infeasibility reason,
> (a)/(d) verdicts restate the user-observable symptom rather than naming a React mechanism.

- **Upstream pin:** `1a2ca3c9f8a39bd8c0dda939a7a23b72da226124` (origin/master, 2026-08-03)
- **Batch 1:** verified against local HEAD `90c20c55`, PR [#207](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/207), 2026-08-20
- **Batch 2:** verified against local HEAD `4b2a7923`, PR [#229](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/229), 2026-08-21

The ~19-commit shared cluster is owned by this one ticket rather than being re-dispositioned
across the twelve popup-family component sweeps, so those sweeps diff against an already-fixed
shared base.

---

## Batch 1 — ported 2026-08-20 (PR #207)

| Upstream | PR | Verdict | User-observable symptom | Evidence |
| --- | --- | --- | --- | --- |
| `3dceedea8` | #4665 | **(c)** | Two failures: closing a Base UI popup while a non-Base-UI overlay was still open released the page scroll lock that overlay needed (or overwrote a site author's own permanent lock); and on pages where `<body>` rather than `<html>` is the scroll container the lock silently did nothing, so the page kept scrolling behind the popup. | `blazix-baseui-scroll-lock.js` — `getViewportScroller` at every site, `isPageScrollLocked` on the true scroller, and a `MutationObserver` takeover on `<html>`+`<body>` replacing the old unconditional bail. Overflow saved and restored as longhands. |
| `84ac4b797` | #4485 | **(c)** | Pinch-zooming on a touch device dragged an open context menu around the screen instead of leaving it anchored. | `shiftLayoutViewport` threaded `MenuPositioner.razor` → `blazix-baseui-menu.js` → shared positioner, supplying a `rootBoundary` from `documentElement.clientWidth/Height`. |
| `bd2f34ddb` | #5299 | **(c)** | A reopened popup could flash at coordinates left over from its previous open before the first real position landed; at full size those coordinates can overflow the layout viewport, making mobile Chrome zoom the page out and reflow the anchor. | `parkPositionerAtViewportOrigin` on `resetPositioner`/`disposePositioner` in `blazix-baseui-floating.js`. Supersedes an earlier `d:moot` reading — the `data-positioned` CSS hide prevents the flash but not the stale-coordinate reflow. |
| `692bc8748` | #5370 | **(c)** + **(b)** | A popup anchored to the physical left grew from the wrong edge during auto-resize, so its content slid sideways while opening instead of expanding in place. | `applyAnchoringStyles` in `menu.js`, `popup-viewport.js`, `popover.js` mirrors upstream `getPopupAnchoringStyles`; `NavigationMenuPopup.razor` carries the same logic in C#; `TooltipViewport.razor` passes explicit `"ltr"`. |
| `1a2ca3c9f` | #5401 | **(c)** | Interrupting a closing popup by reopening it mid-exit released the popup's locked size immediately, so the replacement animation started from a collapsed box and visibly jumped. | `releasePopupSizeWhenAnimationsFinish` in `popup-viewport.js` and `popover.js` re-checks `getAnimations()` on the rejected `finished` promise and awaits replacements. `blazix-baseui-animations.js:97-110` already did this and is unchanged. |

---

## Batch 2 — ported 2026-08-21

### `595c0fa08` (#5340) — guard registered ID cleanup

Blazor initializes a replacement component **before** disposing the outgoing one, the reverse of
React's cleanup ordering. Upstream's guard compares id values
(`currentId === id ? undefined : currentId`); that is not sufficient here, because several ports
derive the registered id from the root id, so every instance resolves the *same* value and an
id-equality guard can never reject a stale clear. Ownership is tracked by instance instead, the
pattern PR [#206](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/206) established for
`FieldsetLegend`, factored into `RegisteredIdOwner`.

| Component | Verdict | User-observable symptom | Evidence |
| --- | --- | --- | --- |
| Field | **(b)** | Replacing a field's label left the control without an accessible name — `aria-labelledby` pointed at an id no element carried. | `LabelableContext.SetLabelId` takes the registering instance; `LabelableProvider.razor`, `FieldRoot.razor`, `FieldItem.razor` own a `RegisteredIdOwner`. Test `FieldLabelTests.KeepsControlAriaLabelledByWhenLabelIsReplaced`. |
| Slider | **(b)** | Same, on the slider root's `aria-labelledby`. | `SliderRootContext.SetLabelId(object, string?)`, `SliderRoot.SetLocalLabelId`. Test `SliderLabelTests.KeepsRootAriaLabelledByWhenLabelIsReplaced`. |
| Combobox — root label | **(b)** | Replacing `Combobox.Label` left the input and trigger without an accessible name. | `IComboboxRootContext.LabelIdOwner`, `ComboboxLabel.razor`. Test `ComboboxRootTests.Label_ShouldKeepTriggerAssociationWhenLabelIsReplaced`. |
| Combobox — group label | **(b)** | Same, on the group's `aria-labelledby`, when the replacement reuses the same explicit id. | `ComboboxGroupContext.SetLabelId(object, string?)`. Test `ComboboxRootTests.GroupLabel_ShouldKeepGroupAssociationWhenSupersededLabelUnmounts`. |
| Select — root label | **(b)** | Replacing `Select.Label` left the trigger without an accessible name. | `ISelectRootContext.LabelIdOwner`, `SelectLabel.razor`. Test `SelectLabelTests.KeepsTriggerAriaLabelledByWhenLabelIsReplaced`. |
| Select — group label | **(b)** | Same, on the group's `aria-labelledby`. | `SelectGroupContext.SetLabelId(object, string?)`. Test `SelectGroupLabelTests.KeepsGroupAriaLabelledByWhenSupersededLabelUnmounts`. |
| Menu — group label | **(b)** | Same, on the group's `aria-labelledby`. | `MenuGroupContext.SetLabelId(object, string?)`, applied by `MenuGroup` and `MenuRadioGroup`. Test `MenuGroupLabelTests.KeepsGroupAriaLabelledByWhenSupersededLabelUnmounts`. |
| Autocomplete — group label | **(b)** | Same, on the group's `aria-labelledby`. | `AutocompleteGroupContext.SetLabelId(object, string?)`. Test `AutocompleteRootTests.GroupLabel_ShouldKeepGroupAssociationWhenSupersededLabelUnmounts`. |
| Progress | **(b)** | Replacing `Progress.Label` left the progress bar without an accessible name. Not on the residue list recorded on #158; found by sweeping every `SetLabelId` sink. | `ProgressRootContext.SetLabelIdAction` takes the instance. Test `ProgressLabelTests.KeepsAriaLabelledByWhenLabelIsReplaced`. |
| Accordion — trigger id | **(b), hardened** | The trigger re-registers on every parameter set, so an applied clear is restored by the render it schedules; the panel's `aria-labelledby` is absent only for that intermediate render. | `AccordionItemContext.SetTriggerId(object, string?)`. Test `AccordionTriggerTests.KeepsPanelAriaLabelledByWhenTriggerIsReplaced` drives a stale clear from a non-owner and asserts the item does not re-render — the only externally observable difference, since the DOM value is restored within the same flush. |
| Toast — title and description | **(b), hardened** | Same shape as Accordion's trigger id: `ToastTitle`/`ToastDescription` re-register on every parameter set, so the drop of `aria-labelledby`/`aria-describedby` lasts one render. | `ToastRootContext.SetTitleId`/`SetDescriptionId` take the instance. Test `ToastTests.TitleAndDescriptionRegistrationsSurviveAReplacement` drives a stale clear from a non-owner and asserts the toast does not re-render. |
| Accordion / Collapsible — panel id | **(b)**, divergent in the opposite direction | Neither panel cleared its registration at all, so removing the panel while the item was open left the trigger's `aria-controls` pointing at an element no longer in the document. Upstream introduced a `null` sentinel for exactly this. | `AccordionPanel`/`CollapsiblePanel` clear on dispose through the ownership guard; both triggers drop `aria-controls` when the id is cleared. Tests `AccordionPanelTests.DropsTriggerAriaControlsWhenPanelIsRemoved`, `CollapsiblePanelTests.DropsTriggerAriaControlsWhenPanelIsRemoved`. |
| Meter | — | Dispositioned and ported separately on PR [#224](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/224). | `docs/audits/meter-upstream-delta-2026-08.md`. |

**Correction to the 2026-08-20 disposition comment.** `ComboboxGroupLabel` was recorded there as
already guarded and excluded from the residue. It carried an id-equality guard, but that guard
never rejects a stale clear when the replacement reuses the same explicit id, which
`ComboboxRootTests.GroupLabel_ShouldKeepGroupAssociationWhenSupersededLabelUnmounts` reproduces
against the pre-fix code. The site is ported above.

### `dc9a4577f` (#5384) — virtual/synthesized pointer classification

| Component | Verdict | User-observable symptom | Evidence |
| --- | --- | --- | --- |
| Menu | **(b)** + **(c)** | Activating a submenu trigger with Android TalkBack did not open the submenu. The trigger's default `OpenOnHover` leaves opening to hover, and a screen-reader press produces no pointer movement for hover to wait on. | `MenuSubmenuTrigger.HandlePointerDownAsync` opens when the press matches the synthesized-pointer shape, mirroring `useClick`'s `'virtual'` reclassification; `blazix-baseui-menu.js` exports `isAndroidPlatform` for the Android half of the shape check. Tests `MenuSubmenuTriggerTests.OpensOnAVirtualPressWhenOpenOnHover` and `.DoesNotOpenOnAnOrdinaryMousePressWhenOpenOnHover`. |

Only `MenuSubmenuTrigger` passes `ignoreMouse: openOnHover` upstream, so it is the only consumer
whose behavior the reclassification changes.

### `9a5c3850f` (#5265) — zero-delta pointer-move guard

| Component | Verdict | User-observable symptom | Evidence |
| --- | --- | --- | --- |
| Select | **(b)** + **(c)** | In Safari, scrolling the list under a stationary pointer moved the highlight to whichever item slid under the cursor, fighting keyboard navigation. | `SelectItem.HandleMouseEnterAsync`/`HandleMouseMoveAsync` ignore zero-delta moves via `PointerEventUtilities.IsStationaryWebKitPointer`; `SelectRoot` reads `isWebKitEngine` once at first render. Tests `SelectItemTests.IgnoresAStationaryPointerMoveOnWebKit` and `.HighlightsAStationaryPointerMoveOnOtherEngines`. |
| Combobox | **(b)** + **(c)** | Same, on the combobox list. | `ComboboxItem.HandlePointerEnterAsync`/`HandlePointerMoveAsync`; `blazix-baseui-combobox.js` also guards the `pointermove` listener that clears the keyboard modality. Test `ComboboxRootTests.Item_ShouldIgnoreAStationaryPointerEnterOnWebKit`. |
| Autocomplete | **(b)** + **(c)** | Same, on the autocomplete list. | `AutocompleteItem` and `blazix-baseui-autocomplete.js`, identical to Combobox. Covered by the Combobox test; the two components share the shape. |
| Menu | **(b)** | Same, on a scrolling menu popup. | `HandleMouseEnterAsync`/`HandleMouseMoveAsync` on `MenuItem`, `MenuCheckboxItem`, `MenuRadioItem`, `MenuLinkItem`, and `MenuSubmenuTrigger`. Test `MenuItemTests.IgnoresAStationaryPointerMoveOnWebKit`. |
| Composite (Menubar, Toolbar, Radio group, …) | **(c)** | Same, on any composite list that highlights on hover. | `handlePointerMove` in `blazix-baseui-composite.js`. No covering test: bUnit does not run the module and the composite hover path has no Playwright fixture; the guard is the same three-term expression the C# sites cover. |

The engine check follows upstream's `platform.engine.webkit` (`CSS.supports('-webkit-backdrop-filter:none')`).
Touch interactions keep highlighting, since a stationary cursor only exists on a pointing device.

### `8b2282a5e` (#5388) — stale close modality

Split per Q6.1.

| Component / half | Verdict | User-observable symptom | Evidence |
| --- | --- | --- | --- |
| Shared focus manager — reset on open | **(d:already-present)** | The symptom upstream fixes is a popup returning focus with a visible focus ring left over from a previous open session. It cannot arise here: `lastInteractionType` is a closure local of the manager created by `createFloatingFocusManager`, and `FloatingFocusManager.razor:221-238` creates a manager only when `managerId is null` and disposes it at close, so every open session starts at `''`. | `blazix-baseui-floating.js:3297`, `FloatingFocusManager.razor:217-240`. |
| Shared focus manager — close-time snapshot | **(c)** | The modality reported to `ReturnFocusCallback` and the one deciding the return-focus ring were two separate reads with an interop round trip between them, so a key press or click landing in that window made the callback and the focus ring disagree. | `captureCloseInteractionType` freezes the value in `blazix-baseui-floating.js`; `dispose()` reads the snapshot for both the hover-close suppression and `returnFocusVisible`. Test `FloatingFocusManagerTests.PassesCloseInteractionTypeToReturnFocusCallback` covers the interop rename; the divergence window itself needs a real interop round trip and has no bUnit or Playwright harness. |
| Select | **(c)** | Select tracks the close modality per **root**, not per open session, and never cleared it, so a session closed without a pointer or key press (a controlled close, for example) resolved the previous session's modality and returned focus to the trigger with a leftover visible focus ring. | `consumeCloseInteractionType` in `blazix-baseui-select.js` returns the value and clears it; `SelectRoot.ResolveFinalFocusInteractionTypeAsync` calls it in place of the pure getter. Test `SelectTestsBase.Js_ReadingTheCloseModalityClearsIt` (Server + WASM). |

**Why the reset is on read, not on open.** Upstream resets the modality when the popup opens. The
port cannot use the rising edge of `setRootOpen` for that: instrumenting
`Focus_FinalFocusReceivesKeyboardCloseTypeAfterKeyboardItemSelection` recorded **three**
closed-to-open transitions on the same root for one user-visible open, while the .NET open state
settles, and the last of them landed *after* the keyboard press — wiping the modality the close
then needed. Resetting there broke all three `Focus_FinalFocusReceives*CloseType*` tests. Consuming
the value at the single close-time read is immune to that churn and gives the same observable
semantics, because the value has exactly one consumer.

### `6feeb1f54` (#5357) — predefined change reasons

| Component | Verdict | User-observable symptom | Evidence |
| --- | --- | --- | --- |
| All | **(d:moot)** | Upstream replaced string literals with constants of identical value, so no reason value changed and no behavior keyed on a reason can differ. The port models reasons as typed C# enums, which makes literal drift structurally impossible. | Value-level check of the two flagged families: `MenuOpenChangeReason` (`Menu/Enumerations.cs:66,69`) carries both `SiblingOpen` and `ListNavigation`; `NumberFieldChangeReason` (`NumberField/Enumerations.cs:52-72`) carries all five of `IncrementPress`/`DecrementPress`/`Wheel`/`Scrub`/`Keyboard`. |

### Popup handles — `7397c99ba` (#5339), `3b5715cc7` (#5387), `071e89201` (#5394)

The Handle surface decision was ratified on
[#157](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/157) and executed in PR
[#203](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/203), so these are no longer
defer-with-spec. The port's handle architecture differs structurally from upstream's: the handle
object **is** the shared state — it owns `registeredTriggers` and the open state, and Roots
subscribe to it (`ComponentHandleBase.Subscribe`). There is no fallback store and no store pointer
that swaps, so the class of defects these three commits repair has no local equivalent.

| Upstream | Verdict | User-observable symptom | Evidence |
| --- | --- | --- | --- |
| `7397c99ba` #5339 | **(d:moot)** | Upstream's symptom is a handle call issued while the Root is still mounting landing on the handle's inert fallback store, so the popup did not open and the trigger was not associated with it. It cannot arise here: every popup Root calls `Subscribe` from `OnInitialized` (`MenuRoot.razor:266`, `DialogRoot.razor:234`, `PopoverRoot.razor:210`, `TooltipRoot.razor:210`, `PreviewCardRoot.razor:175`), which runs before the Root renders its children and before every `OnAfterRender` in the batch, and each trigger family registers (or re-registers) from `OnAfterRender(firstRender)`. | Lifecycle sites above; `MenuTrigger.razor:222`, `PopoverTypedTrigger.razor:278`, `TooltipTypedTrigger.razor:276`, `PreviewCardTypedTrigger.razor:261`, `DialogTypedTrigger.razor:230`. |
| `3b5715cc7` #5387 — hydration snapshot | **(d:moot)** | Upstream's symptom is a detached trigger rendering different markup on the server than on hydration, because the client read a root store the server run could not have. Blazor has no hydration reconciliation of an external store: a prerendered circuit re-runs the component lifecycle on connect, so there is no server-versus-client snapshot to keep stable. | `usePopupHandleStore`'s `getServerSnapshot` has no analog; the port's triggers read `ComponentHandleBase` directly. |
| `3b5715cc7` #5387 — pending vs unmounted active trigger | **(d:moot)** | Upstream's symptom is a hover popup closing itself because an active trigger id that had not yet matched a registered trigger was misread as a trigger that unmounted. The port cannot misclassify: the close is requested only from the unregister callback, which fires from a trigger that had registered. | `TooltipRoot.razor:667` and `PreviewCardRoot.razor:371`/`:538` are the only callers of `QueueCloseOnActiveTriggerUnmount`, and both are reached only from `UnregisterTriggerElement`/`OnTriggerUnregistered`. |
| `071e89201` #5394 | **(a)** | No runtime content: the change stops mounting a component that was already a no-op when no handle was supplied, so a handle-less Root's output and behavior are unchanged. | `PopupHandleAttachment` returned `null` and its effect returned early without a handle; the port has no per-Root attachment component at all. |

**Flagged, out of scope.** `ComponentHandleBase.Subscribe` does not replay existing registrations,
so a Root that mounts in a *later* render batch than its detached triggers never learns about them.
Upstream covers this in `attachStore`, which predates this audit window, so it is not residue of
these three commits. Recorded as a separate follow-up.

### Revisit-on-symptom

`b38becd6e` (#5337) and `006a72a99` (#5341) remain **revisit-on-symptom** per the epic. No symptom
has been recorded against either to date; neither is ported.

---

## Related follow-up folded in

Issue [#167](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/167) left one item open that
lives in the same two handlers as `9a5c3850f`: a forced-open **read-only** Select still highlighted
items on hover. Upstream gates the list-navigation hook itself with
`enabled: !readOnly && !disabled` (`SelectRoot.tsx:349-350`); the local gate at
`blazix-baseui-select.js:393-401` covered keyboard navigation only. `SelectItem`'s hover handlers
now check `ReadOnly` as well. Test `SelectItemTests.DoesNotHighlightOnHoverWhenReadOnly`.
