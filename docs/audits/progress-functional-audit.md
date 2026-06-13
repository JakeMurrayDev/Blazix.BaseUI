# Progress Verification Report

Date: 2026-06-07

## Scope

Performed a 1:1 functional audit and repair of the Blazor Progress port against the vendored React Base UI Progress source and public docs.

Serena and context7 were requested by repository instructions, but no callable Serena or context7 tool was available in this Codex session. Discovery used `rg`, the local `.base-ui` React source, PNPM source commands, repository tests, and local ASP.NET/Blazor references.

The framework-agnostic spec was missing before this audit. It was created at `../base-ui-specs/progress/` and used as the durable behavioral reference for the repaired port.

## React Source Inventory

| Area | React source |
| --- | --- |
| Root | `.base-ui/packages/react/src/progress/root/ProgressRoot.tsx` |
| Root context | `.base-ui/packages/react/src/progress/root/ProgressRootContext.ts` |
| Label | `.base-ui/packages/react/src/progress/label/ProgressLabel.tsx` |
| Track | `.base-ui/packages/react/src/progress/track/ProgressTrack.tsx` |
| Indicator | `.base-ui/packages/react/src/progress/indicator/ProgressIndicator.tsx` |
| Value | `.base-ui/packages/react/src/progress/value/ProgressValue.tsx` |
| Utilities | `.base-ui/packages/react/src/progress/utils/*`, `.base-ui/packages/react/src/utils/formatNumberValue.ts`, `.base-ui/packages/react/src/utils/useRegisteredLabelId.ts`, `.base-ui/packages/react/src/utils/visuallyHidden.ts` |
| Docs | `.base-ui/docs/src/app/(docs)/react/components/progress/page.mdx`, `.base-ui/docs/src/app/(docs)/react/components/progress/types.md` |

## Resolved Gaps

| Gap | React behavior | Repair |
| --- | --- | --- |
| Missing NVDA presentation span | `ProgressRoot` renders a visually hidden `span role="presentation"` with text `x` after children. | `ProgressRoot` now renders the helper span with the audited visually-hidden style. |
| Label role missing | `ProgressLabel` defaults `role="presentation"`. | `ProgressLabel` now sets a default presentation role while preserving consumer role overrides. |
| Label registration stale after id changes | `useRegisteredLabelId` updates context when the resolved label id changes. | `ProgressLabel` registers in `OnParametersSet` and clears only the currently registered id during dispose. |
| Parts outside root silently rendered nothing | React context hook throws `Base UI: ProgressRootContext is missing. Progress parts must be placed within <Progress.Root>.` | Label, Track, Indicator, and Value now throw the same diagnostic when no root context is present. |
| Default ARIA attributes could not be overridden | React merged element props allow consumer props to override defaults. | Root and part attributes now use default-only assignment for default ARIA, role, and state attributes. |
| Number formatting API incomplete | React accepts `Intl.NumberFormatOptions` and `locale`, with percent fallback. | `ProgressRoot` now supports `NumberFormatOptions? Format`, `Locale`, `FormatString`, and `FormatProvider`; default percent formatting remains intact. |
| Value child render function not equivalent | React `children` can be a function receiving formatted value and raw value. | `ProgressValue.ChildContent` now accepts `Func<string?, double?, RenderFragment>?` and resolves at render time. |
| RenderFragment caching present | Blazor implementation cached rendered value content in a field. | Cached content was removed; content resolves during render. |
| Indicator style precedence wrong | React applies default transform style, then consumer element style, then render style prop. | Indicator style merging now preserves default style first, `AdditionalAttributes["style"]` second, and `StyleValue` last. |
| `valueToPercent` edge behavior mismatched | React performs the raw arithmetic calculation. | Blazor `ValueToPercent` now mirrors the raw formula. |

## Commands and Results

| Command | Result | Log |
| --- | --- | --- |
| `rg --files src/BlazorBaseUI/Progress tests/BlazorBaseUI.Tests/Progress tests/BlazorBaseUI.Tests.Contracts/Progress .base-ui/packages/react/src/progress` | Source, test, and React inventory completed. | Console inspection |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~Progress" --no-restore -v minimal` | RED before repair: 16 focused Progress parity failures. | Console inspection |
| `dotnet build BlazorBaseUI.slnx -v minimal` | GREEN: build succeeded, 0 warnings, 0 errors. | `docs/audits/logs/progress-dotnet-build.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~Progress" -v minimal` | GREEN: 74 passed, 0 failed. | `docs/audits/logs/progress-bunit.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~Progress" -v minimal` | RED during test development: 2 locator-only failures caused by matching both the label role and hidden helper role. | `docs/audits/logs/progress-playwright-dotnet.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~Progress" -v minimal` | GREEN: 26 passed, 0 failed. | `docs/audits/logs/progress-playwright-dotnet-pass.log` |
| `bash scripts/lint-rules.sh` | GREEN: 0 violations. | `docs/audits/logs/progress-lint-rules.log` |
| `pnpm --dir .base-ui docs:validate "(docs)/react/components/progress"` | GREEN: no docs files required updates. | `docs/audits/logs/progress-source-docs-validate.log` |
| `pnpm --dir .base-ui exec vitest run --project @base-ui/react packages/react/src/progress` | GREEN: 5 files passed, 83 passed, 3 skipped by source suite. | `docs/audits/logs/progress-source-react-tests.log` |
| In-app Browser at `http://127.0.0.1:5128/tests/progress/server?value=30&showLabel=true&showValue=true&labelText=Downloading` | GREEN: live DOM confirmed label linkage, root ARIA values, helper span, value hiding, data-state attributes, and 30 to 100 percent state transition. | `docs/audits/logs/progress-in-app-browser.json` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj -v minimal` | BASELINE RED: unrelated existing failures outside Progress; Progress targeted suite remained green. | `docs/audits/logs/progress-bunit-full-project.log` |

## Playwright Coverage

Progress Playwright suite after repair:

- Total tests: 26
- Passed: 26
- Failed: 0
- Render modes covered: Server and WASM

Covered states include active/progressing, complete, indeterminate, zero value, custom min and max, label linkage, value rendering, render-prop value rendering, root attribute overrides, missing-context exceptions, indicator style merging, and NVDA helper span rendering.

## Manual In-App Browser Checks

The in-app Browser verified these runtime states against the local Server test page:

- Root had `role="progressbar"`, `aria-valuemin="0"`, `aria-valuemax="100"`, `aria-valuenow="30"`, and `aria-valuetext="30%"`.
- Root had `aria-labelledby` matching the rendered `ProgressLabel` id.
- Label had `role="presentation"`.
- Value had `aria-hidden="true"` and displayed `30%`.
- Hidden helper span existed with `role="presentation"`, text `x`, and the audited visually-hidden style.
- Indicator had `data-progressing` and `width:30%`.
- After clicking `Set 100%`, root and value had complete state, root `aria-valuenow="100"`, and indicator `width:100%`.

Screenshot capture through the in-app Browser timed out twice at the Chrome DevTools Protocol `Page.captureScreenshot` step. DOM automation and accessibility snapshots succeeded and were persisted in JSON.

## Files Changed

- `src/BlazorBaseUI/Progress/ProgressRoot.razor`
- `src/BlazorBaseUI/Progress/ProgressRootContext.cs`
- `src/BlazorBaseUI/Progress/ProgressLabel.razor`
- `src/BlazorBaseUI/Progress/ProgressValue.razor`
- `src/BlazorBaseUI/Progress/ProgressTrack.razor`
- `src/BlazorBaseUI/Progress/ProgressIndicator.razor`
- `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.Client/Shared/Sections/ProgressSection.razor`
- `tests/BlazorBaseUI.Tests.Contracts/Progress/*.cs`
- `tests/BlazorBaseUI.Tests/Progress/*.cs`
- `tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests*/**/Progress*.razor`
- `tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/Tests/Progress/ProgressTestsBase.cs`
- `../base-ui-specs/progress/SPEC.md`
- `../base-ui-specs/progress/pitfalls.md`
- `docs/audits/progress-*.md` and `docs/audits/logs/progress-*.log`

## Residual Risk

The full bUnit project suite currently fails in unrelated Menu, Select, FloatingTree, and NavigationMenu tests. This audit did not modify those components. Targeted Progress unit tests, targeted Progress Playwright tests, solution build, lint, source docs validation, and React source tests are green.

## Conclusion

The Progress port now accounts for every audited React Base UI Progress component, utility, attribute branch, formatting branch, state attribute, and missing-context guard. No known Progress parity gap remains from the audited source.
