# Toast Source Documentation Comparison

Date: 2026-07-21

## Hosts

- React Base UI documentation was started with `pnpm docs:dev` from `.base-ui` and inspected at `/react/components/toast`.
- Blazix documentation was started with `dotnet run` and inspected at `/components/toast`.
- Both pages were opened in the in-app browser in the same audit session.

## Surface Comparison

| Source documentation surface | Blazor documentation surface | Result |
| --- | --- | --- |
| Toast overview and create demo | Toast overview and create demo | Present |
| Provider/Portal/Viewport/Root/Content/Title/Description/Action/Close anatomy | Same component anatomy in Razor form | Present |
| F6 viewport guidance | F6 keyboard guidance | Present |
| `data-base-ui-swipe-ignore` guidance | Canonical attribute plus documented legacy compatibility | Present |
| External/global manager | `ToastManager` and provider manager integration | Present in API and samples |
| Stack variables and expanded/behind states | Equivalent CSS variables and data-state styling | Present |
| Anchored toast | Anchored copy sample using `ToastPositioner` | Present |
| Custom position | Top-center sample | Present |
| Undo action | Action sample with current 10-second timeout | Present |
| Promise loading/success/error | Manager promise sample | Present |
| Custom data | Typed custom-data sample | Present |
| Duplicate-ID/update-key behavior | Manager/upsert API documentation and tests | Accounted for |
| Varying heights | Runtime height measurement and stack CSS | Accounted for in implementation/spec |
| Provider and part API tables | Razor component parameters and source snippets | Framework-equivalent documentation |

## Rendered Output Check

The in-app browser confirmed that the Blazor documentation page rendered Toast viewports with:

- `role="region"`
- `tabindex="-1"`
- `aria-live="polite"`
- `aria-atomic="false"`
- `aria-relevant="additions text"`
- `aria-label="Notifications"`

These values match the React source defaults. The source page and Blazor page both exposed the complete example set and the same behavioral concepts. React uses JSX/React hooks while Blazor samples use Razor, `ToastManager`, and native component lifecycle methods.

## Interaction Boundary

The React page returned HTTP 200 and rendered its documentation/API surface. The Blazor docs page rendered its component output but retained its existing “Preparing controls...” overlay in the in-app session, so demo buttons were not used as correctness evidence. The dedicated Playwright test application exercised every repaired interaction in both Server and WebAssembly modes and passed 26/26 tests. No component behavior was inferred solely from static documentation.

## Delta Applied to Documentation

- Undo action timeout updated to 10 seconds in CSS and Tailwind demos and displayed snippets.
- Canonical `data-base-ui-swipe-ignore` wording added; legacy selector support is explicitly documented.
- Current transition-state wording and corrected stack scale behavior retained.

The documentation comparison found no remaining Toast source behavior that requires a component repair.
