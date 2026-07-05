# Combobox Parity Matrix

## Parts

| React Base UI part | Blazor part | Status | Verification |
| --- | --- | --- | --- |
| `Combobox.Root` | `ComboboxRoot<TValue>` | Implemented | bUnit root tests; Playwright Server/WASM tests |
| `Combobox.Label` | `ComboboxLabel` | Implemented | `Label_ShouldExposeDerivedIdAndLabelTrigger` |
| `Combobox.Value` | `ComboboxValue` | Implemented | selected label and placeholder tests |
| `Combobox.Input` | `ComboboxInput` | Implemented | input attribute and Playwright keyboard tests |
| `Combobox.InputGroup` | `ComboboxInputGroup` | Implemented | build and attribute propagation |
| `Combobox.Trigger` | `ComboboxTrigger` | Implemented | disabled/readonly/required Playwright coverage |
| `Combobox.List` | `ComboboxList` | Implemented | role/list state tests |
| `Combobox.Status` | `ComboboxStatus` | Implemented | build coverage |
| `Combobox.Portal` | `ComboboxPortal` | Implemented | build coverage |
| `Combobox.Backdrop` | `ComboboxBackdrop` | Implemented | build coverage |
| `Combobox.Positioner` | `ComboboxPositioner` | Implemented | Playwright popup visibility and positioner attributes |
| `Combobox.Popup` | `ComboboxPopup` | Implemented | Playwright open/closed assertions |
| `Combobox.Arrow` | `ComboboxArrow` | Implemented | build coverage |
| `Combobox.Icon` | `ComboboxIcon` | Implemented | build coverage |
| `Combobox.Group` | `ComboboxGroup` | Implemented | grouped filtering test |
| `Combobox.GroupLabel` | `ComboboxGroupLabel` | Implemented | grouped filtering and build coverage |
| `Combobox.Item` | `ComboboxItem<TValue>` | Implemented | item press, selected state, highlight tests |
| `Combobox.ItemIndicator` | `ComboboxItemIndicator` | Implemented | selected indicator bUnit and Playwright tests |
| `Combobox.Chips` | `ComboboxChips` | Implemented | multiple selection and chip removal tests |
| `Combobox.Chip` | `ComboboxChip` | Implemented | chip render and removal tests |
| `Combobox.ChipRemove` | `ComboboxChipRemove` | Implemented | bUnit and Playwright chip removal tests |
| `Combobox.Row` | `ComboboxRow` | Implemented | build coverage and grid role mapping |
| `Combobox.Collection` | `ComboboxCollection<TValue>` | Implemented | collection rendering in Playwright page |
| `Combobox.Empty` | `ComboboxEmpty` | Implemented | empty-state filtering behavior |
| `Combobox.Clear` | `ComboboxClear` | Implemented | clear press bUnit and Playwright tests |
| `Combobox.Separator` | `ComboboxSeparator` | Implemented | build coverage |

## Root Features

| React behavior | Blazor equivalent | Status |
| --- | --- | --- |
| controlled/uncontrolled selected value | `Value`, `DefaultValue`, `ValueChanged`, `OnValueChange` | Implemented |
| controlled/uncontrolled multiple values | `Values`, `DefaultValues`, `ValuesChanged`, `Multiple` | Implemented |
| controlled/uncontrolled input value | `InputValue`, `DefaultInputValue`, `InputValueChanged`, `OnInputValueChange` | Implemented |
| controlled/uncontrolled open state | `Open`, `DefaultOpen`, `OpenChanged`, `OnOpenChange`, `OnOpenChangeComplete` | Implemented |
| typed item labels | `ItemToStringLabel`, item `Label` fallback, object `Label` property fallback | Implemented |
| typed form values | `ItemToStringValue`, object `Value` property fallback | Implemented |
| custom equality | `IsItemEqualToValue` | Implemented |
| filtering | `Filter`, default label filtering | Implemented |
| externally filtered items | `FilteredItems`, `FilterDisabled` | Implemented |
| grouped items | `ItemGroups`, `ComboboxOptionGroup<TValue>` | Implemented |
| global limit | `Limit` applied after flattening grouped results | Implemented |
| selected item sync | `ComboboxRootContext<TValue>.IsItemSelected` | Implemented |
| hidden form serialization | visually hidden single input; repeated hidden inputs for multiple | Implemented |
| autofill/hidden input value recovery | `FindValueByFormOrLabel` | Implemented |
| readonly/disabled hidden input guard | `HandleHiddenInputChange` guard | Implemented |
| input-inside-popup | `InputInsidePopup` state from positioner context | Implemented |
| popup open on input/trigger | input/trigger handlers and JS state; `OpenOnInputClick` default `true` | Implemented |
| outside press/escape/focus out close | component-specific JS document handlers | Implemented |
| active item keyboard commit | JS capture handlers plus root callbacks | Implemented |
| Enter without active item submits form | JS only prevents active-item Enter | Implemented |
| chip removal reason | `ComboboxChangeReason.ChipRemovePress` | Implemented |
| chip rendered-index behavior | render-versioned `ComboboxChipsContext` registry | Implemented |

## ARIA and Data Attributes

| Element | React attribute surface | Blazor status |
| --- | --- | --- |
| Input | `role`, `aria-autocomplete`, `aria-expanded`, `aria-haspopup`, `aria-controls`, `aria-activedescendant`, readonly/required state | Implemented |
| Trigger | `aria-haspopup`, `aria-expanded`, `aria-controls`, label, readonly/required/disabled state | Implemented |
| List | `role=listbox/grid`, `aria-multiselectable` | Implemented |
| Item | `role=option/gridcell`, `aria-selected`, `aria-disabled`, `data-highlighted`, `data-selected` | Implemented |
| ItemIndicator | `aria-hidden`, `data-selected` | Implemented |
| Popup | open/closed transition and side data attributes | Implemented |
| Positioner | `role=presentation`, side/align/anchor data attributes | Implemented |
| Chips | `role=toolbar` when chips are present | Implemented |
| Chip | `tabindex=-1`, disabled/readonly state | Implemented |
| ChipRemove | native button default, `tabindex=-1`, disabled state | Implemented |
| Hidden input | `aria-hidden`, `tabindex="-1"`, `aria-invalid`, `aria-describedby`, `name`, `form`, `autocomplete`, `value`, disabled, readonly, required | Implemented |

## Upstream Fix Mapping

| Upstream fix or utility | Blazor equivalent | Test or proof |
| --- | --- | --- |
| grouped `limit` global behavior | context flattening stops after `Limit` | bUnit grouped limit test |
| hidden input ignores readonly/disabled | hidden input change guard | bUnit readonly hidden input test |
| object autofill label/value matching | form-or-label value lookup | bUnit object value test |
| multiple chip removal by rendered index | render-versioned chip index registry | bUnit and Playwright chip removal tests |
| popup input form submit | Enter prevention only with active item | Playwright form submit test |
| `openOnInputClick` default `true` | `OpenOnInputClick = true` | Playwright input-click-open test |
| multiple mode keeps popup open on item press | commit logic preserves open state | Playwright multiple test |
| input-inside-popup focus retention | JS focus and pointer prevention | Playwright input-inside-popup test |
| positioner available variables | shared floating JS size middleware | build and browser popup tests |
| render callback typing | `RenderElement<TState>` for every part | build/analyzer coverage |

## Source Docs Comparison

| Source docs pattern | React docs live result | Blazor result |
| --- | --- | --- |
| Hero demo | React hero demo renders filterable fruit combobox | Blazor docs route renders `ComboboxHeroCss` and `ComboboxHeroTailwind`; browser check opened the hero listbox and selected `Apple` |
| Typed wrapper component | Source docs provide generic wrapper pattern | Blazor docs provide generic `@typeparam TValue` wrapper snippet |
| Multiple select chips | Selected `TypeScript` renders chip `aria-label="TypeScript"` and remove button `aria-label="Remove TypeScript"` with `tabindex="-1"` | Selected `Apple,Banana` renders chips and remove buttons; removing Apple leaves Banana |
| Filterable listbox input | React input exposes `role="combobox"`, `aria-haspopup="listbox"`, `aria-autocomplete="list"`, `aria-controls` when open | Blazor input exposes the same role/ARIA surface in Playwright tests |
| Input inside popup | Source docs route contains input-inside-popup pattern | Blazor Playwright page verifies trigger opens and focuses popup input |
| Grouped options | Source docs route contains grouped pattern | Blazor bUnit verifies grouped global filtering limit |
| Async search (single) | Source docs route contains async single search pattern | Blazor docs route renders `ComboboxAsyncSingleCss` using external `Items` replacement and `FilterDisabled` |
| Async search (multiple) | Source docs route contains async multiple search pattern | Blazor docs route renders `ComboboxAsyncMultipleCss` using external result replacement plus selected-value merging |
| Creatable | Source docs route contains creatable dialog pattern | Blazor docs route renders `ComboboxCreatableCss` with synthetic create item and confirmation dialog |
| Virtualized/performance guidance | Source docs route contains virtualized/memoized item guidance | Blazor item registration avoids RenderFragment caching and keeps item state refresh scoped to context events |
| Memoizing items | Source docs describe `React.memo` alternative to virtualization | Blazor docs map the guidance to stable item components and `Virtualize` |
