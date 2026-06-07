using BlazorBaseUI.Tests.Contracts.PreviewCard;
using BlazorBaseUI.Tests.Infrastructure;
using BlazorBaseUI.PreviewCard;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tests.PreviewCard;

public class PreviewCardViewportTests : BunitContext, IPreviewCardViewportContract
{
    public PreviewCardViewportTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPreviewCardModule(JSInterop);
    }

    private RenderFragment CreateViewportInRoot(
        bool defaultOpen = true,
        RenderFragment<RenderProps<PreviewCardViewportState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<PreviewCardViewportState, string>? classValue = null,
        Func<PreviewCardViewportState, string>? styleValue = null)
    {
        return builder =>
        {
            builder.OpenComponent<PreviewCardRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<PreviewCardTrigger>(0);
                innerBuilder.AddAttribute(1, "Delay", 0);
                innerBuilder.AddAttribute(2, "CloseDelay", 0);
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PreviewCardPortal>(10);
                innerBuilder.AddAttribute(11, "KeepMounted", true);
                innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PreviewCardPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PreviewCardPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<PreviewCardViewport>(0);
                            if (render is not null)
                                popupBuilder.AddAttribute(1, "Render", render);
                            if (classValue is not null)
                                popupBuilder.AddAttribute(2, "ClassValue", classValue);
                            if (styleValue is not null)
                                popupBuilder.AddAttribute(3, "StyleValue", styleValue);
                            if (additionalAttributes is not null)
                                popupBuilder.AddMultipleAttributes(4, additionalAttributes);
                            popupBuilder.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Viewport Content")));
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
    public Task OmitsDataTransitioningWhenNotTransitioning()
    {
        var cut = Render(CreateViewportInRoot());

        var currentContainer = cut.Find("[data-current]");
        currentContainer.ParentElement!.HasAttribute("data-transitioning").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<PreviewCardViewportState>> render = props => builder =>
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
        var cut = Render<PreviewCardViewport>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
