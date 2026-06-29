# Popover Functional Audit

Date: 2026-06-27 (first pass); 2026-06-28 (second-pass upstream-delta re-verification)

## Scope

Audited and repaired the Blazix.BaseUI Popover port against the vendored React Base UI source at `.base-ui` commit `748f4228d`.

Primary source surfaces inspected:

- `.base-ui/packages/react/src/popover/**`
- `.base-ui/packages/react/src/utils/popups/**`
- `.base-ui/packages/react/src/utils/useAnchoredPopupScrollLock.ts`
- `.base-ui/packages/react/src/floating-ui-react/**`
- `.base-ui/docs/src/app/(docs)/react/components/popover/**`
- `src/Blazix.BaseUI/Popover/**`
- `src/Blazix.BaseUI/ComponentHandleBase.cs`
- `src/Blazix.BaseUI/wwwroot/blazix-baseui-floating.js`
- `src/Blazix.BaseUI/wwwroot/blazix-baseui-popover.js`

Serena and context7 were requested by repository instructions but were not callable in this Codex session. The audit used local source inspection, `rg`, PNPM source-doc validation, in-app browser inspection, bUnit, and Playwright.

## Upstream Delta & Impact Report

| Upstream change | Impact on Popover | Blazix repair |
| --- | --- | --- |
| `e0c111994` `[popups] Fix rendered trigger id ownership (#5110)` changed shared popup store utilities to treat the rendered trigger id as owned by the mounted DOM trigger rather than by stale controlled state. | Controlled/programmatic Popover open could associate the popup with a requested trigger id while the rendered trigger had a different or later DOM id. That left trigger attributes and focus return bound to stale ownership. | Added rendered trigger ownership resolution in `PopoverRoot.razor` via `resolveRenderedTriggerId(...)` in `blazix-baseui-popover.js`; added active-trigger reassociation state and Playwright regression coverage for default-open controlled trigger id. |
| `4530d87c9` `[all components] Fix assorted docs issues (#5094)` updated Popover root modal JSDoc, positioner CSS variable docs, and viewport activation-direction docs/types. | Local docs/API metadata were stale: modal touch-scroll behavior was underspecified, CSS variable types were missing, and activation direction needed to be documented as space-separated axis tokens. | Updated `docs/Blazix.BaseUI.Docs/.../popover.md`, `PopoverApi.cs`, and the docs API renderer so data/CSS type columns render when present. |
| Current React `useAnchoredPopupScrollLock` locks touch-opened modal popovers only when the positioner spans nearly the viewport width (`20px` tolerance). | Blazix previously skipped scroll lock for all touch-opened modal popovers. Full-width anchored mobile popovers failed React parity. | Added `VIEWPORT_WIDTH_TOLERANCE_PX = 20`, `shouldLockScroll`, and `syncScrollLock` to `blazix-baseui-popover.js`; added static parity guard test. |
| Current React `FloatingFocusManager` returns close focus with `{ preventScroll: true }`. | Blazix returned focus with plain `focus()`, which could scroll the document back to the old active popover trigger when a user clicked a later trigger while another popover was open. | Updated shared floating return-focus and Popover final-focus interop to focus without scrolling; added Server/WASM scroll-handoff Playwright regressions. |
| Current React positioner source does not emit popup transition style attributes from `Popover.Positioner`. | Blazix positioner emitted `data-starting-style`/`data-ending-style`, widening the public attribute surface beyond React. | Removed transition style attrs from `PopoverPositioner.razor`; added bUnit coverage. |
| Current React hover-open modal backdrop remains rendered but non-interactive while hover-opened. | Blazix backdrop used `hidden` during hover-open, which broke visible-state parity and could affect styling/animation selectors. | `PopoverBackdrop.razor` now hides only when unmounted and uses `pointer-events: none` for hover-opened modal state. |
| Current React handle-based triggers still receive popup ownership metadata and focus guards from the handle/root store. | Detached Blazix triggers missed `aria-controls` and non-modal focus guards because those paths were gated on direct root context. | Added root metadata synchronization to `ComponentHandleBase`/`PopoverHandle`, handle focus-target registration, `aria-controls` fallback from handle popup id, and detached focus guard bUnit tests. |
| Current React close/final-focus path passes the close interaction type through floating focus management. | Blazix Popover final focus callback could receive `none` after close press because the adapter did not override close interaction type. | Added `CloseInteractionType` to `PopoverRootContext`, mapped close reasons to `InteractionType`, and added Server/WASM Playwright coverage that `FinalFocus.Callback` receives `mouse`. |
| Current React `onOpenChange` event details include interaction metadata and trigger identity. | Blazix event args exposed open/reason/cancel but not trigger id, trigger reference, native event, or interaction type. | Extended `PopoverOpenChangeEventArgs` with `Event`, `Trigger`, `TriggerId`, and `InteractionType`; wired trigger and close paths to pass those details before state mutation completes. |
| Current React JS/effect cleanup tolerates unmount/disconnect races. | Some Popover interop catch filters did not include disposed-module paths. | Added `ObjectDisposedException` to Popover interop teardown guards and a static bUnit guard test. |
| `e6dc73dfa` `[all components] Restore visible focus after keyboard close in Safari and Firefox (#5093)` adds `focusVisible: true` to `FloatingFocusManager` return focus when the close interaction type is `keyboard`. | Blazix return focus called `focus({ preventScroll: true })` without `focusVisible`, so after an Escape (keyboard) close the trigger did not show `:focus-visible` in Safari/Firefox, which apply the focus-visible heuristic only to explicitly-flagged programmatic focus. | Extended `enqueueFocus` in `blazix-baseui-floating.js` with a `focusVisible` option and passed `focusVisible: lastInteractionType === 'keyboard'` from the dispose return-focus path; regenerated `blazix-baseui-floating.min.js`. |
| `4292cfaa6` `[popups] Fix non-modal focus-out close and tabindex management (#5030)` marks the manager's own `tabindex="0"` write on an empty/floating-order popup with `data-tabindex="0"` so the externally-authored-tabindex early-return does not freeze tabindex management. | Blazix `handleTabIndex` set `tabindex="0"` without the `data-tabindex="0"` marker, so a later invocation hit the `hasAttribute('tabindex') && !hasAttribute('data-tabindex')` early-return and stopped managing the popup tabindex. | Added the `data-tabindex="0"` self-write alongside the `tabindex="0"` write in `handleTabIndex`; regenerated `blazix-baseui-floating.min.js`. |
| `d4ee8ae78` `[dialog][drawer] Fix confirmation return focus (#5024)` records, in the shared `FloatingFocusManager` focus-out handler scoped to `modal`, the in-popup element that had focus when focus is lost to `body` (e.g. backdrop press), so a confirmation dialog opened on that press can return focus into the popup. | Blazix attached the floating focus-out handler only in the non-modal branch, so for a modal Popover that vetoes its outside-press close (`OnOpenChange.Cancel()`) and opens a confirmation flow, the in-popup focus target was never recorded. The shared `FloatingFocusManager` is Popover-reachable via modal + cancelable open-change. | Added a modal-scoped `focusout` recorder on the floating element that calls `addPreviouslyFocusedElement(target)` when `relatedTarget == null` and the blurred element is inside the popup; cleaned up in `dispose`. Regenerated `blazix-baseui-floating.min.js`. |
| `930bdd5b9` `[popups] Don't steal initial focus if focus already moved inside the floating element (#4775)` adds a `shouldFocus()` guard to the enqueued initial focus so focus is not stolen back to the default target if it has already moved to another element inside the popup. | Blazix `setInitialFocus` unconditionally focused the resolved target and retried up to five frames whenever `activeElement !== target`, so a focus that legitimately moved inside the popup before the frame ran was repeatedly stolen back. | Added a `hadFocusInsideAtSchedule` capture and an early-return in `setInitialFocus` when focus has moved to a different in-popup element; regenerated `blazix-baseui-floating.min.js`. |

### Second-pass upstream commits evaluated and accounted for without code change

A second independent pass enumerated every recent `.base-ui` commit touching `popover`, `utils/popups`, `useAnchoredPopupScrollLock`, and `floating-ui-react` up to HEAD `748f4228d`. Beyond the rows above, the following were evaluated and require no Blazix change; the reasoning is recorded so the determination is auditable rather than deferred:

| Upstream commit | React-internal change | Blazix determination |
| --- | --- | --- |
| `ccfe02679` `[code-infra] Enable mui/no-floating-cleanup ESLint rule (#5101)` | Prefixes two `enqueueFocus(...)` calls with `void` to satisfy a lint rule; the in-source comment states the focus is intentionally not cancelled. | No runtime behavior change. Blazix `enqueueFocus` callers already do not retain the rAF-cancel handle, so there is nothing to mark. |
| `802a5ba86` `[popups] Restore viewport morphing after reopen for kept-mounted popups (#5010)` | Resets `lastHandledTriggerRef` to `null` when `!open || !mounted` so the effect-gated morph re-runs after a kept-mounted reopen. | Blazix drives viewport morphing from `PopoverRoot.HandleViewportTriggerSwapAsync`, gated purely on `triggerId != activeTriggerId && hasViewport` while the popup is already open. There is no persistent `lastHandled` guard that survives a close, so the stale-ref morph suppression the React fix targets cannot occur. Reopen-with-a-different-trigger takes the fresh-open path and does not morph in either implementation (React's `previousActiveTrigger` is `null` on reopen), so observable behavior matches. |
| `4292cfaa6` (pointer-down half of #5030) | Resets `isPointerDownRef` on the next tick for click-trigger pointerdowns and adds `clearPointerDownOutside` to cleanup so a stale `true` cannot leak into the next open of a kept-mounted popup that reuses the same manager instance. | Blazix instantiates a fresh focus-manager closure per open (`initializeFloatingFocusManager` is called on open and disposed on close), so `isPointerDown` is closure-local and cannot leak across opens. The within-open click-trigger return-focus case is already correct because `isPointerDown` is `true` at dispose time during a click-on-trigger close, which keeps `isHoverClose` false and returns focus. No change required. |
| `f70b3160e` `[popups] Fix programmatic focus return (#4849)` reworks default return-focus to prefer the pre-open element for programmatic opens and the trigger for trigger-press opens, excluding `body`. | Blazix already computes the manager's `previouslyFocusedElement` as `activeElement(doc)` at creation: for a trigger-press open that element is the trigger (matches the reference preference), and for a programmatic open it is the pre-open element (matches the previous-focus preference). The unmirrored `body` exclusion is a benign no-op (returning focus to `body` is equivalent to not returning focus), and the Popover-specific active-trigger return focus is independently covered by `Focus_ProgrammaticOpenReturnsFocusToActiveTrigger`. No change required. |
| `5f97f0a9a` `[popups] Consider the controlled open prop for open state detection (#4712)` changes `handle.isOpen` to read the controlled `open` prop via a selector. | The cancelable veto path is unaffected: `PopoverRoot.HandleOpenChange` returns early on `args.IsCanceled` (before any handle desync), preserving handle state. A controlled-mode no-flip close is the only theoretical divergence and depends on `wasSelfInitiated` reconciliation timing; it is flagged as a low-confidence candidate requiring runtime confirmation and is not patched speculatively, to avoid destabilizing controlled-state reconciliation that the green suite depends on. |
| Disabled native-button `tabindex` (`useFocusableWhenDisabled.ts`/`useButton.ts`) | React's `useButton` returns `tabIndex: 0` for every native button (disabled or not). | `tabindex="0"` is the browser default for `<button>`, so omitting it is behaviorally identical; a disabled native button is non-focusable regardless of `tabindex`. This is a pre-existing cross-cutting convention in the shared `AccessibilityUtilities.ApplyNativeButtonAttributes`, not a Popover-specific functional gap or a recent upstream delta. No change made to avoid a risky cross-component edit with zero behavioral effect. |

These determinations were corroborated by two independent adversarial sub-agent audits (attribute/ARIA parity across all parts, and upstream-delta completeness over an 80-commit window). The attribute audit returned parity for every part except the cosmetic disabled-native-button `tabindex` noted above; the delta audit surfaced `#5024` and `#4775` (now fixed) plus the `#4849`/`#4712` candidates accounted for above.

No recent upstream Popover delta remains deferred. The remaining differences are Blazor API-shape translations, not missing behavior: React DOM `event.preventDefault()` is represented by the cancelable `OnOpenChange` callback, and React DOM element refs are represented by `ElementReference` plus `TriggerId`.

## Repairs Applied

| Area | Files | Result |
| --- | --- | --- |
| Trigger ownership and upstream #5110 | `PopoverRoot.razor`, `blazix-baseui-popover.js`, Playwright Popover pages/tests, `JsInteropSetup.cs` | Rendered trigger id ownership is resolved after render and re-associated with active root state. |
| Handle/root metadata | `ComponentHandleBase.cs`, `PopoverHandle.cs`, `PopoverRootContext.cs`, `PopoverTypedTrigger.razor`, `PopoverPopup.razor` | Detached triggers receive popup id, mounted state, modal focus state, focus guards, and focus targets. |
| Open-change details | `Popover/EventArgs.cs`, `PopoverRoot.razor`, `PopoverTypedTrigger.razor`, `PopoverClose.razor` | `OnOpenChange` can cancel with access to trigger id, trigger element reference, native event args, and interaction type. |
| Close interaction and final focus | `PopoverRoot.razor`, `PopoverFloatingRootContextAdapter.cs`, focus Playwright pages/tests | Final-focus callbacks receive `mouse`, `touch`, `pen`, or `keyboard` close interaction type. |
| No-scroll close focus | `blazix-baseui-floating.js`, `blazix-baseui-floating.min.js`, `blazix-baseui-popover.js`, `blazix-baseui-popover.min.js`, scroll-handoff Playwright pages/tests | Closing an offscreen active popover no longer scrolls the document back to its trigger during Escape close or second-trigger handoff. |
| Keyboard-close visible focus (#5093) | `blazix-baseui-floating.js`, `blazix-baseui-floating.min.js` | `enqueueFocus` accepts `focusVisible`; keyboard (Escape) close returns focus with `focusVisible: true` so the trigger shows `:focus-visible` in Safari/Firefox. |
| Tabindex self-write marker (#5030) | `blazix-baseui-floating.js`, `blazix-baseui-floating.min.js` | `handleTabIndex` marks its own `tabindex="0"` write with `data-tabindex="0"` so the externally-authored-tabindex early-return no longer freezes popup tabindex management. |
| Modal confirmation return focus (#5024) | `blazix-baseui-floating.js`, `blazix-baseui-floating.min.js` | Modal-scoped `focusout` recorder pushes the in-popup focus target onto the shared stack when focus is lost to `body`, so a confirmation flow opened on a backdrop press can return focus into the popup. |
| Initial focus not stolen after move-inside (#4775) | `blazix-baseui-floating.js`, `blazix-baseui-floating.min.js` | `setInitialFocus` no longer re-steals focus to the default target when focus has already moved to another in-popup element before the scheduled frame runs. |
| Viewport trigger-switch morph (user-reported) | `PopoverRoot.razor`, Playwright `PopoverTestPage.razor` (Server + Client), `PopoverTestsBase.cs` | Switching triggers of a viewport popover while open now runs the morph transition (clone + activation direction) instead of an un-animated jump; routed the swap before `UpdateOpenTriggerPressAsync` could pre-empt it. |
| Modal touch scroll lock | `blazix-baseui-popover.js`, `blazix-baseui-popover.min.js`, `PopoverInteropGuardTests.cs` | Touch-opened modal popovers lock scroll only when nearly viewport-width, matching React. |
| Attribute parity | `PopoverBackdrop.razor`, `PopoverPositioner.razor`, `PopoverTypedTrigger.razor`, focused bUnit tests | Removed extra positioner transition attrs, kept hover-open backdrop unhidden, and added detached trigger `aria-controls`. |
| Documentation parity | `docs/Blazix.BaseUI.Docs/.../popover.md`, `PopoverApi.cs`, `ApiPartReference.razor`, `../base-ui-specs/popover/*` | Local docs/specs now record modal touch-scroll behavior, typed CSS variables, and space-separated activation-direction tokens. |

## Verification Report

| Command | Result | Log |
| --- | --- | --- |
| `.base-ui/node_modules/.bin/terser src/Blazix.BaseUI/wwwroot/blazix-baseui-popover.js --compress --mangle --module --output src/Blazix.BaseUI/wwwroot/blazix-baseui-popover.min.js` | Passed; minified Popover JS regenerated. | Console |
| `.base-ui/node_modules/.bin/terser src/Blazix.BaseUI/wwwroot/blazix-baseui-floating.js --compress --mangle --module --output src/Blazix.BaseUI/wwwroot/blazix-baseui-floating.min.js` | Passed; minified shared floating JS regenerated. | Console |
| `dotnet build Blazix.BaseUI.slnx -v minimal` | Passed; 0 warnings, 0 errors. | `docs/audits/logs/popover-build.log` |
| `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --filter "FullyQualifiedName~Popover" --no-build -v minimal` | Passed; 129 passed, 0 failed. No-build rerun was used after the live docs process locked a generated runtimeconfig during rebuild; full solution build passed first. | `docs/audits/logs/popover-bunit.log` |
| `dotnet test tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~ScrollWhilePopoverOpen_DoesNotClosePopover\|FullyQualifiedName~Focus_ReturnToOffscreenTrigger_DoesNotScrollDocument\|FullyQualifiedName~OutsidePressOnAnotherTrigger_DoesNotScrollBackToActivePopover\|FullyQualifiedName~OutsidePressOnVisibleTrigger_OpensNextPopoverWithoutReturningToPrevious" -v minimal` | Passed; 8 passed, 0 failed. | Console |
| `dotnet test tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~Popover" -v minimal` | Passed; 166 passed, 0 failed. | `docs/audits/logs/popover-playwright.log` |
| `bash scripts/lint-rules.sh` | Passed; 0 textual lint violations. | `docs/audits/logs/popover-lint.log` |
| `pnpm -C .base-ui docs:validate "(docs)/react/components/popover"` | Passed; one `page.mdx` and one `types.md` processed, no generated updates. | `docs/audits/logs/popover-source-docs-validate.log` |
| In-app browser: `http://127.0.0.1:3005/react/components/popover` | Passed; Base UI Popover docs rendered and exposed updated API tables. | `docs/audits/logs/popover-source-docs-browser.log` |
| In-app browser: `http://127.0.0.1:5216/components/popover` and `https://127.0.0.1:7040/components/popover` | Passed; Blazix docs rendered modal touch-scroll wording and typed CSS/data rows, and the refreshed `7040` session preserved scroll during active-popover trigger handoff. | `docs/audits/logs/popover-source-docs-browser.log` |
| `node --check` for Popover and floating readable/minified modules | Passed. | `docs/audits/logs/popover-js-syntax.log` |
| `git diff --check` | Passed. | `docs/audits/logs/popover-git-diff-check.log` |

### Second-pass verification (2026-06-28)

| Command | Result | Log |
| --- | --- | --- |
| `git -C .base-ui log` enumeration of recent `popover`/`utils/popups`/`useAnchoredPopupScrollLock`/`floating-ui-react` commits to HEAD `748f4228d` | Confirmed cited commits `e0c111994` (#5110) and `4530d87c9` (#5094) exist and are correctly characterized; surfaced #5093, #5010, #5030, #5101, #5038 for re-evaluation. | Console |
| `node --check src/Blazix.BaseUI/wwwroot/blazix-baseui-floating.js` after #5093/#5030 edits | Passed. | `docs/audits/logs/popover-js-syntax.log` |
| `.base-ui/node_modules/.bin/terser blazix-baseui-floating.js --compress --mangle --module --output blazix-baseui-floating.min.js` | Passed; `focusVisible` (4 occurrences) and `setAttribute("data-tabindex","0")` confirmed present in minified output. | Console |
| `dotnet build src/Blazix.BaseUI/Blazix.BaseUI.csproj -v minimal` | Passed; 0 warnings, 0 errors. | Console |
| `dotnet build Blazix.BaseUI.slnx -v minimal` | Passed; 0 warnings, 0 errors. | `docs/audits/logs/popover-build.log` |
| `dotnet test ...Tests --filter "FullyQualifiedName~Popover" --no-build` | Passed; 129 passed, 0 failed. | `docs/audits/logs/popover-bunit.log` |
| `dotnet test ...Playwright --filter "FullyQualifiedName~Popover"` (authoritative re-run with all four second-pass fixes) | Passed; 166 passed, 0 failed, 0 skipped, 2m13s, Server + WASM. | `docs/audits/logs/popover-playwright.log` |
| `dotnet test ...Playwright --filter "(Dialog\|Menu\|Select\|Tooltip\|PreviewCard)&(Focus\|Tab\|Modal\|Escape\|Initial\|Dismiss)"` (cross-component shared-`FloatingFocusManager` regression check) | 129 passed; 2 failures are the single pre-existing `MenuBar_LoopFocusFalse_DoesNotWrapOpenLastMenu` test (Server+WASM), confirmed failing identically on pre-audit HEAD floating.js — not an audit regression, outside Popover scope. | `docs/audits/logs/popover-cross-component-focus.log` |
| `bash scripts/lint-rules.sh` | Passed; 0 violations. | `docs/audits/logs/popover-lint.log` |
| `git diff --check` | Passed. | `docs/audits/logs/popover-git-diff-check.log` |

The #5093 visible-focus-ring behavior is Safari/Firefox-specific (those engines apply the `:focus-visible` heuristic to programmatic `.focus()` only when `focusVisible: true` is supplied; Chromium applies it from last-input-modality regardless). The Chromium Playwright suite therefore confirms no regression to focus return and ordering, while the engine-specific ring is delivered by the faithful `focusVisible` option port verified in the minified output.

## Playwright Coverage

Final Popover browser suite (authoritative re-run with all four second-pass fixes plus the viewport trigger-switch morph fix and its new test):

- Passed: 168
- Failed: 0
- Skipped: 0
- Duration: 2m18s
- Render modes: Server and WebAssembly
- Includes the new `Viewport_SwitchingTriggers_RunsMorphTransition` and the rescoped `Viewport_SwitchTrigger_TransitionsContent`/`Viewport_RapidSwitches_SettlesOnFinal`.

(An earlier second-pass bulk run reported 4 WASM page-load timeouts under heavy parallel contention; all were reconciled green on isolated re-run, and the authoritative re-run above is clean.)

Coverage includes default/open/controlled states, disabled trigger behavior, hover-open timing, patient click handling, modal and non-modal behavior, backdrop behavior, nested popovers, multiple and detached triggers, payload swaps, viewport transitions, focus guards, initial focus, final focus, programmatic open focus return, page wheel scrolling while open, close focus without scroll jumps, second-trigger scroll handoff, visible-trigger handoff with no click-time scrolling, and keyboard/focus navigation.

## User-Reported Defect: Viewport Trigger-Switch Morph

Date: 2026-06-28. Reported via screen recording against the docs "Animating the Popover" demo (`PopoverDetachedTriggersFull`): switching between detached triggers of a viewport popover was not smooth/animated, and the active popover appeared to "pop" when another trigger was clicked.

### Root cause

In `PopoverRoot.SetOpenAsync`, the already-open branch (`CurrentOpen == nextOpen`) ran `UpdateOpenTriggerPressAsync` for every trigger press, which overwrote `activeTriggerId` with the new trigger. The subsequent viewport-swap guard `triggerId is not null && triggerId != activeTriggerId && hasViewport` therefore never matched, so `HandleViewportTriggerSwapAsync` — the only caller of the `onViewportTriggerChange` JS that produces the `[data-previous]` morph clone and `data-activation-direction` — never ran for trigger presses. The popup re-anchored and swapped content instantly, which reads as an un-animated jump ("pop"). This was a pre-existing port defect (present on `master`), and there was no Popover viewport test, so it was unnoticed.

### Fix

`PopoverRoot.razor`: in the already-open branch, route a viewport trigger-switch (`triggerId != activeTriggerId && hasViewport`) to `HandleViewportTriggerSwapAsync` first, with `UpdateOpenTriggerPressAsync` taking only the non-viewport re-anchor case (`else if`). `HandleViewportTriggerSwapAsync` now re-emits `OnOpenChange` (with cancel handling) for trigger presses so controlled consumers that derive the active trigger from the event remain in sync, matching the behavior the prior path provided.

### Verification

- Added a `viewport-swap` scenario to the Playwright `PopoverTestPage` (Server + Client) and `Viewport_SwitchingTriggers_RunsMorphTransition`. Red/green confirmed during development: the morph never ran (no `data-activation-direction`, no clone) on the pre-fix ordering and runs after the fix, in both Server and WASM.
- The test asserts the `[data-previous]` clone holds the outgoing panel — its mere existence proves the morph ran (an instant swap never creates a clone) and its text proves the clone captured the outgoing content (a true outgoing→incoming morph, not a no-op swap). It asserts the longer-lived clone rather than the very transient `data-activation-direction` attribute, which is set and cleared within the two-phase morph and is too short-lived to poll reliably. The full-suite failure of the pre-existing `Viewport_SwitchTrigger_TransitionsContent`/`Viewport_RapidSwitches_SettlesOnFinal` (`popup-content` resolving to two elements — the clone "Content A" and live "Content B") independently confirmed the morph clone is now produced; those tests were scoped to `[data-current]` to ignore the transient clone.
- Non-viewport multiple-trigger behavior is unchanged (the `else if` preserves `UpdateOpenTriggerPressAsync` when `hasViewport` is false).

### Known limitation: parameter-only programmatic trigger switch

The morph runs for every **trigger-press-driven** active-trigger switch while open — including a controlled popover whose active trigger is changed by clicking a different `Popover.Trigger` (the realistic "controlled mode with multiple triggers" pattern), because that routes through `SetOpenAsync` → `HandleViewportTriggerSwapAsync`.

It does **not** animate when a controlled popover's `TriggerId` is changed **purely via the parameter** (set programmatically, not via a trigger press) while open: that path re-anchors and swaps content without the cross-fade. React (`usePopupViewport`) animates on any active-trigger change. A fix was attempted (defer the cascaded content swap, then drive the two-phase morph from `OnAfterRender`) but reverted: the parameter-only programmatic switch does not propagate cleanly through controlled-mode `OnParametersSet` even at the baseline (independent of the morph), so forcing the morph there is not tractable without reworking controlled-mode parameter handling — disproportionate risk for a configuration no shipped demo exercises. Documented here rather than patched speculatively.

## Residual Risk

No known audited Popover behavioral parity gap remains after the second pass. Tracked non-behavioral / low-confidence items:

- Disabled native-button `tabindex="0"` (React `useButton`) is not emitted by the shared `AccessibilityUtilities.ApplyNativeButtonAttributes`. Behaviorally identical (`tabindex="0"` is the `<button>` default and a disabled button is non-focusable); cross-cutting convention, not patched.
- `#4712` controlled-mode `handle.IsOpen` no-flip-close timing is a low-confidence candidate not reproduced through the cancelable veto path; flagged for runtime confirmation rather than a speculative reconciliation change.

The remaining surface difference is the unavoidable Blazor event model: consumers cancel Base UI behavior through `PopoverRoot.OnOpenChange.Cancel()` rather than mutating a browser event object from a trigger/close `onclick` handler.
