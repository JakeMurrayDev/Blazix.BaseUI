---
project: blazorbaseui-navmenu-audit
ticket: T-002
generated: 2026-05-09T22:47:37.1523291+08:00
---

# NavigationMenu 1:1 React-Port Fix Summary

## F-001

- finding_id: F-001
- severity: MUST_FIX
- statement: Content was not portaled into the viewport target.
- react_reference: `.base-ui/packages/react/src/navigation-menu/content/NavigationMenuContent.tsx:133-171`; `.base-ui/packages/react/src/navigation-menu/viewport/NavigationMenuViewport.tsx:120-125`
- blazor_fix: `src/BlazorBaseUI/NavigationMenu/NavigationMenuViewport.razor:23-39`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuViewport.razor:89-95`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuRoot.razor:581-597`, `src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js:499-545`, `src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js:643-654`
- js_native_exception: Yes. The actual DOM reparenting is intentionally performed in JS because Blazor cannot directly portal an already-rendered component subtree into a runtime `ElementReference` target without DOM interop.

Technical justification: The viewport now renders a dedicated target element and captures it, root context stores and forwards that element through `SetViewportTargetElement`, and the JS module moves each content element into `viewportTargetElement || viewportElement` while preserving the original parent for disposal. That matches the React branch's observable contract: active and kept content no longer remains only at declaration site, and viewport target registration is no longer a no-op.

## F-002

- finding_id: F-002
- severity: MUST_FIX
- statement: The viewport focus guard model was missing.
- react_reference: `.base-ui/packages/react/src/navigation-menu/viewport/NavigationMenuViewport.tsx:21-64`; `.base-ui/packages/react/src/navigation-menu/viewport/NavigationMenuViewport.tsx:100-132`
- blazor_fix: `src/BlazorBaseUI/NavigationMenu/NavigationMenuViewport.razor:23-39`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuViewport.razor:98-128`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuRoot.razor:179-182`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuRoot.razor:686-744`, `src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js:656-728`
- js_native_exception: Yes. Tabbable discovery and focus redirection stay in JS because they depend on live DOM order, computed visibility, and `inert` ancestry.

Technical justification: The viewport render path now wraps the target with two `FocusGuard` components, applies `inert` from root context, sets viewport inert on blur, and uses root callbacks to focus either the previous tabbable element or active menu content. The JS helpers perform DOM-native tabbable lookup and focus movement, closing the missing guard and `viewportInert` parity branch without forcing DOM traversal into C#.

## F-003

- finding_id: F-003
- severity: MUST_FIX
- statement: Trigger and Link did not close on focus leaving the menu.
- react_reference: `.base-ui/packages/react/src/navigation-menu/trigger/NavigationMenuTrigger.tsx:772-785`; `.base-ui/packages/react/src/navigation-menu/link/NavigationMenuLink.tsx:49-62`; `.base-ui/packages/react/src/navigation-menu/utils/isOutsideMenuEvent.ts:17-44`
- blazor_fix: `src/BlazorBaseUI/NavigationMenu/Enumerations.cs:18-42`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuRoot.razor:276-283`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuTrigger.razor:140-168`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuLink.razor:100-120`, `src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js:65-77`, `src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js:94-129`
- js_native_exception: Yes. Focus-out containment uses native `focusout` and `relatedTarget` in JS, which is the correct layer for the low-level DOM containment check.

Technical justification: The JS module now listens for document `focusout`, verifies that the blur originated inside an open NavigationMenu, rejects related targets still inside the root, popup, viewport, viewport target, trigger set, content set, or another NavigationMenu, then invokes `OnFocusOut`. The root maps that into `SetValueAsync(null, NavigationMenuCloseReason.FocusOut)`, and trigger/link render paths expose blur forwarding so user handlers still run. This closes the same outside-menu focus transition that React handles through `isOutsideMenuEvent`.

## F-004

- finding_id: F-004
- severity: MUST_FIX
- statement: The `NavigationMenuDismissContext` / `useDismiss` branch was absent.
- react_reference: `.base-ui/packages/react/src/navigation-menu/list/NavigationMenuDismissContext.ts:4-11`; `.base-ui/packages/react/src/navigation-menu/list/NavigationMenuList.tsx:53-65`; `.base-ui/packages/react/src/navigation-menu/trigger/NavigationMenuTrigger.tsx:108`; `.base-ui/packages/react/src/navigation-menu/trigger/NavigationMenuTrigger.tsx:807-813`
- blazor_fix: `src/BlazorBaseUI/NavigationMenu/NavigationMenuRoot.razor:258-283`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuRoot.razor:640-652`, `src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js:25-63`, `src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js:79-129`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuLink.razor:105-115`
- js_native_exception: Yes. The Blazor port implements the dismiss branch as a JS-native equivalent instead of a literal React context because document-level Escape, outside press, nested-menu containment, and trigger exclusion are native DOM event concerns.

Technical justification: The production behavior supplied by React's dismiss props is now present: document Escape prevents default/propagation and closes with `EscapeKey`, outside mousedown excludes triggers and all registered NavigationMenu surfaces before closing with `OutsidePress`, focus-out closes with `FocusOut`, and link press propagates parent close through `EmitLinkCloseAsync`. Although no separate `NavigationMenuDismissContext` file was introduced, the current render and JS paths cover the actionable dismiss semantics called out by the finding.

## F-005

- finding_id: F-005
- severity: MUST_FIX
- statement: Controlled `Value = null` could not be represented.
- react_reference: `.base-ui/packages/react/src/navigation-menu/root/NavigationMenuRoot.tsx:80-88`
- blazor_fix: `src/BlazorBaseUI/NavigationMenu/NavigationMenuRoot.razor:49-51`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuRoot.razor:472-475`, `tests/BlazorBaseUI.Tests/NavigationMenu/NavigationMenuRootTests.cs:227-243`
- js_native_exception: No.

Technical justification: Controlled mode now treats either a non-null `Value` or a bound `ValueChanged` callback as the signal, so `Value=null` plus binding remains controlled instead of falling back to `DefaultValue`. `ApplyValue` only mutates internal `currentValue` for uncontrolled operation or parameter synchronization, and the new unit test verifies that a controlled null value keeps the trigger closed despite a default value.

## F-006

- finding_id: F-006
- severity: MUST_FIX
- statement: Controlled close did not start the close transition.
- react_reference: `.base-ui/packages/react/src/navigation-menu/root/NavigationMenuRoot.tsx:87-119`; `.base-ui/packages/react/src/navigation-menu/root/NavigationMenuRoot.tsx:183-203`
- blazor_fix: `src/BlazorBaseUI/NavigationMenu/NavigationMenuRoot.razor:147-164`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuRoot.razor:188-193`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuRoot.razor:449-484`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuContent.razor:74-89`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuContent.razor:127-166`
- js_native_exception: No.

Technical justification: Root now drives all controlled parameter changes through `ApplyValue`, which begins open transitions when `nextValue` is non-null and begins close transitions when the current context value or mounted state indicates a close. The resulting transition status and mounted state are stored back into context, and content consumes those values to render `data-starting-style`, `data-ending-style`, hidden, inert, and kept-mounted states. That gives close the same lifecycle treatment as open instead of leaving stale mounted state behind.

## F-007

- finding_id: F-007
- severity: MUST_FIX
- statement: Positioner computed placement was not reflected in `data-side` / `data-align`.
- react_reference: `.base-ui/packages/react/src/navigation-menu/positioner/NavigationMenuPositioner.tsx:131-165`
- blazor_fix: `src/BlazorBaseUI/NavigationMenu/NavigationMenuPositioner.razor:215-241`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuPositioner.razor:267-268`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuPositioner.razor:343-366`, `src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js:905-924`, `tests/BlazorBaseUI.Tests/NavigationMenu/NavigationMenuPositionerTests.cs:91-105`
- js_native_exception: Yes. Collision and effective placement are computed by the shared floating JS module, so JS reports the computed side/align back to C#.

Technical justification: The positioner now initializes shared floating interop with a .NET callback, receives effective side, align, anchor-hidden, and arrow-uncentered values in `OnPositionUpdated`, stores them in the positioner context/state, and emits `data-side`/`data-align` from state rather than raw parameters. The added test directly calls the callback and verifies the rendered attributes update to the computed placement.

## F-008

- finding_id: F-008
- severity: MUST_FIX
- statement: Nested positioner collision fallback default differed from React.
- react_reference: `.base-ui/packages/react/src/navigation-menu/positioner/NavigationMenuPositioner.tsx:62-65`; `.base-ui/packages/react/src/internals/constants.ts:18-28`
- blazor_fix: `src/BlazorBaseUI/NavigationMenu/NavigationMenuPositioner.razor:29-37`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuPositioner.razor:81-86`, `src/BlazorBaseUI/NavigationMenu/NavigationMenuPositioner.razor:307-321`
- js_native_exception: No.

Technical justification: `CollisionAvoidance` is now nullable, and `ResolvedCollisionAvoidance` selects React's top-level versus nested fallback axis side based on `RootContext?.IsNested`. `BuildConfig` always passes the resolved object into `PositionerConfig` with `UseCollisionAvoidanceObject = true`, so nested menus default to `FallbackAxisSide.End` while top-level menus keep `None` unless the consumer overrides the parameter.

## F-009

- finding_id: F-009
- severity: SHOULD_FIX
- statement: Trigger keyboard open did not stop default behavior or propagation.
- react_reference: `.base-ui/packages/react/src/navigation-menu/trigger/NavigationMenuTrigger.tsx:753-770`
- blazor_fix: `src/BlazorBaseUI/NavigationMenu/NavigationMenuTrigger.razor:140-154`, `src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js:324-342`, `src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js:344-355`
- js_native_exception: Yes. Keyboard cancellation is intentionally handled with native JS `preventDefault()` and `stopPropagation()` because Blazor event modifiers cannot be applied conditionally per key branch from `RenderElement` attributes.

Technical justification: The trigger continues to expose a Blazor `onkeydown` callback for consumer handlers, while JS installs a native keydown listener on each trigger. For the React open keys (`ArrowDown` in horizontal menus and `ArrowRight` in vertical menus), JS prevents default, stops propagation, updates root active state, and calls back to `OnKeyboardOpen`. This closes the exact branch where React used `stopEvent`.

## F-010

- finding_id: F-010
- severity: SHOULD_FIX
- statement: Production JS contained debug `console.warn` diagnostics.
- react_reference: `.base-ui/packages/react/src/navigation-menu/trigger/NavigationMenuTrigger.tsx:735-740`
- blazor_fix: `src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js:311-323`; grep verification: `rg -n "console\\.warn|console\\.log|debugger" src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js src/BlazorBaseUI/NavigationMenu` returned no matches.
- js_native_exception: No.

Technical justification: The click-capture branch still implements the patient-click guard and immediate-close suppression, but the previous production diagnostics are gone. The remaining code prevents default and stops the click when it falls inside the stick-open window without logging to the console, matching React's non-diagnostic production behavior.

## Changed Files

- `src/BlazorBaseUI/NavigationMenu/Enumerations.cs`
- `src/BlazorBaseUI/NavigationMenu/EventArgs.cs`
- `src/BlazorBaseUI/NavigationMenu/NavigationMenuContent.razor`
- `src/BlazorBaseUI/NavigationMenu/NavigationMenuLink.razor`
- `src/BlazorBaseUI/NavigationMenu/NavigationMenuList.razor`
- `src/BlazorBaseUI/NavigationMenu/NavigationMenuPopup.razor`
- `src/BlazorBaseUI/NavigationMenu/NavigationMenuPortal.razor`
- `src/BlazorBaseUI/NavigationMenu/NavigationMenuPositioner.razor`
- `src/BlazorBaseUI/NavigationMenu/NavigationMenuRoot.razor`
- `src/BlazorBaseUI/NavigationMenu/NavigationMenuRootContext.cs`
- `src/BlazorBaseUI/NavigationMenu/NavigationMenuTrigger.razor`
- `src/BlazorBaseUI/NavigationMenu/NavigationMenuViewport.razor`
- `src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js`
- `tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/Infrastructure/RenderModeExtensions.cs`
- `tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/Tests/NavigationMenu/NavigationMenuTestsBase.cs`
- `tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.Client/Pages/Tests/NavigationMenu/NavigationMenuTestPage.razor`
- `tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/Components/Pages/Tests/NavigationMenu/NavigationMenuTestPage.razor`
- `src/BlazorBaseUI/Portal/FloatingPortalLite.razor`
- `src/BlazorBaseUI/Portal/Portal.razor`
- `tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.Client/Pages/Tests/Field/FieldTestPage.razor`
- `tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/Components/Pages/Tests/Field/FieldTestPage.razor`
- `tests/BlazorBaseUI.Tests.Contracts/NavigationMenu/INavigationMenuArrowContract.cs`
- `tests/BlazorBaseUI.Tests.Contracts/NavigationMenu/INavigationMenuBackdropContract.cs`
- `tests/BlazorBaseUI.Tests.Contracts/NavigationMenu/INavigationMenuContentContract.cs`
- `tests/BlazorBaseUI.Tests.Contracts/NavigationMenu/INavigationMenuLinkContract.cs`
- `tests/BlazorBaseUI.Tests.Contracts/NavigationMenu/INavigationMenuListContract.cs`
- `tests/BlazorBaseUI.Tests.Contracts/NavigationMenu/INavigationMenuPopupContract.cs`
- `tests/BlazorBaseUI.Tests.Contracts/NavigationMenu/INavigationMenuPortalContract.cs`
- `tests/BlazorBaseUI.Tests.Contracts/NavigationMenu/INavigationMenuPositionerContract.cs`
- `tests/BlazorBaseUI.Tests.Contracts/NavigationMenu/INavigationMenuTriggerContract.cs`
- `tests/BlazorBaseUI.Tests.Contracts/NavigationMenu/INavigationMenuViewportContract.cs`
- `tests/BlazorBaseUI.Tests/NavigationMenu/NavigationMenuArrowTests.cs`
- `tests/BlazorBaseUI.Tests/NavigationMenu/NavigationMenuBackdropTests.cs`
- `tests/BlazorBaseUI.Tests/NavigationMenu/NavigationMenuContentTests.cs`
- `tests/BlazorBaseUI.Tests/NavigationMenu/NavigationMenuIconTests.cs`
- `tests/BlazorBaseUI.Tests/NavigationMenu/NavigationMenuItemTests.cs`
- `tests/BlazorBaseUI.Tests/NavigationMenu/NavigationMenuLinkTests.cs`
- `tests/BlazorBaseUI.Tests/NavigationMenu/NavigationMenuListTests.cs`
- `tests/BlazorBaseUI.Tests/NavigationMenu/NavigationMenuPopupTests.cs`
- `tests/BlazorBaseUI.Tests/NavigationMenu/NavigationMenuPortalTests.cs`
- `tests/BlazorBaseUI.Tests/NavigationMenu/NavigationMenuPositionerTests.cs`
- `tests/BlazorBaseUI.Tests/NavigationMenu/NavigationMenuRootTests.cs`
- `tests/BlazorBaseUI.Tests/NavigationMenu/NavigationMenuTriggerTests.cs`
- `tests/BlazorBaseUI.Tests/NavigationMenu/NavigationMenuViewportTests.cs`

## Verdict

- FULLY-CLOSED: F-001, F-002, F-003, F-004, F-005, F-006, F-007, F-008, F-009, F-010
- PARTIAL-CLOSED-WITH-FOLLOWUP: none
- findings_closed: 10/10

All ten original audit findings have an implemented closure in the current diff. JS-native exceptions are applied only to preventDefault/stopPropagation, DOM focus routing, live tabbable discovery, content reparenting, and floating-placement callbacks, which are the same low-level DOM areas the operator explicitly allowed to remain JS-native.
