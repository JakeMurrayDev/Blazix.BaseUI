using Blazix.BaseUI.Field;

namespace Blazix.BaseUI.Tests.Checkbox;

public class CheckboxRootTests : BunitContext, ICheckboxRootContract
{
    public CheckboxRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupCheckboxModule(JSInterop);
    }

    private RenderFragment CreateCheckboxRoot(
        bool? isChecked = null,
        bool defaultChecked = false,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        bool indeterminate = false,
        bool nativeButton = false,
        string? name = null,
        string? form = null,
        string? value = null,
        string? uncheckedValue = null,
        Func<CheckboxRootState, string>? classValue = null,
        Func<CheckboxRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<CheckboxRootState>>? render = null,
        EventCallback<CheckboxCheckedChangeEventArgs>? onCheckedChange = null,
        EventCallback<bool>? checkedChanged = null,
        RenderFragment? childContent = null)
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
            if (nativeButton)
                builder.AddAttribute(attrIndex++, "NativeButton", true);
            if (name is not null)
                builder.AddAttribute(attrIndex++, "Name", name);
            if (form is not null)
                builder.AddAttribute(attrIndex++, "Form", form);
            if (value is not null)
                builder.AddAttribute(attrIndex++, "Value", value);
            if (uncheckedValue is not null)
                builder.AddAttribute(attrIndex++, "UncheckedValue", uncheckedValue);
            if (classValue is not null)
                builder.AddAttribute(attrIndex++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(attrIndex++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
            if (render is not null)
                builder.AddAttribute(attrIndex++, "Render", render);
            if (onCheckedChange.HasValue)
                builder.AddAttribute(attrIndex++, "OnCheckedChange", onCheckedChange.Value);
            if (checkedChanged.HasValue)
                builder.AddAttribute(attrIndex++, "CheckedChanged", checkedChanged.Value);
            if (childContent is not null)
                builder.AddAttribute(attrIndex++, "ChildContent", childContent);

            builder.CloseComponent();
        };
    }

    private RenderFragment CreateCheckboxWithIndicator(
        bool? isChecked = null,
        bool defaultChecked = false,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        bool indeterminate = false,
        IReadOnlyDictionary<string, object>? indicatorAttributes = null)
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
                if (indicatorAttributes is not null)
                    innerBuilder.AddAttribute(1, "AdditionalAttributes", indicatorAttributes);
                innerBuilder.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    // Rendering tests
    [Fact]
    public Task RendersAsSpanByDefault()
    {
        var cut = Render(CreateCheckboxRoot());

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<CheckboxRootState>> renderAsDiv = props => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateCheckboxRoot(
            render: renderAsDiv,
            childContent: b => b.AddContent(0, "Custom")));

        var element = cut.Find("[role='checkbox']");
        element.TagName.ShouldBe("DIV");
        element.TextContent.ShouldBe("Custom");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateCheckboxRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "checkbox-root" },
                { "aria-label", "Toggle setting" }
            }
        ));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("data-testid").ShouldBe("checkbox-root");
        checkbox.GetAttribute("aria-label").ShouldBe("Toggle setting");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateCheckboxRoot(
            classValue: _ => "custom-checkbox"
        ));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("class").ShouldContain("custom-checkbox");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateCheckboxRoot(
            styleValue: _ => "width: 20px"
        ));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("style").ShouldContain("width: 20px");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateCheckboxRoot(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }
        ));

        var checkbox = cut.Find("[role='checkbox']");
        var classAttr = checkbox.GetAttribute("class");
        classAttr.ShouldContain("static-class");
        classAttr.ShouldContain("dynamic-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OverridesBuiltInAttributes()
    {
        var cut = Render(CreateCheckboxRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "role", "switch" },
                { "data-custom", "test-value" },
                { "aria-label", "Custom label" }
            }
        ));

        var checkbox = cut.Find("[role='switch']");
        checkbox.GetAttribute("data-custom").ShouldBe("test-value");
        checkbox.GetAttribute("aria-label").ShouldBe("Custom label");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ExplicitAriaLabelledByOverridesFieldLabel()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<FieldLabel>(0);
                fieldBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(labelBuilder =>
                {
                    labelBuilder.AddContent(0, "Terms");
                }));
                fieldBuilder.CloseComponent();

                fieldBuilder.OpenComponent<CheckboxRoot>(2);
                fieldBuilder.AddAttribute(3, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                    {
                        { "aria-labelledby", "external-label" },
                        { "data-testid", "checkbox-root" }
                    });
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var checkbox = cut.Find("[data-testid='checkbox-root']");
        checkbox.GetAttribute("aria-labelledby").ShouldBe("external-label");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesExternalAriaDescribedByWithFieldDescription()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<CheckboxRoot>(0);
                fieldBuilder.AddAttribute(1, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                    {
                        { "aria-describedby", "external-description" },
                        { "data-testid", "checkbox-root" }
                    });
                fieldBuilder.CloseComponent();

                fieldBuilder.OpenComponent<FieldDescription>(2);
                fieldBuilder.AddAttribute(3, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                    {
                        { "id", "field-description" }
                    });
                fieldBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(descriptionBuilder =>
                {
                    descriptionBuilder.AddContent(0, "Description");
                }));
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        cut.WaitForAssertion(() =>
        {
            var checkbox = cut.Find("[data-testid='checkbox-root']");
            checkbox.GetAttribute("aria-describedby").ShouldBe("external-description field-description");
        });

        return Task.CompletedTask;
    }

    // ARIA and role tests
    [Fact]
    public Task HasRoleCheckbox()
    {
        var cut = Render(CreateCheckboxRoot());

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaCheckedFalseByDefault()
    {
        var cut = Render(CreateCheckboxRoot());

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("aria-checked").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaCheckedTrueWhenChecked()
    {
        var cut = Render(CreateCheckboxRoot(defaultChecked: true));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaCheckedMixedWhenIndeterminate()
    {
        var cut = Render(CreateCheckboxRoot(indeterminate: true));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("aria-checked").ShouldBe("mixed");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaRequiredWhenRequired()
    {
        var cut = Render(CreateCheckboxRoot(required: true));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("aria-required").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveAriaRequiredByDefault()
    {
        var cut = Render(CreateCheckboxRoot());

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.HasAttribute("aria-required").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabindexZeroByDefault()
    {
        var cut = Render(CreateCheckboxRoot());

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("tabindex").ShouldBe("0");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabindexMinusOneWhenDisabled()
    {
        var cut = Render(CreateCheckboxRoot(disabled: true));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("tabindex").ShouldBe("-1");

        return Task.CompletedTask;
    }

    // Disabled tests
    [Fact]
    public Task HasAriaDisabledWhenDisabled()
    {
        var cut = Render(CreateCheckboxRoot(disabled: true));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("aria-disabled").ShouldBe("true");
        checkbox.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveAriaDisabledByDefault()
    {
        var cut = Render(CreateCheckboxRoot());

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.HasAttribute("data-disabled").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButtonUsesExplicitIdOnRootAndOmitsHiddenInputId()
    {
        RenderFragment<RenderProps<CheckboxRootState>> renderAsButton = props => builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateCheckboxRoot(
            nativeButton: true,
            render: renderAsButton,
            additionalAttributes: new Dictionary<string, object>
            {
                { "id", "native-checkbox" },
                { "data-testid", "checkbox-root" }
            }));

        var checkbox = cut.Find("[data-testid='checkbox-root']");
        var input = cut.Find("input[type='checkbox']");

        checkbox.TagName.ShouldBe("BUTTON");
        checkbox.GetAttribute("id").ShouldBe("native-checkbox");
        checkbox.GetAttribute("type").ShouldBe("button");
        input.HasAttribute("id").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task FormPropPassesToCheckboxInputAndUncheckedHiddenInput()
    {
        var cut = Render(CreateCheckboxRoot(
            name: "terms",
            form: "external-form",
            uncheckedValue: "off"));

        var checkboxInput = cut.Find("input[type='checkbox']");
        var uncheckedInput = cut.Find("input[type='hidden']");

        checkboxInput.GetAttribute("form").ShouldBe("external-form");
        uncheckedInput.GetAttribute("form").ShouldBe("external-form");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotChangeStateWhenClickedDisabled()
    {
        var invoked = false;

        var cut = Render(CreateCheckboxRoot(
            disabled: true,
            onCheckedChange: EventCallback.Factory.Create<CheckboxCheckedChangeEventArgs>(this, _ => invoked = true)
        ));

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        invoked.ShouldBeFalse();

        return Task.CompletedTask;
    }

    // ReadOnly tests
    [Fact]
    public Task HasAriaReadonlyWhenReadOnly()
    {
        var cut = Render(CreateCheckboxRoot(readOnly: true));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("aria-readonly").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveAriaReadonlyByDefault()
    {
        var cut = Render(CreateCheckboxRoot());

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.HasAttribute("aria-readonly").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotChangeStateWhenClickedReadOnly()
    {
        var invoked = false;

        var cut = Render(CreateCheckboxRoot(
            readOnly: true,
            onCheckedChange: EventCallback.Factory.Create<CheckboxCheckedChangeEventArgs>(this, _ => invoked = true)
        ));

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        invoked.ShouldBeFalse();

        return Task.CompletedTask;
    }

    // Indeterminate tests
    [Fact]
    public Task IndeterminateDoesNotChangeStateWhenClicked()
    {
        var cut = Render(CreateCheckboxRoot(indeterminate: true));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("aria-checked").ShouldBe("mixed");

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        // Indeterminate state is controlled by parameter, not internal state
        checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("aria-checked").ShouldBe("mixed");

        return Task.CompletedTask;
    }

    [Fact]
    public Task IndeterminateOverridesChecked()
    {
        var cut = Render(CreateCheckboxRoot(indeterminate: true, isChecked: true));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("aria-checked").ShouldBe("mixed");

        return Task.CompletedTask;
    }

    // Name and Value tests
    [Fact]
    public Task SetsNameOnInputOnly()
    {
        var cut = Render(CreateCheckboxRoot(name: "checkbox-name"));

        var checkbox = cut.Find("[role='checkbox']");
        var input = cut.Find("input[type='checkbox']");

        checkbox.HasAttribute("name").ShouldBeFalse();
        input.GetAttribute("name").ShouldBe("checkbox-name");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotSetValueByDefault()
    {
        var cut = Render(CreateCheckboxRoot());

        var input = cut.Find("input[type='checkbox']");
        input.HasAttribute("value").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsValueOnInputOnly()
    {
        var cut = Render(CreateCheckboxRoot(value: "checkbox-value"));

        var checkbox = cut.Find("[role='checkbox']");
        var input = cut.Find("input[type='checkbox']");

        checkbox.HasAttribute("value").ShouldBeFalse();
        input.GetAttribute("value").ShouldBe("checkbox-value");

        return Task.CompletedTask;
    }

    // Hidden checkbox input tests
    [Fact]
    public Task RendersHiddenCheckboxInput()
    {
        var cut = Render(CreateCheckboxRoot());

        var input = cut.Find("input[type='checkbox']");
        input.ShouldNotBeNull();
        input.GetAttribute("aria-hidden").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HiddenInputHasCorrectAttributes()
    {
        var cut = Render(CreateCheckboxRoot(disabled: true, required: true));

        var input = cut.Find("input[type='checkbox']");
        input.HasAttribute("disabled").ShouldBeTrue();
        input.HasAttribute("required").ShouldBeTrue();
        input.GetAttribute("tabindex").ShouldBe("-1");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InputHasId()
    {
        var cut = Render(CreateCheckboxRoot());

        var input = cut.Find("input[type='checkbox']");
        input.HasAttribute("id").ShouldBeTrue();
        input.GetAttribute("id").ShouldNotBeNullOrEmpty();

        return Task.CompletedTask;
    }

    // UncheckedValue tests
    [Fact]
    public Task RendersUncheckedValueHiddenInput()
    {
        var cut = Render(CreateCheckboxRoot(name: "toggle", uncheckedValue: "off"));

        var hiddenInput = cut.Find("input[type='hidden']");
        hiddenInput.ShouldNotBeNull();
        hiddenInput.GetAttribute("name").ShouldBe("toggle");
        hiddenInput.GetAttribute("value").ShouldBe("off");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderUncheckedValueWhenChecked()
    {
        var cut = Render(CreateCheckboxRoot(name: "toggle", uncheckedValue: "off", defaultChecked: true));

        var hiddenInputs = cut.FindAll("input[type='hidden']");
        hiddenInputs.Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    // Style hooks (data attributes) tests
    [Fact]
    public Task HasDataCheckedWhenChecked()
    {
        var cut = Render(CreateCheckboxRoot(defaultChecked: true));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.HasAttribute("data-checked").ShouldBeTrue();
        checkbox.HasAttribute("data-unchecked").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataUncheckedWhenUnchecked()
    {
        var cut = Render(CreateCheckboxRoot());

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.HasAttribute("data-unchecked").ShouldBeTrue();
        checkbox.HasAttribute("data-checked").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataIndeterminateWhenIndeterminate()
    {
        var cut = Render(CreateCheckboxRoot(indeterminate: true));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.HasAttribute("data-indeterminate").ShouldBeTrue();
        checkbox.HasAttribute("data-checked").ShouldBeFalse();
        checkbox.HasAttribute("data-unchecked").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateCheckboxRoot(disabled: true));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataReadonlyWhenReadOnly()
    {
        var cut = Render(CreateCheckboxRoot(readOnly: true));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.HasAttribute("data-readonly").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataRequiredWhenRequired()
    {
        var cut = Render(CreateCheckboxRoot(required: true));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.HasAttribute("data-required").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PlacesStyleHooksOnRootAndIndicator()
    {
        var cut = Render(CreateCheckboxWithIndicator(
            defaultChecked: true,
            disabled: true,
            readOnly: true,
            required: true,
            indicatorAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var checkbox = cut.Find("[role='checkbox']");
        var indicator = cut.Find("[data-testid='indicator']");

        // Root should have all data attributes
        checkbox.HasAttribute("data-checked").ShouldBeTrue();
        checkbox.HasAttribute("data-disabled").ShouldBeTrue();
        checkbox.HasAttribute("data-readonly").ShouldBeTrue();
        checkbox.HasAttribute("data-required").ShouldBeTrue();

        // Indicator should have all data attributes
        indicator.HasAttribute("data-checked").ShouldBeTrue();
        indicator.HasAttribute("data-disabled").ShouldBeTrue();
        indicator.HasAttribute("data-readonly").ShouldBeTrue();
        indicator.HasAttribute("data-required").ShouldBeTrue();

        return Task.CompletedTask;
    }

    // Controlled/Uncontrolled tests
    [Fact]
    public Task UncontrolledModeUsesDefaultChecked()
    {
        var cut = Render(CreateCheckboxRoot(defaultChecked: true));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledModeRespectsCheckedParameter()
    {
        var cut = Render(CreateCheckboxRoot(isChecked: true));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesStateWhenControlledValueChanges()
    {
        // Test that controlled value of false renders correctly
        var cutFalse = Render(CreateCheckboxRoot(isChecked: false));
        var checkboxFalse = cutFalse.Find("[role='checkbox']");
        checkboxFalse.GetAttribute("aria-checked").ShouldBe("false");

        // Test that controlled value of true renders correctly
        var cutTrue = Render(CreateCheckboxRoot(isChecked: true));
        var checkboxTrue = cutTrue.Find("[role='checkbox']");
        checkboxTrue.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    // Event callback tests
    [Fact]
    public Task InvokesOnCheckedChangeOnInputChange()
    {
        var invoked = false;
        bool? receivedValue = null;

        var cut = Render(CreateCheckboxRoot(
            onCheckedChange: EventCallback.Factory.Create<CheckboxCheckedChangeEventArgs>(this, args =>
            {
                invoked = true;
                receivedValue = args.Checked;
            })
        ));

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        invoked.ShouldBeTrue();
        receivedValue.ShouldBe(true);

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnCheckedChangeReceivesBaseUiChangeDetails()
    {
        CheckboxCheckedChangeEventArgs? receivedArgs = null;

        var cut = Render(CreateCheckboxRoot(
            onCheckedChange: EventCallback.Factory.Create<CheckboxCheckedChangeEventArgs>(this, args =>
            {
                args.AllowPropagation();
                receivedArgs = args;
            })
        ));

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        receivedArgs.ShouldNotBeNull();
        receivedArgs!.Reason.ShouldBe(CheckboxChangeReason.None);
        receivedArgs.IsPropagationAllowed.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesCheckedChangedOnInputChange()
    {
        var invoked = false;
        bool? receivedValue = null;

        var cut = Render(CreateCheckboxRoot(
            checkedChanged: EventCallback.Factory.Create<bool>(this, value =>
            {
                invoked = true;
                receivedValue = value;
            })
        ));

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        invoked.ShouldBeTrue();
        receivedValue.ShouldBe(true);

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotInvokeCallbacksWhenDisabled()
    {
        var invoked = false;

        var cut = Render(CreateCheckboxRoot(
            disabled: true,
            onCheckedChange: EventCallback.Factory.Create<CheckboxCheckedChangeEventArgs>(this, _ => invoked = true)
        ));

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        invoked.ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotInvokeCallbacksWhenReadOnly()
    {
        var invoked = false;

        var cut = Render(CreateCheckboxRoot(
            readOnly: true,
            onCheckedChange: EventCallback.Factory.Create<CheckboxCheckedChangeEventArgs>(this, _ => invoked = true)
        ));

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        invoked.ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnCheckedChangeCancellationPreventsStateChange()
    {
        var cut = Render(CreateCheckboxRoot(
            onCheckedChange: EventCallback.Factory.Create<CheckboxCheckedChangeEventArgs>(this, args =>
            {
                args.Cancel();
            })
        ));

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("aria-checked").ShouldBe("false");

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        // State should not change because the event was cancelled
        checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("aria-checked").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnCheckedChangeCancellationResetsOptimisticVisualState()
    {
        var cut = Render(CreateCheckboxRoot(
            onCheckedChange: EventCallback.Factory.Create<CheckboxCheckedChangeEventArgs>(this, args =>
            {
                args.Cancel();
            })
        ));

        var resetStateCountBeforeChange = JSInterop.Invocations.Count(invocation => invocation.Identifier == "resetState");

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        JSInterop.Invocations.Count(invocation => invocation.Identifier == "resetState")
            .ShouldBe(resetStateCountBeforeChange + 1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnCheckedChangeCancellationWithoutCapturedRootElementSkipsResetInterop()
    {
        RenderFragment<RenderProps<CheckboxRootState>> renderWithoutElementReference = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            builder.AddContent(2, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateCheckboxRoot(
            render: renderWithoutElementReference,
            onCheckedChange: EventCallback.Factory.Create<CheckboxCheckedChangeEventArgs>(this, args =>
            {
                args.Cancel();
            })
        ));

        var resetStateCountBeforeChange = JSInterop.Invocations.Count(invocation => invocation.Identifier == "resetState");

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        JSInterop.Invocations.Count(invocation => invocation.Identifier == "resetState")
            .ShouldBe(resetStateCountBeforeChange);

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnCheckedChangeCallbackDisablesOptimisticJsState()
    {
        var cut = Render(CreateCheckboxRoot(
            onCheckedChange: EventCallback.Factory.Create<CheckboxCheckedChangeEventArgs>(this, _ => { })
        ));

        cut.WaitForAssertion(() =>
        {
            var initializeInvocation = JSInterop.Invocations.First(invocation => invocation.Identifier == "initialize");
            initializeInvocation.Arguments[7].ShouldBe(false);
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledCheckboxDisablesOptimisticJsState()
    {
        var cut = Render(CreateCheckboxRoot(isChecked: false));

        cut.WaitForAssertion(() =>
        {
            var initializeInvocation = JSInterop.Invocations.First(invocation => invocation.Identifier == "initialize");
            initializeInvocation.Arguments[7].ShouldBe(false);
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task IndeterminateCheckboxDisablesOptimisticJsState()
    {
        var cut = Render(CreateCheckboxRoot(indeterminate: true));

        cut.WaitForAssertion(() =>
        {
            var initializeInvocation = JSInterop.Invocations.First(invocation => invocation.Identifier == "initialize");
            initializeInvocation.Arguments[7].ShouldBe(false);
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledCheckboxWithoutCancelableCallbacksAllowsOptimisticJsState()
    {
        var cut = Render(CreateCheckboxRoot());

        cut.WaitForAssertion(() =>
        {
            var initializeInvocation = JSInterop.Invocations.First(invocation => invocation.Identifier == "initialize");
            initializeInvocation.Arguments[7].ShouldBe(true);
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesStateWhenInputToggled()
    {
        var cut = Render(CreateCheckboxRoot());

        var checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("aria-checked").ShouldBe("false");

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        checkbox = cut.Find("[role='checkbox']");
        checkbox.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    // Context cascading tests
    [Fact]
    public Task CascadesContextToChildren()
    {
        CheckboxIndicatorState? capturedState = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<CheckboxRoot>(0);
            builder.AddAttribute(1, "DefaultChecked", true);
            builder.AddAttribute(2, "Disabled", true);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<CheckboxIndicator>(0);
                innerBuilder.AddAttribute(1, "ClassValue", (Func<CheckboxIndicatorState, string>)(state =>
                {
                    capturedState = state;
                    return "indicator-class";
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        capturedState.ShouldNotBeNull();
        capturedState!.Value.Checked.ShouldBeTrue();
        capturedState.Value.Disabled.ShouldBeTrue();

        return Task.CompletedTask;
    }

    // Element reference tests
    [Fact]
    public Task ExposesElementReference()
    {
        CheckboxRoot? component = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<CheckboxRoot>(0);
            builder.AddComponentReferenceCapture(1, obj => component = (CheckboxRoot)obj);
            builder.CloseComponent();
        });

        component.ShouldNotBeNull();
        // Element reference is captured after render
        cut.WaitForState(() => component!.Element.HasValue);
        component!.Element.HasValue.ShouldBeTrue();

        return Task.CompletedTask;
    }

    // ClassValue/StyleValue state tests
    [Fact]
    public Task ClassValueReceivesCorrectState()
    {
        CheckboxRootState? capturedState = null;

        var cut = Render(CreateCheckboxRoot(
            defaultChecked: true,
            disabled: true,
            readOnly: true,
            required: true,
            indeterminate: false,
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Value.Checked.ShouldBeTrue();
        capturedState.Value.Disabled.ShouldBeTrue();
        capturedState.Value.ReadOnly.ShouldBeTrue();
        capturedState.Value.Required.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task StyleValueReceivesCorrectState()
    {
        CheckboxRootState? capturedState = null;

        var cut = Render(CreateCheckboxRoot(
            defaultChecked: true,
            styleValue: state =>
            {
                capturedState = state;
                return "color: blue";
            }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Value.Checked.ShouldBeTrue();

        return Task.CompletedTask;
    }
}
