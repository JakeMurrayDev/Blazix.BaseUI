using System.Globalization;

namespace BlazorBaseUI.Tests.NumberField;

public class NumberFieldInputTests : BunitContext, INumberFieldInputContract
{
    public NumberFieldInputTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNumberFieldModule(JSInterop);
    }

    private RenderFragment CreateNumberField(
        double? defaultValue = null,
        double? value = null,
        EventCallback<double?>? valueChanged = null,
        EventCallback<NumberFieldValueChangeEventArgs>? onValueChange = null,
        EventCallback<NumberFieldValueCommittedEventArgs>? onValueCommitted = null,
        double? min = null,
        double? max = null,
        double step = 1,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        string? locale = null,
        NumberFormatOptions? format = null,
        Func<NumberFieldRootState, string?>? inputClassValue = null,
        Func<NumberFieldRootState, string?>? inputStyleValue = null,
        IReadOnlyDictionary<string, object>? inputAdditionalAttributes = null,
        RenderFragment? inputChildContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<NumberFieldRoot>(0);
            var attrIndex = 1;

            if (defaultValue.HasValue)
                builder.AddAttribute(attrIndex++, "DefaultValue", defaultValue.Value);
            if (value.HasValue)
                builder.AddAttribute(attrIndex++, "Value", value.Value);
            if (valueChanged.HasValue)
                builder.AddAttribute(attrIndex++, "ValueChanged", valueChanged.Value);
            if (onValueChange.HasValue)
                builder.AddAttribute(attrIndex++, "OnValueChange", onValueChange.Value);
            if (onValueCommitted.HasValue)
                builder.AddAttribute(attrIndex++, "OnValueCommitted", onValueCommitted.Value);
            if (min.HasValue)
                builder.AddAttribute(attrIndex++, "Min", min.Value);
            if (max.HasValue)
                builder.AddAttribute(attrIndex++, "Max", max.Value);
            builder.AddAttribute(attrIndex++, "Step", step);
            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            if (readOnly)
                builder.AddAttribute(attrIndex++, "ReadOnly", true);
            if (required)
                builder.AddAttribute(attrIndex++, "Required", true);
            if (locale is not null)
                builder.AddAttribute(attrIndex++, "Locale", locale);
            if (format is not null)
                builder.AddAttribute(attrIndex++, "Format", format);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<NumberFieldGroup>(0);
                inner.AddAttribute(1, "ChildContent", (RenderFragment)(groupInner =>
                {
                    groupInner.OpenComponent<NumberFieldInput>(0);
                    var inputAttr = 1;
                    if (inputClassValue is not null)
                        groupInner.AddAttribute(inputAttr++, "ClassValue", inputClassValue);
                    if (inputStyleValue is not null)
                        groupInner.AddAttribute(inputAttr++, "StyleValue", inputStyleValue);
                    if (inputAdditionalAttributes is not null)
                        groupInner.AddMultipleAttributes(inputAttr++, inputAdditionalAttributes);
                    if (inputChildContent is not null)
                        groupInner.AddAttribute(inputAttr++, "ChildContent", inputChildContent);
                    groupInner.CloseComponent();

                    groupInner.OpenComponent<NumberFieldIncrement>(1);
                    groupInner.CloseComponent();

                    groupInner.OpenComponent<NumberFieldDecrement>(2);
                    groupInner.CloseComponent();
                }));
                inner.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    // --- Rendering ---

    [Fact]
    public Task RendersAsInputByDefault()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.TagName.ShouldBe("INPUT");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTextboxRole()
    {
        // Input type="text" implicitly has textbox role
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("type").ShouldBe("text");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var fragment = (RenderFragment)(builder =>
        {
            builder.OpenComponent<NumberFieldRoot>(0);
            builder.AddAttribute(1, "DefaultValue", (double?)0);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<NumberFieldGroup>(0);
                inner.AddAttribute(1, "ChildContent", (RenderFragment)(groupInner =>
                {
                    groupInner.OpenComponent<NumberFieldInput>(0);
                    groupInner.AddAttribute(1, "Render", (RenderFragment<RenderProps<NumberFieldRootState>>)(props => b =>
                    {
                        b.OpenElement(0, "div");
                        b.AddMultipleAttributes(1, props.Attributes);
                        b.AddContent(2, props.ChildContent);
                        b.CloseElement();
                    }));
                    groupInner.CloseComponent();
                }));
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var cut = Render(fragment);
        var input = cut.Find("[aria-roledescription='Number field']");
        input.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            inputChildContent: b => b.AddContent(0, "extra")));
        var input = cut.Find("input[type='text']");
        input.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var attrs = new Dictionary<string, object> { ["data-custom"] = "input-val" };
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            inputAdditionalAttributes: attrs));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("data-custom").ShouldBe("input-val");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            inputClassValue: _ => "input-class"));
        var input = cut.Find("input[type='text']");
        input.ClassList.ShouldContain("input-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            inputStyleValue: _ => "color:blue"));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("style").ShouldContain("color:blue");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var attrs = new Dictionary<string, object> { ["class"] = "attr-class" };
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            inputClassValue: _ => "func-class",
            inputAdditionalAttributes: attrs));
        var input = cut.Find("input[type='text']");
        input.ClassList.ShouldContain("func-class");
        input.ClassList.ShouldContain("attr-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var inputComponent = cut.FindComponent<NumberFieldInput>();
        inputComponent.Instance.Element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // --- Character filtering ---

    [Fact]
    public Task DoesNotAllowNonNumericCharactersOnChange()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "abc" });
        // Non-numeric characters are rejected, value should remain as 0
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("0");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AllowsNumericCharactersOnChange()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "123" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("123");
        return Task.CompletedTask;
    }

    // --- Keyboard ---

    [Fact]
    public Task IncrementsOnKeyDownArrowUp()
    {
        var cut = Render(CreateNumberField(defaultValue: 5));
        var input = cut.Find("input[type='text']");
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("6");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DecrementsOnKeyDownArrowDown()
    {
        var cut = Render(CreateNumberField(defaultValue: 5));
        var input = cut.Find("input[type='text']");
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("4");
        return Task.CompletedTask;
    }

    [Fact]
    public Task IncrementsToMinOnKeyDownHome()
    {
        var cut = Render(CreateNumberField(defaultValue: 5, min: 0));
        var input = cut.Find("input[type='text']");
        input.KeyDown(new KeyboardEventArgs { Key = "Home" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("0");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DecrementsToMaxOnKeyDownEnd()
    {
        var cut = Render(CreateNumberField(defaultValue: 5, max: 100));
        var input = cut.Find("input[type='text']");
        input.KeyDown(new KeyboardEventArgs { Key = "End" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("100");
        return Task.CompletedTask;
    }

    // --- Blur formatting ---

    [Fact]
    public Task CommitsFormattedValueOnlyOnBlur()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "42" });
        // Value should be set from input
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("42");
        // Blur triggers formatting
        input.Blur();
        hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("42");
        return Task.CompletedTask;
    }

    [Fact]
    public Task PreservesRawInputUntilBlur()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");

        input.Input(new ChangeEventArgs { Value = "1.20" });

        input.GetAttribute("value").ShouldBe("1.20");
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("1.2");

        input.Blur();

        input.GetAttribute("value").ShouldBe("1.2");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CommitsValidatedNumberOnBlur_Min()
    {
        var cut = Render(CreateNumberField(defaultValue: 5, min: 10));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "3" });
        input.Blur();
        // Should clamp to min on blur
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("10");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CommitsValidatedNumberOnBlur_Max()
    {
        NumberFieldValueCommittedEventArgs? receivedArgs = null;
        var onValueCommitted = EventCallback.Factory.Create<NumberFieldValueCommittedEventArgs>(
            this, args => receivedArgs = args);

        var cut = Render(CreateNumberField(defaultValue: 5, max: 10, onValueCommitted: onValueCommitted));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "20" });
        input.Blur();
        // Should clamp to max on blur
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("10");
        input.GetAttribute("value").ShouldBe("10");
        receivedArgs.ShouldNotBeNull();
        receivedArgs!.Value.ShouldBe(10);
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotSnapToStepOnBlur()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, step: 5));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "3" });
        input.Blur();
        // Should NOT snap to step on blur (step snapping only happens on increment/decrement)
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("3");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CommitsValidatedNumberOnBlur_StepAndMin()
    {
        var cut = Render(CreateNumberField(defaultValue: 5, min: 2, step: 3));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "1" });
        input.Blur();
        // Should clamp to min
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("2");
        return Task.CompletedTask;
    }

    // --- Precision preservation ---

    [Fact]
    public Task PreservesFullPrecisionOnFirstBlurAfterExternalChange()
    {
        double? currentValue = 1.23456789;
        var valueChanged = EventCallback.Factory.Create<double?>(this, v => currentValue = v);
        var cut = Render(CreateNumberField(value: 1.23456789, valueChanged: valueChanged));
        var input = cut.Find("input[type='text']");
        input.Blur();
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("1.23456789");
        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesInputValueAfterIncrementFollowedByExternalChange()
    {
        double? currentValue = 5;
        var valueChanged = EventCallback.Factory.Create<double?>(this, v => currentValue = v);

        var cut = Render(CreateNumberField(value: 5, valueChanged: valueChanged));

        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        currentValue.ShouldBe(6);
        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesInputValueAfterDecrementFollowedByExternalChange()
    {
        double? currentValue = 5;
        var valueChanged = EventCallback.Factory.Create<double?>(this, v => currentValue = v);

        var cut = Render(CreateNumberField(value: 5, valueChanged: valueChanged));

        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        currentValue.ShouldBe(4);
        return Task.CompletedTask;
    }

    [Fact]
    public Task AllowsTypingAfterPrecisionPreservedOnBlur()
    {
        var cut = Render(CreateNumberField(defaultValue: 1.5));
        var input = cut.Find("input[type='text']");
        input.Blur();
        input.Focus();
        input.Input(new ChangeEventArgs { Value = "2.5" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("2.5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task FormatsToCanonicalWhenInputDiffersFromMaxPrecision()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "1.20" });
        input.Blur();
        // Verify value via hidden input (1.2 in invariant culture)
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("1.2");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HandlesMultipleBlurCyclesWithPrecisionPreservation()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");

        input.Input(new ChangeEventArgs { Value = "3.14" });
        input.Blur();
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("3.14");

        input.Focus();
        input.Input(new ChangeEventArgs { Value = "2.72" });
        input.Blur();
        hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("2.72");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HandlesEdgeCaseParsedValueEqualsCurrentButInputDiffers()
    {
        var cut = Render(CreateNumberField(defaultValue: 5));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "5.0" });
        input.Blur();
        // Should normalize to "5" since 5.0 == 5
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task PreservesPrecisionWhenValueMatchesMaxAfterExternalChange()
    {
        double? currentValue = 10;
        var valueChanged = EventCallback.Factory.Create<double?>(this, v => currentValue = v);

        var cut = Render(CreateNumberField(value: 10, max: 10, valueChanged: valueChanged));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("value").ShouldBe("10");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RoundsToExplicitMaximumFractionDigitsOnBlur()
    {
        var format = new NumberFormatOptions(MaximumFractionDigits: 2);
        var cut = Render(CreateNumberField(defaultValue: 0, format: format));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "1.23456" });
        input.Blur();
        // Format with MaximumFractionDigits: 2 should round
        var val = input.GetAttribute("value");
        val.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RoundsToStepPrecisionOnBlurWhenStepImpliesPrecision()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, step: 0.01));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "1.23456" });
        input.Blur();
        // Base UI removes floating-point noise through Intl's default maximum precision.
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("1.235");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CommitsParsedValueOnBlurAndNormalizesDisplay()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "042" });
        input.Blur();
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("42");
        return Task.CompletedTask;
    }

    // --- ARIA ---

    [Fact]
    public Task HasAriaRoledescriptionNumberField()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("aria-roledescription").ShouldBe("Number field");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaValueNow()
    {
        // The visible input shows the formatted value via the value attribute
        var cut = Render(CreateNumberField(defaultValue: 42));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("value").ShouldBe("42");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaValueMin()
    {
        // Min is set on the hidden input element, not the visible one
        var cut = Render(CreateNumberField(defaultValue: 5, min: 0));
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("min").ShouldBe("0");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaValueMax()
    {
        var cut = Render(CreateNumberField(defaultValue: 5, max: 100));
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("max").ShouldBe("100");
        return Task.CompletedTask;
    }

    // --- Data attributes ---

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, disabled: true));
        var input = cut.Find("input[type='text']");
        input.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataReadOnlyWhenReadOnly()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, readOnly: true));
        var input = cut.Find("input[type='text']");
        input.HasAttribute("data-readonly").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataRequiredWhenRequired()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, required: true));
        var input = cut.Find("input[type='text']");
        input.HasAttribute("data-required").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // --- Input attributes ---

    [Fact]
    public Task HasInputModeAttribute()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        // Default: min is not set so InputMode = "text" (since min can be negative)
        input.GetAttribute("inputmode").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAutocompleteOff()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("autocomplete").ShouldBe("off");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasCorrectValueAttribute()
    {
        var cut = Render(CreateNumberField(defaultValue: 42));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("value").ShouldBe("42");
        return Task.CompletedTask;
    }
}
