namespace Blazix.BaseUI.Docs.Client.Data;

public static class ScrollAreaApi
{
    private static readonly IReadOnlyList<ApiRow> RootStateAttributes =
    [
        new("data-scrolling", "", "", "Present while the user is scrolling inside the scroll area."),
        new("data-has-overflow-x", "", "", "Present when the content is wider than the viewport."),
        new("data-has-overflow-y", "", "", "Present when the content is taller than the viewport."),
        new("data-overflow-x-start", "", "", "Present when horizontal content overflows the inline start edge."),
        new("data-overflow-x-end", "", "", "Present when horizontal content overflows the inline end edge."),
        new("data-overflow-y-start", "", "", "Present when vertical content overflows the block start edge."),
        new("data-overflow-y-end", "", "", "Present when vertical content overflows the block end edge."),
    ];

    private static readonly IReadOnlyList<ApiRow> ScrollbarAttributes =
    [
        new("data-orientation", "\"vertical\" | \"horizontal\"", "\"vertical\"", "Indicates which axis this scrollbar controls."),
        new("data-hovering", "", "", "Present while the scrollbar is hovered."),
        new("data-scrolling", "", "", "Present while this scrollbar's axis is scrolling."),
    ];

    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Root",
            "Groups all scroll area parts, measures overflow state, and renders a <div> element by default.",
            [
                new ApiRow("OverflowEdgeThreshold", "ScrollAreaOverflowEdgeThreshold", "ScrollAreaOverflowEdgeThreshold.Zero", "Pixels that must be passed before overflow edge attributes are applied. Can be normalized per edge."),
                new ApiRow("Render", "RenderFragment<RenderProps<ScrollAreaRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<ScrollAreaRootState, string?>?", "null", "Returns CSS classes based on scroll and overflow state."),
                new ApiRow("StyleValue", "Func<ScrollAreaRootState, string?>?", "null", "Returns inline styles based on scroll and overflow state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The viewport, content, scrollbar, thumb, and corner parts."),
            ],
            RootStateAttributes,
            [
                new ApiRow("--scroll-area-corner-height", "", "", "Measured scrollbar corner height."),
                new ApiRow("--scroll-area-corner-width", "", "", "Measured scrollbar corner width."),
            ]),
        new("Viewport",
            "The actual scrollable container. It renders a <div>, disables native scrollbar painting, and exposes overflow distances.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<ScrollAreaRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<ScrollAreaRootState, string?>?", "null", "Returns CSS classes based on scroll and overflow state."),
                new ApiRow("StyleValue", "Func<ScrollAreaRootState, string?>?", "null", "Returns inline styles based on scroll and overflow state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The scroll area content."),
            ],
            RootStateAttributes,
            [
                new ApiRow("--scroll-area-overflow-x-start", "", "", "Distance from the horizontal start edge."),
                new ApiRow("--scroll-area-overflow-x-end", "", "", "Distance from the horizontal end edge."),
                new ApiRow("--scroll-area-overflow-y-start", "", "", "Distance from the vertical start edge."),
                new ApiRow("--scroll-area-overflow-y-end", "", "", "Distance from the vertical end edge."),
            ]),
        new("Content",
            "Wraps the scrollable content and mirrors the root overflow state. Renders a <div> element by default.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<ScrollAreaRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<ScrollAreaRootState, string?>?", "null", "Returns CSS classes based on scroll and overflow state."),
                new ApiRow("StyleValue", "Func<ScrollAreaRootState, string?>?", "null", "Returns inline styles based on scroll and overflow state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "The content to scroll."),
            ],
            RootStateAttributes,
            []),
        new("Scrollbar",
            "A vertical or horizontal scrollbar. It renders only when its axis overflows unless KeepMounted is true.",
            [
                new ApiRow("Orientation", "Orientation", "Orientation.Vertical", "The axis controlled by this scrollbar."),
                new ApiRow("KeepMounted", "bool", "false", "Keeps the scrollbar element in the DOM even when its axis has no overflow."),
                new ApiRow("Render", "RenderFragment<RenderProps<ScrollAreaScrollbarState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<ScrollAreaScrollbarState, string?>?", "null", "Returns CSS classes based on scrollbar state."),
                new ApiRow("StyleValue", "Func<ScrollAreaScrollbarState, string?>?", "null", "Returns inline styles based on scrollbar state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "Usually a ScrollAreaThumb."),
            ],
            ScrollbarAttributes,
            [
                new ApiRow("--scroll-area-thumb-height", "", "", "Measured vertical thumb height."),
                new ApiRow("--scroll-area-thumb-width", "", "", "Measured horizontal thumb width."),
            ]),
        new("Thumb",
            "The draggable scrollbar thumb. It receives orientation from the parent Scrollbar.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<ScrollAreaThumbState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<ScrollAreaThumbState, string?>?", "null", "Returns CSS classes based on thumb state."),
                new ApiRow("StyleValue", "Func<ScrollAreaThumbState, string?>?", "null", "Returns inline styles based on thumb state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "Optional thumb contents."),
            ],
            [
                new("data-orientation", "\"vertical\" | \"horizontal\"", "", "Matches the parent scrollbar orientation."),
            ],
            []),
        new("Corner",
            "Fills the corner where vertical and horizontal scrollbars meet. It renders only when both axes overflow.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<ScrollAreaCornerState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<ScrollAreaCornerState, string?>?", "null", "Returns CSS classes based on corner state."),
                new ApiRow("StyleValue", "Func<ScrollAreaCornerState, string?>?", "null", "Returns inline styles based on corner state."),
                new ApiRow("ChildContent", "RenderFragment?", "null", "Optional corner contents."),
            ],
            [],
            []),
    ];
}
