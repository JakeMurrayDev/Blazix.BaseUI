using BlazorBaseUI.Popover;
using BlazorBaseUI.Tests.Contracts.Popover;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Popover;

public class PopoverDescriptionTests : BunitContext, IPopoverDescriptionContract
{
    public PopoverDescriptionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPopoverModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateDescriptionInPopover(
        bool defaultOpen = true,
        RenderFragment<RenderProps<PopoverDescriptionState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<PopoverDescriptionState, string>? classValue = null,
        Func<PopoverDescriptionState, string>? styleValue = null)
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
                            popupBuilder.OpenComponent<PopoverDescription>(0);
                            var attrIndex = 1;

                            if (render is not null)
                                popupBuilder.AddAttribute(attrIndex++, "Render", render);
                            if (classValue is not null)
                                popupBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                            if (styleValue is not null)
                                popupBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                            if (additionalAttributes is not null)
                                popupBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);
                            popupBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Description text")));

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
    public Task RendersAsParagraphByDefault()
    {
        var cut = Render(CreateDescriptionInPopover());

        var description = cut.Find("p");
        description.TagName.ShouldBe("P");
        description.TextContent.ShouldBe("Description text");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<PopoverDescriptionState>> render = props => builder =>
        {
            builder.OpenElement(0, "article");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateDescriptionInPopover(render: render));

        var description = cut.Find("article");
        description.TextContent.ShouldBe("Description text");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateDescriptionInPopover(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "description" },
                { "aria-label", "Description" }
            }
        ));

        var description = cut.Find("[data-testid='description']");
        description.GetAttribute("aria-label").ShouldBe("Description");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaDescribedByOnPopup()
    {
        var cut = Render(CreateDescriptionInPopover());

        var description = cut.Find("p");
        var descriptionId = description.GetAttribute("id");

        var popup = cut.Find("[role='dialog']");
        popup.GetAttribute("aria-describedby").ShouldBe(descriptionId);

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreateDescriptionInPopover(
            classValue: _ => "open-class"
        ));

        var description = cut.Find("p");
        description.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreateDescriptionInPopover(
            styleValue: _ => "font-style: italic"
        ));

        var description = cut.Find("p");
        description.GetAttribute("style")!.ShouldContain("font-style: italic");

        return Task.CompletedTask;
    }

}
