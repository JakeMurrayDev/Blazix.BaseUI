# Autocomplete Verification Report

Date: 2026-07-03

## Scope

Verification covers the Blazor `Autocomplete` component, its JavaScript interop, shared popup/floating behavior used by Autocomplete, and the Autocomplete documentation examples.

Verification logs were generated locally during development and intentionally excluded from this commit.

## Commands

| Check | Command | Result |
| --- | --- | --- |
| Upstream fetch | `git -C .base-ui fetch origin master` | Fetched upstream master. |
| Upstream fast-forward | `git -C .base-ui pull --ff-only origin master` | Fast-forwarded React mirror to `95cf9e0339567518ccdf82628c8ef4f3d67cad07`. |
| Source tests | `pnpm --dir .base-ui exec cross-env TZ=UTC VITEST_ENV=chromium vitest --project @base-ui/react --run packages/react/src/autocomplete` | Passed: 3 files, 65 tests passed, 2 skipped. |
| Source docs validation | `npx --yes pnpm@11.5.2 -C .base-ui docs:validate "(docs)/react/components/autocomplete"` | Passed; no files needed updating. |
| In-app browser docs comparison | In-app browser inspected React Autocomplete and Blazor Autocomplete docs. | Confirmed repaired Autocomplete docs signals and source example coverage. |
| Docs example inventory and spacing test | `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --filter "FullyQualifiedName~DocsAutocompleteDemoTests" -v minimal` | Passed. |
| Virtualized fresh-load failing guard | `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --filter "FullyQualifiedName~DocsAutocompleteDemoTests" -v minimal` after adding the guard and before implementation | Failed as expected because the demo did not emit `VirtualizedListStyle` and used `height: min(22.5rem, var(--available-height))`. |
| Virtualized fresh-load regression test | `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --filter "FullyQualifiedName~DocsAutocompleteDemoTests" -v minimal` | Passed: 3 tests. |
| Virtualized fresh-load browser verification | In-app browser loaded `/components/autocomplete#virtualized`, opened the virtualized input on a fresh docs process, then typed `0420` while sampling DOM state every 100 ms. | Passed: zero zero-option samples on full-list open and typed filter; first nonzero sample at 0 ms in both phases. |
| Demo CSS minification | `npx --yes clean-css-cli@5.6.3 -o docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/wwwroot/demos/autocomplete.min.css docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/wwwroot/demos/autocomplete.css` | Passed; minified Autocomplete demo CSS regenerated. |
| Focused bUnit | `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --filter "FullyQualifiedName~Blazix.BaseUI.Tests.Autocomplete.AutocompleteRootTests" -v minimal` | Passed: 27 tests. |
| Focused Playwright regressions | `dotnet test tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~EnterWithOpenPopupAndNoActiveItemSubmitsForm\|FullyQualifiedName~ClearPressClearsValueWithoutMovingFocusFromInput" -v minimal` | Passed: 4 tests. |
| Full Autocomplete Playwright slice | `dotnet test tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~Autocomplete" -v minimal` | Passed: 28 tests. |
| JavaScript syntax | `node --check` on `blazix-baseui-autocomplete.js`, `blazix-baseui-autocomplete.min.js`, `blazix-baseui-floating.js`, and `blazix-baseui-floating.min.js` | Passed with no syntax output. |
| Build | `dotnet build Blazix.BaseUI.slnx -v minimal` | Passed: 0 warnings, 0 errors. |
| Lint rules | `bash scripts/lint-rules.sh` | Passed: 0 textual-rule violations. |
| Whitespace check | `git diff --check` and `git diff --cached --check` | Passed. |

## Verified Repairs

| Repair | Verification |
| --- | --- |
| `AutocompleteInput` no longer forces `type="text"` onto custom render targets. | bUnit `InputRenderAsTextarea_ShouldNotEmitTypeAttribute`. |
| IME composition defers value propagation until composition end. | bUnit `CompositionInput_ShouldDeferValueChangeUntilCompositionEnds`. |
| Enter with an open popup and no active item no longer blocks native submit behavior. | Playwright `EnterWithOpenPopupAndNoActiveItemSubmitsForm`. |
| Clear keeps input focus and is treated as inside the root for outside-press handling. | Playwright `ClearPressClearsValueWithoutMovingFocusFromInput`. |
| Clear uses `data-popup-open` and `data-visible` without stale `data-open` / `data-closed`. | bUnit `Clear_ShouldExposePopupOpenVisibleAndTransitionAttributesOnly`. |
| Positioner exposes `data-empty` and `data-anchor-hidden`. | bUnit `Positioner_ShouldExposeEmptyStateAttribute`; source inspection for anchor-hidden state. |
| Floating UI seeds available-size CSS variables before first placement measurement. | Source inspection plus passing upstream source tests. |
| Blazor Autocomplete docs include every upstream source example category. | bUnit docs source guard plus in-app browser heading inventory. |
| Typing no longer leaves a padded empty live-region row above filtered docs demo items. | bUnit docs source guard plus in-app browser geometry check. |
| Virtualized docs example no longer drops items during fresh-load open or first typed filter. | bUnit source guard plus in-app browser timing check. |

## Sensitive Information Check

The staged patch was scanned for credential material before commit. No findings were present in the staged diff.
