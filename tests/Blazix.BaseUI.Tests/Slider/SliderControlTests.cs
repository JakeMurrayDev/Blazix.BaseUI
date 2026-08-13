namespace Blazix.BaseUI.Tests.Slider;

public class SliderControlTests : BunitContext, ISliderControlContract
{
    public SliderControlTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSliderModule(JSInterop);
    }

    private RenderFragment CreateSliderWithControl(
        Orientation orientation = Orientation.Horizontal,
        bool disabled = false,
        bool readOnly = false,
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
            if (readOnly)
                builder.AddAttribute(4, "ReadOnly", true);
            builder.AddAttribute(5, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderControl>(0);
                if (classValue is not null)
                    innerBuilder.AddAttribute(0, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(1, "StyleValue", styleValue);
                var mergedAttrs = new Dictionary<string, object> { { "data-testid", "slider-control" } };
                if (additionalAttributes is not null)
                {
                    foreach (var kvp in additionalAttributes)
                        mergedAttrs[kvp.Key] = kvp.Value;
                }
                innerBuilder.AddAttribute(2, "AdditionalAttributes", (IReadOnlyDictionary<string, object>)mergedAttrs);
                if (render is not null)
                    innerBuilder.AddAttribute(3, "Render", render);
                innerBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(trackBuilder =>
                {
                    trackBuilder.OpenComponent<SliderThumb>(0);
                    trackBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateSliderWithControl());

        var control = cut.Find("[data-testid='slider-control']");
        control.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<SliderRootState>> render = props => builder =>
        {
            builder.OpenElement(0, "section");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateSliderWithControl(render: render));

        var control = cut.Find("[data-testid='slider-control']");
        control.TagName.ShouldBe("SECTION");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateSliderWithControl(
            additionalAttributes: new Dictionary<string, object>
            {
                { "aria-label", "Volume control" }
            }
        ));

        var control = cut.Find("[data-testid='slider-control']");
        control.GetAttribute("aria-label").ShouldBe("Volume control");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateSliderWithControl(
            classValue: _ => "control-class"
        ));

        var control = cut.Find("[data-testid='slider-control']");
        control.GetAttribute("class").ShouldContain("control-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateSliderWithControl(
            styleValue: _ => "background: blue"
        ));

        var control = cut.Find("[data-testid='slider-control']");
        control.GetAttribute("style").ShouldContain("background: blue");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTouchActionNoneStyle()
    {
        var cut = Render(CreateSliderWithControl());

        var control = cut.Find("[data-testid='slider-control']");
        control.GetAttribute("style").ShouldContain("touch-action: none");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabindexMinusOne()
    {
        var cut = Render(CreateSliderWithControl());

        var control = cut.Find("[data-testid='slider-control']");
        control.GetAttribute("tabindex").ShouldBe("-1");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientation()
    {
        var cut = Render(CreateSliderWithControl(orientation: Orientation.Vertical));

        var control = cut.Find("[data-testid='slider-control']");
        control.GetAttribute("data-orientation").ShouldBe("vertical");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateSliderWithControl(disabled: true));

        var control = cut.Find("[data-testid='slider-control']");
        control.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task EdgeAlignmentAddsBaseUiControlPrehydrationAttribute()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValue", 50.0);
            builder.AddAttribute(2, "ThumbAlignment", ThumbAlignment.Edge);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderControl>(0);
                innerBuilder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
                {
                    { "data-testid", "slider-control" }
                });
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(controlBuilder =>
                {
                    controlBuilder.OpenComponent<SliderThumb>(0);
                    controlBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var control = cut.Find("[data-testid='slider-control']");
        control.HasAttribute("data-base-ui-slider-control").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task DoesNotCommitOnDragEndWhenNoValueApplied()
    {
        // Upstream's `handleTouchEnd` commits only when `currentInteractionValueRef.current != null`,
        // so a press-and-release on an already-positioned thumb commits nothing.
        var committed = 0;

        var cut = Render(builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValue", 50.0);
            builder.AddAttribute(2, "OnValueCommitted",
                EventCallback.Factory.Create<SliderValueCommittedEventArgs<double>>(this, _ => committed++));
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

        var control = cut.FindComponent<SliderControl>().Instance;

        await cut.InvokeAsync(() => control.OnDragEnd([50.0], 0, false, "drag"));

        committed.ShouldBe(0);
    }

    [Fact]
    public async Task CommitsOnDragEndWithOriginatingReason()
    {
        // The commit reason comes from `lastChangeReasonRef`, so a track press that was never
        // dragged commits with the track-press reason.
        SliderChangeReason? reason = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValue", 50.0);
            builder.AddAttribute(2, "OnValueCommitted",
                EventCallback.Factory.Create<SliderValueCommittedEventArgs<double>>(this, args => reason = args.Reason));
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

        var control = cut.FindComponent<SliderControl>().Instance;

        await cut.InvokeAsync(() => control.OnDragEnd([70.0], 0, true, "trackPress"));

        reason.ShouldBe(SliderChangeReason.TrackPress);
    }

    [Fact]
    public async Task DoesNotCommitOnDragEndWhenValueChangeIsCanceled()
    {
        // Upstream assigns `currentInteractionValueRef` only under `if (applied)` and `setValue`
        // returns false for a canceled change, so a drag whose change is canceled never commits.
        var committed = false;

        var cut = Render(builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValue", 50.0);
            builder.AddAttribute(2, "OnValueChange",
                EventCallback.Factory.Create<SliderValueChangeEventArgs<double>>(this, args => args.Cancel()));
            builder.AddAttribute(3, "OnValueCommitted",
                EventCallback.Factory.Create<SliderValueCommittedEventArgs<double>>(this, _ => committed = true));
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(innerBuilder =>
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

        var control = cut.FindComponent<SliderControl>().Instance;

        await cut.InvokeAsync(() => control.OnDragEnd([70.0], 0, true, "drag"));

        committed.ShouldBeFalse();
    }
}
