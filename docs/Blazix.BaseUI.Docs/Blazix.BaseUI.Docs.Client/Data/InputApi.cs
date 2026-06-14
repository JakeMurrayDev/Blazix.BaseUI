namespace Blazix.BaseUI.Docs.Client.Data;

public static class InputApi
{
    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Input",
            "A native input element that automatically works with Field. Renders an <input> element.",
            [
                new ApiRow("Value", "string?", "null", "The controlled value of the input."),
                new ApiRow("ValueChanged", "EventCallback<string>", "—", "Callback fired when the input value changes, supporting two-way binding."),
                new ApiRow("ValueExpression", "Expression<Func<string>>?", "null", "Identifies the bound value for Blazor forms and validation."),
                new ApiRow("DefaultValue", "string?", "null", "The initial value when the input is uncontrolled."),
                new ApiRow("DisplayName", "string?", "null", "The display name used for validation messages."),
                new ApiRow("Name", "string?", "null", "The name attribute of the input element."),
                new ApiRow("Disabled", "bool", "false", "Whether the input should ignore user interaction."),
                new ApiRow("Render", "RenderFragment<RenderProps<InputState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<InputState, string>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<InputState, string>?", "null", "Returns a CSS style based on the component's state."),
            ],
            [
                new ApiRow("data-disabled", "", "", "Present when the input is disabled."),
                new ApiRow("data-valid", "", "", "Present when the input is valid inside a FieldRoot."),
                new ApiRow("data-invalid", "", "", "Present when the input is invalid inside a FieldRoot."),
                new ApiRow("data-dirty", "", "", "Present when the input value has changed inside a FieldRoot."),
                new ApiRow("data-touched", "", "", "Present when the input has been focused and blurred inside a FieldRoot."),
                new ApiRow("data-filled", "", "", "Present when the input has a value."),
                new ApiRow("data-focused", "", "", "Present when the input is focused."),
            ],
            []),
    ];
}
