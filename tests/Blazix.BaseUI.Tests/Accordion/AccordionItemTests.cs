namespace Blazix.BaseUI.Tests.Accordion;

public class AccordionItemTests : BunitContext, IAccordionItemContract
{
    public AccordionItemTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupAccordionModules(JSInterop);
    }

    private RenderFragment CreateAccordionWithItem(
        string itemValue = "test-item",
        bool itemDisabled = false,
        bool rootDisabled = false,
        bool keepMounted = true,
        string[]? defaultValue = null,
        Orientation orientation = Orientation.Vertical,
        Func<AccordionItemState<string>, string>? classValue = null,
        Func<AccordionItemState<string>, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<AccordionItemState<string>>>? render = null)
    {
        return builder =>
        {
            builder.OpenComponent<AccordionRoot<string>>(0);
            builder.AddAttribute(1, "DefaultValue", defaultValue ?? Array.Empty<string>());
            builder.AddAttribute(2, "Disabled", rootDisabled);
            builder.AddAttribute(3, "Orientation", orientation);
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<AccordionItem<string>>(0);
                innerBuilder.AddAttribute(1, "Value", itemValue);
                innerBuilder.AddAttribute(2, "Disabled", itemDisabled);
                if (classValue is not null)
                    innerBuilder.AddAttribute(3, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(4, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(5, "AdditionalAttributes", additionalAttributes);
                if (render is not null)
                    innerBuilder.AddAttribute(6, "Render", render);
                innerBuilder.AddAttribute(7, "ChildContent", CreateItemContent(keepMounted));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateItemContent(bool keepMounted = true)
    {
        return builder =>
        {
            builder.OpenComponent<AccordionHeader>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<AccordionTrigger>(0);
                b.AddAttribute(1, "ChildContent", (RenderFragment)(tb => tb.AddContent(0, "Trigger")));
                b.CloseComponent();
            }));
            builder.CloseComponent();

            builder.OpenComponent<AccordionPanel>(2);
            builder.AddAttribute(3, "KeepMounted", keepMounted);
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(pb => pb.AddContent(0, "Panel Content")));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateAccordionWithItem());

        var items = cut.FindAll("div[data-index]");
        items.Count.ShouldBeGreaterThan(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateAccordionWithItem(
            render: ctx => builder =>
            {
                builder.OpenElement(0, "section");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));

        var item = cut.Find("section[data-index]");
        item.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateAccordionWithItem(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "accordion-item" },
                { "aria-label", "Item" }
            }
        ));

        cut.Markup.ShouldContain("data-testid=\"accordion-item\"");
        cut.Markup.ShouldContain("aria-label=\"Item\"");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateAccordionWithItem(
            classValue: _ => "custom-item-class"
        ));

        cut.Markup.ShouldContain("custom-item-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateAccordionWithItem(
            styleValue: _ => "margin: 10px"
        ));

        cut.Markup.ShouldContain("margin: 10px");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreateAccordionWithItem(defaultValue: ["test-item"]));

        var item = cut.Find("div[data-index][data-open]");
        item.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreateAccordionWithItem());

        var item = cut.Find("div[data-index][data-closed]");
        item.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateAccordionWithItem(itemDisabled: true));

        var item = cut.Find("div[data-index][data-disabled]");
        item.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenRootDisabled()
    {
        var cut = Render(CreateAccordionWithItem(rootDisabled: true));

        var item = cut.Find("div[data-index][data-disabled]");
        item.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataIndexAttribute()
    {
        var cut = Render(CreateAccordionWithItem());

        var item = cut.Find("div[data-index]");
        item.ShouldNotBeNull();
        item.HasAttribute("data-index").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientationAttribute()
    {
        var cut = Render(CreateAccordionWithItem(orientation: Orientation.Horizontal));

        var item = cut.Find("div[data-index]");
        item.GetAttribute("data-orientation").ShouldBe("horizontal");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataHiddenWhenClosedAndUnmounted()
    {
        var cut = Render(CreateAccordionWithItem(keepMounted: false));

        var item = cut.Find("div[data-index]");
        item.HasAttribute("data-hidden").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesResolvedValueWhenValueParameterChanges()
    {
        var itemValue = "first";
        var rootValue = new[] { "second" };

        var cut = Render(builder =>
        {
            builder.OpenComponent<AccordionRoot<string>>(0);
            builder.AddAttribute(1, "Value", rootValue);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<AccordionItem<string>>(0);
                innerBuilder.AddAttribute(1, "Value", itemValue);
                innerBuilder.AddAttribute(2, "ChildContent", CreateItemContent());
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var item = cut.Find("div[data-index]");
        item.HasAttribute("data-closed").ShouldBeTrue();

        var itemComponent = cut.FindComponent<AccordionItem<string>>();
        itemValue = "second";
        itemComponent.Render(parameters => parameters.Add(p => p.Value, itemValue));

        item = cut.Find("div[data-index]");
        item.HasAttribute("data-open").ShouldBeTrue();

        return Task.CompletedTask;
    }
}
