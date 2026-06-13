# Popover Verification Report

Date: 2026-06-07

## Scope

Performed a 1:1 functional audit and repair of the Blazor Popover port against the vendored React Base UI Popover source and shared floating-ui utilities.

Serena and context7 were requested by repository instructions, but no callable Serena or context7 tool was available in this Codex session. Discovery used `rg`, the local `.base-ui` React source, repository tests, ASP.NET Core guidance, Playwright automation, and browser automation through the Node REPL.

Existing unrelated working-tree changes were present and not staged:

- `AGENTS.md`

## React Source Files Compared

- `.base-ui/packages/react/src/popover/root/PopoverRoot.tsx`
- `.base-ui/packages/react/src/popover/store/PopoverStore.ts`
- `.base-ui/packages/react/src/popover/store/PopoverHandle.ts`
- `.base-ui/packages/react/src/popover/trigger/PopoverTrigger.tsx`
- `.base-ui/packages/react/src/popover/popup/PopoverPopup.tsx`
- `.base-ui/packages/react/src/popover/portal/PopoverPortal.tsx`
- `.base-ui/packages/react/src/popover/positioner/PopoverPositioner.tsx`
- `.base-ui/packages/react/src/popover/backdrop/PopoverBackdrop.tsx`
- `.base-ui/packages/react/src/popover/viewport/PopoverViewport.tsx`
- `.base-ui/packages/react/src/popover/title/PopoverTitle.tsx`
- `.base-ui/packages/react/src/popover/description/PopoverDescription.tsx`
- `.base-ui/packages/react/src/popover/close/PopoverClose.tsx`
- `.base-ui/packages/react/src/popover/arrow/PopoverArrow.tsx`
- `.base-ui/packages/react/src/popover/root/PopoverRoot.test.tsx`
- `.base-ui/packages/react/src/popover/trigger/PopoverTrigger.test.tsx`
- `.base-ui/packages/react/src/popover/viewport/PopoverViewport.test.tsx`
- `.base-ui/packages/react/src/floating-ui-react/components/FloatingFocusManager.tsx`
- `.base-ui/packages/react/src/floating-ui-react/components/FloatingPortal.tsx`
- `.base-ui/packages/react/src/floating-ui-react/hooks/useClick.ts`
- `.base-ui/packages/react/src/floating-ui-react/hooks/useHover.ts`

## Commands and Results

| Command | Result | Log |
| --- | --- | --- |
| `rg --files src/BlazorBaseUI/Popover tests/BlazorBaseUI.Tests/Popover .base-ui/packages/react/src/popover` | Source and test inventory completed. | Console inspection |
| `.base-ui/node_modules/.bin/terser src/BlazorBaseUI/wwwroot/blazor-baseui-popover.js --compress --mangle --module --output src/BlazorBaseUI/wwwroot/blazor-baseui-popover.min.js` | Popover JS regenerated. | `docs/audits/logs/popover-js-minify.log` |
| `.base-ui/node_modules/.bin/terser src/BlazorBaseUI/wwwroot/blazor-baseui-floating.js --compress --mangle --module --output src/BlazorBaseUI/wwwroot/blazor-baseui-floating.min.js` | Floating JS regenerated. | `docs/audits/logs/popover-js-minify.log` |
| `node --check src/BlazorBaseUI/wwwroot/blazor-baseui-popover.js` | GREEN: no syntax errors. | `docs/audits/logs/popover-js-syntax.log` |
| `node --check src/BlazorBaseUI/wwwroot/blazor-baseui-popover.min.js` | GREEN: no syntax errors. | `docs/audits/logs/popover-min-js-syntax.log` |
| `node --check src/BlazorBaseUI/wwwroot/blazor-baseui-floating.js` | GREEN: no syntax errors. | `docs/audits/logs/popover-floating-js-syntax.log` |
| `node --check src/BlazorBaseUI/wwwroot/blazor-baseui-floating.min.js` | GREEN: no syntax errors. | `docs/audits/logs/popover-floating-min-js-syntax.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~Popover" -v minimal` | GREEN: 118 passed. | `docs/audits/logs/popover-bunit.log` |
| `dotnet build BlazorBaseUI.slnx -v minimal` | GREEN: build succeeded, 0 warnings, 0 errors. | `docs/audits/logs/popover-build.log` |
| `bash scripts/lint-rules.sh` | GREEN: 0 violations. | `docs/audits/logs/popover-lint.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~Popover" -v minimal` | GREEN: 152 passed. | `docs/audits/logs/popover-playwright.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~PopoverTestsServer" -v minimal` | GREEN: 76 passed. | `docs/audits/logs/popover-playwright-server.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~PopoverTestsWasm" -v minimal` | GREEN: 76 passed. | `docs/audits/logs/popover-playwright-wasm.log` |
| Browser automation against `http://127.0.0.1:5130/tests/popover/server?modal=true&showBackdrop=true` | GREEN: trigger opens modal; popup, explicit backdrop, internal inert backdrop, and state counters are correct; no console errors. | `docs/audits/logs/popover-browser-modal-probe.log` |
| `pnpm -C .base-ui docs:dev` | GREEN: source docs started on `http://localhost:3005` and Next.js reported ready. | `docs/audits/logs/popover-source-docs-dev.log` |
| In-app browser inspection of `http://127.0.0.1:3005/react/components/popover` | GREEN: `Popover · Base UI` loaded with all documented sections and no page errors. | `docs/audits/logs/popover-source-docs-browser.log` |
| `pnpm -C .base-ui docs:validate "(docs)/react/components/popover"` | GREEN: processed Popover docs and generated types with no updates required. | `docs/audits/logs/popover-source-docs-validate.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~BlazorBaseUI.Tests.Tooltip\|FullyQualifiedName~BlazorBaseUI.Tests.PreviewCard" -v minimal` | GREEN: 171 passed. | `docs/audits/logs/popover-direct-consumers-bunit.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~BlazorBaseUI.Playwright.Tests.Tests.Tooltip\|FullyQualifiedName~BlazorBaseUI.Playwright.Tests.Tests.PreviewCard" -v minimal` | GREEN: 124 passed. | `docs/audits/logs/popover-direct-consumers-playwright.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~Tooltip\|FullyQualifiedName~PreviewCard\|FullyQualifiedName~Select\|FullyQualifiedName~Toolbar\|FullyQualifiedName~Tabs\|FullyQualifiedName~NavigationMenu" -v minimal` | RED exploratory run: invalid consumer verdict because the filter selected broad non-consumer suites and substring matches; retained for traceability only. | `docs/audits/logs/popover-consumers-bunit.log` |

## Playwright Coverage

Final combined Popover Playwright suite:

- Total tests: 152
- Passed: 152
- Failed: 0
- Skipped: 0

Final Server Popover Playwright suite:

- Total tests: 76
- Passed: 76
- Failed: 0
- Skipped: 0

Final WASM Popover Playwright suite:

- Total tests: 76
- Passed: 76
- Failed: 0
- Skipped: 0

## Resolved Gaps

| Gap | React source behavior | Repair | Verification |
| --- | --- | --- | --- |
| Trigger registry did not fully track active trigger element, focus target, and payload. | Popover store keeps active trigger element and payload synchronized for multi-trigger content and anchoring. | Added trigger registration APIs to `PopoverRootContext`; root now resolves active payload, active trigger element, and trigger focus target by id. | bUnit trigger/root tests, multi-trigger Playwright tests. |
| `defaultOpen` did not hydrate JS root state and trigger attributes. | React store initializes open state before interactions and exposes the same trigger attributes. | Added JS root hydration and trigger attribute synchronization for default-open state. | bUnit root tests, Playwright default-open cases. |
| Trigger lacked React's click trigger marker. | `PopoverTrigger` emits the internal click trigger identifier used by floating focus and outside-press logic. | Added `data-base-ui-click-trigger=""` with lint suppression. | bUnit trigger attribute test, Playwright focus/outside-press cases. |
| Patient-click and impatient-click behavior diverged. | React `useClick` distinguishes quick click after hover from patient click using a 500ms threshold. | Added JS patient-click threshold handling, hover re-entry stickiness, same-trigger press handling, and semantic Playwright waits. | Full Popover Playwright suite, focused hover-click logs. |
| Hover-open modal showed modal backdrop too early and could close from hover leave after press. | Hover-open popovers do not engage modal backdrop until the click interaction occurs. | Backdrop render gates now include open reason; hover close after trigger press is ignored. | `Modal_HoverOpen_ClickEngagesModal`, backdrop tests. |
| Portal did not mirror React `FloatingPortal` focus guard semantics. | React portal renders outside guards for non-modal portals and focus manager renders inside guards. | `PopoverPortal` now uses `FloatingPortal`, forwards render and attributes, and renders outside guards according to root focus-manager modal state. | bUnit viewport tests, Playwright tab/focus tests. |
| Popup focus attributes and focus targets were incomplete. | React popup renders `role="dialog"`, focusable popup props, labels/descriptions, initial focus, final focus, restore focus, and previous/next focus targets. | Added `data-base-ui-focusable`, complete initial/final focus mode sync, trigger focus target wiring, and popup restore focus behavior. | bUnit popup tests, focus Playwright tests. |
| Floating focus manager did not have JS-friendly tree context and descendant popup focus handling. | React floating tree allows nested floating descendants and avoids treating their focus as an outside focus. | Added JS-friendly node context synchronization, descendant floating target detection, and deferred pointer-down cleanup. | Nested and focus-out Playwright tests. |
| Nested portaled popovers closed the parent or left stale child state incorrectly. | React treats descendant floating roots as owned by the parent and closes descendants when the parent closes. | Added root descendant detection, owned-element checks, same-pointerdown outside-press suppression, and descendant close cleanup. | Nested Playwright tests, browser WASM regression probe. |
| Positioner stayed anchored to stale trigger after multi-trigger swaps. | React positioning context updates to the active trigger element. | Positioner root state now stores positioner id, updates active trigger element, and calls floating `updatePositioner`; floating JS now resets auto-update when trigger changes. | Multi-trigger positioning and viewport Playwright tests. |
| Modal inert backdrop had incomplete Base UI marker parity. | React modal path relies on inert guards and Base UI marker selectors. | Internal backdrop now renders both `data-base-ui-inert` and `data-blazor-base-ui-inert` through a valid attribute dictionary. | Browser modal probe, modal Playwright tests, lint/build. |
| Positioner initialization could throw after portal teardown. | React effects clean up safely during unmount. | `PopoverPositioner` now handles `ObjectDisposedException` in async initialization/disposal paths. | Full Playwright Server/WASM suite. |
| Viewport tests assumed viewport was first popup child. | React focus manager can render inside focus guards before popup content. | bUnit viewport tests now locate the viewport through the `[data-current]` transition wrapper. | Focused bUnit Popover suite. |

## Files Changed

- `src/BlazorBaseUI/FloatingFocusManager/FloatingFocusManager.razor`
- `src/BlazorBaseUI/FloatingTree/FloatingNode.razor`
- `src/BlazorBaseUI/Popover/PopoverBackdrop.razor`
- `src/BlazorBaseUI/Popover/PopoverPopup.razor`
- `src/BlazorBaseUI/Popover/PopoverPortal.razor`
- `src/BlazorBaseUI/Popover/PopoverPositioner.razor`
- `src/BlazorBaseUI/Popover/PopoverRoot.razor`
- `src/BlazorBaseUI/Popover/PopoverRootContext.cs`
- `src/BlazorBaseUI/Popover/PopoverTypedTrigger.razor`
- `src/BlazorBaseUI/Portal/FloatingPortal.razor`
- `src/BlazorBaseUI/wwwroot/blazor-baseui-floating.js`
- `src/BlazorBaseUI/wwwroot/blazor-baseui-floating.min.js`
- `src/BlazorBaseUI/wwwroot/blazor-baseui-popover.js`
- `src/BlazorBaseUI/wwwroot/blazor-baseui-popover.min.js`
- `tests/BlazorBaseUI.Tests/Infrastructure/JsInteropSetup.cs`
- `tests/BlazorBaseUI.Tests/Popover/PopoverPopupTests.cs`
- `tests/BlazorBaseUI.Tests/Popover/PopoverRootTests.cs`
- `tests/BlazorBaseUI.Tests/Popover/PopoverTriggerTests.cs`
- `tests/BlazorBaseUI.Tests/Popover/PopoverViewportTests.cs`
- `tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/Tests/Popover/PopoverTestsBase.cs`
- `../base-ui-specs/popover/SPEC.md`
- `../base-ui-specs/popover/pitfalls.md`
- `docs/audits/popover-functional-audit.md`
- `docs/audits/popover-parity-matrix.md`
- `docs/audits/logs/popover-*.log`

## Conclusion

The Popover port now matches the audited React Base UI Popover behavior for trigger state, payload routing, attributes, portal/focus guards, hover/click timing, nested floating roots, modal inertness, focus lifecycle, viewport transitions, and active-trigger positioning. DOM-heavy behavior remains in component-specific JavaScript and shared floating JavaScript. Blazor state propagation uses native lifecycle methods, context callbacks, and JS interop updates rather than React state-sync loops. No known Popover parity gap remains from the audited source.

The source documentation comparison found no additional implementation gaps. Direct source-level consumers of Popover types, Tooltip and PreviewCard, pass targeted bUnit and Playwright suites.
