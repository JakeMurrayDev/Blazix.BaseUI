# NumberField Verification Report

Date: 2026-07-05

## Commands

| Command | Result | Log |
| --- | --- | --- |
| `pnpm -C .base-ui test:jsdom NumberField --no-watch` | Passed: 7 files, 361 passed, 16 skipped. | `docs/audits/logs/number-field-source-react-jsdom.txt` |
| `pnpm -C .base-ui test:chromium NumberField --no-watch` | Passed: 7 files, 384 passed, 1 skipped. | `docs/audits/logs/number-field-source-react-chromium.txt` |
| `CI=true pnpm -C .base-ui docs:validate "(docs)/react/components/number-field"` | Passed: 0 files updated. | `docs/audits/logs/number-field-source-docs-validate.txt` |
| `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --filter "FullyQualifiedName~NumberField" --logger "console;verbosity=detailed"` | Passed: 254/254. | `docs/audits/logs/number-field-blazor-unit.txt` |
| `dotnet test tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~NumberField" --logger "console;verbosity=detailed"` | Passed: 70/70. | `docs/audits/logs/number-field-blazor-playwright.txt` |
| `npx --yes terser@5.47.1 src/Blazix.BaseUI/wwwroot/blazix-baseui-number-field.js --compress --mangle --module -o src/Blazix.BaseUI/wwwroot/blazix-baseui-number-field.min.js` | Passed. | Minified asset regenerated. |
| `dotnet build Blazix.BaseUI.slnx -v minimal` | Passed: 0 warnings, 0 errors. | `docs/audits/logs/number-field-dotnet-build.txt` |
| `bash scripts/lint-rules.sh` | Passed: 0 violations. | `docs/audits/logs/number-field-lint-rules.txt` |
| `git diff --check` | Passed: no whitespace errors. | `docs/audits/logs/number-field-git-diff-check.txt` |

## In-App Browser

Opened and inspected:

- React source docs: `http://localhost:3005/react/components/number-field`
- Blazor docs: `http://localhost:5216/components/number-field`

Result: both pages exposed `Number Field` title, live textbox, hidden number input, Decrease/Increase controls, and matching anatomy/API headings: Root, ScrubArea, ScrubAreaCursor, Group, Decrement, Input, Increment.

Evidence: `docs/audits/logs/number-field-in-app-browser-docs-comparison.json`

## Coverage Summary

- Active, focused, disabled, read-only, required, dirty, touched, filled, valid, invalid, and scrubbing state attributes were covered by bUnit.
- Keyboard, wheel, paste, button, press-and-hold, scrub, form validity, tab order, disabled, and read-only browser paths were covered by Playwright.
- React source jsdom and Chromium suites were run with PNPM and passed before finalizing the Blazor parity matrix.
