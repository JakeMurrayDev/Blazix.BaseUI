namespace Blazix.BaseUI.Tests.Accordion;

public class AccordionRootTests : BunitContext, IAccordionRootContract
{
    private const string PanelContent1 = "Panel contents 1";
    private const string PanelContent2 = "Panel contents 2";

    public AccordionRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupAccordionModules(JSInterop);
    }

    private RenderFragment CreateAccordionRoot(
        string[]? value = null,
        string[]? defaultValue = null,
        bool disabled = false,
        bool multiple = false,
        Orientation orientation = Orientation.Vertical,
        bool loopFocus = true,
        bool keepMounted = false,
        EventCallback<AccordionValueChangeEventArgs<string>>? onValueChange = null,
        Func<AccordionRootState<string>, string>? classValue = null,
        Func<AccordionRootState<string>, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<AccordionRootState<string>>>? render = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<AccordionRoot<string>>(0);
            var attrIndex = 1;

            if (value is not null)
                builder.AddAttribute(attrIndex++, "Value", value);
            if (defaultValue is not null)
                builder.AddAttribute(attrIndex++, "DefaultValue", defaultValue);
            builder.AddAttribute(attrIndex++, "Disabled", disabled);
            builder.AddAttribute(attrIndex++, "Multiple", multiple);
            builder.AddAttribute(attrIndex++, "Orientation", orientation);
            builder.AddAttribute(attrIndex++, "LoopFocus", loopFocus);
            builder.AddAttribute(attrIndex++, "KeepMounted", keepMounted);
            if (onValueChange.HasValue)
                builder.AddAttribute(attrIndex++, "OnValueChange", onValueChange.Value);
            if (classValue is not null)
                builder.AddAttribute(attrIndex++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(attrIndex++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
            if (render is not null)
                builder.AddAttribute(attrIndex++, "Render", render);

            builder.AddAttribute(attrIndex++, "ChildContent", childContent ?? CreateDefaultItems());
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateDefaultItems(
        string? item1Value = "first",
        string? item2Value = "second",
        bool item1Disabled = false,
        bool item2Disabled = false,
        string? customPanelId = null)
    {
        return builder =>
        {
            builder.OpenComponent<AccordionItem<string>>(0);
            builder.AddAttribute(1, "Value", item1Value);
            if (item1Disabled)
                builder.AddAttribute(2, "Disabled", true);
            builder.AddAttribute(3, "AdditionalAttributes", new Dictionary<string, object> { { "data-testid", "item1" } });
            builder.AddAttribute(4, "ChildContent", CreateItemContent("Trigger 1", PanelContent1, customPanelId));
            builder.CloseComponent();

            builder.OpenComponent<AccordionItem<string>>(5);
            builder.AddAttribute(6, "Value", item2Value);
            if (item2Disabled)
                builder.AddAttribute(7, "Disabled", true);
            builder.AddAttribute(8, "AdditionalAttributes", new Dictionary<string, object> { { "data-testid", "item2" } });
            builder.AddAttribute(9, "ChildContent", CreateItemContent("Trigger 2", PanelContent2));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateItemContent(string triggerText, string panelContent, string? customPanelId = null)
    {
        return builder =>
        {
            builder.OpenComponent<AccordionHeader>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<AccordionTrigger>(0);
                b.AddAttribute(1, "ChildContent", (RenderFragment)(tb => tb.AddContent(0, triggerText)));
                b.CloseComponent();
            }));
            builder.CloseComponent();

            builder.OpenComponent<AccordionPanel>(2);
            builder.AddAttribute(3, "KeepMounted", true);
            if (customPanelId is not null)
                builder.AddAttribute(4, "AdditionalAttributes", new Dictionary<string, object> { { "id", customPanelId } });
            builder.AddAttribute(5, "ChildContent", (RenderFragment)(pb => pb.AddContent(0, panelContent)));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateAccordionRoot());

        var root = cut.Find("div[role='region']");
        root.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateAccordionRoot(
            render: ctx => builder =>
            {
                builder.OpenElement(0, "section");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));

        var section = cut.Find("section[role='region']");
        section.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateAccordionRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "accordion-root" },
                { "aria-label", "Accordion" }
            }
        ));

        cut.Markup.ShouldContain("data-testid=\"accordion-root\"");
        cut.Markup.ShouldContain("aria-label=\"Accordion\"");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateAccordionRoot(
            classValue: _ => "custom-accordion-class"
        ));

        cut.Markup.ShouldContain("custom-accordion-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateAccordionRoot(
            styleValue: _ => "background: red"
        ));

        cut.Markup.ShouldContain("background: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateAccordionRoot(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }
        ));

        cut.Markup.ShouldContain("static-class");
        cut.Markup.ShouldContain("dynamic-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersCorrectAriaAttributes()
    {
        var cut = Render(CreateAccordionRoot(defaultValue: ["first"]));

        var root = cut.Find("div[role='region']");
        root.ShouldNotBeNull();

        var trigger = cut.Find("button");
        trigger.HasAttribute("aria-controls").ShouldBeTrue();
        trigger.HasAttribute("id").ShouldBeTrue();

        var panel = cut.Find($"div[role='region'][id]");
        var panelId = panel.GetAttribute("id");

        trigger.GetAttribute("aria-controls").ShouldBe(panelId);
        panel.HasAttribute("aria-labelledby").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ReferencesManualPanelIdInTriggerAriaControls()
    {
        var cut = Render(CreateAccordionRoot(
            defaultValue: ["first"],
            childContent: CreateDefaultItems(customPanelId: "custom-panel-id")
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-controls").ShouldBe("custom-panel-id");

        var panel = cut.Find("div[id='custom-panel-id']");
        panel.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledOpenState()
    {
        var cut = Render(CreateAccordionRoot());

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        trigger.Click();

        trigger.GetAttribute("aria-expanded").ShouldBe("true");
        trigger.HasAttribute("data-panel-open").ShouldBeTrue();

        var panel = cut.Find($"div[data-open]");
        panel.ShouldNotBeNull();
        panel.TextContent.ShouldContain(PanelContent1);

        trigger.Click();

        trigger.GetAttribute("aria-expanded").ShouldBe("false");
        trigger.HasAttribute("data-panel-open").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledDefaultValueWithCustomItemValue()
    {
        var cut = Render(CreateAccordionRoot(defaultValue: ["first"]));

        var panel1 = cut.Find("div[data-open]");
        panel1.ShouldNotBeNull();
        panel1.TextContent.ShouldContain(PanelContent1);

        var closedPanels = cut.FindAll("div[data-closed][role='region']");
        closedPanels.Count.ShouldBe(1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledOpenState()
    {
        var cut = Render(CreateAccordionRoot(value: []));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        var cutOpen = Render(CreateAccordionRoot(value: ["first"]));

        var triggerOpen = cutOpen.Find("button");
        triggerOpen.GetAttribute("aria-expanded").ShouldBe("true");
        triggerOpen.HasAttribute("data-panel-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledValueWithCustomItemValue()
    {
        var cut = Render(CreateAccordionRoot(value: ["first"]));

        var panel1 = cut.Find("div[data-open]");
        panel1.ShouldNotBeNull();
        panel1.TextContent.ShouldContain(PanelContent1);

        var closedPanels = cut.FindAll("div[data-closed][role='region']");
        closedPanels.Count.ShouldBe(1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task CanDisableWholeAccordion()
    {
        var cut = Render(CreateAccordionRoot(defaultValue: ["first"], disabled: true));

        var item1 = cut.Find("[data-testid='item1']");
        var item2 = cut.Find("[data-testid='item2']");
        var headers = cut.FindAll("h3");
        var triggers = cut.FindAll("button");
        var panel1 = cut.Find($"div[role='region'][id]");

        item1.HasAttribute("data-disabled").ShouldBeTrue();
        item2.HasAttribute("data-disabled").ShouldBeTrue();

        foreach (var header in headers)
        {
            header.HasAttribute("data-disabled").ShouldBeTrue();
        }

        foreach (var trigger in triggers)
        {
            trigger.HasAttribute("data-disabled").ShouldBeTrue();
        }

        panel1.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CanDisableOneAccordionItem()
    {
        var cut = Render(CreateAccordionRoot(
            defaultValue: ["first"],
            childContent: CreateDefaultItems(item1Disabled: true)
        ));

        var item1 = cut.Find("[data-testid='item1']");
        var item2 = cut.Find("[data-testid='item2']");
        var headers = cut.FindAll("h3");
        var triggers = cut.FindAll("button");

        item1.HasAttribute("data-disabled").ShouldBeTrue();
        item2.HasAttribute("data-disabled").ShouldBeFalse();

        headers[0].HasAttribute("data-disabled").ShouldBeTrue();
        headers[1].HasAttribute("data-disabled").ShouldBeFalse();

        triggers[0].HasAttribute("data-disabled").ShouldBeTrue();
        triggers[1].HasAttribute("data-disabled").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task MultipleItemsCanBeOpenWhenMultipleTrue()
    {
        var cut = Render(CreateAccordionRoot(multiple: true));

        var triggers = cut.FindAll("button");

        triggers[0].HasAttribute("data-panel-open").ShouldBeFalse();
        triggers[1].HasAttribute("data-panel-open").ShouldBeFalse();

        triggers[0].Click();
        triggers[1].Click();

        triggers = cut.FindAll("button");
        triggers[0].HasAttribute("data-panel-open").ShouldBeTrue();
        triggers[1].HasAttribute("data-panel-open").ShouldBeTrue();

        var openPanels = cut.FindAll("div[data-open][role='region']");
        openPanels.Count.ShouldBe(2);

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnlyOneItemOpenWhenMultipleFalse()
    {
        var cut = Render(CreateAccordionRoot(multiple: false));

        var triggers = cut.FindAll("button");

        triggers[0].Click();

        triggers = cut.FindAll("button");
        triggers[0].HasAttribute("data-panel-open").ShouldBeTrue();

        triggers[1].Click();

        triggers = cut.FindAll("button");
        triggers[1].HasAttribute("data-panel-open").ShouldBeTrue();
        triggers[0].HasAttribute("data-panel-open").ShouldBeFalse();

        var openPanels = cut.FindAll("div[data-open][role='region']");
        openPanels.Count.ShouldBe(1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientationAttribute()
    {
        var cut = Render(CreateAccordionRoot(orientation: Orientation.Horizontal));

        var root = cut.Find("div[role='region']");
        root.GetAttribute("data-orientation").ShouldBe("horizontal");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChangeWithDefaultItemValue()
    {
        var callCount = 0;
        string[]? receivedValue = null;

        var cut = Render(CreateAccordionRoot(
            multiple: true,
            onValueChange: EventCallback.Factory.Create<AccordionValueChangeEventArgs<string>>(this, args =>
            {
                callCount++;
                receivedValue = args.Value;
            })
        ));

        callCount.ShouldBe(0);

        var triggers = cut.FindAll("button");
        triggers[0].Click();

        callCount.ShouldBe(1);
        receivedValue.ShouldBe(["first"]);

        triggers[1].Click();

        callCount.ShouldBe(2);
        receivedValue.ShouldBe(["first", "second"]);

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChangeWithCustomItemValue()
    {
        var callCount = 0;
        string[]? receivedValue = null;

        var cut = Render(CreateAccordionRoot(
            multiple: true,
            childContent: CreateDefaultItems(item1Value: "one", item2Value: "two"),
            onValueChange: EventCallback.Factory.Create<AccordionValueChangeEventArgs<string>>(this, args =>
            {
                callCount++;
                receivedValue = args.Value;
            })
        ));

        callCount.ShouldBe(0);

        var triggers = cut.FindAll("button");
        triggers[1].Click();

        callCount.ShouldBe(1);
        receivedValue.ShouldBe(["two"]);

        triggers[0].Click();

        callCount.ShouldBe(2);
        receivedValue.ShouldBe(["two", "one"]);

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChangeWhenMultipleFalse()
    {
        var callCount = 0;
        string[]? receivedValue = null;

        var cut = Render(CreateAccordionRoot(
            multiple: false,
            childContent: CreateDefaultItems(item1Value: "one", item2Value: "two"),
            onValueChange: EventCallback.Factory.Create<AccordionValueChangeEventArgs<string>>(this, args =>
            {
                callCount++;
                receivedValue = args.Value;
            })
        ));

        callCount.ShouldBe(0);

        var triggers = cut.FindAll("button");
        triggers[0].Click();

        callCount.ShouldBe(1);
        receivedValue.ShouldBe(["one"]);

        triggers[1].Click();

        callCount.ShouldBe(2);
        receivedValue.ShouldBe(["two"]);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task AsyncOnValueChangeCancellationPreventsStateChange()
    {
        var cut = Render(CreateAccordionRoot(
            onValueChange: EventCallback.Factory.Create<AccordionValueChangeEventArgs<string>>(this, async args =>
            {
                await Task.Yield();
                args.Cancel();
            })
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        await trigger.ClickAsync(new MouseEventArgs());

        trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");
        trigger.HasAttribute("data-panel-open").ShouldBeFalse();
    }

    [Fact]
    public Task CascadesContextToChildren()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<AccordionRoot<string>>(0);
            builder.AddAttribute(1, "DefaultValue", new[] { "first" });
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<AccordionItem<string>>(0);
                innerBuilder.AddAttribute(1, "Value", "first");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<AccordionHeader>(0);
                    itemBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(headerBuilder =>
                    {
                        headerBuilder.OpenComponent<AccordionTrigger>(0);
                        headerBuilder.AddAttribute(1, "ClassValue", (Func<AccordionTriggerState, string>)(state =>
                        {
                            return "trigger-class";
                        }));
                        headerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                        headerBuilder.CloseComponent();
                    }));
                    itemBuilder.CloseComponent();

                    itemBuilder.OpenComponent<AccordionPanel>(2);
                    itemBuilder.AddAttribute(3, "KeepMounted", true);
                    itemBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                    itemBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }
}
