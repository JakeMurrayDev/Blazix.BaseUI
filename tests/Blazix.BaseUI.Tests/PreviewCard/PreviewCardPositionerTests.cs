using Blazix.BaseUI.Tests.Contracts.PreviewCard;
using Blazix.BaseUI.Tests.Infrastructure;
using Blazix.BaseUI.PreviewCard;
using Bunit;
using Microsoft.AspNetCore.Components;
using Side = Blazix.BaseUI.Side;
using Align = Blazix.BaseUI.Align;

namespace Blazix.BaseUI.Tests.PreviewCard;

public class PreviewCardPositionerTests : BunitContext, IPreviewCardPositionerContract
{
    public PreviewCardPositionerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPreviewCardModule(JSInterop);
    }

    private RenderFragment CreatePositionerInRoot(
        bool defaultOpen = true,
        Side side = Side.Bottom,
        Align align = Align.Center,
        RenderFragment<RenderProps<PreviewCardPositionerState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<PreviewCardPositionerState, string>? classValue = null,
        Func<PreviewCardPositionerState, string>? styleValue = null)
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
                    portalBuilder.AddAttribute(1, "Side", side);
                    portalBuilder.AddAttribute(2, "Align", align);
                    if (render is not null)
                        portalBuilder.AddAttribute(3, "Render", render);
                    if (classValue is not null)
                        portalBuilder.AddAttribute(4, "ClassValue", classValue);
                    if (styleValue is not null)
                        portalBuilder.AddAttribute(5, "StyleValue", styleValue);
                    if (additionalAttributes is not null)
                        portalBuilder.AddMultipleAttributes(6, additionalAttributes);
                    portalBuilder.AddAttribute(7, "ChildContent", (RenderFragment)(posBuilder =>
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
        var cut = Render(CreatePositionerInRoot());

        var positioner = cut.Find("[role='presentation']");
        positioner.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<PreviewCardPositionerState>> render = props => builder =>
        {
            builder.OpenElement(0, "section");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreatePositionerInRoot(render: render));

        var positioner = cut.Find("section[role='presentation']");
        positioner.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreatePositionerInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "positioner" },
                { "aria-label", "Positioner" }
            }
        ));

        var positioner = cut.Find("[role='presentation']");
        positioner.GetAttribute("data-testid").ShouldBe("positioner");
        positioner.GetAttribute("aria-label").ShouldBe("Positioner");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRolePresentation()
    {
        var cut = Render(CreatePositionerInRoot());

        var positioner = cut.Find("[role='presentation']");
        positioner.GetAttribute("role").ShouldBe("presentation");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataSideAttribute()
    {
        var cut = Render(CreatePositionerInRoot(side: Side.Bottom));

        var positioner = cut.Find("[role='presentation']");
        positioner.GetAttribute("data-side").ShouldBe("bottom");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataAlignAttribute()
    {
        var cut = Render(CreatePositionerInRoot(align: Align.Start));

        var positioner = cut.Find("[role='presentation']");
        positioner.GetAttribute("data-align").ShouldBe("start");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreatePositionerInRoot(defaultOpen: true));

        var positioner = cut.Find("[role='presentation']");
        positioner.HasAttribute("data-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreatePositionerInRoot(defaultOpen: false));

        var positioner = cut.Find("[role='presentation']");
        positioner.HasAttribute("data-closed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task DoesNotRenderDataInstant()
    {
        var cut = Render(CreatePositionerInRoot(defaultOpen: false));
        var root = cut.FindComponent<PreviewCardRoot>();

        await cut.InvokeAsync(() => root.Instance.SetOpenAsync(true, PreviewCardOpenChangeReason.TriggerFocus, null));

        var positioner = cut.Find("[role='presentation']");
        positioner.HasAttribute("data-instant").ShouldBeFalse();
    }

    [Fact]
    public Task HasHiddenWhenNotMounted()
    {
        var cut = Render(CreatePositionerInRoot(defaultOpen: false));

        var positioner = cut.Find("[role='presentation']");
        positioner.HasAttribute("hidden").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreatePositionerInRoot(
            classValue: _ => "positioner-class"
        ));

        var positioner = cut.Find("[role='presentation']");
        positioner.GetAttribute("class")!.ShouldContain("positioner-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreatePositionerInRoot(
            styleValue: _ => "z-index: 100"
        ));

        var positioner = cut.Find("[role='presentation']");
        positioner.GetAttribute("style")!.ShouldContain("z-index: 100");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesPointerEventsNoneWhenClosed()
    {
        var cut = Render(CreatePositionerInRoot(defaultOpen: false));

        var positioner = cut.Find("[role='presentation']");
        positioner.GetAttribute("style")!.ShouldContain("pointer-events: none");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesPositionerContext()
    {
        var cut = Render(CreatePositionerInRoot(side: Side.Right, align: Align.End));

        // The popup should receive the positioner context and have matching data attributes
        var popup = cut.Find("div[data-side][id]");
        popup.GetAttribute("data-side").ShouldBe("right");
        popup.GetAttribute("data-align").ShouldBe("end");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<PreviewCardPositioner>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
