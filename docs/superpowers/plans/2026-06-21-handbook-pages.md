# Handbook Documentation Pages Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the five stubbed Handbook pages (Animation, Composition, Customization, Forms, Styling) with full documentation re-authored to Blazix.BaseUI's real Blazor/C# API, matching the existing documented-page conventions.

**Architecture:** Each handbook page becomes a dedicated routable Razor page under `Pages/Handbook/` with a literal `@page "/handbook/<slug>"` route (which shadows the existing `/handbook/{Slug}` stub). Pages reuse the existing doc primitives (`DocsPageIntro`, `DocsHeading`, `CodeBlock`, `Demo`). Animation and Forms reuse the already-embedded **Popover Hero** and **Form Hero** demos. Markdown source files under `Content/Handbook/` are served by a new minimal-API route mirroring the components route.

**Tech Stack:** .NET 10 Blazor Web App, Blazix.BaseUI, Tailwind (Play CDN), highlight.js. No test project (docs convention — verification is build + preview).

**Spec:** `docs/superpowers/specs/2026-06-21-handbook-pages-design.md`

**Conventions used throughout:**
- Build command for every task: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.csproj` → expect `Build succeeded` with `0 Error(s)` (warnings acceptable). Building the server project compiles the referenced Client project too.
- **Razor `@` escaping (critical):** In the markup (prose) portion of a `.razor` file, a literal `@` must be written `@@` (e.g. inline `<code>@@bind-Value</code>`). Inside `@code { ... }` raw string literals (`"""..."""`), a single `@` is literal — do **not** double it there. Every `CodeBlock` sample in this plan stores its code in a `@code` raw string literal, so those use single `@`.
- Each page's `<code>` inline snippets use `class="@LiquidGlassClasses.CodeInline"` (a real Razor expression — single `@`, do not escape).
- Commit messages: plain conventional messages, no `Co-Authored-By` footer.

---

## File structure

| File | Responsibility |
|---|---|
| `…Client/Data/DocsNav.cs` (modify) | Flip `IsDocumented: true` on the 5 Handbook links |
| `…Docs/Program.cs` (modify) | Add `/handbook/{slug}.md` route |
| `…Client/Pages/Handbook/StylingPage.razor` (create) | `/handbook/styling` |
| `…Client/Pages/Handbook/CompositionPage.razor` (create) | `/handbook/composition` |
| `…Client/Pages/Handbook/CustomizationPage.razor` (create) | `/handbook/customization` |
| `…Client/Pages/Handbook/AnimationPage.razor` (create) | `/handbook/animation` (reuses Popover Hero demo) |
| `…Client/Pages/Handbook/FormsPage.razor` (create) | `/handbook/forms` (reuses Form Hero demo) |
| `…Docs/Content/Handbook/{styling,composition,customization,animation,forms}.md` (create) | Markdown sources |

Path prefixes (full):
- Client: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/`
- Server: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/`

`HandbookPage.razor` (the `/handbook/{Slug}` stub) is **kept** as the fallback, mirroring how `ComponentPage.razor` is kept though all components are documented.

---

### Task 1: Mark Handbook links as documented

**Files:**
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Data/DocsNav.cs:18-25`

- [ ] **Step 1: Set `IsDocumented: true` on all 5 Handbook links**

Replace the existing Handbook section block:

```csharp
        new("Handbook", "handbook",
        [
            new("Animation", "animation", "/handbook/animation", "A guide to animating Blazix.BaseUI components."),
            new("Composition", "composition", "/handbook/composition", "A guide to composing Blazix.BaseUI components with your own Blazor components."),
            new("Customization", "customization", "/handbook/customization", "A guide to customizing component behavior."),
            new("Forms", "forms", "/handbook/forms", "A guide to using Blazix.BaseUI components in forms."),
            new("Styling", "styling", "/handbook/styling", "A guide to styling Blazix.BaseUI components with any styling solution."),
        ]),
```

with:

```csharp
        new("Handbook", "handbook",
        [
            new("Animation", "animation", "/handbook/animation", "A guide to animating Blazix.BaseUI components.", IsDocumented: true),
            new("Composition", "composition", "/handbook/composition", "A guide to composing Blazix.BaseUI components with your own Blazor components.", IsDocumented: true),
            new("Customization", "customization", "/handbook/customization", "A guide to customizing component behavior.", IsDocumented: true),
            new("Forms", "forms", "/handbook/forms", "A guide to using Blazix.BaseUI components in forms.", IsDocumented: true),
            new("Styling", "styling", "/handbook/styling", "A guide to styling Blazix.BaseUI components with any styling solution.", IsDocumented: true),
        ]),
```

- [ ] **Step 2: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Data/DocsNav.cs
git commit -m "docs: mark handbook links as documented"
```

---

### Task 2: Serve Handbook markdown sources

**Files:**
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Program.cs:47` (insert after the components `.md` route, before `app.MapRazorComponents<App>()`)

- [ ] **Step 1: Add the `/handbook/{slug}.md` route**

Insert this block immediately after the closing `});` of the existing `/components/{slug}.md` route (currently line 47) and before `app.MapRazorComponents<App>()`:

```csharp
app.MapGet("/handbook/{slug}.md", async (string slug, IWebHostEnvironment environment, CancellationToken cancellationToken) =>
{
    if (slug.Any(character => !(char.IsAsciiLetterOrDigit(character) || character == '-')))
    {
        return Results.BadRequest("Invalid handbook slug.");
    }

    var markdownPath = Path.Combine(environment.ContentRootPath, "Content", "Handbook", $"{slug}.md");
    if (!File.Exists(markdownPath))
    {
        return Results.NotFound();
    }

    var markdown = await File.ReadAllTextAsync(markdownPath, cancellationToken);
    return Results.Text(markdown, "text/markdown; charset=utf-8");
});
```

- [ ] **Step 2: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Program.cs
git commit -m "docs: serve handbook markdown sources"
```

---

### Task 3: Styling page

**Files:**
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/Handbook/StylingPage.razor`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Handbook/styling.md`

- [ ] **Step 1: Create the page**

`Pages/Handbook/StylingPage.razor`:

```razor
@page "/handbook/styling"

<PageTitle>Styling · Blazix.BaseUI</PageTitle>

<DocsPageIntro Eyebrow="Handbook"
               Title="Styling"
               Subtitle="A guide to styling Blazix.BaseUI components with any styling solution."
               MarkdownHref="/handbook/styling.md" />

<p class="mb-3 mt-6 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Blazix.BaseUI components are unstyled and ship no CSS. Each part renders a plain HTML element that you
    style yourself — with Tailwind, plain CSS, or any other approach. You keep complete control of the styling layer.
</p>

<DocsHeading Title="Style hooks" />

<DocsHeading Level="3" Title="CSS classes" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Parts that render an HTML element accept the standard <code class="@LiquidGlassClasses.CodeInline">class</code> attribute.
</p>
<CodeBlock Code="@ClassCode" />
<p class="mb-3 mt-4 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    For classes that depend on the part's state, pass a function to
    <code class="@LiquidGlassClasses.CodeInline">ClassValue</code>. It receives the part's state record and returns the class string.
</p>
<CodeBlock Code="@ClassValueCode" />

<DocsHeading Level="3" Title="Data attributes" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Components expose data attributes for styling their states. For example, Switch can be styled through its
    <code class="@LiquidGlassClasses.CodeInline">[data-checked]</code> and
    <code class="@LiquidGlassClasses.CodeInline">[data-unchecked]</code> attributes, among others.
</p>
<CodeBlock Code="@DataAttributeCode" Language="css" />

<DocsHeading Level="3" Title="CSS variables" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Components expose CSS variables to aid styling, often holding dynamic values used in sizing or transform
    calculations. For example, Popover exposes variables such as
    <code class="@LiquidGlassClasses.CodeInline">--available-height</code> and
    <code class="@LiquidGlassClasses.CodeInline">--anchor-width</code> on its parts.
</p>
<CodeBlock Code="@CssVariableCode" Language="css" />
<p class="mb-3 mt-4 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Check each component's API reference for its complete list of data attributes and CSS variables.
</p>

<DocsHeading Level="3" Title="Style attribute" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Parts also accept the standard <code class="@LiquidGlassClasses.CodeInline">style</code> attribute, and a
    <code class="@LiquidGlassClasses.CodeInline">StyleValue</code> function for state-driven inline styles.
</p>
<CodeBlock Code="@StyleValueCode" />

<DocsHeading Title="Tailwind CSS" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Apply Tailwind utility classes through <code class="@LiquidGlassClasses.CodeInline">class</code> (or
    <code class="@LiquidGlassClasses.CodeInline">ClassValue</code> for state variants). Target component states
    with arbitrary variants like <code class="@LiquidGlassClasses.CodeInline">data-[checked]:</code>.
</p>
<CodeBlock Code="@TailwindCode" />

<DocsHeading Title="Plain CSS" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Apply your own class names through <code class="@LiquidGlassClasses.CodeInline">class</code>, then style
    them in any stylesheet.
</p>
<CodeBlock Code="@PlainCssRazorCode" />
<CodeBlock Code="@PlainCssStylesheetCode" Language="css" />

@code {
    private const string ClassCode =
        """
        <SwitchThumb class="switch-thumb" />
        """;

    private const string ClassValueCode =
        """
        <Toggle ClassValue="@(state => state.Pressed ? "toggle pressed" : "toggle")">
            Bold
        </Toggle>
        """;

    private const string DataAttributeCode =
        """
        .switch-thumb {
            background-color: #d4d4d4;
        }

        .switch-thumb[data-checked] {
            background-color: #16a34a;
        }
        """;

    private const string CssVariableCode =
        """
        .popover-popup {
            max-height: var(--available-height);
            max-width: var(--available-width);
            transform-origin: var(--transform-origin);
        }
        """;

    private const string StyleValueCode =
        """
        <SwitchThumb style="height: 1.25rem;" />

        <Toggle StyleValue="@(state => state.Pressed ? "color: #2563eb;" : "color: inherit;")">
            Bold
        </Toggle>
        """;

    private const string TailwindCode =
        """
        <SwitchRoot @bind-Checked="enabled"
                    class="relative h-6 w-10 rounded-full bg-neutral-300 data-[checked]:bg-green-600">
            <SwitchThumb class="size-5 rounded-full bg-white transition-transform data-[checked]:translate-x-4" />
        </SwitchRoot>
        """;

    private const string PlainCssRazorCode =
        """
        <SwitchRoot @bind-Checked="enabled" class="switch">
            <SwitchThumb class="switch-thumb" />
        </SwitchRoot>
        """;

    private const string PlainCssStylesheetCode =
        """
        .switch {
            width: 2.5rem;
            height: 1.5rem;
            border-radius: 9999px;
            background-color: #d4d4d4;
        }

        .switch[data-checked] {
            background-color: #16a34a;
        }
        """;
}
```

- [ ] **Step 2: Create the markdown source**

`Content/Handbook/styling.md`:

```markdown
# Styling

A guide to styling Blazix.BaseUI components with any styling solution.

Rendered docs: `/handbook/styling`

Blazix.BaseUI components are unstyled and ship no CSS. Each part renders a plain HTML element you style yourself.

## Style hooks

- **CSS classes** — the standard `class` attribute, or a `ClassValue` function that receives the part's state record.
- **Data attributes** — state hooks such as Switch's `[data-checked]` / `[data-unchecked]`.
- **CSS variables** — dynamic values such as Popover's `--available-height` and `--anchor-width`.
- **Style attribute** — the standard `style` attribute, or a `StyleValue` function for state-driven inline styles.

Check each component's API reference for its full list of data attributes and CSS variables.

## Tailwind CSS

Apply utility classes through `class` / `ClassValue`; target states with arbitrary variants like `data-[checked]:`.

## Plain CSS

Apply your own class names through `class`, then style them in any stylesheet.
```

- [ ] **Step 3: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Preview check**

Start the preview (server project `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs`). Navigate to `/handbook/styling`. Verify: the page renders inside the content column (not the stub), all sections appear, code blocks are syntax-highlighted, the "Open .md" button links to `/handbook/styling.md`, and `/handbook/styling.md` returns the markdown. Confirm the sidebar marks "Styling" as the active/documented link.

- [ ] **Step 5: Commit**

```bash
git add docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/Handbook/StylingPage.razor docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Handbook/styling.md
git commit -m "docs: add handbook Styling page"
```

---

### Task 4: Composition page

**Files:**
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/Handbook/CompositionPage.razor`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Handbook/composition.md`

Background (verified): the composition primitive is the `Render` parameter (`RenderFragment<RenderProps<TState>>`) present on every part. `RenderProps<TState>` exposes `Attributes` (splat with `@attributes="context.Attributes"`), `State`, `ChildContent`, and `ElementReferenceCallback`. It is the Blazor equivalent of base-ui's `render={(props, state) => ...}` prop.

- [ ] **Step 1: Create the page**

`Pages/Handbook/CompositionPage.razor`:

```razor
@page "/handbook/composition"

<PageTitle>Composition · Blazix.BaseUI</PageTitle>

<DocsPageIntro Eyebrow="Handbook"
               Title="Composition"
               Subtitle="A guide to composing Blazix.BaseUI components with your own Blazor components."
               MarkdownHref="/handbook/composition.md" />

<p class="mb-3 mt-6 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Every part accepts a <code class="@LiquidGlassClasses.CodeInline">Render</code> parameter — a
    <code class="@LiquidGlassClasses.CodeInline">RenderFragment&lt;RenderProps&lt;TState&gt;&gt;</code> that lets you
    replace the element the part renders, or compose it with your own component. It is the Blazor equivalent of
    a render prop.
</p>
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    The function receives a <code class="@LiquidGlassClasses.CodeInline">context</code> of type
    <code class="@LiquidGlassClasses.CodeInline">RenderProps&lt;TState&gt;</code>, which carries everything the part
    would otherwise render:
</p>
<ul class="mb-3 ml-5 max-w-3xl list-disc text-sm leading-6 text-slate-700 dark:text-slate-200">
    <li><code class="@LiquidGlassClasses.CodeInline">context.Attributes</code> — every computed attribute (aria-*, data-*, role, class, style). Splat it with <code class="@LiquidGlassClasses.CodeInline">@@attributes="context.Attributes"</code>.</li>
    <li><code class="@LiquidGlassClasses.CodeInline">context.State</code> — the part's current public state.</li>
    <li><code class="@LiquidGlassClasses.CodeInline">context.ChildContent</code> — the original child content, so you decide where it goes.</li>
    <li><code class="@LiquidGlassClasses.CodeInline">context.ElementReferenceCallback</code> — capture for the part's internal interop; forward it with <code class="@LiquidGlassClasses.CodeInline">@@ref</code>-style capture when rendering a raw element via a builder.</li>
</ul>

<DocsHeading Title="Composing your own component" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Most triggers render a <code class="@LiquidGlassClasses.CodeInline">button</code> by default. Use
    <code class="@LiquidGlassClasses.CodeInline">Render</code> to render your own component instead. Your component
    must splat <code class="@LiquidGlassClasses.CodeInline">context.Attributes</code> onto its underlying element so
    the part's behavior keeps working.
</p>
<CodeBlock Code="@ComposeCustomCode" />

<DocsHeading Title="Changing the rendered element" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    <code class="@LiquidGlassClasses.CodeInline">MenuItem</code> renders a
    <code class="@LiquidGlassClasses.CodeInline">div</code> by default. Render it as an
    <code class="@LiquidGlassClasses.CodeInline">a</code> so it works like a link. Place
    <code class="@LiquidGlassClasses.CodeInline">context.ChildContent</code> wherever the content should appear.
</p>
<CodeBlock Code="@ChangeElementCode" />
<p class="mb-3 mt-4 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Each part renders the most appropriate element by default; rendering a different element is recommended only on
    a case-by-case basis.
</p>

<DocsHeading Title="Rendering from state" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Because <code class="@LiquidGlassClasses.CodeInline">context.State</code> is available, the
    <code class="@LiquidGlassClasses.CodeInline">Render</code> function can vary its content based on the part's state.
</p>
<CodeBlock Code="@RenderFromStateCode" />

@code {
    private const string ComposeCustomCode =
        """
        <MenuTrigger Render="@(context =>
            @<MyButton Size="md" Attributes="context.Attributes">
                Open menu
            </MyButton>)" />
        """;

    private const string ChangeElementCode =
        """
        <MenuItem Render="@(context =>
            @<a @attributes="context.Attributes" href="https://blazix.dev">
                @context.ChildContent
            </a>)">
            Add to Library
        </MenuItem>
        """;

    private const string RenderFromStateCode =
        """
        <SwitchThumb Render="@(context =>
            @<span @attributes="context.Attributes">
                @if (context.State.Checked)
                {
                    <CheckedIcon />
                }
                else
                {
                    <UncheckedIcon />
                }
            </span>)" />
        """;
}
```

> Note for the implementer: `context.State.Checked` in `RenderFromStateCode` is illustrative content inside a `CodeBlock` string (not compiled), so it needs no type to exist. `MyButton`, `CheckedIcon`, `UncheckedIcon` are likewise illustrative names in code samples only.

- [ ] **Step 2: Create the markdown source**

`Content/Handbook/composition.md`:

```markdown
# Composition

A guide to composing Blazix.BaseUI components with your own Blazor components.

Rendered docs: `/handbook/composition`

Every part accepts a `Render` parameter (`RenderFragment<RenderProps<TState>>`) — the Blazor equivalent of a render prop. The `context` carries:

- `context.Attributes` — every computed attribute; splat with `@attributes="context.Attributes"`.
- `context.State` — the part's current public state.
- `context.ChildContent` — the original child content.
- `context.ElementReferenceCallback` — element capture for the part's internal interop.

## Composing your own component

Use `Render` to render your own component; it must splat `context.Attributes` onto its underlying element.

## Changing the rendered element

Render a part as a different element (for example, `MenuItem` as an `<a>`), placing `context.ChildContent` where the content belongs.

## Rendering from state

`context.State` lets the `Render` function vary its content based on the part's state.
```

- [ ] **Step 3: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Preview check**

Navigate to `/handbook/composition`. Verify the page renders, the four `RenderProps` members list correctly (the `@@attributes`/`@@ref` show as literal `@attributes`/`@ref`), code blocks highlight, and `/handbook/composition.md` returns the markdown.

- [ ] **Step 5: Commit**

```bash
git add docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/Handbook/CompositionPage.razor docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Handbook/composition.md
git commit -m "docs: add handbook Composition page"
```

---

### Task 5: Customization page

**Files:**
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/Handbook/CustomizationPage.razor`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Handbook/customization.md`

Background (verified): change events carry an args object deriving from `OpenChangeEventArgs<TReason>` with `Reason` (a type-safe enum, e.g. `TooltipOpenChangeReason.TriggerPress`, `PopoverOpenChangeReason.EscapeKey`), `Cancel()` / `IsCanceled`, `AllowPropagation()` / `IsPropagationAllowed`, and `PreventUnmountOnClose()`. Roots are uncontrolled by default and controllable via `@bind-Open` (`Open` + `OpenChanged`) or explicit `Open` + `OnOpenChange`.

- [ ] **Step 1: Create the page**

`Pages/Handbook/CustomizationPage.razor`:

```razor
@page "/handbook/customization"

<PageTitle>Customization · Blazix.BaseUI</PageTitle>

<DocsPageIntro Eyebrow="Handbook"
               Title="Customization"
               Subtitle="A guide to customizing the behavior of Blazix.BaseUI components."
               MarkdownHref="/handbook/customization.md" />

<DocsHeading Title="Change events" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Change events such as <code class="@LiquidGlassClasses.CodeInline">OnOpenChange</code>,
    <code class="@LiquidGlassClasses.CodeInline">OnValueChange</code>, and
    <code class="@LiquidGlassClasses.CodeInline">OnCheckedChange</code> pass a strongly-typed event-args object.
    They can be raised by a variety of DOM events, effects, or rendering.
</p>
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    The args object lets you customize the change and conditionally run side effects based on what caused it:
</p>
<ul class="mb-3 ml-5 max-w-3xl list-disc text-sm leading-6 text-slate-700 dark:text-slate-200">
    <li><code class="@LiquidGlassClasses.CodeInline">Reason</code> — a type-safe enum describing why the change occurred.</li>
    <li><code class="@LiquidGlassClasses.CodeInline">Cancel()</code> — stops the component from changing its internal state (<code class="@LiquidGlassClasses.CodeInline">IsCanceled</code> reflects this).</li>
    <li><code class="@LiquidGlassClasses.CodeInline">AllowPropagation()</code> — lets the DOM event propagate in cases where Blazix stops it (<code class="@LiquidGlassClasses.CodeInline">IsPropagationAllowed</code> reflects this).</li>
    <li><code class="@LiquidGlassClasses.CodeInline">PreventUnmountOnClose()</code> — keeps the popup mounted after its close transition completes.</li>
</ul>

<DocsHeading Level="3" Title="Canceling a change" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Call <code class="@LiquidGlassClasses.CodeInline">Cancel()</code> to block a specific transition while leaving
    the component uncontrolled. The <code class="@LiquidGlassClasses.CodeInline">Reason</code> enum (its values appear
    in IntelliSense after <code class="@LiquidGlassClasses.CodeInline">args.Reason ==</code>) lets you target one cause.
</p>
<CodeBlock Code="@CancelCode" Language="csharp" />

<DocsHeading Level="3" Title="Allowing propagation" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    In most components, pressing <kbd>Esc</kbd> stops event propagation so parent popups don't close at the same
    time. Call <code class="@LiquidGlassClasses.CodeInline">AllowPropagation()</code> to opt out.
</p>
<CodeBlock Code="@AllowPropagationCode" Language="csharp" />

<DocsHeading Title="Controlling components with state" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Components are uncontrolled by default — they manage their own state. Set
    <code class="@LiquidGlassClasses.CodeInline">DefaultOpen</code> (or
    <code class="@LiquidGlassClasses.CodeInline">DefaultValue</code>) for an uncontrolled initial state.
</p>
<CodeBlock Code="@UncontrolledCode" />
<p class="mb-3 mt-4 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Make a component controlled by binding your own state with
    <code class="@LiquidGlassClasses.CodeInline">@@bind-Open</code> (or
    <code class="@LiquidGlassClasses.CodeInline">@@bind-Value</code>). This also lets you read and drive the state
    from outside the root — for example, opening a dialog from a timer with no trigger.
</p>
<CodeBlock Code="@ControlledCode" />

@code {
    private const string CancelCode =
        """
        <TooltipRoot OnOpenChange="HandleOpenChange">
            ...
        </TooltipRoot>

        @code {
            private void HandleOpenChange(TooltipOpenChangeEventArgs args)
            {
                // Keep the tooltip open when the trigger is pressed.
                if (args.Reason == TooltipOpenChangeReason.TriggerPress)
                {
                    args.Cancel();
                }
            }
        }
        """;

    private const string AllowPropagationCode =
        """
        <PopoverRoot OnOpenChange="HandleOpenChange">
            ...
        </PopoverRoot>

        @code {
            private void HandleOpenChange(PopoverOpenChangeEventArgs args)
            {
                // Let the Escape key bubble so a parent popup can also close.
                if (args.Reason == PopoverOpenChangeReason.EscapeKey)
                {
                    args.AllowPropagation();
                }
            }
        }
        """;

    private const string UncontrolledCode =
        """
        <DialogRoot DefaultOpen="false">
            <DialogTrigger>Open</DialogTrigger>
            ...
        </DialogRoot>
        """;

    private const string ControlledCode =
        """
        <DialogRoot @bind-Open="open">
            No trigger is needed in this case.
        </DialogRoot>

        @code {
            private bool open;

            protected override void OnInitialized()
            {
                _ = OpenAfterDelayAsync();
            }

            private async Task OpenAfterDelayAsync()
            {
                await Task.Delay(1000);
                open = true;
                await InvokeAsync(StateHasChanged);
            }
        }
        """;
}
```

- [ ] **Step 2: Create the markdown source**

`Content/Handbook/customization.md`:

```markdown
# Customization

A guide to customizing the behavior of Blazix.BaseUI components.

Rendered docs: `/handbook/customization`

## Change events

Change events (`OnOpenChange`, `OnValueChange`, `OnCheckedChange`, …) pass a strongly-typed args object:

- `Reason` — a type-safe enum describing the cause of the change.
- `Cancel()` / `IsCanceled` — block the component's internal state change.
- `AllowPropagation()` / `IsPropagationAllowed` — let the DOM event propagate when Blazix stops it.
- `PreventUnmountOnClose()` — keep the popup mounted after its close transition.

### Canceling a change

Call `args.Cancel()` to block a specific transition (gated on `args.Reason`) while leaving the component uncontrolled.

### Allowing propagation

Pressing Escape stops propagation by default so parent popups don't close together; call `args.AllowPropagation()` to opt out.

## Controlling components with state

Components are uncontrolled by default (`DefaultOpen` / `DefaultValue` sets an uncontrolled initial state). Bind your own state with `@bind-Open` / `@bind-Value` to control them and drive state from outside the root.
```

- [ ] **Step 3: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Preview check**

Navigate to `/handbook/customization`. Verify the page renders, the inline `@bind-Open` / `@bind-Value` show literally (from `@@bind-…`), code blocks highlight, and `/handbook/customization.md` returns the markdown.

- [ ] **Step 5: Commit**

```bash
git add docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/Handbook/CustomizationPage.razor docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Handbook/customization.md
git commit -m "docs: add handbook Customization page"
```

---

### Task 6: Animation page (reuses Popover Hero demo)

**Files:**
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/Handbook/AnimationPage.razor`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Handbook/animation.md`

Background (verified): the embedded Popover Hero demo already animates via `[data-starting-style]` / `[data-ending-style]` transitions defined in `wwwroot/demos/popover.css`. `PopoverHeroCss` resolves via `_Imports.razor:100`. Demo wiring mirrors `AccordionPage.razor`. JS-driven animation is intentionally only a short note (decision: omit Motion/Framer port).

- [ ] **Step 1: Create the page**

`Pages/Handbook/AnimationPage.razor`:

```razor
@page "/handbook/animation"

<PageTitle>Animation · Blazix.BaseUI</PageTitle>

<DocsPageIntro Eyebrow="Handbook"
               Title="Animation"
               Subtitle="A guide to animating Blazix.BaseUI components."
               MarkdownHref="/handbook/animation.md" />

<p class="mb-3 mt-6 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Components can be animated with CSS transitions or CSS animations. Each component provides data attributes to
    target its states, plus a few attributes specifically for entry and exit animation.
</p>

<div class="mt-6">
    <Demo Variants="@HeroVariants">
        <PopoverHeroCss />
    </Demo>
</div>

<DocsHeading Title="CSS transitions" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Use these attributes to transition a component as it becomes visible or hidden:
</p>
<ul class="mb-3 ml-5 max-w-3xl list-disc text-sm leading-6 text-slate-700 dark:text-slate-200">
    <li><code class="@LiquidGlassClasses.CodeInline">[data-starting-style]</code> — the initial style to transition from.</li>
    <li><code class="@LiquidGlassClasses.CodeInline">[data-ending-style]</code> — the final style to transition to.</li>
</ul>
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Transitions are recommended over CSS animations because a transition can be cancelled smoothly midway — if the
    user closes a popup before it finishes opening, it animates back to closed without an abrupt jump.
</p>
<CodeBlock Code="@TransitionCode" Language="css" />

<DocsHeading Title="CSS animations" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Use these attributes to drive keyframe animations as a component becomes visible or hidden:
</p>
<ul class="mb-3 ml-5 max-w-3xl list-disc text-sm leading-6 text-slate-700 dark:text-slate-200">
    <li><code class="@LiquidGlassClasses.CodeInline">[data-open]</code> — applied while the component is visible.</li>
    <li><code class="@LiquidGlassClasses.CodeInline">[data-closed]</code> — applied before the component is hidden.</li>
</ul>
<CodeBlock Code="@AnimationCode" Language="css" />

<DocsHeading Title="Keeping components mounted" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Popup components are removed from the DOM when closed. Set
    <code class="@LiquidGlassClasses.CodeInline">KeepMounted</code> on the
    <code class="@LiquidGlassClasses.CodeInline">Portal</code> part to keep them rendered while closed.
</p>
<CodeBlock Code="@KeepMountedCode" />

<DocsHeading Title="JavaScript-driven animation" />
<p class="mb-0 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    For animation that must be driven by JavaScript, do it from a JS module rather than from Blazor — high-frequency
    and lifecycle-bound animation belongs in interop. The CSS transition and animation hooks above cover the common
    enter/exit cases without any JavaScript.
</p>

@code {
    private static readonly IReadOnlyList<DemoVariant> HeroVariants =
    [
        new("CSS",
        [
            new DemoFile("Popover.razor", "xml", "Components.Demos.Popover.Hero.Css.PopoverHeroCss.razor"),
            new DemoFile("popover.css", "css", "wwwroot.demos.popover.css"),
        ]),
        new("Tailwind",
        [
            new DemoFile("Popover.razor", "xml", "Components.Demos.Popover.Hero.Tailwind.PopoverHeroTailwind.razor"),
        ]),
    ];

    private const string TransitionCode =
        """
        .popover-popup {
            transform-origin: var(--transform-origin);
            transition: transform 150ms, opacity 150ms;
        }

        .popover-popup[data-starting-style],
        .popover-popup[data-ending-style] {
            opacity: 0;
            transform: scale(0.9);
        }
        """;

    private const string AnimationCode =
        """
        @keyframes scale-in {
            from { opacity: 0; transform: scale(0.9); }
            to { opacity: 1; transform: scale(1); }
        }

        @keyframes scale-out {
            from { opacity: 1; transform: scale(1); }
            to { opacity: 0; transform: scale(0.9); }
        }

        .popover-popup[data-open] {
            animation: scale-in 250ms ease-out;
        }

        .popover-popup[data-closed] {
            animation: scale-out 250ms ease-in;
        }
        """;

    private const string KeepMountedCode =
        """
        <PopoverPortal KeepMounted="true">
            <PopoverPositioner>
                <PopoverPopup class="popover-popup">...</PopoverPopup>
            </PopoverPositioner>
        </PopoverPortal>
        """;
}
```

- [ ] **Step 2: Create the markdown source**

`Content/Handbook/animation.md`:

```markdown
# Animation

A guide to animating Blazix.BaseUI components.

Rendered docs: `/handbook/animation`

Components animate with CSS transitions or CSS animations, using data attributes to target their states.

## CSS transitions

- `[data-starting-style]` — the initial style to transition from.
- `[data-ending-style]` — the final style to transition to.

Transitions are recommended over animations because they can be cancelled smoothly midway.

## CSS animations

- `[data-open]` — applied while the component is visible.
- `[data-closed]` — applied before it is hidden.

## Keeping components mounted

Set `KeepMounted` on the `Portal` part to keep a popup rendered while closed.

## JavaScript-driven animation

Drive JavaScript-based animation from a JS module rather than Blazor; the CSS hooks above cover common enter/exit cases without any JavaScript.
```

- [ ] **Step 3: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Preview check**

Navigate to `/handbook/animation`. Verify: the Popover demo renders and the popover opens/closes with a transition; the CSS / Tailwind variant selector shows both sources; the `popover.css` source tab is present; sections and code blocks render; `/handbook/animation.md` returns the markdown.

- [ ] **Step 5: Commit**

```bash
git add docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/Handbook/AnimationPage.razor docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Handbook/animation.md
git commit -m "docs: add handbook Animation page"
```

---

### Task 7: Forms page (reuses Form Hero demo)

**Files:**
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/Handbook/FormsPage.razor`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Handbook/forms.md`

Background (verified): `Form` builds an `EditContext` from `Model` and cascades it (`Form.razor:4`), validates with `editContext.Validate()` on submit, and exposes `OnSubmit`/`OnValidSubmit`/`OnInvalidSubmit` (`EventCallback<EditContext>`), `OnFormSubmit` (`FormSubmitEventArgs.Values`), `Errors` (`Dictionary<string,string[]>` keyed by field name), and `ValidationMode` (`OnSubmit`/`OnBlur`/`OnChange`). A standard `<DataAnnotationsValidator />` inside `<Form>` works because the EditContext is cascaded. Field parts: `FieldRoot` (`Name`), `FieldLabel`, `FieldControl<TValue>` (`@bind-Value`, `Name`), `FieldDescription`, `FieldError`. Field state attributes: `data-valid`, `data-invalid`, `data-touched`, `data-dirty`, `data-filled`, `data-focused`, `data-disabled`. Group with `FieldsetRoot` + `FieldsetLegend`. `FormHeroCss` resolves via `_Imports.razor:74`.

- [ ] **Step 1: Create the page**

`Pages/Handbook/FormsPage.razor`:

```razor
@page "/handbook/forms"

<PageTitle>Forms · Blazix.BaseUI</PageTitle>

<DocsPageIntro Eyebrow="Handbook"
               Title="Forms"
               Subtitle="A guide to using Blazix.BaseUI components in forms."
               MarkdownHref="/handbook/forms.md" />

<div class="mt-6">
    <Demo Variants="@HeroVariants">
        <FormHeroCss />
    </Demo>
</div>

<DocsHeading Title="Building form fields" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Compose a field from <code class="@LiquidGlassClasses.CodeInline">FieldRoot</code> and its parts. The root wires
    the label to the control, joins helper text into <code class="@LiquidGlassClasses.CodeInline">aria-describedby</code>,
    and exposes state attributes (<code class="@LiquidGlassClasses.CodeInline">data-valid</code>,
    <code class="@LiquidGlassClasses.CodeInline">data-invalid</code>,
    <code class="@LiquidGlassClasses.CodeInline">data-touched</code>,
    <code class="@LiquidGlassClasses.CodeInline">data-dirty</code>,
    <code class="@LiquidGlassClasses.CodeInline">data-filled</code>,
    <code class="@LiquidGlassClasses.CodeInline">data-focused</code>) for styling.
</p>
<CodeBlock Code="@BuildFieldCode" />

<DocsHeading Title="Naming form controls" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Give each field a <code class="@LiquidGlassClasses.CodeInline">Name</code> so its value and any external errors are
    keyed consistently. <code class="@LiquidGlassClasses.CodeInline">FieldRoot.Name</code> takes precedence over
    <code class="@LiquidGlassClasses.CodeInline">FieldControl.Name</code>.
</p>

<DocsHeading Title="Labeling control groups" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Group related controls with <code class="@LiquidGlassClasses.CodeInline">FieldsetRoot</code> and a single
    <code class="@LiquidGlassClasses.CodeInline">FieldsetLegend</code>; set
    <code class="@LiquidGlassClasses.CodeInline">Disabled</code> on the root to disable the whole group.
</p>
<CodeBlock Code="@FieldsetCode" />

<DocsHeading Title="Submitting data" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Read values straight off your bound <code class="@LiquidGlassClasses.CodeInline">Model</code>, or use
    <code class="@LiquidGlassClasses.CodeInline">OnFormSubmit</code> to receive a
    <code class="@LiquidGlassClasses.CodeInline">FormSubmitEventArgs</code> whose
    <code class="@LiquidGlassClasses.CodeInline">Values</code> are keyed by field name.
</p>
<CodeBlock Code="@SubmitCode" />

<DocsHeading Title="Validation" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    <code class="@LiquidGlassClasses.CodeInline">Form</code> validates on submit by default; choose when with
    <code class="@LiquidGlassClasses.CodeInline">ValidationMode</code>
    (<code class="@LiquidGlassClasses.CodeInline">OnSubmit</code>,
    <code class="@LiquidGlassClasses.CodeInline">OnBlur</code>,
    <code class="@LiquidGlassClasses.CodeInline">OnChange</code>).
    <code class="@LiquidGlassClasses.CodeInline">FieldError</code> renders the active messages.
</p>

<DocsHeading Level="3" Title="Native constraint validation" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Put native HTML validation attributes on the control; Blazix surfaces the browser's messages through
    <code class="@LiquidGlassClasses.CodeInline">FieldError</code>.
</p>
<CodeBlock Code="@NativeValidationCode" />

<DocsHeading Level="3" Title="Server-side errors" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Pass <code class="@LiquidGlassClasses.CodeInline">Errors</code> — a dictionary keyed by field name — to display
    messages returned by your server.
</p>
<CodeBlock Code="@ServerErrorsCode" />

<DocsHeading Title="Blazor integration" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    <code class="@LiquidGlassClasses.CodeInline">Form</code> builds an
    <code class="@LiquidGlassClasses.CodeInline">EditContext</code> from
    <code class="@LiquidGlassClasses.CodeInline">Model</code> and cascades it, so the standard Blazor validation
    components work inside it. Add a <code class="@LiquidGlassClasses.CodeInline">DataAnnotationsValidator</code> and
    annotate your model to validate with data annotations; branch on
    <code class="@LiquidGlassClasses.CodeInline">OnValidSubmit</code> /
    <code class="@LiquidGlassClasses.CodeInline">OnInvalidSubmit</code>.
</p>
<CodeBlock Code="@DataAnnotationsCode" />

@code {
    private static readonly IReadOnlyList<DemoVariant> HeroVariants =
    [
        new("CSS",
        [
            new DemoFile("Form.razor", "xml", "Components.Demos.Form.Hero.Css.FormHeroCss.razor"),
            new DemoFile("form.css", "css", "wwwroot.demos.form.css"),
        ]),
        new("Tailwind",
        [
            new DemoFile("Form.razor", "xml", "Components.Demos.Form.Hero.Tailwind.FormHeroTailwind.razor"),
        ]),
    ];

    private const string BuildFieldCode =
        """
        @using Blazix.BaseUI.Field

        <FieldRoot Name="email">
            <FieldLabel>Email</FieldLabel>
            <FieldControl TValue="string" @bind-Value="model.Email" type="email" />
            <FieldDescription>We will never share it.</FieldDescription>
            <FieldError />
        </FieldRoot>
        """;

    private const string FieldsetCode =
        """
        @using Blazix.BaseUI.Fieldset

        <FieldsetRoot>
            <FieldsetLegend>Billing details</FieldsetLegend>
            <FieldRoot Name="card">
                <FieldLabel>Card number</FieldLabel>
                <FieldControl TValue="string" @bind-Value="model.Card" />
            </FieldRoot>
        </FieldsetRoot>
        """;

    private const string SubmitCode =
        """
        <Form Model="@model" OnFormSubmit="HandleSubmitAsync">
            <FieldRoot Name="name">
                <FieldLabel>Name</FieldLabel>
                <FieldControl TValue="string" @bind-Value="model.Name" />
            </FieldRoot>
            <button type="submit">Submit</button>
        </Form>

        @code {
            private readonly NameModel model = new();

            private Task HandleSubmitAsync(FormSubmitEventArgs args)
            {
                // args.Values["name"] holds the submitted value.
                return Task.CompletedTask;
            }

            private sealed class NameModel
            {
                public string Name { get; set; } = "";
            }
        }
        """;

    private const string NativeValidationCode =
        """
        <FieldRoot Name="url">
            <FieldLabel>Homepage</FieldLabel>
            <FieldControl TValue="string"
                          @bind-Value="model.Url"
                          type="url"
                          required="true"
                          pattern="https?://.*" />
            <FieldError />
        </FieldRoot>
        """;

    private const string ServerErrorsCode =
        """
        <Form Model="@model" Errors="@errors">
            <FieldRoot Name="email">
                <FieldLabel>Email</FieldLabel>
                <FieldControl TValue="string" @bind-Value="model.Email" />
                <FieldError />
            </FieldRoot>
        </Form>

        @code {
            private Dictionary<string, string[]> errors = new()
            {
                ["email"] = ["That email is already registered."]
            };
        }
        """;

    private const string DataAnnotationsCode =
        """
        @using System.ComponentModel.DataAnnotations
        @using Microsoft.AspNetCore.Components.Forms

        <Form Model="@model" OnValidSubmit="HandleValidSubmit">
            <DataAnnotationsValidator />
            <FieldRoot Name="email">
                <FieldLabel>Email</FieldLabel>
                <FieldControl TValue="string" @bind-Value="model.Email" />
                <FieldError />
            </FieldRoot>
            <button type="submit">Sign up</button>
        </Form>

        @code {
            private readonly AccountModel model = new();

            private void HandleValidSubmit(EditContext context)
            {
                // The model passed data-annotation validation.
            }

            private sealed class AccountModel
            {
                [Required, EmailAddress]
                public string Email { get; set; } = "";
            }
        }
        """;
}
```

- [ ] **Step 2: Create the markdown source**

`Content/Handbook/forms.md`:

```markdown
# Forms

A guide to using Blazix.BaseUI components in forms.

Rendered docs: `/handbook/forms`

## Building form fields

Compose a field from `FieldRoot` + `FieldLabel` + `FieldControl<TValue>` + `FieldDescription` + `FieldError`. The root wires label/description/`aria-describedby` and exposes state attributes (`data-valid`, `data-invalid`, `data-touched`, `data-dirty`, `data-filled`, `data-focused`).

## Naming form controls

Give each field a `Name`; `FieldRoot.Name` takes precedence over `FieldControl.Name`.

## Labeling control groups

Group controls with `FieldsetRoot` + one `FieldsetLegend`; `Disabled` on the root disables the group.

## Submitting data

Read the bound `Model`, or use `OnFormSubmit` to receive `FormSubmitEventArgs.Values` keyed by field name.

## Validation

`Form` validates on submit by default (`ValidationMode`: `OnSubmit` / `OnBlur` / `OnChange`); `FieldError` renders messages.

- **Native constraint validation** — native HTML attributes (`required`, `pattern`, …) surfaced via `FieldError`.
- **Server-side errors** — pass `Errors` (keyed by field name).

## Blazor integration

`Form` builds and cascades an `EditContext` from `Model`. Add a `DataAnnotationsValidator` and annotate your model to validate with data annotations; branch on `OnValidSubmit` / `OnInvalidSubmit`.
```

- [ ] **Step 3: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Preview check**

Navigate to `/handbook/forms`. Verify: the Form demo renders, submitting an invalid value shows an error and a valid one submits; CSS / Tailwind variants and the `form.css` source tab show; all sections and code blocks render; `/handbook/forms.md` returns the markdown.

- [ ] **Step 5: Commit**

```bash
git add docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/Handbook/FormsPage.razor docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Handbook/forms.md
git commit -m "docs: add handbook Forms page"
```

---

### Task 8: Full verification (both render modes)

**Files:** none (verification only)

- [ ] **Step 1: Build the whole docs app**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 2: Preview in Server mode**

Start the preview. For each of `/handbook/{animation,composition,customization,forms,styling}`:
- The dedicated page renders inside the content column (not the `DocStub`).
- The sidebar "Handbook" section marks the current link as active.
- The "On this page" rail lists the page's headings and scroll-spy highlights as you scroll.
- The "Open .md" button opens `/handbook/<slug>.md` and returns markdown (200, `text/markdown`).
- Animation: the Popover demo opens/animates. Forms: the form validates/submits.

- [ ] **Step 3: Preview in WASM mode**

Switch the runtime to WASM (the header Server/WASM switch sets the `blazix-docs-render-mode=wasm` cookie and reloads). Repeat the Step 2 checks. (Render-mode timing differs between Server and WASM, so both must be confirmed.)

- [ ] **Step 4: Final commit (if any verification fixes were needed)**

```bash
git add -A docs/Blazix.BaseUI.Docs
git commit -m "docs: verify handbook pages in Server and WASM"
```

(If no fixes were needed, skip this commit.)

---

## Self-review

**Spec coverage:**
- Styling (class/ClassValue, style/StyleValue, data attributes, CSS variables, Tailwind, plain CSS) → Task 3 ✓
- Composition (`Render`/`RenderProps`: compose, change element, render-from-state) → Task 4 ✓
- Customization (Reason/Cancel/AllowPropagation/PreventUnmountOnClose, controlled vs uncontrolled) → Task 5 ✓
- Animation (transitions, animations, KeepMounted, live demo, JS note) → Task 6 ✓
- Forms (build fields, naming, fieldset, submitting, native/server validation, DataAnnotations integration, live demo) → Task 7 ✓
- DocsNav `IsDocumented` → Task 1 ✓; markdown route → Task 2 ✓; markdown sources → Tasks 3–7 ✓
- Verification both render modes → Task 8 ✓

**Placeholder scan:** No TBD/TODO. Illustrative identifiers in code samples (`MyButton`, `CheckedIcon`, `NameModel`, etc.) live only inside `CodeBlock` string constants (not compiled) and are called out where used.

**Type/name consistency:** `DemoVariant`/`DemoFile` signatures match `DemoSources.cs`. Resource paths follow `AccordionPage` (`Components.Demos.…razor`, `wwwroot.demos.….css`). Verified API tokens: `ClassValue`/`StyleValue`, `Render`/`RenderProps<TState>` (`Attributes`/`State`/`ChildContent`/`ElementReferenceCallback`), `OpenChangeEventArgs` members (`Reason`/`Cancel`/`AllowPropagation`/`PreventUnmountOnClose`), `@bind-Open`/`DefaultOpen`, `ToggleState.Pressed`, Switch `data-checked`/`data-unchecked`, Popover `data-open`/`data-closed`, Form (`Model`/`Errors`/`OnFormSubmit`/`OnValidSubmit`/`ValidationMode`), Field/Fieldset parts.

**Razor escaping:** Literal `@` in markup written as `@@`; single `@` retained inside `@code` raw-string `CodeBlock` constants. Called out in conventions and in each preview check.
