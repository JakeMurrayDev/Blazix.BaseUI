namespace BlazorBaseUI.Tests.Toggle;

public class ToggleTests : BunitContext, IToggleContract
{
    public ToggleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupToggleModule(JSInterop);
    }

    private RenderFragment CreateToggle(
        bool? pressed = null,
        bool defaultPressed = false,
        bool disabled = false,
        bool nativeButton = true,
        Func<ToggleState, string>? classValue = null,
        Func<ToggleState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Action<TogglePressedChangeEventArgs>? onPressedChange = null,
        RenderFragment<RenderProps<ToggleState>>? render = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<BlazorBaseUI.Toggle.Toggle>(0);
            var attrIndex = 1;

            if (pressed.HasValue)
                builder.AddAttribute(attrIndex++, "Pressed", pressed.Value);
            if (defaultPressed)
                builder.AddAttribute(attrIndex++, "DefaultPressed", true);
            builder.AddAttribute(attrIndex++, "Disabled", disabled);
            builder.AddAttribute(attrIndex++, "NativeButton", nativeButton);

            if (classValue is not null)
                builder.AddAttribute(attrIndex++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(attrIndex++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
            if (onPressedChange is not null)
                builder.AddAttribute(attrIndex++, "OnPressedChange", EventCallback.Factory.Create(this, onPressedChange));
            if (render is not null)
                builder.AddAttribute(attrIndex++, "Render", render);
            if (childContent is not null)
                builder.AddAttribute(attrIndex++, "ChildContent", childContent);

            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsButtonByDefault()
    {
        var cut = Render(CreateToggle());
        var button = cut.Find("button");
        button.TagName.ShouldBe("BUTTON");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateToggle(
            render: props => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddMultipleAttributes(1, props.Attributes);
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
                builder.AddContent(3, props.ChildContent);
                builder.CloseElement();
            }));
        var element = cut.Find("span");
        element.TagName.ShouldBe("SPAN");
        element.GetAttribute("aria-pressed").ShouldBe("false");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateToggle(
            childContent: b => b.AddContent(0, "Toggle me")));
        var button = cut.Find("button");
        button.TextContent.ShouldBe("Toggle me");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateToggle(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "my-toggle" },
                { "aria-label", "Toggle bold" }
            }));
        var button = cut.Find("button");
        button.GetAttribute("data-testid").ShouldBe("my-toggle");
        button.GetAttribute("aria-label").ShouldBe("Toggle bold");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateToggle(
            classValue: _ => "custom-toggle"));
        var button = cut.Find("button");
        button.GetAttribute("class").ShouldContain("custom-toggle");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateToggle(
            styleValue: _ => "color: red"));
        var button = cut.Find("button");
        button.GetAttribute("style").ShouldContain("color: red");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateToggle(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }));
        var button = cut.Find("button");
        var classAttr = button.GetAttribute("class");
        classAttr.ShouldContain("static-class");
        classAttr.ShouldContain("dynamic-class");
        return Task.CompletedTask;
    }

    // ARIA

    [Fact]
    public Task HasAriaPressedFalseByDefault()
    {
        var cut = Render(CreateToggle());
        var button = cut.Find("button");
        button.GetAttribute("aria-pressed").ShouldBe("false");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaPressedTrueWhenPressed()
    {
        var cut = Render(CreateToggle(pressed: true));
        var button = cut.Find("button");
        button.GetAttribute("aria-pressed").ShouldBe("true");
        return Task.CompletedTask;
    }

    // Native button attributes

    [Fact]
    public Task NativeButton_HasTypeButton()
    {
        var cut = Render(CreateToggle());
        var button = cut.Find("button");
        button.GetAttribute("type").ShouldBe("button");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_OverridesUserTypeAndOmitsForm()
    {
        var cut = Render(CreateToggle(
            additionalAttributes: new Dictionary<string, object>
            {
                ["type"] = "submit",
                ["form"] = "external-form"
            }));

        var button = cut.Find("button");
        button.GetAttribute("type").ShouldBe("button");
        button.HasAttribute("form").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_HasDisabledWhenDisabled()
    {
        var cut = Render(CreateToggle(disabled: true));
        var button = cut.Find("button");
        button.HasAttribute("disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_DoesNotHaveAriaDisabled()
    {
        var cut = Render(CreateToggle(disabled: true));
        var button = cut.Find("button");
        button.HasAttribute("aria-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_DoesNotHaveRoleButton()
    {
        var cut = Render(CreateToggle());
        var button = cut.Find("button");
        button.HasAttribute("role").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // Non-native button attributes

    [Fact]
    public Task NonNativeButton_HasRoleButton()
    {
        var cut = Render(CreateToggle(nativeButton: false));
        var element = cut.Find("[role='button']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_DoesNotHaveType()
    {
        var cut = Render(CreateToggle(nativeButton: false));
        var element = cut.Find("button");
        element.HasAttribute("type").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_HasAriaDisabledWhenDisabled()
    {
        var cut = Render(CreateToggle(nativeButton: false, disabled: true));
        var element = cut.Find("button");
        element.GetAttribute("aria-disabled").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_HasTabIndexMinusOneWhenDisabled()
    {
        var cut = Render(CreateToggle(nativeButton: false, disabled: true));
        var element = cut.Find("button");
        element.GetAttribute("tabindex").ShouldBe("-1");
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataPressedWhenPressed()
    {
        var cut = Render(CreateToggle(pressed: true));
        var button = cut.Find("button");
        button.HasAttribute("data-pressed").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataPressedWhenNotPressed()
    {
        var cut = Render(CreateToggle());
        var button = cut.Find("button");
        button.HasAttribute("data-pressed").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateToggle(disabled: true));
        var button = cut.Find("button");
        button.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataDisabledWhenNotDisabled()
    {
        var cut = Render(CreateToggle());
        var button = cut.Find("button");
        button.HasAttribute("data-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // TabIndex

    [Fact]
    public Task NativeButton_HasDefaultTabIndex()
    {
        var cut = Render(CreateToggle());
        var button = cut.Find("button");
        button.GetAttribute("tabindex").ShouldBe("0");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_HasDefaultTabIndex()
    {
        var cut = Render(CreateToggle(nativeButton: false));
        var element = cut.Find("button");
        element.GetAttribute("tabindex").ShouldBe("0");
        return Task.CompletedTask;
    }

    // State cascading

    [Fact]
    public Task ClassValueReceivesToggleState()
    {
        ToggleState? capturedState = null;
        var cut = Render(CreateToggle(
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }));
        capturedState.ShouldNotBeNull();
        capturedState!.Pressed.ShouldBeFalse();
        capturedState!.Disabled.ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ClassValueReceivesPressedTrue()
    {
        ToggleState? capturedState = null;
        var cut = Render(CreateToggle(
            pressed: true,
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }));
        capturedState.ShouldNotBeNull();
        capturedState!.Pressed.ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ClassValueReceivesDisabledTrue()
    {
        ToggleState? capturedState = null;
        var cut = Render(CreateToggle(
            disabled: true,
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }));
        capturedState.ShouldNotBeNull();
        capturedState!.Disabled.ShouldBeTrue();
        return Task.CompletedTask;
    }

    // Uncontrolled state

    [Fact]
    public Task Uncontrolled_DefaultPressedTrue()
    {
        var cut = Render(CreateToggle(defaultPressed: true));
        var button = cut.Find("button");
        button.GetAttribute("aria-pressed").ShouldBe("true");
        button.HasAttribute("data-pressed").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task Uncontrolled_TogglesOnClick()
    {
        var cut = Render(CreateToggle());
        var button = cut.Find("button");

        button.GetAttribute("aria-pressed").ShouldBe("false");

        button.Click();

        button.GetAttribute("aria-pressed").ShouldBe("true");
        button.HasAttribute("data-pressed").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // Controlled state

    [Fact]
    public Task Controlled_ReflectsParentState()
    {
        // Pressed = true
        var cutTrue = Render(CreateToggle(pressed: true));
        var buttonTrue = cutTrue.Find("button");
        buttonTrue.GetAttribute("aria-pressed").ShouldBe("true");

        // Pressed = false
        var cutFalse = Render(CreateToggle(pressed: false));
        var buttonFalse = cutFalse.Find("button");
        buttonFalse.GetAttribute("aria-pressed").ShouldBe("false");

        return Task.CompletedTask;
    }

    // OnPressedChange

    [Fact]
    public Task OnPressedChange_FiresOnClick()
    {
        bool? receivedValue = null;
        var cut = Render(CreateToggle(
            onPressedChange: args => receivedValue = args.Pressed));

        var button = cut.Find("button");
        button.Click();

        receivedValue.ShouldNotBeNull();
        receivedValue.ShouldBe(true);
        return Task.CompletedTask;
    }

    [Fact]
    public Task GroupedToggle_OnPressedChangeFiresOnClick()
    {
        bool? receivedValue = null;
        var cut = Render(builder =>
        {
            builder.OpenComponent<BlazorBaseUI.ToggleGroup.ToggleGroup>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<BlazorBaseUI.Toggle.Toggle>(0);
                innerBuilder.AddAttribute(1, "Value", "one");
                innerBuilder.AddAttribute(2, "OnPressedChange",
                    EventCallback.Factory.Create<TogglePressedChangeEventArgs>(
                        this,
                        args => receivedValue = args.Pressed));
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "One")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var button = cut.Find("button[aria-pressed]");
        button.Click();

        receivedValue.ShouldBe(true);
        return Task.CompletedTask;
    }

    [Fact]
    public Task GroupedNativeToggle_ExposesAriaDisabledState()
    {
        var enabledCut = Render(builder =>
        {
            builder.OpenComponent<BlazorBaseUI.ToggleGroup.ToggleGroup>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<BlazorBaseUI.Toggle.Toggle>(0);
                innerBuilder.AddAttribute(1, "Value", "one");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "One")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        enabledCut.Find("button[aria-pressed]").GetAttribute("aria-disabled").ShouldBe("false");

        var disabledCut = Render(builder =>
        {
            builder.OpenComponent<BlazorBaseUI.ToggleGroup.ToggleGroup>(0);
            builder.AddAttribute(1, "Disabled", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<BlazorBaseUI.Toggle.Toggle>(0);
                innerBuilder.AddAttribute(1, "Value", "one");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "One")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        disabledCut.Find("button[aria-pressed]").GetAttribute("aria-disabled").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task GroupedEmptyStringValues_AreResolvedToUniqueGeneratedValues()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<BlazorBaseUI.ToggleGroup.ToggleGroup>(0);
            builder.AddAttribute(1, "Multiple", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<BlazorBaseUI.Toggle.Toggle>(0);
                innerBuilder.AddAttribute(1, "Value", "");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "First")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<BlazorBaseUI.Toggle.Toggle>(10);
                innerBuilder.AddAttribute(11, "Value", "");
                innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Second")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var toggles = cut.FindAll("button[aria-pressed]");
        toggles[0].Click();

        toggles = cut.FindAll("button[aria-pressed]");
        toggles[0].GetAttribute("aria-pressed").ShouldBe("true");
        toggles[1].GetAttribute("aria-pressed").ShouldBe("false");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Disabled_OnPressedChangeDoesNotFire()
    {
        var callCount = 0;
        var cut = Render(CreateToggle(
            disabled: true,
            onPressedChange: _ => callCount++));

        var button = cut.Find("button");
        button.Click();

        callCount.ShouldBe(0);
        return Task.CompletedTask;
    }

    // Element reference

    [Fact]
    public Task ExposesElementReference()
    {
        BlazorBaseUI.Toggle.Toggle? component = null;
        var cut = Render(builder =>
        {
            builder.OpenComponent<BlazorBaseUI.Toggle.Toggle>(0);
            builder.AddComponentReferenceCapture(1, obj => component = (BlazorBaseUI.Toggle.Toggle)obj);
            builder.CloseComponent();
        });
        component.ShouldNotBeNull();
        cut.WaitForState(() => component!.Element.HasValue);
        component!.Element.HasValue.ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ToolbarGroup_UnregistersFromRegisteredToolbarWhenCascadeClears()
    {
        var cut = Render<ToggleToolbarContextClearHost>();

        cut.WaitForAssertion(() => cut.Instance.RegisterCount.ShouldBe(1));

        await cut.InvokeAsync(cut.Instance.ClearToolbarContext);

        cut.WaitForAssertion(() => cut.Instance.UnregisterCount.ShouldBe(1));
    }
}

internal sealed class ToggleToolbarContextClearHost : ComponentBase
{
    private readonly ToggleGroupContext groupContext;
    private ToolbarRootContext? toolbarContext;

    public int RegisterCount { get; private set; }

    public int UnregisterCount { get; private set; }

    public ToggleToolbarContextClearHost()
    {
        groupContext = new ToggleGroupContext
        {
            Disabled = false,
            Orientation = Orientation.Horizontal,
            LoopFocus = true,
            Direction = Direction.Ltr,
            IsValueInitialized = true,
            IsInToolbar = true,
            GetValueFunc = Array.Empty<string>,
            SetGroupValueFunc = (_, _, eventArgs) =>
                Task.FromResult(new ToggleGroupValueChangeEventArgs(Array.Empty<string>(), eventArgs)),
            GetGroupElementFunc = () => null
        };

        toolbarContext = new ToolbarRootContext
        {
            Disabled = false,
            Orientation = Orientation.Horizontal,
            RegisterItem = _ => RegisterCount++,
            UnregisterItem = _ => UnregisterCount++
        };
    }

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<IToggleGroupContext>>(0);
        builder.AddAttribute(1, "Value", groupContext);
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(groupBuilder =>
        {
            groupBuilder.OpenComponent<CascadingValue<ToolbarRootContext?>>(0);
            groupBuilder.AddAttribute(1, "Value", toolbarContext);
            groupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(toolbarBuilder =>
            {
                toolbarBuilder.OpenComponent<BlazorBaseUI.Toggle.Toggle>(0);
                toolbarBuilder.AddAttribute(1, "Value", "one");
                toolbarBuilder.AddAttribute(
                    2,
                    "ChildContent",
                    (RenderFragment)(contentBuilder => contentBuilder.AddContent(0, "One")));
                toolbarBuilder.CloseComponent();
            }));
            groupBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }

    public void ClearToolbarContext()
    {
        toolbarContext = null;
        groupContext.IsInToolbar = false;
        StateHasChanged();
    }
}
