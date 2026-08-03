# Upstream Delta Inventory: Base UI since pin `bdcb685`

Resolves wayfinder ticket #145.

- **Pin:** `bdcb685fadcca9d18b18f013c052795a53b6aa33` (2026-07-18, @base-ui/monorepo 1.6.0)
- **Upstream head at audit time:** `1a2ca3c9f8a39bd8c0dda939a7a23b72da226124` (`origin/master`, 2026-08-03)
- **Total commits:** 91 (date range 2026-07-20 → 2026-08-03)
- **Releases:** none — `CHANGELOG.md` is unchanged and `package.json` is still `1.6.0`; every commit below is unreleased post-1.6.0 master work.
- **Method:** primary sources only — `git log`/`git show` over `bdcb685..origin/master` in the vendored upstream clone (`.base-ui`, remote `https://github.com/mui/base-ui.git`). Upstream squash-merges with title-only messages, so classification is based on the diffs themselves.

## Classification axes

- **Kind:** feature / bug fix / refactor / infra (CI, docs, tests-only — skippable for the port).
- **Applicability:** **neutral** (structural DOM/ARIA/a11y, state-machine logic, keyboard/pointer interaction, CSS/data-attributes, JS-layer behavior the Blazor port mirrors in its wwwroot JS modules) vs **React** (hook re-render behavior, effect ordering, React 17/18 rendering, TS typings, store-selector packaging). Rows marked ⚠ are uncertain — verify against the port before acting.

## Summary

- **~30 framework-neutral bug fixes** are candidates for porting.
- **3 framework-neutral API features:** `cancel-open` and `input-press` change-event reasons (Autocomplete/Combobox), inline-combobox expanded state.
- **Notable clusters:**
  1. **Shared popup/positioning/focus plumbing (largest cluster, ~19 commits).** Detached-trigger popup-handle lifecycle fixes (#5339, #5387, #5394), FloatingFocusManager stale close-modality fix (#5388), scroll-lock handoff (#4665), pinch-zoom positioning (#4485), auto-resize origin (#5370), unpositioned-popup origin (#5299), canceled exit unmount (#5401). Per the prior audit lesson, these shared fixes are exactly the kind a per-component diff misses.
  2. **Drawer (5 neutral gesture/scroll fixes)** — Shadow DOM swipes, snap-point jump, iOS touchmove slop, virtual-keyboard scroll, plus a large viewport refactor.
  3. **Toast (4 neutral fixes)** — swipe-direction locking, re-adding a closing toast, remaining-timer math, provider effect ordering.
  4. **Combobox/Autocomplete (6 neutral changes)** — two new event reasons, portalled content, highlight restoration, disabled inheritance, inline expanded state.
  5. **Test-coverage sweep (21 commits, one per component).** Nominally tests-only, but most also land non-trivial source refactors (DrawerViewport −202 lines, SelectRoot ±174, NavigationMenuTrigger ±191). Marked infra/refactor, but skim the source side during component audits — small behavior tweaks may hide in them.

---

## Shared: popup handles, stores, portals (`utils/popups`, `floating-ui-react/components`, stores)

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `1a2ca3c9f` | #5401 | [all components] Fix canceled exit unmount | bug fix | neutral | Web Animations API: when an exit animation is canceled, wait for replacement animations before unmount (`getAnimations().finished` rejection handling); port mirrors animation-finished detection in JS |
| `166e8ac01` | #5400 | [popups] Fix quadratic dev-mode trigger registration check | bug fix | React | Dev-mode-only warning perf in `popupTriggerMap`; only relevant if port replicated the dev diagnostic |
| `071e89201` | #5394 | [all components] Avoid unused popup handle attachments | refactor | ⚠ neutral | Skips attaching detached-trigger handles when unused; applies if/where port implements popup handles (Handle surface deviation already flagged) |
| `3b5715cc7` | #5387 | [all components] Fix popup handle lifecycle regressions | bug fix | ⚠ neutral | Popup-handle store state-machine fixes (`popupHandle`/`popupStoreUtils`); neutral logic but gated on port's handle support |
| `7397c99ba` | #5339 | [all components] Fix popup handle calls during mount | bug fix | ⚠ React | Guards handle calls made during React mount/render phase; concept (calls before init complete) may map to Blazor lifecycle races |
| `8b2282a5e` | #5388 | [all components] Fix stale popup close modality | bug fix | neutral | FloatingFocusManager: reset close/interaction type on open and snapshot at close so return-focus `focusVisible` uses the right modality; port's focus manager JS mirrors this |
| `b089a7ccc` | #5309 | [popups] Mount the popup subtree synchronously on open in React 17 | bug fix | React | React 17 legacy-mode portal mounting; no Blazor analogue |
| `ee38be3e2` | #5250 | [all components] Reduce selector bundle size | refactor | React | Store-selector code-size optimization across all component stores |
| `595c0fa08` | #5340 | [all components] Guard registered ID cleanup | bug fix | neutral | Label-ID registration race: cleanup only clears the ID if it still owns it (`currentId === id ? undefined : currentId`); applies to port's label/aria-labelledby registration |
| `006a72a99` | #5341 | [all components] Simplify lifecycle effect ownership | refactor | React | Moves effect ownership between components (useDismiss/useTypeahead/FloatingDelayGroup/useSwipeDismiss touched); React lifecycle mechanics |
| `b38becd6e` | #5337 | [all components] Correct effect timing | refactor | ⚠ React | useEffect vs layout-effect timing across accordion/collapsible/dialog/drawer/toggle; React-specific but could subtly change observable open/close behavior — skim per component |
| `54cfcc188` | #5386 | [typescript] Preserve published internals types | infra | React | TS typing preservation for published internals |
| `ce7358672` | #5298 | [internals] Export useAnchorPositioning and getDisabledMountTransitionStyles | refactor | React | Packaging/exports move (wide but mechanical file touch across all positioners) |
| `6feeb1f54` | #5357 | [internal] Use predefined change reasons | refactor | neutral | Replaces ad-hoc change-reason strings with shared constants across combobox/menu/menubar/number-field/select/slider; worth mirroring for event-reason parity |

## Shared: positioning & viewport (`useAnchorPositioning`, `usePopupAutoResize`, `useScrollLock`)

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `84ac4b797` | #4485 | [popups] Ignore pinch-zoom shifting | bug fix | neutral | Anchor positioning: layout-viewport `rootBoundary`/shift options so popups don't shift while pinch-zooming; positioning math ported in JS |
| `bd2f34ddb` | #5299 | [all components] Keep unpositioned popups at the viewport origin | bug fix | neutral | Unpositioned popups pinned to viewport origin instead of layout position (prevents scroll jumps / overflow before first position) |
| `692bc8748` | #5370 | [popups] Fix auto-resize origin for left-anchored popups | bug fix | neutral | `usePopupAutoResize` transform-origin math for left-anchored popups; port mirrors auto-resize in JS |
| `1e64978b1` | #5372 | [popups] Avoid redundant re-renders with lazy flip | refactor | React | Re-render avoidance in `useAnchorPositioning`; React render mechanics (flip logic itself unchanged) |
| `3dceedea8` | #4665 | [popups] Fix scroll lock handoff with external overlays | bug fix | neutral | `useScrollLock`: viewport-scroller detection helper, longhand overflow restore, correct handoff when another overlay already locked scroll; pure JS-layer, maps to port's scroll-lock module |
| `8f795a8fd` | #5264 | [utils] Fix usePreviousValue equality comparison | bug fix | ⚠ React | React hook internals (`packages/utils`); applicable only if port replicated the previous-value pattern |
| `7e80f4308` | #5385 | [test] Stabilize React 18 test timing | infra | React | Tests-only (`useIdleCallback` tests) |

## Shared: floating-ui-react interaction hooks

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `dc9a4577f` | #5384 | [menu] Open submenus on Android TalkBack press | bug fix | neutral | `useClick`: recognize TalkBack synthesized presses so submenu opens; a11y input heuristic in JS |
| `9a5c3850f` | #5265 | [combobox][select] Fix hovered item stealing highlight when the list scrolls in Safari | bug fix | neutral | `useListNavigation`: ignore zero-delta WebKit pointer/mouse moves fired while list scrolls under a stationary pointer (upstream #4002); port's list-nav lives in menu.js |

## Accordion / Collapsible

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `ffea4823a` | #5313 | [accordion][collapsible] Expand test coverage | infra (tests + refactor) | ⚠ React | Mostly tests; source side reworks `useCollapsiblePanel` (±57 lines) and trims AccordionItem/stateAttributesMapping — skim for behavior tweaks |

## Autocomplete / Combobox

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `eaf032673` | #5376 | Add `cancel-open` to change event details | feature | neutral | New change-event reason surfaced in open-change details; API parity for port's typed reasons |
| `4fc0e92ca` | #5356 | Add `input-press` to change event details | feature | neutral | New change-event reason for input presses |
| `5611d3008` | #5332 | [combobox] Expose expanded state for inline combobox | feature | neutral | Expanded state (aria/data attribute) for inline mode |
| `f17f2efd6` | #5334 | [combobox] Keep portalled popup content open | bug fix | ⚠ neutral | Portalled list/popup content no longer closes incorrectly; behavior is neutral, mechanism partly React portal handling |
| `f30013819` | #5232 | [combobox] Return highlight to selected item on query clear | bug fix | neutral | Highlight state-machine: on query clear, restore highlight to selected item (adds shared `itemEquality`) |
| `60ba45832` | #5365 | [combobox][select] Inherit root disabled state in items | bug fix | neutral | Items now report disabled when root is disabled (data-attrs/interaction gating) |
| `3d493450f` | #5266 | [combobox] Expand test coverage | infra (tests + refactor) | ⚠ React | Mostly tests; trims dead code in AriaCombobox/input/chip-remove — skim |
| `c824da86a` | #5316 | [test] Stabilize combobox popup reopen tests | infra | React | Tests-only |

## Checkbox / Radio / Switch

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `af3452aa8` | #5278 | [radio][checkbox][switch] Expand test coverage | infra (tests + refactor) | ⚠ React | Mostly tests; CheckboxRoot/CheckboxGroup/RadioGroup source cleanup — skim |

## Dialog

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `da6547172` | #5273 | [dialog] Expand test coverage | infra (tests + refactor) | React | Mostly tests; minor useDialogRoot/DialogStore cleanup |
| `8c7fb7130` | #3139 | [dialog][docs] Add demo for custom focus management | infra | React | Docs demo only |

## Drawer

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `634b14097` | #5360 | Fix Shadow DOM swipe gestures | bug fix | neutral | Swipe target resolution via `getElementAtPoint`/composedPath so gestures work inside shadow roots; JS gesture layer |
| `5df7708bb` | #5308 | Fix snap point jump when pinned pointer moves leave the drag offset unchanged | bug fix | neutral | `useSwipeDismiss` drag-offset math; JS gesture layer |
| `f0478fa6d` | #5257 | Fix cross-axis scroll blocked on iOS below the touchmove slop | bug fix | neutral | touchmove slop threshold handling in DrawerViewport; JS gesture layer |
| `5943dec57` | #5179 | Fix scroll handling when focus moves while the virtual keyboard is open | bug fix | neutral | VirtualKeyboardProvider scroll compensation; JS layer |
| `91e6c8230` | #5289 | Expand test coverage | infra (tests + refactor) | ⚠ React | Large source refactor rides along (DrawerViewport −202 lines, SwipeArea −59); skim against port's drawer JS before assuming no behavior change |

## Field / Form / Fieldset

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `db86f44d8` | #5390 | [field] Fix required validity after custom error | bug fix | neutral | Validation state machine: required validity re-evaluated after a custom error is set/cleared |
| `d060189d4` | #5290 | [field] Fix `data-dirty` tracking for null-valued controls | bug fix | neutral | Dirty tracking when control value is null; data-attribute semantics |
| `293a0f1ed` | #5287 | [form] Focus the first invalid field in document order | bug fix | neutral | On submit, focus first invalid field by DOM document order rather than registration order |
| `8846f9937` | #5383 | [form] Include portaled group values in Form submission | bug fix | neutral | CheckboxGroup/RadioGroup values rendered in portals now included in form serialization |
| `32cabb778` | #5281 | [form][field][fieldset] Expand test coverage | infra (tests + refactor) | React | Mostly tests; small FieldError/FieldItem/FieldsetLegend cleanup |

## Menu / Menubar / Context Menu

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `79f1443d3` | #5363 | [menu] Propagate disabled state to items | bug fix | neutral | Menu/checkbox/radio items inherit root disabled state |
| `022d979ae` | #5342 | [menu] Fix VoiceOver announcement when opening a submenu | bug fix | neutral | Submenu trigger ARIA/focus sequencing so VoiceOver announces the submenu |
| `dc9a4577f` | #5384 | [menu] Open submenus on Android TalkBack press | bug fix | neutral | (Listed under shared hooks — fix lands in `useClick`) |
| `9d61f9291` | #5269 | [menu] Expand test coverage | infra (tests + refactor) | ⚠ React | Mostly tests; ContextMenuTrigger/MenuTrigger source cleanup — skim |
| `3c55b155c` | #5393 | [menubar] Make touch-click cooldown test deterministic | infra | React | Tests-only |

## Navigation Menu

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `09124a6b2` | #5271 | Expand test coverage | infra (tests + refactor) | ⚠ React | Large NavigationMenuTrigger rework (±191 lines) + `isOutsideMenuEvent` cleanup rides along — skim |

## Number Field

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `b6b05c795` | #5280 | Expand test coverage | infra (tests + refactor) | ⚠ React | Mostly tests; touches `parse.ts`, input/scrub-area source — skim parse changes for behavior |

## OTP Field

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `1f10514c4` | #5317 | Expand test coverage | infra (tests + refactor) | React | Mostly tests; minor OTPFieldRoot/Input cleanup |

## Meter / Progress

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `c02319dfc` | #5389 | [progress] Fix clamped value formatting | bug fix | neutral | Formatted value/text uses the clamped value, not the raw prop |
| `086eeaded` | #5312 | [meter][progress] Expand test coverage | infra (tests + refactor) | React | Mostly tests; small ProgressRoot/Value cleanup |

## Popover

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `6d25129e0` | #5272 | Expand test coverage | infra (tests + refactor) | ⚠ React | Mostly tests; tweaks `PopoverPopupDataAttributes` — verify no data-attribute rename affects port |

## Preview Card

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `3e6aada47` | #5318 | Expand test coverage | infra (tests + refactor) | React | Mostly tests; minor store/context cleanup |

## Scroll Area

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `f5a1dff0d` | #5374 | End thumb drag when the primary button is no longer held | bug fix | neutral | Pointer `buttons` check ends drag if button released outside; JS drag logic |
| `1d84749b1` | #5259 | Prevent scroll snapping while dragging the thumb | bug fix | neutral | Suppress CSS scroll-snap during thumb drag; JS + style toggling |
| `5c0138230` | #5311 | Expand test coverage | infra (tests + refactor) | ⚠ React | Mostly tests; ScrollAreaScrollbar refactor (±86 lines) — skim |

## Select

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `60ba45832` | #5365 | Inherit root disabled state in items | bug fix | neutral | (Shared with combobox above) |
| `9a5c3850f` | #5265 | Safari hover stealing highlight | bug fix | neutral | (Listed under shared hooks) |
| `4c56a0b6a` | #5276 | Expand test coverage | infra (tests + refactor) | ⚠ React | Large SelectRoot refactor rides along (±174 lines) — skim |
| `cbc87d195` | #5402 | Stabilize scroll arrow cleanup test | infra | React | Tests-only |

## Slider

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `6f262b1c7` | #5391 | Fix post-v1.6 regressions | bug fix | neutral | SliderThumb + `validateMinimumDistance` logic regressions from the 1.6 slider rework |
| `c5c771b59` | #5277 | Expand test coverage | infra (tests + refactor) | ⚠ React | Source cleanup in control/thumb/collision utils — skim |

## Tabs

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `9f7867437` | #5279 | Expand test coverage | infra (tests + refactor) | React | Mostly tests; TabsRoot/TabsTab cleanup |
| `e00174866` | #5306 | [docs] Add animated panels demo | infra | React | Docs only |

## Toast

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `bf2e52725` | #5295 | Fix swipe direction locking for two-axis swipes | bug fix | neutral | Gesture math: direction lock when both axes enabled; JS layer |
| `244aea550` | #5258 | Fix re-adding a closing toast | bug fix | neutral | Toast state machine: re-adding a toast that is mid-close |
| `f6920f916` | #5261 | Fix remaining timer calculation in ToastStore | bug fix | neutral | Timer bookkeeping math in ToastStore (pause/resume remaining time) |
| `b38093dcf` | #5338 | Fix provider prop effect ordering | bug fix | ⚠ React | Effect-ordering fix so provider prop changes (e.g. timeout) apply correctly; mechanism is React, but verify port applies prop changes to pending timers |
| `e8bb57b4f` | #5275 | Expand test coverage | infra (tests + refactor) | ⚠ React | store.ts/ToastRoot source changes ride along — skim |
| `a0cbf875a` | #5307 | [docs] Fix stacked toast height transition in demos | infra | React | Docs demos only |

## Toolbar

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `364c4e46e` | #5274 | Expand test coverage | infra (tests + refactor) | React | Mostly tests; trivial ToolbarSeparator cleanup |

## Tooltip

| Commit | PR | Title | Kind | Applicability | Rationale |
|---|---|---|---|---|---|
| `67840f641` | #5270 | Expand test coverage | infra (tests + refactor) | React | Mostly tests; minor TooltipStore/Trigger cleanup |

## Infra / CI / docs / tests-only (skippable)

| Commit | PR | Title | Kind |
|---|---|---|---|
| `4857aea24` | #5381 | [ci] Adopt the new Claude review workflow | infra |
| `c2bbc7400` | #5375 | [code-infra] Grant Claude review pull-requests: write | infra |
| `5a87c7752` | #5373 | [code-infra] Bump Claude review pin | infra |
| `ff223571b` | #5323 | [ci] Add Claude PR review workflow | infra |
| `57b2832c6` | #5297 | [internal] Improve review skill workflow | infra |
| `7efe3ee50` | #5368 | [code-infra] Bump @mui/internal-code-infra | infra |
| `ead07d901` | #5364 | [docs-infra] Update docs-infra package | infra |
| `345258735` | #5355 | [code-infra] Remove obsolete trust policy exclusion | infra |
| `e06af74c1` | #5354 | [code-infra] Fix docs validation scripts on Windows | infra |
| `d6c25b494` | #5352 | Bump pnpm to 11.17.0 | infra |
| `fe672ce3f` | #5320 | [docs][website] Update team members | infra |
| `9e536679e` | #5282 | [docs] Fix code block focus outline | infra |
| `36c0c029b` | #5245 | [docs][autocomplete] Command palette demo icon | infra |
| `d6e423ac6` | #5262 | [docs][menu] Use align="start" in demos | infra |
| `34d96188a` | #5244 | [docs] Replace data-popup-open with data-pressed | infra (docs; but signals the documented styling convention moved to `data-pressed` on triggers) |
| `a407327a8` | #5353 | [test] Stabilize browser test timing | infra |
| `278cde261` | #5330 | [test] Stabilize popup interaction tests | infra |
| `6e059c2b0` | #5322 | [test] Stabilize tests across timezones and locales | infra |

## Caveats

- Upstream squash-merge messages have no bodies; classifications are diff-based. Rows marked ⚠ need a look at the port's actual implementation before deciding to port or skip.
- The 21-commit "Expand test coverage" sweep bundles source refactors with tests. They are classified infra here, but the source-side diffs (especially Drawer #5289, Select #5276, Navigation Menu #5271, Scroll Area #5311, Toast #5275) should be skimmed during the next per-component audit — upstream may have folded small behavior corrections into them.
- `b38becd6e` (#5337, effect timing) and `006a72a99` (#5341, effect ownership) are classified React-specific, but they change *when* open/close side effects run; if a future component audit finds timing-sensitive divergence, revisit these first.
