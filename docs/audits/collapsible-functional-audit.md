# Collapsible Functional Audit

Date: 2026-06-26

Scope: `src/Blazix.BaseUI/Collapsible`, `src/Blazix.BaseUI/wwwroot/blazix-baseui-collapsible.js`, Collapsible bUnit tests, Collapsible Playwright pages/tests, and the local React Base UI source under `.base-ui`.

## Result

The Collapsible port now matches the current React Base UI source behavior for root state control, trigger attributes and disabled behavior, panel mount/hidden behavior, transition data attributes, hidden-until-found `beforematch`, and the recent upstream bug-fix deltas evaluated in this audit.

No unresolved functional gaps remain for the audited Collapsible surface.

## Source Baseline

Primary source files:

- `.base-ui/packages/react/src/collapsible/root/CollapsibleRoot.tsx`
- `.base-ui/packages/react/src/collapsible/root/useCollapsibleRoot.ts`
- `.base-ui/packages/react/src/collapsible/trigger/CollapsibleTrigger.tsx`
- `.base-ui/packages/react/src/collapsible/trigger/useCollapsibleTrigger.ts`
- `.base-ui/packages/react/src/collapsible/panel/CollapsiblePanel.tsx`
- `.base-ui/packages/react/src/collapsible/panel/useCollapsiblePanel.ts`
- `.base-ui/packages/react/src/internals/useTransitionStatus.ts`

The source docs route `http://localhost:3005/react/components/collapsible` was run with `pnpm docs:dev` and inspected through the in-app browser. It documented Root, Trigger, Panel, `defaultOpen`, `open`, `onOpenChange`, `disabled`, `hiddenUntilFound`, `keepMounted`, `data-panel-open`, `data-open`, `data-closed`, `data-starting-style`, and `data-ending-style`. The Blazor test page rendered the matching closed and open attributes. Evidence: `docs/audits/logs/collapsible-inapp-browser-comparison.json`.

## Upstream Delta And Impact Report

### `e18d78832` - `[collapsible] Fix trigger and panel state bugs (#4848)`

Impact:

- Disabled trigger press must not invoke `onOpenChange`.
- Controlled mode must not mutate internal state without parameter update.
- Trigger `id` must forward.
- `beforematch` cancellation must not arm the next instant-open suppression.
- A trigger open while `beforematch` is pending must not be blocked or misclassified as a find-in-page reveal.
- Initially open CSS keyframe suppression must clear as soon as close is requested.

Implemented and verified:

- Trigger disabled handling stays at the trigger and returns before root change handling.
- Root `HandleBeforeMatch` now returns `false` if a pending callback resolves after another path already opened the panel.
- Root context records the accepted open reason and panel consumes it before JS `open()` so server and WASM classify accepted `beforematch` opens deterministically.
- JS no longer awaits a generic pending `beforematch` promise inside `open()`. It tracks `openedWhileBeforeMatchPending` and only arms one-shot suppression for accepted, unsuperseded `beforematch`.
- Panel clears `shouldPreventMountAnimation` on close request and JS clears initial transition/animation suppression on close start.
- Added bUnit and Playwright coverage for disabled callbacks, cancel-close, trigger `id`, dynamic state callbacks, pending accepted/canceled beforematch races, and accepted beforematch duration restoration.

Implementation anchors:

- `CollapsibleRootContext.OpenChangeReason` and `ClearOpenChangeReason`: `src/Blazix.BaseUI/Collapsible/CollapsibleRootContext.cs`
- Trigger and beforematch reason assignment: `src/Blazix.BaseUI/Collapsible/CollapsibleRoot.razor`
- Panel reason consumption and beforematch JS call: `src/Blazix.BaseUI/Collapsible/CollapsiblePanel.razor`
- JS pending beforematch handling and `open(panel, skipAnimation, beforeMatchOpen)`: `src/Blazix.BaseUI/wwwroot/blazix-baseui-collapsible.js`

### `d33150322` - `[collapsible] Fix open state when keepMounted with no transitions (#4555)`

Impact:

- `keepMounted` panels with no author motion must still become hidden on close.

Status:

- Existing JS close completion path calls back into Blazor for no-motion close and `OnAnimationEnded("close", true)` sets `isMounted = false`.
- Full Playwright coverage verifies `KeepMounted` closed panels remain attached and hidden.

### `4133d56f7` - `[accordion][collapsible] Simplify shouldRender condition (#4338)`

Impact:

- Render condition is `keepMounted || hiddenUntilFound || mounted || open`.

Status:

- Blazor `IsPresent` implements the equivalent condition.

### `3d0be4e37` - `[collapsible] Refactor panel logic (#4565)`

Impact:

- Panel logic moved to stricter measurement, transition, hidden-until-found, and temporary-style restoration sequencing.

Implemented and verified:

- JS measures dimensions synchronously before open/close motion.
- JS waits one animation frame before close transition completion detection.
- Temporary `0s` duration from accepted `beforematch` is restored before close animation detection.
- Hidden-until-found closed panels persist `data-starting-style` unless CSS keyframes are active, and Blazor owns that attribute so rerenders do not drop it.

## Repairs Applied

- Added root-context open reason tracking for internal trigger vs accepted `beforematch` opens.
- Removed generic JS waiting on `beforeMatchPromise` from `open()`.
- Added JS `openedWhileBeforeMatchPending` to prevent accepted delayed `beforematch` from suppressing an intervening trigger open.
- Added `beforeMatchOpen` parameter to JS `open()` and passed it from `CollapsiblePanel.OpenAsync`.
- Persisted hidden-until-found closed `data-starting-style` from Razor based on detected animation type reported by JS.
- Cleared initial open animation suppression on close request and close JS start.
- Expanded JS interop exception guards to include `ObjectDisposedException`.
- Regenerated `blazix-baseui-collapsible.min.js` from the readable JS source.

## Attribute Surface

Verified:

- Root: `data-open`, `data-closed`, `data-disabled`, `data-starting-style`, `data-ending-style`.
- Trigger: `type="button"`, `tabindex="0"`, `aria-expanded`, open-only `aria-controls`, `aria-disabled` when disabled, `data-panel-open`, `data-disabled`, `data-starting-style`, `data-ending-style`, forwarded `id`.
- Panel: `id`, `hidden`, `hidden="until-found"`, `data-open`, `data-closed`, `data-disabled`, `data-starting-style`, `data-ending-style`, CSS variables `--collapsible-panel-height` and `--collapsible-panel-width`.

## Verification Summary

- `dotnet build Blazix.BaseUI.slnx`: passed.
- `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --filter "FullyQualifiedName~Collapsible" -v minimal`: 64 passed.
- `dotnet test tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~Collapsible" -v minimal`: 68 passed.
- `bash scripts/lint-rules.sh`: 0 violations.
- `.base-ui` source tests: 83 passed in Chromium.
- Source docs route served successfully and was inspected with in-app browser.

Full command evidence is in `docs/audits/logs/`.
