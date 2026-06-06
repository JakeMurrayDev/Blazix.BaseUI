namespace BlazorBaseUI.Tests.ToggleGroup;

public class ToggleGroupTests : BunitContext, IToggleGroupContract
{
    public ToggleGroupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupToggleModule(JSInterop);
        JsInteropSetup.SetupToolbarModule(JSInterop);
    }

    private RenderFragment CreateToggleGroup(
        IReadOnlyList<string>? value = null,
        IReadOnlyList<string>? defaultValue = null,
        bool disabled = false,
        Orientation orientation = Orientation.Horizontal,
        bool loopFocus = true,
        bool multiple = false,
        Func<ToggleGroupState, string>? classValue = null,
        Func<ToggleGroupState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Action<ToggleGroupValueChangeEventArgs>? onValueChange = null,
        RenderFragment<RenderProps<ToggleGroupState>>? render = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<BlazorBaseUI.ToggleGroup.ToggleGroup>(0);
            var attrIndex = 1;

            if (value is not null)
                builder.AddAttribute(attrIndex++, "Value", value);
            if (defaultValue is not null)
                builder.AddAttribute(attrIndex++, "DefaultValue", defaultValue);
            builder.AddAttribute(attrIndex++, "Disabled", disabled);
            builder.AddAttribute(attrIndex++, "Orientation", orientation);
            builder.AddAttribute(attrIndex++, "LoopFocus", loopFocus);
            builder.AddAttribute(attrIndex++, "Multiple", multiple);

            if (classValue is not null)
                builder.AddAttribute(attrIndex++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(attrIndex++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
            if (onValueChange is not null)
                builder.AddAttribute(attrIndex++, "OnValueChange", EventCallback.Factory.Create(this, onValueChange));
            if (render is not null)
                builder.AddAttribute(attrIndex++, "Render", render);
            if (childContent is not null)
                builder.AddAttribute(attrIndex++, "ChildContent", childContent);

            builder.CloseComponent();
        };
    }

    private RenderFragment CreateToggleGroupWithToggles(
        IReadOnlyList<string>? value = null,
        IReadOnlyList<string>? defaultValue = null,
        bool disabled = false,
        Orientation orientation = Orientation.Horizontal,
        bool loopFocus = true,
        bool multiple = false,
        Action<ToggleGroupValueChangeEventArgs>? onValueChange = null,
        EventCallback<IReadOnlyList<string>>? valueChanged = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        bool toggle2Disabled = false)
    {
        return builder =>
        {
            builder.OpenComponent<BlazorBaseUI.ToggleGroup.ToggleGroup>(0);
            var attrIndex = 1;

            if (value is not null)
                builder.AddAttribute(attrIndex++, "Value", value);
            if (defaultValue is not null)
                builder.AddAttribute(attrIndex++, "DefaultValue", defaultValue);
            builder.AddAttribute(attrIndex++, "Disabled", disabled);
            builder.AddAttribute(attrIndex++, "Orientation", orientation);
            builder.AddAttribute(attrIndex++, "LoopFocus", loopFocus);
            builder.AddAttribute(attrIndex++, "Multiple", multiple);

            if (onValueChange is not null)
                builder.AddAttribute(attrIndex++, "OnValueChange", EventCallback.Factory.Create(this, onValueChange));
            if (valueChanged.HasValue)
                builder.AddAttribute(attrIndex++, "ValueChanged", valueChanged.Value);
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                // Toggle "one"
                innerBuilder.OpenComponent<BlazorBaseUI.Toggle.Toggle>(0);
                innerBuilder.AddAttribute(1, "Value", "one");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "One")));
                innerBuilder.CloseComponent();

                // Toggle "two"
                innerBuilder.OpenComponent<BlazorBaseUI.Toggle.Toggle>(10);
                innerBuilder.AddAttribute(11, "Value", "two");
                if (toggle2Disabled)
                    innerBuilder.AddAttribute(12, "Disabled", true);
                innerBuilder.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Two")));
                innerBuilder.CloseComponent();

                // Toggle "three"
                innerBuilder.OpenComponent<BlazorBaseUI.Toggle.Toggle>(20);
                innerBuilder.AddAttribute(21, "Value", "three");
                innerBuilder.AddAttribute(22, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Three")));
                innerBuilder.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateToggleGroup());
        var div = cut.Find("[role='group']");
        div.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateToggleGroup(
            render: props => builder =>
            {
                builder.OpenElement(0, "section");
                builder.AddMultipleAttributes(1, props.Attributes);
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
                builder.AddContent(3, props.ChildContent);
                builder.CloseElement();
            }));
        var element = cut.Find("section");
        element.TagName.ShouldBe("SECTION");
        element.GetAttribute("role").ShouldBe("group");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateToggleGroup(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "my-group" },
                { "aria-label", "Text formatting" }
            }));
        var group = cut.Find("[role='group']");
        group.GetAttribute("data-testid").ShouldBe("my-group");
        group.GetAttribute("aria-label").ShouldBe("Text formatting");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateToggleGroup(
            classValue: _ => "custom-group"));
        var group = cut.Find("[role='group']");
        group.GetAttribute("class").ShouldContain("custom-group");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateToggleGroup(
            styleValue: _ => "gap: 8px"));
        var group = cut.Find("[role='group']");
        group.GetAttribute("style").ShouldContain("gap: 8px");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateToggleGroup(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }));
        var group = cut.Find("[role='group']");
        var classAttr = group.GetAttribute("class");
        classAttr.ShouldContain("static-class");
        classAttr.ShouldContain("dynamic-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRoleGroup()
    {
        var cut = Render(CreateToggleGroup());
        var group = cut.Find("[role='group']");
        group.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateToggleGroup(disabled: true));
        var group = cut.Find("[role='group']");
        group.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataDisabledByDefault()
    {
        var cut = Render(CreateToggleGroup());
        var group = cut.Find("[role='group']");
        group.HasAttribute("data-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataMultipleWhenMultiple()
    {
        var cut = Render(CreateToggleGroup(multiple: true));
        var group = cut.Find("[role='group']");
        group.HasAttribute("data-multiple").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataMultipleByDefault()
    {
        var cut = Render(CreateToggleGroup());
        var group = cut.Find("[role='group']");
        group.HasAttribute("data-multiple").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientationHorizontal()
    {
        var cut = Render(CreateToggleGroup(orientation: Orientation.Horizontal));
        var group = cut.Find("[role='group']");
        group.GetAttribute("data-orientation").ShouldBe("horizontal");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientationVertical()
    {
        var cut = Render(CreateToggleGroup(orientation: Orientation.Vertical));
        var group = cut.Find("[role='group']");
        group.GetAttribute("data-orientation").ShouldBe("vertical");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaOrientationHorizontal()
    {
        var cut = Render(CreateToggleGroup(orientation: Orientation.Horizontal));
        var group = cut.Find("[role='group']");
        group.GetAttribute("aria-orientation").ShouldBe("horizontal");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaOrientationVertical()
    {
        var cut = Render(CreateToggleGroup(orientation: Orientation.Vertical));
        var group = cut.Find("[role='group']");
        group.GetAttribute("aria-orientation").ShouldBe("vertical");
        return Task.CompletedTask;
    }

    // Disabled

    [Fact]
    public Task DisabledGroup_PropagatesDataDisabledToToggles()
    {
        var cut = Render(CreateToggleGroupWithToggles(disabled: true));
        var toggles = cut.FindAll("button[aria-pressed]");
        toggles.Count.ShouldBe(3);
        foreach (var toggle in toggles)
        {
            toggle.HasAttribute("data-disabled").ShouldBeTrue();
        }
        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledGroup_BlocksValueChange()
    {
        var callCount = 0;
        var cut = Render(CreateToggleGroupWithToggles(
            disabled: true,
            onValueChange: _ => callCount++));

        var toggle = cut.Find("button[aria-pressed]");
        toggle.Click();

        callCount.ShouldBe(0);
        return Task.CompletedTask;
    }

    [Fact]
    public Task IndividualToggle_CanBeDisabled()
    {
        var cut = Render(CreateToggleGroupWithToggles(toggle2Disabled: true));
        var toggles = cut.FindAll("button[aria-pressed]");
        toggles.Count.ShouldBe(3);

        // Toggle "one" should not be disabled
        toggles[0].HasAttribute("data-disabled").ShouldBeFalse();
        // Toggle "two" should be disabled
        toggles[1].HasAttribute("data-disabled").ShouldBeTrue();
        // Toggle "three" should not be disabled
        toggles[2].HasAttribute("data-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // Value control

    [Fact]
    public Task ControlledValue_SetsPressedToggles()
    {
        var cut = Render(CreateToggleGroupWithToggles(value: ["two"]));
        var toggles = cut.FindAll("button[aria-pressed]");

        toggles[0].GetAttribute("aria-pressed").ShouldBe("false");
        toggles[1].GetAttribute("aria-pressed").ShouldBe("true");
        toggles[2].GetAttribute("aria-pressed").ShouldBe("false");
        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledDefaultValue_SetsPressedToggles()
    {
        var cut = Render(CreateToggleGroupWithToggles(defaultValue: ["two"]));
        var toggles = cut.FindAll("button[aria-pressed]");

        toggles[0].GetAttribute("aria-pressed").ShouldBe("false");
        toggles[1].GetAttribute("aria-pressed").ShouldBe("true");
        toggles[2].GetAttribute("aria-pressed").ShouldBe("false");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Uncontrolled_TogglesOnClick()
    {
        var cut = Render(CreateToggleGroupWithToggles());
        var toggles = cut.FindAll("button[aria-pressed]");

        // Initially none pressed
        toggles[0].GetAttribute("aria-pressed").ShouldBe("false");

        // Click toggle "one"
        toggles[0].Click();

        // Re-query after click
        toggles = cut.FindAll("button[aria-pressed]");
        toggles[0].GetAttribute("aria-pressed").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Uncontrolled_SingleMode_DeselectsPrevious()
    {
        var cut = Render(CreateToggleGroupWithToggles());
        var toggles = cut.FindAll("button[aria-pressed]");

        // Click toggle "one"
        toggles[0].Click();

        toggles = cut.FindAll("button[aria-pressed]");
        toggles[0].GetAttribute("aria-pressed").ShouldBe("true");

        // Click toggle "two" - should deselect "one"
        toggles[1].Click();

        toggles = cut.FindAll("button[aria-pressed]");
        toggles[0].GetAttribute("aria-pressed").ShouldBe("false");
        toggles[1].GetAttribute("aria-pressed").ShouldBe("true");
        return Task.CompletedTask;
    }

    // Multiple mode

    [Fact]
    public Task Multiple_AllowsMultiplePressed()
    {
        var cut = Render(CreateToggleGroupWithToggles(multiple: true));
        var toggles = cut.FindAll("button[aria-pressed]");

        // Click toggle "one"
        toggles[0].Click();

        toggles = cut.FindAll("button[aria-pressed]");
        toggles[0].GetAttribute("aria-pressed").ShouldBe("true");

        // Click toggle "two" - both should stay pressed
        toggles[1].Click();

        toggles = cut.FindAll("button[aria-pressed]");
        toggles[0].GetAttribute("aria-pressed").ShouldBe("true");
        toggles[1].GetAttribute("aria-pressed").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Single_OnlyOnePressed()
    {
        var cut = Render(CreateToggleGroupWithToggles(defaultValue: ["one"]));
        var toggles = cut.FindAll("button[aria-pressed]");

        toggles[0].GetAttribute("aria-pressed").ShouldBe("true");

        // Click toggle "two"
        toggles[1].Click();

        toggles = cut.FindAll("button[aria-pressed]");
        toggles[0].GetAttribute("aria-pressed").ShouldBe("false");
        toggles[1].GetAttribute("aria-pressed").ShouldBe("true");
        return Task.CompletedTask;
    }

    // OnValueChange

    [Fact]
    public Task OnValueChange_ReceivesCorrectValue()
    {
        IReadOnlyList<string>? receivedValue = null;
        var cut = Render(CreateToggleGroupWithToggles(
            onValueChange: args => receivedValue = args.Value));

        var toggles = cut.FindAll("button[aria-pressed]");
        toggles[0].Click();

        receivedValue.ShouldNotBeNull();
        receivedValue.ShouldContain("one");
        return Task.CompletedTask;
    }

    [Fact]
    public Task OnValueChange_CanBeCanceled()
    {
        var cut = Render(CreateToggleGroupWithToggles(
            onValueChange: args => args.Cancel()));

        var toggles = cut.FindAll("button[aria-pressed]");
        toggles[0].Click();

        // Value should not have changed because Cancel() was called
        toggles = cut.FindAll("button[aria-pressed]");
        toggles[0].GetAttribute("aria-pressed").ShouldBe("false");
        return Task.CompletedTask;
    }

    // Context

    [Fact]
    public Task CascadesContextToChildren()
    {
        IToggleGroupContext? capturedContext = null;
        var cut = Render(CreateToggleGroup(
            childContent: builder =>
            {
                builder.OpenComponent<ContextCapture<IToggleGroupContext>>(0);
                builder.AddAttribute(1, "OnContextCaptured",
                    (Action<IToggleGroupContext>)(ctx => capturedContext = ctx));
                builder.CloseComponent();
            }));

        capturedContext.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ContextContainsDisabledState()
    {
        IToggleGroupContext? capturedContext = null;
        var cut = Render(CreateToggleGroup(
            disabled: true,
            childContent: builder =>
            {
                builder.OpenComponent<ContextCapture<IToggleGroupContext>>(0);
                builder.AddAttribute(1, "OnContextCaptured",
                    (Action<IToggleGroupContext>)(ctx => capturedContext = ctx));
                builder.CloseComponent();
            }));

        capturedContext.ShouldNotBeNull();
        capturedContext!.Disabled.ShouldBeTrue();
        return Task.CompletedTask;
    }

    // State

    [Fact]
    public Task ClassValueReceivesCorrectState()
    {
        ToggleGroupState? capturedState = null;
        var cut = Render(CreateToggleGroup(
            disabled: true,
            multiple: true,
            orientation: Orientation.Vertical,
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }));

        capturedState.ShouldNotBeNull();
        capturedState!.Disabled.ShouldBeTrue();
        capturedState!.Multiple.ShouldBeTrue();
        capturedState!.Orientation.ShouldBe(Orientation.Vertical);
        return Task.CompletedTask;
    }

    [Fact]
    public Task StyleValueReceivesCorrectState()
    {
        ToggleGroupState? capturedState = null;
        var cut = Render(CreateToggleGroup(
            disabled: true,
            multiple: true,
            orientation: Orientation.Vertical,
            styleValue: state =>
            {
                capturedState = state;
                return "gap: 8px";
            }));

        capturedState.ShouldNotBeNull();
        capturedState!.Disabled.ShouldBeTrue();
        capturedState!.Multiple.ShouldBeTrue();
        capturedState!.Orientation.ShouldBe(Orientation.Vertical);
        return Task.CompletedTask;
    }

    // TabIndex

    [Fact]
    public Task InitialRovingTabIndex_UsesFirstEnabledToggleNotPressedToggle()
    {
        var cut = Render(CreateToggleGroupWithToggles(defaultValue: ["two"]));
        var toggles = cut.FindAll("button[aria-pressed]");

        toggles[0].GetAttribute("tabindex").ShouldBe("0");
        toggles[1].GetAttribute("tabindex").ShouldBe("-1");
        toggles[2].GetAttribute("tabindex").ShouldBe("-1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ToolbarDisabled_DisablesToggleGroupAndChildren()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<BlazorBaseUI.Toolbar.ToolbarRoot>(0);
            builder.AddAttribute(1, "Disabled", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(toolbarBuilder =>
            {
                toolbarBuilder.OpenComponent<BlazorBaseUI.ToggleGroup.ToggleGroup>(0);
                toolbarBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(groupBuilder =>
                {
                    groupBuilder.OpenComponent<BlazorBaseUI.Toggle.Toggle>(0);
                    groupBuilder.AddAttribute(1, "Value", "one");
                    groupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "One")));
                    groupBuilder.CloseComponent();
                }));
                toolbarBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var groups = cut.FindAll("[role='group']");
        var toggleGroup = groups.Single(group => group.QuerySelector("button[aria-pressed]") is not null);
        toggleGroup.HasAttribute("data-disabled").ShouldBeTrue();
        toggleGroup.HasAttribute("aria-orientation").ShouldBeFalse();

        var toggle = cut.Find("button[aria-pressed]");
        toggle.HasAttribute("data-disabled").ShouldBeTrue();
        toggle.GetAttribute("aria-disabled").ShouldBe("true");
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
