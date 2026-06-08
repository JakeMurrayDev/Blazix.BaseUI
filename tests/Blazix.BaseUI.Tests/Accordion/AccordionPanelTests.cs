namespace Blazix.BaseUI.Tests.Accordion;

public class AccordionPanelTests : BunitContext, IAccordionPanelContract
{
    public AccordionPanelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupAccordionModules(JSInterop);
    }

    private RenderFragment CreateAccordionWithPanel(
        string itemValue = "test-item",
        bool itemDisabled = false,
        bool keepMounted = true,
        bool rootHiddenUntilFound = false,
        bool? panelHiddenUntilFound = null,
        string[]? defaultValue = null,
        Orientation orientation = Orientation.Vertical,
        Func<AccordionPanelState, string>? classValue = null,
        Func<AccordionPanelState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<AccordionPanelState>>? render = null)
    {
        return builder =>
        {
            builder.OpenComponent<AccordionRoot<string>>(0);
            builder.AddAttribute(1, "DefaultValue", defaultValue ?? Array.Empty<string>());
            builder.AddAttribute(2, "Orientation", orientation);
            builder.AddAttribute(3, "HiddenUntilFound", rootHiddenUntilFound);
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(innerBuilder =>
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
                        headerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(tb => tb.AddContent(0, "Trigger")));
                        headerBuilder.CloseComponent();
                    }));
                    itemBuilder.CloseComponent();

                    itemBuilder.OpenComponent<AccordionPanel>(2);
                    itemBuilder.AddAttribute(3, "KeepMounted", keepMounted);
                    if (panelHiddenUntilFound.HasValue)
                        itemBuilder.AddAttribute(4, "HiddenUntilFound", panelHiddenUntilFound.Value);
                    if (classValue is not null)
                        itemBuilder.AddAttribute(5, "ClassValue", classValue);
                    if (styleValue is not null)
                        itemBuilder.AddAttribute(6, "StyleValue", styleValue);
                    if (additionalAttributes is not null)
                        itemBuilder.AddAttribute(7, "AdditionalAttributes", additionalAttributes);
                    if (render is not null)
                        itemBuilder.AddAttribute(8, "Render", render);
                    itemBuilder.AddAttribute(9, "ChildContent", (RenderFragment)(pb => pb.AddContent(0, "Panel Content")));
                    itemBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateAccordionWithPanel(defaultValue: ["test-item"]));

        var panel = cut.Find("div[role='region'][id]");
        panel.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateAccordionWithPanel(
            defaultValue: ["test-item"],
            render: ctx => builder =>
            {
                builder.OpenElement(0, "section");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));

        var panel = cut.Find("section[role='region']");
        panel.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateAccordionWithPanel(
            defaultValue: ["test-item"],
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "accordion-panel" },
                { "aria-label", "Panel" }
            }
        ));

        cut.Markup.ShouldContain("data-testid=\"accordion-panel\"");
        cut.Markup.ShouldContain("aria-label=\"Panel\"");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateAccordionWithPanel(
            defaultValue: ["test-item"],
            classValue: _ => "custom-panel-class"
        ));

        cut.Markup.ShouldContain("custom-panel-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateAccordionWithPanel(
            defaultValue: ["test-item"],
            styleValue: _ => "padding: 20px"
        ));

        cut.Markup.ShouldContain("padding: 20px");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRoleRegion()
    {
        var cut = Render(CreateAccordionWithPanel(defaultValue: ["test-item"]));

        var panel = cut.Find("div[role='region']");
        panel.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaLabelledbyPointingToTrigger()
    {
        var cut = Render(CreateAccordionWithPanel(defaultValue: ["test-item"]));

        var trigger = cut.Find("button");
        trigger.HasAttribute("id").ShouldBeTrue();

        var panel = cut.Find("div[role='region'][id]");
        panel.HasAttribute("aria-labelledby").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasIdMatchingTriggerAriaControls()
    {
        var cut = Render(CreateAccordionWithPanel(defaultValue: ["test-item"]));

        var trigger = cut.Find("button");
        var ariaControls = trigger.GetAttribute("aria-controls");

        var panel = cut.Find("div[role='region'][id]");
        panel.GetAttribute("id").ShouldBe(ariaControls);

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreateAccordionWithPanel(defaultValue: ["test-item"]));

        var panel = cut.Find("div[role='region'][data-open]");
        panel.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreateAccordionWithPanel());

        var panel = cut.Find("div[role='region'][data-closed]");
        panel.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateAccordionWithPanel(itemDisabled: true));

        var panel = cut.Find("div[role='region'][data-disabled]");
        panel.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataIndexAttribute()
    {
        var cut = Render(CreateAccordionWithPanel(defaultValue: ["test-item"]));

        var panel = cut.Find("div[role='region'][data-index]");
        panel.ShouldNotBeNull();
        panel.HasAttribute("data-index").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientationAttribute()
    {
        var cut = Render(CreateAccordionWithPanel(defaultValue: ["test-item"], orientation: Orientation.Horizontal));

        var panel = cut.Find("div[role='region']");
        panel.GetAttribute("data-orientation").ShouldBe("horizontal");

        return Task.CompletedTask;
    }

    [Fact]
    public Task IsHiddenWhenClosed()
    {
        var cut = Render(CreateAccordionWithPanel(keepMounted: false));

        var panels = cut.FindAll("div[role='region'][id]");
        panels.Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task IsHiddenWhenKeptMountedAndClosed()
    {
        var cut = Render(CreateAccordionWithPanel(keepMounted: true));

        var panel = cut.Find("div[role='region'][id]");
        panel.HasAttribute("hidden").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesHiddenUntilFoundWhenClosed()
    {
        var cut = Render(CreateAccordionWithPanel(
            keepMounted: false,
            rootHiddenUntilFound: true));

        var panel = cut.Find("div[role='region'][id]");
        panel.GetAttribute("hidden").ShouldBe("until-found");

        return Task.CompletedTask;
    }

    [Fact]
    public Task IsVisibleWhenOpen()
    {
        var cut = Render(CreateAccordionWithPanel(defaultValue: ["test-item"]));

        var panel = cut.Find("div[role='region']");
        panel.HasAttribute("hidden").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task KeepsMountedWhenKeepMountedTrue()
    {
        var cut = Render(CreateAccordionWithPanel(keepMounted: true));

        var panels = cut.FindAll("div[role='region']");
        panels.Count.ShouldBeGreaterThan(0);

        cut.Markup.ShouldContain("Panel Content");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task OpensFromBeforeMatchWhenItemIsDisabled()
    {
        var cut = Render(CreateAccordionWithPanel(
            itemDisabled: true,
            keepMounted: false,
            rootHiddenUntilFound: true));

        var panelComponent = cut.FindComponent<AccordionPanel>();

        await panelComponent.Instance.OnBeforeMatch();

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        var panel = cut.Find("div[role='region'][id]");
        panel.HasAttribute("data-open").ShouldBeTrue();
    }

    [Fact]
    public Task HasIdleTransitionStateWhenInitiallyOpen()
    {
        TransitionStatus? transitionStatus = null;

        Render(CreateAccordionWithPanel(
            defaultValue: ["test-item"],
            render: ctx => builder =>
            {
                transitionStatus = ctx.State.TransitionStatus;
                builder.OpenElement(0, "section");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }));

        transitionStatus.ShouldBe(TransitionStatus.Idle);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task UpdatesTransitionStyleAttributesFromJsCallback()
    {
        var cut = Render(CreateAccordionWithPanel(defaultValue: ["test-item"]));
        var panelComponent = cut.FindComponent<AccordionPanel>();

        await panelComponent.InvokeAsync(() => panelComponent.Instance.OnTransitionStatusChanged("starting", 120, null));

        var panel = cut.Find("div[role='region'][id]");
        panel.HasAttribute("data-starting-style").ShouldBeTrue();
        panel.HasAttribute("data-ending-style").ShouldBeFalse();

        await panelComponent.InvokeAsync(() => panelComponent.Instance.OnTransitionStatusChanged("ending", null, 240));

        panel = cut.Find("div[role='region'][id]");
        panel.HasAttribute("data-ending-style").ShouldBeTrue();
        panel.HasAttribute("data-starting-style").ShouldBeFalse();

        await panelComponent.InvokeAsync(() => panelComponent.Instance.OnTransitionStatusChanged("idle", null, null));

        panel = cut.Find("div[role='region'][id]");
        panel.HasAttribute("data-starting-style").ShouldBeFalse();
        panel.HasAttribute("data-ending-style").ShouldBeFalse();
    }

    [Fact]
    public Task KeepsPanelAndTriggerIdsSynchronizedWhenPanelRendersBeforeTrigger()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<AccordionRoot<string>>(0);
            builder.AddAttribute(1, "DefaultValue", new[] { "test-item" });
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(rootBuilder =>
            {
                rootBuilder.OpenComponent<AccordionItem<string>>(0);
                rootBuilder.AddAttribute(1, "Value", "test-item");
                rootBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<AccordionPanel>(0);
                    itemBuilder.AddAttribute(1, "KeepMounted", true);
                    itemBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(panelBuilder => panelBuilder.AddContent(0, "Panel Content")));
                    itemBuilder.CloseComponent();

                    itemBuilder.OpenComponent<AccordionHeader>(3);
                    itemBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(headerBuilder =>
                    {
                        headerBuilder.OpenComponent<AccordionTrigger>(0);
                        headerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(triggerBuilder => triggerBuilder.AddContent(0, "Trigger")));
                        headerBuilder.CloseComponent();
                    }));
                    itemBuilder.CloseComponent();
                }));
                rootBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var trigger = cut.Find("button");
        var panel = cut.Find("div[role='region'][id]");

        panel.GetAttribute("aria-labelledby").ShouldBe(trigger.GetAttribute("id"));
        trigger.GetAttribute("aria-controls").ShouldBe(panel.GetAttribute("id"));

        return Task.CompletedTask;
    }
}
