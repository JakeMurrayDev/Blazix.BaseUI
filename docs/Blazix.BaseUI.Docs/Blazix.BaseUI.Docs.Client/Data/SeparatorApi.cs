namespace Blazix.BaseUI.Docs.Client.Data;

public static class SeparatorApi
{
    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Separator",
            "Renders an accessible separator element. The default element is a <div> with role=\"separator\".",
            [
                new ApiRow("Orientation", "Orientation", "Orientation.Horizontal", "The separator orientation."),
                new ApiRow("Render", "RenderFragment<RenderProps<SeparatorState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<SeparatorState, string>?", "null", "Returns CSS classes based on the separator state."),
                new ApiRow("StyleValue", "Func<SeparatorState, string>?", "null", "Returns inline styles based on the separator state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "Optional child content."),
            ],
            [
                new("data-orientation", "\"horizontal\" | \"vertical\"", "\"horizontal\"", "Matches the Orientation parameter."),
            ],
            []),
    ];
}
