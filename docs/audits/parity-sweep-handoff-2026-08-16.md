# Blazix.BaseUI parity sweep — handoff (2026-08-16)

> **Historical snapshot — not a current work queue.** This is the handoff as written on 2026-08-16,
> preserved unedited below so the method and the reasoning behind it stay readable. Its "what to do
> next" list has since been worked: PR #190 retired the entire five-item Playwright cluster in §1,
> and #197, #198, and #199 closed #190's own follow-ups. For the current state of every finding —
> including the deviations those PRs shipped with — read
> [`parity-milestone1-dispositions.md`](parity-milestone1-dispositions.md) and
> [`parity-limitations.md`](parity-limitations.md), which supersede the "What to do next" and
> "Known deviations" sections below.

Paste this into a fresh session to continue the upstream-parity effort.

## Context

This repo ports [mui/base-ui](https://github.com/mui/base-ui) (React) to Blazor. A vendored
upstream checkout lives at `.base-ui/` (gitignored, local-only — a cloud/fresh clone will NOT
have it, nor the terser toolchain at `.base-ui/node_modules/.bin/terser`).

The screenshot-based parity suite cannot observe event propagation, keyboard/focus behavior,
ARIA wiring, or timing. Everything below came from diffing the C# port against `.base-ui`
source directly. Assume that is where the remaining bugs are too.

## What was completed

9 of the 10 pull requests below landed on master (`3c467edd`); #188 merged into
`fix/tooltip-previewcard-parity` only, after that branch had already merged into master.
15 components swept plus two cross-cutting fixes:

| PR | Scope |
|----|-------|
| #180 | Checkbox + Switch |
| #181 | Select + Combobox |
| #182 | Menu (+ submenu, composite) |
| #183 | ScrollArea + Button |
| #184 | Cross-cutting: pending-open invalidation (Menu/Select/Combobox) |
| #185 | Radio + Slider |
| #186 | Dialog + Popover |
| #187 | Tooltip + PreviewCard |
| #188 | **Did not land on master** — targeted `fix/tooltip-previewcard-parity`; merge commit `50c72af2` landed 19 seconds after that branch's merge into master (`6167ebfc`, PR #187) |
| #189 | NavigationMenu + Drawer |

Roughly 100 behavioral gaps fixed. Representative finds: inverted instant-dismiss mapping,
popups re-anchoring to unrelated triggers, disabled triggers still opening on hover/keyboard,
`aria-invalid` not gated on disabled, viewport inerted while focus was still inside it,
`TriggerId` never forwarded to the Drawer root, and `Math.Round` able to throw on Slider
precision.

## Working method that produced those results — keep it

1. **Isolated git worktree per sweep**, branched from `origin/master`. Other Claude sessions
   share this checkout; never run git state-changing commands in the main repo dir from a
   worktree session, and never edit `.base-ui/`.
2. **Two parallel Opus 5 reviewers (medium effort) -> one Opus 5 implementer (xhigh).**
   Reviewers are READ-ONLY and return structured findings with `status`
   (MISSING/PARTIAL/PRESENT), C#/JS `file:line` evidence, an upstream citation, and a `risk`
   rating.
3. **Always diff the shared utilities**, not just the component directory:
   `utils/popups/`, `utils/popupStateMapping.ts`, `utils/usePopupViewport.tsx`,
   `utils/useSwipeDismiss.ts`, and `floating-ui-react/hooks/*`. A Popover audit was previously
   declared complete while missing four fixes that lived in shared code.
4. **Treat prior audit notes as stale.** Several "known gaps" were already fixed, and at least
   one recorded limitation (NavigationMenu runtime direction, #189) turned out to have no real
   blocker — the sibling components already did it.
5. **Shared-code changes must be additive and opt-in**, defaulting to today's behavior (the
   `guardStaleOpen` pattern), and must be proven with the sibling test filters.
6. **Every new test must be verified to FAIL with its fix reverted.** This caught vacuous tests
   more than once.
7. **Never write a test that cannot observe the behavior.** bUnit has no pointer or focus model.
   If a gap needs those, say so and route it to Playwright rather than faking a green test.
8. **Sibling check every finding.** CodeRabbit reported bugs against one component four separate
   times when the identical defect also existed in a sibling (Tooltip/PreviewCard, Menu roots).

## Repo facts

- CI is **lint-only**. Green checks prove formatting, not correctness. Run
  `dotnet build Blazix.BaseUI.slnx` and the affected component tests locally before every push.
  Full unit suite is currently **2859 passing**.
- Solution file is `Blazix.BaseUI.slnx` (CLAUDE.md's `BlazorBaseUI.slnx` is wrong; MSB1009).
- Editing any `wwwroot/*.js` requires regenerating its `.min.js`:
  `.base-ui/node_modules/.bin/terser <f>.js --module -c -m -o <f>.min.js`.
  **Prove determinism first**: regenerate from `git show HEAD:<path>` and confirm it reproduces
  the committed `.min.js` byte-for-byte. The runtime loads `.min.js`, so a source-only edit is
  invisible (this matters when deliberately breaking something to validate a test).
- Conventions: `AGENTS.md` §2 strict member ordering; `.claude/rules/*.md` (JS interop
  circuit-safe guards, element/module guards, event-handler-override). Contract interface entry
  goes in **before** the test. Commits carry **no** `Co-Authored-By` footer.
- Skills: `/pr-review-workflow` (validate/fix/rebut/reply mechanics), `/babysit-pr` (multi-round
  bot review loop).
- **CodeRabbit is on the free OSS tier with a review quota.** When exhausted the check reports
  *pass* with the title "Review rate limited" — that is NOT an approval. Repeat
  `@coderabbitai review` requests push the reset window further out (observed 108 -> 64 -> 14 ->
  102 minutes). Ask the user to trigger it, and check for a genuine review via the walkthrough
  comment's `updated_at` (it is edited in place) plus "Actionable comments"/"Review finished",
  not just new comments. Also check the review body for "Outside diff range comments" — those
  are not inline and are easy to miss.

## What to do next — recommended order

### 1. Playwright-first branch (highest leverage)
Five deferred items are blocked by the same root cause: shared-JS behavior bUnit cannot test.
One branch with Playwright coverage retires the whole cluster.
- `restMs` rest-timer hover open (shared `createHoverInteraction`; Tooltip + Popover). Upstream
  opens only once the pointer *rests*; the port arms a plain enter-timer.
- Nested tooltip triggers (`closestEnabledTooltipTrigger`, `shouldOpen` veto across C#/JS).
- Global Escape closes only the first open root, no `preventDefault` (shared
  `createEscapeKeyHandler`). #189 fixed this for NavigationMenu locally; other families remain.
- Popover sloppy-touch dismissal timing (touchstart bookkeeping, 5px/10px thresholds, 1s window).
- NavigationMenu focus-guard `isOutsideEvent` (needs a cross-component guard registry and a
  `focusOut` close path).
Precedent recorded only on #188's unlanded branch: it added a Playwright `mouseOnly` test to the
shared `*TestsBase.cs` so both Server and WASM inherit it, with an identical mouse-pointer positive
control, and validated it by flipping the gate in both `.js` and `.min.js`.

### 2. Two genuine defects, cheap relative to impact
- **Dialog runs two focus managers** — the C# `FloatingFocusManager` and JS `focusPopup` both
  call `createFloatingFocusManager` on the same popup. `markOthers` is ref-counted so the union
  of both avoid-lists wins, and return focus runs twice with different targets. Each owner holds
  behavior the other lacks, so this is a consolidation plus a migration of the dropped options.
- **`PopoverRoot.SyncImplicitActiveTrigger`** runs during each trigger registration, so with
  several triggers the *first* registered becomes the implicit active trigger. Upstream's
  `useImplicitActiveTrigger` applies only when the final `triggerCount` is 1.

### 3. Remaining component sweeps
- Derivative layers (fast confidence pass, likely inherit recent fixes):
  `AlertDialog`, `ContextMenu`, `MenuBar`
- Substantial: `Autocomplete`
- Form family: `Field`, `Form`, `Fieldset`, `Input`, `NumberField`, `OtpField`,
  `CheckboxGroup`, `RadioGroup`
- Disclosure/toggle: `Accordion`, `Collapsible`, `Tabs`, `Toggle`, `ToggleGroup`, `Toolbar`
- Primitives (low risk): `Avatar`, `Separator`, `Progress`, `Meter`, `Portal`

### 4. Decisions blocked on the maintainer (not on work)
- Slider: cancelable `OnKeyDown` args (new public args type); `tabindex="-1"` on control/thumb
  wrappers (asserted by existing tests, so removing is a behavioral call).
- NavigationMenu `actionsRef` (new public Parameter).
- Combobox `Locale` parameter (currently uses ambient `CurrentCultureIgnoreCase`).
- `DialogHandle` per-mount lifecycle (#5149 fresh-state-per-mount / newest-root-wins /
  `isOpen` false while detached). `ComponentHandleBase` is shared by four families.
- Modal outside-press requiring the owning backdrop. **Do not apply upstream's rule as-is**: the
  port renders its internal backdrop with `PointerEvents="none"`, so it would make modal dialogs
  *undismissable*. The prerequisite is making that backdrop pointer-interactive, which changes
  modal semantics for Drawer/Menu/Select/Popover too.
- `BaselineStore.ValidatePlatform` (parity harness) rejects 3-component browser versions
  (Firefox `144.0.2`, WebKit `26.0`), but `RejectsParseableButNoncanonicalBrowserVersions`
  deliberately asserts that rejection. Relax + update the test, or drop those browsers.

### 5. Deliberate declines — recorded so they are not re-litigated
- `data-instant="trigger-change"` without a Viewport: needs a positioner-scoped
  "animations finished" signal the port lacks; half the fix is worse than none.
- NavigationMenu full C# ownership of content `data-open`/`data-closed`: JS writes them
  synchronously; moving them to C# adds visible Blazor Server latency.
- NavigationMenu viewport guards/target div rendered only when `!hasPositioner`: would break
  content portaling.
- `data-disabled` on the NavigationMenu trigger: upstream has no such attribute — fidelity
  violation. The disabled gate uses the existing `aria-disabled`.
- Upstream `#5194`, `2437d817e`, `#5003`: React-internal (dead-code dedup, MutationObserver
  index registry, SSR prehydration bundling). No Blazor-observable behavior. Closed.

## Known deviations (recorded, not defects)
- #189 `restMs` keys on `isOpen` rather than upstream's `mounted` (the port's JS state has no
  `mounted`), so the full delay still applies during the exit transition. Conservative.
- Slider: with no realtime subscriber attached, JS never calls `OnDragMove`, so a drag that
  moves away and returns to its origin will not commit where upstream would. With a subscriber
  attached, behavior matches upstream exactly.
