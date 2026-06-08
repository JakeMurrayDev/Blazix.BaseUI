namespace Blazix.BaseUI.Tests.Checkbox;

public class CheckboxIndicatorTests : BunitContext, ICheckboxIndicatorContract
{
    public CheckboxIndicatorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupCheckboxModule(JSInterop);
    }

    private RenderFragment CreateCheckboxWithIndicator(
        bool? isChecked = null,
        bool defaultChecked = false,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        bool indeterminate = false,
        bool keepMounted = false,
        Func<CheckboxIndicatorState, string>? indicatorClassValue = null,
        Func<CheckboxIndicatorState, string>? indicatorStyleValue = null,
        IReadOnlyDictionary<string, object>? indicatorAdditionalAttributes = null,
        RenderFragment<RenderProps<CheckboxIndicatorState>>? indicatorRender = null)
    {
        return builder =>
        {
            builder.OpenComponent<CheckboxRoot>(0);
            var attrIndex = 1;

            if (isChecked.HasValue)
                builder.AddAttribute(attrIndex++, "Checked", isChecked.Value);
            if (defaultChecked)
                builder.AddAttribute(attrIndex++, "DefaultChecked", true);
            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            if (readOnly)
                builder.AddAttribute(attrIndex++, "ReadOnly", true);
            if (required)
                builder.AddAttribute(attrIndex++, "Required", true);
            if (indeterminate)
                builder.AddAttribute(attrIndex++, "Indeterminate", true);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<CheckboxIndicator>(0);
                var indicatorAttrIndex = 1;

                if (keepMounted)
                    innerBuilder.AddAttribute(indicatorAttrIndex++, "KeepMounted", true);
                if (indicatorClassValue is not null)
                    innerBuilder.AddAttribute(indicatorAttrIndex++, "ClassValue", indicatorClassValue);
                if (indicatorStyleValue is not null)
                    innerBuilder.AddAttribute(indicatorAttrIndex++, "StyleValue", indicatorStyleValue);
                if (indicatorAdditionalAttributes is not null)
                    innerBuilder.AddAttribute(indicatorAttrIndex++, "AdditionalAttributes", indicatorAdditionalAttributes);
                if (indicatorRender is not null)
                    innerBuilder.AddAttribute(indicatorAttrIndex++, "Render", indicatorRender);

                innerBuilder.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    private RenderFragment CreateIndicatorWithContext(CheckboxRootContext context,
        bool keepMounted = false,
        Func<CheckboxIndicatorState, string>? classValue = null,
        Func<CheckboxIndicatorState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<CascadingValue<CheckboxRootContext>>(0);
            builder.AddAttribute(1, "Value", context);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<CheckboxIndicator>(0);
                var attrIndex = 1;

                if (keepMounted)
                    innerBuilder.AddAttribute(attrIndex++, "KeepMounted", true);
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
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
        var cut = Render(CreateCheckboxWithIndicator(
            defaultChecked: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<CheckboxIndicatorState>> renderAsDiv = props => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateCheckboxWithIndicator(
            defaultChecked: true,
            indicatorRender: renderAsDiv,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateCheckboxWithIndicator(
            defaultChecked: true,
            indicatorAdditionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "indicator" },
                { "aria-label", "Check indicator" }
            }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("aria-label").ShouldBe("Check indicator");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateCheckboxWithIndicator(
            defaultChecked: true,
            indicatorClassValue: _ => "indicator-class",
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("class").ShouldContain("indicator-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateCheckboxWithIndicator(
            defaultChecked: true,
            indicatorStyleValue: _ => "background: green",
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("style").ShouldContain("background: green");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateCheckboxWithIndicator(
            defaultChecked: true,
            indicatorClassValue: _ => "dynamic-class",
            indicatorAdditionalAttributes: new Dictionary<string, object>
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
        var cut = Render(CreateCheckboxWithIndicator(
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicators = cut.FindAll("[data-testid='indicator']");
        indicators.Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWhenChecked()
    {
        var cut = Render(CreateCheckboxWithIndicator(
            defaultChecked: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWhenIndeterminate()
    {
        var cut = Render(CreateCheckboxWithIndicator(
            indeterminate: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    // keepMounted tests
    [Fact]
    public Task KeepsIndicatorMountedWhenUnchecked()
    {
        var cut = Render(CreateCheckboxWithIndicator(
            keepMounted: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task KeepsIndicatorMountedWhenChecked()
    {
        var cut = Render(CreateCheckboxWithIndicator(
            defaultChecked: true,
            keepMounted: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task KeepsIndicatorMountedWhenIndeterminate()
    {
        var cut = Render(CreateCheckboxWithIndicator(
            indeterminate: true,
            keepMounted: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    // Style hooks (data attributes) tests
    [Fact]
    public Task HasDataCheckedWhenChecked()
    {
        var cut = Render(CreateCheckboxWithIndicator(
            defaultChecked: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-checked").ShouldBeTrue();
        indicator.HasAttribute("data-unchecked").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataUncheckedWhenUncheckedAndKeepMounted()
    {
        var cut = Render(CreateCheckboxWithIndicator(
            keepMounted: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-unchecked").ShouldBeTrue();
        indicator.HasAttribute("data-checked").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataIndeterminateWhenIndeterminate()
    {
        var cut = Render(CreateCheckboxWithIndicator(
            indeterminate: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-indeterminate").ShouldBeTrue();
        indicator.HasAttribute("data-checked").ShouldBeFalse();
        indicator.HasAttribute("data-unchecked").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateCheckboxWithIndicator(
            defaultChecked: true,
            disabled: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataReadonlyWhenReadOnly()
    {
        var cut = Render(CreateCheckboxWithIndicator(
            defaultChecked: true,
            readOnly: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-readonly").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataRequiredWhenRequired()
    {
        var cut = Render(CreateCheckboxWithIndicator(
            defaultChecked: true,
            required: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-required").ShouldBeTrue();

        return Task.CompletedTask;
    }

    // Context tests
    [Fact]
    public Task ReceivesStateFromContext()
    {
        var rootState = new CheckboxRootState(
            Checked: true,
            Disabled: true,
            ReadOnly: true,
            Required: true,
            Indeterminate: false,
            Valid: null,
            Touched: false,
            Dirty: false,
            Filled: true,
            Focused: false);

        var context = new CheckboxRootContext
        {
            Checked = true,
            Disabled = true,
            ReadOnly = true,
            Required = true,
            Indeterminate = false,
            State = rootState
        };

        var cut = Render(CreateIndicatorWithContext(
            context,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-checked").ShouldBeTrue();
        indicator.HasAttribute("data-disabled").ShouldBeTrue();
        indicator.HasAttribute("data-readonly").ShouldBeTrue();
        indicator.HasAttribute("data-required").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ThrowsWhenRenderedOutsideCheckboxRoot()
    {
        var exception = Should.Throw<InvalidOperationException>(() => Render(builder =>
        {
            builder.OpenComponent<CheckboxIndicator>(0);
            builder.AddAttribute(1, "KeepMounted", true);
            builder.AddAttribute(2, "AdditionalAttributes",
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "indicator" } });
            builder.CloseComponent();
        }));

        exception.Message.ShouldBe(
            "Base UI: CheckboxRootContext is missing. Checkbox parts must be placed within <Checkbox.Root>.");
        return Task.CompletedTask;
    }

    // State tests
    [Fact]
    public Task ClassValueReceivesCorrectState()
    {
        CheckboxIndicatorState? capturedState = null;

        var cut = Render(CreateCheckboxWithIndicator(
            defaultChecked: true,
            disabled: true,
            indicatorClassValue: state =>
            {
                capturedState = state;
                return "indicator-class";
            },
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Value.Checked.ShouldBeTrue();
        capturedState.Value.Disabled.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task StyleValueReceivesCorrectState()
    {
        CheckboxIndicatorState? capturedState = null;

        var cut = Render(CreateCheckboxWithIndicator(
            defaultChecked: true,
            indicatorStyleValue: state =>
            {
                capturedState = state;
                return "color: blue";
            },
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Value.Checked.ShouldBeTrue();

        return Task.CompletedTask;
    }
}
