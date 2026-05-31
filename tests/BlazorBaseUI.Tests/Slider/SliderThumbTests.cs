namespace BlazorBaseUI.Tests.Slider;

public class SliderThumbTests : BunitContext, ISliderThumbContract
{
    public SliderThumbTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSliderModule(JSInterop);
    }

    private RenderFragment CreateSliderWithThumb(
        double? defaultValue = null,
        double[]? defaultValues = null,
        double min = 0,
        double max = 100,
        double step = 1,
        Orientation orientation = Orientation.Horizontal,
        bool disabled = false,
        Func<SliderThumbState, string>? classValue = null,
        Func<SliderThumbState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<SliderThumbState>>? render = null,
        int thumbCount = 1,
        EventCallback<FocusEventArgs>? onFocus = null,
        EventCallback<FocusEventArgs>? onBlur = null)
    {
        return builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            if (defaultValue.HasValue)
                builder.AddAttribute(1, "DefaultValue", defaultValue.Value);
            if (defaultValues is not null)
                builder.AddAttribute(2, "DefaultValues", defaultValues);
            builder.AddAttribute(3, "Min", min);
            builder.AddAttribute(4, "Max", max);
            builder.AddAttribute(5, "Step", step);
            builder.AddAttribute(6, "Orientation", orientation);
            if (disabled)
                builder.AddAttribute(7, "Disabled", true);
            builder.AddAttribute(8, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderControl>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(controlBuilder =>
                {
                    controlBuilder.OpenComponent<SliderTrack>(0);
                    controlBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(trackBuilder =>
                    {
                        trackBuilder.OpenComponent<SliderIndicator>(0);
                        trackBuilder.CloseComponent();

                        for (var i = 0; i < thumbCount; i++)
                        {
                            trackBuilder.OpenComponent<SliderThumb>(10 + i);
                            if (thumbCount > 1)
                                trackBuilder.AddAttribute(0, "Index", i);
                            if (classValue is not null)
                                trackBuilder.AddAttribute(1, "ClassValue", classValue);
                            if (styleValue is not null)
                                trackBuilder.AddAttribute(2, "StyleValue", styleValue);
                            var mergedAttrs = new Dictionary<string, object> { { "data-testid", $"slider-thumb-{i}" } };
                            if (additionalAttributes is not null)
                            {
                                foreach (var kvp in additionalAttributes)
                                    mergedAttrs[kvp.Key] = kvp.Value;
                            }
                            trackBuilder.AddAttribute(3, "AdditionalAttributes", (IReadOnlyDictionary<string, object>)mergedAttrs);
                            if (render is not null)
                                trackBuilder.AddAttribute(4, "Render", render);
                            if (onFocus.HasValue)
                                trackBuilder.AddAttribute(5, "OnFocus", onFocus.Value);
                            if (onBlur.HasValue)
                                trackBuilder.AddAttribute(6, "OnBlur", onBlur.Value);
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
        var cut = Render(CreateSliderWithThumb(defaultValue: 50));

        var thumb = cut.Find("[data-testid='slider-thumb-0']");
        thumb.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<SliderThumbState>> render = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateSliderWithThumb(defaultValue: 50, render: render));

        var thumb = cut.Find("[data-testid='slider-thumb-0']");
        thumb.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateSliderWithThumb(
            defaultValue: 50,
            additionalAttributes: new Dictionary<string, object>
            {
                { "aria-label", "Custom thumb" }
            }
        ));

        var thumb = cut.Find("[data-testid='slider-thumb-0']");
        thumb.GetAttribute("aria-label").ShouldBe("Custom thumb");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateSliderWithThumb(
            defaultValue: 50,
            classValue: _ => "thumb-class"
        ));

        var thumb = cut.Find("[data-testid='slider-thumb-0']");
        thumb.GetAttribute("class").ShouldContain("thumb-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateSliderWithThumb(
            defaultValue: 50,
            styleValue: _ => "background: red"
        ));

        var thumb = cut.Find("[data-testid='slider-thumb-0']");
        thumb.GetAttribute("style").ShouldContain("background: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ContainsInputTypeRange()
    {
        var cut = Render(CreateSliderWithThumb(defaultValue: 50));

        var input = cut.Find("[data-testid='slider-thumb-0'] input[type='range']");
        input.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabindexMinusOneOnThumb()
    {
        var cut = Render(CreateSliderWithThumb(defaultValue: 50));

        var thumb = cut.Find("[data-testid='slider-thumb-0']");
        thumb.GetAttribute("tabindex").ShouldBe("-1");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InputHasAriaValuenow()
    {
        var cut = Render(CreateSliderWithThumb(defaultValue: 75));

        var input = cut.Find("input[type='range']");
        input.GetAttribute("aria-valuenow").ShouldBe("75");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InputHasAriaOrientation()
    {
        var cut = Render(CreateSliderWithThumb(defaultValue: 50, orientation: Orientation.Vertical));

        var input = cut.Find("input[type='range']");
        input.GetAttribute("aria-orientation").ShouldBe("vertical");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InputHasMinMaxStep()
    {
        var cut = Render(CreateSliderWithThumb(defaultValue: 50, min: 10, max: 90, step: 5));

        var input = cut.Find("input[type='range']");
        input.GetAttribute("min").ShouldBe("10");
        input.GetAttribute("max").ShouldBe("90");
        input.GetAttribute("step").ShouldBe("5");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InputHasDisabledAttribute()
    {
        var cut = Render(CreateSliderWithThumb(defaultValue: 50, disabled: true));

        var input = cut.Find("input[type='range']");
        input.HasAttribute("disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task GetAriaLabelCallback_SetsAriaLabelOnInput()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValue", 50.0);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderControl>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(controlBuilder =>
                {
                    controlBuilder.OpenComponent<SliderThumb>(0);
                    controlBuilder.AddAttribute(1, "GetAriaLabel", (Func<int, string>)(_ => "Volume"));
                    controlBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var input = cut.Find("input[type='range']");
        input.GetAttribute("aria-label").ShouldBe("Volume");

        return Task.CompletedTask;
    }

    [Fact]
    public Task GetAriaValueTextCallback_SetsAriaValueTextOnInput()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValue", 50.0);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderControl>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(controlBuilder =>
                {
                    controlBuilder.OpenComponent<SliderThumb>(0);
                    controlBuilder.AddAttribute(1, "GetAriaValueText", (Func<string, double, int, string>)((formatted, value, _) => $"Current: {value}%"));
                    controlBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var input = cut.Find("input[type='range']");
        input.GetAttribute("aria-valuetext").ShouldBe("Current: 50%");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AdditionalAttributes_AppliedToThumbElement()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValue", 50.0);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderControl>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(controlBuilder =>
                {
                    controlBuilder.OpenComponent<SliderThumb>(0);
                    var attrs = new Dictionary<string, object> { { "data-custom", "test-value" } };
                    controlBuilder.AddAttribute(1, "AdditionalAttributes", (IReadOnlyDictionary<string, object>)attrs);
                    controlBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        // AdditionalAttributes are applied to the thumb element (outer div), not the input
        var thumb = cut.Find("[data-custom='test-value']");
        thumb.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataIndexAttribute()
    {
        var cut = Render(CreateSliderWithThumb(defaultValues: [20, 80], thumbCount: 2));

        var thumb0 = cut.Find("[data-testid='slider-thumb-0']");
        var thumb1 = cut.Find("[data-testid='slider-thumb-1']");

        thumb0.GetAttribute("data-index").ShouldBe("0");
        thumb1.GetAttribute("data-index").ShouldBe("1");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientation()
    {
        var cut = Render(CreateSliderWithThumb(defaultValue: 50, orientation: Orientation.Vertical));

        var thumb = cut.Find("[data-testid='slider-thumb-0']");
        thumb.GetAttribute("data-orientation").ShouldBe("vertical");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateSliderWithThumb(defaultValue: 50, disabled: true));

        var thumb = cut.Find("[data-testid='slider-thumb-0']");
        thumb.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasPositioningStyle()
    {
        var cut = Render(CreateSliderWithThumb(defaultValue: 50));

        var thumb = cut.Find("[data-testid='slider-thumb-0']");
        var style = thumb.GetAttribute("style");

        style.ShouldContain("position: absolute");
        style.ShouldContain("inset-inline-start: 50");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnFocus()
    {
        var invoked = false;

        var cut = Render(CreateSliderWithThumb(
            defaultValue: 50,
            onFocus: EventCallback.Factory.Create<FocusEventArgs>(this, _ => invoked = true)
        ));

        var input = cut.Find("input[type='range']");
        input.Focus();

        invoked.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnBlur()
    {
        var invoked = false;

        var cut = Render(CreateSliderWithThumb(
            defaultValue: 50,
            onBlur: EventCallback.Factory.Create<FocusEventArgs>(this, _ => invoked = true)
        ));

        var input = cut.Find("input[type='range']");
        input.Focus();
        input.Blur();

        invoked.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaValueTextForRangeSlider()
    {
        var cut = Render(CreateSliderWithThumb(defaultValues: [20, 80], thumbCount: 2));

        var inputs = cut.FindAll("input[type='range']");

        inputs[0].GetAttribute("aria-valuetext").ShouldContain("start range");
        inputs[1].GetAttribute("aria-valuetext").ShouldContain("end range");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HandlesNonIntegerValues()
    {
        var cut = Render(CreateSliderWithThumb(defaultValue: 0.5, min: 0, max: 1, step: 0.1));

        var input = cut.Find("input[type='range']");
        input.GetAttribute("aria-valuenow").ShouldBe("0.5");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InputHasCorrectValueAttribute()
    {
        var cut = Render(CreateSliderWithThumb(defaultValue: 75));

        var input = cut.Find("input[type='range']");
        input.GetAttribute("value").ShouldBe("75");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasVerticalPositioningStyle()
    {
        var cut = Render(CreateSliderWithThumb(defaultValue: 50, orientation: Orientation.Vertical));

        var thumb = cut.Find("[data-testid='slider-thumb-0']");
        var style = thumb.GetAttribute("style") ?? "";

        style.ShouldContain("position: absolute");
        // Vertical sliders use bottom instead of inset-inline-start
        style.ShouldContain("bottom: 50");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsThreeOrMoreThumbs()
    {
        var cut = Render(CreateSliderWithThumb(defaultValues: [10, 50, 90], thumbCount: 3));

        var thumbs = cut.FindAll("[data-testid^='slider-thumb-']");
        thumbs.Count.ShouldBe(3);

        var inputs = cut.FindAll("input[type='range']");
        inputs.Count.ShouldBe(3);
        inputs[0].GetAttribute("aria-valuenow").ShouldBe("10");
        inputs[1].GetAttribute("aria-valuenow").ShouldBe("50");
        inputs[2].GetAttribute("aria-valuenow").ShouldBe("90");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InputHasNameAttribute()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValue", 50.0);
            builder.AddAttribute(2, "Name", "brightness");
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderControl>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(controlBuilder =>
                {
                    controlBuilder.OpenComponent<SliderThumb>(0);
                    controlBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var input = cut.Find("input[type='range']");
        input.GetAttribute("name").ShouldBe("brightness");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RangeInputsUseSameNameAttribute()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValues", new[] { 20.0, 80.0 });
            builder.AddAttribute(2, "Name", "price");
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderControl>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(controlBuilder =>
                {
                    controlBuilder.OpenComponent<SliderThumb>(0);
                    controlBuilder.AddAttribute(1, "Index", 0);
                    controlBuilder.CloseComponent();
                    controlBuilder.OpenComponent<SliderThumb>(2);
                    controlBuilder.AddAttribute(3, "Index", 1);
                    controlBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var inputs = cut.FindAll("input[type='range']");
        inputs[0].GetAttribute("name").ShouldBe("price");
        inputs[1].GetAttribute("name").ShouldBe("price");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RangeThumbsCanResolveImplicitIndices()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValues", new[] { 20.0, 80.0 });
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderControl>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(controlBuilder =>
                {
                    controlBuilder.OpenComponent<SliderThumb>(0);
                    controlBuilder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
                    {
                        { "data-testid", "implicit-thumb-0" }
                    });
                    controlBuilder.CloseComponent();
                    controlBuilder.OpenComponent<SliderThumb>(2);
                    controlBuilder.AddAttribute(3, "AdditionalAttributes", new Dictionary<string, object>
                    {
                        { "data-testid", "implicit-thumb-1" }
                    });
                    controlBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var thumb0 = cut.Find("[data-testid='implicit-thumb-0']");
        var thumb1 = cut.Find("[data-testid='implicit-thumb-1']");
        var inputs = cut.FindAll("input[type='range']");

        thumb0.GetAttribute("data-index").ShouldBe("0");
        thumb1.GetAttribute("data-index").ShouldBe("1");
        inputs[0].GetAttribute("aria-valuenow").ShouldBe("20");
        inputs[1].GetAttribute("aria-valuenow").ShouldBe("80");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ClassValueReceivesThumbState()
    {
        SliderThumbState? capturedState = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValue", 75.0);
            builder.AddAttribute(2, "Disabled", true);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderControl>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(controlBuilder =>
                {
                    controlBuilder.OpenComponent<SliderThumb>(0);
                    controlBuilder.AddAttribute(1, "ClassValue", (Func<SliderThumbState, string>)(state =>
                    {
                        capturedState = state;
                        return "thumb-class";
                    }));
                    controlBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        capturedState.ShouldNotBeNull();
        capturedState!.Index.ShouldBe(0);
        capturedState.Disabled.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RangeThumb_HasZIndex2WhenActive()
    {
        var cut = Render(CreateSliderWithThumb(defaultValues: [20, 80], thumbCount: 2));

        // Focus the first thumb's input to make it active
        var inputs = cut.FindAll("input[type='range']");
        inputs[0].Focus();

        var thumb0 = cut.Find("[data-testid='slider-thumb-0']");
        var style0 = thumb0.GetAttribute("style") ?? "";
        style0.ShouldContain("z-index: 2");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RangeThumb_HasZIndex1WhenLastUsed()
    {
        var cut = Render(CreateSliderWithThumb(defaultValues: [20, 80], thumbCount: 2));

        var inputs = cut.FindAll("input[type='range']");

        // Focus thumb 0 then blur it — thumb 0 becomes last-used with no active thumb
        inputs[0].Focus();
        inputs[0].Blur();

        var thumb0 = cut.Find("[data-testid='slider-thumb-0']");
        var style0 = thumb0.GetAttribute("style") ?? "";
        style0.ShouldContain("z-index: 1");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SingleThumb_HasNoZIndexByDefault()
    {
        var cut = Render(CreateSliderWithThumb(defaultValue: 50));

        var thumb = cut.Find("[data-testid='slider-thumb-0']");
        var style = thumb.GetAttribute("style") ?? "";
        style.ShouldNotContain("z-index");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RoundValueToStepUsesJavaScriptHalfUpRoundingAndPrecision()
    {
        SliderUtilities.RoundValueToStep(0.5, 1, 0).ShouldBe(1);
        SliderUtilities.RoundValueToStep(0.35, 0.1, 0.25).ShouldBe(0.35);

        return Task.CompletedTask;
    }
}
