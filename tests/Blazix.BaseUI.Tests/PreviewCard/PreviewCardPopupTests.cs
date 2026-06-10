using Blazix.BaseUI.Tests.Contracts.PreviewCard;
using Blazix.BaseUI.Tests.Infrastructure;
using Blazix.BaseUI.PreviewCard;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Tests.PreviewCard;

public class PreviewCardPopupTests : BunitContext, IPreviewCardPopupContract
{
    public PreviewCardPopupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPreviewCardModule(JSInterop);
    }

    private RenderFragment CreatePopupInRoot(
        bool defaultOpen = true,
        RenderFragment<RenderProps<PreviewCardPopupState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<PreviewCardPopupState, string>? classValue = null,
        Func<PreviewCardPopupState, string>? styleValue = null)
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
                        if (render is not null)
                            posBuilder.AddAttribute(1, "Render", render);
                        if (classValue is not null)
                            posBuilder.AddAttribute(2, "ClassValue", classValue);
                        if (styleValue is not null)
                            posBuilder.AddAttribute(3, "StyleValue", styleValue);
                        if (additionalAttributes is not null)
                            posBuilder.AddMultipleAttributes(4, additionalAttributes);
                        posBuilder.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
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
        var cut = Render(CreatePopupInRoot());

        var popup = cut.Find("div[data-side]");
        popup.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<PreviewCardPopupState>> render = props => builder =>
        {
            builder.OpenElement(0, "section");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreatePopupInRoot(render: render));

        var popup = cut.Find("section[data-side]");
        popup.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreatePopupInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "popup" },
                { "aria-label", "Preview content" }
            }
        ));

        var popup = cut.Find("div[data-side][id]");
        popup.GetAttribute("data-testid").ShouldBe("popup");
        popup.GetAttribute("aria-label").ShouldBe("Preview content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataSideAttribute()
    {
        var cut = Render(CreatePopupInRoot());

        var popup = cut.Find("div[data-side][id]");
        popup.HasAttribute("data-side").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataAlignAttribute()
    {
        var cut = Render(CreatePopupInRoot());

        var popup = cut.Find("div[data-side][id]");
        popup.HasAttribute("data-align").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreatePopupInRoot(defaultOpen: true));

        var popup = cut.Find("div[data-side][id]");
        popup.HasAttribute("data-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreatePopupInRoot(defaultOpen: false));

        // With KeepMounted, the popup is still in DOM but has data-closed
        var popup = cut.Find("div[data-side][id]");
        popup.HasAttribute("data-closed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasFloatingFocusableAttributes()
    {
        var cut = Render(CreatePopupInRoot(defaultOpen: true));

        var popup = cut.Find("div[data-side][id]");
        popup.GetAttribute("tabindex").ShouldBe("-1");
        popup.GetAttribute("data-floating-ui-focusable").ShouldBe("");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task DoesNotSetInstantDismissOnOutsidePressClose()
    {
        var cut = Render(CreatePopupInRoot(defaultOpen: true));
        var root = cut.FindComponent<PreviewCardRoot>();

        await cut.InvokeAsync(() => root.Instance.SetOpenAsync(false, PreviewCardOpenChangeReason.OutsidePress, null));

        var popup = cut.Find("div[data-side][id]");
        popup.HasAttribute("data-instant").ShouldBeFalse();
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreatePopupInRoot(
            classValue: _ => "popup-class"
        ));

        var popup = cut.Find("div[data-side][id]");
        popup.GetAttribute("class")!.ShouldContain("popup-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreatePopupInRoot(
            styleValue: _ => "background: red"
        ));

        var popup = cut.Find("div[data-side][id]");
        popup.GetAttribute("style")!.ShouldContain("background: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildren()
    {
        var cut = Render(CreatePopupInRoot());

        var popup = cut.Find("div[data-side][id]");
        popup.TextContent.ShouldContain("Content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<PreviewCardPopup>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
