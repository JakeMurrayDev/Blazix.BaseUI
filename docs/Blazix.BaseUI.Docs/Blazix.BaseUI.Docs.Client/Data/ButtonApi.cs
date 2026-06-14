namespace Blazix.BaseUI.Docs.Client.Data;

public static class ButtonApi
{
    public static IReadOnlyList<ComponentPart> Parts { get; } =
    [
        new("Button",
            "A button that can use native button semantics or render as another focusable element. Renders a <button> element by default.",
            [
                new ApiRow("Disabled", "bool", "false", "Determines whether the button should ignore user interaction."),
                new ApiRow("FocusableWhenDisabled", "bool", "false", "Keeps the button in the tab order when disabled by using aria-disabled instead of the disabled attribute."),
                new ApiRow("NativeButton", "bool", "true", "Determines whether the rendered element is a native <button>. Set to false when rendering another tag."),
                new ApiRow("TabIndex", "int", "0", "The tab index applied when the button is focusable."),
                new ApiRow("Render", "RenderFragment<RenderProps<ButtonState>>?", "null", "Replaces the rendered element with a different tag or composes it with another component."),
                new ApiRow("ClassValue", "Func<ButtonState, string?>?", "null", "Returns a CSS class based on the component's state."),
                new ApiRow("StyleValue", "Func<ButtonState, string?>?", "null", "Returns a CSS style based on the component's state."),
            ],
            [
                new ApiRow("data-disabled", "", "", "Present when the button is disabled."),
            ],
            []),
    ];
}
