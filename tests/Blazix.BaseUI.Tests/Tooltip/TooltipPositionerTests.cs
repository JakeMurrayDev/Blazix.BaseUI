using Blazix.BaseUI.Tests.Contracts.Tooltip;
using Blazix.BaseUI.Tests.Infrastructure;
using Blazix.BaseUI.Tooltip;
using Bunit;
using Microsoft.AspNetCore.Components;
using Side = Blazix.BaseUI.Side;
using Align = Blazix.BaseUI.Align;

namespace Blazix.BaseUI.Tests.Tooltip;

public class TooltipPositionerTests : BunitContext, ITooltipPositionerContract
{
    public TooltipPositionerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupTooltipModule(JSInterop);
    }

    private RenderFragment CreatePositionerInRoot(
        bool defaultOpen = true,
        Side side = Side.Top,
        Align align = Align.Center,
        RenderFragment<RenderProps<TooltipPositionerState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<TooltipPositionerState, string>? classValue = null,
        Func<TooltipPositionerState, string>? styleValue = null)
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
                    var attrIndex = 1;
                    portalBuilder.AddAttribute(attrIndex++, "Side", side);
                    portalBuilder.AddAttribute(attrIndex++, "Align", align);
                    if (render is not null)
                        portalBuilder.AddAttribute(attrIndex++, "Render", render);
                    if (classValue is not null)
                        portalBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                    if (styleValue is not null)
                        portalBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                    if (additionalAttributes is not null)
                        portalBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);
                    portalBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<TooltipPopup>(0);
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
        RenderFragment<RenderProps<TooltipPositionerState>> render = props => builder =>
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
    public Task CascadesPositionerContext()
    {
        var cut = Render(CreatePositionerInRoot(side: Side.Right, align: Align.End));

        // The popup should receive the positioner context and have matching data attributes
        var popup = cut.Find("[role='tooltip']");
        popup.GetAttribute("data-side").ShouldBe("right");
        popup.GetAttribute("data-align").ShouldBe("end");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<TooltipPositioner>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
