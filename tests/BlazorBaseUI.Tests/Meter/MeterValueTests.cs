using System.Globalization;

namespace BlazorBaseUI.Tests.Meter;

public class MeterValueTests : BunitContext, IMeterValueContract
{
    public MeterValueTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateMeterWithValue(
        double value = 50,
        NumberFormatOptions? format = null,
        string? formatString = null,
        IFormatProvider? formatProvider = null,
        Func<MeterRootState, string?>? valueClassValue = null,
        Func<MeterRootState, string?>? valueStyleValue = null,
        IReadOnlyDictionary<string, object>? valueAttributes = null,
        RenderFragment<RenderProps<MeterRootState>>? valueRender = null,
        Func<string, double, RenderFragment>? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<MeterRoot>(0);
            var attrIndex = 1;

            builder.AddAttribute(attrIndex++, "Value", value);

            if (format is not null)
                builder.AddAttribute(attrIndex++, "Format", format);
            if (formatString is not null)
                builder.AddAttribute(attrIndex++, "FormatString", formatString);
            if (formatProvider is not null)
                builder.AddAttribute(attrIndex++, "FormatProvider", formatProvider);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<MeterValue>(0);
                var valueAttrIndex = 1;

                if (valueClassValue is not null)
                    innerBuilder.AddAttribute(valueAttrIndex++, "ClassValue", valueClassValue);
                if (valueStyleValue is not null)
                    innerBuilder.AddAttribute(valueAttrIndex++, "StyleValue", valueStyleValue);
                if (valueRender is not null)
                    innerBuilder.AddAttribute(valueAttrIndex++, "Render", valueRender);
                if (childContent is not null)
                    innerBuilder.AddAttribute(valueAttrIndex++, "ChildContent", childContent);

                var attrs = new Dictionary<string, object>
                {
                    { "data-testid", "value" }
                };
                if (valueAttributes is not null)
                {
                    foreach (var kvp in valueAttributes)
                        attrs[kvp.Key] = kvp.Value;
                }
                innerBuilder.AddAttribute(valueAttrIndex++, "AdditionalAttributes",
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
        var cut = Render(CreateMeterWithValue());
        var valueEl = cut.Find("[data-testid='value']");
        valueEl.TagName.ShouldBe("SPAN");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateMeterWithValue(
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
        var cut = Render(CreateMeterWithValue(
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
        var cut = Render(CreateMeterWithValue(
            valueClassValue: _ => "value-custom"
        ));
        var valueEl = cut.Find("[data-testid='value']");
        valueEl.GetAttribute("class").ShouldContain("value-custom");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateMeterWithValue(
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
        var cut = Render(CreateMeterWithValue());
        var valueEl = cut.Find("[data-testid='value']");
        valueEl.GetAttribute("aria-hidden").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AdditionalAriaHiddenOverridesDefault()
    {
        var cut = Render(CreateMeterWithValue(
            valueAttributes: new Dictionary<string, object>
            {
                { "aria-hidden", "false" }
            }
        ));
        var valueEl = cut.Find("[data-testid='value']");
        valueEl.GetAttribute("aria-hidden").ShouldBe("false");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ThrowsWhenRenderedOutsideRoot()
    {
        var exception = Should.Throw<InvalidOperationException>(() => Render(builder =>
        {
            builder.OpenComponent<MeterValue>(0);
            builder.CloseComponent();
        }));
        exception.Message.ShouldBe("Base UI: MeterRootContext is missing. Meter parts must be placed within <Meter.Root>.");
        return Task.CompletedTask;
    }

    // Content rendering

    [Fact]
    public Task RendersFormattedValueWhenNoChildContent()
    {
        var cut = Render(CreateMeterWithValue(value: 30));
        var valueEl = cut.Find("[data-testid='value']");
        var expected = (30.0 / 100.0).ToString("P0", CultureInfo.CurrentCulture);
        valueEl.TextContent.ShouldBe(expected);
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersCustomFormattedValue()
    {
        var cut = Render(CreateMeterWithValue(value: 30, formatString: "F1"));
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

        var cut = Render(CreateMeterWithValue(
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
}
