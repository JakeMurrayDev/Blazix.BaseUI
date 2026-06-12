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
- `liquid-distort` adds refraction (SVG displacement filter `#blx-liquid-distortion` on hover) and a gel wobble on press; reserve it for playful one-off controls like the demo code expander. It falls back to the plain control under `prefers-reduced-motion`.
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
