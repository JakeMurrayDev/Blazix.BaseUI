namespace Blazix.BaseUI.Docs.Client.Data;

public static class ToggleGroupApi
{
    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Group",
            "Provides roving focus and shared value state for Toggle children.",
            [
                new ApiRow("Value", "IReadOnlyList<string>?", "null", "The controlled selected values."),
                new ApiRow("DefaultValue", "IReadOnlyList<string>?", "null", "The initially selected values for uncontrolled usage."),
                new ApiRow("Disabled", "bool", "false", "Disables every toggle in the group."),
                new ApiRow("Orientation", "Orientation", "Orientation.Horizontal", "Controls horizontal or vertical arrow-key navigation."),
                new ApiRow("LoopFocus", "bool", "true", "Allows arrow-key focus to wrap from the last toggle to the first, and the reverse."),
                new ApiRow("Multiple", "bool", "false", "Allows more than one toggle to be pressed at a time."),
                new ApiRow("ValueChanged", "EventCallback<IReadOnlyList<string>>", "default", "Two-way binding callback invoked when the selected values change."),
                new ApiRow("OnValueChange", "EventCallback<ToggleGroupValueChangeEventArgs>", "default", "Pre-commit callback invoked before value changes. Call Cancel() to prevent the update."),
                new ApiRow("Render", "RenderFragment<RenderProps<ToggleGroupState>>?", "null", "Replaces the rendered element or composes it with another component."),
                new ApiRow("ClassValue", "Func<ToggleGroupState, string>?", "null", "Returns CSS classes from group state."),
                new ApiRow("StyleValue", "Func<ToggleGroupState, string>?", "null", "Returns inline styles from group state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "Toggle children."),
            ],
            [
                new ApiRow("role", "\"group\"", "\"group\"", "Identifies the element as a grouped control."),
                new ApiRow("aria-orientation", "\"horizontal\" | \"vertical\"", "", "Set when the group manages its own composite navigation outside Toolbar."),
                new ApiRow("data-orientation", "\"horizontal\" | \"vertical\"", "\"horizontal\"", "Matches the group orientation."),
                new ApiRow("data-disabled", "", "", "Present when the group is disabled."),
                new ApiRow("data-multiple", "", "", "Present when Multiple is true."),
            ],
            []),
    ];
}
