using System.Globalization;

namespace Blazix.BaseUI.Tests.Progress;

public class ProgressRootTests : BunitContext, IProgressRootContract
{
    public ProgressRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateProgressRoot(
        double? value = 50,
        double min = 0,
        double max = 100,
        NumberFormatOptions? format = null,
        string? formatString = null,
        string? locale = null,
        IFormatProvider? formatProvider = null,
        Func<string?, double?, string>? getAriaValueText = null,
        RenderFragment<RenderProps<ProgressRootState>>? render = null,
        Func<ProgressRootState, string?>? classValue = null,
        Func<ProgressRootState, string?>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<ProgressRoot>(0);

            if (value.HasValue)
                builder.AddAttribute(1, "Value", value.Value);
            else
                builder.AddAttribute(2, "Value", (double?)null);

            builder.AddAttribute(3, "Min", min);
            builder.AddAttribute(4, "Max", max);

            if (format is not null)
                builder.AddAttribute(5, "Format", format);
            if (formatString is not null)
                builder.AddAttribute(6, "FormatString", formatString);
            if (locale is not null)
                builder.AddAttribute(7, "Locale", locale);
            if (formatProvider is not null)
                builder.AddAttribute(8, "FormatProvider", formatProvider);
            if (getAriaValueText is not null)
                builder.AddAttribute(9, "GetAriaValueText", getAriaValueText);
            if (render is not null)
                builder.AddAttribute(10, "Render", render);
            if (classValue is not null)
                builder.AddAttribute(11, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(12, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(13, "AdditionalAttributes", additionalAttributes);
            if (childContent is not null)
                builder.AddAttribute(14, "ChildContent", childContent);

            builder.CloseComponent();
        };
    }

    private RenderFragment CreateProgressWithLabel(
        double? value = 50,
        string labelText = "Loading")
    {
        return builder =>
        {
            builder.OpenComponent<ProgressRoot>(0);
            builder.AddAttribute(1, "Value", value);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ProgressLabel>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(labelBuilder =>
                {
                    labelBuilder.AddContent(0, labelText);
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateProgressWithValue(
        double? value = 50,
        NumberFormatOptions? format = null,
        string? formatString = null,
        string? locale = null,
        IFormatProvider? formatProvider = null)
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
        var cut = Render(CreateProgressRoot());
        var progressbar = cut.Find("[role='progressbar']");
        progressbar.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateProgressRoot(
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
        element.GetAttribute("role").ShouldBe("progressbar");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateProgressRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "progress-root" },
                { "aria-label", "File upload progress" }
            }
        ));
        var progressbar = cut.Find("[role='progressbar']");
        progressbar.GetAttribute("data-testid").ShouldBe("progress-root");
        progressbar.GetAttribute("aria-label").ShouldBe("File upload progress");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateProgressRoot(
            classValue: _ => "custom-progress"
        ));
        var progressbar = cut.Find("[role='progressbar']");
        progressbar.GetAttribute("class").ShouldContain("custom-progress");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateProgressRoot(
            styleValue: _ => "width: 200px"
        ));
        var progressbar = cut.Find("[role='progressbar']");
        progressbar.GetAttribute("style").ShouldContain("width: 200px");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateProgressRoot(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }
        ));
        var progressbar = cut.Find("[role='progressbar']");
        var classAttr = progressbar.GetAttribute("class");
        classAttr.ShouldContain("static-class");
        classAttr.ShouldContain("dynamic-class");
        return Task.CompletedTask;
    }

    // ARIA attributes

    [Fact]
    public Task HasRoleProgressbar()
    {
        var cut = Render(CreateProgressRoot());
        var progressbar = cut.Find("[role='progressbar']");
        progressbar.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaValueMin()
    {
        var cut = Render(CreateProgressRoot(value: 30, min: 10));
        var progressbar = cut.Find("[role='progressbar']");
        progressbar.GetAttribute("aria-valuemin").ShouldBe("10");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaValueMax()
    {
        var cut = Render(CreateProgressRoot(value: 30, max: 200));
        var progressbar = cut.Find("[role='progressbar']");
        progressbar.GetAttribute("aria-valuemax").ShouldBe("200");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaValueNow()
    {
        var cut = Render(CreateProgressRoot(value: 30));
        var progressbar = cut.Find("[role='progressbar']");
        progressbar.GetAttribute("aria-valuenow").ShouldBe("30");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaValueText()
    {
        var cut = Render(CreateProgressRoot(value: 30));
        var progressbar = cut.Find("[role='progressbar']");
        var expected = (30.0 / 100.0).ToString("P0", CultureInfo.CurrentCulture);
        progressbar.GetAttribute("aria-valuetext").ShouldBe(expected);
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaLabelledByWhenLabelPresent()
    {
        var cut = Render(CreateProgressWithLabel(value: 30, labelText: "Downloading"));
        var progressbar = cut.Find("[role='progressbar']");
        var label = cut.Find("span");
        var labelId = label.GetAttribute("id");
        labelId.ShouldNotBeNullOrEmpty();
        progressbar.GetAttribute("aria-labelledby").ShouldBe(labelId);
        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesAriaValueNowWhenValueChanges()
    {
        var cut = Render(CreateProgressRoot(value: 50));
        var progressbar = cut.Find("[role='progressbar']");
        progressbar.GetAttribute("aria-valuenow").ShouldBe("50");

        var cut2 = Render(CreateProgressRoot(value: 77));
        var progressbar2 = cut2.Find("[role='progressbar']");
        progressbar2.GetAttribute("aria-valuenow").ShouldBe("77");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotSetAriaValueNowWhenIndeterminate()
    {
        var cut = Render(CreateProgressRoot(value: null));
        var progressbar = cut.Find("[role='progressbar']");
        progressbar.HasAttribute("aria-valuenow").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsIndeterminateAriaValueText()
    {
        var cut = Render(CreateProgressRoot(value: null));
        var progressbar = cut.Find("[role='progressbar']");
        progressbar.GetAttribute("aria-valuetext").ShouldBe("indeterminate progress");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AllowsAriaAttributesToOverrideDefaults()
    {
        var cut = Render(CreateProgressRoot(
            value: 30,
            additionalAttributes: new Dictionary<string, object>
            {
                { "role", "meter" },
                { "aria-valuemin", "-1" },
                { "aria-valuemax", "9" },
                { "aria-valuenow", "manual" },
                { "aria-valuetext", "Manual value" },
                { "aria-labelledby", "external-label" }
            }));

        var element = cut.Find("div");
        element.GetAttribute("role").ShouldBe("meter");
        element.GetAttribute("aria-valuemin").ShouldBe("-1");
        element.GetAttribute("aria-valuemax").ShouldBe("9");
        element.GetAttribute("aria-valuenow").ShouldBe("manual");
        element.GetAttribute("aria-valuetext").ShouldBe("Manual value");
        element.GetAttribute("aria-labelledby").ShouldBe("external-label");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersNvdaPresentationSpan()
    {
        var cut = Render(CreateProgressRoot());
        var hidden = cut.Find("span[role='presentation']");
        hidden.TextContent.ShouldBe("x");
        hidden.GetAttribute("style").ShouldContain("clip-path:inset(50%)");
        hidden.GetAttribute("style").ShouldContain("position:fixed");
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataProgressingWhenInProgress()
    {
        var cut = Render(CreateProgressRoot(value: 50));
        var progressbar = cut.Find("[role='progressbar']");
        progressbar.HasAttribute("data-progressing").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataCompleteWhenComplete()
    {
        var cut = Render(CreateProgressRoot(value: 100, max: 100));
        var progressbar = cut.Find("[role='progressbar']");
        progressbar.HasAttribute("data-complete").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataIndeterminateWhenNull()
    {
        var cut = Render(CreateProgressRoot(value: null));
        var progressbar = cut.Find("[role='progressbar']");
        progressbar.HasAttribute("data-indeterminate").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // Formatting

    [Fact]
    public Task FormatsValueWithCustomFormat()
    {
        var cut = Render(CreateProgressWithValue(value: 30, formatString: "F1"));
        var progressbar = cut.Find("[role='progressbar']");
        var expected = 30.0.ToString("F1", CultureInfo.CurrentCulture);
        progressbar.GetAttribute("aria-valuetext").ShouldBe(expected);
        var valueElement = cut.Find("[data-testid='value']");
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

        var cut = Render(CreateProgressWithValue(
            value: 70.51,
            format: format,
            locale: "de-DE"));

        var progressbar = cut.Find("[role='progressbar']");
        var valueElement = cut.Find("[data-testid='value']");
        progressbar.GetAttribute("aria-valuetext").ShouldBe("70,51");
        valueElement.TextContent.ShouldBe("70,51");
        return Task.CompletedTask;
    }

    [Fact]
    public Task FormatsValueWithFormatProvider()
    {
        var germanCulture = CultureInfo.GetCultureInfo("de-DE");
        var cut = Render(CreateProgressWithValue(
            value: 70.51,
            formatString: "F2",
            formatProvider: germanCulture));
        var valueElement = cut.Find("[data-testid='value']");
        var expected = 70.51.ToString("F2", germanCulture);
        valueElement.TextContent.ShouldBe(expected);
        return Task.CompletedTask;
    }

    [Fact]
    public Task GetAriaValueTextCallbackOverridesDefault()
    {
        var cut = Render(CreateProgressRoot(
            value: 50,
            getAriaValueText: (formatted, val) => $"Step {val} of 100"
        ));
        var progressbar = cut.Find("[role='progressbar']");
        progressbar.GetAttribute("aria-valuetext").ShouldBe("Step 50 of 100");
        return Task.CompletedTask;
    }

    // Context cascading

    [Fact]
    public Task CascadesContextToChildren()
    {
        ProgressRootState? capturedState = null;
        var cut = Render(builder =>
        {
            builder.OpenComponent<ProgressRoot>(0);
            builder.AddAttribute(1, "Value", 50.0);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ProgressTrack>(0);
                innerBuilder.AddAttribute(1, "ClassValue", (Func<ProgressRootState, string?>)(state =>
                {
                    capturedState = state;
                    return "track-class";
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });
        capturedState.ShouldNotBeNull();
        capturedState!.Status.ShouldBe(ProgressStatus.Progressing);
        return Task.CompletedTask;
    }

    // Element reference

    [Fact]
    public Task ExposesElementReference()
    {
        ProgressRoot? component = null;
        var cut = Render(builder =>
        {
            builder.OpenComponent<ProgressRoot>(0);
            builder.AddAttribute(1, "Value", 50.0);
            builder.AddComponentReferenceCapture(2, obj => component = (ProgressRoot)obj);
            builder.CloseComponent();
        });
        component.ShouldNotBeNull();
        cut.WaitForState(() => component!.Element.HasValue);
        component!.Element.HasValue.ShouldBeTrue();
        return Task.CompletedTask;
    }
}
