namespace BlazorBaseUI.Tests.Slider;

public class SliderValueTests : BunitContext, ISliderValueContract
{
    public SliderValueTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSliderModule(JSInterop);
    }

    private RenderFragment CreateSliderWithValue(
        double? defaultValue = null,
        double[]? defaultValues = null,
        Orientation orientation = Orientation.Horizontal,
        bool disabled = false,
        Func<SliderRootState, string>? classValue = null,
        Func<SliderRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<SliderRootState>>? render = null,
        int thumbCount = 1,
        RenderFragment<(string[] FormattedValues, double[] Values)>? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            if (defaultValue.HasValue)
                builder.AddAttribute(1, "DefaultValue", defaultValue.Value);
            if (defaultValues is not null)
                builder.AddAttribute(2, "DefaultValues", defaultValues);
            builder.AddAttribute(3, "Orientation", orientation);
            if (disabled)
                builder.AddAttribute(4, "Disabled", true);
            builder.AddAttribute(5, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderValue>(0);
                if (classValue is not null)
                    innerBuilder.AddAttribute(0, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(1, "StyleValue", styleValue);
                var mergedAttrs = new Dictionary<string, object> { { "data-testid", "slider-value" } };
                if (additionalAttributes is not null)
                {
                    foreach (var kvp in additionalAttributes)
                        mergedAttrs[kvp.Key] = kvp.Value;
                }
                innerBuilder.AddAttribute(2, "AdditionalAttributes", (IReadOnlyDictionary<string, object>)mergedAttrs);
                if (render is not null)
                    innerBuilder.AddAttribute(3, "Render", render);
                if (childContent is not null)
                    innerBuilder.AddAttribute(4, "ChildContent", childContent);
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SliderControl>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(controlBuilder =>
                {
                    controlBuilder.OpenComponent<SliderTrack>(0);
                    controlBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(trackBuilder =>
                    {
                        for (var i = 0; i < thumbCount; i++)
                        {
                            trackBuilder.OpenComponent<SliderThumb>(i);
                            if (thumbCount > 1)
                                trackBuilder.AddAttribute(0, "Index", i);
                            trackBuilder.CloseComponent();
                        }
                    }));
                    controlBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsOutputByDefault()
    {
        var cut = Render(CreateSliderWithValue(defaultValue: 50));

        var value = cut.Find("[data-testid='slider-value']");
        value.TagName.ShouldBe("OUTPUT");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<SliderRootState>> render = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateSliderWithValue(defaultValue: 50, render: render));

        var value = cut.Find("[data-testid='slider-value']");
        value.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateSliderWithValue(
            defaultValue: 50,
            additionalAttributes: new Dictionary<string, object>
            {
                { "aria-label", "Current value" }
            }
        ));

        var value = cut.Find("[data-testid='slider-value']");
        value.GetAttribute("aria-label").ShouldBe("Current value");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateSliderWithValue(
            defaultValue: 50,
            classValue: _ => "value-class"
        ));

        var value = cut.Find("[data-testid='slider-value']");
        value.GetAttribute("class").ShouldContain("value-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateSliderWithValue(
            defaultValue: 50,
            styleValue: _ => "font-weight: bold"
        ));

        var value = cut.Find("[data-testid='slider-value']");
        value.GetAttribute("style").ShouldContain("font-weight: bold");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisplaysSingleValue()
    {
        var cut = Render(CreateSliderWithValue(defaultValue: 42));

        var value = cut.Find("[data-testid='slider-value']");
        value.TextContent.ShouldContain("42");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisplaysRangeValues()
    {
        var cut = Render(CreateSliderWithValue(defaultValues: [20, 80], thumbCount: 2));

        var value = cut.Find("[data-testid='slider-value']");
        value.TextContent.ShouldContain("20");
        value.TextContent.ShouldContain("80");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisplaysMultipleThumbValues()
    {
        var cut = Render(CreateSliderWithValue(defaultValues: [10, 30, 60, 90], thumbCount: 4));

        var value = cut.Find("[data-testid='slider-value']");
        value.TextContent.ShouldContain("10");
        value.TextContent.ShouldContain("30");
        value.TextContent.ShouldContain("60");
        value.TextContent.ShouldContain("90");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaLiveOff()
    {
        var cut = Render(CreateSliderWithValue(defaultValue: 50));

        var value = cut.Find("[data-testid='slider-value']");
        value.GetAttribute("aria-live").ShouldBe("off");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientation()
    {
        var cut = Render(CreateSliderWithValue(defaultValue: 50, orientation: Orientation.Vertical));

        var value = cut.Find("[data-testid='slider-value']");
        value.GetAttribute("data-orientation").ShouldBe("vertical");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateSliderWithValue(defaultValue: 50, disabled: true));

        var value = cut.Find("[data-testid='slider-value']");
        value.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesChildRenderFragment()
    {
        var cut = Render(CreateSliderWithValue(
            defaultValue: 75,
            childContent: ctx => builder =>
            {
                builder.AddContent(0, $"Custom: {ctx.Values[0]}%");
            }
        ));

        var value = cut.Find("[data-testid='slider-value']");
        value.TextContent.ShouldContain("Custom: 75%");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisplaysFormattedValues()
    {
        var cut = Render(CreateSliderWithValue(defaultValue: 0.5, thumbCount: 1));

        var value = cut.Find("[data-testid='slider-value']");
        // The default formatting displays the value
        value.TextContent.ShouldContain("0.5");

        return Task.CompletedTask;
    }

    [Fact]
    public Task FormatOptionsCanDisableGrouping()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValue", 1000.5);
            builder.AddAttribute(2, "Max", 2000.0);
            builder.AddAttribute(3, "Locale", "en-US");
            builder.AddAttribute(4, "Format", new NumberFormatOptions(MaximumFractionDigits: 1, UseGrouping: false));
            builder.AddAttribute(5, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderValue>(0);
                innerBuilder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
                {
                    { "data-testid", "slider-value" }
                });
                innerBuilder.CloseComponent();
                innerBuilder.OpenComponent<SliderControl>(2);
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(controlBuilder =>
                {
                    controlBuilder.OpenComponent<SliderThumb>(0);
                    controlBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var value = cut.Find("[data-testid='slider-value']");
        value.TextContent.ShouldContain("1000.5");
        value.TextContent.ShouldNotContain("1,000.5");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ChildContentReceivesFormattedAndRawValues()
    {
        string[]? capturedFormattedValues = null;
        double[]? capturedRawValues = null;

        var cut = Render(CreateSliderWithValue(
            defaultValues: [25.5, 75.5],
            thumbCount: 2,
            childContent: ctx => builder =>
            {
                capturedFormattedValues = ctx.FormattedValues;
                capturedRawValues = ctx.Values;
                builder.AddContent(0, string.Join(" - ", ctx.FormattedValues));
            }
        ));

        capturedFormattedValues.ShouldNotBeNull();
        capturedFormattedValues!.Length.ShouldBe(2);
        capturedRawValues.ShouldNotBeNull();
        capturedRawValues!.Length.ShouldBe(2);
        capturedRawValues[0].ShouldBe(25.5);
        capturedRawValues[1].ShouldBe(75.5);

        return Task.CompletedTask;
    }
}
