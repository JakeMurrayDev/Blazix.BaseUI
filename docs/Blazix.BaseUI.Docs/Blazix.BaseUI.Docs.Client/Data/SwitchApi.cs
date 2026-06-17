namespace Blazix.BaseUI.Docs.Client.Data;

public static class SwitchApi
{
    private static readonly IReadOnlyList<ApiRow> SwitchStateAttributes =
    [
        new("data-checked", "", "", "Present when the switch is checked."),
        new("data-unchecked", "", "", "Present when the switch is not checked."),
        new("data-disabled", "", "", "Present when the switch is disabled."),
        new("data-readonly", "", "", "Present when the switch is read-only."),
        new("data-required", "", "", "Present when the switch is required."),
        new("data-valid", "", "", "Present when the switch is valid inside FieldRoot."),
        new("data-invalid", "", "", "Present when the switch is invalid inside FieldRoot."),
        new("data-touched", "", "", "Present when the field has been touched inside FieldRoot."),
        new("data-dirty", "", "", "Present when the field value has changed inside FieldRoot."),
        new("data-filled", "", "", "Present when the switch is checked inside FieldRoot."),
        new("data-focused", "", "", "Present when the switch is focused inside FieldRoot."),
    ];

    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Root",
            "Represents the switch itself. It renders a <span> by default, or a native <button> when NativeButton is true, plus hidden form inputs.",
            [
                new ApiRow("Checked", "bool?", "null", "The controlled checked state. Use DefaultChecked for uncontrolled usage."),
                new ApiRow("DefaultChecked", "bool", "false", "The initially checked state for uncontrolled usage."),
                new ApiRow("Disabled", "bool", "false", "Prevents user interaction."),
                new ApiRow("ReadOnly", "bool", "false", "Prevents toggling while keeping the switch focusable."),
                new ApiRow("Required", "bool", "false", "Marks the hidden checkbox as required for form submission."),
                new ApiRow("Name", "string?", "null", "The submitted field name. FieldRoot.Name takes precedence when present."),
                new ApiRow("Form", "string?", "null", "The id of the form that owns the hidden inputs."),
                new ApiRow("UncheckedValue", "string?", "null", "Optional hidden input value submitted when the switch is off."),
                new ApiRow("Value", "string?", "null", "The value submitted when the switch is on. Defaults to native checkbox behavior when omitted."),
                new ApiRow("NativeButton", "bool", "false", "Renders the visible switch as a native button for sibling-label patterns."),
                new ApiRow("CheckedChanged", "EventCallback<bool>", "default", "Two-way binding callback invoked when checked state changes."),
                new ApiRow("OnCheckedChange", "EventCallback<SwitchCheckedChangeEventArgs>", "default", "Pre-commit callback invoked when the switch is activated or deactivated. Call Cancel() to prevent the change."),
                new ApiRow("Render", "RenderFragment<RenderProps<SwitchRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<SwitchRootState, string>?", "null", "Returns CSS classes based on switch state."),
                new ApiRow("StyleValue", "Func<SwitchRootState, string>?", "null", "Returns inline styles based on switch state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "Usually a SwitchThumb."),
            ],
            SwitchStateAttributes,
            []),
        new("Thumb",
            "The movable visual indicator inside the switch. It must be rendered inside SwitchRoot.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<SwitchRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<SwitchRootState, string>?", "null", "Returns CSS classes based on switch state."),
                new ApiRow("StyleValue", "Func<SwitchRootState, string>?", "null", "Returns inline styles based on switch state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "Optional thumb contents."),
            ],
            SwitchStateAttributes,
            []),
    ];
}
