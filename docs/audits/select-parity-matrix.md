# Select Parity Matrix

Date: 2026-07-21
React head: `bdcb685fadcca9d18b18f013c052795a53b6aa33`

| React source/hook/utility | Required behavior | Blazor equivalent | Verification |
| --- | --- | --- | --- |
| `SelectRoot.tsx`, store selectors | Canonical open/value/mounted/active/form state | `SelectRoot.razor`, `SelectRootContext.cs` | 291 bUnit + browser |
| React synced-value/value-change hooks | Effects only after resolved controlled/uncontrolled change | `OnParametersSet` observer and snapshots | Programmatic-value regression |
| Root ID test | Dynamic trigger/label/list/ARIA IDs | Mutable context ID + JS `renameRoot` | Build/bUnit structure |
| Hidden input/form utilities | Single/multiple serialization, disabled, required, autofill, visually hidden variants | Root input rendering and guarded change handler | bUnit |
| Field/label contexts | Name, filled, dirty, touched, invalid, described/labelled state | Field and Labelable contexts | bUnit |
| `SelectTrigger`/click interaction | Combobox semantics, 400ms gate, 5px slip tolerance | Trigger Razor + component JS | Server/WASM Playwright |
| `SelectValue`, `resolveValueLabel` | Placeholder, rich labels, multiple labels, object Label/Value | `SelectValue`, `SelectOption.LabelContent`, `SelectValueResolver` | bUnit/source browser |
| `SelectLabel` | Visible label registration and focus | `SelectLabel` | Server/WASM Playwright |
| `SelectPortal`/FloatingPortal | Mount lifetime and selector/element target | `SelectPortal`, shared `Portal.TargetElement` | Build/browser |
| `SelectPopup` | Role ownership, transition state, focus restore/final focus, delayed element-reference registration | Popup + FloatingFocusManager + first-available-reference lifecycle gate | bUnit + Server/WASM Playwright |
| Focus manager open method | Programmatic opener vs trigger return | `InteractionType`, single FFM owner | In-app browser + Playwright |
| Anchored popup scroll lock | Non-touch lock; full-width touch only | DOM width measurement + positioner desired state | Browser architecture |
| `SelectPositioner`, `useAnchorPositioning` | Static/function offsets, per-side padding, element/virtual anchors/boundaries | Positioner parameters + shared Floating JS | Build/source mapping |
| `useFloating.isPositioned`, `size()` middleware | Popup stays hidden until the current open has fresh anchor/available-size output | Per-open readiness revision, controlled pre-open handoff, suspended generation reset, C# `IsPositioned`, same-frame JS commit | Server/WASM frame regression + WASM docs trace |
| `a68d387d6a` close-transition reset | Reopen cannot reuse stale position or sizing | Close/open invalidation, `data-positioned` removal, fresh Floating UI update | Reopen with trigger width changed 224px to 280px |
| Controlled open and root rename | Pre-open callbacks target exactly the next revision, reject closed-transition auto-updates, and follow a changed root ID | Explicit pre-open reset token + pending readiness revision + mutable positioner registration root ID | Server/WASM controlled reopen and rename regressions |
| Placement watchdog and standard fallback | Retry stalled commits without exposing uncommitted geometry; switch ownership atomically when align placement cannot complete | Watchdog calls placement only; fallback cancels queued align commits and starts standard Floating UI with restored anchor tracking | Server/WASM no-commit and successful-fallback regressions |
| `ownerWindow(floating).devicePixelRatio` | Pixel snapping uses the floating element's realm | `ownerDocument.defaultView?.devicePixelRatio` | JS source inspection and syntax check |
| Collision middleware | Raw shift/size padding; biased flip only | Shared Floating JS overflow options | JS syntax/source mapping |
| Align-item popup logic | Current-revision sizing, fixed pre-position state, no stale origin, `maxHeight:none` | Select JS generation gate and placement commit | Per-rAF in-app browser + Playwright |
| Popup placement effect dependencies | Stable open/layout inputs commit once; active index and arrow visibility do not replay placement | Open/input revision commit guard; transition-only C# readiness call; no fixed-delay probes | Server/WASM 1.2-second grouped-scroll regression |
| Committed placement visibility | A same-input Floating update cannot hide or replace a valid align-item layout | Current-revision commit preserves `data-positioned` and blocks late standard fallback | Server/WASM per-frame height regression |
| Scroll edges/growth | Fractional normalization and reduced mutation path | Select JS normalization/growth handler | Source/browser mapping |
| `SelectList` | Live listbox ownership/registration | `SelectList` | bUnit/browser |
| Composite item registry | Atomic metadata/index and grouped reorder handling | Context registry + shared option MutationObserver | bUnit/browser |
| List navigation | Disabled focusable by arrows; skipped initially | Separate next/next-enabled functions | In-app browser |
| Composite nearest scrolling | Selected and active items scroll correctly through nested group offset parents | Open-layout readiness gate + list-relative rectangle scrolling | Server/WASM overflowing grouped fixture |
| Typeahead | Disabled skip, repeated cycling, buffered current match | JS open + C# closed typeahead | Added bUnit/source tests |
| `SelectItem` | Option state, disabled/read-only guard, consumer-first click, drag | `SelectItem` + root guards | bUnit/browser |
| Item virtual-click gate | Generic virtual clicks require highlight; assistive pointer metadata may activate unhighlighted items | Native capture metadata + C# commit gate | Server/WASM Playwright |
| Custom item keyboard | Preserved modifiers, one activation, prevented keydown | `activateItemFromKeydown` | Source mapping/JS syntax |
| Item tab order | `open && highlighted` | Item attribute builder | In-app browser |
| `SelectItemText` | Text and selected-text refs | `SelectItemText` context registration | bUnit |
| `SelectItemIndicator`/transition hook | Real CSS lifetime and reversal | JS part transition listener | bUnit/build |
| `SelectScrollArrow`/scroll edges | Visibility, hover scrolling, real transition lifetime | Arrow components + Select JS | bUnit/source browser |
| Group/GroupLabel | Group semantics and label association | Group contexts | bUnit |
| Arrow/Icon/Backdrop | Source state and structural attributes | Matching Razor parts | bUnit/source tests |
| `useRenderElement` | Custom render/state/class/style without fragment caching | `RenderElement<TState>`; live fragment resolution | Build/bUnit |

## Verification Judgment

Every Select production part, hook-equivalent, utility, ARIA/data attribute, and audited upstream fix has a verified Blazor equivalent. DOM geometry, focus, ordering, transition, and scroll work remains in JavaScript; canonical public/structural state remains in Blazor.
