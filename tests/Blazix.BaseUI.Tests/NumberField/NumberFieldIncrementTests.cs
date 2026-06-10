namespace Blazix.BaseUI.Tests.NumberField;

public class NumberFieldIncrementTests : BunitContext, INumberFieldIncrementContract
{
    public NumberFieldIncrementTests()
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
        bool snapOnStep = false,
        bool incrementDisabled = false,
        Func<NumberFieldRootState, string?>? incrementClassValue = null,
        Func<NumberFieldRootState, string?>? incrementStyleValue = null,
        IReadOnlyDictionary<string, object>? incrementAdditionalAttributes = null,
        RenderFragment? incrementChildContent = null)
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
            if (snapOnStep)
                builder.AddAttribute(attrIndex++, "SnapOnStep", true);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<NumberFieldGroup>(0);
                inner.AddAttribute(1, "ChildContent", (RenderFragment)(groupInner =>
                {
                    groupInner.OpenComponent<NumberFieldInput>(0);
                    groupInner.CloseComponent();

                    groupInner.OpenComponent<NumberFieldIncrement>(1);
                    var incAttr = 2;
                    if (incrementDisabled)
                        groupInner.AddAttribute(incAttr++, "Disabled", true);
                    if (incrementClassValue is not null)
                        groupInner.AddAttribute(incAttr++, "ClassValue", incrementClassValue);
                    if (incrementStyleValue is not null)
                        groupInner.AddAttribute(incAttr++, "StyleValue", incrementStyleValue);
                    if (incrementAdditionalAttributes is not null)
                        groupInner.AddMultipleAttributes(incAttr++, incrementAdditionalAttributes);
                    if (incrementChildContent is not null)
                        groupInner.AddAttribute(incAttr++, "ChildContent", incrementChildContent);
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
    public Task RendersAsButtonByDefault()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var btn = cut.Find("[aria-label='Increase']");
        btn.TagName.ShouldBe("BUTTON");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasIncreaseLabel()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var btn = cut.Find("[aria-label='Increase']");
        btn.GetAttribute("aria-label").ShouldBe("Increase");
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
                    groupInner.CloseComponent();

                    groupInner.OpenComponent<NumberFieldIncrement>(1);
                    groupInner.AddAttribute(2, "Render", (RenderFragment<RenderProps<NumberFieldRootState>>)(props => b =>
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
        var btn = cut.Find("[aria-label='Increase']");
        btn.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            incrementChildContent: b => b.AddContent(0, "+")));
        var btn = cut.Find("[aria-label='Increase']");
        btn.TextContent.ShouldContain("+");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var attrs = new Dictionary<string, object> { ["data-custom"] = "inc-val" };
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            incrementAdditionalAttributes: attrs));
        var btn = cut.Find("[aria-label='Increase']");
        btn.GetAttribute("data-custom").ShouldBe("inc-val");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            incrementClassValue: _ => "inc-class"));
        var btn = cut.Find("[aria-label='Increase']");
        btn.ClassList.ShouldContain("inc-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            incrementStyleValue: _ => "color:green"));
        var btn = cut.Find("[aria-label='Increase']");
        btn.GetAttribute("style").ShouldContain("color:green");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var attrs = new Dictionary<string, object> { ["class"] = "attr-class" };
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            incrementClassValue: _ => "func-class",
            incrementAdditionalAttributes: attrs));
        var btn = cut.Find("[aria-label='Increase']");
        btn.ClassList.ShouldContain("func-class");
        btn.ClassList.ShouldContain("attr-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var inc = cut.FindComponent<NumberFieldIncrement>();
        inc.Instance.Element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // --- Basic increment ---

    [Fact]
    public Task IncrementsStartingFromZeroOnClick()
    {
        var cut = Render(CreateNumberField());
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task IncrementsToOneFromDefaultValueZero()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task FirstIncrementAfterExternalControlledUpdate()
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
    public Task OnlyCallsOnValueChangeOncePerIncrement()
    {
        var callCount = 0;
        var onValueChange = EventCallback.Factory.Create<NumberFieldValueChangeEventArgs>(
            this, _ => callCount++);

        var cut = Render(CreateNumberField(defaultValue: 0, onValueChange: onValueChange));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        callCount.ShouldBe(1);
        return Task.CompletedTask;
    }

    // --- Press and hold ---

    [Fact]
    public async Task IncrementsContinuouslyWhenHoldingPointerDown()
    {
        // Press-and-hold is handled by JS calling OnAutoChangeTick via JSInvokable.
        // We simulate by invoking the JSInvokable method directly on the dispatcher.
        var cut = Render(CreateNumberField(defaultValue: 0));
        var root = cut.FindComponent<NumberFieldRoot>();

        // Simulate the JS calling OnAutoChangeTick for increment
        await cut.InvokeAsync(() => root.Instance.OnAutoChangeTick(true));
        await cut.InvokeAsync(() => root.Instance.OnAutoChangeTick(true));
        await cut.InvokeAsync(() => root.Instance.OnAutoChangeTick(true));
        await cut.InvokeAsync(() => root.Instance.OnAutoChangeEnd(true));

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("3");
    }

    [Fact]
    public async Task DoesNotIncrementTwiceWithPointerDownAndClick()
    {
        // When pointerdown fires, e.Detail is set to non-zero for click.
        // The click handler checks e.Detail != 0 and returns early.
        var cut = Render(CreateNumberField(defaultValue: 0));
        var btn = cut.Find("[aria-label='Increase']");

        // First: pointerdown triggers auto-change which calls IncrementValue once
        btn.PointerDown(new PointerEventArgs { Button = 0, PointerType = "mouse" });

        // Simulate one auto-change tick from JS
        var root = cut.FindComponent<NumberFieldRoot>();
        await cut.InvokeAsync(() => root.Instance.OnAutoChangeTick(true));
        await cut.InvokeAsync(() => root.Instance.OnAutoChangeEnd(true));

        // Then click with Detail=1 (normal mouse click) should be ignored
        btn.Click(new MouseEventArgs { Detail = 1 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("1");
    }

    // --- State ---

    [Fact]
    public Task DoesNotIncrementWhenReadOnly()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, readOnly: true));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("0");
        return Task.CompletedTask;
    }

    [Fact]
    public Task IncrementsWhenInputIsDirtyNotBlurred_Click()
    {
        var cut = Render(CreateNumberField(defaultValue: 5));
        var input = cut.Find("input[type='text']");

        // Type a value without blurring
        input.Input(new ChangeEventArgs { Value = "7" });

        // Click increment - should increment from the parsed input value (7 -> 8)
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("8");
        return Task.CompletedTask;
    }

    [Fact]
    public async Task IncrementsWhenInputIsDirtyNotBlurred_PointerDown()
    {
        var cut = Render(CreateNumberField(defaultValue: 5));
        var input = cut.Find("input[type='text']");

        // Type a value without blurring
        input.Input(new ChangeEventArgs { Value = "7" });

        // PointerDown triggers auto-change
        var btn = cut.Find("[aria-label='Increase']");
        btn.PointerDown(new PointerEventArgs { Button = 0, PointerType = "mouse" });

        var root = cut.FindComponent<NumberFieldRoot>();
        await cut.InvokeAsync(() => root.Instance.OnAutoChangeTick(true));
        await cut.InvokeAsync(() => root.Instance.OnAutoChangeEnd(true));

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("8");
    }

    // --- SnapOnStep ---

    [Fact]
    public Task SnapOnStep_IncrementsWithoutRoundingWhenFalse()
    {
        var cut = Render(CreateNumberField(defaultValue: 1.5, step: 3, snapOnStep: false));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("4.5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SnapOnStep_SnapsOnIncrementWhenTrue()
    {
        var cut = Render(CreateNumberField(defaultValue: 1.5, step: 3, snapOnStep: true));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        // With snapOnStep, 1.5 + 3 = 4.5, snapped to nearest multiple of 3 from 0 = 3
        hiddenInput.GetAttribute("value").ShouldBe("3");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SnapOnStep_IncrementsWithRespectToMinValue()
    {
        var cut = Render(CreateNumberField(defaultValue: 2, step: 3, min: 2, snapOnStep: true));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        // With min=2 and step=3, snap base is 2. 2 + 3 = 5, snapped to 2 + floor((5-2)/3)*3 = 2 + 3 = 5
        hiddenInput.GetAttribute("value").ShouldBe("5");
        return Task.CompletedTask;
    }

    // --- Disabled ---

    [Fact]
    public Task DoesNotIncrementWhenRootDisabled()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, disabled: true));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("0");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotIncrementWhenButtonDisabled()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, incrementDisabled: true));
        var btn = cut.Find("[aria-label='Increase']");
        btn.Click(new MouseEventArgs { Detail = 0 });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("0");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenRootDisabled()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, disabled: true));
        var btn = cut.Find("[aria-label='Increase']");
        btn.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenButtonDisabled()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, incrementDisabled: true));
        var btn = cut.Find("[aria-label='Increase']");
        btn.GetAttribute("disabled").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // --- Max boundary ---

    [Fact]
    public Task DisabledWhenAtMax()
    {
        var cut = Render(CreateNumberField(defaultValue: 10, max: 10));
        var btn = cut.Find("[aria-label='Increase']");
        btn.GetAttribute("disabled").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // --- Data attributes ---

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, disabled: true));
        var btn = cut.Find("[aria-label='Increase']");
        btn.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataReadOnlyWhenReadOnly()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, readOnly: true));
        var btn = cut.Find("[aria-label='Increase']");
        btn.HasAttribute("data-readonly").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataRequiredWhenRequired()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, required: true));
        var btn = cut.Find("[aria-label='Increase']");
        btn.HasAttribute("data-required").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // --- Native button attributes ---

    [Fact]
    public Task NativeButton_HasTypeButton()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var btn = cut.Find("[aria-label='Increase']");
        btn.GetAttribute("type").ShouldBe("button");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_HasTabIndexMinusOne()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var btn = cut.Find("[aria-label='Increase']");
        btn.GetAttribute("tabindex").ShouldBe("-1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_HasAriaLabelIncrease()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var btn = cut.Find("[aria-label='Increase']");
        btn.GetAttribute("aria-label").ShouldBe("Increase");
        return Task.CompletedTask;
    }
}
