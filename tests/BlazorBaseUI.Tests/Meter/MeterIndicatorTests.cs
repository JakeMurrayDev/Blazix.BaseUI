namespace BlazorBaseUI.Tests.Meter;

public class MeterIndicatorTests : BunitContext, IMeterIndicatorContract
{
    public MeterIndicatorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateMeterWithIndicator(
        double value = 50,
        double min = 0,
        double max = 100,
        Func<MeterRootState, string?>? indicatorClassValue = null,
        Func<MeterRootState, string?>? indicatorStyleValue = null,
        IReadOnlyDictionary<string, object>? indicatorAttributes = null,
        RenderFragment<RenderProps<MeterRootState>>? indicatorRender = null)
    {
        return builder =>
        {
            builder.OpenComponent<MeterRoot>(0);
            var attrIndex = 1;

            builder.AddAttribute(attrIndex++, "Value", value);
            builder.AddAttribute(attrIndex++, "Min", min);
            builder.AddAttribute(attrIndex++, "Max", max);
            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<MeterTrack>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(trackBuilder =>
                {
                    trackBuilder.OpenComponent<MeterIndicator>(0);
                    var indicatorAttrIndex = 1;

                    if (indicatorClassValue is not null)
                        trackBuilder.AddAttribute(indicatorAttrIndex++, "ClassValue", indicatorClassValue);
                    if (indicatorStyleValue is not null)
                        trackBuilder.AddAttribute(indicatorAttrIndex++, "StyleValue", indicatorStyleValue);
                    if (indicatorRender is not null)
                        trackBuilder.AddAttribute(indicatorAttrIndex++, "Render", indicatorRender);

                    var attrs = new Dictionary<string, object>
                    {
                        { "data-testid", "indicator" }
                    };
                    if (indicatorAttributes is not null)
                    {
                        foreach (var kvp in indicatorAttributes)
                            attrs[kvp.Key] = kvp.Value;
                    }
                    trackBuilder.AddAttribute(indicatorAttrIndex++, "AdditionalAttributes",
                        (IReadOnlyDictionary<string, object>)attrs);

                    trackBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateMeterWithIndicator());
        var indicator = cut.Find("[data-testid='indicator']");
        indicator.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateMeterWithIndicator(
            indicatorRender: ctx => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));
        var element = cut.Find("span[data-testid='indicator']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateMeterWithIndicator(
            indicatorAttributes: new Dictionary<string, object>
            {
                { "aria-label", "meter fill" }
            }
        ));
        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("aria-label").ShouldBe("meter fill");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateMeterWithIndicator(
            indicatorClassValue: _ => "indicator-custom"
        ));
        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("class").ShouldContain("indicator-custom");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateMeterWithIndicator(
            indicatorStyleValue: _ => "background: blue"
        ));
        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("style").ShouldContain("background: blue");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AdditionalStyleOverridesIntrinsicStylesWhenConflicting()
    {
        var cut = Render(CreateMeterWithIndicator(
            value: 50,
            indicatorAttributes: new Dictionary<string, object>
            {
                { "style", "width:25%" }
            }
        ));
        var indicator = cut.Find("[data-testid='indicator']");
        var style = indicator.GetAttribute("style");

        style.ShouldContain("width:50%");
        style.ShouldContain("width:25%");
        style.LastIndexOf("width:25%", StringComparison.Ordinal)
            .ShouldBeGreaterThan(style.LastIndexOf("width:50%", StringComparison.Ordinal));
        return Task.CompletedTask;
    }

    // Indicator styles

    [Fact]
    public Task SetsIndicatorStyleForValue()
    {
        var cut = Render(CreateMeterWithIndicator(value: 33));
        var indicator = cut.Find("[data-testid='indicator']");
        var style = indicator.GetAttribute("style");
        style.ShouldContain("inset-inline-start:0");
        style.ShouldContain("width:33%");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsZeroWidthWhenValueIsZero()
    {
        var cut = Render(CreateMeterWithIndicator(value: 0));
        var indicator = cut.Find("[data-testid='indicator']");
        var style = indicator.GetAttribute("style");
        style.ShouldContain("width:0%");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesUserStyleWithIndicatorStyle()
    {
        var cut = Render(CreateMeterWithIndicator(
            value: 50,
            indicatorStyleValue: _ => "background: green"
        ));
        var indicator = cut.Find("[data-testid='indicator']");
        var style = indicator.GetAttribute("style");
        style.ShouldContain("background: green");
        style.ShouldContain("width:50%");
        return Task.CompletedTask;
    }

    [Fact]
    public Task StyleValueOverridesIntrinsicStylesWhenConflicting()
    {
        var cut = Render(CreateMeterWithIndicator(
            value: 50,
            indicatorStyleValue: _ => "width:25%"
        ));
        var indicator = cut.Find("[data-testid='indicator']");
        var style = indicator.GetAttribute("style");

        style.ShouldContain("width:50%");
        style.ShouldContain("width:25%");
        style.LastIndexOf("width:25%", StringComparison.Ordinal)
            .ShouldBeGreaterThan(style.LastIndexOf("width:50%", StringComparison.Ordinal));
        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesReactValueToPercentWhenRangeIsZero()
    {
        var cut = Render(CreateMeterWithIndicator(value: 1, min: 0, max: 0));
        var indicator = cut.Find("[data-testid='indicator']");
        var style = indicator.GetAttribute("style");
        style.ShouldContain("width:Infinity%");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ThrowsWhenRenderedOutsideRoot()
    {
        var exception = Should.Throw<InvalidOperationException>(() => Render(builder =>
        {
            builder.OpenComponent<MeterIndicator>(0);
            builder.CloseComponent();
        }));
        exception.Message.ShouldBe("Base UI: MeterRootContext is missing. Meter parts must be placed within <Meter.Root>.");
        return Task.CompletedTask;
    }
}
