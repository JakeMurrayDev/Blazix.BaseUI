namespace Blazix.BaseUI.Tests.Slider;

public class SliderIndicatorTests : BunitContext, ISliderIndicatorContract
{
    public SliderIndicatorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSliderModule(JSInterop);
    }

    private RenderFragment CreateSliderWithIndicator(
        double? defaultValue = null,
        double[]? defaultValues = null,
        Orientation orientation = Orientation.Horizontal,
        bool disabled = false,
        ThumbAlignment thumbAlignment = ThumbAlignment.Center,
        Func<SliderRootState, string>? classValue = null,
        Func<SliderRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<SliderRootState>>? render = null,
        int thumbCount = 1)
    {
        return builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            if (defaultValue.HasValue)
                builder.AddAttribute(1, "DefaultValue", defaultValue.Value);
            if (defaultValues is not null)
                builder.AddAttribute(2, "DefaultValues", defaultValues);
            builder.AddAttribute(3, "Orientation", orientation);
            builder.AddAttribute(6, "ThumbAlignment", thumbAlignment);
            if (disabled)
                builder.AddAttribute(4, "Disabled", true);
            builder.AddAttribute(5, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderControl>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(controlBuilder =>
                {
                    controlBuilder.OpenComponent<SliderTrack>(0);
                    controlBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(trackBuilder =>
                    {
                        trackBuilder.OpenComponent<SliderIndicator>(0);
                        if (classValue is not null)
                            trackBuilder.AddAttribute(0, "ClassValue", classValue);
                        if (styleValue is not null)
                            trackBuilder.AddAttribute(1, "StyleValue", styleValue);
                        var mergedAttrs = new Dictionary<string, object> { { "data-testid", "slider-indicator" } };
                        if (additionalAttributes is not null)
                        {
                            foreach (var kvp in additionalAttributes)
                                mergedAttrs[kvp.Key] = kvp.Value;
                        }
                        trackBuilder.AddAttribute(2, "AdditionalAttributes", (IReadOnlyDictionary<string, object>)mergedAttrs);
                        if (render is not null)
                            trackBuilder.AddAttribute(3, "Render", render);
                        trackBuilder.CloseComponent();

                        for (var i = 0; i < thumbCount; i++)
                        {
                            trackBuilder.OpenComponent<SliderThumb>(10 + i);
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
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateSliderWithIndicator(defaultValue: 50));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        indicator.TagName.ShouldBe("DIV");

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

        var cut = Render(CreateSliderWithIndicator(defaultValue: 50, render: render));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        indicator.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateSliderWithIndicator(
            defaultValue: 50,
            additionalAttributes: new Dictionary<string, object>
            {
                { "aria-hidden", "true" }
            }
        ));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        indicator.GetAttribute("aria-hidden").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateSliderWithIndicator(
            defaultValue: 50,
            classValue: _ => "indicator-class"
        ));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        indicator.GetAttribute("class").ShouldContain("indicator-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateSliderWithIndicator(
            defaultValue: 50,
            styleValue: _ => "background: green"
        ));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        indicator.GetAttribute("style").ShouldContain("background: green");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasPositioningStyleForSingleValue()
    {
        var cut = Render(CreateSliderWithIndicator(defaultValue: 50));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        var style = indicator.GetAttribute("style");

        style.ShouldContain("position:");
        style.ShouldContain("width: 50");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasPositioningStyleForRangeValue()
    {
        var cut = Render(CreateSliderWithIndicator(defaultValues: [20, 80], thumbCount: 2));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        var style = indicator.GetAttribute("style");

        style.ShouldContain("position:");
        style.ShouldContain("inset-inline-start: 20");
        style.ShouldContain("width: 60");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientation()
    {
        var cut = Render(CreateSliderWithIndicator(defaultValue: 50, orientation: Orientation.Vertical));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        indicator.GetAttribute("data-orientation").ShouldBe("vertical");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateSliderWithIndicator(defaultValue: 50, disabled: true));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        indicator.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasVerticalPositioningStyleForSingleValue()
    {
        var cut = Render(CreateSliderWithIndicator(defaultValue: 50, orientation: Orientation.Vertical));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        var style = indicator.GetAttribute("style") ?? "";

        style.ShouldContain("position:");
        // Vertical orientation uses height instead of width
        style.ShouldContain("height: 50");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasVerticalPositioningStyleForRangeValue()
    {
        var cut = Render(CreateSliderWithIndicator(
            defaultValues: [20, 80],
            thumbCount: 2,
            orientation: Orientation.Vertical));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        var style = indicator.GetAttribute("style") ?? "";

        style.ShouldContain("position:");
        // Vertical range uses bottom and height
        style.ShouldContain("bottom: 20");
        style.ShouldContain("height: 60");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasInsetPositioningStyleForSingleValue()
    {
        var cut = Render(CreateSliderWithIndicator(
            defaultValue: 50,
            thumbAlignment: ThumbAlignment.Edge));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        var style = indicator.GetAttribute("style") ?? "";

        style.ShouldContain("--start-position:");
        style.ShouldContain("visibility: hidden");
        style.ShouldContain("width: var(--start-position)");

        return Task.CompletedTask;
    }

    [Fact]
    public Task EdgeClientOnlyUsesInsetPositioningStyle()
    {
        var cut = Render(CreateSliderWithIndicator(
            defaultValue: 50,
            thumbAlignment: ThumbAlignment.EdgeClientOnly));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        var style = indicator.GetAttribute("style") ?? "";

        style.ShouldContain("--start-position:");
        style.ShouldContain("visibility: hidden");
        style.ShouldContain("width: var(--start-position)");

        return Task.CompletedTask;
    }

    [Fact]
    public Task EdgeAlignmentAddsBaseUiIndicatorPrehydrationAttribute()
    {
        var cut = Render(CreateSliderWithIndicator(
            defaultValue: 50,
            thumbAlignment: ThumbAlignment.Edge));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        indicator.HasAttribute("data-base-ui-slider-indicator").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasInsetPositioningStyleForRangeValue()
    {
        var cut = Render(CreateSliderWithIndicator(
            defaultValues: [20, 80],
            thumbCount: 2,
            thumbAlignment: ThumbAlignment.Edge));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        var style = indicator.GetAttribute("style") ?? "";

        style.ShouldContain("--start-position:");
        style.ShouldContain("--relative-size:");
        style.ShouldContain("visibility: hidden");
        style.ShouldContain("width: var(--relative-size)");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasVerticalInsetPositioningStyleForSingleValue()
    {
        var cut = Render(CreateSliderWithIndicator(
            defaultValue: 50,
            orientation: Orientation.Vertical,
            thumbAlignment: ThumbAlignment.Edge));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        var style = indicator.GetAttribute("style") ?? "";

        style.ShouldContain("--start-position:");
        style.ShouldContain("visibility: hidden");
        style.ShouldContain("height: var(--start-position)");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasVerticalInsetPositioningStyleForRangeValue()
    {
        var cut = Render(CreateSliderWithIndicator(
            defaultValues: [20, 80],
            thumbCount: 2,
            orientation: Orientation.Vertical,
            thumbAlignment: ThumbAlignment.Edge));

        var indicator = cut.Find("[data-testid='slider-indicator']");
        var style = indicator.GetAttribute("style") ?? "";

        style.ShouldContain("--start-position:");
        style.ShouldContain("--relative-size:");
        style.ShouldContain("visibility: hidden");
        style.ShouldContain("height: var(--relative-size)");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDragging()
    {
        var cut = Render(CreateSliderWithIndicator(defaultValue: 50));

        var indicator = cut.Find("[data-testid='slider-indicator']");

        // Default state: not dragging, so data-dragging should not be present
        indicator.HasAttribute("data-dragging").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataValid()
    {
        var cut = Render(CreateSliderWithIndicator(defaultValue: 50));

        var indicator = cut.Find("[data-testid='slider-indicator']");

        // Default state without Field: valid is null, so data-valid should not be present
        indicator.HasAttribute("data-valid").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataInvalid()
    {
        var cut = Render(CreateSliderWithIndicator(defaultValue: 50));

        var indicator = cut.Find("[data-testid='slider-indicator']");

        // Default state without Field: valid is null, so data-invalid should not be present
        indicator.HasAttribute("data-invalid").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataTouched()
    {
        var cut = Render(CreateSliderWithIndicator(defaultValue: 50));

        var indicator = cut.Find("[data-testid='slider-indicator']");

        // Default state: not touched
        indicator.HasAttribute("data-touched").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDirty()
    {
        var cut = Render(CreateSliderWithIndicator(defaultValue: 50));

        var indicator = cut.Find("[data-testid='slider-indicator']");

        // Default state: not dirty
        indicator.HasAttribute("data-dirty").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataFocused()
    {
        var cut = Render(CreateSliderWithIndicator(defaultValue: 50));

        var indicator = cut.Find("[data-testid='slider-indicator']");

        // Default state: not focused
        indicator.HasAttribute("data-focused").ShouldBeFalse();

        return Task.CompletedTask;
    }
}
