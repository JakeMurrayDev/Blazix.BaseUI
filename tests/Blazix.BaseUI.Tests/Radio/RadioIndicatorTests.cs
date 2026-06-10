using Blazix.BaseUI.RadioGroup;

namespace Blazix.BaseUI.Tests.Radio;

public class RadioIndicatorTests : BunitContext, IRadioIndicatorContract
{
    public RadioIndicatorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupRadioModule(JSInterop);
    }

    private RenderFragment CreateRadioWithIndicator(
        string value = "a",
        string? defaultValue = null,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        bool keepMounted = false,
        RenderFragment<RenderProps<RadioIndicatorState>>? indicatorRender = null,
        Func<RadioIndicatorState, string>? indicatorClassValue = null,
        Func<RadioIndicatorState, string>? indicatorStyleValue = null,
        IReadOnlyDictionary<string, object>? indicatorAttributes = null,
        RenderFragment? indicatorChildContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<RadioGroup<string>>(0);
            if (defaultValue is not null)
                builder.AddAttribute(1, "DefaultValue", defaultValue);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(groupBuilder =>
            {
                groupBuilder.OpenComponent<RadioRoot<string>>(0);
                var attrIndex = 1;

                groupBuilder.AddAttribute(attrIndex++, "Value", value);
                if (disabled)
                    groupBuilder.AddAttribute(attrIndex++, "Disabled", true);
                if (readOnly)
                    groupBuilder.AddAttribute(attrIndex++, "ReadOnly", true);
                if (required)
                    groupBuilder.AddAttribute(attrIndex++, "Required", true);

                groupBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerBuilder =>
                {
                    innerBuilder.OpenComponent<RadioIndicator>(0);
                    var indAttrIndex = 1;

                    if (keepMounted)
                        innerBuilder.AddAttribute(indAttrIndex++, "KeepMounted", true);
                    if (indicatorRender is not null)
                        innerBuilder.AddAttribute(indAttrIndex++, "Render", indicatorRender);
                    if (indicatorClassValue is not null)
                        innerBuilder.AddAttribute(indAttrIndex++, "ClassValue", indicatorClassValue);
                    if (indicatorStyleValue is not null)
                        innerBuilder.AddAttribute(indAttrIndex++, "StyleValue", indicatorStyleValue);
                    if (indicatorAttributes is not null)
                        innerBuilder.AddAttribute(indAttrIndex++, "AdditionalAttributes", indicatorAttributes);
                    if (indicatorChildContent is not null)
                        innerBuilder.AddAttribute(indAttrIndex++, "ChildContent", indicatorChildContent);

                    innerBuilder.CloseComponent();
                }));

                groupBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateIndicatorWithContext(
        bool checked_ = false,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        bool keepMounted = false,
        Func<RadioIndicatorState, string>? classValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        var state = new RadioRootState(checked_, disabled, readOnly, required, null, false, false, false, false);
        var context = new RadioRootContext
        {
            Checked = checked_,
            Disabled = disabled,
            ReadOnly = readOnly,
            Required = required,
            State = state
        };

        return builder =>
        {
            builder.OpenComponent<CascadingValue<RadioRootContext>>(0);
            builder.AddComponentParameter(1, "Value", context);
            builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<RadioIndicator>(0);
                var attrIndex = 1;

                if (keepMounted)
                    innerBuilder.AddAttribute(attrIndex++, "KeepMounted", true);
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);

                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // Rendering tests
    [Fact]
    public Task RendersAsSpanByDefault()
    {
        var cut = Render(CreateRadioWithIndicator(
            defaultValue: "a",
            indicatorAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<RadioIndicatorState>> renderAsDiv = props => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateRadioWithIndicator(
            defaultValue: "a",
            indicatorRender: renderAsDiv,
            indicatorAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateRadioWithIndicator(
            defaultValue: "a",
            indicatorAttributes: new Dictionary<string, object>
            {
                { "data-testid", "indicator" },
                { "aria-label", "Selected" }
            }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("aria-label").ShouldBe("Selected");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateRadioWithIndicator(
            defaultValue: "a",
            indicatorClassValue: _ => "custom-indicator",
            indicatorAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("class").ShouldContain("custom-indicator");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateRadioWithIndicator(
            defaultValue: "a",
            indicatorStyleValue: _ => "width: 10px",
            indicatorAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("style").ShouldContain("width: 10px");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateRadioWithIndicator(
            defaultValue: "a",
            indicatorClassValue: _ => "dynamic-class",
            indicatorAttributes: new Dictionary<string, object>
            {
                { "data-testid", "indicator" },
                { "class", "static-class" }
            }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        var classAttr = indicator.GetAttribute("class");
        classAttr.ShouldContain("static-class");
        classAttr.ShouldContain("dynamic-class");

        return Task.CompletedTask;
    }

    // Visibility tests
    [Fact]
    public Task DoesNotRenderByDefault()
    {
        var cut = Render(CreateRadioWithIndicator(
            indicatorAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        // Indicator should not be rendered when unchecked without KeepMounted
        var indicators = cut.FindAll("[data-testid='indicator']");
        indicators.Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWhenChecked()
    {
        var cut = Render(CreateRadioWithIndicator(
            defaultValue: "a",
            indicatorAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    // KeepMounted tests
    [Fact]
    public Task KeepsIndicatorMountedWhenUnchecked()
    {
        var cut = Render(CreateRadioWithIndicator(
            keepMounted: true,
            indicatorAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        // Should be present even when unchecked
        var indicator = cut.Find("[data-testid='indicator']");
        indicator.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task KeepsIndicatorMountedWhenChecked()
    {
        var cut = Render(CreateRadioWithIndicator(
            defaultValue: "a",
            keepMounted: true,
            indicatorAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    // Style hooks tests
    [Fact]
    public Task HasDataCheckedWhenChecked()
    {
        var cut = Render(CreateRadioWithIndicator(
            defaultValue: "a",
            indicatorAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-checked").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataUncheckedWhenUncheckedAndKeepMounted()
    {
        var cut = Render(CreateRadioWithIndicator(
            keepMounted: true,
            indicatorAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-unchecked").ShouldBeTrue();
        indicator.HasAttribute("data-checked").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateRadioWithIndicator(
            defaultValue: "a",
            disabled: true,
            indicatorAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataReadonlyWhenReadOnly()
    {
        var cut = Render(CreateRadioWithIndicator(
            defaultValue: "a",
            readOnly: true,
            indicatorAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-readonly").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataRequiredWhenRequired()
    {
        var cut = Render(CreateRadioWithIndicator(
            defaultValue: "a",
            required: true,
            indicatorAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-required").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task InitiallyCheckedIndicatorDoesNotUseStartingStyle()
    {
        var cut = Render(CreateIndicatorWithContext(
            checked_: true,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-starting-style").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task IgnoresStaleExitTransitionWhenCheckedAgain()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<RadioGroup<string>>(0);
            builder.AddAttribute(1, "DefaultValue", "a");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(groupBuilder =>
            {
                groupBuilder.OpenComponent<RadioRoot<string>>(0);
                groupBuilder.AddAttribute(1, "Value", "a");
                groupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
                {
                    innerBuilder.OpenComponent<RadioIndicator>(0);
                    innerBuilder.AddAttribute(1, "AdditionalAttributes",
                        (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "indicator-a" } });
                    innerBuilder.CloseComponent();
                }));
                groupBuilder.CloseComponent();

                groupBuilder.OpenComponent<RadioRoot<string>>(10);
                groupBuilder.AddAttribute(11, "Value", "b");
                groupBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var inputs = cut.FindAll("input[type='radio']");

        await inputs[1].TriggerEventAsync("onchange", new ChangeEventArgs { Value = "b" });
        cut.Find("[data-testid='indicator-a']").HasAttribute("data-ending-style").ShouldBeTrue();

        inputs = cut.FindAll("input[type='radio']");
        await inputs[0].TriggerEventAsync("onchange", new ChangeEventArgs { Value = "a" });

        var indicator = cut.FindComponent<RadioIndicator>();
        await cut.InvokeAsync(indicator.Instance.OnTransitionEnded);

        inputs = cut.FindAll("input[type='radio']");
        await inputs[1].TriggerEventAsync("onchange", new ChangeEventArgs { Value = "b" });

        cut.Find("[data-testid='indicator-a']").HasAttribute("data-ending-style").ShouldBeTrue();
    }

    // Context tests
    [Fact]
    public Task ReceivesStateFromContext()
    {
        RadioIndicatorState? capturedState = null;

        var cut = Render(CreateIndicatorWithContext(
            checked_: true,
            disabled: true,
            classValue: state =>
            {
                capturedState = state;
                return "test";
            }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Checked.ShouldBeTrue();
        capturedState.Disabled.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ThrowsWithoutRadioRootContext()
    {
        var exception = Should.Throw<InvalidOperationException>(() => Render(builder =>
        {
            builder.OpenComponent<RadioIndicator>(0);
            builder.AddAttribute(1, "AdditionalAttributes",
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "indicator" } });
            builder.CloseComponent();
        }));

        exception.Message.ShouldBe(
            "Base UI: RadioRootContext is missing. Radio parts must be placed within <Radio.Root>.");

        return Task.CompletedTask;
    }

    // State tests
    [Fact]
    public Task ClassValueReceivesCorrectState()
    {
        RadioIndicatorState? capturedState = null;

        var cut = Render(CreateRadioWithIndicator(
            defaultValue: "a",
            disabled: true,
            readOnly: true,
            required: true,
            indicatorClassValue: state =>
            {
                capturedState = state;
                return "test-class";
            }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Checked.ShouldBeTrue();
        capturedState.Disabled.ShouldBeTrue();
        capturedState.ReadOnly.ShouldBeTrue();
        capturedState.Required.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task StyleValueReceivesCorrectState()
    {
        RadioIndicatorState? capturedState = null;

        var cut = Render(CreateRadioWithIndicator(
            defaultValue: "a",
            indicatorStyleValue: state =>
            {
                capturedState = state;
                return "color: blue";
            }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Checked.ShouldBeTrue();

        return Task.CompletedTask;
    }

}
