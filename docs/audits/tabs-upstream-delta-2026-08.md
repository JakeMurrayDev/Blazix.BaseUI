# Tabs Upstream Delta & Impact Report

Date: August 21, 2026
Repository: Blazix.BaseUI
Component: Tabs
Source of truth: `.base-ui/packages/react/src/tabs` @ `1a2ca3c9f8a39bd8c0dda939a7a23b72da226124` (origin/master, 2026-08-03)
Prior audit baseline: `bdcb685fadcca9d18b18f013c052795a53b6aa33` (2026-07-18, the cycle-1 delta-inventory pin)
Verified against local HEAD: `4b2a7923`
Ticket: [#170](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/170) (tranche-2 staleness refresh, upstream sync cycle 1)

> The previous Tabs audit doc was deleted in `7a8f9560`, so this record is rebuilt from scratch
> and is pin-dated by construction.

## Delta Window

Tabs' transitive import graph is 140 files. Thirteen commits in the window touch it; two are
Tabs-owned, one of which (`9f7867437`, #5279) is a "expand test coverage" commit carrying seven
source hunks. Walking those hunks — rather than trusting the title — is what produced this
refresh's two ports.

## Tabs-owned commits

| Upstream | Verdict | Notes |
|---|---|---|
| `9f7867437` #5279 | **split — see per-hunk table** | Two ports, one already-present, four skips. |
| `e00174866` #5306 | **(a) skip — React-specific** | No runtime content — a docs demo for animated panels. |

### `9f7867437` (#5279) per-hunk dispositions (Q6.1)

| Upstream hunk | Verdict | User-observable symptom | Evidence |
|---|---|---|---|
| `TabsTab.tsx` — `isMainButtonRef.current = event.button === 0` is now set on **every** pointer-down, and the release listener is registered for every button (adding `pointercancel`, and removing both listeners explicitly) instead of only for the main button | **(b) + (c) port** | Two symptoms. **(1)** With `ActivateOnFocus`, pressing a tab with a secondary button — a right-click for a context menu, or a middle-click — focuses it and therefore selected it. Upstream has suppressed that since before this window; the port had no press-state gate at all, so every secondary press selected the tab. **(2)** Upstream's own bug, which this commit fixes: a secondary press registered no release listener, so the tab stayed "pressing" forever and every later focus activation on that tab was suppressed. | Ported in this change set. `Tabs/TabsTab.razor`: `isPressing`/`isMainButton` fields, an `onpointerdown` handler that sets both (skipping active and disabled tabs, matching upstream's `if (active \|\| disabled) return;`), the focus gate `(!isPressing \|\| isMainButton)` in `HandleFocusAsync`, and a `[JSInvokable] OnTabPressEnd`. `blazix-baseui-tabs.js`: `watchTabPressEnd`/`unwatchTabPressEnd` register `pointerup` **and** `pointercancel` on the **document**, so a release away from the tab still ends the press. Covered by `TabsTestsBase.ActivateOnFocus_SecondaryPress_DoesNotActivateTab` and `ActivateOnFocus_AfterSecondaryPressReleasedOutside_FocusStillActivates`. |
| `TabsTab.tsx` — the focus condition simplifies from `(!isPressingRef.current \|\| (isPressingRef.current && isMainButtonRef.current))` to `(!isPressingRef.current \|\| isMainButtonRef.current)` | **(a) skip — React-specific** | No user-observable symptom: inside the `\|\|`, `isPressingRef.current` is necessarily `true` when the second operand is evaluated, so the conjunct was redundant. | The port carries the simplified form directly (`Tabs/TabsTab.razor`, `HandleFocusAsync`). |
| `TabsTab.tsx` — the tab resize-observer registration moves from a `useIsoLayoutEffect` keyed on a ref into the ref callback itself | **(b) port** | A `Render` template that swaps the tab's host element left the old element registered: the list kept observing it and kept it in its ordered tab set, so the indicator tracked a detached node and navigation saw a phantom tab. | Ported in this change set. `Tabs/TabsTab.razor` records `registeredElement` on each successful registration and, when a later render produces a different `ElementReference`, unregisters the previous element before registering the new one. Test infeasibility: the Tabs test page has no control for swapping a tab's `Render` template at runtime, so this hunk has no covering test; it is verified by inspection against `upsertTab`/`deleteTab` in `blazix-baseui-tabs.js:182-202`, which key strictly by element. |
| `TabsRoot.tsx` — `findTabElement` drops the `value === undefined` early return and the `tabMetadata.value ?? tabMetadata.index` fallback; the mounted-panel map narrows from `Map<TabsTab.Value \| number, string>` to `Map<TabsTab.Value, string>` | **(d:already-present)** | A tab with no explicit value was addressable by its index, so two lookups could disagree about which element a value referred to once tabs were reordered. | `Tabs/TabsRootContext.cs:408-414` — `GetTabElementByValue` looks the element up strictly in `tabsByValue` keyed by the typed value, returning `null` for anything that is not a `TValue`. There is no index fallback and no mixed-key map to narrow. |
| `TabsPanel.tsx` — the `id == null` guard is folded ahead of the `hidden && !keepMounted` guard, with a comment about React 17 resolving `useId` in a passive effect | **(a) skip — React-specific** | No user-observable symptom outside React 17: it prevents registering a panel before its generated id exists on the first commit. | Blazor generates ids synchronously during the component's own lifecycle, so a panel never reaches registration with a null id. |
| `TabsRoot.tsx` — `registerMountedTabPanel` drops its `prev.get(panelValue) === panelId` early return | **(a) skip — React-specific** | No user-observable symptom: a React `setState` bail-out that avoided allocating an identical `Map`. | Render-scheduling optimization only; the resulting map is identical. The unregister half keeps its ownership check (`prev.get(panelValue) !== panelId`), which the port mirrors in its own panel bookkeeping. |
| `TabsRoot.tsx` — the automatic-fallback branch replaces a `setActivationDirectionState(prev => …)` equality bail-out with an unconditional object | **(a) skip — React-specific** | No user-observable symptom: another re-render dedup; the committed value is the same `{ previousValue: fallbackValue, tabActivationDirection: 'none' }` either way. | Render-scheduling optimization only. |

## Shared-layer commits

| Upstream | Verdict | User-observable symptom | Evidence |
|---|---|---|---|
| `8b2282a5e` #5388, `7397c99ba` #5339, `3b5715cc7` #5387, `071e89201` #5394 | **owned by [#158](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/158) batch 2** | Focus-manager close modality and popup-handle lifecycle. | Batch 2 has not landed as of `4b2a7923`. Recorded, not re-dispositioned. |
| `1a2ca3c9f` #5401, `166e8ac01` #5400, `071e89201` #5394, `b089a7ccc` #5309, `dc9a4577f` #5384, `9a5c3850f` #5265 | **(d:moot)** | Popup lifecycle, dev-mode trigger registration, React 17 portal mounting, submenu activation and list-navigation hover. | Tabs renders no popup, opens no floating element and runs no list navigation of that kind; these reach the import graph through shared barrel re-exports, not through any executed Tabs path. |
| `54cfcc188` #5386, `ee38be3e2` #5250, `006a72a99` #5341 | **(a) skip — React-specific** | Declaration emission, selector code size, effect-ownership relocation. None changes DOM output, ARIA, focus order, keyboard/pointer behavior, timing constants or the public API. `006a72a99` stays **revisit-on-symptom** per #158. |

## Note on the local architecture

`ActivateOnFocus` has **two** activation paths in the port, which the ported gate deliberately
guards only one of:

- **Keyboard list navigation** activates in JavaScript — `setFocusedTab` in
  `blazix-baseui-tabs.js:612-630` calls the list's `OnNavigateToTab` directly and never reaches
  the tab's own focus handler.
- **Every other focus** (pointer-induced, `Tab` key, programmatic) reaches
  `TabsTab.HandleFocusAsync`, which is where the press gate lives.

That split matches upstream's observable behavior — upstream's single `onFocus` path allows
keyboard focus because no press is in progress — but it is why the covering test for the release
listener drives programmatic focus rather than arrow keys. An arrow-key test cannot observe the
gate at all, and an earlier draft of that test passed against a deliberately broken build for
exactly this reason.

## Test coverage

| Behavior | Test |
|---|---|
| A secondary press does not select the tab under `ActivateOnFocus` | `TabsTestsBase.ActivateOnFocus_SecondaryPress_DoesNotActivateTab` — confirmed to fail against `4b2a7923` without the gate (the tab reported `aria-selected="true"`). |
| A secondary press released away from the tab does not leave it stuck, so a later focus still activates it | `TabsTestsBase.ActivateOnFocus_AfterSecondaryPressReleasedOutside_FocusStillActivates` — confirmed to fail against a build with `OnTabPressEnd` deliberately neutered. |
| Host-element swap re-registration | No covering test — the Tabs test page has no runtime control for swapping a tab's `Render` template. Verified by inspection (see the per-hunk row above). |
