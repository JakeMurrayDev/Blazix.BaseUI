namespace BlazorBaseUI.Tests.Slider;

public class SliderTrackTests : BunitContext, ISliderTrackContract
{
    public SliderTrackTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSliderModule(JSInterop);
    }

    private RenderFragment CreateSliderWithTrack(
        Orientation orientation = Orientation.Horizontal,
        bool disabled = false,
        Func<SliderRootState, string>? classValue = null,
        Func<SliderRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<SliderRootState>>? render = null)
    {
        return builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValue", 50.0);
            builder.AddAttribute(2, "Orientation", orientation);
            if (disabled)
                builder.AddAttribute(3, "Disabled", true);
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderControl>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(controlBuilder =>
                {
                    controlBuilder.OpenComponent<SliderTrack>(0);
                    if (classValue is not null)
                        controlBuilder.AddAttribute(0, "ClassValue", classValue);
                    if (styleValue is not null)
                        controlBuilder.AddAttribute(1, "StyleValue", styleValue);
                    var mergedAttrs = new Dictionary<string, object> { { "data-testid", "slider-track" } };
                    if (additionalAttributes is not null)
                    {
                        foreach (var kvp in additionalAttributes)
                            mergedAttrs[kvp.Key] = kvp.Value;
                    }
                    controlBuilder.AddAttribute(2, "AdditionalAttributes", (IReadOnlyDictionary<string, object>)mergedAttrs);
                    if (render is not null)
                        controlBuilder.AddAttribute(3, "Render", render);
                    controlBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(trackBuilder =>
                    {
                        trackBuilder.OpenComponent<SliderThumb>(0);
                        trackBuilder.CloseComponent();
                    }));
                    controlBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateTrackWithContext(
        SliderRootState state,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            var context = new SliderRootContext { State = state };
            builder.OpenComponent<CascadingValue<SliderRootContext>>(0);
            builder.AddAttribute(1, "Value", context);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderTrack>(0);
                var mergedAttrs = new Dictionary<string, object> { { "data-testid", "slider-track" } };
                if (additionalAttributes is not null)
                {
                    foreach (var kvp in additionalAttributes)
                        mergedAttrs[kvp.Key] = kvp.Value;
                }
                innerBuilder.AddAttribute(1, "AdditionalAttributes", (IReadOnlyDictionary<string, object>)mergedAttrs);
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateSliderWithTrack());

        var track = cut.Find("[data-testid='slider-track']");
        track.TagName.ShouldBe("DIV");

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

        var cut = Render(CreateSliderWithTrack(render: render));

        var track = cut.Find("[data-testid='slider-track']");
        track.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateSliderWithTrack(
            additionalAttributes: new Dictionary<string, object>
            {
                { "aria-hidden", "true" }
            }
        ));

        var track = cut.Find("[data-testid='slider-track']");
        track.GetAttribute("aria-hidden").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateSliderWithTrack(
            classValue: _ => "track-class"
        ));

        var track = cut.Find("[data-testid='slider-track']");
        track.GetAttribute("class").ShouldContain("track-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateSliderWithTrack(
            styleValue: _ => "background: gray"
        ));

        var track = cut.Find("[data-testid='slider-track']");
        track.GetAttribute("style").ShouldContain("background: gray");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasPositionRelativeStyle()
    {
        var cut = Render(CreateSliderWithTrack());

        var track = cut.Find("[data-testid='slider-track']");
        track.GetAttribute("style").ShouldContain("position: relative");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientation()
    {
        var cut = Render(CreateSliderWithTrack(orientation: Orientation.Vertical));

        var track = cut.Find("[data-testid='slider-track']");
        track.GetAttribute("data-orientation").ShouldBe("vertical");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateSliderWithTrack(disabled: true));

        var track = cut.Find("[data-testid='slider-track']");
        track.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDraggingWhenDragging()
    {
        var state = SliderRootState.Default with { Dragging = true };
        var cut = Render(CreateTrackWithContext(state));

        var track = cut.Find("[data-testid='slider-track']");
        track.HasAttribute("data-dragging").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataValidWhenValid()
    {
        var state = SliderRootState.Default with { Valid = true };
        var cut = Render(CreateTrackWithContext(state));

        var track = cut.Find("[data-testid='slider-track']");
        track.HasAttribute("data-valid").ShouldBeTrue();
        track.HasAttribute("data-invalid").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataInvalidWhenInvalid()
    {
        var state = SliderRootState.Default with { Valid = false };
        var cut = Render(CreateTrackWithContext(state));

        var track = cut.Find("[data-testid='slider-track']");
        track.HasAttribute("data-invalid").ShouldBeTrue();
        track.HasAttribute("data-valid").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataValidOrInvalidWhenValidIsNull()
    {
        var state = SliderRootState.Default with { Valid = null };
        var cut = Render(CreateTrackWithContext(state));

        var track = cut.Find("[data-testid='slider-track']");
        track.HasAttribute("data-valid").ShouldBeFalse();
        track.HasAttribute("data-invalid").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataTouchedWhenTouched()
    {
        var state = SliderRootState.Default with { Touched = true };
        var cut = Render(CreateTrackWithContext(state));

        var track = cut.Find("[data-testid='slider-track']");
        track.HasAttribute("data-touched").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDirtyWhenDirty()
    {
        var state = SliderRootState.Default with { Dirty = true };
        var cut = Render(CreateTrackWithContext(state));

        var track = cut.Find("[data-testid='slider-track']");
        track.HasAttribute("data-dirty").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataFocusedWhenFocused()
    {
        var state = SliderRootState.Default with { Focused = true };
        var cut = Render(CreateTrackWithContext(state));

        var track = cut.Find("[data-testid='slider-track']");
        track.HasAttribute("data-focused").ShouldBeTrue();

        return Task.CompletedTask;
    }
}
