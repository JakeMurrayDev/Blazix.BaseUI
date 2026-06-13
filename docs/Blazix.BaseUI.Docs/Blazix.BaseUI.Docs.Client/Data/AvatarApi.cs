namespace Blazix.BaseUI.Docs.Client.Data;

public static class AvatarApi
{
    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Root",
            "Displays a user's profile picture, initials, or fallback icon. Renders a <span> element by default.",
            [
                new ApiRow("Render", "RenderFragment<RenderProps<AvatarRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<AvatarRootState, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<AvatarRootState, string?>?", "null", "Returns a CSS style based on the component's state."),
            ],
            [],
            []),
        new("Image",
            "The image to be displayed in the avatar. Standard <img> attributes such as src and alt are forwarded to the element. Renders an <img> element by default.",
            [
                new ApiRow("OnLoadingStatusChange", "EventCallback<ImageLoadingStatus>", "—", "Callback fired when the image loading status changes (Idle, Loading, Loaded, Error)."),
                new ApiRow("Render", "RenderFragment<RenderProps<AvatarRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<AvatarRootState, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<AvatarRootState, string?>?", "null", "Returns a CSS style based on the component's state."),
            ],
            [],
            []),
        new("Fallback",
            "Rendered when the image fails to load or when no image is provided. Renders a <span> element by default.",
            [
                new ApiRow("Delay", "int?", "null", "How long to wait before showing the fallback, in milliseconds. Prevents a flash of fallback content while the image loads."),
                new ApiRow("Render", "RenderFragment<RenderProps<AvatarRootState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<AvatarRootState, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<AvatarRootState, string?>?", "null", "Returns a CSS style based on the component's state."),
            ],
            [],
            []),
    ];
}
