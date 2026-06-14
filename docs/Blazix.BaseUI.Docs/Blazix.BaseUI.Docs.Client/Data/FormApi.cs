namespace Blazix.BaseUI.Docs.Client.Data;

public static class FormApi
{
    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Form",
            "A native form wrapper that coordinates Field registrations, validation modes, external errors, and submit callbacks. Renders a <form> element by default.",
            [
                new ApiRow("EditContext", "EditContext?", "null", "The explicit Blazor edit context. Do not also provide Model."),
                new ApiRow("Model", "object?", "null", "The model used to create an EditContext when EditContext is not supplied."),
                new ApiRow("ValidationMode", "ValidationMode", "OnSubmit", "Controls the default validation mode for child FieldRoot components."),
                new ApiRow("NoValidate", "bool", "true", "When true, renders novalidate and lets Blazix coordinate validation feedback."),
                new ApiRow("Errors", "Dictionary<string, string[]>?", "null", "External errors keyed by field name, commonly returned from a server."),
                new ApiRow("OnSubmit", "EventCallback<EditContext>", "-", "Callback invoked for a valid submit. When supplied, it runs instead of OnFormSubmit. Do not combine with OnValidSubmit or OnInvalidSubmit."),
                new ApiRow("OnValidSubmit", "EventCallback<EditContext>", "-", "Callback invoked when the form submits and all registered fields are valid."),
                new ApiRow("OnInvalidSubmit", "EventCallback<EditContext>", "-", "Callback invoked when the form submits and one or more registered fields are invalid."),
                new ApiRow("OnFormSubmit", "EventCallback<FormSubmitEventArgs>", "-", "Callback invoked for a valid submit with collected values keyed by field name."),
                new ApiRow("ActionsRef", "Action<FormActions>?", "null", "Receives imperative actions, including ValidateAsync for all fields or one named field."),
                new ApiRow("Render", "RenderFragment<RenderProps<FormState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<FormState, string>?", "null", "Returns a CSS class based on the form state."),
                new ApiRow("StyleValue", "Func<FormState, string>?", "null", "Returns a CSS style based on the form state."),
                new ApiRow("ChildContent", "RenderFragment<EditContext>?", "null", "The form contents. The context parameter is the active EditContext."),
            ],
            [],
            []),
    ];
}
