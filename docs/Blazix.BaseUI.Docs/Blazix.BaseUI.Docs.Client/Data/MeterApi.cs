namespace Blazix.BaseUI.Docs.Client.Data;

public static class MeterApi
{
    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Root",
            "Groups all parts of the meter and provides the value for screen readers. Renders a <div> element by default.",
            [
                new ApiRow("Value", "double", "—", "The current value."),
                new ApiRow("Min", "double", "0", "The minimum value in the range."),
                new ApiRow("Max", "double", "100", "The maximum value in the range."),
                new ApiRow("Format", "NumberFormatOptions?", "null", "Options used to format the displayed value."),
                new ApiRow("FormatString", "string?", "null", "A .NET numeric format string used to display the value."),
                new ApiRow("Locale", "string?", "null", "The locale used when formatting the value. When omitted, the current culture is used."),
                new ApiRow("FormatProvider", "IFormatProvider?", "null", "The .NET format provider used when FormatString is supplied."),
                new ApiRow("GetAriaValueText", "Func<string, double, string>?", "null", "Returns a human-readable aria-valuetext from the formatted value and raw value."),
                new ApiRow("Render", "RenderFragment<RenderProps<MeterRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<MeterRootState, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<MeterRootState, string?>?", "null", "Returns a CSS style based on the component's state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The meter parts to render."),
            ],
            [],
            []),
        new("Label",
            "An accessible label for the meter. Renders a <span> element by default.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<MeterRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<MeterRootState, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<MeterRootState, string?>?", "null", "Returns a CSS style based on the component's state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The label contents."),
            ],
            [],
            []),
        new("Track",
            "Contains the meter indicator and represents the entire range. Renders a <div> element by default.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<MeterRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<MeterRootState, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<MeterRootState, string?>?", "null", "Returns a CSS style based on the component's state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The track contents."),
            ],
            [],
            []),
        new("Indicator",
            "Visualizes the position of the value along the range. Renders a <div> element by default.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<MeterRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<MeterRootState, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<MeterRootState, string?>?", "null", "Returns a CSS style based on the component's state. The component appends its width and inset styles."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The indicator contents."),
            ],
            [],
            []),
        new("Value",
            "A text element displaying the current formatted value. Renders a <span> element by default.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<MeterRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<MeterRootState, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<MeterRootState, string?>?", "null", "Returns a CSS style based on the component's state."),
                new ApiRow("ChildContent", "Func<string, double, RenderFragment>?", "null", "Custom value renderer that receives the formatted value and raw value."),
            ],
            [],
            []),
    ];
}
