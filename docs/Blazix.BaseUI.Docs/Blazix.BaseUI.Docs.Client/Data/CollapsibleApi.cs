namespace Blazix.BaseUI.Docs.Client.Data;

public static class CollapsibleApi
{
    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Root",
            "Groups the trigger and panel while owning the open state. Renders a <div> element by default.",
            [
                new ApiRow("Open", "bool?", "null", "The controlled open state. To render an uncontrolled collapsible, use DefaultOpen instead."),
                new ApiRow("DefaultOpen", "bool", "false", "The uncontrolled open state used on the initial render."),
                new ApiRow("Disabled", "bool", "false", "Determines whether the component should ignore user interaction."),
                new ApiRow("OpenChanged", "EventCallback<bool>", "—", "Callback invoked when the open state changes, supporting two-way binding."),
                new ApiRow("OnOpenChange", "EventCallback<CollapsibleOpenChangeEventArgs>", "—", "Callback invoked before the open state is committed. The event can be canceled."),
                new ApiRow("Render", "RenderFragment<RenderProps<CollapsibleRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<CollapsibleRootState, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<CollapsibleRootState, string?>?", "null", "Returns a CSS style based on the component's state."),
            ],
            [
                new ApiRow("data-open", "", "", "Present when the panel is open."),
                new ApiRow("data-closed", "", "", "Present when the panel is closed."),
                new ApiRow("data-disabled", "", "", "Present when the collapsible is disabled."),
                new ApiRow("data-starting-style", "", "", "Present when the panel is animating in."),
                new ApiRow("data-ending-style", "", "", "Present when the panel is animating out."),
            ],
            []),
        new("Trigger",
            "A button that opens and closes the panel. Renders a <button> element by default.",
            [
                new ApiRow("Disabled", "bool?", "null", "Determines whether the trigger ignores user interaction. Inherits from Root when null."),
                new ApiRow("NativeButton", "bool", "true", "Determines whether the component renders a native <button> element."),
                new ApiRow("Render", "RenderFragment<RenderProps<CollapsibleRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<CollapsibleRootState, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<CollapsibleRootState, string?>?", "null", "Returns a CSS style based on the component's state."),
            ],
            [
                new ApiRow("data-panel-open", "", "", "Present when the panel is open."),
                new ApiRow("data-disabled", "", "", "Present when the trigger is disabled."),
                new ApiRow("data-starting-style", "", "", "Present when the panel is animating in."),
                new ApiRow("data-ending-style", "", "", "Present when the panel is animating out."),
            ],
            []),
        new("Panel",
            "The collapsible content area. Renders a <div> element by default.",
            [
                new ApiRow("KeepMounted", "bool", "false", "Keeps the panel in the DOM while closed."),
                new ApiRow("HiddenUntilFound", "bool", "false", "Uses hidden=\"until-found\" so browser find-in-page can reveal closed content."),
                new ApiRow("Render", "RenderFragment<RenderProps<CollapsiblePanelState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<CollapsiblePanelState, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<CollapsiblePanelState, string?>?", "null", "Returns a CSS style based on the component's state."),
            ],
            [
                new ApiRow("data-open", "", "", "Present when the panel is open."),
                new ApiRow("data-closed", "", "", "Present when the panel is closed."),
                new ApiRow("data-disabled", "", "", "Present when the root is disabled."),
                new ApiRow("data-starting-style", "", "", "Present when the panel is animating in."),
                new ApiRow("data-ending-style", "", "", "Present when the panel is animating out."),
            ],
            [
                new ApiRow("--collapsible-panel-height", "", "", "The measured height of the panel content; used to animate open and close transitions."),
                new ApiRow("--collapsible-panel-width", "", "", "The measured width of the panel content; used for width-based animations."),
            ]),
    ];
}
