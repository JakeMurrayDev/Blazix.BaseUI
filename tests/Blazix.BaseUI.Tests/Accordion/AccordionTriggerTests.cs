namespace Blazix.BaseUI.Tests.Accordion;

public class AccordionTriggerTests : BunitContext, IAccordionTriggerContract
{
    public AccordionTriggerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupAccordionModules(JSInterop);
    }

    private RenderFragment CreateAccordionWithTrigger(
        string itemValue = "test-item",
        bool itemDisabled = false,
        bool? triggerDisabled = null,
        bool nativeButton = true,
        string[]? defaultValue = null,
        Orientation orientation = Orientation.Vertical,
        Func<AccordionTriggerState, string>? classValue = null,
        Func<AccordionTriggerState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<AccordionTriggerState>>? render = null)
    {
        return builder =>
        {
            builder.OpenComponent<AccordionRoot<string>>(0);
            builder.AddAttribute(1, "DefaultValue", defaultValue ?? Array.Empty<string>());
            builder.AddAttribute(2, "Orientation", orientation);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<AccordionItem<string>>(0);
                innerBuilder.AddAttribute(1, "Value", itemValue);
                innerBuilder.AddAttribute(2, "Disabled", itemDisabled);
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<AccordionHeader>(0);
                    itemBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(headerBuilder =>
                    {
                        headerBuilder.OpenComponent<AccordionTrigger>(0);
                        headerBuilder.AddAttribute(1, "NativeButton", nativeButton);
                        if (triggerDisabled.HasValue)
                            headerBuilder.AddAttribute(2, "Disabled", triggerDisabled.Value);
                        if (classValue is not null)
                            headerBuilder.AddAttribute(3, "ClassValue", classValue);
                        if (styleValue is not null)
                            headerBuilder.AddAttribute(4, "StyleValue", styleValue);
                        if (additionalAttributes is not null)
                            headerBuilder.AddAttribute(5, "AdditionalAttributes", additionalAttributes);
                        if (render is not null)
                            headerBuilder.AddAttribute(6, "Render", render);
                        headerBuilder.AddAttribute(7, "ChildContent", (RenderFragment)(tb => tb.AddContent(0, "Trigger")));
                        headerBuilder.CloseComponent();
                    }));
                    itemBuilder.CloseComponent();

                    itemBuilder.OpenComponent<AccordionPanel>(2);
                    itemBuilder.AddAttribute(3, "KeepMounted", true);
                    itemBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(pb => pb.AddContent(0, "Panel Content")));
                    itemBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsButtonByDefault()
    {
        var cut = Render(CreateAccordionWithTrigger());

        var trigger = cut.Find("button");
        trigger.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateAccordionWithTrigger(
            nativeButton: false,
            render: ctx => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));

        var trigger = cut.Find("span[role='button']");
        trigger.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateAccordionWithTrigger(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "accordion-trigger" },
                { "aria-label", "Trigger" }
            }
        ));

        cut.Markup.ShouldContain("data-testid=\"accordion-trigger\"");
        cut.Markup.ShouldContain("aria-label=\"Trigger\"");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateAccordionWithTrigger(
            classValue: _ => "custom-trigger-class"
        ));

        cut.Markup.ShouldContain("custom-trigger-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateAccordionWithTrigger(
            styleValue: _ => "cursor: pointer"
        ));

        cut.Markup.ShouldContain("cursor: pointer");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedFalseWhenClosed()
    {
        var cut = Render(CreateAccordionWithTrigger());

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedTrueWhenOpen()
    {
        var cut = Render(CreateAccordionWithTrigger(defaultValue: ["test-item"]));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaControlsWhenOpen()
    {
        var cut = Render(CreateAccordionWithTrigger(defaultValue: ["test-item"]));

        var trigger = cut.Find("button");
        trigger.HasAttribute("aria-controls").ShouldBeTrue();

        var panelId = trigger.GetAttribute("aria-controls");
        panelId.ShouldNotBeNullOrEmpty();

        var panel = cut.Find($"div[id='{panelId}']");
        panel.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataPanelOpenWhenOpen()
    {
        var cut = Render(CreateAccordionWithTrigger(defaultValue: ["test-item"]));

        var trigger = cut.Find("button[data-panel-open]");
        trigger.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateAccordionWithTrigger(itemDisabled: true));

        var trigger = cut.Find("button[data-disabled]");
        trigger.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task TriggerDisabledFalseDoesNotOverrideDisabledItem()
    {
        var cut = Render(CreateAccordionWithTrigger(itemDisabled: true, triggerDisabled: false));

        var trigger = cut.Find("button[data-disabled]");
        trigger.GetAttribute("aria-disabled").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataIndexAndOrientation()
    {
        var cut = Render(CreateAccordionWithTrigger(orientation: Orientation.Horizontal));

        var trigger = cut.Find("button");
        trigger.GetAttribute("data-index").ShouldBe("0");
        trigger.GetAttribute("data-orientation").ShouldBe("horizontal");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTypeButtonWhenNativeButton()
    {
        var cut = Render(CreateAccordionWithTrigger(nativeButton: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("type").ShouldBe("button");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRoleButtonWhenNotNativeButton()
    {
        var cut = Render(CreateAccordionWithTrigger(
            nativeButton: false,
            render: ctx => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));

        var trigger = cut.Find("span[role='button']");
        trigger.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task TogglesOnClick()
    {
        var cut = Render(CreateAccordionWithTrigger());

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");
        trigger.HasAttribute("data-panel-open").ShouldBeFalse();

        trigger.Click();

        trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");
        trigger.HasAttribute("data-panel-open").ShouldBeTrue();

        trigger.Click();

        trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");
        trigger.HasAttribute("data-panel-open").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledTriggerIgnoresClick()
    {
        var cut = Render(CreateAccordionWithTrigger(itemDisabled: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        trigger.Click();

        trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }
}
