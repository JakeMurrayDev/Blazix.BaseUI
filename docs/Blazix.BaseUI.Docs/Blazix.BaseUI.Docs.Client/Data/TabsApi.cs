namespace Blazix.BaseUI.Docs.Client.Data;

public static class TabsApi
{
    private static readonly IReadOnlyList<ApiRow> RootAttributes =
    [
        new("data-orientation", "\"horizontal\" | \"vertical\"", "\"horizontal\"", "Matches the root orientation."),
        new("data-activation-direction", "\"left\" | \"right\" | \"up\" | \"down\" | \"none\"", "", "Direction used by panel and indicator transitions after a tab change."),
    ];

    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Root",
            "Groups the tab list and panels, and manages controlled or uncontrolled selected value.",
            [
                new ApiRow("TValue", "type parameter", "-", "The value type shared by Root, Tab, and Panel."),
                new ApiRow("Value", "TValue?", "null", "The controlled selected value."),
                new ApiRow("DefaultValue", "TValue?", "null", "The initially selected value for uncontrolled usage."),
                new ApiRow("Orientation", "Orientation", "Orientation.Horizontal", "Controls horizontal or vertical keyboard navigation and data attributes."),
                new ApiRow("ValueChanged", "EventCallback<TValue?>", "default", "Two-way binding callback invoked when the selected value changes."),
                new ApiRow("OnValueChange", "EventCallback<TabsValueChangeEventArgs<TValue>>", "default", "Pre-commit callback invoked before selection changes. Call Cancel() to prevent the update."),
                new ApiRow("Render", "RenderFragment<RenderProps<TabsRootState>>?", "null", "Replaces the rendered element or composes it with another component."),
                new ApiRow("ClassValue", "Func<TabsRootState, string?>?", "null", "Returns CSS classes from root state."),
                new ApiRow("StyleValue", "Func<TabsRootState, string?>?", "null", "Returns inline styles from root state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The TabsList and TabsPanel parts."),
            ],
            RootAttributes,
            []),
        new("List",
            "Contains the tabs and optional indicator. Renders a tablist.",
            [
                new ApiRow("ActivateOnFocus", "bool", "false", "Selects a tab as soon as it receives roving focus."),
                new ApiRow("LoopFocus", "bool", "true", "Allows arrow-key focus to loop from the last tab to the first, and the reverse."),
                new ApiRow("Render", "RenderFragment<RenderProps<TabsRootState>>?", "null", "Replaces the rendered element or composes it with another component."),
                new ApiRow("ClassValue", "Func<TabsRootState, string?>?", "null", "Returns CSS classes from root state."),
                new ApiRow("StyleValue", "Func<TabsRootState, string?>?", "null", "Returns inline styles from root state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "TabsTab items and an optional TabsIndicator."),
            ],
            [
                new ApiRow("role", "\"tablist\"", "\"tablist\"", "Identifies the element as a tab list."),
                new ApiRow("aria-orientation", "\"vertical\"", "", "Set only for vertical lists."),
                ..RootAttributes,
            ],
            []),
        new("Tab",
            "A selectable tab. It renders a button by default and can render an anchor for link-style tabs.",
            [
                new ApiRow("Value", "TValue", "default!", "The value selected when this tab is activated."),
                new ApiRow("Disabled", "bool", "false", "Prevents activation and marks the tab disabled."),
                new ApiRow("NativeButton", "bool", "true", "Renders as a native button when using the default element."),
                new ApiRow("Render", "RenderFragment<RenderProps<TabsTabState>>?", "null", "Replaces the rendered element or composes it with another component."),
                new ApiRow("ClassValue", "Func<TabsTabState, string?>?", "null", "Returns CSS classes from tab state."),
                new ApiRow("StyleValue", "Func<TabsTabState, string?>?", "null", "Returns inline styles from tab state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The tab label."),
            ],
            [
                new ApiRow("role", "\"tab\"", "\"tab\"", "Identifies the element as a tab."),
                new ApiRow("aria-selected", "\"true\" | \"false\"", "", "Reflects whether the tab is selected."),
                new ApiRow("aria-controls", "string", "", "References the matching panel id when one is registered."),
                new ApiRow("aria-disabled", "\"true\"", "", "Set when the tab is disabled."),
                new ApiRow("data-active", "", "", "Present on the selected tab."),
                new ApiRow("data-disabled", "", "", "Present when disabled."),
                ..RootAttributes,
            ],
            []),
        new("Indicator",
            "A visual indicator positioned to the selected tab.",
            [
                new ApiRow("RenderBeforeHydration", "bool", "false", "Renders before client measurement has located the active tab."),
                new ApiRow("Render", "RenderFragment<RenderProps<TabsIndicatorState>>?", "null", "Replaces the rendered element or composes it with another component."),
                new ApiRow("ClassValue", "Func<TabsIndicatorState, string?>?", "null", "Returns CSS classes from indicator state."),
                new ApiRow("StyleValue", "Func<TabsIndicatorState, string?>?", "null", "Returns inline styles from indicator state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "Optional indicator contents."),
            ],
            RootAttributes,
            [
                new ApiRow("--active-tab-left", "", "", "Measured left offset of the active tab."),
                new ApiRow("--active-tab-top", "", "", "Measured top offset of the active tab."),
                new ApiRow("--active-tab-right", "", "", "Measured right offset of the active tab."),
                new ApiRow("--active-tab-bottom", "", "", "Measured bottom offset of the active tab."),
                new ApiRow("--active-tab-width", "", "", "Measured width of the active tab."),
                new ApiRow("--active-tab-height", "", "", "Measured height of the active tab."),
            ]),
        new("Panel",
            "The content associated with one tab value.",
            [
                new ApiRow("Value", "TValue", "default!", "The tab value that controls this panel."),
                new ApiRow("KeepMounted", "bool", "false", "Keeps inactive panels in the DOM."),
                new ApiRow("Render", "RenderFragment<RenderProps<TabsPanelState>>?", "null", "Replaces the rendered element or composes it with another component."),
                new ApiRow("ClassValue", "Func<TabsPanelState, string?>?", "null", "Returns CSS classes from panel state."),
                new ApiRow("StyleValue", "Func<TabsPanelState, string?>?", "null", "Returns inline styles from panel state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "Panel contents."),
            ],
            [
                new ApiRow("role", "\"tabpanel\"", "\"tabpanel\"", "Identifies the element as a tab panel."),
                new ApiRow("data-hidden", "", "", "Present when the panel is inactive and kept mounted."),
                new ApiRow("hidden", "", "", "Set when the panel is inactive and kept mounted."),
                new ApiRow("inert", "", "", "Set when the panel is inactive and kept mounted."),
                new ApiRow("data-index", "number", "", "Panel order within the root."),
                new ApiRow("data-starting-style", "", "", "Present while the panel is entering."),
                new ApiRow("data-ending-style", "", "", "Present while the panel is exiting."),
                ..RootAttributes,
            ],
            []),
    ];
}
