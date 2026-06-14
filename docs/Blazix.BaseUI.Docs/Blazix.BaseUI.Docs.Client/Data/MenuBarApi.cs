namespace Blazix.BaseUI.Docs.Client.Data;

public static class MenuBarApi
{
    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Root",
            "The container for a horizontal or vertical set of menus. Renders a <div> element by default.",
            [
                new ApiRow("Disabled", "bool", "false", "Whether the whole menubar should ignore user interaction."),
                new ApiRow("LoopFocus", "bool", "true", "Whether arrow-key focus wraps from the last trigger back to the first."),
                new ApiRow("Modal", "bool", "true", "Whether open menus in the menubar use modal behavior."),
                new ApiRow("Orientation", "Orientation", "Horizontal", "The orientation of the menubar."),
                new ApiRow("Render", "RenderFragment<RenderProps<MenuBarRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<MenuBarRootState, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<MenuBarRootState, string?>?", "null", "Returns a CSS style based on the component's state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The menubar contents."),
            ],
            [
                new ApiRow("data-orientation", "\"horizontal\" | \"vertical\"", "", "The orientation of the menubar."),
                new ApiRow("data-has-submenu-open", "", "", "Present when any menu in the menubar is open."),
                new ApiRow("data-modal", "", "", "Present when the menubar is modal."),
            ],
            []),
    ];
}
