# Docs Site Overhaul — Base UI Layout Parity

**Date:** 2026-06-12
**Status:** Approved
**App:** `docs/Blazix.BaseUI.Docs` (Blazor Web App, InteractiveAuto, Tailwind Play CDN)

## Goal

Rebuild the docs site shell and component-page format to mimic the base-ui.com
documentation layout while keeping the existing Liquid Glass design system
(`docs/DESIGN.md`). The sidebar lists every Blazix component; only Accordion gets
full documentation in this pass — everything else is a stub page. The showcase
("Demo") component shows live demos with CSS and Tailwind source variants and a
copy button. The docs UI itself is built strictly from Blazix.BaseUI components
wherever an interactive primitive is needed.

## Non-Goals

- Documenting any component other than Accordion.
- Combobox / OTP Field pages (Blazix doesn't have these components).
- A separate Radio Group page (the source docs cover it under Radio).
- Markdown-driven page rendering (pages stay code-first Razor).

## 1. Shell Layout

Mimics base-ui.com structure; Liquid Glass stays the material language (glass =
chrome and transient surfaces, opaque content layer, ambient wallpaper kept).

- **Header:** slim fixed glass bar — wordmark (links home), GitHub link, the
  **dark mode switch**, and the **Server/WASM runtime switch**. On mobile, a
  menu button opens the nav in a Blazix **Drawer** (which also hosts both
  switches).
- **Dark mode switch:** the existing `ThemeToggle` behavior, rebuilt on the
  Blazix **Switch** component. Persistence stays as-is: `blazix-docs-theme` in
  localStorage plus the inline boot script that applies the class before first
  paint (no FOUC); system preference is the fallback.
- **Server/WASM runtime switch:** a Blazix **ToggleGroup** with two items,
  "Server" and "WASM", showing the active runtime. Render mode is site-wide:
  selecting a runtime writes a `blazix-docs-render-mode` cookie via the docs JS
  module and reloads; `App.razor` reads the cookie during static rendering and
  applies `InteractiveServer` (default, no cookie) or
  `InteractiveWebAssembly` to `Routes`/`HeadOutlet`. The current fixed
  `InteractiveAuto` is dropped — the switch must truthfully display the active
  runtime, and Auto's server-then-WASM handoff can't. Per-demo render-mode
  islands were rejected: they'd force a static shell and break interactive nav
  components.
- **Three-column grid (desktop):**
  - Left: **SideNav**, ~17.5rem, sticky, scrollable via Blazix **ScrollArea**.
  - Center: content column, max-width ~48rem.
  - Right: **QuickNav** ("On this page") built from headings registered by the
    page; scroll-spy via an `IntersectionObserver` JS module (observers belong
    in JS per the interop rules).
- **Responsive collapse:** quick-nav column drops first, then side-nav (mobile
  uses the Drawer).
- `DocsWindow` framing around the main content is removed in favor of the open
  content column. Shared class strings remain centralized in
  `LiquidGlassClasses.cs`.

## 2. Navigation Data

`ComponentCatalog` is replaced by a `DocsNav` catalog of sections → links:

- **Overview:** Quick Start, About, Accessibility. Quick Start is real and is
  the home page — the nav link points at `/`, replacing the current Home
  content. About and Accessibility are stubs.
- **Handbook:** Animation, Composition, Customization, Forms, Styling — all
  stubs. (Base UI's "TypeScript" page is dropped as not applicable to Blazor.)
- **Components (35, Base UI order):** Accordion, Alert Dialog, Autocomplete,
  Avatar, Button, Checkbox, Checkbox Group, Collapsible, Context Menu, Dialog,
  Drawer, Field, Fieldset, Form, Input, Menu, Menubar, Meter, Navigation Menu,
  Number Field, Popover, Preview Card, Progress, Radio, Scroll Area, Select,
  Separator, Slider, Switch, Tabs, Toast, Toggle, Toggle Group, Toolbar,
  Tooltip.
- **Utils:** CSP Provider, Direction Provider, Portal, Render Element — stubs
  mapped to the actual Blazix utilities.

Routes: `/components/{slug}`, `/handbook/{slug}`, `/overview/{slug}`,
`/utils/{slug}`. The existing raw-markdown route `/components/{slug}.md`
(served from `Content/Components`) is preserved; `accordion.md` is updated to
match the new page.

Nav metadata per link: `Section`, `Name`, `Slug`, `Summary`, `IsDocumented`.

## 3. Component Page Template

Documented components get a dedicated Razor page following the source page
section order:

1. Title (h1) + subtitle line
2. Hero **Demo**
3. **Anatomy** — import note + code skeleton of all parts
4. **Examples** — each example is a Demo with its own heading
5. **API reference** — one subsection per part (e.g. Root, Item, Header,
   Trigger, Panel), each with up to three tables: **Parameters**,
   **Data attributes**, **CSS variables**

Data model: `ComponentPart { Name, Description, Parameters: ApiRow[],
DataAttributes: ApiRow[], CssVariables: ApiRow[] }` where
`ApiRow { Name, Type, DefaultValue, Description }` (Type/Default omitted for
attribute/variable tables).

Headings register themselves (id, title, level) with a cascading TOC context so
QuickNav renders "On this page" automatically.

Undocumented components/pages render a shared stub: title, summary,
"documentation in progress" callout.

## 4. Showcase ("Demo") Component

Mirrors the Base UI demo UX, built from Blazix components:

- **Live preview** on top — the real demo component rendered inline.
- **Toolbar** below the preview:
  - Left: file tabs (Blazix **Tabs**) when the active variant has multiple
    files.
  - Right: variant selector (Blazix **Select**) with options **CSS** and
    **Tailwind**; **copy button** that copies the active file's source via a
    clipboard JS module (circuit-safe guard), showing a check icon for ~2s,
    with a Blazix **Tooltip** label.
- **Code block:** collapsed past ~12 lines with gradient fade and a
  "Show code" expander (Blazix **Collapsible**).
- **Variants:**
  - *Tailwind*: single `.razor` file, utility classes inline. Play CDN is
    Tailwind v3 — use `data-[open]:` style variants and alpha values in
    multiples of 5.
  - *CSS*: `.razor` file + stylesheet shown as a second file tab (the Blazor
    analog of the source docs' CSS Modules variant).
- **No drift:** demo sources live at
  `Blazix.BaseUI.Docs.Client/Components/Demos/{Component}/{DemoName}/{Variant}/`
  and are included as `EmbeddedResource` in the client csproj. The Demo
  component reads file text from the assembly manifest at runtime (works under
  both Server and WASM render modes). Authors never paste code strings.
- Demo registration: a small `DemoSource` model
  `{ Variant, Files: [{ Name, Language, ResourcePath }] }` passed to the Demo
  component alongside the live `ChildContent`.
- **Syntax highlighting:** highlight.js from CDN; re-highlight on tab/variant
  switch through the docs JS module.

## 5. Accordion Content

Ported from the Base UI accordion page plus existing `AccordionDocs.razor` /
`accordion.md`:

- Hero demo: single-open accordion with chevron rotation and panel height
  animation (`--accordion-panel-height`), in both CSS and Tailwind variants.
- Example: "Open multiple panels" (`Multiple`), both variants.
- Anatomy snippet for AccordionRoot / Item / Header / Trigger / Panel.
- Per-part API tables sourced from the actual Blazix parameter sets
  (Root: Value, DefaultValue, Multiple, Disabled, Orientation, LoopFocus,
  KeepMounted, HiddenUntilFound, OnValueChange, …; plus data attributes like
  `data-open`, `data-disabled`, `data-starting-style`, `data-ending-style` and
  CSS variables `--accordion-panel-height` / `--accordion-panel-width`).
- `Content/Components/accordion.md` updated to mirror the page.

## 6. JS Interop

One docs JS module (lazy `Lazy<Task<IJSObjectReference>>`, circuit-safe
try/catch guards, `Element.HasValue` guards, dispose pattern per project
rules) providing:

- `copyText(text)` — clipboard write.
- TOC scroll-spy — `IntersectionObserver` over registered heading ids,
  callback to .NET for active-link highlight.
- `highlight(element)` — invoke highlight.js on swapped code blocks.
- `setRenderMode(mode)` — write the `blazix-docs-render-mode` cookie and
  reload.

## 7. Verification Criteria

- `dotnet build` solution: 0 errors.
- Preview run-through:
  - Sidebar shows 4 sections; Components lists all 35 in the specified order.
  - Accordion page sections match the source order (hero → anatomy → examples
    → per-part API reference).
  - Demo: variant switch swaps code (and file tabs for CSS variant), copy
    button writes the active file to the clipboard and shows check feedback,
    collapsed code expands.
  - Stub pages render for undocumented links; `/components/accordion.md`
    still serves markdown.
  - Mobile width: Drawer nav works; quick-nav hidden.
  - Dark mode switch toggles and persists the theme;
    `prefers-reduced-motion` fallbacks still behave.
  - Runtime switch: site loads as Server by default; selecting WASM reloads
    and the demos run under WebAssembly (verify via the switch state and
    interactivity after reload); selection persists across navigation.
