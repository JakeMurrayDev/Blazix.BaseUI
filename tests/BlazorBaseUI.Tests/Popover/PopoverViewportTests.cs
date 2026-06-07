using BlazorBaseUI.Popover;
using BlazorBaseUI.Tests.Contracts.Popover;
using BlazorBaseUI.Tests.Infrastructure;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Popover;

public class PopoverViewportTests : BunitContext, IPopoverViewportContract
{
    public PopoverViewportTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPopoverModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateViewportInPopover(
        bool defaultOpen = true,
        RenderFragment<RenderProps<PopoverViewportState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<PopoverViewportState, string>? classValue = null,
        Func<PopoverViewportState, string>? styleValue = null)
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
                            popupBuilder.OpenComponent<PopoverViewport>(0);
                            var attrIndex = 1;

                            if (render is not null)
                                popupBuilder.AddAttribute(attrIndex++, "Render", render);
                            if (classValue is not null)
                                popupBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                            if (styleValue is not null)
                                popupBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                            if (additionalAttributes is not null)
                                popupBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);
                            popupBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Viewport Content")));

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

    private static IElement FindViewportElement(IElement popup)
    {
        var currentContainer = popup.QuerySelector("[data-current]");
        currentContainer.ShouldNotBeNull();
        var viewport = currentContainer.ParentElement;
        viewport.ShouldNotBeNull();
        return viewport!;
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateViewportInPopover());

        var popup = cut.Find("[role='dialog']");
        var viewport = FindViewportElement(popup);
        viewport!.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<PopoverViewportState>> render = props => builder =>
        {
            builder.OpenElement(0, "section");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateViewportInPopover(render: render));

        var popup = cut.Find("[role='dialog']");
        var viewport = popup.QuerySelector("section");
        viewport.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateViewportInPopover(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "viewport" },
                { "aria-label", "Viewport" }
            }
        ));

        var viewport = cut.Find("[data-testid='viewport']");
        viewport.GetAttribute("aria-label").ShouldBe("Viewport");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildrenInCurrentContainer()
    {
        var cut = Render(CreateViewportInPopover());

        var popup = cut.Find("[role='dialog']");
        popup.TextContent.ShouldContain("Viewport Content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataCurrentAttribute()
    {
        var cut = Render(CreateViewportInPopover());

        var popup = cut.Find("[role='dialog']");
        var viewport = FindViewportElement(popup);
        // Viewport does not have data-open; it uses data-transitioning instead
        viewport!.HasAttribute("data-open").ShouldBeFalse();
        viewport!.HasAttribute("data-transitioning").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task SetsInstantTypeTriggerChangeOnTransitionEnd()
    {
        var cut = Render(CreateViewportInPopover());

        var viewport = cut.FindComponent<PopoverViewport>();

        // Simulate a viewport transition start then end
        await cut.InvokeAsync(() => viewport.Instance.OnViewportTransitionStart("right down"));
        await cut.InvokeAsync(() => viewport.Instance.OnViewportTransitionEnd());

        // After transition end, the popup should have data-instant="trigger-change"
        var popup = cut.Find("[role='dialog']");
        popup.GetAttribute("data-instant").ShouldBe("trigger-change");
    }

    [Fact]
    public Task HasDataInstantWhenSet()
    {
        var cut = Render(CreateViewportInPopover());

        var popup = cut.Find("[role='dialog']");
        var viewport = FindViewportElement(popup);

        // By default, PopoverInstantType is None so data-instant should not be rendered
        viewport!.HasAttribute("data-instant").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task HasActivationDirectionAfterTransitionStart()
    {
        var cut = Render(CreateViewportInPopover());

        var viewport = cut.FindComponent<PopoverViewport>();

        await cut.InvokeAsync(() => viewport.Instance.OnViewportTransitionStart("right down"));

        var popup = cut.Find("[role='dialog']");
        var viewportEl = FindViewportElement(popup);
        viewportEl!.GetAttribute("data-activation-direction").ShouldBe("right down");
        viewportEl!.HasAttribute("data-transitioning").ShouldBeTrue();
    }

    [Fact]
    public Task HasDataCurrentOnInnerWrapper()
    {
        var cut = Render(CreateViewportInPopover());

        var popup = cut.Find("[role='dialog']");
        var viewport = FindViewportElement(popup);

        // data-current should NOT be on the viewport element itself
        viewport!.HasAttribute("data-current").ShouldBeFalse();

        // data-current should be on an inner wrapper div
        var innerWrapper = viewport.QuerySelector("[data-current]");
        innerWrapper.ShouldNotBeNull();
        innerWrapper!.TagName.ShouldBe("DIV");
        innerWrapper.TextContent.ShouldContain("Viewport Content");

        return Task.CompletedTask;
    }
}
