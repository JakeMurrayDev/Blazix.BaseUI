# Dialog Functional Audit

Date: 2026-06-29

## Scope

Audited and repaired the Blazix.BaseUI **Dialog** (and the Dialog-derived **AlertDialog**) port against the vendored React Base UI source at `.base-ui` commit `748f4228d`.

Primary source surfaces inspected:

- `.base-ui/packages/react/src/dialog/**` (root, store, trigger, portal, backdrop, popup, viewport, title, description, close)
- `.base-ui/packages/react/src/alert-dialog/**`
- `.base-ui/packages/react/src/utils/popups/**` (`store.ts`, `popupStoreUtils.ts`), `utils/popupStateMapping.ts`, `utils/InternalBackdrop.tsx`, `utils/useOpenInteractionType.ts`
- `.base-ui/packages/react/src/floating-ui-react/components/FloatingFocusManager.tsx`, `hooks/useDismiss.ts`, `utils/markOthers.ts`
- `.base-ui/docs/src/app/(docs)/react/components/dialog/**`
- `src/Blazix.BaseUI/Dialog/**`, `src/Blazix.BaseUI/AlertDialog/**`
- `src/Blazix.BaseUI/Utilities/FloatingFocusManager/**`, `src/Blazix.BaseUI/IFloatingRootContext.cs`, `src/Blazix.BaseUI/OpenChangeEventArgs.cs`
- `src/Blazix.BaseUI/wwwroot/blazix-baseui-dialog.js`, `src/Blazix.BaseUI/wwwroot/blazix-baseui-floating.js`

## Method

The audit was executed with four parallel read-only sub-agents — (1) exhaustive React Dialog/AlertDialog source catalog, (2) exhaustive Blazor port catalog, (3) upstream-delta `git show` analysis of every recent `[dialog]`/`[alert dialog]`/`[popups]` commit, and (4) a shared floating/popup-infrastructure sweep mapping Blazor `blazix-baseui-floating.js`/`FloatingFocusManager.razor` to React `floating-ui-react`/`utils` — followed by maintainer synthesis, white-box code verification, bUnit, Playwright (Server + WASM), in-app browser inspection, and PNPM source-doc validation.

Per the shared-utility lesson recorded in prior audits, the upstream delta was diffed against the **shared** floating infrastructure, not only the `dialog/` directory. The immediately-preceding Popover audit (`2d815ba0`) had already back-ported the shared `blazix-baseui-floating.js` fixes (#5093, #5030, #5024, #4775); this audit confirms Dialog inherits them and identifies the Dialog-specific residue.

## Upstream Delta & Impact Report

Baseline: the Blazor Dialog port's behavioral content tracks upstream to ~2026-06-09 to 2026-06-15 (last substantive Blazor Dialog commit `416bc559`, "Remove non-rename divergences"; `15d9a824` only relocated internal components). Every recent upstream commit touching `dialog`, `alert-dialog`, `utils/popups`, and `floating-ui-react` through HEAD `748f4228d` was enumerated and evaluated.

| Upstream commit | Change | Classification | Blazix determination |
| --- | --- | --- | --- |
| `e6dc73dfa` `[all components] Restore visible focus after keyboard close in Safari and Firefox (#5093)` | `FloatingFocusManager` adds `focusVisible: true` to return focus when close type is `keyboard`. | Cross-component (shared) | **Present (inherited).** `blazix-baseui-floating.js` computes `returnFocusVisible = lastInteractionType === 'keyboard'` and passes `focusVisible` through `enqueueFocus`, with an inline `#5093` comment. Dialog consumes the shared manager (`blazix-baseui-dialog.js:258`), so the keyboard-close visible-focus ring is delivered. No Dialog change required. |
| `4292cfaa6` `[popups] Fix non-modal focus-out close and tabindex management (#5030)` | Manager's own `tabindex="0"` write is marked `data-tabindex="0"`; click-trigger pointer-down state reset on next tick. | Shared | **Present / N/A.** `handleTabIndex` writes both `tabindex="0"` and `data-tabindex="0"` (inline `#5030` comment). The pointer-down-reset half is moot: Blazix creates a fresh per-open manager closure (`isPointerDown` is closure-local), so no stale leak across opens. No change required. |
| `d4ee8ae78` `[dialog][drawer] Fix confirmation return focus (#5024)` | `FloatingFocusManager` records the in-popup element that had focus when focus is lost to `body`, scoped to `modal`. | Shared | **Present (inherited).** `blazix-baseui-floating.js` installs a `modal`-scoped `focusout` recorder with the same `relatedTarget == null && contains(...)` guard and an inline `#5024` comment. No Dialog change required. |
| `930bdd5b9` `[popups] Don't steal initial focus if focus already moved inside (#4775)` | `enqueueFocus` gains a `shouldFocus()` guard so initial focus is not re-stolen after focus legitimately moved inside. | Shared | **Present (inherited).** `setInitialFocus` captures `hadFocusInsideAtSchedule` and early-returns when focus has moved to another in-popup element (inline `#4775` comment). No change required. |
| `fe2101a31` `[dialog] Slim dialog root and dedupe default initial focus (#5034)` | React-internal refactor; portable nugget is touch-initial-focus (`interactionType === 'touch' ? popupRef : true`) to suppress the virtual keyboard. | Dialog-specific | **Present (behavior).** The React-tree-shape refactor is N/A to Blazor. The touch-initial-focus behavior already exists in `blazix-baseui-floating.js` `getInitialFocusTarget` (`isTouchInteraction → floatingElement`). No change required. |
| `ea3818dec` `[dialog] Fix touch outside-press dismissal without a backdrop (#5096)` | React `useDialogRoot.outsidePress` special-cases `touchend` (`changedTouches.length === 1 && touches.length === 0`) because its prior `touches.length !== 1` guard rejected valid single-finger taps on backdrop-less dialogs. | Dialog-specific | **Accounted for — architecturally moot.** Blazix does not port React's `useDismiss` touch-count guard. Backdrop-less dismissal is JS-driven: non-modal uses capture-phase `pointerdown` (`setupOutsideClickListener`), `trap-focus` uses capture-phase `click` (`setupBackdropClickListener`); both fire for touch with `button === 0` and never had the touch-count rejection the React fix repairs. No code change. |
| `db574a044` `[dialog] Fix menu focus flake (#4970)` | Test-only assertion change in `DialogRoot.test.tsx`. | Dialog-specific | **N/A.** No production code changed upstream; nothing to port. |
| `802a5ba86` `[popups] Restore viewport morphing after reopen for kept-mounted popups (#5010)` | Resets `lastHandledTriggerRef` on close so the kept-mounted viewport morph re-runs on reopen. | Shared | **N/A for Dialog.** `Dialog.Viewport` is a plain positioning/scroll container (`role="presentation"`, `pointer-events:none` while closed) with no multi-trigger morph — the React fix targets anchored-popup viewport morphing that the Dialog viewport does not perform. No change required. |
| `f70b3160e` `[popups] Fix programmatic focus return (#4849)` | Programmatic opens prefer the pre-open focused element over the trigger on return; excludes `body`. | Shared | **Accounted for.** Blazix captures the manager's `previouslyFocusedElement = activeElement(doc)` at creation: for a trigger-press open that element is the trigger; for a programmatic open it is the pre-open element — both common cases coincide with the React preference. The unmirrored `body`-exclusion is a benign no-op (returning focus to `body` ≡ not returning focus). Same determination reached in the Popover audit; not patched speculatively. |
| `e0c111994` `[popups] Fix rendered trigger id ownership (#5110)` | Reassociates the active trigger id to a registered id by matching DOM element identity when the active id is not in the registry. | Shared | **Accounted for.** In the Blazor Dialog the trigger's registration key **is** its rendered DOM `id` (`DialogTypedTrigger` uses one value for both), so the React rendered-id-vs-internal-id divergence the fix repairs cannot arise as it does in React (which auto-generates a separate internal id). The defensive element-identity reassociation is Popover-specific infrastructure (a JS trigger-element map Dialog does not maintain); no reproducible Dialog gap. Flagged, not patched speculatively. |
| `ae858c8b0` `[internal] Remove dead popup focus and dismiss paths (#4945)` | Removes dead `orderRef` branches, `referencePressEvent` indirection, and `markerIgnoreElements`. | Shared (cleanup) | **N/A.** Upstream dead-code removal. `markerIgnoreElements`/`referencePressEvent` were never ported. (Blazor `handleTabIndex` retains the now-removed `order`-includes-`floating` branch as harmless divergence — noted in Residual Risk.) |

### Newly-identified Dialog parity gaps (not tracked to a single upstream commit)

| Gap | Evidence | Determination |
| --- | --- | --- |
| `DialogOpenChangeEventArgs` did not expose the owning trigger or originating interaction. | React `Dialog.Root.ChangeEventDetails` exposes `trigger: Element | undefined` (and `event`), and `DialogStore.setOpen` sets `eventDetails.trigger = activeTriggerElement` on close. The Blazor args carried only `Open`/`Reason`/`IsCanceled`/`PreventUnmount`/`IsPropagationAllowed`. The sibling `PopoverOpenChangeEventArgs` already exposes `Trigger`/`TriggerId`/`Event`/`InteractionType`. | **Repaired.** See Repairs §1. |
| Callback `FinalFocus` was resolved eagerly at open with the open interaction type. | `DialogPopup.ResolveFocusCallback` invoked `cb.Fn(Context.InteractionType)` at open/parameter time and baked the result into the JS focus manager. React evaluates the `finalFocus` function at **close** with the **close** interaction type. | **Repaired.** See Repairs §2. |

The base `OpenChangeEventArgs<TReason>` already implements `IsPropagationAllowed`/`AllowPropagation()` (React's `allowPropagation`), so that is **not** a gap; Dialog inherits it.

## Repairs Applied

| # | Area | Files | Result |
| --- | --- | --- | --- |
| 1 | Open-change trigger association | `Dialog/EventArgs.cs`, `Dialog/DialogRoot.razor`, `Dialog/DialogRootTests.cs` (+ contract) | `DialogOpenChangeEventArgs` now exposes `Trigger` (`ElementReference?`), `TriggerId`, `InteractionType`, and `Event` (matching `PopoverOpenChangeEventArgs` and React `ChangeEventDetails`). `DialogRoot.SetOpenAsync` populates `Trigger`/`TriggerId` (active trigger, with the implicit single-trigger fallback) and `InteractionType` from existing state — no signature churn. AlertDialog inherits the change. |
| 2 | Close-interaction-type `FinalFocus` callback | `Dialog/DialogRootContext.cs`, `Dialog/DialogRoot.razor`, `Dialog/DialogPopup.razor`, `wwwroot/blazix-baseui-dialog.js` (+ `.min.js`) | A callback `FinalFocus` is now deferred at open (the manager defaults to the trigger) and re-resolved at close with the resolved close interaction type (`escape-key`/`focus-out` → `keyboard`; `close-press`/`outside-press`/`trigger-press` → `mouse`, mirroring the Popover/Select mapping). `DialogRoot.SetOpenAsync` pushes the re-resolved target to the shared module state via `setFinalFocusElement` before close; `cleanupFocusManager(rootState, true)` passes it as the dispose `overrideReturnFocusElement`. Static `FinalFocus` (None/Default/Element) and the common single-trigger return path are behaviorally unchanged. |

Both repairs are additive: the close-time re-resolution is gated to the callback case (`DialogPopup` registers `ResolveFinalFocusForClose` only when `FinalFocus is FocusTarget.Callback`), and the JS dispose override is null for every non-`element` final-focus mode, so the existing 96 bUnit and full Playwright Dialog suites are unaffected.

## Attribute / ARIA Parity

A full part-by-part attribute comparison (React catalog vs. Blazor catalog) confirmed parity across all nine parts and the AlertDialog wrappers:

- **Trigger** — `id`, `aria-haspopup="dialog"` (hardcoded `dialog` even for AlertDialog, matching React), `aria-expanded`, `aria-controls` (only when active/known), `data-popup-open`, `data-disabled`.
- **Backdrop** — `role="presentation"`, `hidden` when unmounted, `data-open`/`data-closed`/`data-starting-style`/`data-ending-style`, `user-select:none` + `-webkit-user-select:none`; nested-dialog backdrop suppressed unless `ForceRender`.
- **Popup** — `id`, `role` (`dialog`/`alertdialog`), `aria-labelledby`, `aria-describedby`, `tabindex="-1"`, `data-open`/`data-closed`/`data-starting-style`/`data-ending-style`/`data-nested`/`data-nested-dialog-open`, `--nested-dialogs` CSS var.
- **Viewport** — `role="presentation"`, `hidden`, the four transition/state data-attributes plus `data-nested`/`data-nested-dialog-open`, `pointer-events:none` while closed.
- **Title** `h2` + label id; **Description** `p` + description id; **Close** `button` + `data-disabled`.

Blazor additionally emits `aria-modal="true"` on the modal/trap-focus popup (static for SSR/bUnit, plus the shared `FloatingFocusManager` at runtime). React Base UI deliberately relies on `markOthers` `aria-hidden` rather than `aria-modal`; this is a shared, cross-component decision in `blazix-baseui-floating.js` (it applies to Popover/Menu/Select identically) in the more-accessible direction, and is recorded in Residual Risk rather than altered in a single-component audit.

## Residual Risk

No audited Dialog behavioral parity gap remains after the repairs. Tracked non-behavioral / low-confidence items:

- **Callback `FinalFocus` returning an element at open but `null`/default at close** is now handled correctly (open is deferred, so close re-resolution to default falls back to the trigger). The close-interaction-type mapping is reason-derived (`close-press` → `mouse`) rather than captured from the actual close pointer event, matching the sibling Popover/Select mapping; a keyboard-activated close-button press is reported as `mouse`, consistent with those ports.
- **`#5110` element-identity reassociation** is not ported; in the Blazor port the trigger registration key equals the rendered DOM id, so the React rendered-vs-internal mismatch does not reproduce. Flagged for runtime confirmation if a future detached-trigger-with-divergent-DOM-id scenario is introduced.
- **`aria-modal="true"`** on the modal popup is a shared `blazix-baseui-floating.js` convention (more accessible than React's `aria-hidden`-only approach); cross-component, not altered here.
- **`handleTabIndex` `order`-includes-`floating` branch** in shared `blazix-baseui-floating.js` predates and diverges from upstream `#4945`'s removal; harmless dead logic, cross-component, not altered here.
- **`DialogOpenChangeReason.Swipe`/`CloseWatcher`** enum members are declared but never produced by the Dialog path (no swipe gesture or CloseWatcher integration in this port); harmless unused surface, left intact to avoid a risky enum change.
- **`DialogRootContext.ViewportElement`** is captured but not forwarded to JS (no `setViewportElement` consumer); inert and benign.

The remaining surface difference is the unavoidable Blazor event model: consumers cancel Base UI behavior through `DialogRoot.OnOpenChange` → `args.Cancel()`/`args.AllowPropagation()`/`args.PreventUnmountOnClose()` rather than mutating a browser event object, and the native `Event` is not marshaled across JS-driven dismissal (escape/outside/focus-out) paths.
