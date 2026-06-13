# Select Functional Audit

Date: 2026-06-10
Component: Select
Source: `.base-ui/packages/react/src/select`
Port: `src/Blazix.BaseUI/Select`

## Audit Scope

- Compared every React Select part, context, store selector, data attribute file, and test file under `.base-ui/packages/react/src/select`.
- Audited Blazor implementation, bUnit tests, Playwright tests, JS interop modules, and minified JS assets.
- Used multiple subagents:
  - Dirac: React source behavior extraction.
  - Ptolemy: Blazor implementation and test gap extraction.
  - Meitner: PNPM source verification command planning.
- Serena and context7 were requested by repo instructions but were not available in this environment. Default repository search and local source were used.

## Repaired Gaps

1. Trigger side state
   - Added root popup-side state and trigger `data-popup-side`.
   - Preserved source behavior where align-item DOM uses `data-side="none"` while trigger reports physical side.

2. Trigger mouseup gating and event forwarding
   - Matched the 400 ms selected and unselected mouseup gate.
   - Preserved consumer `onmousedown` and `onfocus` handlers.
   - Added root-level JS listener fallback so trigger DOM behavior is registered even if trigger-specific JS initialization lags behind first render.

3. Hidden input parity
   - Disabled hidden-input change handling while disabled.
   - Treated empty serialized value as placeholder.
   - Expanded tests for primary and multi-value hidden inputs.

4. Popup/list role switching
   - Switched `HasList` to live list element state instead of stale id state.
   - Cleared list and popup elements on dispose.
   - Added popup state-change subscription and dispatcher-safe rerender.

5. Positioner and align-item lifecycle
   - Replayed positioner registration after JS initialization for default-open races.
   - Added align-item placement scheduling, probing, and watchdog logic in component JS.
   - Updated fallback state immediately when align-item positioning is not active.
   - Recomputed rendered side when align-item state changes even if physical side does not.

6. Item pointer behavior
   - Implemented pointerdown-started mouse click selection for unhighlighted items.
   - Rejected synthetic direct unhighlighted mouse clicks.
   - Implemented squared drag threshold 64 for drag-to-select.
   - Preserved touch direct click behavior.

7. Transition and stale callbacks
   - Ignored stale `OnOpenChangeComplete` callbacks.
   - Corrected initial selected item-indicator transition state.

8. Dispatcher and disposal integrity
   - Made group label, popup, root, and list state changes dispatcher-safe where lifecycle callbacks can cross render timing.
   - Added disposed guards for list registration.

9. Test infrastructure
   - Updated JSInterop setup for minified module paths.
   - Serialized Select Playwright fixtures to avoid shared browser interference.
   - Added/updated unit and browser tests for repaired behaviors.

## Files Modified

- `src/Blazix.BaseUI/Select/ISelectRootContext.cs`
- `src/Blazix.BaseUI/Select/SelectGroup.razor`
- `src/Blazix.BaseUI/Select/SelectItem.razor`
- `src/Blazix.BaseUI/Select/SelectItemIndicator.razor`
- `src/Blazix.BaseUI/Select/SelectList.razor`
- `src/Blazix.BaseUI/Select/SelectPopup.razor`
- `src/Blazix.BaseUI/Select/SelectPositioner.razor`
- `src/Blazix.BaseUI/Select/SelectRoot.razor`
- `src/Blazix.BaseUI/Select/SelectRootContext.cs`
- `src/Blazix.BaseUI/Select/SelectTrigger.razor`
- `src/Blazix.BaseUI/Select/SelectTriggerState.cs`
- `src/Blazix.BaseUI/wwwroot/blazix-baseui-floating.js`
- `src/Blazix.BaseUI/wwwroot/blazix-baseui-floating.min.js`
- `src/Blazix.BaseUI/wwwroot/blazix-baseui-select.js`
- `src/Blazix.BaseUI/wwwroot/blazix-baseui-select.min.js`
- `tests/Blazix.BaseUI.Tests/Infrastructure/JsInteropSetup.cs`
- `tests/Blazix.BaseUI.Tests/Select/*`
- `tests/Blazix.BaseUI.Tests.Contracts/Select/ISelectTriggerContract.cs`
- `tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests/Tests/Select/*`

## Residuals

- Blazor Select validation is green.
- React source Chromium Select browser tests are green.
- React source Firefox and WebKit Select browser runs reproduce failures inside the upstream React source test environment:
  - Firefox: `SelectRoot.test.tsx`, `highlightItemOnHover`, expected `data-highlighted`.
  - WebKit: `SelectRoot.test.tsx`, `select inside popover`, act warning reported through `vitest-fail-on-console`.
- The broad React source all-browser command also runs unrelated non-Select suites and fails from unrelated upstream files. This is logged separately and was not used as Blazor port pass/fail evidence.

## Audit Result

All identified Blazor Select functional gaps from the React source comparison were repaired or explicitly accounted for in code, tests, and `../base-ui-specs/select`.
