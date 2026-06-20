# Next session — document the next component

> Scratch handoff. **Do not commit.** Update the "Next target" + "Where things
> stand" as you go; delete once every component is documented.

## Where things stand

- The docs overhaul is **merged to `master`** (PR #102). The shell, Demo showcase,
  Server/WASM + dark-mode switches, and component pages are live.
- **Fully documented component pages (sidebar order):** Accordion, Alert Dialog,
  Autocomplete, Avatar, Button, Checkbox, Checkbox Group, Collapsible, Context Menu,
  Dialog, Drawer, Field, Fieldset, Form, Input, Menu, Menubar, Meter, Navigation Menu,
  Number Field, Popover, Preview Card, Progress, Radio, Scroll Area, Select,
  Separator, Slider, Switch, Tabs, Toast, Toggle, Toggle Group, Toolbar, Tooltip.
  Each is flipped to `IsDocumented: true` in `DocsNav.cs`.
- Current in-progress branch: `docs/phase-t`. It contains uncommitted docs for
  Tabs, Toast, Toggle, Toggle Group, Toolbar, and Tooltip. All Base UI source
  examples found for these components are represented in the pages or source snippets.
- Verification was run against the local docs host on port 5216:
  `dotnet build Blazix.BaseUI.slnx` passed with 0 warnings and 0 errors, and
  `bash scripts/lint-rules.sh` passed with 0 violations. Playwright verified the
  T pages, markdown endpoints, demo source tabs, CSS/Tailwind variant switches, copy
  buttons, collapsed code expansion, sample interactions, and Toast/Tooltip Server +
  WASM render-mode behavior. Desktop/mobile screenshots live in `output/playwright/phase-t/`.
- Follow-up feedback was addressed on the same branch:
  - Toggle now uses the exact Base UI outline heart path while unpressed and shows the
    filled heart only after pressing.
  - Tooltip's Bold icon was checked against source; Base UI uses a filled glyph, but
    the docs intentionally use outlined/stroked Bold, Italic, and Underline icons per
    follow-up review.
  - Toast was redone against source for hero, anchored, custom position, undo,
    promise, custom data, dedupe, and varying-height examples. The page now uses the
    stacked/Sonner CSS variables/transforms, and the top-position demo has its own
    source-equivalent top-origin stack rules.
  - Toast top placement was investigated against `ToastRoot`, `ToastViewport`, and
    `ToastStore`: the component emits the expected stack variables and data attrs;
    the off-screen top behavior was caused by docs CSS reusing bottom-stack root rules.
  - Toast anchored now mirrors the source copy-trigger example more closely and uses
    the source side-aware arrow rules; Playwright confirmed the arrow centers on the
    copy button.
  - Additional verification was run on port 5228 in Development mode: regression grep,
    browser checks, `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.csproj`,
    and `bash scripts/lint-rules.sh` all passed.
- Durable verification notes live in
  `docs/audits/phase-t-docs-sample-test-report.md`.
- Run it: `dotnet run --project docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs`
  (both docs projects are in `Blazix.BaseUI.slnx`; there is no nested docs `.sln`).
  Or use the `docs` profile in `.claude/launch.json` (port 5216).
- Design rules: [docs/DESIGN.md](docs/DESIGN.md). Plan/spec the original Accordion
  work followed: `docs/superpowers/plans/2026-06-12-docs-site-overhaul.md`.

## Next target: component docs complete

All component entries in `DocsNav.cs` are now documented. Next session can either:

- review and polish the overview/handbook/utils docs; or
- delete this scratch handoff after the phase T branch is personally tested and merged.

## The recipe (mirror the documented components 1:1 — that's the whole point)

For a component with display name `Foo Bar` and slug `foo-bar`, mirror the existing
file set (Accordion / Avatar are the cleanest references; Autocomplete shows the
many-parts case):

1. **Demo components** — real Blazix components, plain `class="..."` only (NO
   `ClassValue`, NO `LiquidGlassClasses`, copy-pasteable user code):
   - `…Client/Components/Demos/FooBar/Hero/Css/FooBarHeroCss.razor`
   - `…Client/Components/Demos/FooBar/Hero/Tailwind/FooBarHeroTailwind.razor`
   - (+ a second example folder **only if base-ui's page has one**, e.g.
     `…/SomeExample/Css` + `…/SomeExample/Tailwind`. Avatar had none — don't force it.)
   - CSS and Tailwind variants must render **identically**.
2. **Shared CSS-variant stylesheet** — `…/Docs/wwwroot/demos/foo-bar.css`,
   classes prefixed `.blx-foo-bar-*` to avoid collisions.
3. **Link it** in `…/Docs/Components/App.razor` `<head>`, next to the existing
   `demos/*.min.css` links: `<link rel="stylesheet" href="@Assets["demos/foo-bar.min.css"]" />`.
4. **API data** — `…/Client/Data/FooBarApi.cs`:
   `public static class FooBarApi { public static IReadOnlyList<ComponentPart> Parts { get; } = [ … ]; }`
   one `ComponentPart` per part (Parameters / DataAttributes / CssVariables),
   reusing the existing `ComponentPart` + `ApiRow` records. Document **all** public
   parts in the API reference even if the hero/anatomy omits some.
5. **Page** — `…/Client/Pages/FooBarPage.razor`, `@page "/components/foo-bar"`.
   A literal route **wins** over the `/components/{Slug}` stub, so this just works.
   Section order (match the docs / base-ui): `DocsPageIntro` → hero `<Demo>` →
   `<DocsHeading Title="Anatomy">` + `<CodeBlock>` → [`<DocsHeading Title="Examples">`
   (+ `Level="3"` per example) only if there are examples] → `<DocsHeading
   Title="API reference">` then `@foreach (var part in FooBarApi.Parts) {
   <ApiPartReference Part="part" /> }`.
6. **`_Imports.razor`** (`…/Client/_Imports.razor`) — add `@using Blazix.BaseUI.FooBar`
   and the `Css` demo namespaces the page renders, e.g.
   `@using …Client.Components.Demos.FooBar.Hero.Css`. (Only the **Css** variant is
   rendered live; the Tailwind variant is shown as source only, so it doesn't need
   a `_Imports` entry — but it must still compile.)
7. **Markdown** — rewrite `…/Docs/Content/Components/foo-bar.md` (already exists as a
   stub) to mirror the page, like `accordion.md` / `avatar.md` do.
8. **DocsNav** — flip the `foo-bar` entry in `…/Client/Data/DocsNav.cs` to
   `IsDocumented: true`.

### `DemoVariant` / `DemoFile` wiring (in the page's `@code`)

`DemoFile.ResourcePath` is the embedded-resource manifest name **minus** the
`Blazix.BaseUI.Docs.Client.` prefix, folders joined by dots, e.g.
`Components.Demos.FooBar.Hero.Css.FooBarHeroCss.razor` and `wwwroot.demos.foo-bar.css`.
The csproj already embeds `Components\Demos\**\*.razor` and `wwwroot\demos\**\*.css`
— no csproj change needed.

✅ **Hyphenated css names are preserved** in the manifest (confirmed: `alert-dialog.css`
→ `…wwwroot.demos.alert-dialog.css` resolves fine). Still cheap to verify a new
hyphenated name after building — the Demo loader throws an `InvalidOperationException`
naming the missing resource if `ResourcePath` is wrong:

```bash
cd docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client
python3 -c "d=open('bin/Debug/net10.0/Blazix.BaseUI.Docs.Client.dll','rb').read(); print(b'Blazix.BaseUI.Docs.Client.wwwroot.demos.foo-bar.css' in d)"
```

## Conventions that bit us — don't relearn them

- **Tailwind is Play CDN v3.** Alpha modifiers must be **multiples of 5**
  (`bg-white/55`, not `/52`). Use `data-[x]:` / `group-data-[x]:` arbitrary
  variants (NOT v4 bare `data-x:`). Arbitrary props like `[backdrop-filter:url(#id)]`,
  `w-[var(--anchor-width)]`, `max-h-[min(18rem,var(--available-height))]` are fine.
- **CSS-variant dark mode uses `.dark` class selectors**, never
  `@media (prefers-color-scheme)`. The docs toggle dark mode by putting `.dark` on
  `<html>` (Tailwind `darkMode: 'class'`), so OS-preference media queries won't track
  the in-page switch and the CSS variant would diverge from the Tailwind `dark:`
  variant. Write `.dark .blx-foo-bar-popup { … }`.
- **Data attributes are presence-based** (`data-open`, `data-highlighted`, etc.
  emitted only when true) — presence selectors `[data-open]` work. Confirmed across
  Accordion / Alert Dialog / Autocomplete.
- **Restart the dev server after adding a page or an `App.razor` head link.** Blazor
  does not hot-reload new `@page` routes or host-`<head>` changes; `preview_stop` +
  `preview_start` (or restart `dotnet run`).
- **Verify under BOTH render modes for animated/overlay components.** Set the
  `blazix-docs-render-mode` cookie (`server` / `wasm`) and reload — WASM coalesces
  renders into one frame, so animation/timing bugs differ from Server. Gate any
  transition on a real paint, never on render count. (Pure-CSS transitions driven by
  the library's data attributes — Alert Dialog enter/exit — behaved identically in both.)
- **Overlay/portal components** (Alert Dialog, Autocomplete popup) render through a
  portal to `document.body`, so they overlay the whole page, not the Demo card —
  that's expected. When an overlay is open it covers the demo card and flattens the
  "Show code" pill's refraction; screenshot the **closed** state for visual proof.
- **Toast docs previews have several standalone providers on one page.** Each source
  sample keeps its own `ToastProvider`/`ToastViewport`; the docs render those previews
  inline, so fixed viewport layers can collide across sections. Phase T uses a
  docs-only `ToastDemoViewportLayer` helper to lift the last interacted Toast viewport.
  Keep that pattern for any new fixed Toast sample, and verify with `elementFromPoint`
  while older toasts from other sections are still visible.
- **`liquid-prism` / `liquid-wobble`** (the demo "Show code" button refraction) must
  stay on `backdrop-filter`, never element `filter` — an element filter warps the
  label. It's in the shared `Demo.razor`, identical on every page. Documented in DESIGN.md.
- **Big components → dispatch a research subagent.** Autocomplete had 21 public
  parts; a background `general-purpose` agent extracted the per-part API + the
  authoritative Blazor composition (from `demo/.../Sections/<Name>Section.razor`)
  while the simpler component was built in parallel. Generic parts (e.g.
  `AutocompleteRoot<TValue>`, `Item`, `Collection`) need an explicit `TValue="…"`.
- **The demo-app `…Section.razor` files are the authoritative composition.**
  `demo/Blazix.BaseUI.Demo/Blazix.BaseUI.Demo.Client/Shared/Sections/<Name>Section.razor`
  shows exactly how to wire each component in Blazor (binding, item sources, context).
  Read it before writing demos — it's how the `bool?` `Open`/`OpenChanged` and the
  `Items` + `Collection`/`@bind-Value` patterns were confirmed.

## Done-when

- `dotnet build Blazix.BaseUI.slnx` → 0 errors, 0 warnings.
- `/components/foo-bar`: hero demo works, CSS/Tailwind variant switch + file tabs +
  copy button work, collapsed code expands, QuickNav lists the parts and scroll-spies.
  `/components/foo-bar.md` serves the new markdown. Verify in the browser preview
  (the `Claude_Preview` tools), both render modes for anything animated/overlay.
- Spot-check the API tables against `src/Blazix.BaseUI/FooBar/*.cs` / `*.razor`.
