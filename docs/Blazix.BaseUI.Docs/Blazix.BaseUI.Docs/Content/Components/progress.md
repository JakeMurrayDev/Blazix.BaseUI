# Progress

Displays the status of a task that takes a long time.

Rendered docs: `/components/progress`

## Anatomy

```razor
@using Blazix.BaseUI.Progress

<ProgressRoot Value="20">
    <ProgressLabel />
    <ProgressTrack>
        <ProgressIndicator />
    </ProgressTrack>
    <ProgressValue />
</ProgressRoot>
```

## API reference

### Root

Owns the progress value, accessible progressbar attributes, label registration, formatted value, and status.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `double?` | `null` | Current progress value; null is indeterminate. |
| `Min` | `double` | `0` | Minimum range value. |
| `Max` | `double` | `100` | Maximum range value. |
| `Format` | `NumberFormatOptions?` | `null` | Number format options. |
| `FormatString` | `string?` | `null` | .NET numeric format string. |
| `Locale` | `string?` | `null` | Locale used for formatting. |
| `FormatProvider` | `IFormatProvider?` | `null` | .NET format provider. |
| `GetAriaValueText` | `Func<string?, double?, string>?` | `null` | Custom `aria-valuetext`. |
| `Render` | `RenderFragment<RenderProps<ProgressRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<ProgressRootState, string?>?` | `null` | Returns CSS classes based on status. |
| `StyleValue` | `Func<ProgressRootState, string?>?` | `null` | Returns inline styles based on status. |
| `ChildContent` | `RenderFragment?` | `null` | The Progress parts. |

### Label

Registers an id with the root and labels the progressbar. Supports `Render`, `ClassValue`, `StyleValue`, and `ChildContent`.

### Track

Contains the indicator. Supports `Render`, `ClassValue`, `StyleValue`, and `ChildContent`.

### Indicator

Visualizes determinate progress with a computed width. Supports `Render`, `ClassValue`, `StyleValue`, and `ChildContent`.

### Value

Displays the formatted value or invokes `ChildContent` as `Func<string?, double?, RenderFragment>`.

All Progress parts expose exactly one of `data-progressing`, `data-complete`, or `data-indeterminate`.
