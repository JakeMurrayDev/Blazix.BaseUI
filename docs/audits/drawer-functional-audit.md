# Drawer Functional Audit

Date: May 27, 2026
Repository: BlazorBaseUI
Component: Drawer
Source of truth: `.base-ui/packages/react/src/drawer`

## Scope

This audit compared the Blazor Drawer port against the React Base UI Drawer source and implemented the missing port. The audited React files were:

- `backdrop/DrawerBackdrop.tsx`
- `backdrop/DrawerBackdropCssVars.ts`
- `backdrop/DrawerBackdropDataAttributes.ts`
- `close/DrawerClose.tsx`
- `content/DrawerContent.tsx`
- `content/DrawerContentDataAttributes.ts`
- `description/DrawerDescription.tsx`
- `indent/DrawerIndent.tsx`
- `indent-background/DrawerIndentBackground.tsx`
- `popup/DrawerPopup.tsx`
- `popup/DrawerPopupCssVars.ts`
- `popup/DrawerPopupDataAttributes.ts`
- `portal/DrawerPortal.tsx`
- `provider/DrawerProvider.tsx`
- `provider/DrawerProviderContext.ts`
- `root/DrawerRoot.tsx`
- `root/DrawerRootContext.ts`
- `root/useDrawerSnapPoints.ts`
- `swipe-area/DrawerSwipeArea.tsx`
- `swipe-area/DrawerSwipeAreaDataAttributes.ts`
- `title/DrawerTitle.tsx`
- `trigger/DrawerTrigger.tsx`
- `viewport/DrawerViewport.tsx`
- `viewport/DrawerViewportContext.tsx`
- `viewport/DrawerViewportDataAttributes.ts`

## Resolved Gaps

- Added the missing Drawer component family under `src/BlazorBaseUI/Drawer`.
- Added `DrawerRoot` controlled and uncontrolled state handling, `DefaultOpen`, `Open`, `OpenChanged`, `OpenChange`, modal mode, dismissibility, nested drawer state, snap points, active snap point, swipe direction, sequential snap release, and swipe disabled handling.
- Added `DrawerProvider`, `DrawerIndent`, and `DrawerIndentBackground` equivalents for nested drawer layout offsets and background transforms.
- Added `DrawerPortal`, `DrawerBackdrop`, `DrawerViewport`, `DrawerPopup`, `DrawerSwipeArea`, `DrawerContent`, `DrawerTitle`, `DrawerDescription`, `DrawerTrigger`, and `DrawerClose`.
- Added React-equivalent data attributes, roles, ids, aria references, CSS custom properties, and event attributes.
- Added `DrawerOpenChangeReason` and bridged Drawer-specific reasons through Dialog with `Swipe` and `CloseWatcher`.
- Added a component-specific Drawer JavaScript module for DOM-heavy behavior: viewport measurement, swipe tracking, snap-point math, CloseWatcher handling, outside-press suppression during swipe-open, provider transforms, and CSS variable synchronization.
- Repaired explicit `SnapPoint=null` and `DefaultSnapPoint=null` semantics so parameter presence, not callback presence, controls fallback behavior.
- Repaired nested drawer count propagation for `--nested-drawers`, parent frontmost-height reporting, and nested reporter disposal so parent popup state matches React's nested drawer store updates.
- Repaired initially-open Android `CloseWatcher` setup by binding the watcher when the popup element becomes available and when popup state updates.
- Repaired snap release logic to use directional offsets, React-equivalent velocity thresholds, sequential snap target selection, null snap-point notification before swipe close, and document-level pointer release tracking for transformed drawer surfaces.
- Updated Dialog JavaScript only where Drawer integration requires it: Drawer outside-press suppression and pending-open callback focus resolution.
- Added retry-safe initial callback focus handling for Blazor render timing.
- Preserved Blazor lifecycle semantics instead of React state-sync loops.
- Added non-parallel Drawer Playwright collection isolation because Drawer coverage uses browser-level mouse gestures.
- Added unit coverage and Playwright coverage for open, close, focus, backdrop dismissal, escape dismissal, swipe open, swipe close, disabled swipe area, and snap points.
- Added Drawer demo pages and the reusable demo section, then expanded it with every source documentation scenario: hero side drawer, controlled state, position, nested drawers, snap points, indent effect, non-modal drawer, mobile navigation, swipe-to-open, action sheet, and detached triggers.
- Added demo animation coverage for Drawer transitions: CSS-backed directional panel motion, backdrop fade/blur, content reveal keyframes, handle reveal keyframes, swipe-rail pulse, snap/stack transform continuity, and reduced-motion handling.
- Replaced utility-dependent demo drawer sizing with explicit Fluent 2 surface, stroke, shadow, spacing, radius, and responsive sizing rules. This repaired full-width side drawers, full-screen snap-point presentation, non-modal close hit testing, nested drawer trigger hit testing, contained drawer shrink-wrap, and action-sheet surface styling.
- Added source spec artifacts at `../base-ui-specs/drawer/SPEC.md` and `../base-ui-specs/drawer/pitfalls.md`.

## Verification Commands

### Build

Command:

```bash
dotnet build BlazorBaseUI.slnx 2>&1 | tee docs/audits/logs/drawer-build.log
```

Result:

- Build succeeded.
- Warnings: 0
- Errors: 0
- Log: `docs/audits/logs/drawer-build.log`

### Unit Tests

Command:

```bash
dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --no-build --filter "FullyQualifiedName~DrawerTests" -v normal 2>&1 | tee docs/audits/logs/drawer-bunit.log
```

Result:

- Test run succeeded.
- Total tests: 15
- Passed: 15
- Failed: 0
- Log: `docs/audits/logs/drawer-bunit.log`

### Playwright Tests

Command:

```bash
dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --no-build --filter "FullyQualifiedName~Drawer" -v normal 2>&1 | tee docs/audits/logs/drawer-playwright.log
```

Result:

- Test run succeeded.
- Total tests: 16
- Passed: 16
- Failed: 0
- Server and WASM render modes passed.
- Log: `docs/audits/logs/drawer-playwright.log`

### Lint Rules

Command:

```bash
bash scripts/lint-rules.sh 2>&1 | tee docs/audits/logs/drawer-lint.log
```

Result:

- Total violations: 0
- Log: `docs/audits/logs/drawer-lint.log`
- Note: the macOS run emits BSD `grep` option warnings from script internals, but the script summary reports zero rule violations.

### Demo Animation Smoke

Commands:

```bash
dotnet run --project demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.csproj --urls http://127.0.0.1:5124
PWCLI="${PWCLI:-playwright-cli}"
"$PWCLI" open http://127.0.0.1:5124/drawer
"$PWCLI" snapshot
"$PWCLI" click e267
"$PWCLI" eval "(() => { const panel = document.querySelector('.drawer-demo-panel[data-open]'); const backdrop = document.querySelector('.drawer-demo-backdrop[data-open]'); const reveal = document.querySelector('.drawer-demo-panel[data-open] .drawer-demo-reveal'); return { panel: !!panel, panelTransition: panel ? getComputedStyle(panel).transitionProperty : null, panelDuration: panel ? getComputedStyle(panel).transitionDuration : null, backdrop: !!backdrop, backdropTransition: backdrop ? getComputedStyle(backdrop).transitionProperty : null, backdropDuration: backdrop ? getComputedStyle(backdrop).transitionDuration : null, reveal: !!reveal, revealAnimation: reveal ? getComputedStyle(reveal).animationName : null }; })()"
"$PWCLI" console error
"$PWCLI" close
```

Result:

- Wrapper executable check passed.
- Drawer page opened.
- Hero drawer opened.
- Panel transition: `transform, opacity, box-shadow, height, padding-bottom`.
- Panel duration: `0.26s`.
- Backdrop transition: `opacity, backdrop-filter`.
- Backdrop duration: `0.24s`.
- Reveal animation: `drawer-demo-content-in`.
- Console errors: 0.

### Demo Styling And Interaction Smoke

Commands:

```bash
dotnet run --project demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.csproj --urls http://127.0.0.1:5124
PWCLI="${PWCLI:-playwright-cli}"
"$PWCLI" open http://127.0.0.1:5124/drawer
"$PWCLI" resize 1280 720
"$PWCLI" run-code "desktop drawer demo geometry/assertions"
"$PWCLI" resize 390 844
"$PWCLI" run-code "mobile drawer demo geometry/assertions"
"$PWCLI" console error
"$PWCLI" close
```

Result:

- Desktop side drawers: 420px wide, 12px viewport margin, visible width 420px, pointer events `auto`.
- Mobile side drawers: 366px wide in a 390px viewport, 12px viewport margin, pointer events `auto`.
- Snap-point drawer: visible height 496px at the default `31rem` snap point on both desktop and mobile, not full screen.
- Non-modal drawer: close button click reduced open dialogs from 1 to 0.
- Nested drawers: clicking Security settings kept two open dialogs mounted; Account and Security content both remained present.
- Mobile navigation: 366px wide in a 390px viewport and 672px tall, with scrollable content contained inside the drawer.
- Action sheet: 480px desktop width and 390px mobile width, contained in separated Fluent surface cards.
- Console errors: 0.
- Log: `docs/audits/logs/drawer-playwright-demo-styling.log`

## Manual Checks

- Confirmed all React Drawer source files listed in scope have a Blazor equivalent or an explicit integration point.
- Confirmed Drawer uses `RenderElement<TState>` and does not cache `RenderFragment` content.
- Confirmed Drawer interop is component-specific in `blazor-baseui-drawer.js`.
- Confirmed shared Dialog JavaScript changes are limited to Drawer integration requirements.
- Confirmed DOM-heavy drag, measurement, focus, CloseWatcher, and snap-point work remains in JavaScript.
- Confirmed transformed popup and swipe-area gestures bind release listeners at the document level and track the active pointer id, preventing lost release events under Server/WASM suite load.
- Confirmed `ClassValue` and `StyleValue` use state-based callbacks where exposed.
- Confirmed Drawer demo is reachable from the demo navigation and includes every scenario listed in `.base-ui/docs/src/app/(docs)/react/components/drawer/page.mdx`.
- Confirmed Drawer demo animation is active in Playwright by checking computed panel transition properties, 260ms panel duration, 240ms backdrop duration, content reveal keyframes, and zero console errors.
- Confirmed Drawer demo sizing and spacing uses Fluent 2 tokens and explicit responsive constraints instead of missing generated utility classes.
- Confirmed non-modal close and nested drawer triggers are clickable because panel hit testing is explicitly restored with `pointer-events: auto`.
- Confirmed generated `.cs` component stubs contain only namespace, XML doc comment, and partial class declaration.

## Final Status

All audited Drawer functional gaps have been repaired. Build, bUnit, Playwright, and lint verification passed. The implementation is staged for review and not committed.

## Addendum — Upstream-Delta Re-Audit, July 18, 2026

The Drawer was re-audited against `.base-ui @ bdcb685fa` (origin/master, 2026-07-18), covering the 47 dependency-graph commits that landed upstream after this audit's baseline (`7c25be77`, 2026-05-27). Eight upstream changes were ported (headlined by the rewritten swipe engine #4867/#4980/#5105/#5112/#5057/#5181, the new `Drawer.VirtualKeyboardProvider` #4353, and the CloseWatcher topmost gate #4920), fourteen current-state parity gaps were repaired (including the elimination of per-frame JS→.NET swipe interop), shared dialog/button fixes #5096 and #4838 were landed, and every remaining in-window commit was verified as already present or React-only. An independent adversarial review found and fixed 1 blocker, 5 major, and 6 minor integration defects. Full disposition: `drawer-upstream-delta-2026-07.md`; verification evidence: `drawer-verification-report.md`; refreshed parity rows: `drawer-parity-matrix.md`.
