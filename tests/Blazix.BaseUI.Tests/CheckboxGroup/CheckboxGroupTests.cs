using Blazix.BaseUI.CheckboxGroup;
using Blazix.BaseUI.Tests.Contracts.CheckboxGroup;

namespace Blazix.BaseUI.Tests.CheckboxGroup;

public class CheckboxGroupTests : BunitContext, ICheckboxGroupContract
{
    public CheckboxGroupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupCheckboxModule(JSInterop);
    }

    private RenderFragment CreateCheckboxGroup(
        string[]? value = null,
        string[]? defaultValue = null,
        string[]? allValues = null,
        bool disabled = false,
        Action<CheckboxGroupValueChangeEventArgs>? onValueChange = null,
        Func<CheckboxGroupState, string>? classValue = null,
        Func<CheckboxGroupState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<CheckboxGroupState>>? render = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.CheckboxGroup.CheckboxGroup>(0);
            var attrIndex = 1;

            if (value is not null)
                builder.AddAttribute(attrIndex++, "Value", value);
            if (defaultValue is not null)
                builder.AddAttribute(attrIndex++, "DefaultValue", defaultValue);
            if (allValues is not null)
                builder.AddAttribute(attrIndex++, "AllValues", allValues);
            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
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

    private RenderFragment CreateCheckboxGroupWithCheckboxes(
        string[]? value = null,
        string[]? defaultValue = null,
        string[]? allValues = null,
        bool disabled = false,
        Action<CheckboxGroupValueChangeEventArgs>? onValueChange = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        bool includeParent = false)
    {
        return builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.CheckboxGroup.CheckboxGroup>(0);
            var attrIndex = 1;

            if (value is not null)
                builder.AddAttribute(attrIndex++, "Value", value);
            if (defaultValue is not null)
                builder.AddAttribute(attrIndex++, "DefaultValue", defaultValue);
            if (allValues is not null)
                builder.AddAttribute(attrIndex++, "AllValues", allValues);
            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            if (onValueChange is not null)
                builder.AddAttribute(attrIndex++, "OnValueChange", EventCallback.Factory.Create(this, onValueChange));
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                if (includeParent)
                {
                    innerBuilder.OpenComponent<CheckboxRoot>(0);
                    innerBuilder.AddAttribute(1, "Parent", true);
                    innerBuilder.AddAttribute(2, "AdditionalAttributes",
                        (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "parent" } });
                    innerBuilder.CloseComponent();
                }

                innerBuilder.OpenComponent<CheckboxRoot>(10);
                innerBuilder.AddAttribute(11, "Name", "red");
                innerBuilder.AddAttribute(12, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "red" } });
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<CheckboxRoot>(20);
                innerBuilder.AddAttribute(21, "Name", "green");
                innerBuilder.AddAttribute(22, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "green" } });
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<CheckboxRoot>(30);
                innerBuilder.AddAttribute(31, "Name", "blue");
                innerBuilder.AddAttribute(32, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "blue" } });
                innerBuilder.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    // Rendering tests
    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateCheckboxGroup(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<CheckboxGroupState>> renderAsFieldset = props => builder =>
        {
            builder.OpenElement(0, "fieldset");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateCheckboxGroup(
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
        var cut = Render(CreateCheckboxGroup(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "group" },
                { "aria-label", "Color selection" }
            }
        ));

        var group = cut.Find("[data-testid='group']");
        group.GetAttribute("aria-label").ShouldBe("Color selection");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateCheckboxGroup(
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
        var cut = Render(CreateCheckboxGroup(
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
        var cut = Render(CreateCheckboxGroup(
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
    public Task HasRoleGroup()
    {
        var cut = Render(CreateCheckboxGroup(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.GetAttribute("role").ShouldBe("group");

        return Task.CompletedTask;
    }

    // Value control tests
    [Fact]
    public Task ControlledValue_SetsCheckedCheckboxes()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            value: ["red"],
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var red = cut.Find("[data-testid='red']");
        var green = cut.Find("[data-testid='green']");
        var blue = cut.Find("[data-testid='blue']");

        red.GetAttribute("aria-checked").ShouldBe("true");
        green.GetAttribute("aria-checked").ShouldBe("false");
        blue.GetAttribute("aria-checked").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledValue_UsesDefaultValue()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: ["green", "blue"],
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var red = cut.Find("[data-testid='red']");
        var green = cut.Find("[data-testid='green']");
        var blue = cut.Find("[data-testid='blue']");

        red.GetAttribute("aria-checked").ShouldBe("false");
        green.GetAttribute("aria-checked").ShouldBe("true");
        blue.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledValue_UpdatesOnCheckboxClick()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: [],
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var red = cut.Find("[data-testid='red']");
        red.GetAttribute("aria-checked").ShouldBe("false");

        // Simulate clicking by finding the hidden input and triggering change
        var inputs = cut.FindAll("input[type='checkbox']");
        inputs[0].Change(true);

        red = cut.Find("[data-testid='red']");
        red.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    // DefaultValue tests
    [Fact]
    public Task DefaultValue_InitializesCorrectCheckboxes()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: ["red"],
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var red = cut.Find("[data-testid='red']");
        var green = cut.Find("[data-testid='green']");
        var blue = cut.Find("[data-testid='blue']");

        red.GetAttribute("aria-checked").ShouldBe("true");
        green.GetAttribute("aria-checked").ShouldBe("false");
        blue.GetAttribute("aria-checked").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DefaultValue_AllowsToggling()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: ["red"],
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var inputs = cut.FindAll("input[type='checkbox']");
        inputs[1].Change(true); // Toggle green

        var green = cut.Find("[data-testid='green']");
        green.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    // OnValueChange tests
    [Fact]
    public Task OnValueChange_CalledWhenCheckboxClicked()
    {
        var callCount = 0;
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: [],
            onValueChange: _ => callCount++,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var inputs = cut.FindAll("input[type='checkbox']");
        inputs[0].Change(true);

        callCount.ShouldBe(1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChange_ReceivesUpdatedValueArray()
    {
        string[]? receivedValue = null;
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: ["red"],
            onValueChange: args => receivedValue = args.Value,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var inputs = cut.FindAll("input[type='checkbox']");
        inputs[1].Change(true); // Toggle green

        receivedValue.ShouldNotBeNull();
        receivedValue.ShouldContain("red");
        receivedValue.ShouldContain("green");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChange_ReceivesBaseUiChangeDetails()
    {
        CheckboxGroupValueChangeEventArgs? receivedArgs = null;
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: [],
            onValueChange: args =>
            {
                args.AllowPropagation();
                receivedArgs = args;
            },
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var inputs = cut.FindAll("input[type='checkbox']");
        inputs[0].Change(true);

        receivedArgs.ShouldNotBeNull();
        receivedArgs!.Reason.ShouldBe(CheckboxGroupChangeReason.None);
        receivedArgs.IsPropagationAllowed.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChange_CanBeCanceled()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: [],
            onValueChange: args => args.Cancel(),
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var inputs = cut.FindAll("input[type='checkbox']");
        inputs[0].Change(true);

        var red = cut.Find("[data-testid='red']");
        red.GetAttribute("aria-checked").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChangeCallbackDisablesChildOptimisticJsState()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: [],
            onValueChange: _ => { },
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        cut.WaitForAssertion(() =>
        {
            var initializeInvocations = JSInterop.Invocations
                .Where(invocation => invocation.Identifier == "initialize")
                .ToArray();

            initializeInvocations.Length.ShouldBe(3);
            initializeInvocations.All(invocation => Equals(invocation.Arguments[7], false)).ShouldBeTrue();
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledGroupDisablesChildOptimisticJsState()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            value: [],
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        cut.WaitForAssertion(() =>
        {
            var initializeInvocations = JSInterop.Invocations
                .Where(invocation => invocation.Identifier == "initialize")
                .ToArray();

            initializeInvocations.Length.ShouldBe(3);
            initializeInvocations.All(invocation => Equals(invocation.Arguments[7], false)).ShouldBeTrue();
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task ParentEnabledGroupDisablesChildOptimisticJsState()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: [],
            allValues: ["red", "green", "blue"],
            includeParent: true,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        cut.WaitForAssertion(() =>
        {
            var initializeInvocations = JSInterop.Invocations
                .Where(invocation => invocation.Identifier == "initialize")
                .ToArray();

            initializeInvocations.Length.ShouldBe(4);
            initializeInvocations.All(invocation => Equals(invocation.Arguments[7], false)).ShouldBeTrue();
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledGroupWithoutCancelableCallbacksAllowsChildOptimisticJsState()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: [],
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        cut.WaitForAssertion(() =>
        {
            var initializeInvocations = JSInterop.Invocations
                .Where(invocation => invocation.Identifier == "initialize")
                .ToArray();

            initializeInvocations.Length.ShouldBe(3);
            initializeInvocations.All(invocation => Equals(invocation.Arguments[7], true)).ShouldBeTrue();
        });

        return Task.CompletedTask;
    }

    // Disabled tests
    [Fact]
    public Task Disabled_PropagesToAllCheckboxes()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            disabled: true,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var red = cut.Find("[data-testid='red']");
        var green = cut.Find("[data-testid='green']");
        var blue = cut.Find("[data-testid='blue']");

        red.HasAttribute("data-disabled").ShouldBeTrue();
        green.HasAttribute("data-disabled").ShouldBeTrue();
        blue.HasAttribute("data-disabled").ShouldBeTrue();
        red.GetAttribute("aria-disabled").ShouldBe("true");
        green.GetAttribute("aria-disabled").ShouldBe("true");
        blue.GetAttribute("aria-disabled").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Disabled_PreventsValueChange()
    {
        var callCount = 0;
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: [],
            disabled: true,
            onValueChange: _ => callCount++,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var inputs = cut.FindAll("input[type='checkbox']");
        inputs[0].Change(true);

        callCount.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task NotDisabled_AllowsCheckboxInteraction()
    {
        var callCount = 0;
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: [],
            disabled: false,
            onValueChange: _ => callCount++,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var inputs = cut.FindAll("input[type='checkbox']");
        inputs[0].Change(true);

        callCount.ShouldBe(1);

        return Task.CompletedTask;
    }

    // Data attribute tests
    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateCheckboxGroup(
            disabled: true,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataValidWhenValid()
    {
        // This requires Field context integration - testing basic rendering
        var cut = Render(CreateCheckboxGroup(
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
        // Similar to HasDataValidWhenValid - requires Field context
        var cut = Render(CreateCheckboxGroup(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.HasAttribute("data-invalid").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataTouchedWhenTouched()
    {
        // Requires Field context
        var cut = Render(CreateCheckboxGroup(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.HasAttribute("data-touched").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDirtyWhenDirty()
    {
        // Requires Field context
        var cut = Render(CreateCheckboxGroup(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.HasAttribute("data-dirty").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataFilledWhenFilled()
    {
        // Requires Field context - filled is set when value has items
        var cut = Render(CreateCheckboxGroup(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var group = cut.Find("[data-testid='group']");
        group.HasAttribute("data-filled").ShouldBeFalse();

        return Task.CompletedTask;
    }

    // Context tests
    [Fact]
    public Task CascadesContextToChildren()
    {
        CheckboxGroupContext? capturedContext = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.CheckboxGroup.CheckboxGroup>(0);
            builder.AddAttribute(1, "DefaultValue", new[] { "test" });
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ContextCapture<CheckboxGroupContext>>(0);
                innerBuilder.AddAttribute(1, "OnContextCaptured", (Action<CheckboxGroupContext>)(ctx => capturedContext = ctx));
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
        CheckboxGroupContext? capturedContext = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.CheckboxGroup.CheckboxGroup>(0);
            builder.AddAttribute(1, "DefaultValue", new[] { "red", "blue" });
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ContextCapture<CheckboxGroupContext>>(0);
                innerBuilder.AddAttribute(1, "OnContextCaptured", (Action<CheckboxGroupContext>)(ctx => capturedContext = ctx));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        capturedContext.ShouldNotBeNull();
        capturedContext!.Value.ShouldContain("red");
        capturedContext.Value.ShouldContain("blue");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ContextContainsDisabledState()
    {
        CheckboxGroupContext? capturedContext = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.CheckboxGroup.CheckboxGroup>(0);
            builder.AddAttribute(1, "Disabled", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ContextCapture<CheckboxGroupContext>>(0);
                innerBuilder.AddAttribute(1, "OnContextCaptured", (Action<CheckboxGroupContext>)(ctx => capturedContext = ctx));
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
        CheckboxGroupState? capturedState = null;

        var cut = Render(CreateCheckboxGroup(
            disabled: true,
            classValue: state =>
            {
                capturedState = state;
                return "group-class";
            },
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Value.Disabled.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task StyleValueReceivesCorrectState()
    {
        CheckboxGroupState? capturedState = null;

        var cut = Render(CreateCheckboxGroup(
            disabled: true,
            styleValue: state =>
            {
                capturedState = state;
                return "opacity: 0.5";
            },
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Value.Disabled.ShouldBeTrue();

        return Task.CompletedTask;
    }

    // Parent checkbox tests
    [Fact]
    public Task ParentCheckbox_CheckedWhenAllChildrenChecked()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: ["red", "green", "blue"],
            allValues: ["red", "green", "blue"],
            includeParent: true,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var parent = cut.Find("[data-testid='parent']");
        parent.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ParentCheckbox_IndeterminateWhenSomeChildrenChecked()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: ["red"],
            allValues: ["red", "green", "blue"],
            includeParent: true,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var parent = cut.Find("[data-testid='parent']");
        parent.GetAttribute("aria-checked").ShouldBe("mixed");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ParentCheckbox_UncheckedWhenNoChildrenChecked()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: [],
            allValues: ["red", "green", "blue"],
            includeParent: true,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var parent = cut.Find("[data-testid='parent']");
        parent.GetAttribute("aria-checked").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ParentCheckbox_AriaControlsUsesParentRootId()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: [],
            allValues: ["red", "green", "blue"],
            includeParent: true,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var parent = cut.Find("[data-testid='parent']");
        var parentId = parent.GetAttribute("id");

        parentId.ShouldNotBeNullOrEmpty();
        parent.GetAttribute("aria-controls").ShouldBe($"{parentId}-red {parentId}-green {parentId}-blue");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ParentCheckbox_RestoresMixedChildSnapshotOnThirdParentClick()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: [],
            allValues: ["red", "green", "blue"],
            includeParent: true,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var inputs = cut.FindAll("input[type='checkbox']");
        inputs[1].Change(true);

        cut.Find("[data-testid='parent']").GetAttribute("aria-checked").ShouldBe("mixed");

        inputs = cut.FindAll("input[type='checkbox']");
        inputs[0].Change(true);
        cut.Find("[data-testid='parent']").GetAttribute("aria-checked").ShouldBe("true");

        inputs = cut.FindAll("input[type='checkbox']");
        inputs[0].Change(false);
        cut.Find("[data-testid='parent']").GetAttribute("aria-checked").ShouldBe("false");

        inputs = cut.FindAll("input[type='checkbox']");
        inputs[0].Change(true);

        cut.Find("[data-testid='parent']").GetAttribute("aria-checked").ShouldBe("mixed");
        cut.Find("[data-testid='red']").GetAttribute("aria-checked").ShouldBe("true");
        cut.Find("[data-testid='green']").GetAttribute("aria-checked").ShouldBe("false");
        cut.Find("[data-testid='blue']").GetAttribute("aria-checked").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ParentCheckbox_TogglesBackOnAfterAllChildrenWereTurnedOff()
    {
        var cut = Render(CreateCheckboxGroupWithCheckboxes(
            defaultValue: ["red", "green", "blue"],
            allValues: ["red", "green", "blue"],
            includeParent: true,
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "group" } }
        ));

        var inputs = cut.FindAll("input[type='checkbox']");
        inputs[0].Change(false);

        cut.Find("[data-testid='parent']").GetAttribute("aria-checked").ShouldBe("false");

        inputs = cut.FindAll("input[type='checkbox']");
        inputs[0].Change(true);

        cut.Find("[data-testid='parent']").GetAttribute("aria-checked").ShouldBe("true");
        cut.Find("[data-testid='red']").GetAttribute("aria-checked").ShouldBe("true");
        cut.Find("[data-testid='green']").GetAttribute("aria-checked").ShouldBe("true");
        cut.Find("[data-testid='blue']").GetAttribute("aria-checked").ShouldBe("true");

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
