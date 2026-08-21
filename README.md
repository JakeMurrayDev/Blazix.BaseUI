# Blazix.BaseUI

An unofficial Blazor port of the [Base UI](https://base-ui.com) headless component library.

Blazix.BaseUI gives you unstyled, accessible component primitives — the behavior, keyboard
interaction, focus management, and ARIA wiring — with no opinion about how they look. You compose
the parts and bring your own CSS.

> Not affiliated with or endorsed by the Base UI team.

## Requirements

**.NET 10** or later. The library targets `net10.0` and works in both Blazor Server and Blazor
WebAssembly.

## Install

```bash
dotnet add package Blazix.BaseUI
```

Each component family lives in its own namespace. Add the ones you use to `_Imports.razor`:

```razor
@using Blazix.BaseUI.Accordion
@using Blazix.BaseUI.Switch
@using Blazix.BaseUI.Tooltip
```

No JavaScript setup and no stylesheet reference are needed. Components import their own JS modules
on demand from the package's static web assets.

## Usage

Components are unstyled parts you compose and style yourself — with plain CSS classes through
`class`, or state-driven classes through `ClassValue`:

```razor
<SwitchRoot @bind-Checked="enabled" class="switch">
    <SwitchThumb class="thumb" />
</SwitchRoot>

@code {
    private bool enabled;
}
```

Every part also exposes `data-*` attributes reflecting its state, so you can style entirely from
CSS:

```css
.switch[data-checked] {
    background-color: rebeccapurple;
}
```

## Components

Accordion · Alert Dialog · Autocomplete · Avatar · Button · Checkbox · Checkbox Group ·
Collapsible · Combobox · Context Menu · Dialog · Drawer · Field · Fieldset · Form · Input ·
Menu · Menubar · Meter · Navigation Menu · Number Field · OTP Field · Popover · Preview Card ·
Progress · Radio · Scroll Area · Select · Separator · Slider · Switch · Tabs · Toast · Toggle ·
Toggle Group · Toolbar · Tooltip

Plus utilities: CSP Provider, Direction Provider, Portal, and Render Element.

## Status

Early development, and versioned accordingly — this is a `0.x` preview. The public API may change
between releases. See
[CHANGELOG.md](https://github.com/JakeMurrayDev/Blazix.BaseUI/blob/master/CHANGELOG.md).

## Links

- [Source and issues](https://github.com/JakeMurrayDev/Blazix.BaseUI)
- [Contributing and build instructions](https://github.com/JakeMurrayDev/Blazix.BaseUI/blob/master/AGENTS.md)

## License

MIT. See [LICENSE.txt](https://github.com/JakeMurrayDev/Blazix.BaseUI/blob/master/LICENSE.txt).
