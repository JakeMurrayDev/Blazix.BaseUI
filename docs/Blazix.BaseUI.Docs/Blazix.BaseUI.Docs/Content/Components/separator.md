# Separator

A separator element accessible to screen readers.

Rendered docs: `/components/separator`

## Anatomy

```razor
@using Blazix.BaseUI.Separator

<Separator />
```

## API reference

### Separator

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Orientation` | `Orientation` | `Orientation.Horizontal` | The separator orientation. |
| `Render` | `RenderFragment<RenderProps<SeparatorState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<SeparatorState, string>?` | `null` | Returns CSS classes based on separator state. |
| `StyleValue` | `Func<SeparatorState, string>?` | `null` | Returns inline styles based on separator state. |
| `ChildContent` | `RenderFragment?` | `null` | Optional child content. |

Separator renders `role="separator"`, `aria-orientation`, and `data-orientation`.
