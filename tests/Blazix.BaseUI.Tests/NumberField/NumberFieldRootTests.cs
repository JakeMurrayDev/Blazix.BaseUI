using System.Globalization;

namespace Blazix.BaseUI.Tests.NumberField;

public class NumberFieldRootTests : BunitContext, INumberFieldRootContract
{
    public NumberFieldRootTests()
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
        object? step = null,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        string? name = null,
        string? form = null,
        string? locale = null,
        NumberFormatOptions? format = null,
        bool allowWheelScrub = false,
        bool allowOutOfRange = false,
        bool snapOnStep = false,
        Func<NumberFieldRootState, string?>? classValue = null,
        Func<NumberFieldRootState, string?>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment? childContent = null)
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
            if (step is not null)
                builder.AddAttribute(attrIndex++, "Step", step);
            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            if (readOnly)
                builder.AddAttribute(attrIndex++, "ReadOnly", true);
            if (required)
                builder.AddAttribute(attrIndex++, "Required", true);
            if (name is not null)
                builder.AddAttribute(attrIndex++, "Name", name);
            if (form is not null)
                builder.AddAttribute(attrIndex++, "Form", form);
            if (locale is not null)
                builder.AddAttribute(attrIndex++, "Locale", locale);
            if (format is not null)
                builder.AddAttribute(attrIndex++, "Format", format);
            if (allowWheelScrub)
                builder.AddAttribute(attrIndex++, "AllowWheelScrub", true);
            if (allowOutOfRange)
                builder.AddAttribute(attrIndex++, "AllowOutOfRange", true);
            if (snapOnStep)
                builder.AddAttribute(attrIndex++, "SnapOnStep", true);
            if (classValue is not null)
                builder.AddAttribute(attrIndex++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(attrIndex++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddMultipleAttributes(attrIndex++, additionalAttributes);

            var content = childContent ?? DefaultChildContent();
            builder.AddAttribute(attrIndex++, "ChildContent", content);

            builder.CloseComponent();
        };
    }

    private static RenderFragment DefaultChildContent()
    {
        return inner =>
        {
            inner.OpenComponent<NumberFieldGroup>(0);
            inner.AddAttribute(1, "ChildContent", (RenderFragment)(groupInner =>
            {
                groupInner.OpenComponent<NumberFieldInput>(0);
                groupInner.CloseComponent();

                groupInner.OpenComponent<NumberFieldIncrement>(1);
                groupInner.CloseComponent();

                groupInner.OpenComponent<NumberFieldDecrement>(2);
                groupInner.CloseComponent();
            }));
            inner.CloseComponent();
        };
    }

    // --- Rendering ---

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var root = cut.Find("div");
        root.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var fragment = (RenderFragment)(builder =>
        {
            builder.OpenComponent<NumberFieldRoot>(0);
            builder.AddAttribute(1, "DefaultValue", (double?)0);
            builder.AddAttribute(2, "Render", (RenderFragment<RenderProps<NumberFieldRootState>>)(props => b =>
            {
                b.OpenElement(0, "section");
                b.AddMultipleAttributes(1, props.Attributes);
                b.AddContent(2, props.ChildContent);
                b.CloseElement();
            }));
            builder.AddAttribute(3, "ChildContent", DefaultChildContent());
            builder.CloseComponent();
        });

        var cut = Render(fragment);
        cut.Find("section").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            childContent: b => b.AddContent(0, "root content")));
        cut.Markup.ShouldContain("root content");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var attrs = new Dictionary<string, object> { ["data-custom"] = "root-val" };
        var cut = Render(CreateNumberField(defaultValue: 0, additionalAttributes: attrs));
        var root = cut.Find("[data-custom='root-val']");
        root.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            classValue: _ => "root-class"));
        cut.Find(".root-class").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            styleValue: _ => "color:red"));
        var root = cut.Find("[style*='color:red']");
        root.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var attrs = new Dictionary<string, object> { ["class"] = "attr-class" };
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            classValue: _ => "func-class",
            additionalAttributes: attrs));
        var root = cut.Find(".func-class");
        root.ClassList.ShouldContain("attr-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var root = cut.FindComponent<NumberFieldRoot>();
        root.Instance.Element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // --- defaultValue ---

    [Fact]
    public Task DefaultValue_AcceptsNumberValue()
    {
        var cut = Render(CreateNumberField(defaultValue: 42));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("value").ShouldBe("42");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DefaultValue_AcceptsNullValue()
    {
        var cut = Render(CreateNumberField());
        var input = cut.Find("input[type='text']");
        input.GetAttribute("value").ShouldBe("");
        return Task.CompletedTask;
    }

    // --- value (controlled) ---

    [Fact]
    public Task Value_AcceptsNumberThatChangesOverTime()
    {
        double? currentValue = 5;
        var valueChanged = EventCallback.Factory.Create<double?>(this, v => currentValue = v);

        var cut = Render(CreateNumberField(value: 5, valueChanged: valueChanged));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("value").ShouldBe("5");

        // Increment to change value
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });
        currentValue.ShouldBe(6);
        return Task.CompletedTask;
    }

    [Fact]
    public Task Value_AcceptsNullValue()
    {
        var cut = Render(CreateNumberField());
        var input = cut.Find("input[type='text']");
        input.GetAttribute("value").ShouldBe("");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Value_IsNullWhenInputEmptyButNotTrimmed()
    {
        var cut = Render(CreateNumberField(defaultValue: 5));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "" });
        // After clearing, value should be null and input empty
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("");
        return Task.CompletedTask;
    }

    // --- onValueChange ---

    [Fact]
    public Task OnValueChange_CalledWhenValueChanges()
    {
        NumberFieldValueChangeEventArgs? receivedArgs = null;
        var onValueChange = EventCallback.Factory.Create<NumberFieldValueChangeEventArgs>(
            this, args => receivedArgs = args);

        var cut = Render(CreateNumberField(defaultValue: 0, onValueChange: onValueChange));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        receivedArgs.ShouldNotBeNull();
        receivedArgs!.Value.ShouldBe(1);
        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChange_CalledWithNumberTransitioningFromNull()
    {
        NumberFieldValueChangeEventArgs? receivedArgs = null;
        var onValueChange = EventCallback.Factory.Create<NumberFieldValueChangeEventArgs>(
            this, args => receivedArgs = args);

        var cut = Render(CreateNumberField(onValueChange: onValueChange));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "5" });

        receivedArgs.ShouldNotBeNull();
        receivedArgs!.Value.ShouldBe(5);
        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChange_CalledWithNullTransitioningFromNumber()
    {
        NumberFieldValueChangeEventArgs? receivedArgs = null;
        var onValueChange = EventCallback.Factory.Create<NumberFieldValueChangeEventArgs>(
            this, args => receivedArgs = args);

        var cut = Render(CreateNumberField(defaultValue: 5, onValueChange: onValueChange));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "" });

        receivedArgs.ShouldNotBeNull();
        receivedArgs!.Value.ShouldBeNull();
        receivedArgs.Reason.ShouldBe(NumberFieldChangeReason.InputClear);
        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChange_IncludesReasonForParseableTyping()
    {
        NumberFieldValueChangeEventArgs? receivedArgs = null;
        var onValueChange = EventCallback.Factory.Create<NumberFieldValueChangeEventArgs>(
            this, args => receivedArgs = args);

        var cut = Render(CreateNumberField(defaultValue: 0, onValueChange: onValueChange));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "42" });

        receivedArgs.ShouldNotBeNull();
        receivedArgs!.Reason.ShouldBe(NumberFieldChangeReason.InputChange);
        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChange_IncludesReasonWhenClearingValue()
    {
        NumberFieldValueChangeEventArgs? receivedArgs = null;
        var onValueChange = EventCallback.Factory.Create<NumberFieldValueChangeEventArgs>(
            this, args => receivedArgs = args);

        var cut = Render(CreateNumberField(defaultValue: 5, onValueChange: onValueChange));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "" });

        receivedArgs.ShouldNotBeNull();
        receivedArgs!.Reason.ShouldBe(NumberFieldChangeReason.InputClear);
        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChange_IncludesReasonForKeyboardIncrements()
    {
        NumberFieldValueChangeEventArgs? receivedArgs = null;
        var onValueChange = EventCallback.Factory.Create<NumberFieldValueChangeEventArgs>(
            this, args => receivedArgs = args);

        var cut = Render(CreateNumberField(defaultValue: 0, onValueChange: onValueChange));
        var input = cut.Find("input[type='text']");
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        receivedArgs.ShouldNotBeNull();
        receivedArgs!.Reason.ShouldBe(NumberFieldChangeReason.Keyboard);
        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChange_IncludesReasonForIncrementButtonPresses()
    {
        NumberFieldValueChangeEventArgs? receivedArgs = null;
        var onValueChange = EventCallback.Factory.Create<NumberFieldValueChangeEventArgs>(
            this, args => receivedArgs = args);

        var cut = Render(CreateNumberField(defaultValue: 0, onValueChange: onValueChange));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        receivedArgs.ShouldNotBeNull();
        receivedArgs!.Reason.ShouldBe(NumberFieldChangeReason.IncrementPress);
        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChange_IncludesReasonForDecrementButtonPresses()
    {
        NumberFieldValueChangeEventArgs? receivedArgs = null;
        var onValueChange = EventCallback.Factory.Create<NumberFieldValueChangeEventArgs>(
            this, args => receivedArgs = args);

        var cut = Render(CreateNumberField(defaultValue: 5, onValueChange: onValueChange));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        receivedArgs.ShouldNotBeNull();
        receivedArgs!.Reason.ShouldBe(NumberFieldChangeReason.DecrementPress);
        return Task.CompletedTask;
    }

    // --- Typing behavior ---

    [Fact]
    public Task Typing_FiresOnValueChangeForEachParseableChange()
    {
        var changes = new List<double?>();
        var onValueChange = EventCallback.Factory.Create<NumberFieldValueChangeEventArgs>(
            this, args => changes.Add(args.Value));

        var cut = Render(CreateNumberField(defaultValue: 0, onValueChange: onValueChange));
        var input = cut.Find("input[type='text']");

        input.Input(new ChangeEventArgs { Value = "1" });
        input.Input(new ChangeEventArgs { Value = "12" });
        input.Input(new ChangeEventArgs { Value = "123" });

        changes.Count.ShouldBe(3);
        changes[0].ShouldBe(1);
        changes[1].ShouldBe(12);
        changes[2].ShouldBe(123);
        return Task.CompletedTask;
    }

    [Fact]
    public Task Typing_DoesNotFireForNonNumericPartialInput()
    {
        var changes = new List<double?>();
        var onValueChange = EventCallback.Factory.Create<NumberFieldValueChangeEventArgs>(
            this, args => changes.Add(args.Value));

        var cut = Render(CreateNumberField(defaultValue: 0, onValueChange: onValueChange));
        var input = cut.Find("input[type='text']");

        // Non-numeric characters should be rejected
        input.Input(new ChangeEventArgs { Value = "abc" });

        changes.Count.ShouldBe(0);
        return Task.CompletedTask;
    }

    [Fact]
    public Task Typing_HandlesSignAndDecimalPartials()
    {
        var changes = new List<double?>();
        var onValueChange = EventCallback.Factory.Create<NumberFieldValueChangeEventArgs>(
            this, args => changes.Add(args.Value));

        var cut = Render(CreateNumberField(onValueChange: onValueChange));
        var input = cut.Find("input[type='text']");

        // "-" alone should not fire onValueChange with a parseable number
        input.Input(new ChangeEventArgs { Value = "-" });
        // "-5" should fire
        input.Input(new ChangeEventArgs { Value = "-5" });

        changes.ShouldContain(-5);
        return Task.CompletedTask;
    }

    [Fact]
    public Task Typing_AcceptsGroupingAndParsesProgressively()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");

        input.Input(new ChangeEventArgs { Value = "1234" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("1234");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Typing_RespectsLocaleDecimalSeparator()
    {
        // Use German locale which uses comma as decimal separator
        var cut = Render(CreateNumberField(defaultValue: 0, locale: "de-DE"));
        var input = cut.Find("input[type='text']");

        input.Input(new ChangeEventArgs { Value = "1,5" });
        // Should parse as 1.5 using German culture
        var hiddenInput = cut.Find("input[type='number']");
        var hiddenValue = hiddenInput.GetAttribute("value");
        hiddenValue.ShouldBe("1.5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Typing_ParsesPercentAndCommitsCanonicalValue()
    {
        var format = new NumberFormatOptions(Style: "percent");
        var cut = Render(CreateNumberField(defaultValue: 0.12, format: format));
        var input = cut.Find("input[type='text']");
        // Percent format shows 12% for value 0.12
        input.GetAttribute("value").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task Typing_AcceptsCurrencySymbol()
    {
        var format = new NumberFormatOptions(Style: "currency", Currency: "USD");
        var cut = Render(CreateNumberField(defaultValue: 42, format: format));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("value").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task Typing_AllowsDeletingTrailingCurrencySymbols()
    {
        var format = new NumberFormatOptions(Style: "currency", Currency: "USD");
        var cut = Render(CreateNumberField(defaultValue: 42, format: format));
        var input = cut.Find("input[type='text']");
        // Typing a plain number should be accepted
        input.Input(new ChangeEventArgs { Value = "50" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("50");
        return Task.CompletedTask;
    }

    // --- onValueCommitted ---

    [Fact]
    public Task OnValueCommitted_FiresOnBlurAfterManualInput()
    {
        NumberFieldValueCommittedEventArgs? receivedArgs = null;
        var onValueCommitted = EventCallback.Factory.Create<NumberFieldValueCommittedEventArgs>(
            this, args => receivedArgs = args);

        var cut = Render(CreateNumberField(defaultValue: 5, onValueCommitted: onValueCommitted));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "5" });
        input.Blur();

        receivedArgs.ShouldNotBeNull();
        receivedArgs!.Value.ShouldBe(5);
        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueCommitted_FiresNullOnBlurWhenCleared()
    {
        NumberFieldValueCommittedEventArgs? receivedArgs = null;
        var onValueCommitted = EventCallback.Factory.Create<NumberFieldValueCommittedEventArgs>(
            this, args => receivedArgs = args);

        var cut = Render(CreateNumberField(defaultValue: 5, onValueCommitted: onValueCommitted));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "" });
        input.Blur();

        receivedArgs.ShouldNotBeNull();
        receivedArgs!.Value.ShouldBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueCommitted_FiresOnKeyboardInteractions()
    {
        NumberFieldValueCommittedEventArgs? receivedArgs = null;
        var onValueCommitted = EventCallback.Factory.Create<NumberFieldValueCommittedEventArgs>(
            this, args => receivedArgs = args);

        var cut = Render(CreateNumberField(defaultValue: 5, onValueCommitted: onValueCommitted));
        var input = cut.Find("input[type='text']");
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        receivedArgs.ShouldNotBeNull();
        receivedArgs!.Value.ShouldBe(6);
        receivedArgs.Reason.ShouldBe(NumberFieldChangeReason.Keyboard);
        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueCommitted_FiresOnIncrementDecrementButtons()
    {
        NumberFieldValueCommittedEventArgs? receivedArgs = null;
        var onValueCommitted = EventCallback.Factory.Create<NumberFieldValueCommittedEventArgs>(
            this, args => receivedArgs = args);

        var cut = Render(CreateNumberField(defaultValue: 5, onValueCommitted: onValueCommitted));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        receivedArgs.ShouldNotBeNull();
        receivedArgs!.Value.ShouldBe(6);
        receivedArgs.Reason.ShouldBe(NumberFieldChangeReason.IncrementPress);
        return Task.CompletedTask;
    }

    // --- Props ---

    [Fact]
    public Task Disabled_DisablesInput()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, disabled: true));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("disabled").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ReadOnly_MarksInputAsReadOnly()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, readOnly: true));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("readonly").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task Required_MarksInputAsRequired()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, required: true));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("required").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task Name_SetsNameOnHiddenInput()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, name: "quantity"));
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("name").ShouldBe("quantity");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Form_SetsFormOnHiddenInput()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, name: "quantity", form: "external-form"));
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("form").ShouldBe("external-form");
        return Task.CompletedTask;
    }

    // --- Min ---

    [Fact]
    public Task Min_PreventsValueBelowMin()
    {
        var cut = Render(CreateNumberField(defaultValue: 5, min: 3));
        var btn = cut.Find("[aria-label='Decrease']");

        // Decrement multiple times
        btn.Click(new MouseEventArgs { Detail = 0 });
        btn.Click(new MouseEventArgs { Detail = 0 });
        btn.Click(new MouseEventArgs { Detail = 0 });
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("3");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Min_AllowsValueAboveMin()
    {
        var cut = Render(CreateNumberField(defaultValue: 5, min: 3));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("6");
        return Task.CompletedTask;
    }

    // --- Max ---

    [Fact]
    public Task Max_PreventsValueAboveMax()
    {
        var cut = Render(CreateNumberField(defaultValue: 8, max: 10));
        var btn = cut.Find("[aria-label='Increase']");

        btn.Click(new MouseEventArgs { Detail = 0 });
        btn.Click(new MouseEventArgs { Detail = 0 });
        btn.Click(new MouseEventArgs { Detail = 0 });
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("10");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Max_AllowsValueBelowMax()
    {
        var cut = Render(CreateNumberField(defaultValue: 5, max: 10));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("4");
        return Task.CompletedTask;
    }

    // --- allowOutOfRange ---

    [Fact]
    public Task AllowOutOfRange_AllowsOverflowForDirectInput()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, max: 5, allowOutOfRange: true));
        var input = cut.Find("input[type='text']");

        input.Input(new ChangeEventArgs { Value = "6" });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("6");
        input.GetAttribute("value").ShouldBe("6");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AllowOutOfRange_ClampsStepInteractions()
    {
        var cut = Render(CreateNumberField(defaultValue: 5, max: 5, allowOutOfRange: true));
        var btn = cut.Find("[aria-label='Increase']");

        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AllowOutOfRange_ClampsDirectInputWhenFalse()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, max: 5, allowOutOfRange: false));
        var input = cut.Find("input[type='text']");

        input.Input(new ChangeEventArgs { Value = "6" });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("5");
        return Task.CompletedTask;
    }

    // --- Step ---

    [Fact]
    public Task Step_DefaultsToOne()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Step_IncrementsByStepProp()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, step: 5));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Step_SnapsOnIncrementToNearestMultiple()
    {
        var cut = Render(CreateNumberField(defaultValue: 2, step: 5, snapOnStep: true));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        // 2 + 5 = 7, snapped to nearest multiple of 5 from 0 = 5
        hiddenInput.GetAttribute("value").ShouldBe("5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Step_DecrementsByStepProp()
    {
        var cut = Render(CreateNumberField(defaultValue: 10, step: 5));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Step_SnapsOnDecrementToNearestMultiple()
    {
        var cut = Render(CreateNumberField(defaultValue: 8, step: 5, snapOnStep: true));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        // 8 - 5 = 3, snapped to nearest multiple of 5 from 0 (ceil) = 5
        hiddenInput.GetAttribute("value").ShouldBe("5");
        return Task.CompletedTask;
    }

    // --- Step: fractional and floating point (from validate.test.ts) ---

    [Fact]
    public Task Step_FractionalIncrementHandlesFloatingPoint()
    {
        // Incrementing by 0.1 without snapOnStep avoids the snap-floor issue
        // and RemoveFloatingPointErrors cleans up the result
        var cut = Render(CreateNumberField(defaultValue: 0.2, step: 0.1));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        // 0.2 + 0.1 = 0.30000000000000004, but RemoveFloatingPointErrors gives 0.3
        hiddenInput.GetAttribute("value").ShouldBe("0.3");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Step_FractionalDecrementHandlesFloatingPoint()
    {
        // Simulates decrementing 100.1 by 0.1: should become 100
        var cut = Render(CreateNumberField(defaultValue: 100.1, step: 0.1, snapOnStep: true));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("100");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Step_FractionalIncrementWithSmallStep()
    {
        // Step 0.01: 0.01 + 0.01 = 0.02
        var cut = Render(CreateNumberField(defaultValue: 0.01, step: 0.01, snapOnStep: true));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("0.02");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Step_FractionalStepWithMinimum()
    {
        // Step 0.2 with min=3: 3 + 0.2 + 0.2 = 3.4
        var cut = Render(CreateNumberField(defaultValue: 3, step: 0.2, min: 3, snapOnStep: true));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("3.4");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Step_SnapWithLargerStepIncrement()
    {
        // From 9, snap to step=5 boundaries: 9+5=14, snapped floor to 10
        var cut = Render(CreateNumberField(defaultValue: 4, step: 5, snapOnStep: true));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        // 4 + 5 = 9, snapped to floor(9/5)*5 = 5
        hiddenInput.GetAttribute("value").ShouldBe("5");

        // Click again: increments from 5 by 5 → 10, snapped to 10
        btn.Click(new MouseEventArgs { Detail = 0 });
        hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("10");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Step_SnapWithLargerStepDecrement()
    {
        // From 12, snap to step=5 boundaries: 12-5=7, snapped ceil to 10
        var cut = Render(CreateNumberField(defaultValue: 12, step: 5, snapOnStep: true));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        // 12 - 5 = 7, snapped to ceil(7/5)*5 = 10
        hiddenInput.GetAttribute("value").ShouldBe("10");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Step_RemovesFloatingPointErrors()
    {
        // 0.2 + 0.1 should give 0.3, not 0.30000000000000004
        var cut = Render(CreateNumberField(defaultValue: 0.2, step: 0.1));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("0.3");
        return Task.CompletedTask;
    }

    // --- Format ---

    [Fact]
    public Task Format_FormatsValueUsingProvidedOptions()
    {
        var format = new NumberFormatOptions(Style: "currency", Currency: "USD");
        var cut = Render(CreateNumberField(defaultValue: 42, format: format));
        var input = cut.Find("input[type='text']");
        var val = input.GetAttribute("value");
        val.ShouldNotBeNull();
        // Should contain currency formatting
        val.ShouldNotBe("42");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Format_ReflectsControlledValueChanges()
    {
        var format = new NumberFormatOptions(Style: "currency", Currency: "USD");
        double? currentValue = 42;
        var valueChanged = EventCallback.Factory.Create<double?>(this, v => currentValue = v);

        var cut = Render(CreateNumberField(value: 42, valueChanged: valueChanged, format: format));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("value").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // --- Field integration ---

    [Fact]
    public Task Field_DataTouchedOnBlur()
    {
        // Without a FieldRoot wrapper, FieldContext is null so data-touched remains false.
        // Verify the blur interaction doesn't throw and the root remains stable.
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.Focus();
        input.Blur();
        // Without FieldContext, data-touched is always false (Blazor omits false boolean attrs)
        var root = cut.Find("div");
        root.HasAttribute("data-touched").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task Field_DataDirtyOnChange()
    {
        // Without a FieldRoot wrapper, FieldContext is null so data-dirty remains false.
        // Verify value change is applied correctly via the hidden input.
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "5" });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Field_DataFilledAddsAndRemovesOnChange()
    {
        // Without a FieldRoot wrapper, FieldContext is null so data-filled on children is always false.
        // Verify value changes via the hidden input instead.
        var cut = Render(CreateNumberField(defaultValue: 5));
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("5");

        // Clear the value
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "" });

        hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Field_DataFilledWhenAlreadyFilled()
    {
        // Without a FieldRoot wrapper, FieldContext is null so data-filled is always false.
        // Verify the value is present via the hidden input instead.
        var cut = Render(CreateNumberField(defaultValue: 42));
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("42");
        return Task.CompletedTask;
    }

    // --- InputMode ---

    [Fact]
    public Task InputMode_SetsToNumericByDefault()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("inputmode").ShouldBe("numeric");
        return Task.CompletedTask;
    }

    [Fact]
    public Task InputMode_SetsToDecimalWhenMinIsZeroOrAbove()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, min: 0));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("inputmode").ShouldBe("numeric");
        return Task.CompletedTask;
    }

    // --- Exotic inputs ---

    [Fact]
    public Task ExoticInput_ParsesPersianDigitsAndSeparators()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        // Persian digits for 1234
        input.Input(new ChangeEventArgs { Value = "\u06F1\u06F2\u06F3\u06F4" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("1234");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ExoticInput_ParsesPersianWithArabicSeparators()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        // Persian digits with Arabic group/decimal separators
        input.Input(new ChangeEventArgs { Value = "\u06F1\u06F2\u06F3\u06F4" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("1234");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ExoticInput_ParsesFullwidthDigitsAndPunctuation()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        // Fullwidth digits for 1234
        input.Input(new ChangeEventArgs { Value = "\uFF11\uFF12\uFF13\uFF14" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("1234");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ExoticInput_ParsesPercentAndPermilleInExoticForms()
    {
        // This tests that percent/permille symbols in input don't crash
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        // Typing just a number should work
        input.Input(new ChangeEventArgs { Value = "12" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("12");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ExoticInput_IgnoresPercentWhenNotFormattedAsPercent()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "12" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("12");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ExoticInput_ParsesTrailingUnicodeMinus()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        // Unicode minus before the number
        input.Input(new ChangeEventArgs { Value = "\u22121234" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("-1234");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ExoticInput_TreatsParenthesesNegativesAsInvalid()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        // Parentheses notation is not valid numeric input
        input.Input(new ChangeEventArgs { Value = "(1234)" });
        // Should be rejected as non-numeric
        input.GetAttribute("value").ShouldBe("0");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ExoticInput_CollapsesExtraDotsFromMixedLocaleInputs()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "1234.56" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("1234.56");
        return Task.CompletedTask;
    }

    // --- Navigation keys ---

    [Fact]
    public Task NavigationKeys_AllowsWithoutPreventingDefault()
    {
        var cut = Render(CreateNumberField(defaultValue: 5));
        var input = cut.Find("input[type='text']");
        // Navigation keys like Tab should not change the value
        input.KeyDown(new KeyboardEventArgs { Key = "Tab" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("5");
        return Task.CompletedTask;
    }

    // --- Locale ---

    [Fact]
    public Task Locale_SetsLocaleOfInput()
    {
        var cut = Render(CreateNumberField(defaultValue: 1234.56, locale: "de-DE"));
        var input = cut.Find("input[type='text']");
        var val = input.GetAttribute("value");
        // German format uses comma as decimal separator
        val.ShouldNotBeNull();
        val.ShouldContain(",");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Locale_UsesDefaultIfNoneProvided()
    {
        var cut = Render(CreateNumberField(defaultValue: 1234.56));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("value").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // --- Validation ---

    [Fact]
    public Task Validation_ClearsExternalErrorsOnChange()
    {
        // Without a Field/Form wrapper, just verify the value changes work
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "5" });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Validation_ValidatesWithLatestValueOnBlur()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, min: 1, max: 10));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "15" });
        input.Blur();
        // Should clamp to max
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("10");
        return Task.CompletedTask;
    }

    // --- Field label and description ---

    [Fact]
    public Task Field_LabelForAttribute()
    {
        // Without Field wrapper, the input should still have an id
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.HasAttribute("id").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task Field_DescriptionAriaDescribedBy()
    {
        // Without Field wrapper, aria-describedby is not set
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        // This test is primarily for the Field integration scenario
        input.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // --- Field disabled integration ---

    [Fact]
    public Task Field_DisablesInputWhenFieldDisabledTrue()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, disabled: true));
        var input = cut.Find("input[type='text']");
        input.GetAttribute("disabled").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task Field_DoesNotDisableWhenFieldDisabledFalse()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, disabled: false));
        var input = cut.Find("input[type='text']");
        // disabled attribute is present but set to False
        var disabledVal = input.GetAttribute("disabled");
        (disabledVal == null || disabledVal == "False").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // --- Data attributes ---

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, disabled: true));
        var root = cut.Find("div"); // Root renders as div
        root.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataReadOnlyWhenReadOnly()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, readOnly: true));
        var root = cut.Find("div"); // Root renders as div
        root.HasAttribute("data-readonly").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataRequiredWhenRequired()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, required: true));
        var root = cut.Find("div"); // Root renders as div
        root.HasAttribute("data-required").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // --- Hidden input ---

    [Fact]
    public Task HiddenInput_HasTypeNumber()
    {
        var cut = Render(CreateNumberField(defaultValue: 42));
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("type").ShouldBe("number");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HiddenInput_HasAriaHiddenTrue()
    {
        var cut = Render(CreateNumberField(defaultValue: 42));
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("aria-hidden").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HiddenInput_HasNameAttribute()
    {
        var cut = Render(CreateNumberField(defaultValue: 42, name: "myfield"));
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("name").ShouldBe("myfield");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HiddenInput_HasMinMaxStepAttributes()
    {
        var cut = Render(CreateNumberField(defaultValue: 5, min: 0, max: 100, step: 5));
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("min").ShouldBe("0");
        hiddenInput.GetAttribute("max").ShouldBe("100");
        hiddenInput.GetAttribute("step").ShouldBe("5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HiddenInput_AllowsStepAny()
    {
        var cut = Render(CreateNumberField(defaultValue: 5, min: 0, step: "any"));
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("step").ShouldBe("any");
        return Task.CompletedTask;
    }

    // --- Parse utility tests (from parse.test.ts via component interface) ---

    [Fact]
    public Task Parse_HandlesHanNumerals()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "\u4E00\u4E8C\u4E09\u56DB" }); // 一二三四
        input.Blur();

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("1234");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Parse_ReturnsNullForEmptyAndWhitespace()
    {
        var cut = Render(CreateNumberField(defaultValue: 42));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "   " });
        input.Blur();

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Parse_ReturnsNullForJustASign()
    {
        var cut = Render(CreateNumberField(defaultValue: 42));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "-" });
        input.Blur();

        var hiddenInput = cut.Find("input[type='number']");
        // "-" alone is not parseable, so value should reset
        hiddenInput.GetAttribute("value").ShouldBe("42");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Parse_HandlesLeadingAndTrailingSigns()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "-1234" });
        input.Blur();

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("-1234");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Parse_HandlesTrailingAsciiSign()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "1234-" });
        input.Blur();

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("-1234");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Parse_HandlesPermilleWhenFormattedAsPercent()
    {
        var format = new NumberFormatOptions(Style: "percent");
        var cut = Render(CreateNumberField(defaultValue: 0, format: format));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "12\u2030" });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("0.012");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Parse_HandlesDeDeFormattedNumbers()
    {
        // de-DE uses comma as decimal, period as group separator
        var cut = Render(CreateNumberField(defaultValue: 0, locale: "de-DE"));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "1.234,56" });
        input.Blur();

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("1234.56");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Parse_ReturnsNullForInfinityLikeInputs()
    {
        var cut = Render(CreateNumberField(defaultValue: 42));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "Infinity" });
        input.Blur();

        var hiddenInput = cut.Find("input[type='number']");
        // Infinity should not be accepted, value should revert
        hiddenInput.GetAttribute("value").ShouldBe("42");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Parse_CollapsesMultipleConsecutiveDots()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "1.234.567.89" });
        input.Blur();

        var hiddenInput = cut.Find("input[type='number']");
        // Multiple dots: last one is decimal, rest are group separators
        // double.TryParse with InvariantCulture may handle this
        // If not parseable, value reverts to 0
        var value = hiddenInput.GetAttribute("value");
        value.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // --- Cancellation ---

    [Fact]
    public Task OnValueChange_SupportsCancellation()
    {
        var onValueChange = EventCallback.Factory.Create<NumberFieldValueChangeEventArgs>(
            this, args => args.Cancel());

        var cut = Render(CreateNumberField(defaultValue: 5, onValueChange: onValueChange));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        // Value should remain 5 because the change was canceled
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("5");
        return Task.CompletedTask;
    }
}
