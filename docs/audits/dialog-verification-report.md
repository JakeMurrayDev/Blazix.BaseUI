# Dialog Verification Report

Date: 2026-06-29

## Repairs verified

1. **Open-change trigger association** — `DialogOpenChangeEventArgs` exposes `Trigger`/`TriggerId`/`InteractionType`/`Event`; `DialogRoot.SetOpenAsync` populates them.
2. **Close-interaction-type `FinalFocus` callback** — callback `FinalFocus` deferred at open and re-resolved at close with the close interaction type, applied via `setFinalFocusElement` + `cleanupFocusManager` dispose override.

Files changed: `src/Blazix.BaseUI/Dialog/EventArgs.cs`, `DialogRootContext.cs`, `DialogRoot.razor`, `DialogPopup.razor`; `src/Blazix.BaseUI/wwwroot/blazix-baseui-dialog.js` (+ `.min.js`); tests `tests/Blazix.BaseUI.Tests.Contracts/Dialog/IDialogRootContract.cs`, `tests/Blazix.BaseUI.Tests/Dialog/DialogRootTests.cs`.

## Command log

| Command | Result | Log |
| --- | --- | --- |
| `dotnet build Blazix.BaseUI.slnx -v q` (baseline, pre-change) | Passed; exit 0. | Console |
| `dotnet build Blazix.BaseUI.slnx -v minimal` (post-change) | **Passed; 0 warnings, 0 errors.** | `docs/audits/logs/dialog-dotnet-build.log` |
| `node --check blazix-baseui-dialog.js` + `…-dialog.min.js` | Passed (both readable + minified). | `docs/audits/logs/dialog-js-syntax-check.log` |
| `terser blazix-baseui-dialog.js --compress --mangle --module --output …min.js` | Passed; minified Dialog JS regenerated (`finalFocusMode` override present). | Console |
| `dotnet test …Tests --filter "FullyQualifiedName~Dialog"` (Dialog + AlertDialog bUnit) | **Passed; 98 passed, 0 failed** (96 prior + 2 new). | `docs/audits/logs/dialog-bunit.log` |
| `dotnet test …Tests` (full bUnit suite) | 2685 passed, **40 failed — all pre-existing and non-Dialog** (see below). | Console / TRX |
| `dotnet test …Playwright --filter "FullyQualifiedName~Dialog"` (Server + WASM) | **Passed; 65 passed, 0 failed, 0 skipped, 25 s.** | `docs/audits/logs/dialog-playwright.log` |
| `bash scripts/lint-rules.sh` | **Passed; 0 violations.** | `docs/audits/logs/dialog-lint-rules.log` |
| `pnpm -C .base-ui docs:validate "(docs)/react/components/dialog"` | **Passed; no files needed updating** (React source docs authoritative + in sync). | `docs/audits/logs/dialog-source-docs-validate.log` |
| In-app browser `http://localhost:5216/components/dialog` (Auto mode) | **Passed;** Dialog demo opens as modal (`role=dialog`, `aria-modal=true`, `data-open`), backdrop rendered, focus trapped inside, Escape closes, focus returns to trigger. | `docs/audits/logs/dialog-in-app-browser.json` |
| `git diff --check` | **Passed; clean** (no whitespace errors). | `docs/audits/logs/dialog-git-diff-check.log` |

## New regression tests (red → green)

| Test | Proves | Pre-fix behavior |
| --- | --- | --- |
| `DialogRootTests.OnOpenChangeIncludesTriggerAssociation` | `OnOpenChange` args carry the owning trigger id + element (React `ChangeEventDetails.trigger`). | Could not compile — `TriggerId`/`Trigger` did not exist on the args. |
| `DialogRootTests.FinalFocusCallbackReceivesCloseInteractionType` | A callback `FinalFocus` receives the **close** interaction type (`"mouse"` on close-press). | Callback was resolved eagerly at open with the open interaction type (`""` for a default-open dialog) and never re-evaluated at close. |

## Pre-existing full-suite failures (not introduced by this audit)

The full bUnit run reports 40 failures, **none in Dialog/AlertDialog**. Grouped by class:
`FloatingFocusManagerTests` (27), `AvatarImageTests` (6), `MenuRootTests` (2), `FloatingNodeTests` (2), `FloatingTreeTests` (2), `AvatarFallbackTests` (1).

These were confirmed **pre-existing**: the audit's tracked changes were stashed (`git stash push -- <8 files>`), the test project rebuilt at the clean baseline, and the same 40 failures reproduced identically (40 failed / 94 passed in those classes). The audit's edits are strictly Dialog-scoped and touch none of those components' source. The Dialog/AlertDialog filter is fully green (98/98).

`DialogTestsBase.PopupCanReceiveFocus` failed once during a heavily-parallel Playwright run and **passed on isolated re-run** — a parallel-contention flake (consistent with prior audit observations), not a regression. The authoritative Dialog Playwright run (65/65) is green.

## Conclusion

Both repairs build clean (0/0), pass the full Dialog bUnit suite (98/98) and the Server+WASM Playwright suite (65/65), introduce no lint violations, regenerate valid minified JS, and are confirmed working in the in-app browser. No Dialog parity gap remains unaddressed; every recent upstream `[dialog]`/`[alert dialog]`/`[popups]` delta is implemented (inherited from the shared floating infrastructure) or accounted for with recorded justification in `dialog-functional-audit.md`. Changes are staged but not committed.
