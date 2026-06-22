# Handbook Documentation Pages — Design

**Goal:** Replace the five stubbed Handbook pages (Animation, Composition, Customization, Forms, Styling) in `docs/Blazix.BaseUI.Docs` with full documentation, re-authored from base-ui's React handbook to Blazix.BaseUI's real Blazor/C# API, following the existing documented-page conventions.

**Source mirror:** `.base-ui/docs/src/app/(docs)/react/handbook/{animation,composition,customization,forms,styling}/page.mdx`

**Companion to:** `docs/superpowers/specs/2026-06-12-docs-site-overhaul-design.md` (the docs site this extends).

---

## Principles

- **Blazix-accurate, not transliterated.** Every concept is documented as the library actually implements it (verified against `src/Blazix.BaseUI/`), not as a literal port of React idioms.
- **Match existing conventions.** Reuse `DocsPageIntro`, `DocsHeading`, `CodeBlock`, `Demo`/`DemoVariant`/`DemoFile`, and the markdown-source parity that component pages already use. No new doc primitives.
- **Surgical.** Touch only handbook-related files. Keep the `HandbookPage.razor` stub (mirrors how `ComponentPage.razor` is kept even though all components are documented).

## API mapping (React → Blazix), verified against source

| base-ui (React) | Blazix.BaseUI (Blazor) | Source |
|---|---|---|
| `className` / `className={state => …}` | `class` / `ClassValue` (`Func<TState,string?>`) | `RenderElement.razor:53` |
| `style` / `style={state => …}` | `style` / `StyleValue` (`Func<TState,string?>`) | `RenderElement.razor:59` |
| `render={<El/>}` / `render={(props,state)=>…}` | `Render` (`RenderFragment<RenderProps<TState>>`) | `RenderElement.razor:47` |
| `onOpenChange(open, eventDetails)` | `OnOpenChange` with `*OpenChangeEventArgs` | `OpenChangeEventArgs.cs` |
| `eventDetails.reason` (string) | `args.Reason` (type-safe enum) | `OpenChangeEventArgs.cs:30` |
| `eventDetails.cancel()` | `args.Cancel()` / `args.IsCanceled` | `OpenChangeEventArgs.cs:50` |
| `eventDetails.allowPropagation()` | `args.AllowPropagation()` | `OpenChangeEventArgs.cs:55` |
| (n/a) | `args.PreventUnmountOnClose()` | `OpenChangeEventArgs.cs:61` |
| `open` + `onOpenChange={setOpen}` | `@bind-Open` or `Open` + `OnOpenChange` | component roots |
| `[data-starting-style]`/`[data-ending-style]`/`[data-open]`/`[data-closed]` | identical | library emits same attrs |
| `keepMounted` on `Portal` | `KeepMounted` on `*Portal` | `Popover/PopoverPortal.razor:28` |
| react-hook-form / TanStack Form | `EditForm` + DataAnnotations + Blazix Field parts | `Content/Components/form.md` |

## Per-page design

Each page is a routable Razor page under `Pages/Handbook/` with a literal `@page` route (shadows the `/handbook/{Slug}` stub), a `DocsPageIntro` (with `MarkdownHref`), `DocsHeading` sections, and `CodeBlock`s. Animation and Forms additionally embed a live `Demo` with CSS + Tailwind variants.

### 1. Styling — `/handbook/styling` (prose + code)

Sections:
- **Style hooks**
  - CSS classes — `class` and `ClassValue` (function form receives state).
  - Data attributes — styling component states (e.g. `[data-checked]`/`[data-unchecked]`, `[data-open]`/`[data-closed]`); point to each component's API reference.
  - CSS variables — dynamic values exposed on parts (e.g. Popover `--available-height`, `--anchor-width`, `--transform-origin`).
  - Style prop — `style` and `StyleValue` (function form).
- **Tailwind CSS** — apply utility classes via `class`/`ClassValue`.
- **Plain CSS** — apply named classes and style them in a stylesheet (replaces base-ui's "CSS Modules" / "CSS-in-JS" sections; matches the docs' established CSS+Tailwind variant story).

### 2. Composition — `/handbook/composition` (prose + code)

Sections:
- **Composing custom components** — use `Render` to render a part as your own Blazor component; the custom component must forward attributes/`@ref` to its root element.
- **Composing multiple components** — nesting `Render` delegates (e.g. Tooltip trigger composed with a Dialog/Menu trigger).
- **Changing the default rendered element** — use `Render` to render a different element (e.g. a Menu item as an `<a>`).
- **Render with state** — the `RenderProps<TState>` context exposes attributes + component state for full control / conditional content.

### 3. Customization — `/handbook/customization` (prose + code)

Sections:
- **Blazix change events** — signature shape `OnOpenChange="EventCallback<…OpenChangeEventArgs>"`; the args object and its members (`Open`/`Value`, `Reason`, `Cancel`, `AllowPropagation`, `IsCanceled`, `IsPropagationAllowed`, `PreventUnmountOnClose`).
  - **Canceling a change** — `args.Cancel()` to keep a component uncontrolled while blocking a specific transition (reason-gated).
  - **Allowing propagation** — `args.AllowPropagation()` for Esc-key propagation across nested popups.
- **Controlling components with state** — uncontrolled by default; control via `@bind-Open`/`@bind-Value` or explicit `Open`+`OnOpenChange`; example opening a Dialog from a timer without a trigger.

### 4. Animation — `/handbook/animation` (prose + live demo)

Sections:
- Intro: components animate with CSS transitions or CSS animations via data attributes.
- **CSS transitions** — `[data-starting-style]` / `[data-ending-style]` (recommended; cancellable mid-flight).
- **CSS animations** — `[data-open]` / `[data-closed]` keyframes.
- **Keeping components mounted** — `KeepMounted` on the `*Portal` part.
- **Live demo** — reuse the existing **Popover Hero** demo, which already animates via `[data-starting-style]`/`[data-ending-style]` transitions (`wwwroot/demos/popover.css`). No new demo files; the prose walks through the relevant CSS.
- **JavaScript-driven animation** — short note: animation driven by JS belongs in a JS interop module (per project JS-interop rules); no Motion/Framer port (decision below).

### 5. Forms — `/handbook/forms` (prose + live demo)

Sections (Blazix Field/Fieldset/Form):
- **Naming form controls** — `FieldRoot.Name` (precedence) / `FieldControl<TValue>.Name`.
- **Describing the control** — `FieldDescription` (joins `aria-describedby`), `FieldError`.
- **Labeling control groups** — `FieldsetRoot` + `FieldsetLegend` (`aria-labelledby`), `Disabled` on the group.
- **Building form fields** — `FieldRoot` + `FieldLabel` + `FieldControl` + `FieldDescription` + `FieldError`; field state data attributes (`data-valid`, `data-invalid`, `data-touched`, `data-dirty`, `data-filled`, `data-focused`, `data-disabled`).
- **Submitting data** — `OnFormSubmit` → `FormSubmitEventArgs.Values` (keyed by field name), or read the bound `Model`.
- **Validation**
  - Native constraint validation (HTML attributes like `required`).
  - Custom validation (`FieldValidity` render context).
  - Server-side validation (`Form.Errors` keyed by `FieldRoot.Name`, surfaced via `FieldError`).
  - Displaying errors (`FieldError` / `FieldValidity`).
- **Blazor integration** — Blazix `Form` builds an `EditContext` from `Model` and validates via `editContext.Validate()` on submit; drop a standard `<DataAnnotationsValidator />` inside `<Form>` (the Form cascades its `EditContext`, Form.razor:4) to honor `[Required]`/`[EmailAddress]` etc.; branch with `OnValidSubmit`/`OnInvalidSubmit`; choose timing via `ValidationMode` (OnSubmit/OnBlur/OnChange). Replaces base-ui's React-Hook-Form / TanStack Form sections.
- **Live demo** — reuse the existing **Form Hero** demo (Field + Form + validation + external errors).

## Cross-cutting implementation

1. **`DocsNav.cs`** — set `IsDocumented: true` on the 5 Handbook links.
2. **Pages** — create `Pages/Handbook/{Animation,Composition,Customization,Forms,Styling}Page.razor`, each with its literal `@page "/handbook/<slug>"`, `<PageTitle>`, and `DocsPageIntro` (Eyebrow="Handbook", `MarkdownHref="/handbook/<slug>.md"`). Keep `Pages/HandbookPage.razor` as the fallback stub.
3. **Markdown source** — create `Content/Handbook/{slug}.md` (5 files) and add a `/handbook/{slug}.md` minimal-API route to `Program.cs` mirroring the existing `/components/{slug}.md` route (reads `Content/Handbook/{slug}.md`).
4. **Demos** — reuse the existing **Popover Hero** (Animation) and **Form Hero** (Forms) demos. Wire them into the handbook pages via `DemoVariant`/`DemoFile` pointing at the existing embedded resources (e.g. `Components.Demos.Popover.Hero.Css.PopoverHeroCss.razor`, `wwwroot.demos.popover.css`), exactly like `AccordionPage`. No new demo `.razor`/`.css` files.

## Decisions

- **Demos:** live demos on Animation + Forms; prose + static code on Composition, Customization, Styling. (Matches base-ui.)
- **Markdown parity:** full parity — `.md` source files + route + `Open .md` button. (Matches component pages.)
- **Forms depth:** native Blazor integration — `EditForm` + DataAnnotations + Blazix Field parts.
- **Animation JS section:** omit the Motion/Framer demos; keep one short note that JS-driven animation belongs in a JS interop module.

## Out of scope

- The Overview (`about`, `accessibility`) and Utils stub pages.
- Any change to the library (`src/Blazix.BaseUI/`).
- New doc UI primitives or layout changes.

## Verification

- `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.sln` → 0 errors.
- Preview in **both** Server and WASM render modes (known timing differences between modes):
  - All 5 handbook routes render inside the content column; sidebar marks them as documented (active styling).
  - Animation and Forms demos render and run (Popover opens/animates; form validates/submits); CSS and Tailwind variant tabs both show source.
  - `/handbook/<slug>.md` endpoints return the markdown; `Open .md` button links correctly.
  - Scroll-spy ("On this page") tracks the new headings.
