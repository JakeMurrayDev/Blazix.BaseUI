using BlazorBaseUI.Tests.Contracts.PreviewCard;
using BlazorBaseUI.Tests.Infrastructure;
using BlazorBaseUI.PreviewCard;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tests.PreviewCard;

public class PreviewCardTriggerTests : BunitContext, IPreviewCardTriggerContract
{
    public PreviewCardTriggerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPreviewCardModule(JSInterop);
    }

    private RenderFragment CreateTriggerInRoot(
        bool defaultOpen = false,
        string? id = null,
        RenderFragment<RenderProps<PreviewCardTriggerState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<PreviewCardTriggerState, string>? classValue = null,
        Func<PreviewCardTriggerState, string>? styleValue = null,
        bool includePositioner = true)
    {
        return builder =>
        {
            builder.OpenComponent<PreviewCardRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<PreviewCardTrigger>(0);
                var attrIndex = 1;
                innerBuilder.AddAttribute(attrIndex++, "Delay", 0);
                innerBuilder.AddAttribute(attrIndex++, "CloseDelay", 0);
                if (id is not null)
                    innerBuilder.AddAttribute(attrIndex++, "Id", id);
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
                    innerBuilder.OpenComponent<PreviewCardPortal>(10);
                    innerBuilder.AddAttribute(11, "KeepMounted", true);
                    innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(portalBuilder =>
                    {
                        portalBuilder.OpenComponent<PreviewCardPositioner>(0);
                        portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                        {
                            posBuilder.OpenComponent<PreviewCardPopup>(0);
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
    public Task RendersAsAnchorByDefault()
    {
        var cut = Render(CreateTriggerInRoot());

        var trigger = cut.Find("a");
        trigger.TagName.ShouldBe("A");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<PreviewCardTriggerState>> render = props => builder =>
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
                { "aria-label", "Open preview" }
            }
        ));

        var trigger = cut.Find("a");
        trigger.GetAttribute("data-testid").ShouldBe("trigger");
        trigger.GetAttribute("aria-label").ShouldBe("Open preview");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataPopupOpenWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true));

        var trigger = cut.Find("a");
        trigger.HasAttribute("data-popup-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task InitializesJsHoverByDefaultWithTriggerId()
    {
        var cut = Render(CreateTriggerInRoot(id: "trigger-one"));

        cut.WaitForAssertion(() => JSInterop.Invocations.Any(invocation =>
            invocation.Identifier == "initializeHoverInteraction" &&
            invocation.Arguments.Count >= 2 &&
            Equals(invocation.Arguments[1], "trigger-one")).ShouldBeTrue());

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreateTriggerInRoot(
            defaultOpen: true,
            classValue: state => state.Open ? "open-class" : "closed-class"
        ));

        var trigger = cut.Find("a");
        trigger.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreateTriggerInRoot(
            styleValue: _ => "color: blue"
        ));

        var trigger = cut.Find("a");
        trigger.GetAttribute("style")!.ShouldContain("color: blue");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<PreviewCardTrigger>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Trigger"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
