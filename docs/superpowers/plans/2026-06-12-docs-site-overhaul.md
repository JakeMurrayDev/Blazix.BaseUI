# Docs Site Overhaul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild `docs/Blazix.BaseUI.Docs` to mimic the base-ui.com docs layout (header + sidebar + content + "On this page" rail) with all 35 components in the sidebar, a Demo showcase component with CSS/Tailwind source variants and a copy button, a dark-mode switch, a site-wide Server/WASM runtime switch, and full Accordion documentation — keeping the Liquid Glass design system and building docs UI strictly from Blazix.BaseUI components.

**Architecture:** Code-first Razor pages on the existing Blazor Web App. A static `DocsNav` catalog drives the sidebar and stub pages; documented components get dedicated routable pages (`/components/accordion` literal route beats the `/components/{Slug}` stub route). Demo source files are compiled as components AND embedded as resources so displayed code never drifts. One JS module (`docs-interop.js`) handles clipboard, scroll-spy, syntax highlight, and the render-mode cookie. `App.razor` picks `InteractiveServer`/`InteractiveWebAssembly` per-request from a cookie.

**Tech Stack:** .NET 10 Blazor Web App, Blazix.BaseUI (project reference), Tailwind Play CDN (v3 — alpha values must be multiples of 5, `data-[x]:` arbitrary variants), highlight.js CDN.

**Testing note:** The docs app has no test project (established repo convention — tests cover the library). Verification per task = `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.sln` with 0 errors; UI tasks add preview checks. The spec's §7 checklist is the final acceptance gate.

**Spec:** `docs/superpowers/specs/2026-06-12-docs-site-overhaul-design.md`

**Conventions used throughout:**
- All JS interop follows the project rules: `Lazy<Task<IJSObjectReference>>`, `catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)`, module dispose guarded by `moduleTask.IsValueCreated`.
- Client project root namespace: `Blazix.BaseUI.Docs.Client`.
- Build command for every task: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.sln` → expect `Build succeeded` with `0 Error(s)` (warnings acceptable).
- Commit messages end with `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.

---

### Task 1: DocsNav catalog

**Files:**
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Data/DocsNav.cs`

- [ ] **Step 1: Create the nav catalog**

```csharp
namespace Blazix.BaseUI.Docs.Client.Data;

public sealed record DocsNavLink(string Name, string Slug, string Href, string Summary, bool IsDocumented = false);

public sealed record DocsNavSection(string Title, string Area, IReadOnlyList<DocsNavLink> Links);

public static class DocsNav
{
    public static IReadOnlyList<DocsNavSection> Sections { get; } =
    [
        new("Overview", "overview",
        [
            new("Quick start", "quick-start", "/", "A quick guide to getting started with Blazix.BaseUI.", IsDocumented: true),
            new("About", "about", "/overview/about", "An overview of the project and its goals."),
            new("Accessibility", "accessibility", "/overview/accessibility", "How Blazix.BaseUI approaches accessibility."),
        ]),
        new("Handbook", "handbook",
        [
            new("Animation", "animation", "/handbook/animation", "A guide to animating Blazix.BaseUI components."),
            new("Composition", "composition", "/handbook/composition", "A guide to composing Blazix.BaseUI components with your own Blazor components."),
            new("Customization", "customization", "/handbook/customization", "A guide to customizing component behavior."),
            new("Forms", "forms", "/handbook/forms", "A guide to using Blazix.BaseUI components in forms."),
            new("Styling", "styling", "/handbook/styling", "A guide to styling Blazix.BaseUI components with any styling solution."),
        ]),
        new("Components", "components",
        [
            new("Accordion", "accordion", "/components/accordion", "A set of collapsible panels with headings.", IsDocumented: true),
            new("Alert Dialog", "alert-dialog", "/components/alert-dialog", "A dialog that requires a response from the user."),
            new("Autocomplete", "autocomplete", "/components/autocomplete", "An input that suggests options as you type."),
            new("Avatar", "avatar", "/components/avatar", "An image with a textual fallback."),
            new("Button", "button", "/components/button", "An accessible button with full styling freedom."),
            new("Checkbox", "checkbox", "/components/checkbox", "A control for toggling between checked states."),
            new("Checkbox Group", "checkbox-group", "/components/checkbox-group", "Manages shared state for a series of checkboxes."),
            new("Collapsible", "collapsible", "/components/collapsible", "A collapsible panel controlled by a button."),
            new("Context Menu", "context-menu", "/components/context-menu", "A menu opened by right-clicking an area."),
            new("Dialog", "dialog", "/components/dialog", "A popup that opens on top of the page."),
            new("Drawer", "drawer", "/components/drawer", "A dialog that slides in from the edge of the screen."),
            new("Field", "field", "/components/field", "Labelling and validation for form controls."),
            new("Fieldset", "fieldset", "/components/fieldset", "A grouped set of fields with a legend."),
            new("Form", "form", "/components/form", "A native form with consolidated error handling."),
            new("Input", "input", "/components/input", "A native input with managed state."),
            new("Menu", "menu", "/components/menu", "A list of actions in a dropdown."),
            new("Menubar", "menubar", "/components/menubar", "A horizontal collection of menus."),
            new("Meter", "meter", "/components/meter", "A graphical display of a numeric value."),
            new("Navigation Menu", "navigation-menu", "/components/navigation-menu", "A collection of links and menus for site navigation."),
            new("Number Field", "number-field", "/components/number-field", "A numeric input with increment and decrement buttons."),
            new("Popover", "popover", "/components/popover", "An accessible popup anchored to a trigger."),
            new("Preview Card", "preview-card", "/components/preview-card", "A popup that appears when hovering a link."),
            new("Progress", "progress", "/components/progress", "Displays the status of a task over time."),
            new("Radio", "radio", "/components/radio", "A control for selecting one option from a set."),
            new("Scroll Area", "scroll-area", "/components/scroll-area", "A scrollable container with custom scrollbars."),
            new("Select", "select", "/components/select", "A form component for choosing a value from a list of options."),
            new("Separator", "separator", "/components/separator", "A visual divider between sections."),
            new("Slider", "slider", "/components/slider", "A control for selecting a value from a range."),
            new("Switch", "switch", "/components/switch", "A control for toggling between on and off."),
            new("Tabs", "tabs", "/components/tabs", "Organizes content into panels with tabbed navigation."),
            new("Toast", "toast", "/components/toast", "Briefly displays notifications."),
            new("Toggle", "toggle", "/components/toggle", "A two-state button that can be on or off."),
            new("Toggle Group", "toggle-group", "/components/toggle-group", "Manages shared state for a series of toggle buttons."),
            new("Toolbar", "toolbar", "/components/toolbar", "A container for grouping buttons and controls."),
            new("Tooltip", "tooltip", "/components/tooltip", "A popup that labels or describes an element on hover or focus."),
        ]),
        new("Utils", "utils",
        [
            new("CSP Provider", "csp-provider", "/utils/csp-provider", "Support for strict Content Security Policies."),
            new("Direction Provider", "direction-provider", "/utils/direction-provider", "Enables right-to-left behavior for components."),
            new("Portal", "portal", "/utils/portal", "Renders content in a different part of the DOM."),
            new("Render Element", "render-element", "/utils/render-element", "The element-rendering primitive behind every component part."),
        ]),
    ];

    public static DocsNavLink? Find(string area, string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var section = Sections.FirstOrDefault(s => s.Area == area);
        return section?.Links.FirstOrDefault(l => string.Equals(l.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.sln`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Data/DocsNav.cs
git commit -m "docs: add DocsNav catalog with full component list"
```

---

### Task 2: docs-interop.js module + highlight.js wiring

**Files:**
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/wwwroot/docs-interop.js`
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Components/App.razor` (head only)

- [ ] **Step 1: Create the JS module**

`wwwroot/docs-interop.js` (sits next to the existing `docs-theme.js`):

```javascript
const SPY_KEY = Symbol.for('BlazixDocs.QuickNav.Spy');

export function copyText(text) {
  return navigator.clipboard.writeText(text);
}

export function setRenderMode(mode) {
  const normalized = mode === 'wasm' ? 'wasm' : 'server';
  document.cookie = `blazix-docs-render-mode=${normalized}; path=/; max-age=31536000; samesite=lax`;
  location.reload();
}

export function highlightElement(element) {
  if (element && window.hljs) {
    delete element.dataset.highlighted;
    window.hljs.highlightElement(element);
  }
}

export function observeHeadings(dotnet, ids) {
  disconnectHeadings();

  const headings = ids
    .map((id) => document.getElementById(id))
    .filter(Boolean);

  if (headings.length === 0) {
    return;
  }

  const observer = new IntersectionObserver((entries) => {
    const visible = entries.filter((entry) => entry.isIntersecting);
    if (visible.length > 0) {
      dotnet.invokeMethodAsync('SetActiveHeading', visible[0].target.id);
    }
  }, { rootMargin: '-90px 0px -65% 0px', threshold: 0 });

  headings.forEach((heading) => observer.observe(heading));
  window[SPY_KEY] = observer;
}

export function disconnectHeadings() {
  const existing = window[SPY_KEY];
  if (existing) {
    existing.disconnect();
    delete window[SPY_KEY];
  }
}
```

- [ ] **Step 2: Add highlight.js to App.razor head**

In `App.razor`, directly after the `tailwind.config` `<script>` block (line ~25), insert:

```html
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/styles/github-dark.min.css" />
    <script src="https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/highlight.min.js"></script>
    <style>
        code.hljs {
            background: transparent;
            padding: 0;
        }
    </style>
```

- [ ] **Step 3: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.sln`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/wwwroot/docs-interop.js docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Components/App.razor
git commit -m "docs: add docs-interop JS module and highlight.js"
```

---

### Task 3: TOC infrastructure (TocContext, DocsHeading, QuickNav)

**Files:**
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Services/TocContext.cs`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Docs/DocsHeading.razor`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Docs/QuickNav.razor`
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/_Imports.razor`

- [ ] **Step 1: Create TocContext**

`Services/TocContext.cs` — `TocEntry` is intentionally a class (reference identity) so a disposing component only ever removes its own registration, even if an id collides across page transitions:

```csharp
namespace Blazix.BaseUI.Docs.Client.Services;

public sealed class TocEntry(string id, string title, int level)
{
    public string Id { get; } = id;

    public string Title { get; } = title;

    public int Level { get; } = level;
}

public sealed class TocContext
{
    private readonly List<TocEntry> entries = [];

    public IReadOnlyList<TocEntry> Entries => entries;

    public event Action? Changed;

    public void Register(TocEntry entry)
    {
        entries.Add(entry);
        Changed?.Invoke();
    }

    public void Unregister(TocEntry entry)
    {
        if (entries.Remove(entry))
        {
            Changed?.Invoke();
        }
    }

    public void Clear()
    {
        if (entries.Count == 0)
        {
            return;
        }

        entries.Clear();
        Changed?.Invoke();
    }
}
```

- [ ] **Step 2: Create DocsHeading**

`Components/Docs/DocsHeading.razor`:

```razor
@implements IDisposable

@if (Level == 3)
{
    <h3 id="@ResolvedId" class="mb-3 mt-8 scroll-mt-24 text-lg font-semibold tracking-normal text-slate-950 dark:text-white">@Title</h3>
}
else
{
    <h2 id="@ResolvedId" class="mb-4 mt-10 scroll-mt-24 border-b border-white/45 pb-2 text-xl font-semibold tracking-normal text-slate-950 dark:border-white/10 dark:text-white">@Title</h2>
}

@code {
    private TocEntry? entry;

    [CascadingParameter]
    public TocContext? Toc { get; set; }

    /// <summary>
    /// Gets or sets the heading text.
    /// </summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an explicit anchor id. Defaults to a slug of <see cref="Title"/>.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the heading level (2 or 3). Defaults to 2.
    /// </summary>
    [Parameter]
    public int Level { get; set; } = 2;

    private string ResolvedId => Id ?? Slugify(Title);

    protected override void OnInitialized()
    {
        if (Toc is null)
        {
            return;
        }

        entry = new TocEntry(ResolvedId, Title, Level);
        Toc.Register(entry);
    }

    public void Dispose()
    {
        if (entry is not null)
        {
            Toc?.Unregister(entry);
        }
    }

    private static string Slugify(string value) =>
        string.Concat(value.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-')).Trim('-');
}
```

- [ ] **Step 3: Create QuickNav**

`Components/Docs/QuickNav.razor`:

```razor
@implements IAsyncDisposable
@inject IJSRuntime JSRuntime
@inject NavigationManager NavigationManager

<nav aria-label="On this page" class="text-sm">
    @if (Toc is not null && Toc.Entries.Count > 0)
    {
        <div class="mb-3 px-3 text-[11px] font-semibold uppercase tracking-[0.16em] text-slate-600 dark:text-slate-300">On this page</div>
        <ul class="m-0 grid list-none gap-0.5 p-0">
            @foreach (var entry in Toc.Entries)
            {
                <li>
                    <a href="@($"{PathWithoutFragment()}#{entry.Id}")"
                       class="@TocLinkClass(entry)"
                       style="@(entry.Level == 3 ? "padding-left: 1.75rem;" : null)">
                        @entry.Title
                    </a>
                </li>
            }
        </ul>
    }
</nav>

@code {
    private const string JsModulePath = "/docs-interop.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private DotNetObjectReference<QuickNav>? selfReference;
    private string? activeId;
    private int version;
    private int observedVersion = -1;

    [CascadingParameter]
    public TocContext? Toc { get; set; }

    public QuickNav()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        if (Toc is not null)
        {
            Toc.Changed += HandleTocChanged;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Toc is null || Toc.Entries.Count == 0 || observedVersion == version)
        {
            return;
        }

        observedVersion = version;

        try
        {
            selfReference ??= DotNetObjectReference.Create(this);
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("observeHeadings", selfReference, Toc.Entries.Select(e => e.Id).ToArray());
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    [JSInvokable]
    public void SetActiveHeading(string id)
    {
        if (activeId == id)
        {
            return;
        }

        activeId = id;
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (Toc is not null)
        {
            Toc.Changed -= HandleTocChanged;
        }

        if (moduleTask.IsValueCreated)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("disconnectHeadings");
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }

        selfReference?.Dispose();
    }

    private void HandleTocChanged()
    {
        version++;
        activeId = null;
        _ = InvokeAsync(StateHasChanged);
    }

    private string PathWithoutFragment() => new Uri(NavigationManager.Uri).AbsolutePath;

    private string TocLinkClass(TocEntry entry)
    {
        var state = activeId == entry.Id
            ? "border-sky-200/70 bg-sky-100/65 text-slate-950 dark:border-sky-300/30 dark:bg-sky-400/20 dark:text-sky-50"
            : "border-transparent text-slate-700 hover:bg-white/50 hover:text-slate-950 dark:text-slate-300 dark:hover:bg-white/10 dark:hover:text-white";
        return $"liquid-control block rounded-[18px] border px-3 py-1.5 no-underline {state}";
    }
}
```

- [ ] **Step 4: Add the Services namespace to client `_Imports.razor`**

Append to `Blazix.BaseUI.Docs.Client/_Imports.razor`:

```razor
@using Blazix.BaseUI.Docs.Client.Services
```

- [ ] **Step 5: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.sln`
Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add -A docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client
git commit -m "docs: add TOC context, DocsHeading, and QuickNav scroll-spy"
```

---

### Task 4: Server/WASM runtime switch

**Files:**
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Layout/RuntimeSwitch.razor`
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Components/App.razor`
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/_Imports.razor`

- [ ] **Step 1: Add Toggle/ToggleGroup namespaces to client `_Imports.razor`**

Append:

```razor
@using Blazix.BaseUI
@using Blazix.BaseUI.Toggle
@using Blazix.BaseUI.ToggleGroup
```

(`@using Blazix.BaseUI` also brings in the shared `Orientation` enum needed later.)

- [ ] **Step 2: Create RuntimeSwitch**

`Layout/RuntimeSwitch.razor`. It displays the truthful runtime via `RendererInfo.Name` ("Server" / "WebAssembly"; "Static" during prerender falls back to the server default) and switches by writing the cookie + reloading via JS:

```razor
@implements IAsyncDisposable
@inject IJSRuntime JSRuntime

<ToggleGroup Value="@currentValue"
             OnValueChange="HandleValueChangeAsync"
             aria-label="Interactive render mode"
             ClassValue="@(_ => "inline-flex items-center gap-1 rounded-full border border-white/45 bg-white/40 p-1 shadow-[inset_0_1px_0_rgba(255,255,255,0.6)] dark:border-white/10 dark:bg-white/10")">
    <Toggle Value="server" ClassValue="@(state => ToggleClass(state.Pressed))">Server</Toggle>
    <Toggle Value="wasm" ClassValue="@(state => ToggleClass(state.Pressed))">WASM</Toggle>
</ToggleGroup>

@code {
    private const string JsModulePath = "/docs-interop.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private IReadOnlyList<string> currentValue = ["server"];

    public RuntimeSwitch()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        currentValue = [RendererInfo.Name == "WebAssembly" ? "wasm" : "server"];
    }

    private async Task HandleValueChangeAsync(ToggleGroupValueChangeEventArgs args)
    {
        var next = args.Value.FirstOrDefault();
        if (next is null || currentValue.Contains(next))
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("setRenderMode", next);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private static string ToggleClass(bool pressed)
    {
        var state = pressed
            ? "border-sky-200/70 bg-sky-200/50 text-slate-950 dark:border-sky-300/30 dark:bg-sky-400/25 dark:text-sky-50"
            : "border-transparent text-slate-700 hover:bg-white/50 dark:text-slate-300 dark:hover:bg-white/10";
        return $"liquid-control inline-flex h-7 items-center rounded-full border px-3 text-xs font-semibold {state}";
    }

    public async ValueTask DisposeAsync()
    {
        if (!moduleTask.IsValueCreated)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }
}
```

- [ ] **Step 3: Make App.razor pick the render mode from the cookie**

In `App.razor`:

1. Replace `<HeadOutlet @rendermode="InteractiveAuto" />` with `<HeadOutlet @rendermode="ActiveRenderMode" />`.
2. Replace `<Routes @rendermode="InteractiveAuto" />` with `<Routes @rendermode="ActiveRenderMode" />`.
3. Add this `@code` block at the end of the file:

```razor
@code {
    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    private IComponentRenderMode ActiveRenderMode =>
        string.Equals(HttpContext?.Request.Cookies["blazix-docs-render-mode"], "wasm", StringComparison.OrdinalIgnoreCase)
            ? InteractiveWebAssembly
            : InteractiveServer;
}
```

(`InteractiveServer` / `InteractiveWebAssembly` come from the existing `@using static Microsoft.AspNetCore.Components.Web.RenderMode` in the server `_Imports.razor`.)

- [ ] **Step 4: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.sln`
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add -A docs/Blazix.BaseUI.Docs
git commit -m "docs: add Server/WASM runtime switch with cookie-selected render mode"
```

---

### Task 5: Shell rebuild (header, SideNav, three-column MainLayout, mobile Drawer)

**Files:**
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Layout/SideNav.razor`
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Layout/MainLayout.razor` (full rewrite)
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Data/LiquidGlassClasses.cs` (add two constants)
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/_Imports.razor`
- Delete: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Layout/NavMenu.razor`

- [ ] **Step 1: Add namespaces to client `_Imports.razor`**

Append:

```razor
@using Blazix.BaseUI.Drawer
@using Blazix.BaseUI.ScrollArea
@using Blazix.BaseUI.Tabs
@using Blazix.BaseUI.Select
@using Blazix.BaseUI.Tooltip
@using Blazix.BaseUI.Button
```

(Tabs/Select/Tooltip/Button are used by Tasks 6–7; adding them now keeps `_Imports` churn in one place.)

- [ ] **Step 2: Add shared classes to LiquidGlassClasses.cs**

Add these constants to the class (do not modify existing ones):

```csharp
    public const string ContentSurface = "liquid-materialize min-h-[60vh] rounded-[30px] border border-white/60 bg-white/85 px-5 py-8 text-slate-900 shadow-[0_18px_46px_rgba(15,23,42,0.1),inset_0_1px_0_rgba(255,255,255,0.78)] sm:px-8 dark:border-white/10 dark:bg-slate-900/80 dark:text-slate-100 dark:shadow-[0_22px_64px_rgba(0,0,0,0.32),inset_0_1px_0_rgba(255,255,255,0.1)]";
    public const string SideNavItem = "liquid-control flex items-center rounded-[18px] border border-transparent px-3 py-1.5 text-sm font-medium text-slate-700 no-underline hover:border-white/50 hover:bg-white/50 hover:text-slate-950 aria-[current=page]:border-sky-200/70 aria-[current=page]:bg-sky-100/65 aria-[current=page]:text-slate-950 dark:text-slate-300 dark:hover:border-white/10 dark:hover:bg-white/10 dark:hover:text-white dark:aria-[current=page]:border-sky-300/30 dark:aria-[current=page]:bg-sky-400/20 dark:aria-[current=page]:text-sky-50";
```

- [ ] **Step 3: Create SideNav**

`Layout/SideNav.razor` — Base UI-style sections, scrolled with the Blazix ScrollArea:

```razor
<ScrollAreaRoot ClassValue="@(_ => "relative h-full")">
    <ScrollAreaViewport ClassValue="@(_ => "h-full w-full pr-3")">
        <nav class="grid gap-6 pb-10" aria-label="Documentation">
            @foreach (var section in DocsNav.Sections)
            {
                <div>
                    <div class="px-3 pb-2 text-[11px] font-semibold uppercase tracking-[0.16em] text-slate-600 dark:text-slate-300">@section.Title</div>
                    <div class="grid gap-0.5">
                        @foreach (var link in section.Links)
                        {
                            <NavLink class="@LiquidGlassClasses.SideNavItem"
                                     href="@link.Href"
                                     Match="@(link.Href == "/" ? NavLinkMatch.All : NavLinkMatch.Prefix)"
                                     @onclick="HandleNavigateAsync">
                                @link.Name
                            </NavLink>
                        }
                    </div>
                </div>
            }
        </nav>
    </ScrollAreaViewport>
    <ScrollAreaScrollbar Orientation="Orientation.Vertical"
                         ClassValue="@(_ => "absolute inset-y-0 right-0 flex w-1.5 justify-center rounded-full bg-white/30 dark:bg-white/10")">
        <ScrollAreaThumb ClassValue="@(_ => "w-full rounded-full bg-slate-400/60 dark:bg-slate-500/60")" />
    </ScrollAreaScrollbar>
</ScrollAreaRoot>

@code {
    [Parameter]
    public EventCallback OnNavigate { get; set; }

    private async Task HandleNavigateAsync()
    {
        await OnNavigate.InvokeAsync();
    }
}
```

- [ ] **Step 4: Rewrite MainLayout**

Full replacement of `Layout/MainLayout.razor`. Header is the only fixed chrome; grid is `sidebar / content / quick-nav` collapsing right-to-left; mobile nav is a Blazix Drawer; the TOC context cascades to pages and QuickNav and clears on navigation:

```razor
@inherits LayoutComponentBase
@implements IDisposable
@inject NavigationManager NavigationManager

<CascadingValue Value="toc" IsFixed="true">
    <div class="mx-auto min-h-screen max-w-[100rem] px-3 sm:px-5">
        <header class="liquid-glass liquid-materialize sticky top-3 z-30 flex items-center justify-between gap-3 rounded-[28px] border border-white/45 bg-white/50 px-3 py-2 shadow-[0_18px_54px_rgba(15,23,42,0.14),inset_0_1px_0_rgba(255,255,255,0.72)] dark:border-white/10 dark:bg-slate-950/60 dark:shadow-[0_18px_54px_rgba(0,0,0,0.32)]">
            <div class="flex min-w-0 items-center gap-2">
                <DrawerRoot @bind-Open="isNavOpen">
                    <DrawerTrigger aria-label="Open navigation"
                                   ClassValue="@(_ => $"{LiquidGlassClasses.IconButton} lg:hidden")">
                        <span class="grid gap-1.5" aria-hidden="true">
                            <span class="block h-0.5 w-5 rounded-full bg-current"></span>
                            <span class="block h-0.5 w-5 rounded-full bg-current"></span>
                            <span class="block h-0.5 w-5 rounded-full bg-current"></span>
                        </span>
                    </DrawerTrigger>
                    <DrawerPortal>
                        <DrawerBackdrop ClassValue="@(_ => "fixed inset-0 z-40 bg-slate-950/30 backdrop-blur-sm dark:bg-black/45")" />
                        <DrawerViewport ClassValue="@(_ => "fixed inset-0 z-50")">
                            <DrawerPopup ClassValue="@(_ => "liquid-glass fixed inset-y-3 left-3 z-50 w-[min(88vw,320px)] overflow-hidden rounded-[28px] border border-white/45 bg-white/80 p-4 shadow-[0_30px_95px_rgba(15,23,42,0.2)] dark:border-white/10 dark:bg-slate-950/85")">
                                <DrawerContent ClassValue="@(_ => "flex h-full flex-col gap-4")">
                                    <DrawerTitle ClassValue="@(_ => "m-0 text-sm font-semibold text-slate-950 dark:text-white")">Navigation</DrawerTitle>
                                    <div class="flex items-center justify-between gap-3">
                                        <RuntimeSwitch />
                                        <ThemeToggle Compact="true" />
                                    </div>
                                    <div class="min-h-0 flex-1">
                                        <SideNav OnNavigate="CloseNav" />
                                    </div>
                                </DrawerContent>
                            </DrawerPopup>
                        </DrawerViewport>
                    </DrawerPortal>
                </DrawerRoot>
                <a href="/" class="flex min-w-0 items-center gap-2 no-underline">
                    <span class="truncate text-sm font-semibold text-slate-950 dark:text-white">Blazix.BaseUI</span>
                    <span class="hidden text-[11px] font-medium uppercase tracking-[0.14em] text-sky-700 sm:inline dark:text-sky-300">Docs</span>
                </a>
            </div>
            <div class="flex items-center gap-2">
                <span class="hidden lg:inline-flex"><RuntimeSwitch /></span>
                <ThemeToggle Compact="true" />
                <a class="@LiquidGlassClasses.IconButton"
                   href="https://github.com/JakeMurrayDev/Blazix.BaseUI"
                   target="_blank"
                   rel="noreferrer"
                   aria-label="GitHub repository">
                    <svg width="18" height="18" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27s1.36.09 2 .27c1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.01 8.01 0 0 0 16 8c0-4.42-3.58-8-8-8Z"/></svg>
                </a>
            </div>
        </header>

        <div class="grid gap-6 py-6 lg:grid-cols-[17.5rem_minmax(0,1fr)] xl:grid-cols-[17.5rem_minmax(0,1fr)_15rem]">
            <aside class="hidden lg:block">
                <div class="sticky top-[5.25rem] h-[calc(100vh-6.5rem)]">
                    <SideNav />
                </div>
            </aside>
            <main class="min-w-0">
                <article class="@LiquidGlassClasses.ContentSurface">
                    @Body
                </article>
            </main>
            <aside class="hidden xl:block">
                <div class="sticky top-[5.25rem] max-h-[calc(100vh-6.5rem)] overflow-y-auto">
                    <QuickNav />
                </div>
            </aside>
        </div>
    </div>
</CascadingValue>

<div id="blazor-error-ui"
     data-nosnippet
     class="liquid-glass fixed bottom-3 left-3 right-3 z-50 hidden rounded-[24px] border border-white/50 bg-white/75 p-3 shadow-[0_18px_54px_rgba(15,23,42,0.18)] dark:border-white/10 dark:bg-slate-950/80">
    An unhandled error has occurred.
    <a href="." class="@LiquidGlassClasses.Button">Reload</a>
    <span class="dismiss">x</span>
</div>

@code {
    private readonly TocContext toc = new();

    private bool isNavOpen;

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= HandleLocationChanged;
    }

    private void HandleLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        toc.Clear();
    }

    private void CloseNav()
    {
        isNavOpen = false;
    }
}
```

- [ ] **Step 5: Delete NavMenu.razor**

```bash
rm docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Layout/NavMenu.razor
```

- [ ] **Step 6: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.sln`
Expected: Build succeeded, 0 errors.

If `DrawerViewport`, `DrawerContent`, or `DrawerTitle` fail to compile with the parameters shown, check their `.razor` files under `src/Blazix.BaseUI/Drawer/` for the exact parameter names and adjust (all parts accept `ClassValue`).

- [ ] **Step 7: Preview smoke check**

Start the preview (`dotnet run` on the server project via preview_start, project `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs`). Verify: header renders; sidebar shows Overview/Handbook/Components/Utils with 35 component links; existing pages (e.g. `/components/accordion`) still render inside the new content column; narrow viewport (preview_resize ~390px) shows the hamburger and the Drawer opens with nav + switches; runtime switch reloads into WASM mode and back.

- [ ] **Step 8: Commit**

```bash
git add -A docs/Blazix.BaseUI.Docs
git commit -m "docs: rebuild shell with Base UI three-column layout and Drawer nav"
```

---

### Task 6: Doc primitives (CopyButton, CodeBlock, ApiTable, ComponentPart, ApiPartReference, DocsPageIntro)

**Files:**
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Docs/CopyButton.razor`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Docs/ApiPartReference.razor`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Docs/DocsPageIntro.razor`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Data/ComponentPart.cs`
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Docs/CodeBlock.razor` (full rewrite)
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Docs/ApiTable.razor` (full rewrite)

- [ ] **Step 1: Create CopyButton**

`Components/Docs/CopyButton.razor` — Blazix Tooltip parts; the trigger is the button; check feedback for 2 s:

```razor
@implements IAsyncDisposable
@inject IJSRuntime JSRuntime

<TooltipProvider>
    <TooltipRoot>
        <TooltipTrigger aria-label="Copy code"
                        ClassValue="@(_ => ButtonClass)"
                        @onclick="CopyAsync">
            @if (copied)
            {
                <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="M2.5 8.5 6 12l7.5-8" /></svg>
            }
            else
            {
                <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><rect x="5.5" y="5.5" width="8" height="8" rx="1.5" /><path d="M10.5 5.5v-2a1.5 1.5 0 0 0-1.5-1.5H4A1.5 1.5 0 0 0 2.5 3.5V9A1.5 1.5 0 0 0 4 10.5h1.5" /></svg>
            }
        </TooltipTrigger>
        <TooltipPortal>
            <TooltipPositioner SideOffset="6">
                <TooltipPopup ClassValue="@(_ => PopupClass)">@(copied ? "Copied" : "Copy code")</TooltipPopup>
            </TooltipPositioner>
        </TooltipPortal>
    </TooltipRoot>
</TooltipProvider>

@code {
    private const string JsModulePath = "/docs-interop.js";
    private const string ButtonClass = "liquid-control inline-flex size-8 items-center justify-center rounded-full border border-white/45 bg-white/55 text-slate-900 shadow-[inset_0_1px_0_rgba(255,255,255,0.72)] hover:bg-white/70 focus:outline focus:outline-2 focus:outline-offset-2 focus:outline-sky-400 dark:border-white/10 dark:bg-white/10 dark:text-slate-100 dark:hover:bg-white/15";
    private const string PopupClass = "liquid-glass rounded-full border border-white/50 bg-white/80 px-3 py-1 text-xs font-medium text-slate-900 shadow-[0_10px_28px_rgba(15,23,42,0.16)] dark:border-white/10 dark:bg-slate-950/80 dark:text-slate-100";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool copied;
    private CancellationTokenSource? resetCts;

    /// <summary>
    /// Gets or sets the text copied to the clipboard.
    /// </summary>
    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    public CopyButton()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    private async Task CopyAsync()
    {
        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("copyText", Text);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
            return;
        }

        resetCts?.Cancel();
        resetCts?.Dispose();
        resetCts = new CancellationTokenSource();
        copied = true;

        try
        {
            await Task.Delay(2000, resetCts.Token);
            copied = false;
        }
        catch (TaskCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        resetCts?.Cancel();
        resetCts?.Dispose();

        if (!moduleTask.IsValueCreated)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }
}
```

- [ ] **Step 2: Rewrite CodeBlock**

`Components/Docs/CodeBlock.razor` — adds highlight.js (via the module) and a copy button. `@key` forces DOM recreation when the code changes so highlight.js DOM mutations never fight the Blazor diff. `Language` is the highlight.js language (`xml` works well for Razor markup):

```razor
@implements IAsyncDisposable
@inject IJSRuntime JSRuntime

<div class="relative" @key="@Code">
    <pre class="@LiquidGlassClasses.CodeBlock"><code class="@($"language-{Language}")" @ref="codeElement">@Code</code></pre>
    @if (ShowCopy)
    {
        <div class="absolute right-2.5 top-2.5">
            <CopyButton Text="@Code" />
        </div>
    }
</div>

@code {
    private const string JsModulePath = "/docs-interop.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private ElementReference codeElement;
    private string? highlightedCode;

    [Parameter]
    public string Code { get; set; } = string.Empty;

    [Parameter]
    public string Language { get; set; } = "xml";

    [Parameter]
    public bool ShowCopy { get; set; } = true;

    public CodeBlock()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (highlightedCode == Code)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("highlightElement", codeElement);
            highlightedCode = Code;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!moduleTask.IsValueCreated)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }
}
```

- [ ] **Step 3: Create ComponentPart record**

`Data/ComponentPart.cs`:

```csharp
namespace Blazix.BaseUI.Docs.Client.Data;

public sealed record ComponentPart(
    string Name,
    string Description,
    IReadOnlyList<ApiRow> Parameters,
    IReadOnlyList<ApiRow> DataAttributes,
    IReadOnlyList<ApiRow> CssVariables);
```

- [ ] **Step 4: Rewrite ApiTable with optional columns**

`Components/Docs/ApiTable.razor` (full replacement). Attribute/variable tables pass `ShowType="false" ShowDefault="false"` and render two columns:

```razor
<div class="max-w-full overflow-x-auto rounded-[24px] border border-white/55 bg-white/80 shadow-[inset_0_1px_0_rgba(255,255,255,0.78),0_14px_36px_rgba(15,23,42,0.08)] dark:border-white/10 dark:bg-slate-950/60">
    <table class="@($"w-full border-collapse text-left text-sm {(ShowType ? "min-w-[720px] table-fixed" : "min-w-[420px]")}")">
        <thead class="bg-white/50 text-[11px] uppercase tracking-[0.14em] text-slate-600 dark:bg-white/10 dark:text-slate-300">
            <tr>
                <th class="@($"{(ShowType ? "w-[22%]" : "w-[30%]")} border-b border-white/45 px-4 py-3 font-semibold dark:border-white/10")">Name</th>
                @if (ShowType)
                {
                    <th class="w-[28%] border-b border-white/45 px-4 py-3 font-semibold dark:border-white/10">Type</th>
                }
                @if (ShowDefault)
                {
                    <th class="w-[12%] border-b border-white/45 px-4 py-3 font-semibold dark:border-white/10">Default</th>
                }
                <th class="border-b border-white/45 px-4 py-3 font-semibold dark:border-white/10">Description</th>
            </tr>
        </thead>
        <tbody class="divide-y divide-white/45 dark:divide-white/10">
            @foreach (var row in Rows)
            {
                <tr class="align-top">
                    <td class="break-words px-4 py-3 font-mono text-[12px] font-semibold text-slate-950 dark:text-slate-50">@row.Name</td>
                    @if (ShowType)
                    {
                        <td class="px-4 py-3 [overflow-wrap:anywhere]">
                            <code class="@LiquidGlassClasses.CodeInline">@row.Type</code>
                        </td>
                    }
                    @if (ShowDefault)
                    {
                        <td class="px-4 py-3 [overflow-wrap:anywhere]">
                            <code class="@LiquidGlassClasses.CodeInline">@row.DefaultValue</code>
                        </td>
                    }
                    <td class="whitespace-normal px-4 py-3 leading-6 text-slate-700 [overflow-wrap:anywhere] dark:text-slate-200">@row.Description</td>
                </tr>
            }
        </tbody>
    </table>
</div>

@code {
    [Parameter]
    public IReadOnlyList<ApiRow> Rows { get; set; } = [];

    [Parameter]
    public bool ShowType { get; set; } = true;

    [Parameter]
    public bool ShowDefault { get; set; } = true;
}
```

- [ ] **Step 5: Create ApiPartReference**

`Components/Docs/ApiPartReference.razor`:

```razor
<DocsHeading Level="3" Title="@Part.Name" Id="@($"api-{Part.Name.ToLowerInvariant().Replace(' ', '-')}")" />
<p class="mb-4 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">@Part.Description</p>
<div class="grid gap-4">
    @if (Part.Parameters.Count > 0)
    {
        <ApiTable Rows="@Part.Parameters" />
    }
    @if (Part.DataAttributes.Count > 0)
    {
        <div>
            <div class="mb-2 text-[11px] font-semibold uppercase tracking-[0.16em] text-slate-600 dark:text-slate-300">Data attributes</div>
            <ApiTable Rows="@Part.DataAttributes" ShowType="false" ShowDefault="false" />
        </div>
    }
    @if (Part.CssVariables.Count > 0)
    {
        <div>
            <div class="mb-2 text-[11px] font-semibold uppercase tracking-[0.16em] text-slate-600 dark:text-slate-300">CSS variables</div>
            <ApiTable Rows="@Part.CssVariables" ShowType="false" ShowDefault="false" />
        </div>
    }
</div>

@code {
    [Parameter, EditorRequired]
    public ComponentPart Part { get; set; } = null!;
}
```

- [ ] **Step 6: Create DocsPageIntro**

`Components/Docs/DocsPageIntro.razor` — shared eyebrow/title/subtitle header used by every page:

```razor
<p class="mb-1 text-[11px] font-semibold uppercase tracking-[0.18em] text-sky-700 dark:text-sky-300">@Eyebrow</p>
<div class="flex flex-wrap items-start justify-between gap-3">
    <h1 tabindex="-1" class="m-0 text-3xl font-semibold tracking-normal text-slate-950 focus:outline-none dark:text-white">@Title</h1>
    @if (!string.IsNullOrEmpty(MarkdownHref))
    {
        <a class="@LiquidGlassClasses.Button" href="@MarkdownHref">Open .md</a>
    }
</div>
@if (!string.IsNullOrEmpty(Subtitle))
{
    <p class="mb-0 mt-3 max-w-3xl text-base leading-7 text-slate-700 dark:text-slate-200">@Subtitle</p>
}

@code {
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string Eyebrow { get; set; } = string.Empty;

    [Parameter]
    public string? Subtitle { get; set; }

    [Parameter]
    public string? MarkdownHref { get; set; }
}
```

- [ ] **Step 7: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.sln`
Expected: Build succeeded, 0 errors. (Existing pages call `CodeBlock` with only `Code` and `ApiTable` with only `Rows` — both remain valid because new parameters have defaults.)

- [ ] **Step 8: Commit**

```bash
git add -A docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client
git commit -m "docs: add copy button, highlighted code block, and per-part API reference primitives"
```

---

### Task 7: Demo showcase component + embedded demo sources

**Files:**
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Data/DemoSources.cs`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Docs/Demo.razor`
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Blazix.BaseUI.Docs.Client.csproj`

- [ ] **Step 1: Create demo source models and loader**

`Data/DemoSources.cs`. Demo `.razor` files are compiled as components AND embedded as resources; `ResourcePath` is the manifest-name suffix after the root namespace (folders joined with dots):

```csharp
using System.Collections.Concurrent;

namespace Blazix.BaseUI.Docs.Client.Data;

public sealed record DemoFile(string Name, string Language, string ResourcePath)
{
    public string Code => DemoSources.GetCode(ResourcePath);
}

public sealed record DemoVariant(string Name, IReadOnlyList<DemoFile> Files);

public static class DemoSources
{
    private const string ResourcePrefix = "Blazix.BaseUI.Docs.Client.";

    private static readonly ConcurrentDictionary<string, string> Cache = new();

    public static string GetCode(string resourcePath) => Cache.GetOrAdd(resourcePath, static path =>
    {
        var assembly = typeof(DemoSources).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourcePrefix + path)
            ?? throw new InvalidOperationException($"Embedded demo source '{ResourcePrefix}{path}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Replace("\r\n", "\n").TrimEnd() + "\n";
    });
}
```

- [ ] **Step 2: Add EmbeddedResource globs to the client csproj**

Add this `ItemGroup` to `Blazix.BaseUI.Docs.Client.csproj` (the `.razor` files stay compiled components; this additionally embeds their text):

```xml
  <ItemGroup>
    <EmbeddedResource Include="Components\Demos\**\*.razor" />
    <EmbeddedResource Include="wwwroot\demos\**\*.css" />
  </ItemGroup>
```

- [ ] **Step 3: Create the Demo component**

`Components/Docs/Demo.razor` — live preview, toolbar (file tabs via Blazix Tabs, variant Select, CopyButton), collapsible code via Blazix Collapsible (trigger + root state; content stays mounted so the collapsed preview shows the first lines under a gradient):

```razor
<div class="overflow-clip rounded-[28px] border border-white/60 bg-white/80 shadow-[0_14px_34px_rgba(15,23,42,0.08),inset_0_1px_0_rgba(255,255,255,0.86)] dark:border-white/10 dark:bg-slate-900/70">
    <div class="flex min-h-32 items-center justify-center px-6 py-10">
        @ChildContent
    </div>

    <div class="flex flex-wrap items-center justify-between gap-2 border-t border-white/45 px-3 py-2 dark:border-white/10">
        <div class="min-w-0">
            @if (ActiveVariant.Files.Count > 1)
            {
                <TabsRoot TValue="string" Value="@ActiveFile.Name" ValueChanged="HandleFileChanged">
                    <TabsList TValue="string" ClassValue="@(_ => "flex items-center gap-1")">
                        @foreach (var file in ActiveVariant.Files)
                        {
                            <TabsTab TValue="string"
                                     Value="@file.Name"
                                     ClassValue="@(state => FileTabClass(state.Selected))">
                                @file.Name
                            </TabsTab>
                        }
                    </TabsList>
                </TabsRoot>
            }
            else
            {
                <span class="px-2 font-mono text-xs text-slate-700 dark:text-slate-300">@ActiveFile.Name</span>
            }
        </div>
        <div class="flex items-center gap-2">
            @if (Variants.Count > 1)
            {
                <SelectRoot TValue="string" Value="@ActiveVariant.Name" ValueChanged="HandleVariantChanged">
                    <SelectTrigger aria-label="Styling variant"
                                   ClassValue="@(_ => "liquid-control inline-flex h-8 items-center gap-2 rounded-full border border-white/45 bg-white/55 px-3 text-xs font-semibold text-slate-900 hover:bg-white/70 focus:outline focus:outline-2 focus:outline-offset-2 focus:outline-sky-400 dark:border-white/10 dark:bg-white/10 dark:text-slate-100 dark:hover:bg-white/15")">
                        <SelectValue TValue="string" />
                        <SelectIcon>
                            <svg width="10" height="10" viewBox="0 0 10 10" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path d="m1.5 3.5 3.5 3.5L8.5 3.5" /></svg>
                        </SelectIcon>
                    </SelectTrigger>
                    <SelectPortal>
                        <SelectPositioner SideOffset="4">
                            <SelectPopup ClassValue="@(_ => "liquid-glass z-50 overflow-clip rounded-[20px] border border-white/50 bg-white/80 p-1 text-sm shadow-[0_18px_46px_rgba(15,23,42,0.16)] dark:border-white/10 dark:bg-slate-950/80")">
                                @foreach (var variant in Variants)
                                {
                                    <SelectItem TValue="string"
                                                Value="@variant.Name"
                                                Label="@variant.Name"
                                                ClassValue="@(state => VariantItemClass(state.Selected))">
                                        <SelectItemText>@variant.Name</SelectItemText>
                                    </SelectItem>
                                }
                            </SelectPopup>
                        </SelectPositioner>
                    </SelectPortal>
                </SelectRoot>
            }
            <CopyButton Text="@ActiveFile.Code" />
        </div>
    </div>

    <CollapsibleRoot Open="@isExpanded" OpenChanged="@(open => isExpanded = open)">
        <div class="relative">
            <div class="@(ShouldCollapse ? "max-h-[16.5rem] overflow-hidden" : null)">
                <CodeBlock Code="@ActiveFile.Code" Language="@ActiveFile.Language" ShowCopy="false" />
            </div>
            @if (ShouldCollapse)
            {
                <div class="pointer-events-none absolute inset-x-0 bottom-0 h-28 rounded-b-[24px] bg-gradient-to-t from-slate-950 to-transparent"></div>
            }
            @if (IsCollapsible)
            {
                <div class="@(ShouldCollapse ? "absolute inset-x-0 bottom-3 flex justify-center" : "flex justify-center pb-3")">
                    <CollapsibleTrigger ClassValue="@(_ => LiquidGlassClasses.Button)">
                        @(isExpanded ? "Hide code" : "Show code")
                    </CollapsibleTrigger>
                </div>
            }
        </div>
    </CollapsibleRoot>
</div>

@code {
    private string? activeVariantName;
    private string? activeFileName;
    private bool isExpanded;

    /// <summary>
    /// Gets or sets the code variants (e.g. CSS and Tailwind) shown for this demo.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<DemoVariant> Variants { get; set; } = [];

    /// <summary>
    /// Gets or sets the live demo content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private DemoVariant ActiveVariant =>
        Variants.FirstOrDefault(v => v.Name == activeVariantName) ?? Variants[0];

    private DemoFile ActiveFile =>
        ActiveVariant.Files.FirstOrDefault(f => f.Name == activeFileName) ?? ActiveVariant.Files[0];

    private bool IsCollapsible => ActiveFile.Code.Count(c => c == '\n') > 12;

    private bool ShouldCollapse => IsCollapsible && !isExpanded;

    private void HandleFileChanged(string? value) => activeFileName = value;

    private void HandleVariantChanged(string? value)
    {
        activeVariantName = value;
        activeFileName = null;
    }

    private static string FileTabClass(bool selected)
    {
        var state = selected
            ? "border-sky-200/70 bg-sky-100/65 text-slate-950 dark:border-sky-300/30 dark:bg-sky-400/20 dark:text-sky-50"
            : "border-transparent text-slate-700 hover:bg-white/50 dark:text-slate-300 dark:hover:bg-white/10";
        return $"liquid-control inline-flex h-8 items-center rounded-full border px-3 font-mono text-xs {state}";
    }

    private static string VariantItemClass(bool selected)
    {
        var state = selected
            ? "bg-sky-100/65 text-slate-950 dark:bg-sky-400/20 dark:text-sky-50"
            : "text-slate-800 hover:bg-white/60 dark:text-slate-200 dark:hover:bg-white/10";
        return $"flex cursor-default items-center rounded-[14px] px-3 py-1.5 text-xs font-semibold {state}";
    }
}
```

Notes for the implementer:
- `TabsTab`'s state property for selection is named per `src/Blazix.BaseUI/Tabs/TabsTabState.cs` — if `state.Selected` does not compile, open that file and use the actual property (e.g. `Active`).
- Same for `SelectItem` state (`state.Selected` vs `state.Highlighted`) — check `src/Blazix.BaseUI/Select/SelectItemState.cs`.
- The live preview is a single `ChildContent` regardless of selected code variant; the CSS and Tailwind variants are visually identical by design.

- [ ] **Step 4: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.sln`
Expected: Build succeeded, 0 errors. (No demo folders exist yet; the globs match nothing, which is fine.)

- [ ] **Step 5: Commit**

```bash
git add -A docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client
git commit -m "docs: add Demo showcase component with variants, file tabs, and copy"
```

---

### Task 8: Accordion demo components (CSS + Tailwind variants)

**Files:**
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Demos/Accordion/Hero/Css/AccordionHeroCss.razor`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Demos/Accordion/Hero/Tailwind/AccordionHeroTailwind.razor`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Demos/Accordion/Multiple/Css/AccordionMultipleCss.razor`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Demos/Accordion/Multiple/Tailwind/AccordionMultipleTailwind.razor`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/wwwroot/demos/accordion.css`
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Components/App.razor` (one stylesheet link)

Background: `data-panel-open`, `data-starting-style`, `data-ending-style` are emitted as boolean attributes (present when true, absent when false — `attrs["data-panel-open"] = state.Open` in `AccordionTrigger.razor:244`), so presence selectors (`[data-panel-open]`, `data-[panel-open]:`) are correct. The panel exposes `--accordion-panel-height` for the height animation. Static `class="..."` attributes merge with `ClassValue` and keep the displayed source clean.

- [ ] **Step 1: Create the shared demo stylesheet**

`wwwroot/demos/accordion.css` (served globally; also embedded for display — class names are prefixed to avoid collisions):

```css
.blx-accordion {
  box-sizing: border-box;
  display: flex;
  width: 24rem;
  max-width: calc(100vw - 8rem);
  flex-direction: column;
  justify-content: center;
  color: inherit;
}

.blx-accordion-item {
  border-bottom: 1px solid rgba(100, 116, 139, 0.4);
}

.blx-accordion-header {
  margin: 0;
}

.blx-accordion-trigger {
  box-sizing: border-box;
  display: flex;
  width: 100%;
  align-items: baseline;
  justify-content: space-between;
  gap: 1rem;
  margin: 0;
  padding: 0.5rem 0;
  border: none;
  background: none;
  color: inherit;
  font: inherit;
  font-weight: 500;
  text-align: left;
  cursor: pointer;
}

.blx-accordion-trigger:focus-visible {
  outline: 2px solid #38bdf8;
  outline-offset: 2px;
}

.blx-accordion-icon {
  flex-shrink: 0;
  width: 0.75rem;
  height: 0.75rem;
  margin-right: 0.5rem;
  transition: transform 150ms ease-out;
}

.blx-accordion-trigger[data-panel-open] .blx-accordion-icon {
  transform: rotate(45deg);
}

.blx-accordion-panel {
  box-sizing: border-box;
  height: var(--accordion-panel-height);
  overflow: hidden;
  font-size: 0.875rem;
  line-height: 1.25rem;
  transition: height 150ms ease-out;
}

.blx-accordion-panel[data-starting-style],
.blx-accordion-panel[data-ending-style] {
  height: 0;
}

.blx-accordion-content {
  padding-bottom: 0.75rem;
}

@media (prefers-reduced-motion: reduce) {
  .blx-accordion-icon,
  .blx-accordion-panel {
    transition: none;
  }
}
```

- [ ] **Step 2: Create the hero CSS-variant demo**

`Components/Demos/Accordion/Hero/Css/AccordionHeroCss.razor`:

```razor
<AccordionRoot TValue="string" class="blx-accordion">
    <AccordionItem Value="@("what-is-blazix")">
        <AccordionHeader class="blx-accordion-header">
            <AccordionTrigger class="blx-accordion-trigger">
                What is Blazix.BaseUI?
                <svg class="blx-accordion-icon" viewBox="0 0 12 12" fill="currentColor" aria-hidden="true"><path d="M6.75 0H5.25V5.25H0V6.75L5.25 6.75V12H6.75V6.75L12 6.75V5.25H6.75V0Z" /></svg>
            </AccordionTrigger>
        </AccordionHeader>
        <AccordionPanel class="blx-accordion-panel">
            <div class="blx-accordion-content">
                Blazix.BaseUI is a library of high-quality unstyled Blazor components for design systems and web apps.
            </div>
        </AccordionPanel>
    </AccordionItem>
    <AccordionItem Value="@("get-started")">
        <AccordionHeader class="blx-accordion-header">
            <AccordionTrigger class="blx-accordion-trigger">
                How do I get started?
                <svg class="blx-accordion-icon" viewBox="0 0 12 12" fill="currentColor" aria-hidden="true"><path d="M6.75 0H5.25V5.25H0V6.75L5.25 6.75V12H6.75V6.75L12 6.75V5.25H6.75V0Z" /></svg>
            </AccordionTrigger>
        </AccordionHeader>
        <AccordionPanel class="blx-accordion-panel">
            <div class="blx-accordion-content">
                Head to the Quick start guide in the docs. If you have used headless component libraries before, you will feel right at home.
            </div>
        </AccordionPanel>
    </AccordionItem>
    <AccordionItem Value="@("can-i-use-it")">
        <AccordionHeader class="blx-accordion-header">
            <AccordionTrigger class="blx-accordion-trigger">
                Can I use it in my project?
                <svg class="blx-accordion-icon" viewBox="0 0 12 12" fill="currentColor" aria-hidden="true"><path d="M6.75 0H5.25V5.25H0V6.75L5.25 6.75V12H6.75V6.75L12 6.75V5.25H6.75V0Z" /></svg>
            </AccordionTrigger>
        </AccordionHeader>
        <AccordionPanel class="blx-accordion-panel">
            <div class="blx-accordion-content">
                Of course! Blazix.BaseUI is free and open source.
            </div>
        </AccordionPanel>
    </AccordionItem>
</AccordionRoot>
```

- [ ] **Step 3: Create the hero Tailwind-variant demo**

`Components/Demos/Accordion/Hero/Tailwind/AccordionHeroTailwind.razor` (Tailwind v3 Play CDN syntax — `data-[x]:` and `group-data-[x]:` arbitrary variants):

```razor
<AccordionRoot TValue="string" class="flex w-96 max-w-[calc(100vw-8rem)] flex-col justify-center">
    <AccordionItem Value="@("what-is-blazix")" class="border-b border-slate-500/40">
        <AccordionHeader class="m-0">
            <AccordionTrigger class="group flex w-full cursor-pointer items-baseline justify-between gap-4 border-0 bg-transparent py-2 text-left font-medium text-inherit focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-400">
                What is Blazix.BaseUI?
                <svg class="mr-2 size-3 shrink-0 transition-transform duration-150 ease-out group-data-[panel-open]:rotate-45" viewBox="0 0 12 12" fill="currentColor" aria-hidden="true"><path d="M6.75 0H5.25V5.25H0V6.75L5.25 6.75V12H6.75V6.75L12 6.75V5.25H6.75V0Z" /></svg>
            </AccordionTrigger>
        </AccordionHeader>
        <AccordionPanel class="h-[var(--accordion-panel-height)] overflow-hidden text-sm transition-[height] duration-150 ease-out data-[ending-style]:h-0 data-[starting-style]:h-0">
            <div class="pb-3">Blazix.BaseUI is a library of high-quality unstyled Blazor components for design systems and web apps.</div>
        </AccordionPanel>
    </AccordionItem>
    <AccordionItem Value="@("get-started")" class="border-b border-slate-500/40">
        <AccordionHeader class="m-0">
            <AccordionTrigger class="group flex w-full cursor-pointer items-baseline justify-between gap-4 border-0 bg-transparent py-2 text-left font-medium text-inherit focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-400">
                How do I get started?
                <svg class="mr-2 size-3 shrink-0 transition-transform duration-150 ease-out group-data-[panel-open]:rotate-45" viewBox="0 0 12 12" fill="currentColor" aria-hidden="true"><path d="M6.75 0H5.25V5.25H0V6.75L5.25 6.75V12H6.75V6.75L12 6.75V5.25H6.75V0Z" /></svg>
            </AccordionTrigger>
        </AccordionHeader>
        <AccordionPanel class="h-[var(--accordion-panel-height)] overflow-hidden text-sm transition-[height] duration-150 ease-out data-[ending-style]:h-0 data-[starting-style]:h-0">
            <div class="pb-3">Head to the Quick start guide in the docs. If you have used headless component libraries before, you will feel right at home.</div>
        </AccordionPanel>
    </AccordionItem>
    <AccordionItem Value="@("can-i-use-it")" class="border-b border-slate-500/40">
        <AccordionHeader class="m-0">
            <AccordionTrigger class="group flex w-full cursor-pointer items-baseline justify-between gap-4 border-0 bg-transparent py-2 text-left font-medium text-inherit focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-400">
                Can I use it in my project?
                <svg class="mr-2 size-3 shrink-0 transition-transform duration-150 ease-out group-data-[panel-open]:rotate-45" viewBox="0 0 12 12" fill="currentColor" aria-hidden="true"><path d="M6.75 0H5.25V5.25H0V6.75L5.25 6.75V12H6.75V6.75L12 6.75V5.25H6.75V0Z" /></svg>
            </AccordionTrigger>
        </AccordionHeader>
        <AccordionPanel class="h-[var(--accordion-panel-height)] overflow-hidden text-sm transition-[height] duration-150 ease-out data-[ending-style]:h-0 data-[starting-style]:h-0">
            <div class="pb-3">Of course! Blazix.BaseUI is free and open source.</div>
        </AccordionPanel>
    </AccordionItem>
</AccordionRoot>
```

- [ ] **Step 4: Create the "multiple" variants**

`Components/Demos/Accordion/Multiple/Css/AccordionMultipleCss.razor`: copy `AccordionHeroCss.razor` exactly, changing only the root tag to:

```razor
<AccordionRoot TValue="string" Multiple="true" class="blx-accordion">
```

`Components/Demos/Accordion/Multiple/Tailwind/AccordionMultipleTailwind.razor`: copy `AccordionHeroTailwind.razor` exactly, changing only the root tag to:

```razor
<AccordionRoot TValue="string" Multiple="true" class="flex w-96 max-w-[calc(100vw-8rem)] flex-col justify-center">
```

- [ ] **Step 5: Link the demo stylesheet in App.razor**

In `App.razor` head, after the favicon link, add:

```html
    <link rel="stylesheet" href="@Assets["demos/accordion.css"]" />
```

- [ ] **Step 6: Build and verify embedded resources**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.sln`
Expected: Build succeeded, 0 errors.

Then verify the embedded manifest names match what Task 9's `ResourcePath` strings will expect:

```bash
cd docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client
cat > /tmp/list-resources.csx <<'EOF'
var asm = System.Reflection.Assembly.LoadFrom("bin/Debug/net10.0/Blazix.BaseUI.Docs.Client.dll");
foreach (var name in asm.GetManifestResourceNames()) System.Console.WriteLine(name);
EOF
dotnet tool run dotnet-script /tmp/list-resources.csx 2>/dev/null || dotnet script /tmp/list-resources.csx 2>/dev/null || echo "dotnet-script unavailable - rely on runtime check"
```

Expected output includes:

```
Blazix.BaseUI.Docs.Client.Components.Demos.Accordion.Hero.Css.AccordionHeroCss.razor
Blazix.BaseUI.Docs.Client.Components.Demos.Accordion.Hero.Tailwind.AccordionHeroTailwind.razor
Blazix.BaseUI.Docs.Client.Components.Demos.Accordion.Multiple.Css.AccordionMultipleCss.razor
Blazix.BaseUI.Docs.Client.Components.Demos.Accordion.Multiple.Tailwind.AccordionMultipleTailwind.razor
Blazix.BaseUI.Docs.Client.wwwroot.demos.accordion.css
```

If `dotnet-script` isn't installed, skip this — the runtime check in Task 9 Step 5 covers it (the Demo component throws an `InvalidOperationException` that names the exact missing resource, and `DemoSources.GetCode` can then be aligned with the real names).

- [ ] **Step 7: Commit**

```bash
git add -A docs/Blazix.BaseUI.Docs
git commit -m "docs: add Accordion demo components in CSS and Tailwind variants"
```

---

### Task 9: Accordion API data + Accordion page

**Files:**
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Data/AccordionApi.cs`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/AccordionPage.razor`
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/_Imports.razor` (one namespace)

- [ ] **Step 1: Add the Demos namespaces to `_Imports.razor`**

Append:

```razor
@using Blazix.BaseUI.Docs.Client.Components.Demos.Accordion.Hero.Css
@using Blazix.BaseUI.Docs.Client.Components.Demos.Accordion.Multiple.Css
```

- [ ] **Step 2: Create AccordionApi data**

`Data/AccordionApi.cs` — parameter data sourced from `src/Blazix.BaseUI/Accordion/*.razor`:

```csharp
namespace Blazix.BaseUI.Docs.Client.Data;

public static class AccordionApi
{
    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Root",
            "Groups all parts of the accordion. Renders a <div> element by default.",
            [
                new ApiRow("Value", "TValue[]?", "null", "The controlled value of the item(s) that should be expanded. To render an uncontrolled accordion, use DefaultValue instead."),
                new ApiRow("DefaultValue", "TValue[]", "[]", "The uncontrolled value of the item(s) that should be initially expanded. To render a controlled accordion, use Value instead."),
                new ApiRow("Disabled", "bool", "false", "Determines whether the component should ignore user interaction."),
                new ApiRow("Multiple", "bool", "false", "Determines whether multiple items can be open at the same time."),
                new ApiRow("Orientation", "Orientation", "Vertical", "The visual orientation of the accordion. Controls whether roving focus uses left/right or up/down arrow keys."),
                new ApiRow("LoopFocus", "bool", "true", "Determines whether focus loops back to the first item when the end of the list is reached while using the arrow keys."),
                new ApiRow("HiddenUntilFound", "bool", "false", "Allows the browser's built-in page search to find and expand panel contents. Overrides KeepMounted and uses hidden=\"until-found\" to hide the element without removing it from the DOM."),
                new ApiRow("KeepMounted", "bool", "false", "Determines whether closed panels stay in the DOM. Ignored when HiddenUntilFound is used."),
                new ApiRow("ValueChanged", "EventCallback<TValue[]>", "—", "Callback invoked when the value changes, supporting two-way binding."),
                new ApiRow("OnValueChange", "EventCallback<AccordionValueChangeEventArgs<TValue>>", "—", "Callback invoked when an accordion item is expanded or collapsed."),
                new ApiRow("Render", "RenderFragment<RenderProps<AccordionRootState<TValue>>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<AccordionRootState<TValue>, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<AccordionRootState<TValue>, string?>?", "null", "Returns a CSS style based on the component's state."),
            ],
            [
                new ApiRow("data-orientation", "", "", "Indicates the orientation of the accordion: \"vertical\" or \"horizontal\"."),
                new ApiRow("data-disabled", "", "", "Present when the accordion is disabled."),
            ],
            []),
        new("Item",
            "Groups an accordion header with the corresponding panel. Renders a <div> element by default.",
            [
                new ApiRow("Value", "TValue?", "auto", "A unique value identifying this accordion item. A unique id is generated automatically when omitted (string values)."),
                new ApiRow("Disabled", "bool", "false", "Determines whether the component should ignore user interaction."),
                new ApiRow("OnOpenChange", "EventCallback<CollapsibleOpenChangeEventArgs>", "—", "Callback invoked when the panel is opened or closed."),
                new ApiRow("Render", "RenderFragment<RenderProps<AccordionItemState<TValue>>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<AccordionItemState<TValue>, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<AccordionItemState<TValue>, string?>?", "null", "Returns a CSS style based on the component's state."),
            ],
            [
                new ApiRow("data-open", "", "", "Present when the item's panel is open."),
                new ApiRow("data-closed", "", "", "Present when the item's panel is closed."),
                new ApiRow("data-disabled", "", "", "Present when the item is disabled."),
                new ApiRow("data-index", "", "", "The zero-based index of the item."),
                new ApiRow("data-orientation", "", "", "Indicates the orientation of the accordion."),
            ],
            []),
        new("Header",
            "A heading that labels the corresponding panel. Renders an <h3> element by default.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<AccordionHeaderState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<AccordionHeaderState, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<AccordionHeaderState, string?>?", "null", "Returns a CSS style based on the component's state."),
            ],
            [
                new ApiRow("data-open", "", "", "Present when the item's panel is open."),
                new ApiRow("data-closed", "", "", "Present when the item's panel is closed."),
                new ApiRow("data-disabled", "", "", "Present when the item is disabled."),
                new ApiRow("data-index", "", "", "The zero-based index of the item."),
                new ApiRow("data-orientation", "", "", "Indicates the orientation of the accordion."),
            ],
            []),
        new("Trigger",
            "A button that opens and closes the corresponding panel. Renders a <button> element by default.",
            [
                new ApiRow("Disabled", "bool?", "null", "Determines whether the trigger ignores user interaction. Inherits from the parent item when null."),
                new ApiRow("NativeButton", "bool", "true", "Determines whether the component renders a native <button> element."),
                new ApiRow("Render", "RenderFragment<RenderProps<AccordionTriggerState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<AccordionTriggerState, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<AccordionTriggerState, string?>?", "null", "Returns a CSS style based on the component's state."),
            ],
            [
                new ApiRow("data-panel-open", "", "", "Present when the corresponding panel is open."),
                new ApiRow("data-disabled", "", "", "Present when the trigger is disabled."),
                new ApiRow("data-index", "", "", "The zero-based index of the item."),
                new ApiRow("data-orientation", "", "", "Indicates the orientation of the accordion."),
                new ApiRow("data-value", "", "", "The value of the accordion item."),
            ],
            []),
        new("Panel",
            "A collapsible panel with the accordion item contents. Renders a <div> element by default.",
            [
                new ApiRow("KeepMounted", "bool?", "null", "Determines whether the panel stays in the DOM while closed. Inherits from Root when null."),
                new ApiRow("HiddenUntilFound", "bool?", "null", "Allows the browser's built-in page search to find and expand the panel contents. Inherits from Root when null."),
                new ApiRow("Render", "RenderFragment<RenderProps<AccordionPanelState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<AccordionPanelState, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<AccordionPanelState, string?>?", "null", "Returns a CSS style based on the component's state."),
            ],
            [
                new ApiRow("data-open", "", "", "Present when the panel is open."),
                new ApiRow("data-closed", "", "", "Present when the panel is closed."),
                new ApiRow("data-disabled", "", "", "Present when the item is disabled."),
                new ApiRow("data-index", "", "", "The zero-based index of the item."),
                new ApiRow("data-orientation", "", "", "Indicates the orientation of the accordion."),
                new ApiRow("data-starting-style", "", "", "Present when the panel is animating in."),
                new ApiRow("data-ending-style", "", "", "Present when the panel is animating out."),
            ],
            [
                new ApiRow("--accordion-panel-height", "", "", "The measured height of the panel content; used to animate the open and close transitions."),
                new ApiRow("--accordion-panel-width", "", "", "The measured width of the panel content; used for horizontal animations."),
            ]),
    ];
}
```

- [ ] **Step 3: Create AccordionPage**

`Pages/AccordionPage.razor` (the literal route takes precedence over `/components/{Slug}`):

```razor
@page "/components/accordion"

<PageTitle>Accordion · Blazix.BaseUI</PageTitle>

<DocsPageIntro Eyebrow="Components"
               Title="Accordion"
               Subtitle="A set of collapsible panels with headings."
               MarkdownHref="/components/accordion.md" />

<div class="mt-8">
    <Demo Variants="@HeroVariants">
        <AccordionHeroCss />
    </Demo>
</div>

<DocsHeading Title="Anatomy" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Import the component namespace and assemble its parts:
</p>
<CodeBlock Code="@AnatomyCode" />

<DocsHeading Title="Examples" />
<DocsHeading Level="3" Title="Open multiple panels" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    By default only one panel is open at a time. Set <code class="@LiquidGlassClasses.CodeInline">Multiple="true"</code> to let the user open several panels at once.
</p>
<div class="mb-2">
    <Demo Variants="@MultipleVariants">
        <AccordionMultipleCss />
    </Demo>
</div>

<DocsHeading Title="API reference" />
@foreach (var part in AccordionApi.Parts)
{
    <ApiPartReference Part="part" />
}

@code {
    private const string AnatomyCode =
        """
        @using Blazix.BaseUI.Accordion

        <AccordionRoot TValue="string">
            <AccordionItem>
                <AccordionHeader>
                    <AccordionTrigger />
                </AccordionHeader>
                <AccordionPanel />
            </AccordionItem>
        </AccordionRoot>
        """;

    private static readonly IReadOnlyList<DemoVariant> HeroVariants =
    [
        new("CSS",
        [
            new DemoFile("Accordion.razor", "xml", "Components.Demos.Accordion.Hero.Css.AccordionHeroCss.razor"),
            new DemoFile("accordion.css", "css", "wwwroot.demos.accordion.css"),
        ]),
        new("Tailwind",
        [
            new DemoFile("Accordion.razor", "xml", "Components.Demos.Accordion.Hero.Tailwind.AccordionHeroTailwind.razor"),
        ]),
    ];

    private static readonly IReadOnlyList<DemoVariant> MultipleVariants =
    [
        new("CSS",
        [
            new DemoFile("Accordion.razor", "xml", "Components.Demos.Accordion.Multiple.Css.AccordionMultipleCss.razor"),
            new DemoFile("accordion.css", "css", "wwwroot.demos.accordion.css"),
        ]),
        new("Tailwind",
        [
            new DemoFile("Accordion.razor", "xml", "Components.Demos.Accordion.Multiple.Tailwind.AccordionMultipleTailwind.razor"),
        ]),
    ];
}
```

- [ ] **Step 4: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.sln`
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Preview check**

Open `/components/accordion` in the preview:
- Hero demo renders and animates (click a trigger: panel height animates, icon rotates 45°).
- Section order: hero demo → Anatomy → Examples → API reference (Root/Item/Header/Trigger/Panel).
- QuickNav shows "On this page" with Anatomy, Examples, Open multiple panels, API reference, and the five part entries; clicking scrolls; the active item updates on scroll.
- Demo toolbar: variant Select switches CSS ↔ Tailwind; CSS variant shows two file tabs (`Accordion.razor`, `accordion.css`); tab switch swaps code; copy button copies the active file (paste somewhere to verify) and shows the check for ~2 s; "Show code" expands the collapsed block.
- If the embedded resource exception appears, fix `ResourcePath` values per Task 8 Step 6.

- [ ] **Step 6: Commit**

```bash
git add -A docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client
git commit -m "docs: add full Accordion documentation page with demos and API reference"
```

---

### Task 10: Stub pages, ComponentPage rewrite, Quick start home

**Files:**
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Docs/DocStub.razor`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/OverviewPage.razor`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/HandbookPage.razor`
- Create: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/UtilsPage.razor`
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/ComponentPage.razor` (full rewrite)
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/Home.razor` (full rewrite)

- [ ] **Step 1: Create DocStub**

`Components/Docs/DocStub.razor`:

```razor
<PageTitle>@(Link is null ? "Not found · Blazix.BaseUI" : $"{Link.Name} · Blazix.BaseUI")</PageTitle>

@if (Link is null)
{
    <DocsPageIntro Eyebrow="404" Title="Not found" Subtitle="No documentation exists at this address." />
    <p class="mt-6 text-sm leading-6 text-slate-700 dark:text-slate-200">
        Pick a page from the sidebar, or head back to the <a href="/">Quick start</a>.
    </p>
}
else
{
    <DocsPageIntro Eyebrow="@Area" Title="@Link.Name" Subtitle="@Link.Summary" />
    <div class="mt-8">
        <DocsCallout Title="In progress">
            <p class="m-0">This page hasn't been written yet — documentation is on the way. The component itself ships in the current Blazix.BaseUI package.</p>
        </DocsCallout>
    </div>
}

@code {
    [Parameter]
    public DocsNavLink? Link { get; set; }

    [Parameter, EditorRequired]
    public string Area { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Rewrite ComponentPage as the stub fallback**

`Pages/ComponentPage.razor` (full replacement — drops `ComponentCatalog`, `ComponentDemo`, `AccordionDocs` usage):

```razor
@page "/components/{Slug}"

<DocStub Area="Components" Link="@link" />

@code {
    [Parameter]
    public string? Slug { get; set; }

    private DocsNavLink? link;

    protected override void OnParametersSet()
    {
        link = DocsNav.Find("components", Slug);
    }
}
```

- [ ] **Step 3: Create the Overview, Handbook, and Utils stub routes**

`Pages/OverviewPage.razor`:

```razor
@page "/overview/{Slug}"

<DocStub Area="Overview" Link="@link" />

@code {
    [Parameter]
    public string? Slug { get; set; }

    private DocsNavLink? link;

    protected override void OnParametersSet()
    {
        // Quick start lives at "/", so /overview/quick-start is not a valid address.
        var found = DocsNav.Find("overview", Slug);
        link = found is { Slug: "quick-start" } ? null : found;
    }
}
```

`Pages/HandbookPage.razor`:

```razor
@page "/handbook/{Slug}"

<DocStub Area="Handbook" Link="@link" />

@code {
    [Parameter]
    public string? Slug { get; set; }

    private DocsNavLink? link;

    protected override void OnParametersSet()
    {
        link = DocsNav.Find("handbook", Slug);
    }
}
```

`Pages/UtilsPage.razor`:

```razor
@page "/utils/{Slug}"

<DocStub Area="Utils" Link="@link" />

@code {
    [Parameter]
    public string? Slug { get; set; }

    private DocsNavLink? link;

    protected override void OnParametersSet()
    {
        link = DocsNav.Find("utils", Slug);
    }
}
```

- [ ] **Step 4: Rewrite Home as Quick start**

`Pages/Home.razor` (full replacement):

```razor
@page "/"

<PageTitle>Quick start · Blazix.BaseUI</PageTitle>

<DocsPageIntro Eyebrow="Overview"
               Title="Quick start"
               Subtitle="A quick guide to getting started with Blazix.BaseUI." />

<DocsHeading Title="Install the package" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Add the package to your Blazor project:
</p>
<CodeBlock Code="dotnet add package Blazix.BaseUI" Language="bash" />

<DocsHeading Title="Import the namespaces" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Each component family lives in its own namespace. Add the ones you use to <code class="@LiquidGlassClasses.CodeInline">_Imports.razor</code>:
</p>
<CodeBlock Code="@ImportsCode" />

<DocsHeading Title="Assemble a component" />
<p class="mb-3 max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Components are unstyled parts that you compose and style yourself — with plain CSS classes through
    <code class="@LiquidGlassClasses.CodeInline">class</code>, or state-driven classes through
    <code class="@LiquidGlassClasses.CodeInline">ClassValue</code>:
</p>
<CodeBlock Code="@ExampleCode" />

<DocsHeading Title="Explore the components" />
<p class="max-w-3xl text-sm leading-6 text-slate-700 dark:text-slate-200">
    Every component is listed in the sidebar. Documented pages show a live demo with CSS and Tailwind source,
    anatomy, and a full API reference — <a href="/components/accordion">start with the Accordion</a>.
    Each documented component also serves its markdown source at
    <code class="@LiquidGlassClasses.CodeInline">/components/name.md</code>.
</p>

@code {
    private const string ImportsCode =
        """
        @using Blazix.BaseUI.Accordion
        @using Blazix.BaseUI.Switch
        @using Blazix.BaseUI.Tooltip
        """;

    private const string ExampleCode =
        """
        <SwitchRoot @bind-Checked="enabled" class="switch">
            <SwitchThumb class="thumb" />
        </SwitchRoot>

        @code {
            private bool enabled;
        }
        """;
}
```

- [ ] **Step 5: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.sln`
Expected: Build succeeded, 0 errors. (`AccordionDocs.razor`, `ComponentDemo.razor`, and `ComponentCatalog` still exist but are now unreferenced by pages — deleted next task.)

- [ ] **Step 6: Preview check**

- `/` shows Quick start with TOC entries in QuickNav.
- `/components/switch` shows the stub (name, summary, "In progress" callout).
- `/handbook/styling`, `/overview/about`, `/utils/portal` show stubs.
- `/components/accordion` still shows the full page (literal route wins).
- `/components/nonexistent` shows the Not found stub.

- [ ] **Step 7: Commit**

```bash
git add -A docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client
git commit -m "docs: add stub pages for all nav areas and Quick start home"
```

---

### Task 11: Update accordion.md

**Files:**
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Components/accordion.md` (full rewrite)

- [ ] **Step 1: Rewrite accordion.md to mirror the page**

Full replacement:

````markdown
# Accordion

A set of collapsible panels with headings.

Rendered docs: `/components/accordion`

## Anatomy

```razor
@using Blazix.BaseUI.Accordion

<AccordionRoot TValue="string">
    <AccordionItem>
        <AccordionHeader>
            <AccordionTrigger />
        </AccordionHeader>
        <AccordionPanel />
    </AccordionItem>
</AccordionRoot>
```

## Examples

### Open multiple panels

By default only one panel is open at a time. Set `Multiple="true"` to let the user open several panels at once.

```razor
<AccordionRoot TValue="string" Multiple="true">
    ...
</AccordionRoot>
```

## API reference

### Root

Groups all parts of the accordion. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `TValue[]?` | `null` | The controlled value of the item(s) that should be expanded. To render an uncontrolled accordion, use `DefaultValue` instead. |
| `DefaultValue` | `TValue[]` | `[]` | The uncontrolled value of the item(s) that should be initially expanded. |
| `Disabled` | `bool` | `false` | Determines whether the component should ignore user interaction. |
| `Multiple` | `bool` | `false` | Determines whether multiple items can be open at the same time. |
| `Orientation` | `Orientation` | `Vertical` | The visual orientation of the accordion. Controls arrow-key direction for roving focus. |
| `LoopFocus` | `bool` | `true` | Determines whether focus loops back to the first item when the end is reached with arrow keys. |
| `HiddenUntilFound` | `bool` | `false` | Allows the browser's built-in page search to find and expand panel contents. Overrides `KeepMounted`. |
| `KeepMounted` | `bool` | `false` | Determines whether closed panels stay in the DOM. Ignored when `HiddenUntilFound` is used. |
| `ValueChanged` | `EventCallback<TValue[]>` | — | Callback invoked when the value changes, supporting two-way binding. |
| `OnValueChange` | `EventCallback<AccordionValueChangeEventArgs<TValue>>` | — | Callback invoked when an item is expanded or collapsed. |
| `Render` | `RenderFragment<RenderProps<AccordionRootState<TValue>>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AccordionRootState<TValue>, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AccordionRootState<TValue>, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-orientation`, `data-disabled`.

### Item

Groups an accordion header with the corresponding panel. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `TValue?` | auto | A unique value identifying this item. Generated automatically when omitted (string values). |
| `Disabled` | `bool` | `false` | Determines whether the component should ignore user interaction. |
| `OnOpenChange` | `EventCallback<CollapsibleOpenChangeEventArgs>` | — | Callback invoked when the panel is opened or closed. |
| `Render` | `RenderFragment<RenderProps<AccordionItemState<TValue>>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AccordionItemState<TValue>, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AccordionItemState<TValue>, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-open`, `data-closed`, `data-disabled`, `data-index`, `data-orientation`.

### Header

A heading that labels the corresponding panel. Renders an `<h3>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<AccordionHeaderState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AccordionHeaderState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AccordionHeaderState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-open`, `data-closed`, `data-disabled`, `data-index`, `data-orientation`.

### Trigger

A button that opens and closes the corresponding panel. Renders a `<button>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool?` | `null` | Determines whether the trigger ignores user interaction. Inherits from the parent item when `null`. |
| `NativeButton` | `bool` | `true` | Determines whether the component renders a native `<button>` element. |
| `Render` | `RenderFragment<RenderProps<AccordionTriggerState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AccordionTriggerState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AccordionTriggerState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-panel-open`, `data-disabled`, `data-index`, `data-orientation`, `data-value`.

### Panel

A collapsible panel with the accordion item contents. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `KeepMounted` | `bool?` | `null` | Determines whether the panel stays in the DOM while closed. Inherits from Root when `null`. |
| `HiddenUntilFound` | `bool?` | `null` | Allows the browser's built-in page search to find and expand the panel contents. Inherits from Root when `null`. |
| `Render` | `RenderFragment<RenderProps<AccordionPanelState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AccordionPanelState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AccordionPanelState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-open`, `data-closed`, `data-disabled`, `data-index`, `data-orientation`, `data-starting-style`, `data-ending-style`.

CSS variables: `--accordion-panel-height`, `--accordion-panel-width`.
````

- [ ] **Step 2: Verify the markdown route**

With the preview running, fetch `/components/accordion.md` and confirm it returns the new content as `text/markdown`.

- [ ] **Step 3: Commit**

```bash
git add docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Components/accordion.md
git commit -m "docs: align accordion.md with the new docs page"
```

---

### Task 12: Cleanup of orphaned files

**Files:**
- Delete: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Data/ComponentCatalog.cs`
- Delete: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Data/ComponentDoc.cs`
- Delete: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Docs/ComponentDemo.razor`
- Delete: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/ComponentDocs/AccordionDocs.razor`
- Delete: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Docs/DocsWindow.razor`
- Delete: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Docs/DocsSection.razor`
- Modify: `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/_Imports.razor`

These deletions are all orphans created by this overhaul (every former consumer was rewritten in Tasks 5–10). `DocsCallout.razor` stays (used by `DocStub`). The 8 legacy `Content/Components/*.md` files for undocumented components stay (harmless, still served).

- [ ] **Step 1: Confirm nothing references the files, then delete**

```bash
cd docs/Blazix.BaseUI.Docs
grep -rn "ComponentCatalog\|ComponentDemo\|AccordionDocs\|DocsWindow\|DocsSection" --include="*.razor" --include="*.cs" . | grep -v obj | grep -v bin
```

Expected: matches only inside the files being deleted. Then:

```bash
rm Blazix.BaseUI.Docs.Client/Data/ComponentCatalog.cs \
   Blazix.BaseUI.Docs.Client/Data/ComponentDoc.cs \
   Blazix.BaseUI.Docs.Client/Components/Docs/ComponentDemo.razor \
   Blazix.BaseUI.Docs.Client/Components/ComponentDocs/AccordionDocs.razor \
   Blazix.BaseUI.Docs.Client/Components/Docs/DocsWindow.razor \
   Blazix.BaseUI.Docs.Client/Components/Docs/DocsSection.razor
rmdir Blazix.BaseUI.Docs.Client/Components/ComponentDocs
```

- [ ] **Step 2: Remove the dead namespace from `_Imports.razor`**

Delete this line from `Blazix.BaseUI.Docs.Client/_Imports.razor`:

```razor
@using Blazix.BaseUI.Docs.Client.Components.ComponentDocs
```

Also remove component-namespace usings that are now unused only if nothing references them (`Blazix.BaseUI.AlertDialog`, `.Autocomplete`, `.Avatar`, `.Checkbox`, `.CheckboxGroup` were used by the deleted `ComponentDemo.razor` — grep before removing; keep `.Accordion`, `.Button`, `.Collapsible`, `.Switch` and everything added in this overhaul).

- [ ] **Step 3: Build**

Run: `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.sln`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add -A docs/Blazix.BaseUI.Docs
git commit -m "docs: remove components orphaned by the docs overhaul"
```

---

### Task 13: Final verification (spec §7 acceptance)

- [ ] **Step 1: Full build**

```bash
dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.sln
dotnet build Blazix.BaseUI.slnx
```

Expected: both succeed with 0 errors.

- [ ] **Step 2: Preview acceptance run-through**

Using the preview tools against the running docs site, verify every item; capture a screenshot of the Accordion page and the mobile drawer:

1. Sidebar shows 4 sections; Components lists all 35 in spec order (spot-check first/last: Accordion … Tooltip).
2. `/components/accordion` section order: hero demo → Anatomy → Examples → API reference (Root, Item, Header, Trigger, Panel — each with Parameters and Data attributes; Panel also CSS variables).
3. Demo: variant switch swaps code; CSS variant shows two file tabs; copy button writes the active file to the clipboard (verify via `preview_eval` reading `navigator.clipboard.readText()` or paste) and shows check feedback; collapsed code expands and collapses.
4. Stub pages render for undocumented links; `/components/accordion.md` serves markdown.
5. Mobile width (~390px): Drawer nav opens/closes, quick-nav rail hidden.
6. Dark mode switch toggles and persists across reload (`blazix-docs-theme` in localStorage).
7. Runtime switch: default is Server; selecting WASM sets the cookie, reloads, and the switch shows WASM after interactivity (verify `document.cookie` contains `blazix-docs-render-mode=wasm`); switching back works; selection persists across navigation.
8. `prefers-reduced-motion`: emulate via preview and confirm panel/icon transitions are disabled.

- [ ] **Step 3: Fix anything found, re-run the failing check, then commit**

```bash
git add -A docs
git commit -m "docs: final polish from acceptance run-through"
```

(Skip the commit if no changes were needed.)
