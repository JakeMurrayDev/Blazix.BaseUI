using Blazix.BaseUI.Popover;
using Blazix.BaseUI.Tests.Contracts.Popover;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blazix.BaseUI.Tests.Popover;

public class PopoverTitleTests : BunitContext, IPopoverTitleContract
{
    public PopoverTitleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPopoverModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateTitleInPopover(
        bool defaultOpen = true,
        RenderFragment<RenderProps<PopoverTitleState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<PopoverTitleState, string>? classValue = null,
        Func<PopoverTitleState, string>? styleValue = null)
    {
        return builder =>
        {
            builder.OpenComponent<PopoverRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<PopoverRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PopoverPortal>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PopoverPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<PopoverTitle>(0);
                            var attrIndex = 1;

                            if (render is not null)
                                popupBuilder.AddAttribute(attrIndex++, "Render", render);
                            if (classValue is not null)
                                popupBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                            if (styleValue is not null)
                                popupBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                            if (additionalAttributes is not null)
                                popupBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);
                            popupBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Title text")));

                            popupBuilder.CloseComponent();
                        }));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsH2ByDefault()
    {
        var cut = Render(CreateTitleInPopover());

        var title = cut.Find("h2");
        title.TagName.ShouldBe("H2");
        title.TextContent.ShouldBe("Title text");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<PopoverTitleState>> render = props => builder =>
        {
            builder.OpenElement(0, "h3");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateTitleInPopover(render: render));

        var title = cut.Find("h3");
        title.TextContent.ShouldBe("Title text");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateTitleInPopover(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "title" },
                { "aria-label", "Title" }
            }
        ));

        var title = cut.Find("[data-testid='title']");
        title.GetAttribute("aria-label").ShouldBe("Title");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaLabelledByOnPopup()
    {
        var cut = Render(CreateTitleInPopover());

        var title = cut.Find("h2");
        var titleId = title.GetAttribute("id");

        var popup = cut.Find("[role='dialog']");
        popup.GetAttribute("aria-labelledby").ShouldBe(titleId);

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreateTitleInPopover(
            classValue: _ => "open-class"
        ));

        var title = cut.Find("h2");
        title.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreateTitleInPopover(
            styleValue: _ => "font-weight: bold"
        ));

        var title = cut.Find("h2");
        title.GetAttribute("style")!.ShouldContain("font-weight: bold");

        return Task.CompletedTask;
    }

}
