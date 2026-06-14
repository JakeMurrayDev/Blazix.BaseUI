# Button

A button component that can be rendered as another tag or focusable when disabled.

Rendered docs: `/components/button`

## Usage guidelines

- Unlike the native button element, `type="submit"` must be specified on `Button` for it to act as a submit button.
- The Button component enforces button semantics. Style links directly when navigation needs to look like a button.

## Anatomy

```razor
@using Blazix.BaseUI.Button

<Button>Submit</Button>
```

## Examples

### Rendering as another tag

Set `NativeButton="false"` when the rendered element is not a native `<button>`. The component will add button semantics and keyboard activation.

```razor
@using Blazix.BaseUI
@using Blazix.BaseUI.Button

<Button NativeButton="false" Render="@RenderAsDiv">
    Button that can contain complex children
</Button>

@code {
    private RenderFragment<RenderProps<ButtonState>> RenderAsDiv => props =>
        RenderUtilities.CreateElement("div", props);
}
```

### Rendering links as buttons

Links already have their own navigation semantics. If a link needs button styling, style the `<a>` element directly instead of rendering it through Button.

### Loading states

Use `FocusableWhenDisabled="true"` when a button disables itself after activation. Focus remains on the control while the operation runs.

```razor
<Button Disabled="loading" FocusableWhenDisabled="true" @onclick="HandleClickAsync">
    @(loading ? "Submitting" : "Submit")
</Button>

@code {
    private bool loading;

    private async Task HandleClickAsync()
    {
        loading = true;
        await Task.Delay(4000);
        loading = false;
    }
}
```

## API reference

### Button

A button that can use native button semantics or render as another focusable element. Renders a `<button>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Determines whether the button should ignore user interaction. |
| `FocusableWhenDisabled` | `bool` | `false` | Keeps the button in the tab order when disabled by using `aria-disabled` instead of the disabled attribute. |
| `NativeButton` | `bool` | `true` | Determines whether the rendered element is a native `<button>`. Set to `false` when rendering another tag. |
| `TabIndex` | `int` | `0` | The tab index applied when the button is focusable. |
| `Render` | `RenderFragment<RenderProps<ButtonState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<ButtonState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<ButtonState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-disabled`.
