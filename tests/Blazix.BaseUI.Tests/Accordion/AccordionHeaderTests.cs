namespace Blazix.BaseUI.Tests.Accordion;

public class AccordionHeaderTests : BunitContext, IAccordionHeaderContract
{
    public AccordionHeaderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupAccordionModules(JSInterop);
    }

    private RenderFragment CreateAccordionWithHeader(
        string itemValue = "test-item",
        bool itemDisabled = false,
        string[]? defaultValue = null,
        Orientation orientation = Orientation.Vertical,
        Func<AccordionHeaderState, string>? classValue = null,
        Func<AccordionHeaderState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<AccordionHeaderState>>? render = null)
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
                    if (classValue is not null)
                        itemBuilder.AddAttribute(1, "ClassValue", classValue);
                    if (styleValue is not null)
                        itemBuilder.AddAttribute(2, "StyleValue", styleValue);
                    if (additionalAttributes is not null)
                        itemBuilder.AddAttribute(3, "AdditionalAttributes", additionalAttributes);
                    if (render is not null)
                        itemBuilder.AddAttribute(4, "Render", render);
                    itemBuilder.AddAttribute(5, "ChildContent", (RenderFragment)(b =>
                    {
                        b.OpenComponent<AccordionTrigger>(0);
                        b.AddAttribute(1, "ChildContent", (RenderFragment)(tb => tb.AddContent(0, "Trigger")));
                        b.CloseComponent();
                    }));
                    itemBuilder.CloseComponent();

                    itemBuilder.OpenComponent<AccordionPanel>(6);
                    itemBuilder.AddAttribute(7, "KeepMounted", true);
                    itemBuilder.AddAttribute(8, "ChildContent", (RenderFragment)(pb => pb.AddContent(0, "Panel Content")));
                    itemBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsH3ByDefault()
    {
        var cut = Render(CreateAccordionWithHeader());

        var header = cut.Find("h3");
        header.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateAccordionWithHeader(
            render: ctx => builder =>
            {
                builder.OpenElement(0, "h2");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));

        var header = cut.Find("h2[data-index]");
        header.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateAccordionWithHeader(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "accordion-header" },
                { "aria-label", "Header" }
            }
        ));

        cut.Markup.ShouldContain("data-testid=\"accordion-header\"");
        cut.Markup.ShouldContain("aria-label=\"Header\"");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateAccordionWithHeader(
            classValue: _ => "custom-header-class"
        ));

        cut.Markup.ShouldContain("custom-header-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateAccordionWithHeader(
            styleValue: _ => "font-weight: bold"
        ));

        cut.Markup.ShouldContain("font-weight: bold");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenParentDisabled()
    {
        var cut = Render(CreateAccordionWithHeader(itemDisabled: true));

        var header = cut.Find("h3[data-disabled]");
        header.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreateAccordionWithHeader(defaultValue: ["test-item"]));

        var header = cut.Find("h3[data-open]");
        header.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreateAccordionWithHeader());

        var header = cut.Find("h3[data-closed]");
        header.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataIndexAttribute()
    {
        var cut = Render(CreateAccordionWithHeader());

        var header = cut.Find("h3[data-index]");
        header.ShouldNotBeNull();
        header.HasAttribute("data-index").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientationAttribute()
    {
        var cut = Render(CreateAccordionWithHeader(orientation: Orientation.Horizontal));

        var header = cut.Find("h3");
        header.GetAttribute("data-orientation").ShouldBe("horizontal");

        return Task.CompletedTask;
    }
}
