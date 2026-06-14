# Meter

A graphical display of a numeric value within a range.

Rendered docs: `/components/meter`

## Anatomy

```razor
@using Blazix.BaseUI.Meter

<MeterRoot Value="24">
    <MeterLabel>Storage Used</MeterLabel>
    <MeterValue />
    <MeterTrack>
        <MeterIndicator />
    </MeterTrack>
</MeterRoot>
```

## API reference

### Root

Groups all parts of the meter and provides the value for screen readers. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `double` | — | The current value. |
| `Min` | `double` | `0` | The minimum value in the range. |
| `Max` | `double` | `100` | The maximum value in the range. |
| `Format` | `NumberFormatOptions?` | `null` | Options used to format the displayed value. |
| `FormatString` | `string?` | `null` | A .NET numeric format string used to display the value. |
| `Locale` | `string?` | `null` | The locale used when formatting the value. When omitted, the current culture is used. |
| `FormatProvider` | `IFormatProvider?` | `null` | The .NET format provider used when `FormatString` is supplied. |
| `GetAriaValueText` | `Func<string, double, string>?` | `null` | Returns a human-readable `aria-valuetext` from the formatted value and raw value. |
| `Render` | `RenderFragment<RenderProps<MeterRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<MeterRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<MeterRootState, string?>?` | `null` | Returns a CSS style based on state. |

### Label

An accessible label for the meter. Renders a `<span>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<MeterRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<MeterRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<MeterRootState, string?>?` | `null` | Returns a CSS style based on state. |

### Track

Contains the meter indicator and represents the entire range. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<MeterRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<MeterRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<MeterRootState, string?>?` | `null` | Returns a CSS style based on state. |

### Indicator

Visualizes the position of the value along the range. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<MeterRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<MeterRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<MeterRootState, string?>?` | `null` | Returns a CSS style based on state. The component appends its width and inset styles. |

### Value

A text element displaying the current formatted value. Renders a `<span>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `ChildContent` | `Func<string, double, RenderFragment>?` | `null` | Custom value renderer that receives the formatted value and raw value. |
| `Render` | `RenderFragment<RenderProps<MeterRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<MeterRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<MeterRootState, string?>?` | `null` | Returns a CSS style based on state. |
