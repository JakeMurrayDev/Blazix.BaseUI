# NavigationMenu Verification Report

Date: 2026-05-10
Branch: bunchofchanges
Scope: `src/BlazorBaseUI/NavigationMenu/**`, `src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js`, NavigationMenu unit contracts/tests, and NavigationMenu Playwright coverage.

## Objective Checklist

| Requirement | Evidence |
|---|---|
| 1:1 functional audit against React Base UI source | Previous static audit enumerated React NavigationMenu files and ten parity findings; `.audit/2026-05-09-fix-summary.md` maps each finding to implemented Blazor/C#/JS fixes. |
| Zero-deferral utility/hook accounting | `docs/audits/navigation-menu-parity.md` now contains the hook/utility matrix and marks each React source concept as covered, covered by JS-native equivalent, or not applicable after grep over NavigationMenu source. |
| Attribute, ARIA, and data-attribute parity | bUnit NavigationMenu suite covers root orientation, trigger ARIA/data states, content/popup/backdrop/positioner state attributes, viewport target/guards, and controlled null state. |
| Native Blazor lifecycle instead of React state loops | Controlled updates now flow through `ApplyValue`, `TransitionLifecycleManager`, `OnParametersSet`, and typed callbacks; no RenderFragment caching is introduced. |
| Optimized DOM interop | DOM-heavy behaviors remain in `blazor-baseui-navigation-menu.js`: hover timing, safe polygon, document dismiss, focus routing, viewport reparenting, size observers, and floating placement callbacks. |
| Browser validation for active, disabled, focus, dismiss, and interaction states | NavigationMenu Playwright suite passes 24/24 across Server and WASM render modes, including disabled trigger and focus-out coverage. Full log: `.audit/navigation-menu-playwright.log`. |
| Review-ready staged artifacts | NavigationMenu parity files, supporting shared plumbing, tests/contracts, docs, and Playwright log are intended for staging; no commit is made. |

## Commands Run

| Command | Result |
|---|---|
| `node --check src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js` | PASS, exit 0. |
| `rg -n "console\\.warn|console\\.log|debugger|TODO|FIXME|MISSING|not implemented" src/BlazorBaseUI/NavigationMenu src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js` | PASS, no matches. |
| `dotnet restore tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj -p:NuGetAudit=false -p:UseSharedCompilation=false -v minimal` from `.claude/tmp-files/bbui-sdk10` | PASS. Required because repo defaulted to .NET 11 preview without `global.json`; temp folder pins installed SDK `10.0.203`. |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter "FullyQualifiedName~BlazorBaseUI.Tests.NavigationMenu" -v minimal` from `.claude/tmp-files/bbui-sdk10` | PASS: Failed 0, Passed 135, Skipped 0, Total 135. |
| `dotnet restore tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj -p:NuGetAudit=false -p:UseSharedCompilation=false -v minimal` from `.claude/tmp-files/bbui-sdk10` | PASS. |
| `dotnet build tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --no-restore -p:UseSharedCompilation=false -v minimal` from `.claude/tmp-files/bbui-sdk10` | PASS: 0 warnings, 0 errors. |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter "FullyQualifiedName~BlazorBaseUI.Playwright.Tests.Tests.NavigationMenu" -v normal` from `.claude/tmp-files/bbui-sdk10` | PASS: Test Run Successful, Total tests 24, Passed 24. Output captured in `.audit/navigation-menu-playwright.log`. |
| `dotnet build src/BlazorBaseUI/BlazorBaseUI.csproj --no-restore -p:UseSharedCompilation=false -v minimal` from `.claude/tmp-files/bbui-sdk10` | PASS: 0 warnings, 0 errors. |
| `git diff --check -- src/BlazorBaseUI/NavigationMenu src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js tests/BlazorBaseUI.Tests/NavigationMenu tests/BlazorBaseUI.Tests.Contracts/NavigationMenu tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/Tests/NavigationMenu tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.Client/Pages/Tests/NavigationMenu tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/Components/Pages/Tests/NavigationMenu tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/Infrastructure/RenderModeExtensions.cs docs/audits/navigation-menu-parity.md .audit/2026-05-09-audit-report.md .audit/2026-05-09-fix-summary.md .audit/navigation-menu-verification-report.md` | PASS; only line-ending conversion warnings from Git. |
| `bash scripts/lint-rules.sh` | BLOCKED by local environment: WSL reports no installed distributions. |

## Repair Log

| Finding | Resolution |
|---|---|
| Content was not portaled into viewport target | Viewport now registers a target element; JS re-parents content into `viewportTargetElement || viewportElement` and restores original parent on disposal. |
| Viewport focus guard model missing | Viewport now renders before/after `FocusGuard` components, routes focus through JS tabbable lookup, and applies root `ViewportInert`. |
| Trigger/link focus-out did not close | JS document `focusout` detects outside-menu focus movement and calls `OnFocusOut`; trigger/link blur forwarding remains intact. |
| Dismiss context/useDismiss branch absent | JS-native document `Escape`, outside press, focus-out, and link-close propagation cover dismiss semantics without a React-style context object. |
| Controlled `Value = null` failed | Controlled mode now treats `ValueChanged.HasDelegate` as controlled and keeps `null` as a valid closed value. |
| Controlled close transition missing | Controlled parameter changes route through `ApplyValue` and transition lifecycle state. |
| Positioner computed placement not reflected | Floating placement callback updates C# state/context; rendered `data-side` and `data-align` use computed values. |
| Nested collision fallback differed | `CollisionAvoidance` default is resolved from root nested state. |
| Trigger keyboard open did not stop default/propagation | Native JS keydown handles the open keys with `preventDefault` and `stopPropagation`. |
| Production JS diagnostics | Debug console diagnostics removed; marker scan has no matches. |
| Final Playwright regressions after repair | Added root `data-orientation`; narrowed patient-click guard to hover-opened menus and clears stale hover timers on click. |

## Automated Log

Primary Playwright log: `.audit/navigation-menu-playwright.log`

Final summary excerpt:

```text
Test Run Successful.
Total tests: 24
     Passed: 24
 Total time: 23.9388 Seconds
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Residual Risk

The verification commands intentionally target NavigationMenu. The full repository has broad unrelated dirty work and prior unrelated Select failures; this report does not claim full-solution parity outside NavigationMenu.

`bash scripts/lint-rules.sh` could not run because this Windows environment has no installed WSL distributions. No lint findings were produced.
