namespace Blazix.BaseUI.Docs.Client.Data;

public static class ToggleApi
{
    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Toggle",
            "A two-state button that can be controlled or uncontrolled. It renders a native button by default.",
            [
                new ApiRow("Pressed", "bool?", "null", "The controlled pressed state."),
                new ApiRow("DefaultPressed", "bool", "false", "The initial pressed state for uncontrolled usage."),
                new ApiRow("Disabled", "bool", "false", "Prevents user interaction."),
                new ApiRow("NativeButton", "bool", "true", "Renders as a native button when using the default element."),
                new ApiRow("Value", "string?", "null", "The value reported to a parent ToggleGroup."),
                new ApiRow("PressedChanged", "EventCallback<bool>", "default", "Two-way binding callback invoked when pressed state changes."),
                new ApiRow("OnPressedChange", "EventCallback<TogglePressedChangeEventArgs>", "default", "Pre-commit callback invoked before pressed state changes. Call Cancel() to prevent the update."),
                new ApiRow("Render", "RenderFragment<RenderProps<ToggleState>>?", "null", "Replaces the rendered element or composes it with another component."),
                new ApiRow("ClassValue", "Func<ToggleState, string>?", "null", "Returns CSS classes from pressed, disabled, and group state."),
                new ApiRow("StyleValue", "Func<ToggleState, string>?", "null", "Returns inline styles from pressed, disabled, and group state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The button contents."),
            ],
            [
                new ApiRow("aria-pressed", "\"true\" | \"false\"", "", "Reflects the pressed state when Toggle is not inside a ToggleGroup."),
                new ApiRow("aria-disabled", "\"true\"", "", "Set for disabled non-native buttons or grouped disabled toggles."),
                new ApiRow("data-pressed", "", "", "Present when pressed."),
                new ApiRow("data-disabled", "", "", "Present when disabled."),
            ],
            []),
    ];
}
