namespace Blazix.BaseUI.Docs.Client.Data;

public static class OtpFieldApi
{
    private static readonly IReadOnlyList<ApiRow> SharedStateAttributes =
    [
        new("data-complete", "", "", "Present when all slots are filled."),
        new("data-disabled", "", "", "Present when the field is disabled."),
        new("data-readonly", "", "", "Present when the field is read-only."),
        new("data-required", "", "", "Present when the field is required."),
        new("data-valid", "", "", "Present when the field is valid in a Field context."),
        new("data-invalid", "", "", "Present when the field is invalid in a Field context."),
        new("data-touched", "", "", "Present when the field has been touched in a Field context."),
        new("data-dirty", "", "", "Present when the field value differs from its initial value in a Field context."),
        new("data-filled", "", "", "Present when the root has a value or the slot contains a character."),
        new("data-focused", "", "", "Present when the OTP field is focused."),
    ];

    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Root",
            "Groups all OTP field parts, normalizes the value, integrates with Field/Form, and renders a hidden native text input for form submission. Renders a <div> element by default.",
            [
                new ApiRow("Id", "string?", "null", "The id assigned to the first visible input. Later inputs derive ids as {id}-2, {id}-3, and so on."),
                new ApiRow("AutoComplete", "string?", "\"one-time-code\"", "The autocomplete value applied to the first slot and hidden validation input."),
                new ApiRow("Form", "string?", "null", "The id of an external form associated with the hidden validation input."),
                new ApiRow("Length", "int", "required", "The number of OTP input slots."),
                new ApiRow("AutoSubmit", "bool", "false", "Submits the owning form when the OTP becomes complete."),
                new ApiRow("Mask", "bool", "false", "Renders slot inputs as password fields unless a slot overrides type."),
                new ApiRow("InputMode", "string?", "numeric/text from validation", "Overrides the virtual keyboard hint."),
                new ApiRow("ValidationType", "OtpFieldValidationType", "Numeric", "Controls built-in character filtering: Numeric, Alpha, Alphanumeric, or None."),
                new ApiRow("NormalizeValue", "Func<string, string>?", "null", "Custom normalization applied after whitespace stripping and built-in validation."),
                new ApiRow("Required", "bool", "false", "Determines whether a value is required before form submission."),
                new ApiRow("Disabled", "bool", "false", "Determines whether the field ignores user interaction."),
                new ApiRow("ReadOnly", "bool", "false", "Determines whether the field can receive focus but cannot change value."),
                new ApiRow("Name", "string?", "null", "The field name submitted by the hidden native input."),
                new ApiRow("Value", "string?", "null", "The controlled OTP value."),
                new ApiRow("ValueChanged", "EventCallback<string>", "—", "Callback invoked for two-way binding."),
                new ApiRow("DefaultValue", "string?", "null", "The initial uncontrolled OTP value."),
                new ApiRow("OnValueChange", "EventCallback<OtpFieldValueChangeEventArgs>", "—", "Callback fired when a normalized value is accepted. The event can be canceled."),
                new ApiRow("OnValueInvalid", "EventCallback<OtpFieldValueInvalidEventArgs>", "—", "Callback fired when input or paste text is rejected."),
                new ApiRow("OnValueComplete", "EventCallback<OtpFieldValueCompleteEventArgs>", "—", "Callback fired when the value becomes complete or a complete value is pasted over an already complete value."),
                new ApiRow("Render", "RenderFragment<RenderProps<OtpFieldRootState>>?", "null", "Replaces the rendered root element."),
                new ApiRow("ClassValue", "Func<OtpFieldRootState, string?>?", "null", "Returns a CSS class based on root state."),
                new ApiRow("StyleValue", "Func<OtpFieldRootState, string?>?", "null", "Returns a CSS style based on root state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The OTP input slots and optional separators."),
            ],
            SharedStateAttributes,
            []),
        new("Input",
            "An individual OTP character slot. Renders an <input> element by default and infers its index from render order.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<OtpFieldInputState>>?", "null", "Replaces the rendered input element."),
                new ApiRow("ClassValue", "Func<OtpFieldInputState, string?>?", "null", "Returns a CSS class based on slot state."),
                new ApiRow("StyleValue", "Func<OtpFieldInputState, string?>?", "null", "Returns a CSS style based on slot state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "Optional input contents when a custom render target supports children."),
            ],
            SharedStateAttributes,
            [
                new ApiRow("data-blazix-otp-input", "", "", "Internal marker used by the OTP browser interop module."),
            ]),
    ];
}
