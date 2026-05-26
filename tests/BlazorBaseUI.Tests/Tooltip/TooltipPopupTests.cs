using BlazorBaseUI.Tests.Contracts.Tooltip;
using BlazorBaseUI.Tests.Infrastructure;
using BlazorBaseUI.Tooltip;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tests.Tooltip;

public class TooltipPopupTests : BunitContext, ITooltipPopupContract
{
    public TooltipPopupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupTooltipModule(JSInterop);
    }

    private RenderFragment CreatePopupInRoot(
        bool defaultOpen = true,
        RenderFragment<RenderProps<TooltipPopupState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<TooltipPopupState, string>? classValue = null,
        Func<TooltipPopupState, string>? styleValue = null)
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
                        var attrIndex = 1;
                        if (render is not null)
                            posBuilder.AddAttribute(attrIndex++, "Render", render);
                        if (classValue is not null)
                            posBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                        if (styleValue is not null)
                            posBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                        if (additionalAttributes is not null)
                            posBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);
                        posBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
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

        var popup = cut.Find("[role='tooltip']");
        popup.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<TooltipPopupState>> render = props => builder =>
        {
            builder.OpenElement(0, "section");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreatePopupInRoot(render: render));

        var popup = cut.Find("section[role='tooltip']");
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
                { "aria-label", "Tooltip content" }
            }
        ));

        var popup = cut.Find("[role='tooltip']");
        popup.GetAttribute("data-testid").ShouldBe("popup");
        popup.GetAttribute("aria-label").ShouldBe("Tooltip content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRoleTooltip()
    {
        var cut = Render(CreatePopupInRoot());

        var popup = cut.Find("[role='tooltip']");
        popup.GetAttribute("role").ShouldBe("tooltip");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasFocusablePopupAttributes()
    {
        var cut = Render(CreatePopupInRoot());

        var popup = cut.Find("[role='tooltip']");
        popup.GetAttribute("tabindex").ShouldBe("-1");
        popup.HasAttribute("data-base-ui-focusable").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataSideAttribute()
    {
        var cut = Render(CreatePopupInRoot());

        var popup = cut.Find("[role='tooltip']");
        popup.HasAttribute("data-side").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataAlignAttribute()
    {
        var cut = Render(CreatePopupInRoot());

        var popup = cut.Find("[role='tooltip']");
        popup.HasAttribute("data-align").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreatePopupInRoot(defaultOpen: true));

        var popup = cut.Find("[role='tooltip']");
        popup.HasAttribute("data-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreatePopupInRoot(defaultOpen: false));

        // With KeepMounted, the popup is still in DOM but has data-closed
        var popup = cut.Find("[role='tooltip']");
        popup.HasAttribute("data-closed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreatePopupInRoot(
            classValue: _ => "popup-class"
        ));

        var popup = cut.Find("[role='tooltip']");
        popup.GetAttribute("class")!.ShouldContain("popup-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreatePopupInRoot(
            styleValue: _ => "background: red"
        ));

        var popup = cut.Find("[role='tooltip']");
        popup.GetAttribute("style")!.ShouldContain("background: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildren()
    {
        var cut = Render(CreatePopupInRoot());

        var popup = cut.Find("[role='tooltip']");
        popup.TextContent.ShouldContain("Content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<TooltipPopup>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
