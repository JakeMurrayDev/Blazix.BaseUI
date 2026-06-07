using System.Globalization;

namespace BlazorBaseUI.Tests.Progress;

public class ProgressIndicatorTests : BunitContext, IProgressIndicatorContract
{
    public ProgressIndicatorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateProgressWithIndicator(
        double? value = 50,
        double min = 0,
        double max = 100,
        Func<ProgressRootState, string?>? indicatorClassValue = null,
        Func<ProgressRootState, string?>? indicatorStyleValue = null,
        IReadOnlyDictionary<string, object>? indicatorAttributes = null,
        RenderFragment<RenderProps<ProgressRootState>>? indicatorRender = null)
    {
        return builder =>
        {
            builder.OpenComponent<ProgressRoot>(0);
            var attrIndex = 1;

            if (value.HasValue)
                builder.AddAttribute(attrIndex++, "Value", value.Value);
            else
                builder.AddAttribute(attrIndex++, "Value", (double?)null);

            builder.AddAttribute(attrIndex++, "Min", min);
            builder.AddAttribute(attrIndex++, "Max", max);
            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ProgressTrack>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(trackBuilder =>
                {
                    trackBuilder.OpenComponent<ProgressIndicator>(0);
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
        var cut = Render(CreateProgressWithIndicator());
        var indicator = cut.Find("[data-testid='indicator']");
        indicator.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateProgressWithIndicator(
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
        var cut = Render(CreateProgressWithIndicator(
            indicatorAttributes: new Dictionary<string, object>
            {
                { "aria-label", "progress fill" }
            }
        ));
        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("aria-label").ShouldBe("progress fill");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateProgressWithIndicator(
            indicatorClassValue: _ => "indicator-custom"
        ));
        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("class").ShouldContain("indicator-custom");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateProgressWithIndicator(
            indicatorStyleValue: _ => "background: blue"
        ));
        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("style").ShouldContain("background: blue");
        return Task.CompletedTask;
    }

    // Indicator styles

    [Fact]
    public Task SetsIndicatorStyleForDeterminateValue()
    {
        var cut = Render(CreateProgressWithIndicator(value: 33));
        var indicator = cut.Find("[data-testid='indicator']");
        var style = indicator.GetAttribute("style");
        style.ShouldContain("inset-inline-start:0");
        style.ShouldContain("width:33%");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsZeroWidthWhenValueIsZero()
    {
        var cut = Render(CreateProgressWithIndicator(value: 0));
        var indicator = cut.Find("[data-testid='indicator']");
        var style = indicator.GetAttribute("style");
        style.ShouldContain("width:0%");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NoIndicatorStyleForIndeterminateValue()
    {
        var cut = Render(CreateProgressWithIndicator(value: null));
        var indicator = cut.Find("[data-testid='indicator']");
        var style = indicator.GetAttribute("style");
        if (style is not null)
        {
            style.ShouldNotContain("width:");
            style.ShouldNotContain("inset-inline-start:");
        }
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesUserStyleWithIndicatorStyle()
    {
        var cut = Render(CreateProgressWithIndicator(
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
    public Task AdditionalStyleOverridesIndicatorStyle()
    {
        var cut = Render(CreateProgressWithIndicator(
            value: 50,
            indicatorAttributes: new Dictionary<string, object>
            {
                { "style", "width:88%" }
            }));
        var indicator = cut.Find("[data-testid='indicator']");
        var style = indicator.GetAttribute("style");
        style.ShouldNotBeNull();
        style.IndexOf("width:50%", StringComparison.Ordinal)
            .ShouldBeLessThan(style.IndexOf("width:88%", StringComparison.Ordinal));
        return Task.CompletedTask;
    }

    [Fact]
    public Task StyleValueOverridesIndicatorStyle()
    {
        var cut = Render(CreateProgressWithIndicator(
            value: 50,
            indicatorStyleValue: _ => "width:88%"));
        var indicator = cut.Find("[data-testid='indicator']");
        var style = indicator.GetAttribute("style");
        style.ShouldNotBeNull();
        style.IndexOf("width:50%", StringComparison.Ordinal)
            .ShouldBeLessThan(style.IndexOf("width:88%", StringComparison.Ordinal));
        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesSourceValueToPercentWhenMinEqualsMax()
    {
        var cut = Render(CreateProgressWithIndicator(value: 50, min: 50, max: 50));
        var indicator = cut.Find("[data-testid='indicator']");
        var style = indicator.GetAttribute("style");
        style.ShouldContain("width:NaN%");
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataStatusAttribute()
    {
        var cut = Render(CreateProgressWithIndicator(value: 50));
        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-progressing").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ThrowsWhenRenderedWithoutRoot()
    {
        var exception = Should.Throw<InvalidOperationException>(() => Render(builder =>
        {
            builder.OpenComponent<ProgressIndicator>(0);
            builder.CloseComponent();
        }));
        exception.Message.ShouldBe("Base UI: ProgressRootContext is missing. Progress parts must be placed within <Progress.Root>.");
        return Task.CompletedTask;
    }
}
