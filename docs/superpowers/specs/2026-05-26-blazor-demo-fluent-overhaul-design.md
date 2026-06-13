# Blazor Demo Fluent Overhaul Design

## Goal

Overhaul the BlazorBaseUI demo so it feels like a Microsoft Fluent 2 engineering console: token-driven, accessible, compact, and useful for inspecting component behavior in both light and dark mode.

## Approved Direction

The approved mockup direction is the Fluent engineering console. The demo uses a calm Microsoft product surface rather than a marketing page. It keeps the current component examples, but presents them inside a more coherent documentation shell with a command bar, compact navigation, theme controls, render-mode controls, and Fluent-styled example cards.

## Design Principles

- Use Fluent 2 ideas: semantic design tokens, Segoe UI typography, neutral surfaces, restrained brand blue accents, clear interaction states, small radius values, and purposeful elevation.
- Make light and dark mode first-class. Theme values must be semantic CSS custom properties instead of one-off Tailwind color classes.
- Keep the demo app work-focused and scannable. Avoid decorative hero sections, oversized typography, nested cards, or marketing composition.
- Use BlazorBaseUI primitives in the demo chrome where they fit naturally, especially `Button`, `SwitchRoot`, `TabsRoot`, and `NavigationMenu`.
- Preserve current component behavior examples unless a localized rewrite is needed to make the page fit the new shell.

## App Structure

The server project remains the Blazor Web App host. Component example implementations stay in the client project under `Shared/Sections`, with server and WebAssembly route wrappers continuing to point at the same shared section components.

The redesign focuses on shared surfaces first:

- `MainLayout.razor` becomes the Fluent shell: command bar, nav rail, mobile overlay, theme state, and cascading theme context.
- `NavMenu.razor` becomes a grouped component catalog with component availability reflected in link styling.
- `ComponentShowcase.razor` becomes the page header and render-mode control surface.
- `DemoSection.razor` becomes the reusable Fluent example card with preview, optional notes, and optional code disclosure.
- `Home.razor` becomes the demo overview and component catalog landing page.
- `app.css` holds semantic tokens and reusable demo classes that can override older utility classes across pages without rewriting every example.

## Theme Behavior

The app starts in light mode. The shell exposes a light/dark switch using `SwitchRoot`; the selected theme is applied through a `data-theme` attribute on the shell root. CSS variables under `[data-theme="light"]` and `[data-theme="dark"]` define surfaces, text, strokes, brand colors, status colors, shadow, focus, and code colors.

Theme state lives in `MainLayout.razor` for the initial implementation. If later pages need to read it, a small demo theme context can be added, but the first pass only needs CSS inheritance.

## Navigation

Navigation is a fixed desktop rail and slide-in mobile panel. The rail groups routes into Overview, Inputs, Overlays, Navigation, Feedback, and Utilities. Links for routes that do not exist yet remain visible but disabled-looking so the full Base UI surface is still discoverable without creating broken navigation.

The top command bar contains the brand lockup, a search-looking field for visual orientation, the render version chip, and the theme switch.

## Component Pages

Each component page uses `ComponentShowcase` for:

- Component name and description.
- Render-mode switch between InteractiveServer and InteractiveWebAssembly using `TabsRoot`.
- Metadata chips such as primitive type and render mode.
- A consistent content area for sections.

Each example section uses `DemoSection` for:

- Title and description.
- Fluent surface and preview area.
- Optional code disclosure using `Collapsible` if available or native `details` if the component is not worth coupling to the disclosure behavior.
- Consistent light/dark styling for nested examples.

Existing examples can continue to use Tailwind utility classes initially. Global demo CSS should map common old classes and component data attributes into the new token system where possible, then high-traffic pages can be refined individually.

## BlazorBaseUI Usage

The demo chrome must use BlazorBaseUI components, not only HTML controls:

- `Button` for command-bar icon buttons, mobile menu toggle, and prominent home actions.
- `SwitchRoot` and `SwitchThumb` for light/dark mode.
- `TabsRoot`, `TabsList`, `TabsTab`, and related tabs primitives for render-mode selection.
- `NavigationMenu` can be used for desktop catalog grouping if it fits without adding fragile hover behavior to the side rail; otherwise keep route navigation as `NavLink` and use BlazorBaseUI primitives elsewhere.

## Testing And Verification

Verification must include:

- `dotnet build BlazorBaseUI.slnx`.
- Focused demo project build if full solution build is slow or fails for unrelated tests.
- Run the demo app locally.
- Browser verification at desktop and mobile widths.
- Confirm light/dark toggling changes the shell and component previews.
- Confirm server/WASM render-mode navigation still works on at least one component page.
- Confirm core pages render without text overlap or broken navigation.

## Out Of Scope

- Rewriting every component example's internal markup in one pass.
- Adding real search indexing.
- Adding persistent theme storage unless it is trivial after the shell works.
- Changing library component behavior or public APIs.
