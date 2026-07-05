# Combobox Functional Audit

## Scope

Component audited: `src/Blazix.BaseUI/Combobox`.

React source audited:

- `.base-ui/packages/react/src/combobox/index.parts.ts`
- `.base-ui/packages/react/src/combobox/root/ComboboxRoot.tsx`
- `.base-ui/packages/react/src/combobox/root/AriaCombobox.tsx`
- `.base-ui/packages/react/src/combobox/input/ComboboxInput.tsx`
- `.base-ui/packages/react/src/combobox/trigger/ComboboxTrigger.tsx`
- `.base-ui/packages/react/src/combobox/list/ComboboxList.tsx`
- `.base-ui/packages/react/src/combobox/item/ComboboxItem.tsx`
- `.base-ui/packages/react/src/combobox/chips/ComboboxChips.tsx`
- `.base-ui/packages/react/src/combobox/chip/ComboboxChip.tsx`
- `.base-ui/packages/react/src/combobox/chip-remove/ComboboxChipRemove.tsx`
- `.base-ui/docs/src/app/(docs)/react/components/combobox/page.mdx`

Framework-agnostic spec audited:

- `../base-ui-specs/combobox/SPEC.md`
- `../base-ui-specs/combobox/pitfalls.md`

## Result

Combobox was missing from the Blazor port. The implementation now provides a native Blazor component family with React Base UI Combobox behavior mapped into Blazor lifecycle, state, cascading context, and component-specific JavaScript.

Implemented public parts:

- `ComboboxRoot<TValue>`
- `ComboboxLabel`
- `ComboboxValue`
- `ComboboxInput`
- `ComboboxInputGroup`
- `ComboboxTrigger`
- `ComboboxList`
- `ComboboxStatus`
- `ComboboxPortal`
- `ComboboxBackdrop`
- `ComboboxPositioner`
- `ComboboxPopup`
- `ComboboxArrow`
- `ComboboxIcon`
- `ComboboxGroup`
- `ComboboxGroupLabel`
- `ComboboxItem<TValue>`
- `ComboboxItemIndicator`
- `ComboboxChips`
- `ComboboxChip`
- `ComboboxChipRemove`
- `ComboboxRow`
- `ComboboxCollection<TValue>`
- `ComboboxEmpty`
- `ComboboxClear`
- `ComboboxSeparator`

Implemented Blazor equivalents for React utilities:

- `useComboboxFilter` -> `ComboboxRoot<TValue>.Filter`, default label filtering, `FilterDisabled`, `Limit`
- `useFilteredItems` -> `ComboboxRootContext<TValue>.GetFlatFilteredItems()`, `ComboboxCollection<TValue>`, grouped item flattening
- selected-value store selectors -> `ComboboxRootContext<TValue>` functions and typed selected-value callbacks
- chip composite list index -> `ComboboxChipsContext` render-versioned chip index registry

No remaining functional gaps were identified in the audited source surface.

Implemented documentation surface:

- docs route `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/ComboboxPage.razor`
- markdown source `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Components/combobox.md`
- API reference data `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Data/ComboboxApi.cs`
- demo CSS assets `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/wwwroot/demos/combobox.css` and `.min.css`
- live docs demos for hero, multiple select, input inside popup, grouped, async search single, async search multiple, creatable, and virtualized
- source examples without live demos mapped to Blazor snippets: typed wrapper component, visible chip limiting, grouped item data, label-in-popup pattern, and memoized/virtualized item guidance

## Upstream Delta & Impact Report

Upstream mirror state:

- Local `.base-ui` checkout before fetch: `95cf9e0339567518ccdf82628c8ef4f3d67cad07`
- Remote `origin/master` verified by fetch: `ca246a6068d98f8fa27fa1c382743184550a0360`
- `git -C .base-ui log HEAD..origin/master -- packages/react/src/combobox docs/src/app/(docs)/react/components/combobox` returned no Combobox source or docs commits after the local mirror.

Recent Combobox-impacting commits evaluated from the local mirror:

| Upstream change | Source impact | Blazor mapping |
| --- | --- | --- |
| `f8d9cbc93 [combobox] Stop filtering grouped items after limit (#5086)` | Grouped filtering stops after the global limit, not per group. | `ComboboxRootContext<TValue>.GetFlatFilteredItems()` stops once `Limit` is reached. Covered by `GroupedFiltering_ShouldStopAfterGlobalLimit`. |
| `d169cd58f [combobox] Fix autofill and selected state edge cases (#4972)` and `473342172 [combobox][select] Fix browser autofill with object values when autofill uses the label (#4560)` | Hidden form value changes must resolve both serialized value and label while preserving selected object identity. | `FindValueByFormOrLabel`, `ItemToStringLabel`, `ItemToStringValue`, and hidden input change handling map form labels/values back to typed values. Covered by object value tests. |
| `e972d74f9 [combobox] Avoid re-rendering every item on each keystroke (#4964)` | Item state updates should not force full list churn beyond visible/highlighted/selected transitions. | Root keeps item registrations in `ComboboxRootContext<TValue>` and item components refresh from context only when root or item-map events occur. Filtering is computed in context; render fragments are not cached. |
| `1bce6e1c8 [combobox] Keep ArrowLeft/ArrowRight on the input caret in grid mode (#4948)` | Arrow-left/right in the input must not be hijacked for grid navigation. | JS keyboard handler delegates vertical navigation and commit behavior; left/right caret movement remains native for input. |
| `254809a28 [select][combobox][number field] Ignore hidden-input changes while readonly or disabled (#4810)` | Hidden input autofill/change events must be ignored when disabled or readonly. | `HandleHiddenInputChange` returns without mutation when `ResolvedDisabled` or `ReadOnly` is true. Covered by `HiddenInputChange_ShouldBeIgnoredWhenReadOnly`. |
| `53c3d1e35 [combobox] Preserve closeQuery when closing multiple input-inside-popup combobox (#4715)` | Multiple mode and popup input mode should not collapse selection/input state incorrectly on close. | Multiple selection keeps popup open on item press, clears typed input only where React does, and stores selection separately from input value. Covered by Playwright multiple-selection and input-inside-popup tests. |
| `1e7473eab [combobox] Fix popup input form submit (#4687)` | Enter with no active item must allow form submit instead of always preventing default. | JS only prevents Enter when an active item exists. Covered by `EnterWithOpenPopupAndNoActiveItemSubmitsForm`. |
| React source default `openOnInputClick = true` | Clicking the input opens the popup by default. | `OpenOnInputClick` defaults to `true`. Covered by `InputClickOpensPopupByDefault` in Server and WASM browser tests. |
| `299d309c1 [combobox] Respect rendered chips for keyboard navigation (#4572)` and chip context tests | Chip removal is index-based over rendered chips. | `ComboboxChipsContext` uses a render-versioned registry keyed by chip instance; `ComboboxChipRemove` removes by resolved chip index. Covered by bUnit and Playwright chip removal. |
| `d14973b33 [combobox] Fix chip context error (#4877)` | Chip remove requires a chip context and must not operate outside a chip. | `ComboboxChipRemove` renders only when both root and chip context exist. |
| `3fde28905 [popups] Prevent unwanted flip with capped scrollable content (#5120)` | Positioners seed available size vars before measurement and expose collision config. | Combobox positioner delegates to shared floating JS, including `--available-width`, `--available-height`, collision padding, boundary, side/align data attributes, and anchor size variables. |
| `6c3195e29 [all components] Type render callback props to the rendered element (#5104)` | Render callbacks receive element-specific render props. | All Combobox parts use `RenderElement<TState>` with typed `RenderFragment<RenderProps<TState>>?`. |
| `4cc8e31ca [all components] Clarify data-starting-style attribute description (#5151)` | Documentation-only state attribute clarification. | No Combobox runtime code required. Existing transition states flow through popup/backdrop/positioner state attributes. |

## Repair Log

- Added component-specific JS module `blazix-baseui-combobox.js` and minified asset.
- Added typed root selection state with independent `Value`, `Values`, `InputValue`, `Open`, and highlight state.
- Added native hidden form inputs for single and multiple selection.
- Added selected label/value serializers for primitive and object values.
- Added readonly/disabled hidden-input guards.
- Added item registration, filtering, grouped filtering, global limit enforcement, active item navigation, and item commit behavior.
- Added input, trigger, popup, list, collection, item, indicator, value, clear, label, chips, chip remove, group, group label, row, status, arrow, backdrop, portal, positioner, separator, and empty parts.
- Added data/ARIA attributes for open, disabled, readonly, required, empty, highlighted, selected, side, align, and form states.
- Added render-versioned chip index registry to preserve correct removal indexes across dynamic selected-value renders.
- Added source parity tests in bUnit and browser-level Playwright coverage for Server and WASM.

## Accessibility and Attribute Coverage

- Input renders `role="combobox"`, `aria-autocomplete`, `aria-expanded`, `aria-haspopup`, `aria-controls`, `aria-activedescendant`, `aria-readonly`, and `aria-required` as applicable.
- List renders `role="listbox"` or `role="grid"` and `aria-multiselectable` in multiple mode.
- Items render `role="option"` or `role="gridcell"`, `aria-selected`, `aria-disabled`, `data-highlighted`, and `data-selected`.
- Trigger renders `aria-haspopup`, `aria-expanded`, `aria-controls`, `aria-readonly`, `aria-required`, and field/data state attributes.
- Label derives its id from the root and labels the trigger through root label state.
- Chips render `role="toolbar"` when selections exist.
- Chip remove renders a native button by default with `tabindex="-1"`, disabled state, and root focus restoration.
- Hidden form inputs serialize selected value(s) and preserve disabled, readonly, required, form, and autocomplete attributes.

## Source Docs Comparison

The upstream docs route was run with PNPM:

```bash
pnpm --dir .base-ui --filter docs dev
```

Live source route checked:

- `http://127.0.0.1:3005/react/components/combobox`
- Title: `Combobox · Base UI`
- H1: `Combobox`
- Confirmed sections: multiple select, input inside popup, grouped, virtualized.
- React multiple demo confirmed: input `role="combobox"`, `aria-haspopup="listbox"`, `aria-autocomplete="list"`, controlled listbox, filtered `TypeScript` option, chip `aria-label="TypeScript"`, chip remove `aria-label="Remove TypeScript"`, and `tabindex="-1"`.

Blazor component test page checked in the in-app browser:

- `http://127.0.0.1:5123/tests/combobox/server?defaultOpen=true&multiple=true&defaultValues=Apple`
- Initial state: interactive true, open true, selected values `Apple`, hidden inputs `Apple`.
- After selecting Banana: open true, selected values `Apple,Banana`, hidden inputs `Apple`, `Banana`.
- After removing Apple chip: selected values `Banana`, hidden input `Banana`, last reason `ChipRemovePress`, active element `combobox-input`.

Blazor docs route checked in the in-app browser:

- `http://127.0.0.1:5124/components/combobox`
- Title: `Combobox · Blazix.BaseUI`
- H1: `Combobox`
- Confirmed sections: usage guidelines, anatomy, typed wrapper component, multiple select, input inside popup, grouped, async search single, async search multiple, creatable, virtualized, memoizing items, and full API reference.
- Hero demo confirmed: scoped trigger opens one listbox; options include `Apple`, `Banana`, `Orange`, and other fruit values; selecting `Apple` closes the popup, sets the input value to `Apple`, and reveals the clear button.
- Multiple docs demo confirmed: input filters to `TypeScript`; selecting it renders chip `aria-label="TypeScript"` and chip remove `tabindex="-1"`.

Docs parity matrix:

| React docs example | Blazor docs equivalent |
| --- | --- |
| Hero | `ComboboxHeroCss`, `ComboboxHeroTailwind` |
| Typed wrapper component | Blazor generic wrapper snippet |
| Multiple select | `ComboboxMultipleCss` and visible-chip-limit snippet |
| Input inside popup | `ComboboxInputInsidePopupCss` and `ComboboxLabel` snippet |
| Grouped | `ComboboxGroupedCss` and grouped item data snippet |
| Async search (single) | `ComboboxAsyncSingleCss` |
| Async search (multiple) | `ComboboxAsyncMultipleCss` |
| Creatable | `ComboboxCreatableCss` |
| Virtualized | `ComboboxVirtualizedCss` |
| Memoizing items | Blazor stable-item and virtualization guidance |

## Evidence

- Full bUnit log: `docs/audits/logs/combobox-bunit.txt`
- Full Playwright CLI log: `docs/audits/logs/combobox-playwright.txt`
- Build log: `docs/audits/logs/combobox-build.txt`
- Lint log: `docs/audits/logs/combobox-lint.txt`
- JS syntax log: `docs/audits/logs/combobox-node-check.txt`
- Source docs evidence: `docs/audits/logs/combobox-source-docs.txt`
- In-app browser evidence: `docs/audits/logs/combobox-inapp-browser.txt`
- Docs browser evidence: `docs/audits/logs/combobox-docs-inapp-browser.txt`
- Docs build log: `docs/audits/logs/combobox-docs-build.txt`
- Docs test log: `docs/audits/logs/combobox-docs-bunit.txt`
