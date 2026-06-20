namespace Blazix.BaseUI.Docs.Client.Data;

public static class ToolbarApi
{
    private static readonly IReadOnlyList<ApiRow> OrientationAttributes =
    [
        new("data-orientation", "\"horizontal\" | \"vertical\"", "\"horizontal\"", "Matches the toolbar orientation."),
    ];

    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Root",
            "Groups controls into a toolbar and manages roving focus.",
            [
                new ApiRow("Disabled", "bool", "false", "Disables toolbar navigation and controls that inherit disabled state."),
                new ApiRow("LoopFocus", "bool", "true", "Allows roving focus to wrap from the last item to the first, and the reverse."),
                new ApiRow("Orientation", "Orientation", "Orientation.Horizontal", "Controls horizontal or vertical arrow-key navigation."),
                new ApiRow("Render", "RenderFragment<RenderProps<ToolbarRootState>>?", "null", "Replaces the rendered element or composes it with another component."),
                new ApiRow("ClassValue", "Func<ToolbarRootState, string>?", "null", "Returns CSS classes from toolbar state."),
                new ApiRow("StyleValue", "Func<ToolbarRootState, string>?", "null", "Returns inline styles from toolbar state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "Toolbar controls, groups, separators, and composed components."),
            ],
            [
                new ApiRow("role", "\"toolbar\"", "\"toolbar\"", "Identifies the element as a toolbar."),
                new ApiRow("aria-orientation", "\"horizontal\" | \"vertical\"", "", "Matches the toolbar orientation."),
                new ApiRow("data-disabled", "", "", "Present when disabled."),
                ..OrientationAttributes,
            ],
            []),
        new("Button",
            "A focusable toolbar button. It renders a native button by default.",
            [
                new ApiRow("Disabled", "bool", "false", "Prevents activation."),
                new ApiRow("FocusableWhenDisabled", "bool", "true", "Keeps the disabled button in the roving-focus order."),
                new ApiRow("NativeButton", "bool", "true", "Renders as a native button when using the default element."),
                new ApiRow("Render", "RenderFragment<RenderProps<ToolbarButtonState>>?", "null", "Replaces the rendered element or composes it with another component."),
                new ApiRow("ClassValue", "Func<ToolbarButtonState, string>?", "null", "Returns CSS classes from button state."),
                new ApiRow("StyleValue", "Func<ToolbarButtonState, string>?", "null", "Returns inline styles from button state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "Button contents."),
            ],
            [
                new ApiRow("data-disabled", "", "", "Present when disabled."),
                new ApiRow("data-focusable", "", "", "Present when a disabled item remains focusable."),
                new ApiRow("aria-disabled", "\"true\"", "", "Set when disabled but focusable."),
                ..OrientationAttributes,
            ],
            []),
        new("Link",
            "A toolbar item rendered as an anchor.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<ToolbarLinkState>>?", "null", "Replaces the rendered element or composes it with another component."),
                new ApiRow("ClassValue", "Func<ToolbarLinkState, string>?", "null", "Returns CSS classes from link state."),
                new ApiRow("StyleValue", "Func<ToolbarLinkState, string>?", "null", "Returns inline styles from link state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "Link contents."),
            ],
            OrientationAttributes,
            []),
        new("Input",
            "A toolbar item rendered as an input while preserving roving-focus behavior.",
            [
                new ApiRow("Disabled", "bool", "false", "Disables the input."),
                new ApiRow("FocusableWhenDisabled", "bool", "true", "Keeps the disabled input in the roving-focus order."),
                new ApiRow("DefaultValue", "string?", "null", "The uncontrolled initial input value."),
                new ApiRow("Render", "RenderFragment<RenderProps<ToolbarInputState>>?", "null", "Replaces the rendered element or composes it with another component."),
                new ApiRow("ClassValue", "Func<ToolbarInputState, string>?", "null", "Returns CSS classes from input state."),
                new ApiRow("StyleValue", "Func<ToolbarInputState, string>?", "null", "Returns inline styles from input state."),
            ],
            [
                new ApiRow("data-disabled", "", "", "Present when disabled."),
                new ApiRow("data-focusable", "", "", "Present when a disabled item remains focusable."),
                new ApiRow("aria-disabled", "\"true\"", "", "Set when disabled but focusable."),
                ..OrientationAttributes,
            ],
            []),
        new("Group",
            "Groups related toolbar controls and passes disabled state to supported children.",
            [
                new ApiRow("Disabled", "bool", "false", "Disables controls inside the group."),
                new ApiRow("Render", "RenderFragment<RenderProps<ToolbarRootState>>?", "null", "Replaces the rendered element or composes it with another component."),
                new ApiRow("ClassValue", "Func<ToolbarRootState, string>?", "null", "Returns CSS classes from toolbar state."),
                new ApiRow("StyleValue", "Func<ToolbarRootState, string>?", "null", "Returns inline styles from toolbar state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "Grouped controls."),
            ],
            [
                new ApiRow("role", "\"group\"", "\"group\"", "Identifies the element as a grouped control set."),
                new ApiRow("data-disabled", "", "", "Present when disabled."),
                ..OrientationAttributes,
            ],
            []),
        new("Separator",
            "A visual divider between toolbar groups. Its orientation is perpendicular to the toolbar.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<SeparatorState>>?", "null", "Replaces the rendered element or composes it with another component."),
                new ApiRow("ClassValue", "Func<SeparatorState, string>?", "null", "Returns CSS classes from separator state."),
                new ApiRow("StyleValue", "Func<SeparatorState, string>?", "null", "Returns inline styles from separator state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "Optional separator contents."),
            ],
            [
                new ApiRow("role", "\"separator\"", "\"separator\"", "Identifies the divider as a separator."),
                new ApiRow("data-orientation", "\"horizontal\" | \"vertical\"", "", "Perpendicular to the toolbar orientation."),
            ],
            []),
    ];
}
