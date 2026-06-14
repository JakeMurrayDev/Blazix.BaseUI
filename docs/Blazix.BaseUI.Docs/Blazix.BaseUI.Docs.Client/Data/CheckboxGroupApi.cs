namespace Blazix.BaseUI.Docs.Client.Data;

public static class CheckboxGroupApi
{
    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("CheckboxGroup",
            "Provides shared state for CheckboxRoot children. Renders a <div> element by default.",
            [
                new ApiRow("Value", "string[]?", "null", "The controlled selected values. To render an uncontrolled group, use DefaultValue instead."),
                new ApiRow("DefaultValue", "string[]?", "null", "The uncontrolled selected values used on the initial render."),
                new ApiRow("AllValues", "string[]?", "null", "All child values that a parent checkbox should control."),
                new ApiRow("Disabled", "bool", "false", "Disables the group and cascades the disabled state to child checkboxes."),
                new ApiRow("ValueChanged", "EventCallback<string[]>", "—", "Callback invoked when the selected values change, supporting two-way binding."),
                new ApiRow("OnValueChange", "EventCallback<CheckboxGroupValueChangeEventArgs>", "—", "Callback invoked before the group value is committed. The event can be canceled."),
                new ApiRow("Render", "RenderFragment<RenderProps<CheckboxGroupState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<CheckboxGroupState, string>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<CheckboxGroupState, string>?", "null", "Returns a CSS style based on the component's state."),
            ],
            [
                new ApiRow("data-disabled", "", "", "Present when the group is disabled."),
                new ApiRow("data-valid", "", "", "Present when field validation reports a valid state."),
                new ApiRow("data-invalid", "", "", "Present when field validation reports an invalid state."),
                new ApiRow("data-touched", "", "", "Present after the associated field has been blurred."),
                new ApiRow("data-dirty", "", "", "Present after the selected values differ from the field's initial value."),
                new ApiRow("data-filled", "", "", "Present when at least one value is selected."),
                new ApiRow("data-focused", "", "", "Present when a child checkbox has focus."),
            ],
            []),
    ];
}
