# Toolbar Verification Report

Date: 2026-06-06

## Scope

Performed a 1:1 functional audit and repair of the Blazor Toolbar port against the vendored React Base UI Toolbar and Composite source.

Serena and context7 were requested by repository instructions, but no callable Serena or context7 tool was available in this Codex session. Discovery used `rg`, the local `.base-ui` React source, repository tests, and local ASP.NET/Blazor skill references.

Existing unrelated working-tree changes were present and not modified or staged:

- `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.Client/Shared/Sections/ToggleGroupSection.razor`
- `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.Client/Shared/Sections/ToggleSection.razor`
- Toggle audit artifacts outside the Toolbar source/test scope.

## Commands and Results

| Command | Result | Log |
| --- | --- | --- |
| `rg --files src/BlazorBaseUI/Toolbar tests/BlazorBaseUI.Tests/Toolbar .base-ui/packages/react/src/toolbar` | Source and test inventory completed. | Console inspection |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "<focused Toolbar parity cases>" -v normal` | RED: focused parity tests failed before implementation. | `docs/audits/logs/toolbar-red-playwright-focused.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~ToolbarTestsServer.DisabledFocusableButton_DoesNotInvokeActivationHandlers" -v normal` | RED: disabled focusable activation reached Blazor handler before repair. | `docs/audits/logs/toolbar-red-playwright-disabled-activation.log` |
| `npx --yes terser src/BlazorBaseUI/wwwroot/blazor-baseui-toolbar.js --compress --mangle --module --output src/BlazorBaseUI/wwwroot/blazor-baseui-toolbar.min.js` | Minified toolbar JS regenerated. | Console inspection |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "<focused Toolbar parity cases>" -v normal` | GREEN: 6 focused tests passed. | `docs/audits/logs/toolbar-playwright-focused.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~Toolbar" -v normal` | GREEN: 102 passed. | `docs/audits/logs/toolbar-bunit.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~ToolbarTestsServer" -v normal` | GREEN: 33 passed. | `docs/audits/logs/toolbar-playwright-server.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~ToolbarTestsWasm" -v normal` | GREEN: 5 passed, 28 skipped by existing WASM keyboard warmup policy. | `docs/audits/logs/toolbar-playwright-wasm.log` |
| `node --check src/BlazorBaseUI/wwwroot/blazor-baseui-toolbar.js` | GREEN: no syntax errors. | `docs/audits/logs/toolbar-js-syntax.log` |
| `node --check src/BlazorBaseUI/wwwroot/blazor-baseui-toolbar.min.js` | GREEN: no syntax errors. | `docs/audits/logs/toolbar-min-js-syntax.log` |
| `dotnet build BlazorBaseUI.slnx -v minimal` | GREEN: build succeeded, 0 warnings, 0 errors. | `docs/audits/logs/toolbar-dotnet-build.log` |
| `bash scripts/lint-rules.sh` | GREEN: 0 violations. | `docs/audits/logs/toolbar-lint-rules.log` |
| In-app Browser at `http://127.0.0.1:5261/tests/toolbar/server` variants | GREEN: representative RTL, Home, input selection, disabled click, and disabled input key checks passed. | `docs/audits/logs/toolbar-in-app-browser-verification.log` |

## Playwright Coverage

Server Playwright Toolbar suite:

- Total tests: 33
- Passed: 33
- Failed: 0

WASM Playwright Toolbar suite:

- Total tests: 33
- Passed: 5
- Skipped: 28
- Failed: 0
- Skip reason in existing test suite: WASM JIT warmup causes unreliable keyboard event processing.

Focused parity Playwright suite after repair:

- Total tests: 6
- Passed: 6
- Failed: 0

## Manual In-App Browser Checks

The in-app Browser verified these runtime states against the local Server test page:

- RTL toolbar: `ArrowLeft` from button 1 moved focus to button 2.
- Default horizontal toolbar: `Home` left focus on button 3.
- Toolbar input: focus selected the full `abcd` value.
- Disabled focusable button: pointer click left the activation count at `0`.
- Disabled focusable input in a vertical toolbar: focus stayed on the input after `ArrowDown` and `ArrowUp`.

## Files Changed

- `src/BlazorBaseUI/wwwroot/blazor-baseui-toolbar.js`
- `src/BlazorBaseUI/wwwroot/blazor-baseui-toolbar.min.js`
- `tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.Client/Pages/Tests/Toolbar/ToolbarTestPage.razor`
- `tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/Components/Pages/Tests/Toolbar/ToolbarTestPage.razor`
- `tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/Infrastructure/RenderModeExtensions.cs`
- `tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/Tests/Toolbar/ToolbarTests.Wasm.cs`
- `tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/Tests/Toolbar/ToolbarTestsBase.cs`
- `../base-ui-specs/toolbar/SPEC.md`
- `../base-ui-specs/toolbar/pitfalls.md`
- `docs/audits/toolbar-*.md and docs/audits/logs/toolbar-*.log`

## Conclusion

The Toolbar port now matches the audited React Base UI Toolbar and Composite behaviors for attributes, disabled semantics, keyboard navigation, RTL navigation, native input behavior, focus management, and lifecycle synchronization. No known Toolbar parity gap remains from the audited source.
