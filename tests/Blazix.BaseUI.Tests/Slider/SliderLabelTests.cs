namespace Blazix.BaseUI.Tests.Slider;

public class SliderLabelTests : BunitContext, ISliderLabelContract
{
    public SliderLabelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSliderModule(JSInterop);
        JsInteropSetup.SetupLabelModule(JSInterop);
    }

    private RenderFragment CreateSliderWithLabel(
        Orientation orientation = Orientation.Horizontal,
        bool disabled = false,
        Func<SliderRootState, string>? classValue = null,
        Func<SliderRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<SliderRootState>>? render = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValue", 50.0);
            builder.AddAttribute(2, "Orientation", orientation);
            if (disabled)
                builder.AddAttribute(3, "Disabled", true);
            builder.AddAttribute(5, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderLabel>(0);
                if (classValue is not null)
                    innerBuilder.AddAttribute(0, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(1, "StyleValue", styleValue);
                var mergedAttrs = new Dictionary<string, object> { { "data-testid", "slider-label" } };
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
                else
                    innerBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Volume")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SliderControl>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(controlBuilder =>
                {
                    controlBuilder.OpenComponent<SliderTrack>(0);
                    controlBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(trackBuilder =>
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

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateSliderWithLabel());

        var label = cut.Find("[data-testid='slider-label']");
        label.TagName.ShouldBe("DIV");

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

        var cut = Render(CreateSliderWithLabel(render: render));

        var label = cut.Find("[data-testid='slider-label']");
        label.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateSliderWithLabel(
            additionalAttributes: new Dictionary<string, object>
            {
                { "aria-hidden", "true" }
            }
        ));

        var label = cut.Find("[data-testid='slider-label']");
        label.GetAttribute("aria-hidden").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateSliderWithLabel(
            classValue: _ => "label-class"
        ));

        var label = cut.Find("[data-testid='slider-label']");
        label.GetAttribute("class").ShouldContain("label-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateSliderWithLabel(
            styleValue: _ => "color: red"
        ));

        var label = cut.Find("[data-testid='slider-label']");
        label.GetAttribute("style").ShouldContain("color: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasGeneratedLabelId()
    {
        var cut = Render(CreateSliderWithLabel());

        var label = cut.Find("[data-testid='slider-label']");
        var id = label.GetAttribute("id");
        id.ShouldNotBeNullOrEmpty();
        id.ShouldEndWith("-label");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OverridesUserProvidedId()
    {
        var cut = Render(CreateSliderWithLabel(
            additionalAttributes: new Dictionary<string, object>
            {
                { "id", "my-custom-id" }
            }
        ));

        var label = cut.Find("[data-testid='slider-label']");
        var id = label.GetAttribute("id");
        id.ShouldNotBe("my-custom-id");
        id.ShouldEndWith("-label");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientation()
    {
        var cut = Render(CreateSliderWithLabel(orientation: Orientation.Vertical));

        var label = cut.Find("[data-testid='slider-label']");
        label.GetAttribute("data-orientation").ShouldBe("vertical");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateSliderWithLabel(disabled: true));

        var label = cut.Find("[data-testid='slider-label']");
        label.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDragging()
    {
        var cut = Render(CreateSliderWithLabel());

        var label = cut.Find("[data-testid='slider-label']");
        // Initially not dragging — attribute should not be present (Blazor omits false booleans)
        label.HasAttribute("data-dragging").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RegistersLabelIdToRoot()
    {
        var cut = Render(CreateSliderWithLabel());

        var root = cut.Find("[role='group']");
        var label = cut.Find("[data-testid='slider-label']");
        var labelId = label.GetAttribute("id");

        root.GetAttribute("aria-labelledby").ShouldBe(labelId);

        return Task.CompletedTask;
    }
}
