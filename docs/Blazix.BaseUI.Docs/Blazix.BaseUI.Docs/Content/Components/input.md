# Input

A native input element that automatically works with Field.

Rendered docs: `/components/input`

## Usage guidelines

- Every input needs an accessible name from a native `label` element or the Field component.
- Use Field when you want validation state, dirty/touched tracking, and data attributes coordinated with labels and errors.

## Anatomy

```razor
@using Blazix.BaseUI.Input

<Input />
```

## API reference

### Input

A native input element that automatically works with Field. Renders an `<input>` element.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `string?` | `null` | The controlled value of the input. |
| `ValueChanged` | `EventCallback<string>` | — | Callback fired when the input value changes, supporting two-way binding. |
| `ValueExpression` | `Expression<Func<string>>?` | `null` | Identifies the bound value for Blazor forms and validation. |
| `DefaultValue` | `string?` | `null` | The initial value when the input is uncontrolled. |
| `DisplayName` | `string?` | `null` | The display name used for validation messages. |
| `Name` | `string?` | `null` | The name attribute of the input element. |
| `Disabled` | `bool` | `false` | Whether the input should ignore user interaction. |
| `Render` | `RenderFragment<RenderProps<InputState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<InputState, string>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<InputState, string>?` | `null` | Returns a CSS style based on state. |

| Data attribute | Description |
| --- | --- |
| `data-disabled` | Present when the input is disabled. |
| `data-valid` | Present when the input is valid inside a FieldRoot. |
| `data-invalid` | Present when the input is invalid inside a FieldRoot. |
| `data-dirty` | Present when the input value has changed inside a FieldRoot. |
| `data-touched` | Present when the input has been focused and blurred inside a FieldRoot. |
| `data-filled` | Present when the input has a value. |
| `data-focused` | Present when the input is focused. |
