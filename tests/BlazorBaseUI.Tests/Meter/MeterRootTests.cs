using System.Globalization;

namespace BlazorBaseUI.Tests.Meter;

public class MeterRootTests : BunitContext, IMeterRootContract
{
    public MeterRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateMeterRoot(
        double value = 50,
        double min = 0,
        double max = 100,
        NumberFormatOptions? format = null,
        string? formatString = null,
        string? locale = null,
        IFormatProvider? formatProvider = null,
        Func<string, double, string>? getAriaValueText = null,
        RenderFragment<RenderProps<MeterRootState>>? render = null,
        Func<MeterRootState, string?>? classValue = null,
        Func<MeterRootState, string?>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<MeterRoot>(0);
            var attrIndex = 1;

            builder.AddAttribute(attrIndex++, "Value", value);
            builder.AddAttribute(attrIndex++, "Min", min);
            builder.AddAttribute(attrIndex++, "Max", max);

            if (format is not null)
                builder.AddAttribute(attrIndex++, "Format", format);
            if (formatString is not null)
                builder.AddAttribute(attrIndex++, "FormatString", formatString);
            if (locale is not null)
                builder.AddAttribute(attrIndex++, "Locale", locale);
            if (formatProvider is not null)
                builder.AddAttribute(attrIndex++, "FormatProvider", formatProvider);
            if (getAriaValueText is not null)
                builder.AddAttribute(attrIndex++, "GetAriaValueText", getAriaValueText);
            if (render is not null)
                builder.AddAttribute(attrIndex++, "Render", render);
            if (classValue is not null)
                builder.AddAttribute(attrIndex++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(attrIndex++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
            if (childContent is not null)
                builder.AddAttribute(attrIndex++, "ChildContent", childContent);

            builder.CloseComponent();
        };
    }

    private RenderFragment CreateMeterWithLabel(
        double value = 50,
        string labelText = "Usage")
    {
        return builder =>
        {
            builder.OpenComponent<MeterRoot>(0);
            builder.AddAttribute(1, "Value", value);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<MeterLabel>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(labelBuilder =>
                {
                    labelBuilder.AddContent(0, labelText);
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateMeterWithValue(
        double value = 50,
        NumberFormatOptions? format = null,
        string? formatString = null,
        string? locale = null,
        IFormatProvider? formatProvider = null)
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
            if (locale is not null)
                builder.AddAttribute(attrIndex++, "Locale", locale);
            if (formatProvider is not null)
                builder.AddAttribute(attrIndex++, "FormatProvider", formatProvider);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<MeterValue>(0);
                innerBuilder.AddAttribute(1, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                    {
                        { "data-testid", "value" }
                    });
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateMeterRoot());
        var meter = cut.Find("[role='meter']");
        meter.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateMeterRoot(
            render: ctx => builder =>
            {
                builder.OpenElement(0, "section");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));
        var element = cut.Find("section");
        element.ShouldNotBeNull();
        element.GetAttribute("role").ShouldBe("meter");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateMeterRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "meter-root" },
                { "aria-label", "Disk usage" }
            }
        ));
        var meter = cut.Find("[role='meter']");
        meter.GetAttribute("data-testid").ShouldBe("meter-root");
        meter.GetAttribute("aria-label").ShouldBe("Disk usage");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateMeterRoot(
            classValue: _ => "custom-meter"
        ));
        var meter = cut.Find("[role='meter']");
        meter.GetAttribute("class").ShouldContain("custom-meter");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateMeterRoot(
            styleValue: _ => "width: 200px"
        ));
        var meter = cut.Find("[role='meter']");
        meter.GetAttribute("style").ShouldContain("width: 200px");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateMeterRoot(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }
        ));
        var meter = cut.Find("[role='meter']");
        var classAttr = meter.GetAttribute("class");
        classAttr.ShouldContain("static-class");
        classAttr.ShouldContain("dynamic-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersHiddenPresentationSpanForScreenReaders()
    {
        var cut = Render(CreateMeterRoot());
        var hidden = cut.Find("span[role='presentation']");

        hidden.TextContent.ShouldBe("x");
        var style = hidden.GetAttribute("style");
        style.ShouldContain("clip-path:inset(50%)");
        style.ShouldContain("position:fixed");
        style.ShouldContain("top:0");
        style.ShouldContain("left:0");
        return Task.CompletedTask;
    }

    // ARIA attributes

    [Fact]
    public Task HasRoleMeter()
    {
        var cut = Render(CreateMeterRoot());
        var meter = cut.Find("[role='meter']");
        meter.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaValueMin()
    {
        var cut = Render(CreateMeterRoot(value: 30, min: 10));
        var meter = cut.Find("[role='meter']");
        meter.GetAttribute("aria-valuemin").ShouldBe("10");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaValueMax()
    {
        var cut = Render(CreateMeterRoot(value: 30, max: 200));
        var meter = cut.Find("[role='meter']");
        meter.GetAttribute("aria-valuemax").ShouldBe("200");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaValueNow()
    {
        var cut = Render(CreateMeterRoot(value: 30));
        var meter = cut.Find("[role='meter']");
        meter.GetAttribute("aria-valuenow").ShouldBe("30");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaValueText()
    {
        var cut = Render(CreateMeterRoot(value: 30));
        var meter = cut.Find("[role='meter']");
        meter.GetAttribute("aria-valuetext").ShouldBe("30%");
        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesInvariantRawValueForDefaultAriaValueText()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

            var cut = Render(CreateMeterRoot(value: 86.49));
            var meter = cut.Find("[role='meter']");
            meter.GetAttribute("aria-valuetext").ShouldBe("86.49%");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }

        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesJavaScriptNumberStringForDefaultAriaValueText()
    {
        var small = Render(CreateMeterRoot(value: 0.000001));
        var smallMeter = small.Find("[role='meter']");
        smallMeter.GetAttribute("aria-valuetext").ShouldBe("0.000001%");

        var large = Render(CreateMeterRoot(value: 100000000000000000000.0));
        var largeMeter = large.Find("[role='meter']");
        largeMeter.GetAttribute("aria-valuetext").ShouldBe("100000000000000000000%");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AdditionalAriaValueTextOverridesComputedValue()
    {
        var cut = Render(CreateMeterRoot(
            value: 30,
            formatString: "F1",
            additionalAttributes: new Dictionary<string, object>
            {
                { "aria-valuetext", "Battery charge: 30 percent" }
            }
        ));
        var meter = cut.Find("[role='meter']");
        meter.GetAttribute("aria-valuetext").ShouldBe("Battery charge: 30 percent");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaLabelledByWhenLabelPresent()
    {
        var cut = Render(CreateMeterWithLabel(value: 30, labelText: "Disk Usage"));
        var meter = cut.Find("[role='meter']");
        var label = cut.Find("span");
        var labelId = label.GetAttribute("id");
        labelId.ShouldNotBeNullOrEmpty();
        meter.GetAttribute("aria-labelledby").ShouldBe(labelId);
        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesAriaValueNowWhenValueChanges()
    {
        var cut = Render(CreateMeterRoot(value: 50));
        var meter = cut.Find("[role='meter']");
        meter.GetAttribute("aria-valuenow").ShouldBe("50");

        var cut2 = Render(CreateMeterRoot(value: 77));
        var meter2 = cut2.Find("[role='meter']");
        meter2.GetAttribute("aria-valuenow").ShouldBe("77");
        return Task.CompletedTask;
    }

    // Formatting

    [Fact]
    public Task FormatsValueWithFormatString()
    {
        var cut = Render(CreateMeterWithValue(value: 30, formatString: "F1"));
        var meter = cut.Find("[role='meter']");
        var expected = 30.0.ToString("F1", CultureInfo.CurrentCulture);
        meter.GetAttribute("aria-valuetext").ShouldBe(expected);
        var valueElement = cut.Find("[data-testid='value']");
        valueElement.TextContent.ShouldBe(expected);
        return Task.CompletedTask;
    }

    [Fact]
    public Task FormatsValueWithFormatProvider()
    {
        var germanCulture = CultureInfo.GetCultureInfo("de-DE");
        var cut = Render(CreateMeterWithValue(
            value: 70.51,
            formatString: "F2",
            formatProvider: germanCulture));
        var valueElement = cut.Find("[data-testid='value']");
        var expected = 70.51.ToString("F2", germanCulture);
        valueElement.TextContent.ShouldBe(expected);
        return Task.CompletedTask;
    }

    [Fact]
    public Task FormatsValueWithNumberFormatOptionsAndLocale()
    {
        var format = new NumberFormatOptions(
            Style: "decimal",
            MinimumFractionDigits: 2,
            MaximumFractionDigits: 2);

        var cut = Render(CreateMeterWithValue(
            value: 86.49,
            format: format,
            locale: "de-DE"));

        var meter = cut.Find("[role='meter']");
        var valueElement = cut.Find("[data-testid='value']");

        meter.GetAttribute("aria-valuetext").ShouldBe("86,49");
        valueElement.TextContent.ShouldBe("86,49");
        return Task.CompletedTask;
    }

    [Fact]
    public Task FormatsValueWithSignificantDigitOptions()
    {
        var format = new NumberFormatOptions(
            MinimumSignificantDigits: 3,
            MaximumSignificantDigits: 3);

        var grouped = Render(CreateMeterWithValue(
            value: 12345,
            format: format,
            locale: "en-US"));

        grouped.Find("[role='meter']").GetAttribute("aria-valuetext").ShouldBe("12,300");
        grouped.Find("[data-testid='value']").TextContent.ShouldBe("12,300");

        var padded = Render(CreateMeterWithValue(
            value: 1.2,
            format: format,
            locale: "en-US"));

        padded.Find("[role='meter']").GetAttribute("aria-valuetext").ShouldBe("1.20");
        padded.Find("[data-testid='value']").TextContent.ShouldBe("1.20");
        return Task.CompletedTask;
    }

    [Fact]
    public Task FormatsValueWithMinimumIntegerAndSignificantDigitOptions()
    {
        var format = new NumberFormatOptions(
            MinimumIntegerDigits: 3,
            MinimumSignificantDigits: 3,
            MaximumSignificantDigits: 3);

        var cut = Render(CreateMeterWithValue(
            value: 1.2,
            format: format,
            locale: "en-US"));

        cut.Find("[role='meter']").GetAttribute("aria-valuetext").ShouldBe("001.20");
        cut.Find("[data-testid='value']").TextContent.ShouldBe("001.20");
        return Task.CompletedTask;
    }

    [Fact]
    public Task GetAriaValueTextCallbackOverridesDefault()
    {
        var cut = Render(CreateMeterRoot(
            value: 50,
            getAriaValueText: (formatted, val) => $"Level {val} of 100"
        ));
        var meter = cut.Find("[role='meter']");
        meter.GetAttribute("aria-valuetext").ShouldBe("Level 50 of 100");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AriaValueTextUsesFormattedValueWhenFormatProvided()
    {
        var cut = Render(CreateMeterRoot(
            value: 50,
            formatString: "F1"
        ));
        var meter = cut.Find("[role='meter']");
        var expected = 50.0.ToString("F1", CultureInfo.CurrentCulture);
        meter.GetAttribute("aria-valuetext").ShouldBe(expected);
        return Task.CompletedTask;
    }

    // Context cascading

    [Fact]
    public Task CascadesContextToChildren()
    {
        MeterRootState? capturedState = null;
        var cut = Render(builder =>
        {
            builder.OpenComponent<MeterRoot>(0);
            builder.AddAttribute(1, "Value", 50.0);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<MeterTrack>(0);
                innerBuilder.AddAttribute(1, "ClassValue", (Func<MeterRootState, string?>)(state =>
                {
                    capturedState = state;
                    return "track-class";
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });
        capturedState.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // Element reference

    [Fact]
    public Task ExposesElementReference()
    {
        MeterRoot? component = null;
        var cut = Render(builder =>
        {
            builder.OpenComponent<MeterRoot>(0);
            builder.AddAttribute(1, "Value", 50.0);
            builder.AddComponentReferenceCapture(2, obj => component = (MeterRoot)obj);
            builder.CloseComponent();
        });
        component.ShouldNotBeNull();
        cut.WaitForState(() => component!.Element.HasValue);
        component!.Element.HasValue.ShouldBeTrue();
        return Task.CompletedTask;
    }
}
