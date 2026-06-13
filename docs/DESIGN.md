# Blazix.BaseUI Docs Design

## Direction

The docs app uses an Apple Liquid Glass-inspired material system: floating translucent navigation and command surfaces, soft refraction, adaptive color from the content layer, and motion that feels light, responsive, and physical. Content stays readable and mostly opaque; glass is reserved for navigation, controls, cards, and transient surfaces.

## Rules

- Use Tailwind utility classes only for app UI styling.
- Color alpha modifiers must be multiples of 5 (`bg-white/70`, not `bg-white/72`) — the Tailwind Play CDN silently drops other values, leaving the element unstyled.
- Use large concentric radii. Avoid squared-off chrome. Tightly nested corners stay concentric: inner radius = outer radius minus the inset.
- Use light translucent surfaces, white borders, layered shadows, `backdrop-blur`, and restrained cyan/sky tinting.
- Glass (`liquid-glass` / `backdrop-filter`) is reserved for chrome (header, nav, window shells) and transient surfaces (dialogs, popups, scrims). Everything inside a glass shell is content layer: opaque fills only, never a nested `backdrop-blur`.
- Animate glass with ambient background movement, materialization, hover lift on cards, and pressed gel feedback on controls. The sheen runs only as a hover one-shot; the ambient wallpaper is the only persistent animation. Window shells do not lift on hover.
- The liquid prism pair lives in `App.razor`: `#liquid-prism-intense` (single displacement pass, scale 200) and `#liquid-prism-intense-2` (two-stage displacement chain, scale 30 then 20). On real controls apply a prism as a `backdrop-filter` so it refracts the content *behind* the glass — applying it as an element `filter` warps the element's own pixels, including any text, which mangles small controls. The demo code expander uses `#liquid-prism-intense` as a constant resting backdrop; hover only scales/lightens (no filter swap) and `liquid-wobble` adds the gel press animation. Reserve the trio for playful one-off controls; elements using them apply border radius inline (e.g. `rounded-full`), not via shared panel classes. Everything falls back to the plain control (Chromium-only for SVG backdrop filters) and stays calm under `prefers-reduced-motion`. `wwwroot/liquid-glass-preview.html` compares the two filters side by side and deliberately shows Variant B as an element `filter` to demonstrate that mangling.
- Respect `prefers-reduced-motion`, `prefers-reduced-transparency`, and `prefers-contrast` (glass falls back to near-opaque fills in both themes).
- Do not stack glass on glass where it muddies hierarchy; use more opaque content panels inside glass shells.
- Prefer dense documentation screens over marketing pages.
- Put reusable UI frame pieces in `Blazix.BaseUI.Docs.Client/Components/Docs`.
- Put shared design classes in `Blazix.BaseUI.Docs.Client/Data/LiquidGlassClasses.cs`.
- Put navigation metadata in `Blazix.BaseUI.Docs.Client/Data/DocsNav.cs`.
- Every documented component should have `/components/{slug}` and `/components/{slug}.md`.
- Do not commit `*.log` files.

## Component Page Shape

Each documented component page mirrors the Base UI section order: intro (eyebrow, title, subtitle), hero demo, anatomy, examples, then a per-part API reference (parameters, data attributes, CSS variables). Demos are real Blazix.BaseUI components in two source variants — CSS (a shared stylesheet under `wwwroot/demos/`) and Tailwind (inline `class` utilities) — with their source files embedded as resources so the showcased code never drifts. Undocumented pages render the shared stub.

## Markdown

Markdown files live in `Blazix.BaseUI.Docs/Content/Components`. The server maps them to `/components/{slug}.md` so the docs can mirror the Base UI route pattern.
