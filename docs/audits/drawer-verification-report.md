# Drawer Upstream-Delta Verification Report

Date: July 18, 2026
Branch: `audit/update-drawer`
Scope: verification of the Drawer upstream-delta implementation (see `drawer-upstream-delta-2026-07.md`)

## Commands and Results

| # | Command | Result |
|---|---|---|
| 1 | `dotnet build Blazix.BaseUI.slnx` | **PASS** — Build succeeded, 0 warnings, 0 errors (`logs/drawer-build.log`). Requires the AngleSharp 1.5.0 pin; without it, restore fails with NU1902 (also fails on `master` — environmental advisory-database refresh, not a branch regression) |
| 2 | `dotnet test tests/Blazix.BaseUI.Tests` (full suite) | **PASS (no regressions)** — 2728/2768; the 40 failures are byte-identical by fully-qualified name to a clean `master` worktree run (pre-existing FloatingFocusManager/FloatingTree/Menu environmental failures; zero Drawer tests among them). All 24 Drawer bUnit tests pass (`logs/drawer-bunit.log`) |
| 3 | `dotnet test …Playwright.Tests --filter "FullyQualifiedName~Drawer\|~Dialog\|~Popover"` | **PASS** — **274/274**, real Chromium, Server + WASM render modes, 2 m 42 s (`logs/drawer-playwright.log`). Includes all 33 Drawer tests (swipe dismiss threshold/velocity, reverse-cancel, snap points incl. sequential + sqrt overshoot damping, swipe-area drag-open + re-grab, controlled-dismiss rejection revert, keyboard-inset var, composite-key containment, disabled/focus states) plus the Dialog and Popover sibling-regression suites exercising the shared `dialog.js`/`button.js` changes |
| 4 | Flake triage | Two single-test cold-start flakes observed on first-boot runs (`SnapPointDragUpdatesActiveSnapPoint`, `DialogTestsServer.PopupCanReceiveFocus`); both pass 3/3 in isolation and in warm full-suite runs. The drawer test was hardened: the fixed 200 ms pre-release wait was replaced with an observed `data-swiping` attribute assertion |
| 5 | Minified-asset integrity | **PASS** — `node --check` on all touched sources; regenerated `drawer/dialog/button.min.js` verified to contain the new code paths via interop-identifier greps (`OnSwipeRelease`, `changedTouches`) |
| 6 | `git diff --check` | **PASS** |

## Source-Docs Comparison (pnpm)

The upstream docs site was run from the vendored checkout (`cd .base-ui && pnpm i && pnpm docs:dev`, Next.js on port 3005) and compared against the Blazix demo app (`dotnet run`, port 5228, InteractiveServer) in Chrome.

**DOM contract, open state (hero side drawer, `swipeDirection="right"`):**

| Surface | Upstream | Blazix | Verdict |
|---|---|---|---|
| Popup | `role=dialog`, `tabindex=-1`, `data-open`, `data-starting-style`, `data-swipe-direction=right`, `aria-labelledby`, `aria-describedby`; style vars `--drawer-swipe-movement-x/y`, `--drawer-swipe-progress`, `--nested-drawers`, `--drawer-snap-point-offset`, `--drawer-swipe-strength`, `--drawer-frontmost-height` | Identical attribute set and var families | **Match** |
| Backdrop | `role=presentation`, `aria-hidden`, `data-open`, `data-starting-style`, inert marker, `user-select:none`, `--drawer-swipe-progress`, `--drawer-swipe-strength` | Identical | **Match** |
| Viewport | `role=presentation`, `data-open`, `data-starting-style` | Identical | **Match** |

Recorded deviations, all intentional or benign: (1) `data-blazix-base-ui-*` attribute prefix (library-wide convention; the upstream-compatible `data-base-ui-inert` is also emitted); (2) `aria-modal="true"` rendered explicitly where upstream relies solely on inert-ing background content — redundant but harmless with inert also applied; (3) `--drawer-swipe-movement-x/y` written only during gestures rather than rendered at rest — values are identical whenever a gesture is active.

**Behavioral spot check:** an identical fast horizontal flick (sub-50%-offset displacement) dismissed the drawer on both implementations — the velocity-based dismissal path, exercised end-to-end over a live SignalR circuit on the Blazix side (also validating the authoritative-dismiss fix M1: single clean dismiss animation, no snap-back flash).

## Review Cycle

Independent adversarial review of the full diff against `.base-ui @ bdcb685fa`: 1 blocker, 5 major, 6 minor findings; all fixed; every fix re-verified structurally (file:line) plus by the 274/274 browser run above. Final review verdict: **SHIP**.
