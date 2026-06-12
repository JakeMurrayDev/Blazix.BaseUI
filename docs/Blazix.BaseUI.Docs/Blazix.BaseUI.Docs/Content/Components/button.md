# Button

Button provides accessible activation behavior for native buttons and deliberate custom element rendering.

## Import

```razor
@using Blazix.BaseUI.Button
```

## Anatomy

```razor
<Button Disabled="isSaving" FocusableWhenDisabled="true">
    Save changes
</Button>
```

## Notes

- Prefer native button rendering for ordinary actions.
- `FocusableWhenDisabled` keeps focus during loading states.
- `ClassValue` receives `ButtonState` for disabled styling.
