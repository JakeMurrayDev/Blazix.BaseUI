# Collapsible Verification Report

Date: 2026-06-26

## Commands

| Command | Result | Log |
| --- | --- | --- |
| `dotnet build Blazix.BaseUI.slnx` | Passed, 0 warnings, 0 errors. | `docs/audits/logs/collapsible-dotnet-build.txt` |
| `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --filter "FullyQualifiedName~Collapsible" -v minimal` | Passed, 64 tests. | `docs/audits/logs/collapsible-unit-tests.txt` |
| `dotnet test tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~Collapsible" -v minimal` | Passed, 68 tests. | `docs/audits/logs/collapsible-playwright-tests.txt` |
| `bash scripts/lint-rules.sh` | Passed, 0 violations. | `docs/audits/logs/collapsible-lint-rules.txt` |
| `pnpm exec cross-env VITEST_ENV=chromium vitest run --project @base-ui/react packages/react/src/collapsible/root/CollapsibleRoot.test.tsx packages/react/src/collapsible/panel/CollapsiblePanel.test.tsx packages/react/src/collapsible/trigger/CollapsibleTrigger.test.tsx` from `.base-ui` | Passed, 83 tests. | `docs/audits/logs/collapsible-upstream-source-tests.txt` |
| `pnpm docs:dev` from `.base-ui` | Served source docs on `http://localhost:3005`; Collapsible route returned HTTP 200. | `docs/audits/logs/collapsible-source-docs-status.txt` |

## In-App Browser Checks

Tool: in-app browser.

Source docs route:

- URL: `http://localhost:3005/react/components/collapsible`
- Title: `Collapsible · Base UI`
- Headings observed: `Collapsible`, `Anatomy`, `API reference`, `Root`, `Trigger`, `Panel`
- Documented surface observed: Root, Trigger, Panel, `defaultOpen`, `open`, `onOpenChange`, `disabled`, `hiddenUntilFound`, `keepMounted`, `data-panel-open`, `data-open`, `data-closed`, `data-starting-style`, `data-ending-style`

Blazor test route:

- URL: `http://127.0.0.1:5137/tests/collapsible/server?hiddenUntilFound=true&keepMounted=true&animated=true&animationDuration=123`
- Initial closed state rendered `hidden="until-found"`, `data-closed`, `data-starting-style`, and panel CSS variables.
- Open state rendered root `data-open`, trigger `aria-expanded="true"`, trigger `aria-controls`, trigger `data-panel-open`, and panel `data-open`.

Evidence: `docs/audits/logs/collapsible-inapp-browser-comparison.json`.

## Manual Static Checks

- Compared React source files and upstream commits listed in `collapsible-functional-audit.md`.
- Confirmed minified Collapsible JS was regenerated from `blazix-baseui-collapsible.js` with Terser.
- Confirmed no `.log` files were created for audit evidence; durable logs use `.txt` and `.json`.

## Final Status

Collapsible verification passed. The branch is review-ready after staging.
