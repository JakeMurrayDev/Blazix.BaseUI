using BlazorBaseUI.Tests.Contracts.PreviewCard;
using BlazorBaseUI.Tests.Infrastructure;
using BlazorBaseUI.PreviewCard;
using Bunit;
using Microsoft.AspNetCore.Components;
using Side = BlazorBaseUI.Side;
using Align = BlazorBaseUI.Align;

namespace BlazorBaseUI.Tests.PreviewCard;

public class PreviewCardArrowTests : BunitContext, IPreviewCardArrowContract
{
    public PreviewCardArrowTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPreviewCardModule(JSInterop);
    }

    private RenderFragment CreateArrowInRoot(
        bool defaultOpen = true,
        Side side = Side.Bottom,
        Align align = Align.Center,
        RenderFragment<RenderProps<PreviewCardArrowState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<PreviewCardArrowState, string>? classValue = null,
        Func<PreviewCardArrowState, string>? styleValue = null)
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
                    portalBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PreviewCardPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<PreviewCardArrow>(0);
                            if (render is not null)
                                popupBuilder.AddAttribute(1, "Render", render);
                            if (classValue is not null)
                                popupBuilder.AddAttribute(2, "ClassValue", classValue);
                            if (styleValue is not null)
                                popupBuilder.AddAttribute(3, "StyleValue", styleValue);
                            if (additionalAttributes is not null)
                                popupBuilder.AddMultipleAttributes(4, additionalAttributes);
                            popupBuilder.CloseComponent();

                            popupBuilder.AddContent(10, "Content");
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
        var cut = Render(CreateArrowInRoot());

        var arrow = cut.Find("[aria-hidden='true'][data-side]");
        arrow.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<PreviewCardArrowState>> render = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateArrowInRoot(render: render));

        var arrow = cut.Find("span[aria-hidden='true']");
        arrow.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateArrowInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "arrow" }
            }
        ));

        var arrow = cut.Find("[aria-hidden='true'][data-side]");
        arrow.GetAttribute("data-testid").ShouldBe("arrow");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaHiddenTrue()
    {
        var cut = Render(CreateArrowInRoot());

        var arrow = cut.Find("[aria-hidden='true'][data-side]");
        arrow.GetAttribute("aria-hidden").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataSideAttribute()
    {
        var cut = Render(CreateArrowInRoot(side: Side.Bottom));

        var arrow = cut.Find("[aria-hidden='true'][data-side]");
        arrow.GetAttribute("data-side").ShouldBe("bottom");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataAlignAttribute()
    {
        var cut = Render(CreateArrowInRoot(align: Align.Start));

        var arrow = cut.Find("[aria-hidden='true'][data-side]");
        arrow.GetAttribute("data-align").ShouldBe("start");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreateArrowInRoot(defaultOpen: true));

        var arrow = cut.Find("[aria-hidden='true'][data-side]");
        arrow.HasAttribute("data-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreateArrowInRoot(defaultOpen: false));

        var arrow = cut.Find("[aria-hidden='true'][data-side]");
        arrow.HasAttribute("data-closed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task DoesNotRenderDataInstant()
    {
        var cut = Render(CreateArrowInRoot(defaultOpen: false));
        var root = cut.FindComponent<PreviewCardRoot>();

        await cut.InvokeAsync(() => root.Instance.SetOpenAsync(true, PreviewCardOpenChangeReason.TriggerFocus, null));

        var arrow = cut.Find("[aria-hidden='true'][data-side]");
        arrow.HasAttribute("data-instant").ShouldBeFalse();
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreateArrowInRoot(
            classValue: _ => "arrow-class"
        ));

        var arrow = cut.Find("[aria-hidden='true'][data-side]");
        arrow.GetAttribute("class")!.ShouldContain("arrow-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreateArrowInRoot(
            styleValue: _ => "width: 10px"
        ));

        var arrow = cut.Find("[aria-hidden='true'][data-side]");
        arrow.GetAttribute("style")!.ShouldContain("width: 10px");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<PreviewCardArrow>();

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
