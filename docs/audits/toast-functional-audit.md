# Toast Functional Audit

Date: 2026-07-21

## Decision

The Toast port was audited against React Base UI commit `bdcb685fadcca9d18b18f013c052795a53b6aa33` and repaired. The audited baseline was `b6ec388df83d3ef3e01bd0505797b63e489480d4`; all 255 intervening commits were screened and every direct or shared Toast-impacting change was mapped below.

The resulting implementation has observable parity for manager/store behavior, timers, limits, promise states, content rendering, viewport focus, keyboard operation, swipe handling, animation completion, anchored positioning, portal targeting, attributes, ARIA, and Server/WASM behavior. No Toast implementation item identified by the source audit remains deferred.

## Source Surface Audited

The audit covered all production files under `.base-ui/packages/react/src/toast/`, including Provider, Viewport, Root, Content, Title, Description, Action, Close, Portal, Positioner, Arrow, store, manager creation, `useToastManager`, promise resolution, focus visibility, and Toast types. It also covered the shared source used by Toast: `FocusGuard`, button keyboard behavior, render-element composition, floating positioning, owner-document/window helpers, event listener cleanup, animation completion, and portal mechanics.

The framework-agnostic specification was refreshed in `../base-ui-specs/toast/SPEC.md` and `../base-ui-specs/toast/pitfalls.md` with every newly confirmed behavior.

## Resolved Functional Gaps

### Store, manager, timers, and limits

- Provider timeout and limit now synchronize through one native Blazor parameter lifecycle path. Limit state is recomputed immediately when either parameter changes.
- Timer execution no longer uses `Task.Run`. Cancellation and continuation remain on the normal asynchronous component path.
- Timer pause state is reset after timer completion, individual clearing, and close-all clearing.
- Closing all toasts clears all timer registrations and interaction state.
- Height updates are deduplicated before store notification.
- Duplicate-ID add/upsert preserves omitted values, applies explicit `null`, increments `UpdateKey`, and does not mutate the caller's options object.
- Promise transitions force `loading`, `success`, and `error` types. Success/error options clear a loading timeout unless that state explicitly supplies one.
- Promise options are cloned before ID resolution; manager calls no longer modify user-owned option instances.
- Limited toasts remain mounted, receive `inert`, and are excluded from focus routing. A zero limit is supported.

### Rendering and attributes

- Title, Description, and Action render when either render content or a render delegate is supplied.
- Renderability matches the source for `null`, booleans, empty strings, empty markup, recursive enumerables, and numeric zero. No `RenderFragment` content is cached.
- Portal render state is strongly typed as `ToastPortalState`.
- Portal targeting supports the selector form and a concrete `ElementReference` container.
- Viewport omits `--toast-frontmost-height` until a non-zero height exists.
- Root update interop is snapshot-deduplicated and does not perform the former first-render update loop.
- Positioner parameters use direct-parameter presence tracking so direct values override manager values field-by-field, including explicit null anchor removal. Anchor removal and tracking changes reset/reinitialize floating state.
- Canonical `data-base-ui-swipe-ignore` is implemented; legacy `data-swipe-ignore` remains accepted for compatibility.

### Focus, keyboard, and accessibility

- Viewport listeners are installed only while toasts exist and are correctly rebound after empty/non-empty cycles.
- Listener ownership uses the viewport's owner document and owner window.
- F6 stores the previous focus element, focuses the viewport without scrolling, pauses timers, and expands the stack.
- The complete three-guard layout matches the source. Guard navigation skips ending and limited toasts and restores prior focus when no eligible toast exists.
- Direct viewport Tab routing is performed in JavaScript, eliminating Blazor Server interop latency from focus order. Shift+Tab restores prior focus and resumes timers.
- Focus after close scans following siblings first, then previous siblings, and otherwise restores the previous focus element.
- Viewport blur resumes timers only when the window is focused. Window focus/blur and document touch interaction update timer state consistently.
- FocusGuard's button-role workaround now uses Apple-platform plus WebKit detection rather than Safari user-agent detection.
- Action and Close use shared non-native button behavior. Enter and Space activation, Space `preventDefault`, disabled suppression, focusability, and listener disposal match `useButton` semantics.
- High-priority announcement mirrors remain keyed, visually hidden, atomic alerts. Root dialog/alertdialog behavior and ARIA relationships remain source-equivalent.

### Swipe, animation, and DOM ownership

- Component JS owns pointer capture, axis lock, damping, threshold/velocity decisions, CSS swipe variables, and `preventDefault` decisions.
- Document pointer-up and pointer-cancel listeners are abortable and are removed after each gesture.
- Touch-move prevention applies only to the active pointer. Ending swipe state is returned to .NET as one batched callback.
- Mouse-move now forwards the correct consumer event. Deferred mouse-leave is flushed only after ending transitions and touch activity settle.
- Root listeners are rebound when an element is replaced and disposed through abortable listener groups.
- Animation completion waits for all root animations from `getAnimations()` and ignores descendant transition events. Removal does not race multi-animation exit styles.
- Resize/Mutation observers are created only when both browser APIs exist, and height callbacks are deduplicated.
- JS-to-.NET callback failures are no longer silently swallowed; expected disposal/disconnect paths remain guarded in C#.

## Attribute and State Audit

| Part | Required observable output | Status |
| --- | --- | --- |
| Provider | timeout, limit, external manager subscription | Verified |
| Viewport | `tabindex=-1`, `role=region`, `aria-live=polite`, `aria-atomic=false`, `aria-relevant`, `aria-label=Notifications`, `data-expanded`, conditional `--toast-frontmost-height` | Verified |
| Root | dialog/alertdialog role, `tabindex`, `aria-modal`, label/description IDs, `aria-hidden`, `inert`, type/expanded/limited/swipe/transition data attributes, index/offset/height/swipe CSS variables | Verified |
| Content | `data-expanded`, `data-behind` | Verified |
| Title | conditional element, `id`, `data-type` | Verified |
| Description | conditional element, `id`, `data-type` | Verified |
| Action | native/non-native button semantics, action attributes/content, `data-type` | Verified |
| Close | native/non-native button semantics, `aria-hidden`, `data-type` | Verified |
| Portal | typed render state, selector container, concrete DOM container | Verified |
| Positioner | role, side/align/anchor-hidden data, floating CSS variables, direct/manager precedence | Verified |
| Arrow | `aria-hidden`, side/align/uncentered data | Verified |

## Upstream Delta & Impact Report

| Commit | Upstream change evaluated | Impact and Blazor implementation |
| --- | --- | --- |
| `9c53cce4f` | Internal ellipsis wording | Test/docs wording only; no runtime delta required. Accounted for. |
| `b35d32224` | Timer and limit edge cases | Implemented dynamic limit recomputation, timer reset/clear rules, close-all cleanup, zero-limit inert/focus behavior, listener empty-state lifecycle, touch/mouseleave/window-focus corrections, and exact focus routing. Added bUnit and Server/WASM browser regression coverage. |
| `4238b5baa` | Undo demo timeout increase | Both Blazor docs variants and displayed snippets now use 10 seconds. |
| `1677a0e37` | Toast docs heading order | Compared against current source page; Blazor docs retain equivalent navigable sections in framework-appropriate order. No runtime delta. |
| `86c467bd4` | Shared docs demo chrome changes | Documentation infrastructure concern, not Toast runtime. Current Toast examples were compared in both docs hosts. |
| `4cc8e31ca` | Clarify `data-starting-style` description | Current Toast docs wording and parity record use transition-state semantics. Runtime attribute was already present. |
| `e74e19f47` | Correct stack scale calculation | Existing Blazor Toast demo stack transform already uses the corrected calculation; verified against current source docs. |
| `1342a27db` | Reduce Toast bundle size | Viewport global listeners now exist only when non-empty. Frontmost height style is omitted when absent. Interop updates and callbacks are deduplicated. Minified assets were regenerated. |
| `7a0fd2f84` | Published TypeScript state typing | Added typed `ToastPortalState` and typed render delegate, preserving the public state surface in C#. |
| `ec5609b9c` | Render content supplied through render props | Title, Description, and Action mount when a render delegate supplies output even without ordinary child/value content. Added bUnit coverage without fragment caching. |
| `16d50fc41` | Documentation HTML validation/casing | Compared and accounted for in current Blazor Toast documentation. No runtime delta. |
| `bf831b754` | Shared platform detection improvement | FocusGuard now detects Apple platform plus WebKit for the button-role workaround; Safari-only user-agent logic was removed. |
| `c9c90dce2` | Shared custom-tag button keyboard handling | Toast Action and Close now initialize shared button interop and implement native/non-native Enter/Space/disabled semantics with correct cleanup. |

Other commits in the 255-commit range were screened by file/path and dependency impact. Changes confined to unrelated components, build tooling, docs infrastructure, or floating behaviors already present in `PositionerInterop` did not require Toast-specific code changes. They are accounted for as non-impacting rather than omitted.

## Lifecycle and Performance Assessment

- Parameter synchronization uses `OnParametersSet`/`SetParametersAsync`; no manual render-time state-sync loop remains.
- DOM-heavy focus, gesture, observer, animation, and floating work remains in component-specific JS or the shared floating module.
- C# owns public/structural Toast state; JavaScript owns transient browser interaction state.
- Interop calls are snapshot-deduplicated, listener lifetimes are abortable, and the minified Toast asset is materially smaller than the unminified source.
- Every async disposal path guards expected disconnect, cancellation, and disposed-object conditions.

## Final Status

The repaired Toast port is review-ready, staged but not committed, and has no identified unresolved source-parity defect. Verification details are recorded in `docs/audits/toast-verification-report.md`; the source mapping is in `docs/audits/toast-parity-matrix.md`.
