using Blazix.BaseUI.Tests.Contracts.Tooltip;
using Blazix.BaseUI.Tests.Infrastructure;
using Blazix.BaseUI.Tooltip;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Tests.Tooltip;

public class TooltipViewportTests : BunitContext, ITooltipViewportContract
{
    public TooltipViewportTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupTooltipModule(JSInterop);
    }

    private RenderFragment CreateViewportInRoot(
        bool defaultOpen = true,
        RenderFragment<RenderProps<TooltipViewportState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<TooltipViewportState, string>? classValue = null,
        Func<TooltipViewportState, string>? styleValue = null)
    {
        return builder =>
        {
            builder.OpenComponent<TooltipRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<TooltipTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<TooltipPortal>(10);
                innerBuilder.AddAttribute(11, "KeepMounted", true);
                innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<TooltipPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<TooltipPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<TooltipViewport>(0);
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

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateViewportInRoot());

        // The viewport renders a div with a data-current child
        var currentContainer = cut.Find("[data-current]");
        currentContainer.ParentElement!.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<TooltipViewportState>> render = props => builder =>
        {
            builder.OpenElement(0, "section");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateViewportInRoot(render: render));

        var currentContainer = cut.Find("[data-current]");
        currentContainer.ParentElement!.TagName.ShouldBe("SECTION");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateViewportInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "viewport" }
            }
        ));

        var currentContainer = cut.Find("[data-current]");
        currentContainer.ParentElement!.GetAttribute("data-testid").ShouldBe("viewport");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildrenInCurrentContainer()
    {
        var cut = Render(CreateViewportInRoot());

        var currentContainer = cut.Find("[data-current]");
        currentContainer.TextContent.ShouldContain("Viewport Content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersAStablePreviousContentCacheOutsideCurrentContent()
    {
        var cut = Render(CreateViewportInRoot());

        var viewport = cut.Find("[data-current]").ParentElement!;
        var previousCache = viewport.QuerySelector("[data-previous-cache]");

        previousCache.ShouldNotBeNull();
        previousCache!.TextContent.ShouldBeEmpty();
        previousCache.HasAttribute("data-previous").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task TriggerHandoffPreparesViewportWithoutDisablingItsAnimation()
    {
        RenderFragment content = builder =>
        {
            builder.OpenComponent<TooltipTrigger>(0);
            builder.AddAttribute(1, "Id", "trigger-one");
            builder.AddAttribute(2, "Delay", 0);
            builder.AddAttribute(3, "UseJsHover", false);
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(child => child.AddContent(0, "One")));
            builder.CloseComponent();

            builder.OpenComponent<TooltipTrigger>(10);
            builder.AddAttribute(11, "Id", "trigger-two");
            builder.AddAttribute(12, "Delay", 0);
            builder.AddAttribute(13, "UseJsHover", false);
            builder.AddAttribute(14, "ChildContent", (RenderFragment)(child => child.AddContent(0, "Two")));
            builder.CloseComponent();

            builder.OpenComponent<TooltipPortal>(20);
            builder.AddAttribute(21, "KeepMounted", true);
            builder.AddAttribute(22, "ChildContent", (RenderFragment)(portalBuilder =>
            {
                portalBuilder.OpenComponent<TooltipPositioner>(0);
                portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(positionerBuilder =>
                {
                    positionerBuilder.OpenComponent<TooltipPopup>(0);
                    positionerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<TooltipViewport>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(child => child.AddContent(0, "Content")));
                        popupBuilder.CloseComponent();
                    }));
                    positionerBuilder.CloseComponent();
                }));
                portalBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render<TooltipRoot>(parameters => parameters.Add(root => root.ChildContent, content));

        cut.Find("#trigger-one").MouseEnter();
        cut.Find("#trigger-two").MouseEnter();

        JSInterop.Invocations.Any(invocation => invocation.Identifier == "prepareTransition").ShouldBeTrue();
        cut.Find("[data-current]").ParentElement!.HasAttribute("data-instant").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreateViewportInRoot(
            classValue: _ => "viewport-class"
        ));

        var currentContainer = cut.Find("[data-current]");
        currentContainer.ParentElement!.GetAttribute("class")!.ShouldContain("viewport-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreateViewportInRoot(
            styleValue: _ => "overflow: hidden"
        ));

        var currentContainer = cut.Find("[data-current]");
        currentContainer.ParentElement!.GetAttribute("style")!.ShouldContain("overflow: hidden");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<TooltipViewport>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task TransitionCallbacksUpdateStateOnRendererDispatcher()
    {
        var cut = Render(CreateViewportInRoot());
        var viewport = cut.FindComponent<TooltipViewport>();

        await viewport.Instance.OnTransitionStarted("right");

        var element = cut.Find("[data-current]").ParentElement!;
        element.GetAttribute("data-activation-direction").ShouldBe("right");
        element.HasAttribute("data-transitioning").ShouldBeTrue();

        await viewport.Instance.OnTransitionEnded();

        element = cut.Find("[data-current]").ParentElement!;
        element.HasAttribute("data-activation-direction").ShouldBeFalse();
        element.HasAttribute("data-transitioning").ShouldBeFalse();
    }
}
