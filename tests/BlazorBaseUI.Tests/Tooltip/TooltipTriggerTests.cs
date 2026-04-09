using BlazorBaseUI.Tests.Contracts.Tooltip;
using BlazorBaseUI.Tests.Infrastructure;
using BlazorBaseUI.Tooltip;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tests.Tooltip;

public class TooltipTriggerTests : BunitContext, ITooltipTriggerContract
{
    public TooltipTriggerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupTooltipModule(JSInterop);
    }

    private RenderFragment CreateTriggerInRoot(
        bool defaultOpen = false,
        bool triggerDisabled = false,
        RenderFragment<RenderProps<TooltipTriggerState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<TooltipTriggerState, string>? classValue = null,
        Func<TooltipTriggerState, string>? styleValue = null,
        bool includePositioner = true)
    {
        return builder =>
        {
            builder.OpenComponent<TooltipRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<TooltipTrigger>(0);
                var attrIndex = 1;

                if (triggerDisabled)
                    innerBuilder.AddAttribute(attrIndex++, "Disabled", true);
                if (render is not null)
                    innerBuilder.AddAttribute(attrIndex++, "Render", render);
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);
                innerBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                if (includePositioner)
                {
                    innerBuilder.OpenComponent<TooltipPortal>(10);
                    innerBuilder.AddAttribute(11, "KeepMounted", true);
                    innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(portalBuilder =>
                    {
                        portalBuilder.OpenComponent<TooltipPositioner>(0);
                        portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                        {
                            posBuilder.OpenComponent<TooltipPopup>(0);
                            posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                            posBuilder.CloseComponent();
                        }));
                        portalBuilder.CloseComponent();
                    }));
                    innerBuilder.CloseComponent();
                }
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsButtonByDefault()
    {
        var cut = Render(CreateTriggerInRoot());

        var trigger = cut.Find("button");
        trigger.TagName.ShouldBe("BUTTON");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<TooltipTriggerState>> render = props => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateTriggerInRoot(render: render));

        var trigger = cut.Find("div[id]");
        trigger.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateTriggerInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "trigger" },
                { "aria-label", "Open tooltip" }
            }
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("data-testid").ShouldBe("trigger");
        trigger.GetAttribute("aria-label").ShouldBe("Open tooltip");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaDescribedByWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true));

        var trigger = cut.Find("button");
        var ariaDescribedBy = trigger.GetAttribute("aria-describedby");
        ariaDescribedBy.ShouldNotBeNullOrEmpty();

        // Verify the tooltip popup has the matching id
        var popup = cut.Find("[role='tooltip']");
        popup.GetAttribute("id").ShouldBe(ariaDescribedBy);

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataPopupOpenWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-popup-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDisabledAttributeWhenDisabled()
    {
        var cut = Render(CreateTriggerInRoot(triggerDisabled: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-trigger-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotOpenWhenDisabled()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false, triggerDisabled: true));

        var trigger = cut.Find("button");
        trigger.Focus();

        // Should not open
        cut.FindAll("[role='tooltip'][data-open]").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreateTriggerInRoot(
            defaultOpen: true,
            classValue: state => state.Open ? "open-class" : "closed-class"
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreateTriggerInRoot(
            styleValue: _ => "color: blue"
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("style")!.ShouldContain("color: blue");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<TooltipTrigger>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Trigger"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
