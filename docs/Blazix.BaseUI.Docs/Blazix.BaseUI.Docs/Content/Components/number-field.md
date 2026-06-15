# Number Field

A numeric input element with increment and decrement buttons, and a scrub area.

Rendered docs: `/components/number-field`

## Usage guidelines

- Form controls must have an accessible name from a native `label` element or the Field component.
- Use `DefaultValue` for uncontrolled fields and `Value` with `ValueChanged` for controlled fields.
- `AllowOutOfRange` only affects direct text input; buttons, keyboard steps, wheel, and scrub interactions still clamp to the configured range.

## Anatomy

```razor
@using Blazix.BaseUI.NumberField

<NumberFieldRoot>
    <NumberFieldScrubArea>
        <NumberFieldScrubAreaCursor />
    </NumberFieldScrubArea>
    <NumberFieldGroup>
        <NumberFieldDecrement />
        <NumberFieldInput />
        <NumberFieldIncrement />
    </NumberFieldGroup>
</NumberFieldRoot>
```

## API reference

### Root

Groups all parts of the number field, manages numeric value state, and renders a hidden native number input for forms. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Id` | `string?` | `null` | The id assigned to the visible input element. |
| `Min` | `double?` | `null` | The minimum allowed value. |
| `Max` | `double?` | `null` | The maximum allowed value. |
| `SmallStep` | `double` | `0.1` | The step amount used when incrementing with the Alt key held. |
| `Step` | `object?` | `1d` | The normal step amount. The string `"any"` preserves native form step behavior while interactions use the default step. |
| `LargeStep` | `double` | `10` | The step amount used when incrementing with the Shift key held. |
| `Required` | `bool` | `false` | Determines whether the user must enter a value before form submission. |
| `Disabled` | `bool` | `false` | Determines whether the field ignores user interaction. |
| `ReadOnly` | `bool` | `false` | Determines whether the field displays its value but cannot be changed. |
| `Name` | `string?` | `null` | The field name submitted by the hidden native input. |
| `Form` | `string?` | `null` | The id of an external form associated with the hidden native input. |
| `Value` | `double?` | `null` | The controlled numeric value. To render an uncontrolled field, use `DefaultValue` instead. |
| `DefaultValue` | `double?` | `null` | The initial uncontrolled numeric value. |
| `ValueChanged` | `EventCallback<double?>` | — | Callback invoked when the numeric value changes, supporting two-way binding. |
| `OnValueChange` | `EventCallback<NumberFieldValueChangeEventArgs>` | — | Callback fired when the numeric value changes. The event can be canceled. |
| `OnValueCommitted` | `EventCallback<NumberFieldValueCommittedEventArgs>` | — | Callback fired when an interaction commits a value, such as blur, scrub end, wheel, or button press. |
| `AllowWheelScrub` | `bool` | `false` | Allows mouse wheel scrubbing while the input is focused and hovered. |
| `AllowOutOfRange` | `bool` | `false` | Allows direct text input to exceed `Min` and `Max`. Step interactions still clamp. |
| `SnapOnStep` | `bool` | `false` | Snaps step interactions to the nearest configured step. |
| `Format` | `NumberFormatOptions?` | `null` | Options used to format and parse the input value. |
| `Locale` | `string?` | `null` | The locale used for number formatting and parsing. When omitted, the runtime locale is used. |
| `Render` | `RenderFragment<RenderProps<NumberFieldRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NumberFieldRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NumberFieldRootState, string?>?` | `null` | Returns a CSS style based on state. |
| `ChildContent` | `RenderFragment?` | `null` | The number field parts to render. |

### ScrubArea

An optional area that lets pointer dragging adjust the value. Renders a `<span>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Direction` | `ScrubDirection` | `Horizontal` | The pointer movement direction used for scrubbing. |
| `PixelSensitivity` | `int` | `2` | How many pixels the pointer must move before the value changes. |
| `TeleportDistance` | `int?` | `null` | The distance the cursor may move from the scrub area center before looping back. |
| `Render` | `RenderFragment<RenderProps<NumberFieldRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NumberFieldRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NumberFieldRootState, string?>?` | `null` | Returns a CSS style based on state. The component appends touch-action and user-select styles. |
| `ChildContent` | `RenderFragment?` | `null` | The scrub label and optional cursor. |

### ScrubAreaCursor

A floating visual cursor rendered only during mouse scrubbing when pointer lock is available. Renders a `<span>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<NumberFieldRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NumberFieldRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NumberFieldRootState, string?>?` | `null` | Returns a CSS style based on state. The component appends fixed positioning and pointer-events styles. |
| `ChildContent` | `RenderFragment?` | `null` | The cursor contents. |

### Group

Groups the decrement button, input, and increment button. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<NumberFieldRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NumberFieldRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NumberFieldRootState, string?>?` | `null` | Returns a CSS style based on state. |
| `ChildContent` | `RenderFragment?` | `null` | The grouped controls. |

### Decrement

A button that decreases the value. Renders a `<button>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Determines whether the decrement button ignores user interaction. |
| `NativeButton` | `bool` | `true` | Renders the component as a native `<button>` when replacing it through `Render`. |
| `Render` | `RenderFragment<RenderProps<NumberFieldRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NumberFieldRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NumberFieldRootState, string?>?` | `null` | Returns a CSS style based on state. The component appends user-select styles. |
| `ChildContent` | `RenderFragment?` | `null` | The button contents. |

### Input

The visible text input for editing the number. Renders an `<input>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `AriaRoledescription` | `string` | `"Number field"` | A user-friendly description for the input role. |
| `Render` | `RenderFragment<RenderProps<NumberFieldRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NumberFieldRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NumberFieldRootState, string?>?` | `null` | Returns a CSS style based on state. |
| `ChildContent` | `RenderFragment?` | `null` | Optional input contents when a custom render target supports children. |

### Increment

A button that increases the value. Renders a `<button>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Determines whether the increment button ignores user interaction. |
| `NativeButton` | `bool` | `true` | Renders the component as a native `<button>` when replacing it through `Render`. |
| `Render` | `RenderFragment<RenderProps<NumberFieldRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NumberFieldRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NumberFieldRootState, string?>?` | `null` | Returns a CSS style based on state. The component appends user-select styles. |
| `ChildContent` | `RenderFragment?` | `null` | The button contents. |

All Number Field parts expose `data-scrubbing`, `data-disabled`, `data-readonly`, `data-required`, and Field-context state attributes: `data-valid`, `data-invalid`, `data-touched`, `data-dirty`, `data-filled`, and `data-focused`.
