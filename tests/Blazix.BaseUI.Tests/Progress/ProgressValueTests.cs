using System.Globalization;

namespace Blazix.BaseUI.Tests.Progress;

public class ProgressValueTests : BunitContext, IProgressValueContract
{
    public ProgressValueTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateProgressWithValue(
        double? value = 50,
        NumberFormatOptions? format = null,
        string? formatString = null,
        string? locale = null,
        IFormatProvider? formatProvider = null,
        Func<ProgressRootState, string?>? valueClassValue = null,
        Func<ProgressRootState, string?>? valueStyleValue = null,
        IReadOnlyDictionary<string, object>? valueAttributes = null,
        RenderFragment<RenderProps<ProgressRootState>>? valueRender = null,
        Func<string?, double?, RenderFragment>? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<ProgressRoot>(0);

            if (value.HasValue)
                builder.AddAttribute(1, "Value", value.Value);
            else
                builder.AddAttribute(2, "Value", (double?)null);

            if (format is not null)
                builder.AddAttribute(3, "Format", format);
            if (formatString is not null)
                builder.AddAttribute(4, "FormatString", formatString);
            if (locale is not null)
                builder.AddAttribute(5, "Locale", locale);
            if (formatProvider is not null)
                builder.AddAttribute(6, "FormatProvider", formatProvider);

            builder.AddAttribute(7, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ProgressValue>(0);

                if (valueClassValue is not null)
                    innerBuilder.AddAttribute(1, "ClassValue", valueClassValue);
                if (valueStyleValue is not null)
                    innerBuilder.AddAttribute(2, "StyleValue", valueStyleValue);
                if (valueRender is not null)
                    innerBuilder.AddAttribute(3, "Render", valueRender);
                if (childContent is not null)
                    innerBuilder.AddAttribute(4, "ChildContent", childContent);

                var attrs = new Dictionary<string, object>
                {
                    { "data-testid", "value" }
                };
                if (valueAttributes is not null)
                {
                    foreach (var kvp in valueAttributes)
                        attrs[kvp.Key] = kvp.Value;
                }
                innerBuilder.AddAttribute(5, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)attrs);

                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsSpanByDefault()
    {
        var cut = Render(CreateProgressWithValue());
        var valueEl = cut.Find("[data-testid='value']");
        valueEl.TagName.ShouldBe("SPAN");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateProgressWithValue(
            valueRender: ctx => builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));
        var element = cut.Find("div[data-testid='value']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateProgressWithValue(
            valueAttributes: new Dictionary<string, object>
            {
                { "data-custom", "test" }
            }
        ));
        var valueEl = cut.Find("[data-testid='value']");
        valueEl.GetAttribute("data-custom").ShouldBe("test");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateProgressWithValue(
            valueClassValue: _ => "value-custom"
        ));
        var valueEl = cut.Find("[data-testid='value']");
        valueEl.GetAttribute("class").ShouldContain("value-custom");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateProgressWithValue(
            valueStyleValue: _ => "font-weight: bold"
        ));
        var valueEl = cut.Find("[data-testid='value']");
        valueEl.GetAttribute("style").ShouldContain("font-weight: bold");
        return Task.CompletedTask;
    }

    // ARIA

    [Fact]
    public Task HasAriaHidden()
    {
        var cut = Render(CreateProgressWithValue());
        var valueEl = cut.Find("[data-testid='value']");
        valueEl.GetAttribute("aria-hidden").ShouldBe("true");
        return Task.CompletedTask;
    }

    // Content rendering

    [Fact]
    public Task RendersFormattedValueWhenNoChildContent()
    {
        var cut = Render(CreateProgressWithValue(value: 30));
        var valueEl = cut.Find("[data-testid='value']");
        var expected = (30.0 / 100.0).ToString("P0", CultureInfo.CurrentCulture);
        valueEl.TextContent.ShouldBe(expected);
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersCustomFormattedValue()
    {
        var cut = Render(CreateProgressWithValue(value: 30, formatString: "F1"));
        var valueEl = cut.Find("[data-testid='value']");
        var expected = 30.0.ToString("F1", CultureInfo.CurrentCulture);
        valueEl.TextContent.ShouldBe(expected);
        return Task.CompletedTask;
    }

    [Fact]
    public Task ChildContentReceivesFormattedValueAndNumber()
    {
        string? capturedFormatted = null;
        double? capturedValue = null;

        var cut = Render(CreateProgressWithValue(
            value: 30,
            formatString: "F1",
            childContent: (formatted, val) =>
            {
                capturedFormatted = formatted;
                capturedValue = val;
                return b => b.AddContent(0, $"Custom: {formatted}");
            }
        ));

        var expected = 30.0.ToString("F1", CultureInfo.CurrentCulture);
        capturedFormatted.ShouldBe(expected);
        capturedValue.ShouldBe(30.0);

        var valueEl = cut.Find("[data-testid='value']");
        valueEl.TextContent.ShouldContain($"Custom: {expected}");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ChildContentReceivesIndeterminateAndNull()
    {
        string? capturedFormatted = null;
        double? capturedValue = null;

        var cut = Render(CreateProgressWithValue(
            value: null,
            childContent: (formatted, val) =>
            {
                capturedFormatted = formatted;
                capturedValue = val;
                return b => b.AddContent(0, formatted);
            }
        ));

        capturedFormatted.ShouldBe("indeterminate");
        capturedValue.ShouldBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersNothingForIndeterminateWhenNoChildContent()
    {
        var cut = Render(CreateProgressWithValue(value: null));
        var valueEl = cut.Find("[data-testid='value']");
        valueEl.TextContent.ShouldBeEmpty();
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataStatusAttribute()
    {
        var cut = Render(CreateProgressWithValue(value: 50));
        var valueEl = cut.Find("[data-testid='value']");
        valueEl.HasAttribute("data-progressing").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ThrowsWhenRenderedWithoutRoot()
    {
        var exception = Should.Throw<InvalidOperationException>(() => Render(builder =>
        {
            builder.OpenComponent<ProgressValue>(0);
            builder.CloseComponent();
        }));
        exception.Message.ShouldBe("Base UI: ProgressRootContext is missing. Progress parts must be placed within <Progress.Root>.");
        return Task.CompletedTask;
    }
}
