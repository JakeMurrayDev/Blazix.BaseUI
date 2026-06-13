# Select Verification Report

Date: 2026-06-10
Repository: `Blazix.BaseUI`

## Local Blazor Verification

| Command | Result | Log |
| --- | --- | --- |
| `dotnet build Blazix.BaseUI.slnx` | Passed. 0 warnings, 0 errors. | `docs/audits/logs/select-dotnet-build.log` |
| `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --filter "FullyQualifiedName~Blazix.BaseUI.Tests.Select."` | Passed. 281 passed, 0 failed. | `docs/audits/logs/select-bunit-select.log` |
| `dotnet test tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~Blazix.BaseUI.Playwright.Tests.Tests.Select."` | Passed. 44 passed, 0 failed. | `docs/audits/logs/select-playwright.log` |
| `/opt/homebrew/bin/bash scripts/lint-rules.sh` | Passed. 0 textual lint violations. | `docs/audits/logs/select-lint-rules.log` |

## Source Verification

| Command | Result | Log |
| --- | --- | --- |
| `pnpm --dir .base-ui run docs:validate -- "(docs)/react/components/select"` | Passed. No files needed updating. Source emitted existing `IncludesInstantiable` warnings. | `docs/audits/logs/select-source-docs-validate.log` |
| `pnpm --dir .base-ui run test:jsdom -- packages/react/src/select` | Passed. 277 files passed, 4 skipped; 6152 tests passed, 867 skipped. The workspace script runs additional configured projects. | `docs/audits/logs/select-source-jsdom.log` |
| `pnpm --dir .base-ui --filter @base-ui/react run typescript` | Passed. | `docs/audits/logs/select-source-typescript.log` |
| `pnpm --dir .base-ui exec playwright install chromium firefox webkit` | Passed after the first source browser run reported missing source browser binaries. | `docs/audits/logs/select-source-playwright-install.log` |
| Bundled .NET Playwright Node driver `cli.js install chromium` | Passed after the .NET Playwright fixture reported missing Chromium build v1200 and `pwsh` was unavailable. | `docs/audits/logs/select-dotnet-playwright-install.log` |
| `pnpm --dir .base-ui run test:browsers -- packages/react/src/select` | Failed due unrelated non-Select suites because the workspace script runs all configured projects. Select entries in the log show execution across browsers. | `docs/audits/logs/select-source-browsers.log` |
| `pnpm --dir .base-ui exec cross-env TZ=UTC VITEST_ENV=all-browsers vitest --project @base-ui/react --run packages/react/src/select` | Failed inside React source browser run. 1393 passed, 48 skipped, 2 WebKit Select failures. | `docs/audits/logs/select-source-browsers-select-only.log` |
| `pnpm --dir .base-ui exec cross-env TZ=UTC VITEST_ENV=chromium vitest --project @base-ui/react --run packages/react/src/select` | Passed. 18 files passed; 465 passed, 16 skipped. | `docs/audits/logs/select-source-browsers-chromium.log` |
| `pnpm --dir .base-ui exec cross-env TZ=UTC VITEST_ENV=firefox vitest --project @base-ui/react --run packages/react/src/select` | Failed inside React source browser run. 17 files passed, 1 failed; 464 passed, 16 skipped, 1 source failure. | `docs/audits/logs/select-source-browsers-firefox.log` |
| `pnpm --dir .base-ui exec cross-env TZ=UTC VITEST_ENV=webkit vitest --project @base-ui/react --run packages/react/src/select` | Failed inside React source browser run. 17 files passed, 1 failed; 464 passed, 16 skipped, 1 source failure. | `docs/audits/logs/select-source-browsers-webkit.log` |

## Manual Browser Check

The in-app browser was used against the local test host to inspect a default-value Select instance. The audited checks confirmed:

- popup opens and renders visible content
- positioner receives stable nonzero geometry
- `data-positioned` is released after placement
- trigger receives `data-popup-side="bottom"`
- align-item DOM state uses source-equivalent `data-side` behavior

## Source Browser Residuals

These residuals are in the local React source browser tests and were not introduced by the Blazor port:

- Firefox Select-only source run: `SelectRoot.test.tsx`, `prop: highlightItemOnHover`, expected `data-highlighted` after popup mouseout while disabled.
- WebKit Select-only source run: `SelectRoot.test.tsx`, `select inside popover`, `vitest-fail-on-console` failed on a React `act(...)` warning.
- Broad all-browser source run: unrelated upstream suites failed, including Menubar, Drawer, Menu, Checkbox, TemporalAdapterLuxon, and others.

## Staging Intent

Stage only source, JS, and test changes required for the Select repair. Do not stage `docs/audits/**` logs or reports. The framework-agnostic spec was written under `../base-ui-specs/select`; that directory is outside this repository and is not a git repository in this environment.
