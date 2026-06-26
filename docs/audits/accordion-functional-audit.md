# Accordion Functional Audit

Date: 2026-06-26

Scope: `src/Blazix.BaseUI/Accordion`, `src/Blazix.BaseUI/wwwroot/blazix-baseui-accordion-trigger.js`, Accordion bUnit tests, Accordion Playwright tests, local React Base UI source under `.base-ui`, and framework-agnostic spec files under `../base-ui-specs/accordion`.

## Result

The Accordion port now matches the current React Base UI source for root value control, item state, header semantics, trigger activation, panel attributes, hidden-until-found behavior, item cancellation, controlled state, manual trigger IDs, and the recent upstream Accordion bug-fix deltas evaluated in this audit.

No unresolved functional gaps remain for the audited Accordion surface.

## Source Baseline

Primary React source files:

- `.base-ui/packages/react/src/accordion/root/AccordionRoot.tsx`
- `.base-ui/packages/react/src/accordion/item/AccordionItem.tsx`
- `.base-ui/packages/react/src/accordion/header/AccordionHeader.tsx`
- `.base-ui/packages/react/src/accordion/trigger/AccordionTrigger.tsx`
- `.base-ui/packages/react/src/accordion/panel/AccordionPanel.tsx`
- `.base-ui/packages/react/src/accordion/item/stateAttributesMapping.ts`
- `.base-ui/packages/react/src/collapsible/root/useCollapsibleRoot.ts`
- `.base-ui/packages/react/src/collapsible/panel/useCollapsiblePanel.ts`
- `.base-ui/packages/react/src/internals/use-button/useButton.ts`

Source docs route:

- URL: `http://localhost:3005/react/components/accordion`
- Title observed through the in-app browser: `Accordion · Base UI`
- Visible docs sections observed: `Accordion`, `Anatomy`, `Examples`, `Open multiple panels`, `API reference`, `Root`, `Item`, `Header`, `Trigger`, `Panel`
- Source docs validation command reported: `No files needed updating`

Evidence:

- `docs/audits/logs/accordion-source-pnpm-2026-06-26.txt`
- `docs/audits/logs/accordion-source-docs-pnpm-2026-06-26.txt`
- `docs/audits/logs/accordion-in-app-browser-comparison-2026-06-26.json`

## Upstream Delta And Impact Report

### `3980d3576` - `[docs] Remove accordion roving focus (#4981)`

Impact:

- Accordion arrow-key roving focus is no longer documented behavior.
- Documentation must not imply `Orientation` or `LoopFocus` controls keyboard focus.

Implemented:

- `AccordionRoot.razor` XML docs now state that `Orientation` is exposed through state/data attributes only and does not control trigger keyboard focus.
- `LoopFocus` is documented as deprecated and source-compatible only.
- `../base-ui-specs/accordion/SPEC.md` and `../base-ui-specs/accordion/pitfalls.md` were updated with the Space keyup rule and the non-roving-focus pitfall.

### `be47a6214` - `[accordion] Align keyboard navigation with APG (#4965)`

Impact:

- Trigger Space activation must happen on `keyup`, not `keydown`.
- Trigger Space `keydown` must only prevent page scroll.
- Enter remains the activation path for non-native triggers.
- Removed roving-focus machinery must not be recreated in Blazor.

Implemented:

- `blazix-baseui-accordion-trigger.js` now prevents default on Space `keydown` and calls `element.click()` on Space `keyup`.
- `AccordionTrigger.razor` no longer passes orientation, direction, or loop-focus values into trigger JS.
- Trigger JS configuration was reduced to native-button state only.
- The minified trigger module was regenerated from the readable source.
- Playwright coverage verifies Space does not toggle on `keydown` and toggles after `keyup`.

### `a3cfc4f98` - `[accordion] Remove region role from Accordion.Root (#4961)`

Impact:

- Root must render as a plain `div` without `role="region"`.
- Panel remains the region element.

Implemented:

- `AccordionRoot.razor` emits no root role.
- bUnit and in-app browser evidence confirm the root role is absent and the panel role remains `region`.

### `9069ba886` - `[accordion] Fix trigger behavior bugs (#4833)`

Impact:

- Manual trigger `id` must update `aria-labelledby` on the panel.
- Removing a manual trigger `id` must restore the generated trigger ID.
- `Accordion.Item` `onOpenChange` cancellation must prevent root value changes.
- Controlled root value must not mutate without a parameter update.
- Disabled item/root state must suppress activation without overriding disabled behavior from a child trigger.

Implemented:

- Trigger ID registration now updates and restores item context IDs.
- Panel `aria-labelledby` tracks the current trigger ID.
- `AccordionItem.HandleTrigger` and `HandleBeforeMatch` route through cancelable event details before root state changes.
- `AccordionRoot.HandleValueChangeAsync` returns before mutating uncontrolled state or invoking `ValueChanged` when details are canceled.
- New bUnit coverage verifies manual trigger ID change, cancellation, controlled state, disabled inheritance, and beforematch cancellation.

### `4133d56f7` - `[accordion][collapsible] Simplify shouldRender condition (#4338)`

Impact:

- Panel presence condition is `keepMounted || hiddenUntilFound || mounted || open`.
- Root-level `hiddenUntilFound` and `keepMounted` must inherit to panels, with panel-level overrides.

Implemented:

- `AccordionPanel.IsPresent` follows the same render condition through inherited root options and local panel options.
- Tests verify root inheritance and panel override behavior.

### Collapsible upstream fixes consumed by Accordion

Evaluated commits:

- `e18d78832` - `[collapsible] Fix trigger and panel state bugs (#4848)`
- `3d0be4e37` - `[collapsible] Refactor panel logic (#4565)`
- `d33150322` - `[collapsible] Fix open state when keepMounted with no transitions (#4555)`

Impact:

- Accordion panels reuse the shared Collapsible panel JS.
- Accepted `beforematch` opens must pass an instant-open signal into JS.
- Canceled `beforematch` opens must not open the panel or arm animation suppression.
- No-motion and keep-mounted close paths must leave closed panels hidden.

Implemented:

- `AccordionItem` records accepted open-change reason in the shared `CollapsibleRootContext`.
- `AccordionPanel` consumes the reason and calls shared JS `open(element, false, beforeMatchOpen)`.
- `AccordionPanel` clears the reason after consumption.
- bUnit coverage verifies the JS `open` call receives the instant-open flag for accepted `beforematch`.
- Playwright coverage verifies hidden-until-found, keep-mounted, animation, and DOM state behavior.

### Evaluated With No Blazor Runtime Delta

The following upstream changes were reviewed and require no new Blazor runtime behavior beyond the implementation already present:

- React hook-only stabilization: `useControlled`, `useStableCallback`, `useIsoLayoutEffect`, `useRenderElement`.
- Type-only and package infrastructure updates: generic value typing, package export normalization, forwarded ref type cleanup.
- React-only allocation cleanup and memoization removal. Blazor equivalents use direct lifecycle state and do not cache `RenderFragment` content.

## Repairs Applied

- Moved Accordion trigger Space activation from `keydown` to `keyup`.
- Removed stale trigger JS roving-focus config and the corresponding C# interop arguments.
- Added beforematch reason propagation from item to panel and the shared Collapsible JS instant-open parameter.
- Added `ObjectDisposedException` guards to Accordion trigger and panel JS interop disposal and lifecycle calls.
- Updated stale `Orientation` and `LoopFocus` docs.
- Added bUnit JSInterop setup for minified Accordion trigger and Collapsible panel module paths.
- Added regression tests for beforematch instant-open, manual trigger ID changes, item cancellation, disabled inheritance, and Space keyup timing.
- Updated test contracts to include the newly covered parity cases.
- Updated framework-agnostic Accordion spec and pitfalls files outside this Git repository.

## Attribute Surface

Verified:

- Root: default `div`, `dir`, `data-blazix-base-ui-accordion-root`, `data-orientation`, `data-disabled` when disabled, no `role`.
- Item: default `div`, `data-open`, `data-closed`, `data-disabled`, `data-orientation`, item index state.
- Header: default `h3`, forwarded attributes, no injected role.
- Trigger: default `button`, `type="button"`, `id`, `aria-expanded`, open-only `aria-controls`, `aria-disabled` when disabled, `data-panel-open`, `data-open`, `data-closed`, `data-disabled`, `data-orientation`, transition data attributes.
- Panel: default `div`, `id`, `role="region"`, `aria-labelledby`, `hidden`, `hidden="until-found"`, `data-open`, `data-closed`, `data-disabled`, `data-orientation`, `data-starting-style`, `data-ending-style`, CSS variables `--accordion-panel-height` and `--accordion-panel-width`.

The React root maps the `value` state to no open/closed state attribute. Blazor therefore does not emit `data-open` or `data-closed` on `AccordionRoot`.

## Verification Summary

- `pnpm --dir .base-ui exec vitest run --project @base-ui/react packages/react/src/accordion`: passed, 99 tests, 14 upstream skips.
- `pnpm --dir .base-ui --filter docs run validate`: passed, no generated docs needed updating.
- `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --filter "FullyQualifiedName~Accordion" -v minimal`: passed, 92 tests.
- `dotnet test tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~Accordion" -v minimal`: passed, 68 tests.
- `dotnet build Blazix.BaseUI.slnx -v minimal`: passed, 0 warnings, 0 errors.
- `bash scripts/lint-rules.sh`: passed, 0 lint violations.
- `git diff --check`: passed.
- In-app browser comparison captured source docs and Blazor Accordion server route evidence.

Full command evidence is in `docs/audits/logs/`.
