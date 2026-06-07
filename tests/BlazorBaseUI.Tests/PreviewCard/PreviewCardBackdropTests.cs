using BlazorBaseUI.Tests.Contracts.PreviewCard;
using BlazorBaseUI.Tests.Infrastructure;
using BlazorBaseUI.PreviewCard;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tests.PreviewCard;

public class PreviewCardBackdropTests : BunitContext, IPreviewCardBackdropContract
{
    public PreviewCardBackdropTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPreviewCardModule(JSInterop);
    }

    private RenderFragment CreateBackdropInRoot(
        bool defaultOpen = true,
        RenderFragment<RenderProps<PreviewCardBackdropState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<PreviewCardBackdropState, string>? classValue = null,
        Func<PreviewCardBackdropState, string>? styleValue = null)
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
                    portalBuilder.OpenComponent<PreviewCardBackdrop>(0);
                    if (render is not null)
                        portalBuilder.AddAttribute(1, "Render", render);
                    if (classValue is not null)
                        portalBuilder.AddAttribute(2, "ClassValue", classValue);
                    if (styleValue is not null)
                        portalBuilder.AddAttribute(3, "StyleValue", styleValue);
                    if (additionalAttributes is not null)
                        portalBuilder.AddMultipleAttributes(4, additionalAttributes);
                    portalBuilder.CloseComponent();

                    portalBuilder.OpenComponent<PreviewCardPositioner>(10);
                    portalBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PreviewCardPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
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
        var cut = Render(CreateBackdropInRoot());

        // Find the backdrop by role=presentation that is NOT the positioner
        var backdrops = cut.FindAll("[role='presentation']");
        var backdrop = backdrops.First(e => !e.HasAttribute("data-side"));
        backdrop.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<PreviewCardBackdropState>> render = props => builder =>
        {
            builder.OpenElement(0, "section");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateBackdropInRoot(render: render));

        var backdrop = cut.Find("section[role='presentation']");
        backdrop.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateBackdropInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "backdrop" }
            }
        ));

        var backdrop = cut.Find("[data-testid='backdrop']");
        backdrop.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRolePresentation()
    {
        var cut = Render(CreateBackdropInRoot());

        var backdrops = cut.FindAll("[role='presentation']");
        var backdrop = backdrops.First(e => !e.HasAttribute("data-side"));
        backdrop.GetAttribute("role").ShouldBe("presentation");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreateBackdropInRoot(defaultOpen: true));

        var backdrops = cut.FindAll("[role='presentation']");
        var backdrop = backdrops.First(e => !e.HasAttribute("data-side"));
        backdrop.HasAttribute("data-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasPointerEventsNone()
    {
        var cut = Render(CreateBackdropInRoot());

        var backdrops = cut.FindAll("[role='presentation']");
        var backdrop = backdrops.First(e => !e.HasAttribute("data-side"));
        backdrop.GetAttribute("style")!.ShouldContain("pointer-events: none");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasNonSelectablePresentationStyles()
    {
        var cut = Render(CreateBackdropInRoot());

        var backdrops = cut.FindAll("[role='presentation']");
        var backdrop = backdrops.First(e => !e.HasAttribute("data-side"));
        var style = backdrop.GetAttribute("style")!;
        style.ShouldContain("user-select: none");
        style.ShouldContain("-webkit-user-select: none");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreateBackdropInRoot(
            classValue: _ => "backdrop-class"
        ));

        var backdrops = cut.FindAll("[role='presentation']");
        var backdrop = backdrops.First(e => !e.HasAttribute("data-side"));
        backdrop.GetAttribute("class")!.ShouldContain("backdrop-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreateBackdropInRoot(
            styleValue: _ => "background: rgba(0,0,0,0.5)"
        ));

        var backdrops = cut.FindAll("[role='presentation']");
        var backdrop = backdrops.First(e => !e.HasAttribute("data-side"));
        backdrop.GetAttribute("style")!.ShouldContain("background: rgba(0,0,0,0.5)");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<PreviewCardBackdrop>();

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
