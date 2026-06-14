# Form

A native form element with consolidated field validation and error handling.

Rendered docs: `/components/form`

## Usage guidelines

- Use Field parts inside Form so validation state, focus targeting, and submitted values share the same registry.
- Provide `Errors` when server validation returns field-level messages.
- Use `OnFormSubmit` when you want values collected by field name instead of reading the model directly.

## Anatomy

```razor
@using Blazix.BaseUI.Field
@using Blazix.BaseUI.Form

<Form Model="@model">
    <FieldRoot Name="email">
        <FieldLabel>Email</FieldLabel>
        <FieldControl TValue="string" @bind-Value="model.Email" />
        <FieldError />
    </FieldRoot>
</Form>

@code {
    private readonly EmailModel model = new();

    private sealed class EmailModel
    {
        public string Email { get; set; } = "";
    }
}
```

## Examples

### Submit form values as a dictionary

`OnFormSubmit` receives a `FormSubmitEventArgs` value with registered field values keyed by field name.

```razor
<Form Model="@model" OnFormSubmit="HandleSubmitAsync">
    <FieldRoot Name="name">
        <FieldLabel>Name</FieldLabel>
        <FieldControl TValue="string" @bind-Value="model.Name" />
    </FieldRoot>
</Form>

@code {
    private IReadOnlyDictionary<string, object?>? submittedValues;

    private Task HandleSubmitAsync(FormSubmitEventArgs args)
    {
        submittedValues = args.Values;
        return Task.CompletedTask;
    }
}
```

### External errors

Set `Errors` to a dictionary keyed by `FieldRoot.Name` to display server-style validation messages through `FieldError`.

## API reference

### Form

Coordinates Field registrations, validation modes, external errors, focus targeting, and submit callbacks. It renders a native `form` element by default.
