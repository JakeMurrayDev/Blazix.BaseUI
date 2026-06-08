using Blazix.BaseUI.RadioGroup;

namespace Blazix.BaseUI.Tests.RadioGroup;

public class RadioGroupTests : BunitContext, IRadioGroupContract
{
    public RadioGroupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupRadioModule(JSInterop);
    }

    private RenderFragment CreateRadioGroup(
        string? value = null,
        string? defaultValue = null,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        string? name = null,
        string? form = null,
        Action<RadioGroupValueChangeEventArgs<string>>? onValueChange = null,
        Func<RadioGroupState, string>? classValue = null,
        Func<RadioGroupState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<RadioGroupState>>? render = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.RadioGroup.RadioGroup<string>>(0);
            var attrIndex = 1;

            if (value is not null)
                builder.AddAttribute(attrIndex++, "Value", value);
            if (defaultValue is not null)
                builder.AddAttribute(attrIndex++, "DefaultValue", defaultValue);
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
            if (onValueChange is not null)
                builder.AddAttribute(attrIndex++, "OnValueChange", EventCallback.Factory.Create(this, onValueChange));
            if (classValue is not null)
                builder.AddAttribute(attrIndex++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(attrIndex++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
            if (render is not null)
                builder.AddAttribute(attrIndex++, "Render", render);
            if (childContent is not null)
                builder.AddAttribute(attrIndex++, "ChildContent", childContent);

            builder.CloseComponent();
        };
    }

    private RenderFragment CreateRadioGroupWithRadios(
        string? value = null,
        string? defaultValue = null,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        string? name = null,
        string? form = null,
        Action<RadioGroupValueChangeEventArgs<string>>? onValueChange = null,
        EventCallback<string?>? valueChanged = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        bool includeIndicators = false)
    {
        return builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.RadioGroup.RadioGroup<string>>(0);
            var attrIndex = 1;

            if (value is not null)
                builder.AddAttribute(attrIndex++, "Value", value);
            if (defaultValue is not null)
                builder.AddAttribute(attrIndex++, "DefaultValue", defaultValue);
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
            if (onValueChange is not null)
                builder.AddAttribute(attrIndex++, "OnValueChange", EventCallback.Factory.Create(this, onValueChange));
            if (valueChanged.HasValue)
                builder.AddAttribute(attrIndex++, "ValueChanged", valueChanged.Value);
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                // Radio A
                innerBuilder.OpenComponent<RadioRoot<string>>(0);
                innerBuilder.AddAttribute(1, "Value", "a");
                innerBuilder.AddAttribute(2, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "radio-a" } });
                if (includeIndicators)
                {
                    innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(indBuilder =>
                    {
                        indBuilder.OpenComponent<RadioIndicator>(0);
                        indBuilder.AddAttribute(1, "AdditionalAttributes",
                            (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "indicator-a" } });
                        indBuilder.CloseComponent();
                    }));
                }
                innerBuilder.CloseComponent();

                // Radio B
                innerBuilder.OpenComponent<RadioRoot<string>>(10);
                innerBuilder.AddAttribute(11, "Value", "b");
                innerBuilder.AddAttribute(12, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "radio-b" } });
                if (includeIndicators)
                {
                    innerBuilder.AddAttribute(13, "ChildContent", (RenderFragment)(indBuilder =>
                    {
                        indBuilder.OpenComponent<RadioIndicator>(0);
                        indBuilder.AddAttribute(1, "AdditionalAttributes",
                            (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "indicator-b" } });
                        indBuilder.CloseComponent();
                    }));
                }
                innerBuilder.CloseComponent();

                // Radio C
                innerBuilder.OpenComponent<RadioRoot<string>>(20);
                innerBuilder.AddAttribute(21, "Value", "c");
                innerBuilder.AddAttribute(22, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "radio-c" } });
                if (includeIndicators)
                {
                    innerBuilder.AddAttribute(23, "ChildContent", (RenderFragment)(indBuilder =>
                    {
                        indBuilder.OpenComponent<RadioIndicator>(0);
                        indBuilder.AddAttribute(1, "AdditionalAttributes",
                            (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "indicator-c" } });
                        indBuilder.CloseComponent();
                    }));
                }
                innerBuilder.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    // Rendering tests
    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateRadioGroup(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<RadioGroupState>> renderAsFieldset = props => builder =>
        {
            builder.OpenElement(0, "fieldset");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateRadioGroup(
            render: renderAsFieldset,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.TagName.ShouldBe("FIELDSET");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateRadioGroup(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "group" },
                { "aria-label", "Radio selection" }
            }
        ));

        var group = cut.Find("[data-testid='group']");
        group.GetAttribute("aria-label").ShouldBe("Radio selection");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AdditionalAttributesCanOverrideBuiltInRole()
    {
        var cut = Render(CreateRadioGroup(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "group" },
                { "role", "switch" }
            }
        ));

        var group = cut.Find("[data-testid='group']");
        group.GetAttribute("role").ShouldBe("switch");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateRadioGroup(
            classValue: _ => "group-class",
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.GetAttribute("class").ShouldContain("group-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateRadioGroup(
            styleValue: _ => "background: red",
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.GetAttribute("style").ShouldContain("background: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateRadioGroup(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "group" },
                { "class", "static-class" }
            }
        ));

        var group = cut.Find("[data-testid='group']");
        var classAttr = group.GetAttribute("class");
        classAttr.ShouldContain("static-class");
        classAttr.ShouldContain("dynamic-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRoleRadiogroup()
    {
        var cut = Render(CreateRadioGroup(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.GetAttribute("role").ShouldBe("radiogroup");

        return Task.CompletedTask;
    }

    // Value control tests
    [Fact]
    public Task ControlledValue_SetsCheckedRadio()
    {
        var cut = Render(CreateRadioGroupWithRadios(
            value: "b",
            valueChanged: EventCallback.Factory.Create<string?>(this, _ => { })
        ));

        var radioA = cut.Find("[data-testid='radio-a']");
        var radioB = cut.Find("[data-testid='radio-b']");
        var radioC = cut.Find("[data-testid='radio-c']");

        radioA.GetAttribute("aria-checked").ShouldBe("false");
        radioB.GetAttribute("aria-checked").ShouldBe("true");
        radioC.GetAttribute("aria-checked").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ValueParameterControlsSelectionWithoutValueChanged()
    {
        var cut = Render(CreateRadioGroupWithRadios(value: "b"));

        var radioA = cut.Find("[data-testid='radio-a']");
        var radioB = cut.Find("[data-testid='radio-b']");
        var radioC = cut.Find("[data-testid='radio-c']");

        radioA.GetAttribute("aria-checked").ShouldBe("false");
        radioB.GetAttribute("aria-checked").ShouldBe("true");
        radioC.GetAttribute("aria-checked").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledValue_UsesDefaultValue()
    {
        var cut = Render(CreateRadioGroupWithRadios(defaultValue: "a"));

        var radioA = cut.Find("[data-testid='radio-a']");
        var radioB = cut.Find("[data-testid='radio-b']");

        radioA.GetAttribute("aria-checked").ShouldBe("true");
        radioB.GetAttribute("aria-checked").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledValue_UpdatesOnRadioClick()
    {
        var cut = Render(CreateRadioGroupWithRadios());

        var radioA = cut.Find("[data-testid='radio-a']");
        radioA.GetAttribute("aria-checked").ShouldBe("false");

        // Simulate clicking by triggering input change
        var inputs = cut.FindAll("input[type='radio']");
        inputs[0].Change("a");

        radioA = cut.Find("[data-testid='radio-a']");
        radioA.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    // OnValueChange tests
    [Fact]
    public Task OnValueChange_CalledWhenRadioClicked()
    {
        var callCount = 0;
        var cut = Render(CreateRadioGroupWithRadios(
            onValueChange: _ => callCount++
        ));

        var inputs = cut.FindAll("input[type='radio']");
        inputs[0].Change("a");

        callCount.ShouldBe(1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChange_ReceivesSelectedValue()
    {
        string? receivedValue = null;
        var cut = Render(CreateRadioGroupWithRadios(
            onValueChange: args => receivedValue = args.Value
        ));

        var inputs = cut.FindAll("input[type='radio']");
        inputs[1].Change("b");

        receivedValue.ShouldBe("b");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChange_CanBeCanceled()
    {
        var cut = Render(CreateRadioGroupWithRadios(
            onValueChange: args => args.Cancel()
        ));

        var inputs = cut.FindAll("input[type='radio']");
        inputs[0].Change("a");

        var radioA = cut.Find("[data-testid='radio-a']");
        radioA.GetAttribute("aria-checked").ShouldBe("false");

        return Task.CompletedTask;
    }

    // Disabled tests
    [Fact]
    public Task HasAriaDisabledWhenDisabled()
    {
        var cut = Render(CreateRadioGroup(
            disabled: true,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.GetAttribute("aria-disabled").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveAriaDisabledByDefault()
    {
        var cut = Render(CreateRadioGroup(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.HasAttribute("aria-disabled").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Disabled_PropagesToAllRadios()
    {
        var cut = Render(CreateRadioGroupWithRadios(disabled: true));

        var radioA = cut.Find("[data-testid='radio-a']");
        var radioB = cut.Find("[data-testid='radio-b']");
        var radioC = cut.Find("[data-testid='radio-c']");

        radioA.HasAttribute("data-disabled").ShouldBeTrue();
        radioB.HasAttribute("data-disabled").ShouldBeTrue();
        radioC.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Disabled_PreventsValueChange()
    {
        var callCount = 0;
        var cut = Render(CreateRadioGroupWithRadios(
            disabled: true,
            onValueChange: _ => callCount++
        ));

        var inputs = cut.FindAll("input[type='radio']");
        inputs[0].Change("a");

        callCount.ShouldBe(0);

        return Task.CompletedTask;
    }

    // ReadOnly tests
    [Fact]
    public Task HasAriaReadonlyWhenReadOnly()
    {
        var cut = Render(CreateRadioGroup(
            readOnly: true,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.GetAttribute("aria-readonly").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveAriaReadonlyByDefault()
    {
        var cut = Render(CreateRadioGroup(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.HasAttribute("aria-readonly").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ReadOnly_PreventsValueChange()
    {
        var callCount = 0;
        var cut = Render(CreateRadioGroupWithRadios(
            readOnly: true,
            onValueChange: _ => callCount++
        ));

        var inputs = cut.FindAll("input[type='radio']");
        inputs[0].Change("a");

        callCount.ShouldBe(0);

        return Task.CompletedTask;
    }

    // Data attribute tests
    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateRadioGroup(
            disabled: true,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataReadonlyWhenReadOnly()
    {
        var cut = Render(CreateRadioGroup(
            readOnly: true,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.HasAttribute("data-readonly").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataRequiredWhenRequired()
    {
        var cut = Render(CreateRadioGroup(
            required: true,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.HasAttribute("data-required").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataValidWhenValid()
    {
        var cut = Render(CreateRadioGroup(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        // Without Field context, Valid is null, so neither data-valid nor data-invalid
        group.HasAttribute("data-valid").ShouldBeFalse();
        group.HasAttribute("data-invalid").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataInvalidWhenInvalid()
    {
        var cut = Render(CreateRadioGroup(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.HasAttribute("data-invalid").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataTouchedWhenTouched()
    {
        var cut = Render(CreateRadioGroup(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.HasAttribute("data-touched").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDirtyWhenDirty()
    {
        var cut = Render(CreateRadioGroup(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.HasAttribute("data-dirty").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataFilledWhenFilled()
    {
        var cut = Render(CreateRadioGroup(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.HasAttribute("data-filled").ShouldBeFalse();

        return Task.CompletedTask;
    }

    // Style hooks on children
    [Fact]
    public Task PlacesStyleHooksOnGroupRadioAndIndicator()
    {
        var cut = Render(CreateRadioGroupWithRadios(
            defaultValue: "a",
            disabled: true,
            required: true,
            includeIndicators: true,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        var radioA = cut.Find("[data-testid='radio-a']");
        var indicatorA = cut.Find("[data-testid='indicator-a']");

        // Group should have data attributes
        group.HasAttribute("data-disabled").ShouldBeTrue();
        group.HasAttribute("data-required").ShouldBeTrue();

        // Radio should have data attributes
        radioA.HasAttribute("data-checked").ShouldBeTrue();
        radioA.HasAttribute("data-disabled").ShouldBeTrue();
        radioA.HasAttribute("data-required").ShouldBeTrue();

        // Indicator should have data attributes
        indicatorA.HasAttribute("data-checked").ShouldBeTrue();
        indicatorA.HasAttribute("data-disabled").ShouldBeTrue();
        indicatorA.HasAttribute("data-required").ShouldBeTrue();

        return Task.CompletedTask;
    }

    // Value prop tests
    [Fact]
    public Task DoesNotForwardValuePropToElement()
    {
        var cut = Render(CreateRadioGroup(
            defaultValue: "a",
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.HasAttribute("value").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsTabindexZeroOnSelectedRadioOnly()
    {
        var cut = Render(CreateRadioGroupWithRadios(defaultValue: "b"));

        var radioA = cut.Find("[data-testid='radio-a']");
        var radioB = cut.Find("[data-testid='radio-b']");
        var radioC = cut.Find("[data-testid='radio-c']");

        radioA.GetAttribute("tabindex").ShouldBe("-1");
        radioB.GetAttribute("tabindex").ShouldBe("0");
        radioC.GetAttribute("tabindex").ShouldBe("-1");

        return Task.CompletedTask;
    }

    // Hidden input tests
    [Fact]
    public Task DoesNotRenderGroupLevelHiddenRadioInput()
    {
        var cut = Render(CreateRadioGroup(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        cut.FindAll("input[type='radio']").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task FormPropPassesToEachRadioInput()
    {
        var cut = Render(CreateRadioGroupWithRadios(
            defaultValue: "a",
            name: "my-radio",
            form: "external-form"
        ));

        var inputs = cut.FindAll("input[type='radio']");
        inputs.Count.ShouldBe(3);
        inputs.ShouldAllBe(input => input.GetAttribute("form") == "external-form");

        return Task.CompletedTask;
    }

    // Context tests
    [Fact]
    public Task CascadesContextToChildren()
    {
        IRadioGroupContext<string>? capturedContext = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.RadioGroup.RadioGroup<string>>(0);
            builder.AddAttribute(1, "DefaultValue", "test");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ContextCapture<IRadioGroupContext<string>>>(0);
                innerBuilder.AddAttribute(1, "OnContextCaptured", (Action<IRadioGroupContext<string>>)(ctx => capturedContext = ctx));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        capturedContext.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ContextContainsCorrectValue()
    {
        IRadioGroupContext<string>? capturedContext = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.RadioGroup.RadioGroup<string>>(0);
            builder.AddAttribute(1, "DefaultValue", "b");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ContextCapture<IRadioGroupContext<string>>>(0);
                innerBuilder.AddAttribute(1, "OnContextCaptured", (Action<IRadioGroupContext<string>>)(ctx => capturedContext = ctx));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        capturedContext.ShouldNotBeNull();
        capturedContext!.CheckedValue.ShouldBe("b");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ContextContainsDisabledState()
    {
        IRadioGroupContext<string>? capturedContext = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.RadioGroup.RadioGroup<string>>(0);
            builder.AddAttribute(1, "Disabled", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ContextCapture<IRadioGroupContext<string>>>(0);
                innerBuilder.AddAttribute(1, "OnContextCaptured", (Action<IRadioGroupContext<string>>)(ctx => capturedContext = ctx));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        capturedContext.ShouldNotBeNull();
        capturedContext!.Disabled.ShouldBeTrue();

        return Task.CompletedTask;
    }

    // State tests
    [Fact]
    public Task ClassValueReceivesCorrectState()
    {
        RadioGroupState? capturedState = null;

        var cut = Render(CreateRadioGroup(
            disabled: true,
            classValue: state =>
            {
                capturedState = state;
                return "group-class";
            },
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Disabled.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task StyleValueReceivesCorrectState()
    {
        RadioGroupState? capturedState = null;

        var cut = Render(CreateRadioGroup(
            disabled: true,
            styleValue: state =>
            {
                capturedState = state;
                return "opacity: 0.5";
            },
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Disabled.ShouldBeTrue();

        return Task.CompletedTask;
    }

    // Helper components
    private sealed class ContextCapture<TContext> : ComponentBase
    {
        [CascadingParameter]
        private TContext? Context { get; set; }

        [Parameter]
        public Action<TContext>? OnContextCaptured { get; set; }

        protected override void OnParametersSet()
        {
            if (Context is not null)
            {
                OnContextCaptured?.Invoke(Context);
            }
        }
    }
}
