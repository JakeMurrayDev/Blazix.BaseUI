# Autocomplete Functional Audit

Date: 2026-07-03

## Result

The Blazor `Autocomplete` component was audited against the current React Base UI Autocomplete source, Autocomplete docs, and the shared upstream internals that Autocomplete consumes. The repaired scope is Autocomplete only.

## Sources Audited

- React Base UI mirror: `.base-ui` fast-forwarded to `95cf9e0339567518ccdf82628c8ef4f3d67cad07`.
- React source paths: `packages/react/src/autocomplete` and shared input/list/positioning internals used by Autocomplete.
- React docs path: `docs/src/app/(docs)/react/components/autocomplete`.
- Blazor source paths: `src/Blazix.BaseUI/Autocomplete`, `src/Blazix.BaseUI/wwwroot/blazix-baseui-autocomplete.js`, and shared `src/Blazix.BaseUI/wwwroot/blazix-baseui-floating.js`.

## Upstream Delta And Impact Report

| Upstream change | Impact | Blazor disposition |
| --- | --- | --- |
| Group filtering stops after the global limit | Grouped Autocomplete filtering must stop once the global item limit is reached. | Already present. `AutocompleteRootContext.GetFlatFilteredItems` breaks when `result.Count >= Limit`. |
| Inline Autocomplete open-state docs | Source docs clarify the inline visibility contract. | Accounted in the parity matrix as an Autocomplete behavior to preserve or revisit explicitly. |
| Disabled/readonly input update guard | Hidden input/value sync must ignore disabled and readonly paths. | Present for Autocomplete input state handling. |
| Avoid item re-render pressure on keystroke | Typing should not cache render fragments or force broad item re-render work. | No RenderFragment caching was added. Filtering remains state-driven through Autocomplete context and external item collections. |
| ArrowLeft/ArrowRight caret behavior | Left/right keys stay native caret movement when no active item exists. | Present. Autocomplete JS only captures Up/Down/Enter/Escape/Tab navigation keys. |
| Render callback props typed to rendered element | Custom render targets must not receive invalid intrinsic attributes. | Implemented for `AutocompleteInput`: no forced `type="text"`; bUnit covers textarea render target. |
| Popup capped-size flip fix | `--available-height` must be valid before the first Floating UI `flip()` measurement. | Implemented in shared `blazix-baseui-floating.js`: seeds `--available-width: 100vw` and `--available-height: 100vh` before `computePosition`. |
| Autocomplete clear element attributes | Clear exposes popup-open, visible, and disabled state without stale open/closed attrs. | Implemented verified attrs `data-popup-open`, `data-visible`, and disabled state. |
| IME composition handling | Controlled input changes are deferred until composition end. | Implemented in `AutocompleteInput` for non-Android Blazor path; value changes are deferred during composition. |
| Enter key semantics | Enter commits an active item only; otherwise native submit behavior continues. | Implemented in JS by passing `activeIndex` to `setRootOpen` and only preventing default when `activeIndex >= 0`. |
| Virtualized docs scroller sizing | Virtualized docs list height is based on total virtual item size, while floating available height only caps the scroller. | Implemented in the Blazor virtualized docs demo with `--total-size` derived from filtered item count and `height: min(22.5rem, var(--total-size)); max-height: var(--available-height, 22.5rem);`. |

## Repairs Applied

- Removed forced `type="text"` from `AutocompleteInput` component attributes.
- Added IME composition start/end handling to defer input value propagation.
- Changed Enter key handling so an open popup with no active item does not block native form submit.
- Added root context focus callback and Clear focus restoration.
- Registered Clear with autocomplete JS so Clear outside the popup is treated as inside the root and prevents input blur before click.
- Replaced Clear `data-open`/`data-closed` with source-equivalent `data-popup-open`.
- Added `data-empty` and `data-anchor-hidden` to `AutocompletePositioner`.
- Seeded shared Floating UI available-size CSS variables before first placement measurement.
- Updated Server and WASM Playwright fixtures to exercise form submit and Clear focus behavior.
- Added the full upstream Autocomplete example set to the Blazor docs page: async search, inline autocomplete, grouped, fuzzy matching, limit results, auto highlight, command palette, grid layout, and virtualized.
- Fixed Autocomplete docs demo spacing by moving visible empty-state padding from the persistent `AutocompleteEmpty` live-region root to an inner child element, matching the React source pattern.
- Fixed the virtualized docs demo fresh-load disappearance by matching the React source scroller sizing model.

## Notes

Verification logs were generated locally during the audit and intentionally excluded from the commit scope.
