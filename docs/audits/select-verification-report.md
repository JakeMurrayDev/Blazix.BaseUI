# Select Verification Report

Date: 2026-07-21

## Blazor Verification

| Command/check | Result |
| --- | --- |
| `dotnet build Blazix.BaseUI.slnx -p:NuGetAudit=false` | Passed; 0 warnings, 0 errors. |
| `bash scripts/lint-rules.sh` | Passed; 0 violations. Structural analyzers also passed in build. |
| `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --no-build -p:NuGetAudit=false --filter "FullyQualifiedName~Blazix.BaseUI.Tests.Select."` | Passed; 291/291. |
| `dotnet test ...Blazix.BaseUI.Playwright.Tests.csproj --no-build --filter "FullyQualifiedName~Blazix.BaseUI.Playwright.Tests.Tests.Select"` | Passed; 86/86 across Server and WebAssembly. |
| Focused popup-readiness regressions | Passed; 12/12 covering flat/grouped frame sizing, externally controlled reopen, runtime root rename/closed-transition rejection, watchdog no-commit behavior, and successful standard fallback in Server/WASM. |
| `node --check` on Select/Floating/Portal source and minified modules | Passed. |
| Vendored terser `--compress --mangle --module` for Select/Floating/Portal | Passed; minified assets regenerated after final JS changes. |
| `git diff --check` | Passed. |

The original full Playwright TRX is stored locally at `docs/audits/logs/select-playwright-2026-07-19.trx`. The final 86-case follow-up TRX is stored at `docs/audits/logs/select-playwright-2026-07-21-final.trx`. The popup-resize rerun transcript is stored at `docs/audits/logs/select-popup-resize-playwright-2026-07-21.log`. Generated logs are excluded from staging by repository policy.

An exploratory broad unit filter matched 325 tests because unrelated method names contained `Select`; it reported 324 passes and one unrelated `FloatingFocusManagerTests.PassesInitialFocusSelectorOption` timing failure. The exact Select namespace rerun, including the delayed popup-reference regression added afterward, passed 291/291.

### Diagnostic iterations

- The first full run after removing the unsafe watchdog visibility release passed 77/82. Five WASM keyboard/item cases remained hidden because popup JS initialization was conditioned on the parent's first render while the child `RenderElement` reference arrived later. The delayed-reference lifecycle repair made those five cases and the complete 13-case diagnostic filter pass.
- The next full run passed 81/82; one Server `ArrowUpNavigatesToPreviousItem` assertion was intermittent and passed five consecutive isolated reruns. A clean full rerun then passed 82/82 before the two successful-fallback cases were added.
- The initial successful-fallback regression did not enter the intended collision branch. The fixture was corrected to invoke the align placement entry point explicitly; both Server and WASM cases then passed and proved callback dispatch, Floating-owned `data-positioned`, non-`none` side, resolved size variables, and nonzero final geometry.
- The first forced delayed-reference bUnit fixture did not produce an `ElementReference` under bUnit's custom-render capture. The fixture was corrected to inject the late reference into `RenderElement`, then verified zero initialization before availability and exactly one initialization across subsequent renders.
- After the opening/scroll repair, the Select minified module was regenerated, the complete solution rebuilt, and the packaged-asset Playwright run passed 86/86.
- The opening/scroll follow-up added a 16-option, three-group, `contain: layout` fixture with a 128px list viewport. The new Server/WASM regression selected the final item, reopened, moved from maximum scroll to zero, waited 1200 ms beyond the former final probe, and verified the popup stayed open, the selection remained `thyme`, and the first option was visible.
- Per-animation-frame sizing capture now records popup/positioner height plus list `scrollTop`; every visible frame must remain equal to the first visible frame. Position changes are recorded but intentionally excluded because this regression concerns resizing, not repositioning. The first visible frame must occur within 16 animation frames of the open DOM state under the loaded Server/WASM harness.
- The final WASM trace exposed a transient Floating update that cleared `data-positioned` after an align-item revision had committed. The old watchdog treated the missing token as failure and replaced the 556px aligned layout with the 128px standard layout. Current-revision commits now remain authoritative, preserve the visibility token through same-input Floating churn, and reject late standard fallback.

## React Source Verification

| Command/check | Result |
| --- | --- |
| `pnpm --dir .base-ui install --offline --frozen-lockfile` | Passed; workspace already current. |
| `pnpm --dir .base-ui --filter @base-ui/utils build` | Passed; required because the workspace React package resolves Utils through its built symlink. |
| `pnpm --dir .base-ui test:jsdom -- src/select` | Passed; workspace runner executed 304 files: 299 passed, 5 skipped; 6,875 passed, 1,113 skipped. All Select suites passed. |
| `pnpm --dir .base-ui exec cross-env TZ=UTC VITEST_ENV=chromium vitest --project @base-ui/react --run packages/react/src/select` | Passed; 18/18 files, 493 passed, 16 skipped. |
| `pnpm --dir .base-ui --filter @base-ui/react run typescript` | Passed. |
| `pnpm --dir .base-ui run docs:validate -- "(docs)/react/components/select"` | Passed; no generated files changed. Existing `IncludesInstantiable` informational warnings were emitted. |

An initial package-scoped Vitest invocation could not resolve `@base-ui/utils/error` because the local build-symlink target was stale. Building `@base-ui/utils` through PNPM repaired the source workspace; the canonical workspace jsdom and targeted Chromium runs then passed.

## In-App Browser Comparison

Compared the live React docs at `http://localhost:3005/react/components/select` with the local Blazor Server and WebAssembly harnesses.

- React and Blazor triggers exposed `role=combobox`, `aria-expanded=true`, `aria-controls`, and a controlled listbox.
- Both rendered one `tabindex=0` active option and string `aria-selected` values on open.
- Blazor Arrow/Home/End navigation retained disabled-option focusability as upstream requires.
- Enter on a disabled active option did not select or close.
- Enter on an enabled option selected it, closed the popup, and returned focus to the combobox.
- Every kept-mounted closing option immediately had `tabindex=-1`.
- Server and WebAssembly harnesses produced no browser console errors or warnings.
- Live source docs rendered all Select anatomy, positioning, multiple, object-value, grouped, labeling, placeholder, and API sections used for the comparison.

### Reported WASM popup-resize reproduction

- `ffprobe` identified the supplied recording as 830x1156 H.264, 346 frames, 6.025 seconds.
- `ffmpeg` extracted every frame. On the recorded second open, frame 180 showed all options at the narrow intrinsic width, frame 181 hid the popup, and frame 182 showed the same populated popup at its final trigger-derived width. This isolated sizing readiness from repositioning and from incremental child rendering.
- A pre-fix WASM DOM trace measured a fully populated visible popup at 129.7px before `--anchor-width`/`--available-height` existed, followed by the final 252px popup after middleware resolution.
- After repair, the rebuilt docs were forced into WASM mode and sampled on every animation frame:

| Docs sample | Visible frames sampled | Options present from first frame | Anchor width | Popup widths observed |
| --- | ---: | ---: | ---: | --- |
| Usage/hero | 290 | 5 | 160px | `188px` only |
| Object values | 288 | 3 | 256px | `284px` only |
| Grouped | 257 | 16 | 224px | `252px` only |

Every sampled visible frame had `data-positioned`; no intrinsic-to-final resize occurred.

## Automated Coverage Inventory

- bUnit Select facts: 291.
- Concrete Playwright Select cases: 86 (Server/WASM inheritance and theory data included).
- React Chromium Select tests: 509 discovered, 493 passed, 16 skipped by upstream conditions.
- Added regressions cover multiple disabled hidden inputs, multiple serializer isolation, dirty order/comparer semantics, disabled/repeated closed typeahead, programmatic value/no force-mount, generic/assistive virtual clicks, 5px/6px trigger release boundaries, first-visible-frame sizing and vertical stability with a changed trigger width, externally controlled reopen, root rename and closed-transition rejection, delayed popup element-reference initialization, watchdog no-commit ownership, successful standard fallback sizing/visibility, and grouped bottom-to-top scroll persistence beyond the former one-second probe window.

## Staging Boundary

Stage the Select repair, shared JS/portal/positioner dependencies, tests, durable audit Markdown, and regenerated minified assets. Do not stage raw logs or `output/`. Do not commit until the maintainer reports personal testing complete.
