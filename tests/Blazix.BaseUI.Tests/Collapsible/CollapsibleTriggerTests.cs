namespace Blazix.BaseUI.Tests.Collapsible;

public class CollapsibleTriggerTests : BunitContext, ICollapsibleTriggerContract
{
    public CollapsibleTriggerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupCollapsiblePanel(JSInterop);
    }

    private RenderFragment CreateTriggerInRoot(
        bool defaultOpen = false,
        bool disabled = false,
        Func<CollapsibleRootState, string>? classValue = null,
        Func<CollapsibleRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<CollapsibleRootState>>? render = null,
        bool includePanel = true,
        IReadOnlyDictionary<string, object>? panelAdditionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<CollapsibleRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            if (disabled)
                builder.AddAttribute(2, "Disabled", true);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<CollapsibleTrigger>(0);
                var attrIndex = 1;

                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                if (render is not null)
                    innerBuilder.AddAttribute(attrIndex++, "Render", render);
                innerBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                if (includePanel)
                {
                    innerBuilder.OpenComponent<CollapsiblePanel>(10);
                    var panelAttrIndex = 11;
                    innerBuilder.AddAttribute(panelAttrIndex++, "KeepMounted", true);
                    if (panelAdditionalAttributes is not null)
                        innerBuilder.AddAttribute(panelAttrIndex++, "AdditionalAttributes", panelAdditionalAttributes);
                    innerBuilder.AddAttribute(panelAttrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
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
        var cut = Render(CreateTriggerInRoot(
            render: ctx => builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));

        var trigger = cut.Find("div[aria-expanded]");
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
                { "aria-label", "Toggle panel" }
            }
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("data-testid").ShouldBe("trigger");
        trigger.GetAttribute("aria-label").ShouldBe("Toggle panel");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateTriggerInRoot(
            classValue: _ => "trigger-class"
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("class")!.ShouldContain("trigger-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateTriggerInRoot(
            styleValue: _ => "color: red"
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("style")!.ShouldContain("color: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedFalseWhenClosed()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedTrueWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaControlsWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true));

        var trigger = cut.Find("button");

        // aria-controls attribute should exist
        trigger.HasAttribute("aria-controls").ShouldBeTrue();

        // The panel should have an id attribute - find panel with id attribute
        var panel = cut.Find("div[id]");
        var panelId = panel.GetAttribute("id");
        panelId.ShouldNotBeNullOrEmpty();

        // Note: On initial render, trigger renders before panel, so aria-controls may be empty.
        // After re-render, they should match. Trigger a re-render by clicking the trigger twice.
        trigger.Click(); // close
        trigger.Click(); // open again

        var ariaControlsAfterRerender = trigger.GetAttribute("aria-controls");
        ariaControlsAfterRerender.ShouldNotBeNullOrEmpty();
        ariaControlsAfterRerender.ShouldBe(panelId);

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasNoAriaControlsWhenClosed()
    {
        // Per React source, aria-controls is only set when open
        var cut = Render(CreateTriggerInRoot(defaultOpen: false, includePanel: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("aria-controls").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataPanelOpenWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-panel-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasNoDataPanelOpenWhenClosed()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false));

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-panel-open").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateTriggerInRoot(disabled: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaDisabledWhenDisabled()
    {
        var cut = Render(CreateTriggerInRoot(disabled: true));

        var trigger = cut.Find("button");
        // CollapsibleTrigger always uses focusableWhenDisabled=true,
        // so it sets aria-disabled instead of the native disabled attribute
        // to keep the element focusable for screen readers.
        trigger.HasAttribute("disabled").ShouldBeFalse();
        trigger.GetAttribute("aria-disabled").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task TogglesOnClick()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        trigger.Click();

        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotToggleWhenDisabled()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false, disabled: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        trigger.Click();

        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ReceivesCorrectState()
    {
        CollapsibleRootState? capturedState = null;

        var cut = Render(CreateTriggerInRoot(
            defaultOpen: true,
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Open.ShouldBeTrue();
        capturedState.Disabled.ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ReferencesCustomPanelIdInAriaControls()
    {
        var cut = Render(CreateTriggerInRoot(
            defaultOpen: true,
            panelAdditionalAttributes: new Dictionary<string, object>
            {
                { "id", "custom-panel-id" }
            }
        ));

        var trigger = cut.Find("button");

        // Close and reopen to pick up panel ID registration
        trigger.Click();
        trigger.Click();

        trigger.GetAttribute("aria-controls").ShouldBe("custom-panel-id");

        var panel = cut.Find("#custom-panel-id");
        panel.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        // CollapsibleTrigger renders nothing when context is missing (doesn't throw)
        var cut = Render<CollapsibleTrigger>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Toggle"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
