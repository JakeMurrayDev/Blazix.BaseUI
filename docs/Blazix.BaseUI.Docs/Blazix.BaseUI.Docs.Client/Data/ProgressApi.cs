namespace Blazix.BaseUI.Docs.Client.Data;

public static class ProgressApi
{
    private static readonly IReadOnlyList<ApiRow> SharedStateAttributes =
    [
        new("data-progressing", "", "", "Present when Value is finite and not equal to Max."),
        new("data-complete", "", "", "Present when Value is finite and equal to Max."),
        new("data-indeterminate", "", "", "Present when Value is null, NaN, or infinite."),
    ];

    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Root",
            "Owns the progress value, accessible progressbar attributes, label registration, formatted value, and status. Renders a <div> element by default.",
            [
                new ApiRow("Value", "double?", "null", "The current progress value. Null renders the indeterminate state."),
                new ApiRow("Min", "double", "0", "The minimum value for the progress range."),
                new ApiRow("Max", "double", "100", "The maximum value for the progress range."),
                new ApiRow("Format", "NumberFormatOptions?", "null", "Options used to format the visible value, matching the source Intl.NumberFormatOptions behavior."),
                new ApiRow("FormatString", "string?", "null", "A .NET numeric format string for Blazor-native formatting scenarios."),
                new ApiRow("Locale", "string?", "null", "The locale used when formatting the value."),
                new ApiRow("FormatProvider", "IFormatProvider?", "null", "The .NET format provider used when FormatString is supplied."),
                new ApiRow("GetAriaValueText", "Func<string?, double?, string>?", "null", "Returns the human-readable aria-valuetext for the current formatted and raw value."),
                new ApiRow("Render", "RenderFragment<RenderProps<ProgressRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<ProgressRootState, string?>?", "null", "Returns a CSS class based on the current progress status."),
                new ApiRow("StyleValue", "Func<ProgressRootState, string?>?", "null", "Returns a CSS style based on the current progress status."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The Progress parts to render."),
            ],
            SharedStateAttributes,
            []),
        new("Label",
            "Labels the progressbar and registers its id with the Root. Renders a <span> element by default.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<ProgressRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<ProgressRootState, string?>?", "null", "Returns a CSS class based on the current progress status."),
                new ApiRow("StyleValue", "Func<ProgressRootState, string?>?", "null", "Returns a CSS style based on the current progress status."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The label contents."),
            ],
            SharedStateAttributes,
            []),
        new("Track",
            "Contains the indicator and mirrors the Root status. Renders a <div> element by default.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<ProgressRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<ProgressRootState, string?>?", "null", "Returns a CSS class based on the current progress status."),
                new ApiRow("StyleValue", "Func<ProgressRootState, string?>?", "null", "Returns a CSS style based on the current progress status."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The track contents, usually ProgressIndicator."),
            ],
            SharedStateAttributes,
            []),
        new("Indicator",
            "Visualizes the current value with a computed width when the progress is determinate. Renders a <div> element by default.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<ProgressRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<ProgressRootState, string?>?", "null", "Returns a CSS class based on the current progress status."),
                new ApiRow("StyleValue", "Func<ProgressRootState, string?>?", "null", "Returns a CSS style based on the current progress status."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "Optional indicator contents."),
            ],
            SharedStateAttributes,
            []),
        new("Value",
            "Displays the formatted value or invokes a custom value renderer. Renders a <span> element by default.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<ProgressRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<ProgressRootState, string?>?", "null", "Returns a CSS class based on the current progress status."),
                new ApiRow("StyleValue", "Func<ProgressRootState, string?>?", "null", "Returns a CSS style based on the current progress status."),
                new ApiRow("ChildContent", "Func<string?, double?, RenderFragment>?", "null", "Receives the formatted value and raw value for custom value markup."),
            ],
            SharedStateAttributes,
            []),
    ];
}
