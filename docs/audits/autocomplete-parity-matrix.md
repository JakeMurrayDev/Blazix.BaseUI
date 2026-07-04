# Autocomplete Parity Matrix

React source: `.base-ui` at `95cf9e0339567518ccdf82628c8ef4f3d67cad07`.

## Component Surface

| React Base UI surface | Blazor equivalent | Status |
| --- | --- | --- |
| `Autocomplete.Root` | `AutocompleteRoot<TValue>` | Present |
| `Autocomplete.Input` | `AutocompleteInput` | Present |
| `Autocomplete.InputGroup` | `AutocompleteInputGroup` | Present |
| `Autocomplete.Trigger` | `AutocompleteTrigger` | Present |
| `Autocomplete.Clear` | `AutocompleteClear` | Present |
| `Autocomplete.Value` | `AutocompleteValue` | Present |
| `Autocomplete.Portal` | `AutocompletePortal` | Present |
| `Autocomplete.Backdrop` | `AutocompleteBackdrop` | Present |
| `Autocomplete.Positioner` | `AutocompletePositioner` | Present |
| `Autocomplete.Popup` | `AutocompletePopup` | Present |
| `Autocomplete.Arrow` | `AutocompleteArrow` | Present |
| `Autocomplete.List` | `AutocompleteList` | Present |
| `Autocomplete.Item` | `AutocompleteItem` | Present |
| `Autocomplete.Group` | `AutocompleteGroup` | Present |
| `Autocomplete.GroupLabel` | `AutocompleteGroupLabel` | Present |
| `Autocomplete.Collection` | `AutocompleteCollection` | Present |
| `Autocomplete.Row` | `AutocompleteRow` | Present |
| `Autocomplete.Empty` | `AutocompleteEmpty` | Present |
| `Autocomplete.Status` | `AutocompleteStatus` | Present |
| `Autocomplete.Separator` | `AutocompleteSeparator` | Present |

## Repaired Internals

| React hook / utility / fix | Blazor equivalent | Verification |
| --- | --- | --- |
| Input does not inject invalid `type="text"` into custom render targets | `AutocompleteInput` omits forced `type`; native input defaults to text | bUnit `InputRenderAsTextarea_ShouldNotEmitTypeAttribute` |
| IME composition defers controlled update | `isComposing` + `pendingCompositionValue` in `AutocompleteInput` | bUnit `CompositionInput_ShouldDeferValueChangeUntilCompositionEnds` |
| Enter commits only active item | `blazix-baseui-autocomplete.js` checks `root.activeIndex >= 0` before `preventDefault` | Playwright `EnterWithOpenPopupAndNoActiveItemSubmitsForm` |
| Clear `data-popup-open` / `data-visible` | `AutocompleteClear` emits popup-open and visible; no stale `data-open`/`data-closed` | bUnit `Clear_ShouldExposePopupOpenVisibleAndTransitionAttributesOnly`; Playwright Clear test |
| Clear pointer-down focus retention | `setClearElement` JS registration includes Clear in outside-press containment and prevents input blur | Playwright `ClearPressClearsValueWithoutMovingFocusFromInput` |
| Positioner empty state | `AutocompletePositioner` emits `data-empty` | bUnit `Positioner_ShouldExposeEmptyStateAttribute` |
| Positioner anchor-hidden state | `AutocompletePositioner` emits `data-anchor-hidden` from root context | Source inspection; docs/API updated |
| Grouped limit after global count | `AutocompleteRootContext.GetFlatFilteredItems` breaks after `result.Count >= Limit` | Source inspection |
| ArrowLeft/ArrowRight native caret behavior | JS keyboard handler does not capture left/right | Source inspection |
| Popup capped-size flip fix | `blazix-baseui-floating.js` seeds `--available-width` / `--available-height` before `computePosition` | Source inspection; upstream source tests passed |
| Source Autocomplete examples | Blazor docs include async search, inline autocomplete, grouped, fuzzy matching, limit, auto highlight, command palette, grid, and virtualized examples | `DocsAutocompleteDemoTests.AutocompletePage_IncludesEverySourceExample`; in-app browser heading inventory |
| `Empty` live-region root remains mounted but layout-neutral | Docs demos apply empty-state padding to child content instead of `AutocompleteEmpty` root | `DocsAutocompleteDemoTests.AutocompleteDemos_ApplyEmptyVisualSpacingToChildrenOnly`; in-app browser geometry check |
| Virtualized docs scroller uses total size for height and available height as cap | Blazor virtualized docs demo sets `--total-size` from filtered count and item size; CSS uses `height: min(22.5rem, var(--total-size))` with `max-height: var(--available-height, 22.5rem)` | `DocsAutocompleteDemoTests.AutocompleteVirtualizedDemo_UsesTotalSizeForStableInitialScrollerHeight`; in-app browser fresh-load timing check |

## Remaining Autocomplete Notes

| Note | Impact |
| --- | --- |
| Inline open-state behavior differs from the latest source docs wording | React docs require inline plus open visibility. Current Blazor inline rendering treats inline content as available even when `Open` is false; keep this documented for a future focused Autocomplete behavior pass. |
| Clear transition lifecycle is limited to verified state attrs | `data-popup-open`, `data-visible`, and disabled state are verified. No separate Clear transition animation lifecycle was added in this Autocomplete pass. |
