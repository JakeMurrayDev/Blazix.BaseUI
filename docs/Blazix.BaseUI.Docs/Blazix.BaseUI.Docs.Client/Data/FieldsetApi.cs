namespace Blazix.BaseUI.Docs.Client.Data;

public static class FieldsetApi
{
    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Root",
            "Groups related controls and associates them with a legend. Renders a <fieldset> element by default.",
            [
                new ApiRow("Disabled", "bool", "false", "Marks the fieldset as disabled and cascades disabled state to descendant fields."),
                new ApiRow("Render", "RenderFragment<RenderProps<FieldsetRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<FieldsetRootState, string>?", "null", "Returns a CSS class based on the fieldset state."),
                new ApiRow("StyleValue", "Func<FieldsetRootState, string>?", "null", "Returns a CSS style based on the fieldset state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The fieldset contents."),
            ],
            [
                new ApiRow("data-disabled", "", "", "Present when the fieldset is disabled."),
            ],
            []),
        new("Legend",
            "Labels the fieldset and supplies the id used by Root aria-labelledby. Renders a <div> element by default.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<FieldsetLegendState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<FieldsetLegendState, string>?", "null", "Returns a CSS class based on the legend state."),
                new ApiRow("StyleValue", "Func<FieldsetLegendState, string>?", "null", "Returns a CSS style based on the legend state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The legend contents."),
            ],
            [
                new ApiRow("data-disabled", "", "", "Present when the parent fieldset is disabled."),
            ],
            []),
    ];
}
