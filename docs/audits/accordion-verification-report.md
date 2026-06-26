# Accordion Verification Report

Date: 2026-06-26

## Commands

| Command | Result | Log |
| --- | --- | --- |
| `pnpm --dir .base-ui exec vitest run --project @base-ui/react packages/react/src/accordion` | Passed. 5 files, 99 passed, 14 upstream skips. | `docs/audits/logs/accordion-source-pnpm-2026-06-26.txt` |
| `pnpm --dir .base-ui --filter docs run validate` | Passed. No generated docs needed updating. | `docs/audits/logs/accordion-source-docs-pnpm-2026-06-26.txt` |
| `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --filter "FullyQualifiedName~Accordion" -v minimal` | Passed. 92 tests. | `docs/audits/logs/accordion-unit-2026-06-26.txt` |
| `dotnet test tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~Accordion" -v minimal` | Passed. 68 tests. | `docs/audits/logs/accordion-playwright-2026-06-26.txt` |
| `dotnet build Blazix.BaseUI.slnx -v minimal` | Passed. 0 warnings, 0 errors. | `docs/audits/logs/accordion-build-2026-06-26.txt` |
| `bash scripts/lint-rules.sh` | Passed. 0 textual-rule violations; structural rules covered by analyzers in the clean build. | `docs/audits/logs/accordion-lint-2026-06-26.txt` |
| `git diff --check` | Passed. No whitespace errors. | Terminal output was empty. |

## In-App Browser Checks

Tool: in-app browser.

Source docs route:

- URL: `http://localhost:3005/react/components/accordion`
- Title: `Accordion · Base UI`
- Headings observed: `Accordion`, `Anatomy`, `Examples`, `Open multiple panels`, `API reference`, `Root`, `Item`, `Header`, `Trigger`, `Panel`
- Documented surface observed in rendered page text: Root, Item, Header, Trigger, Panel, `multiple`, `keepMounted`, `hiddenUntilFound`, `data-panel-open`, `data-open`, `data-disabled`

Blazor test route:

- URL: `http://127.0.0.1:5309/tests/accordion/server?keepMounted=true&hiddenUntilFound=true&animated=true&animationDuration=123&multiple=true&showThirdItem=true&showFourthItem=true`
- Initial closed state rendered root `div` with no role, root `data-orientation="vertical"`, item `data-closed`, header `h3`, trigger `button type="button"` with `aria-expanded="false"`, panel `role="region"`, panel `hidden="until-found"`, panel `aria-labelledby`, panel `data-closed`, and panel `data-starting-style`.
- Click open state rendered item `data-open`, trigger `aria-expanded="true"`, trigger `aria-controls`, trigger `data-panel-open`, panel `data-open`, panel `role="region"`, and no panel `hidden` attribute.
- Space key activation opened the trigger/panel from a closed state.

Evidence:

- `docs/audits/logs/accordion-in-app-browser-comparison-2026-06-26.json`

## Static Checks

- Compared React source files under `.base-ui/packages/react/src/accordion`.
- Compared shared Collapsible source paths used by Accordion panel behavior.
- Reviewed upstream Git history for Accordion and Collapsible commits affecting runtime behavior.
- Confirmed `AccordionRoot` does not emit root `data-open` or `data-closed` because React maps root `value` to no state attribute.
- Confirmed minified Accordion trigger JS was regenerated from `blazix-baseui-accordion-trigger.js` with Terser.
- Confirmed durable generated evidence uses `.txt` and `.json`; no new `*.log` file was created.

## Spec Update

Updated non-repository spec files:

- `../base-ui-specs/accordion/SPEC.md`
- `../base-ui-specs/accordion/pitfalls.md`

The sibling `../base-ui-specs` directory is not a Git repository in this workspace, so those edits cannot be staged through Git here.

## Final Status

Accordion verification passed. The repository changes are ready to stage without a commit.
