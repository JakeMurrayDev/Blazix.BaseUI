# Toggle and ToggleGroup Verification Report

## Scope

- Audited Blazor `Toggle` and `ToggleGroup` against local React Base UI sources under `.base-ui/packages/react/src/toggle` and `.base-ui/packages/react/src/toggle-group`.
- Included supporting React internals: `useButton`, `useFocusableWhenDisabled`, `CompositeRoot`, `useCompositeRoot`, `CompositeItem`, `useCompositeItem`, `createBaseUIEventDetails`, and toolbar root context behavior.
- Serena and context7 were requested by repository instructions but were not available as callable tools in this session. Local source files and repository tests were used as the source of truth.

## Resolved Gaps

- Added grouped native `aria-disabled="true|false"` parity.
- Removed forwarded `form` from `Toggle` and preserved component-owned `type="button"` override.
- Resolved missing and empty-string grouped values through stable generated IDs.
- Fired grouped `Toggle.OnPressedChange` with shared cancellation state after group value handling.
- Added Base UI event detail fields: `Reason`, `Event`, `Cancel()`, `AllowPropagation()`, `IsCanceled`, and `IsPropagationAllowed`.
- Changed `ToggleGroup multiple=true` press/depress behavior to append and remove first matching value without deduplication.
- Added `aria-orientation` outside Toolbar and preserved Toolbar branch omission.
- Changed roving tab stop initialization from pressed item to first enabled item.
- Added RTL horizontal arrow reversal, Home/End, loop boundary, disabled skip, and cross-axis no-op browser coverage.
- Added Toolbar disabled inheritance and Toolbar-owned roving focus for ToggleGroup inside Toolbar.
- Kept DOM-order focus, active focus, preventDefault, and key activation behavior in JS interop.
- Added source-docs parity fixes to the Blazor demo: accessible names for icon-only controls, an RTL `DirectionProvider` ToggleGroup example, and a controlled required-selection example matching the upstream private experiment pattern.
- Updated out-of-repository specs:
  - `/Users/jakemurray/RiderProjects/base-ui-specs/toggle/SPEC.md`
  - `/Users/jakemurray/RiderProjects/base-ui-specs/toggle-group/SPEC.md`
  - `/Users/jakemurray/RiderProjects/base-ui-specs/toggle-group/pitfalls.md`

## Verification Commands

| Command | Result | Log |
| --- | --- | --- |
| `bash scripts/lint-rules.sh` | Passed. 0 violations for RULE-01, RULE-04, RULE-05, RULE-06. | `docs/audits/logs/toggle-toggle-group-lint-rules.log` |
| `dotnet build BlazorBaseUI.slnx` | Passed. 0 warnings, 0 errors. Analyzer-backed structural rules ran during build. | `docs/audits/logs/toggle-toggle-group-dotnet-build.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~BlazorBaseUI.Tests.Toggle.\|FullyQualifiedName~BlazorBaseUI.Tests.ToggleGroup." --logger "console;verbosity=normal"` | Passed. 68 tests passed. | `docs/audits/logs/toggle-toggle-group-dotnet-test-bunit.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~BlazorBaseUI.Playwright.Tests.Tests.Toggle.\|FullyQualifiedName~BlazorBaseUI.Playwright.Tests.Tests.ToggleGroup." --logger "console;verbosity=normal"` | Passed. 68 total: 56 passed, 12 skipped. Skips are WASM keyboard tests marked unreliable by existing suite policy; Server mode covers those keyboard branches. | `docs/audits/logs/toggle-toggle-group-dotnet-test-playwright.log` |
| `cd .base-ui && pnpm --version` | Passed. `10.33.4`. | `docs/audits/logs/toggle-toggle-group-pnpm-version.log` |
| `cd .base-ui && pnpm install --frozen-lockfile` | Passed. Lockfile up to date. | `docs/audits/logs/toggle-toggle-group-pnpm-install.log` |
| `cd .base-ui && pnpm docs:dev` | Passed. Upstream Base UI docs served at `http://localhost:3005`. | Terminal session |
| `dotnet run --project demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.csproj --urls http://127.0.0.1:5281` | Passed. Blazor demo served at `http://127.0.0.1:5281`. | Terminal session |
| `dotnet build BlazorBaseUI.slnx` after demo-doc parity fixes | Passed. 0 warnings, 0 errors. | `docs/audits/logs/toggle-toggle-group-docs-comparison-dotnet-build.log` |
| Upstream docs route GET checks | Passed. `/react/components/toggle`, `/react/components/toggle-group`, and `/experiments/toggle-group` returned 200. | `docs/audits/logs/toggle-toggle-group-source-docs-routes.log` |
| Blazor demo route GET checks | Passed. `/toggle` and `/toggle-group` returned 200. | `docs/audits/logs/toggle-toggle-group-blazor-docs-routes.log` |
| `git diff --check` | Passed. No whitespace errors. | Terminal check, no output. |

## Manual Checks

- Compared React `Toggle.tsx` props destructuring and state flow to Blazor parameters and lifecycle.
- Compared React `ToggleGroup.tsx` controlled/uncontrolled group value flow to Blazor `Value`, `DefaultValue`, `ValueChanged`, and `OnValueChange`.
- Compared React `useButton` and `useFocusableWhenDisabled` native/non-native attributes against Blazor rendered attributes.
- Compared React `CompositeRoot`/`CompositeItem` keyboard and roving focus behavior against `blazor-baseui-toggle.js`.
- Inspected Toolbar integration branch: React uses Toolbar composite focus inside Toolbar and omits nested ToggleGroup composite root.
- In-app browser manual smoke route: `http://127.0.0.1:5279/tests/togglegroup/server?toolbar=true&defaultValue=one`. Confirmed rendered Toolbar-mode DOM branch (`ToggleGroup` present, grouped toggles present, no nested ToggleGroup keydown handlers). Interactive proof is the saved Playwright CLI log.
- Ran upstream Base UI docs from local `.base-ui` source with `pnpm docs:dev` and compared rendered public Toggle, public ToggleGroup, and private ToggleGroup experiment pages against the Blazor demo.
- In-app browser source-docs checks confirmed:
  - upstream `Favorite` toggled `aria-pressed=false` to `true` and added `data-pressed`;
  - upstream public ToggleGroup single-select and multiple demos matched `role`, `data-orientation`, `data-multiple`, `aria-pressed`, and roving `tabindex` behavior;
  - Blazor patched docs expose no nameless icon-only pressed buttons;
  - Blazor required-selection demo keeps `Align center` pressed when an empty controlled value is attempted;
  - Blazor RTL demo moves focus from `Toggle bold` to `Toggle italic` on ArrowLeft under `DirectionProvider`.
- Detailed source-docs comparison is recorded in `docs/audits/toggle-toggle-group-source-docs-comparison.md`.

## Notes

- `docs/audits/logs/toggle-toggle-group-*.log` is ignored by `.gitignore`; the final stage operation must force-add these proof logs.
- Files under `/Users/jakemurray/RiderProjects/base-ui-specs` are outside the BlazorBaseUI Git repository and cannot be staged from this repo.
