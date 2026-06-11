# Tooltip Functional Audit

Date: 2026-05-27

Component: `Tooltip`

React source audited: `.base-ui/packages/react/src/tooltip`

Blazor source audited: `src/BlazorBaseUI/Tooltip`, `src/BlazorBaseUI/wwwroot/blazor-baseui-tooltip.js`, `src/BlazorBaseUI/wwwroot/blazor-baseui-floating.js`

Spec artifacts:

- `../base-ui-specs/tooltip/SPEC.md`
- `../base-ui-specs/tooltip/pitfalls.md`

## React Parts Found

- `TooltipRoot`
- `TooltipTrigger`
- `TooltipPopup`
- `TooltipPositioner`
- `TooltipArrow`
- `TooltipProvider`
- `TooltipPortal`
- `TooltipViewport`
- `TooltipHandle`
- `TooltipStore`

## Resolved Gaps

| Gap | React source behavior | Blazor repair | Verification |
| --- | --- | --- | --- |
| Enabled trigger identifier missing | Enabled triggers emit `data-base-ui-tooltip-trigger`; disabled triggers omit it. | Added exact attribute on enabled triggers and scoped lint suppression for Base UI parity. | `TooltipTriggerTests.HasBaseUiTooltipTriggerIdentifierWhenEnabled`, `OmitsBaseUiTooltipTriggerIdentifierWhenDisabled` |
| Popup focusable props missing | Popup receives `tabIndex=-1` and `data-base-ui-focusable`. | Added both exact attributes to `TooltipPopup`. | `TooltipPopupTests.HasFocusablePopupAttributes` |
| Already-open trigger switch was ignored | Opening from another trigger switches active trigger/payload without remount. | `TooltipRoot.SetOpenAsync` now treats active-trigger changes as state changes, updates payload, syncs JS root state, and notifies triggers. | `TooltipRootTests.SwitchesActiveTriggerAndPayloadWhileAlreadyOpen`, Playwright `MultipleTriggers_HoverSwitchesActivePayload` |
| Trigger attributes did not rerender on root state changes | Trigger state comes from store selectors in React. | Removed derived attribute caching and added root context state notifications for descendant triggers. | Tooltip bUnit and Playwright suites |
| Disabled trigger did not close an active tooltip | Disabled trigger hover/focus closes the current tooltip and does not become active. | Trigger hover/focus close an open root/handle when disabled. | `TooltipRootTests.DisabledTriggerClosesActiveTooltip` |
| Pointer down handler override gap | React merges internal pointer handling with consumer handler. | Internal pointer handling now forwards `onpointerdown` via `EventUtilities`. | `TooltipTriggerTests.ForwardsPointerDownHandlerAfterInternalHandling` |
| Delayed hover open survived click | React `cancelPendingOpen` clears pending hover open on press/click. | Added JS `cancelPendingHoverOpen` and trigger pointer handling. | Playwright `ClickBeforeHoverDelayCancelsPendingOpen` |
| Provider delay group handoff was delayed | React keeps the group open delay at zero while one tooltip is open and during the timeout after close. | `FloatingDelayGroup` now notifies descendants when the effective group delay changes and triggers update existing JS hover controllers in place. | Playwright `ProviderDelayGroup_OpensNextTooltipImmediatelyDuringTimeout` |
| Arrow demo offset produced gap/overlap | React leaves perpendicular arrow placement to CSS while JS owns only the along-edge coordinate. | Demo/test arrow CSS now uses block SVG sizing and side-specific offsets that overlap the popup edge by 1px. | Playwright `Arrow_TouchesPopupEdgeWithoutGapOrDeepOverlap` |
| Demo arrow CSS was not emitted for updated Tailwind arbitrary values | React demo supplies concrete popup/arrow CSS that positions the arrow outside the popup. | Demo now uses a scoped `.demo-tooltip-arrow` CSS class and `relative flex flex-col` popup class instead of relying on stale generated Tailwind arbitrary values. | Browser regression log `tooltip-demo-browser-regression.log` |
| Hover delay update reinitialized JS controllers | React `delayRef` updates are consumed without replacing hover listeners. | Trigger delay changes now call `updateHoverInteractionDelays` instead of recreating hover interactions. | Full Tooltip Playwright suite |
| Delay-group callbacks invoked disposed JS module during teardown | React effects clean up against mounted refs and tolerate provider teardown order. | `FloatingDelayGroup` marks itself disposed before module disposal, makes cascaded callbacks no-ops after disposal, and catches `ObjectDisposedException` with JS disconnect cases. | `FloatingDelayGroupTests.ContextCallbacksDoNotInvokeJsAfterDispose` |
| JS root state was unsynced for `DefaultOpen` | Initially open tooltip must respond to Escape/outside JS interactions. | `InitializeJsAsync` now calls `setRootOpen` when initially open. | Playwright `Escape_ClosesDefaultOpenTooltip` |
| Hover JS was root-wide instead of per-trigger | React tracks active trigger through trigger-specific hover interactions. | Tooltip JS now keeps per-trigger hover interactions and per-trigger element map. | Playwright multi-trigger hover tests |
| `onOpenChange` lacked trigger detail | React change details include `trigger`. | Added `TriggerId` and `TriggerElement` to `TooltipOpenChangeEventArgs`. | `TooltipRootTests.OnOpenChangeIncludesTriggerId` |
| bUnit provider tests mocked wrong floating module path | Runtime imports `blazor-baseui-floating.min.js`. | `SetupFloatingDelayGroupModule` now mocks minified and non-minified floating modules. | Full Tooltip bUnit suite |
| Demo rendered duplicate trigger IDs | React examples keep trigger IDs unique so ARIA ownership and DOM lookup remain deterministic. | Typed payload handle demo now uses `typed-user-*` IDs instead of reusing the payload demo's `user-*` IDs. | `DemoShellTests.TooltipSection_DoesNotRenderDuplicateElementIds`, live demo Playwright audit |

## Parity Matrix

| React hook/utility/source | Blazor equivalent | Status |
| --- | --- | --- |
| `TooltipStore.useStore` | `TooltipRoot` fields plus optional `TooltipHandle` state synchronization | Verified |
| `store.useControlledProp('openProp')` | `Open` parameter and `CurrentOpen` computed state | Verified |
| `store.useControlledProp('triggerIdProp')` | `TriggerId`, `DefaultTriggerId`, `activeTriggerId` | Verified |
| `store.useContextCallback` | `EventCallback<TooltipOpenChangeEventArgs>` and `EventCallback<bool>` | Verified |
| `createChangeEventDetails` | `TooltipOpenChangeEventArgs` with cancel, propagation, prevent-unmount, trigger ID, and trigger element | Verified |
| `useOnFirstRender` default-open repair | `OnInitialized` plus first-render JS `setRootOpen` sync | Verified |
| `useImplicitActiveTrigger` | `TrySetImplicitActiveTrigger()` | Verified |
| `useOpenStateTransitions` | `TransitionLifecycleManager` and JS transition callbacks | Verified |
| `preventUnmountOnClose()` | `OpenChangeEventArgs.PreventUnmountOnClose()` | Verified |
| `onOpenChangeComplete` | `OnOpenChangeComplete` invoked after transition end or force unmount | Verified |
| `useDelayGroup` | `TooltipProvider` plus `FloatingDelayGroupContext` state notifications | Verified |
| Provider instant phase | `TooltipInstantType.Delay`, effective delay group state, and in-place JS hover delay updates | Verified |
| `useHoverReferenceInteraction` | Tooltip JS `initializeHoverInteraction` per trigger | Verified |
| `safePolygon()` | Shared floating JS `createHoverInteraction` safe-polygon path | Verified |
| `cancelPendingOpen` | JS `cancelPendingHoverOpen` plus trigger pointer handling | Verified |
| `useFocus` | Trigger focus/blur handlers in Blazor | Verified |
| `useDismiss` outside press | Tooltip JS `createDismissInteraction` | Verified |
| Escape key dismissal | Tooltip JS global Escape handler and `OnEscapeKey` | Verified |
| `useClientPoint` | Tooltip JS cursor tracking and virtual anchor support | Verified |
| `useTriggerDataForwarding` | trigger element/payload dictionaries and handle registration | Verified |
| `PopupTriggerMap` | `triggerElements` plus JS `triggerElements` map | Verified |
| `setOpenTriggerState` | `SetActiveTrigger`, payload sync, active trigger attribute sync | Verified |
| `useRenderElement` | Shared `RenderElement<TState>` | Verified |
| `triggerOpenStateMapping` | Trigger `data-popup-open` and state object | Verified |
| `popupStateMapping` | Popup, positioner, and arrow data attributes | Verified |
| `FOCUSABLE_POPUP_PROPS` | `tabindex="-1"` and `data-base-ui-focusable` | Verified |
| `TooltipHandle` | `TooltipHandle<TPayload>` and `ITooltipHandleSubscriber` | Verified |
| `TooltipPortalContext` | `TooltipPortalContext.KeepMounted` | Verified |
| `usePopupViewport` | `TooltipViewport` plus `blazor-baseui-tooltip-viewport.js` | Verified |
| Positioning utilities | `PositionerInterop` plus tooltip JS `initializePositioner`/`updatePosition` | Verified |

## Attribute Matrix

| Part | React attributes | Blazor status |
| --- | --- | --- |
| Trigger | `id`, `type=button`, `aria-describedby`, `data-popup-open`, `data-trigger-disabled`, `data-base-ui-tooltip-trigger` | Present |
| Popup | `id`, `role=tooltip`, `tabIndex=-1`, `data-base-ui-focusable`, `data-side`, `data-align`, `data-open`, `data-closed`, transition attrs, `data-instant` | Present |
| Positioner | `role=presentation`, `hidden`, `data-side`, `data-align`, `data-open`, `data-closed`, `data-anchor-hidden`, transition attrs, `data-instant` | Present |
| Arrow | `aria-hidden=true`, `data-open`, `data-closed`, `data-side`, `data-align`, `data-uncentered`, `data-instant` | Present |
| Viewport | `data-current`, `data-previous`, `data-activation-direction`, `data-transitioning`, `data-instant`, current/previous transition attrs | Present |

## Verification Commands

| Command | Result | Log |
| --- | --- | --- |
| `dotnet build BlazorBaseUI.slnx` | Passed, 0 warnings, 0 errors | `docs/audits/logs/tooltip-dotnet-build.log` |
| `bash scripts/lint-rules.sh` | Passed, 0 violations. The log records macOS `grep -P` warnings from the existing script before the zero-violation summary. | `docs/audits/logs/tooltip-lint-rules.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~Tooltip"` | Passed, 89/89 | `docs/audits/logs/tooltip-bunit-tests.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~FloatingDelayGroupTests"` | Passed, 17/17 | `docs/audits/logs/tooltip-floating-delay-group-tests.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~DemoShellTests"` | Passed, 5/5 | `docs/audits/logs/tooltip-demo-shell-tests.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~TooltipTests"` | Passed, 76/76 | `docs/audits/logs/tooltip-playwright-tests.log` |
| Playwright browser automation against `http://127.0.0.1:5099/tooltip` | Passed: 48/48 at 1440x1100 and 48/48 at 816x640; no duplicate IDs; no browser console errors; provider instant switching at 66ms/65ms | `docs/audits/logs/tooltip-demo-browser-regression.log` |
| `git diff --check` | Passed, no whitespace errors | `docs/audits/logs/tooltip-git-diff-check.log` |

## TDD Failure Evidence

Initial bUnit tests were added before implementation and failed for the verified gaps:

- Missing popup focusable attributes.
- Missing `data-base-ui-tooltip-trigger`.
- Active trigger switch retained the first payload.
- Pointer down consumer handler was not forwarded.
- Disabled trigger did not close the active tooltip.
- Demo browser probe failed with 27px right-arrow overlap before `.demo-tooltip-arrow` was introduced.
- `FloatingDelayGroupTests.ContextCallbacksDoNotInvokeJsAfterDispose` failed because disposed delay-group callbacks still invoked JS.
- `DemoShellTests.TooltipSection_DoesNotRenderDuplicateElementIds` failed against the previous typed handle demo IDs with duplicates `user-1`, `user-2`, and `user-3`, then passed after switching them to `typed-user-*`.

The final bUnit and Playwright runs above validate the repaired behavior.

## Manual Checks

- Compared React files under `.base-ui/packages/react/src/tooltip/root`, `trigger`, `popup`, `positioner`, `arrow`, `provider`, `portal`, `viewport`, and `store`.
- Compared Tooltip data-attribute enum files against Blazor component attribute builders.
- Confirmed JS-heavy behavior remains in JS: safe polygon hover, outside press, Escape, cursor tracking, virtual anchors, positioning, transition timing, and viewport morphing.
- Confirmed provider delay-group state stays DOM-side for timing and only notifies Blazor to update existing hover controller delays.
- Confirmed arrow perpendicular placement remains CSS-owned, matching React `arrowStyles`.
- Confirmed the demo popup is the positioned containing block for its arrow, matching the React demo popup class.
- Confirmed provider teardown does not invoke disposed JS module references through stale cascaded callbacks.
- Confirmed Blazor lifecycle replaces React sync loops: mutable root context refreshes in lifecycle/state methods and triggers subscribe to root state notifications.
- Confirmed minified tooltip and floating JS files are regenerated after JS source changes.

## Final State

No remaining Tooltip parity gaps were identified in the audited React source after the repairs above.
