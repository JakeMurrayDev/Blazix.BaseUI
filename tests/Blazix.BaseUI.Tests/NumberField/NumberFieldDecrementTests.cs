namespace Blazix.BaseUI.Tests.NumberField;

public class NumberFieldDecrementTests : BunitContext, INumberFieldDecrementContract
{
    public NumberFieldDecrementTests()
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
        bool decrementDisabled = false,
        Func<NumberFieldRootState, string?>? decrementClassValue = null,
        Func<NumberFieldRootState, string?>? decrementStyleValue = null,
        IReadOnlyDictionary<string, object>? decrementAdditionalAttributes = null,
        RenderFragment? decrementChildContent = null)
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
                    groupInner.CloseComponent();

                    groupInner.OpenComponent<NumberFieldDecrement>(2);
                    var decAttr = 3;
                    if (decrementDisabled)
                        groupInner.AddAttribute(decAttr++, "Disabled", true);
                    if (decrementClassValue is not null)
                        groupInner.AddAttribute(decAttr++, "ClassValue", decrementClassValue);
                    if (decrementStyleValue is not null)
                        groupInner.AddAttribute(decAttr++, "StyleValue", decrementStyleValue);
                    if (decrementAdditionalAttributes is not null)
                        groupInner.AddMultipleAttributes(decAttr++, decrementAdditionalAttributes);
                    if (decrementChildContent is not null)
                        groupInner.AddAttribute(decAttr++, "ChildContent", decrementChildContent);
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
        var btn = cut.Find("[aria-label='Decrease']");
        btn.TagName.ShouldBe("BUTTON");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDecreaseLabel()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.GetAttribute("aria-label").ShouldBe("Decrease");
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

                    groupInner.OpenComponent<NumberFieldDecrement>(1);
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
        var btn = cut.Find("[aria-label='Decrease']");
        btn.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            decrementChildContent: b => b.AddContent(0, "-")));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.TextContent.ShouldContain("-");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var attrs = new Dictionary<string, object> { ["data-custom"] = "dec-val" };
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            decrementAdditionalAttributes: attrs));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.GetAttribute("data-custom").ShouldBe("dec-val");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            decrementClassValue: _ => "dec-class"));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.ClassList.ShouldContain("dec-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            decrementStyleValue: _ => "color:red"));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.GetAttribute("style").ShouldContain("color:red");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var attrs = new Dictionary<string, object> { ["class"] = "attr-class" };
        var cut = Render(CreateNumberField(
            defaultValue: 0,
            decrementClassValue: _ => "func-class",
            decrementAdditionalAttributes: attrs));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.ClassList.ShouldContain("func-class");
        btn.ClassList.ShouldContain("attr-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var dec = cut.FindComponent<NumberFieldDecrement>();
        dec.Instance.Element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // --- Basic decrement ---

    [Fact]
    public Task DecrementsStartingFromZeroOnClick()
    {
        var cut = Render(CreateNumberField());
        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("-1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DecrementsToMinusOneFromDefaultValueZero()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("-1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task FirstDecrementAfterExternalControlledUpdate()
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
    public Task OnlyCallsOnValueChangeOncePerDecrement()
    {
        var callCount = 0;
        var onValueChange = EventCallback.Factory.Create<NumberFieldValueChangeEventArgs>(
            this, _ => callCount++);

        var cut = Render(CreateNumberField(defaultValue: 0, onValueChange: onValueChange));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        callCount.ShouldBe(1);
        return Task.CompletedTask;
    }

    // --- Press and hold ---

    [Fact]
    public async Task DecrementsContinuouslyWhenHoldingPointerDown()
    {
        var cut = Render(CreateNumberField(defaultValue: 10));
        var root = cut.FindComponent<NumberFieldRoot>();

        await cut.InvokeAsync(() => root.Instance.OnAutoChangeTick(false));
        await cut.InvokeAsync(() => root.Instance.OnAutoChangeTick(false));
        await cut.InvokeAsync(() => root.Instance.OnAutoChangeTick(false));
        await cut.InvokeAsync(() => root.Instance.OnAutoChangeEnd(false));

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("7");
    }

    [Fact]
    public async Task DoesNotDecrementTwiceWithPointerDownAndClick()
    {
        var cut = Render(CreateNumberField(defaultValue: 10));
        var btn = cut.Find("[aria-label='Decrease']");

        btn.PointerDown(new PointerEventArgs { Button = 0, PointerType = "mouse" });

        var root = cut.FindComponent<NumberFieldRoot>();
        await cut.InvokeAsync(() => root.Instance.OnAutoChangeTick(false));
        await cut.InvokeAsync(() => root.Instance.OnAutoChangeEnd(false));

        btn.Click(new MouseEventArgs { Detail = 1 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("9");
    }

    // --- State ---

    [Fact]
    public Task DoesNotDecrementWhenReadOnly()
    {
        var cut = Render(CreateNumberField(defaultValue: 5, readOnly: true));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DecrementsWhenInputIsDirtyNotBlurred_Click()
    {
        var cut = Render(CreateNumberField(defaultValue: 10));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "7" });

        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("6");
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DecrementsWhenInputIsDirtyNotBlurred_PointerDown()
    {
        var cut = Render(CreateNumberField(defaultValue: 10));
        var input = cut.Find("input[type='text']");
        input.Input(new ChangeEventArgs { Value = "7" });

        var btn = cut.Find("[aria-label='Decrease']");
        btn.PointerDown(new PointerEventArgs { Button = 0, PointerType = "mouse" });

        var root = cut.FindComponent<NumberFieldRoot>();
        await cut.InvokeAsync(() => root.Instance.OnAutoChangeTick(false));
        await cut.InvokeAsync(() => root.Instance.OnAutoChangeEnd(false));

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("6");
    }

    // --- SnapOnStep ---

    [Fact]
    public Task SnapOnStep_DecrementsWithoutRoundingWhenFalse()
    {
        var cut = Render(CreateNumberField(defaultValue: 4.5, step: 3, snapOnStep: false));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("1.5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SnapOnStep_SnapsOnDecrementWhenTrue()
    {
        var cut = Render(CreateNumberField(defaultValue: 4.5, step: 3, snapOnStep: true));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        // With snapOnStep, 4.5 - 3 = 1.5, snapped to nearest multiple of 3 from 0 = ceil(1.5/3)*3 = 3
        hiddenInput.GetAttribute("value").ShouldBe("3");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SnapOnStep_DecrementsWithRespectToMinValue()
    {
        var cut = Render(CreateNumberField(defaultValue: 8, step: 3, min: 2, snapOnStep: true));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });

        var hiddenInput = cut.Find("input[type='number']");
        // With min=2 and step=3, snap base is 2. 8 - 3 = 5, snapped to 2 + ceil((5-2)/3)*3 = 2 + 3 = 5
        hiddenInput.GetAttribute("value").ShouldBe("5");
        return Task.CompletedTask;
    }

    // --- Disabled ---

    [Fact]
    public Task DoesNotDecrementWhenRootDisabled()
    {
        var cut = Render(CreateNumberField(defaultValue: 5, disabled: true));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotDecrementWhenButtonDisabled()
    {
        var cut = Render(CreateNumberField(defaultValue: 5, decrementDisabled: true));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.Click(new MouseEventArgs { Detail = 0 });
        var hiddenInput = cut.Find("input[type='number']");
        hiddenInput.GetAttribute("value").ShouldBe("5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenRootDisabled()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, disabled: true));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenButtonDisabled()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, decrementDisabled: true));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.GetAttribute("disabled").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // --- Min boundary ---

    [Fact]
    public Task DisabledWhenAtMin()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, min: 0));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.GetAttribute("disabled").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // --- Data attributes ---

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, disabled: true));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataReadOnlyWhenReadOnly()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, readOnly: true));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.HasAttribute("data-readonly").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataRequiredWhenRequired()
    {
        var cut = Render(CreateNumberField(defaultValue: 0, required: true));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.HasAttribute("data-required").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // --- Native button attributes ---

    [Fact]
    public Task NativeButton_HasTypeButton()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.GetAttribute("type").ShouldBe("button");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_HasTabIndexMinusOne()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.GetAttribute("tabindex").ShouldBe("-1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_HasAriaLabelDecrease()
    {
        var cut = Render(CreateNumberField(defaultValue: 0));
        var btn = cut.Find("[aria-label='Decrease']");
        btn.GetAttribute("aria-label").ShouldBe("Decrease");
        return Task.CompletedTask;
    }
}
