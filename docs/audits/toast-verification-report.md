# Toast Verification Report

Date: 2026-07-21

## Final Results

| Verification | Result |
| --- | --- |
| React Toast source tests | Passed: 272 passed, 25 skipped, 297 total across 15 files |
| Focused Blazor bUnit tests | Passed: 28/28 |
| Full Toast Playwright tests | Passed: 26/26; 13 Server and 13 WebAssembly |
| Complete solution build/analyzers | Passed: 0 warnings, 0 errors |
| Repository lint rules | Passed: 0 violations |
| JavaScript syntax | Passed |
| Whitespace integrity | Passed |
| PNPM React docs host | Started and served Toast page |
| Blazor docs host | Started and served Toast page |
| In-app browser comparison | Completed; documented separately |

## Commands Executed

| Command | Result |
| --- | --- |
| `pnpm exec vitest run --project @base-ui/react packages/react/src/toast` from `.base-ui` | 15 files; 272 passed; 25 skipped; exit 0 |
| `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --filter "FullyQualifiedName~ToastTests"` before repair | 13/13 passed; established baseline |
| `dotnet build src/Blazix.BaseUI/Blazix.BaseUI.csproj -p:NuGetAudit=false -v minimal` | Passed, 0 warnings/errors |
| `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --filter "FullyQualifiedName~ToastTests\|FullyQualifiedName~FocusGuardTests" --no-restore -p:NuGetAudit=false -v normal` | Passed 28/28 |
| `dotnet build tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests.csproj --no-restore -p:NuGetAudit=false` | Passed |
| `dotnet test tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~ToastTests" --no-restore -p:NuGetAudit=false -v normal` | Passed 26/26 in 12.7729 seconds |
| `dotnet build Blazix.BaseUI.slnx --no-restore -p:NuGetAudit=false -v minimal` | Passed, 0 warnings/errors |
| `bash scripts/lint-rules.sh` | Passed, 0 violations |
| `node --check src/Blazix.BaseUI/wwwroot/blazix-baseui-toast.js` | Passed |
| `node --check src/Blazix.BaseUI/wwwroot/blazix-baseui-floating.js` | Passed |
| Regenerate Toast and floating minified assets with vendored Terser using `--compress --mangle --module`, then `cmp` against checked-in `.min.js` files | Passed; byte-identical |
| `git diff --check` | Passed |
| `pnpm docs:dev` from `.base-ui` | Next.js docs ready on port 3005; Toast route returned 200 |
| `dotnet run --project docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.csproj --no-restore --urls http://127.0.0.1:5079` | Blazor docs ready; Toast route served |

`-p:NuGetAudit=false` was used only to prevent transitive advisory policy from turning restore metadata into a verification failure. It does not alter compiled source or dependencies.

## Browser Coverage

The 26 final tests execute the same 13 Toast contracts in Server and WebAssembly modes:

1. Low- and high-priority roles, ARIA, and live-region behavior.
2. Close and removal callbacks.
3. Swipe dismissal and published swipe attributes.
4. F6 viewport focus and expansion.
5. Canonical swipe-ignore behavior.
6. Document-level pointer release cleanup and subsequent swipe correctness.
7. Active-pointer-only touch-move prevention.
8. Non-native Action and Close activation by Enter and Space.
9. Zero-limit inert behavior and focus-guard restoration.
10. Empty/non-empty viewport listener rebind.
11. Existing manager, promise, limit, and anchored-positioning contracts retained by the shared page.

The full final transcript is retained locally at `docs/audits/logs/toast-playwright-full.log`. Raw logs remain uncommitted by repository policy.

## Intermediate Defects Detected During Verification

Two non-final browser runs exposed real timing defects and were not treated as final evidence:

- The listener-rebind test invoked F6 before Server interop completed. The test now waits for the rendered toast and listener-binding interval before asserting.
- With `Limit=0`, Blazor Server render latency could leave a short interval where Tab entered a toast before `inert` arrived. Tab routing was moved into the existing viewport JS listener so focus skips limited/ending roots synchronously.

After both repairs, the complete 26-test run passed. The retained final log contains only the clean run.

One non-final solution build was launched concurrently with the bUnit and Playwright projects while capturing logs. MSBuild reported generated static-web-asset/PDB file locks because the processes shared `obj` and `bin` outputs. The identical solution build was rerun alone, passed with zero warnings/errors, and replaced the raw final build log.

## Manual and Static Checks

- Inspected every changed Toast Razor/C#/JS file against the corresponding React production source.
- Confirmed all component JS listeners have deterministic cleanup and owner-document/window scope.
- Confirmed minified Toast and floating assets were regenerated from their source counterparts.
- Confirmed no `RenderFragment` content is cached.
- Confirmed no Toast state synchronization uses `Task.Run` or a render-loop parameter copier.
- Confirmed `output/` and raw `docs/audits/logs/` evidence were not staged.
- Confirmed the worktree contains no commit produced by this audit.

## Environment Notes

The React docs server reported a Next.js development-origin warning because the in-app browser used `127.0.0.1` while the server advertised `localhost`; the page still returned HTTP 200 and rendered. The Blazor docs page exposed its existing “Preparing controls...” overlay, preventing direct demo clicks in that page session. Component interaction proof therefore comes from the dedicated Server/WASM Playwright application, while the in-app session was used for source-page, anatomy, API, guidance, and rendered-attribute comparison.
