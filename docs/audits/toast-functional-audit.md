# Toast Functional Audit

Date: 2026-06-02

## Scope

Audited the Blazor Toast port against React Base UI source under `.base-ui/packages/react/src/toast/`.

React files/components accounted for:

- `provider/ToastProvider.tsx`
- `viewport/ToastViewport.tsx`
- `root/ToastRoot.tsx`
- `content/ToastContent.tsx`
- `title/ToastTitle.tsx`
- `description/ToastDescription.tsx`
- `close/ToastClose.tsx`
- `action/ToastAction.tsx`
- `portal/ToastPortal.tsx`
- `positioner/ToastPositioner.tsx`
- `arrow/ToastArrow.tsx`
- `createToastManager.ts`
- `useToastManager.ts`
- `store.ts`
- `utils/focusVisible.ts`
- `utils/resolvePromiseOptions.ts`

No Toast spec existed before this audit. Added:

- `../base-ui-specs/toast/SPEC.md`
- `../base-ui-specs/toast/pitfalls.md`

Serena and context7 were requested by workspace instructions but were not exposed as callable tools in this session. Fallback was local React source, repository code search, official ASP.NET Core/Blazor guidance, and repository analyzers.

## Resolved Gaps

- Added the complete `BlazorBaseUI.Toast` component family.
- Added `ToastProvider`, provider store, manager context, and external `ToastManager`.
- Implemented add, update, close-one, close-all, duplicate-id upsert, limit marking, timers, pause/resume, promise loading/success/error, on-close, and on-remove flows.
- Added EventCallback equivalents for close/remove callbacks so store-originated lifecycle callbacks can trigger Blazor rendering.
- Added all audited Toast part attributes, ARIA attributes, state data attributes, and CSS variables.
- Added high-priority `alertdialog` behavior and hidden live-region announcement mirror.
- Added global `F6`, focus guard routing, window focus/blur pause/resume, focus restoration after close, and viewport focus expansion.
- Added component-specific JS file `blazor-baseui-toast.js` plus import target `blazor-baseui-toast.min.js`.
- Kept DOM-heavy behavior in JS: focus event wiring, height observation, transition/animation completion, swipe gesture handling, and floating position updates.
- Added action attribute precedence matching React: element props first, `toast.actionProps` second, internal button behavior last.
- Added disabled action/close suppression for non-native buttons.
- Added anchored positioner and arrow state/attributes.
- Added bUnit and Playwright coverage for Toast behavior in Server and WASM modes.

## Parity Matrix

| React source | Blazor equivalent | Verification |
| --- | --- | --- |
| `ToastProvider` default timeout `5000`, limit `3`, external manager subscription | `ToastProvider.razor`, `ToastStore`, `ToastManagerContext` | bUnit manager/add/limit tests; build analyzers |
| `createToastManager.add` id generation and event emission | `ToastManager.Add` | bUnit duplicate id/upsert and manager tests |
| `createToastManager.close` one/all | `ToastManager.Close`, `ToastStore.CloseToast` | bUnit close test; Playwright close tests |
| `createToastManager.update` | `ToastManager.Update`, `ToastStore.UpdateToast` | bUnit update/type assertions |
| `createToastManager.promise` | `ToastManager.Promise`, `ToastStore.PromiseToast`, `ToastPromiseOption<T>` | bUnit promise success/error; Playwright promise success |
| `useToastManager` in-tree API | `ToastManagerContext` cascaded by `ToastProvider` | Playwright test pages render provider context toasts |
| `store.ts` newest-first list | `ToastStore.toasts` newest-first insertion | bUnit limit order assertions |
| `store.ts` duplicate id upsert/update key | `ToastStore.AddToast` existing-id update path | `DuplicateIdUpsertsExistingToastAndRefreshesUpdateKey` |
| `store.ts` limit marking | `ToastStore.ApplyLimit` | bUnit and Playwright limit assertions |
| `store.ts` timers pause/resume | `ToastStore.PauseTimers`, `ResumeTimers`; viewport/root JS callbacks | Playwright focus/swipe tests; code audit |
| `store.ts` loading has no timer | `ToastStore` skips timers for `type == "loading"` | Promise tests |
| `store.ts` close transition status, height zero, callbacks | `ToastStore.CloseToast` | bUnit close lifecycle; Playwright close counters |
| `store.ts` remove callbacks and limit recompute | `ToastStore.RemoveToast` | Playwright removed-count tests |
| `focusVisible.ts` | JS `matchesFocusVisible` | In-app browser F6 focus check |
| `useRenderElement` | Shared `RenderElement<TState>` | All Toast parts render through `RenderElement` except portal wrapper |
| `useButton` in close/action | `AccessibilityUtilities` plus disabled click suppression | bUnit non-native disabled action; Playwright disabled action |
| `useOpenChangeComplete` | JS transition/animation listeners and no-transition completion | Playwright close/remove and swipe remove tests |
| Root height `useIsoLayoutEffect` | JS `ResizeObserver`/`MutationObserver` plus `ToastStore.UpdateToastHeight` | Build and browser tests; manual DOM check |
| Root swipe state machine | JS pointer capture, damping, axis lock, reverse cancel, CSS variables | `SwipeDismissesToastAndPublishesSwipeAttributes` |
| Viewport global `F6` | JS window keydown and `ToastViewport.OnGlobalFocusHotkey` | Playwright F6 test; in-app browser check |
| Viewport focus guard | `FocusGuard` plus JS focus routing | Playwright F6/focus assertions |
| Viewport window blur/focus | JS window listeners and store pause/resume | Code audit; included in parity spec |
| High-priority live region | `ToastViewport` visually hidden mirror | bUnit high-priority attribute test |
| `ToastContent` data attrs | `ToastContent.razor` | bUnit part attributes |
| `ToastTitle` render-if-content and id registration | `ToastTitle.razor` | bUnit `aria-labelledby`/title rendering |
| `ToastDescription` render-if-content and id registration | `ToastDescription.razor` | bUnit `aria-describedby`/description rendering |
| `ToastClose` close behavior | `ToastClose.razor` | bUnit user click; Playwright close |
| `ToastAction` action props and render-if-content | `ToastAction.razor`, `ToastActionOptions` | bUnit action attrs; Playwright disabled action |
| `ToastPortal` | `ToastPortal.razor` wrapping existing `Portal` | Build/analyzer validation |
| `ToastPositioner` floating metadata | `ToastPositioner.razor`, `PositionerInterop`, JS floating bridge | bUnit positioner/arrow attrs |
| `ToastArrow` metadata | `ToastArrow.razor` | bUnit arrow attrs |

## Attribute Matrix

| Part | React attributes | Blazor status |
| --- | --- | --- |
| Viewport | `tabIndex`, `role`, `aria-live`, `aria-atomic`, `aria-relevant`, `aria-label`, `data-expanded`, `--toast-frontmost-height` | Implemented and tested |
| Root | `role`, `tabIndex`, `aria-modal`, `aria-labelledby`, `aria-describedby`, `aria-hidden`, `inert`, `data-expanded`, `data-limited`, `data-type`, `data-swiping`, `data-swipe-direction`, `data-starting-style`, `data-ending-style`, root CSS vars | Implemented and tested |
| Content | `data-expanded`, `data-behind` | Implemented and tested |
| Title | `id`, `data-type` | Implemented and tested |
| Description | `id`, `data-type` | Implemented and tested |
| Close | native/non-native button attrs, `aria-hidden`, `data-type`, click close | Implemented and tested |
| Action | native/non-native button attrs, `data-type`, action children/attrs/click | Implemented and tested |
| Positioner | `role`, `data-side`, `data-align`, `data-anchor-hidden`, `--toast-index` | Implemented and tested |
| Arrow | `aria-hidden`, `data-side`, `data-align`, `data-uncentered` | Implemented and tested |

## Verification Commands

Persistent logs:

- `docs/audits/logs/toast-build.log`
- `docs/audits/logs/toast-lint.log`
- `docs/audits/logs/toast-bunit.log`
- `docs/audits/logs/toast-playwright.log`
- `docs/audits/logs/toast-in-app-browser.log`
- `docs/audits/logs/toast-in-app-browser.jpg`
- `docs/audits/logs/toast-git-diff-check.log`
- `docs/audits/logs/toast-js-syntax-check.log`
- `docs/audits/logs/toast-source-docs-security.log`
- `docs/audits/logs/toast-demo-rapid-browser.log`
- `docs/audits/logs/toast-demo-rapid-browser.png`

Commands and results:

| Command | Result |
| --- | --- |
| `dotnet build BlazorBaseUI.slnx` | Passed. 0 warnings, 0 errors. |
| `bash scripts/lint-rules.sh` | Passed. 0 violations. |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~ToastTests" -v minimal` | Passed. 7/7. |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~Toast" -v minimal` | Passed. 14/14. |
| `git diff --check` | Passed. No whitespace errors. |
| `node --check src/BlazorBaseUI/wwwroot/blazor-baseui-toast.js && node --check src/BlazorBaseUI/wwwroot/blazor-baseui-toast.min.js` | Passed. No JS syntax errors. |
| In-app browser manual check against `http://127.0.0.1:5118/tests/toast/server` | Passed. Logged low toast attributes, high-priority F6 focus expansion, close/remove counts, and swipe dismissal counts. |

Diagnostic note: one non-final parallel `dotnet test` invocation failed before tests executed because two .NET commands wrote the same static web assets cache file concurrently. The command was rerun sequentially and passed.

## Source Docs Demo Follow-Up

Date: 2026-06-04

The React Base UI docs workspace was checked with pnpm before attempting to run the docs page.

| Command | Result |
| --- | --- |
| `pnpm --version` in `.base-ui` | `10.33.4` |
| `pnpm audit --audit-level moderate --json` in `.base-ui` | Failed with 4 low, 23 moderate, 21 high, and 1 critical advisory. |

The React docs server was not started because the audit identified a direct vulnerable `docs>next` runtime chain and a critical `@vitest/browser` advisory in the workspace. Source docs comparison was therefore performed from checked-in React docs demo files only.

React docs demo files checked:

- `.base-ui/docs/src/app/(docs)/react/components/toast/demos/hero/tailwind/index.tsx`
- `.base-ui/docs/src/app/(docs)/react/components/toast/demos/anchored/tailwind/index.tsx`
- `.base-ui/docs/src/app/(docs)/react/components/toast/demos/position/tailwind/index.tsx`

Demo parity gaps resolved:

- Added keyed Blazor render identity for rapidly inserted toast roots and anchored positioners to match React `key={toast.id}` list rendering.
- Disabled the anchored copy trigger while the anchored toast is active, matching the source demo's disabled copied state.
- Switched the top-center viewport swipe direction to `up`; bottom-end remains `down` and `right`.
- Replaced the demo stack CSS with Base UI's collapsed scale/peek model using `--gap`, `--peek`, `--scale`, `--shrink`, `--height`, and `--offset-y`.

Rapid summon verification against `http://127.0.0.1:5120/toast` passed with no displayed Blazor error UI, no new browser error logs, no stuck `data-starting-style` roots, a single anchored toast under rapid clicking, and correct collapsed stack transforms in bottom and top viewport modes.

## In-App Browser Check

Manual browser log:

```text
url=http://127.0.0.1:5118/tests/toast/server
add-low button: count=1
low-role=dialog
low-type=info
viewport-role=region
closed-after-close=1
removed-after-close=1
high-role=alertdialog
high-aria-hidden-before=true
focus-active=toast-viewport
viewport-expanded=true
high-aria-hidden-after=null
closed-after-swipe=2
removed-after-swipe=2
```

## Final Status

Toast functional parity is implemented and verified for the audited React source surface. The component is staged-ready and not committed.
