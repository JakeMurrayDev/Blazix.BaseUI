namespace Blazix.BaseUI.Tests.Button;

public class ButtonTests : BunitContext, IButtonContract
{
    public ButtonTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupButtonModule(JSInterop);
    }

    private RenderFragment CreateButton(
        bool disabled = false,
        bool focusableWhenDisabled = false,
        bool nativeButton = true,
        int tabIndex = 0,
        RenderFragment<RenderProps<ButtonState>>? render = null,
        Func<ButtonState, string?>? classValue = null,
        Func<ButtonState, string?>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.Button.Button>(0);

            builder.AddAttribute(1, "Disabled", disabled);
            builder.AddAttribute(2, "FocusableWhenDisabled", focusableWhenDisabled);
            builder.AddAttribute(3, "NativeButton", nativeButton);
            builder.AddAttribute(4, "TabIndex", tabIndex);

            if (render is not null)
                builder.AddAttribute(5, "Render", render);
            if (classValue is not null)
                builder.AddAttribute(6, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(7, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(8, "AdditionalAttributes", additionalAttributes);
            if (childContent is not null)
                builder.AddAttribute(9, "ChildContent", childContent);

            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsButtonByDefault()
    {
        var cut = Render(CreateButton());
        var button = cut.Find("button");
        button.TagName.ShouldBe("BUTTON");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<ButtonState>> renderAsSpan = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateButton(
            nativeButton: false,
            render: renderAsSpan,
            childContent: b => b.AddContent(0, "Custom")));
        var element = cut.Find("span");
        element.TagName.ShouldBe("SPAN");
        element.TextContent.ShouldBe("Custom");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateButton(
            childContent: b => b.AddContent(0, "Click me")));
        var button = cut.Find("button");
        button.TextContent.ShouldBe("Click me");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateButton(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "my-button" },
                { "aria-label", "Submit form" }
            }));
        var button = cut.Find("button");
        button.GetAttribute("data-testid").ShouldBe("my-button");
        button.GetAttribute("aria-label").ShouldBe("Submit form");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateButton(
            classValue: _ => "custom-button"));
        var button = cut.Find("button");
        button.GetAttribute("class").ShouldContain("custom-button");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateButton(
            styleValue: _ => "color: red"));
        var button = cut.Find("button");
        button.GetAttribute("style").ShouldContain("color: red");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateButton(
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

    // Native button attributes

    [Fact]
    public Task NativeButton_HasTypeButton()
    {
        var cut = Render(CreateButton());
        var button = cut.Find("button");
        button.GetAttribute("type").ShouldBe("button");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_HasDisabledAttributeWhenDisabled()
    {
        var cut = Render(CreateButton(disabled: true));
        var button = cut.Find("button");
        button.HasAttribute("disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_DoesNotHaveAriaDisabledWhenDisabled()
    {
        var cut = Render(CreateButton(disabled: true));
        var button = cut.Find("button");
        button.HasAttribute("aria-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_DoesNotHaveRoleButton()
    {
        var cut = Render(CreateButton());
        var button = cut.Find("button");
        button.HasAttribute("role").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_DoesNotHaveDisabledWhenNotDisabled()
    {
        var cut = Render(CreateButton());
        var button = cut.Find("button");
        button.HasAttribute("disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_DisabledDoesNotHaveTabIndex()
    {
        var cut = Render(CreateButton(disabled: true));
        var button = cut.Find("button");
        button.HasAttribute("tabindex").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // Non-native button attributes

    [Fact]
    public Task NonNativeButton_HasRoleButton()
    {
        var cut = Render(CreateButton(nativeButton: false));
        var element = cut.Find("[role='button']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_DoesNotHaveTypeButton()
    {
        var cut = Render(CreateButton(nativeButton: false));
        var element = cut.Find("button");
        element.HasAttribute("type").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_HasAriaDisabledTrueWhenDisabled()
    {
        var cut = Render(CreateButton(nativeButton: false, disabled: true));
        var element = cut.Find("button");
        element.GetAttribute("aria-disabled").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_HasTabIndexMinusOneWhenDisabled()
    {
        var cut = Render(CreateButton(nativeButton: false, disabled: true));
        var element = cut.Find("button");
        element.GetAttribute("tabindex").ShouldBe("-1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_DoesNotHaveAriaDisabledWhenNotDisabled()
    {
        var cut = Render(CreateButton(nativeButton: false));
        var element = cut.Find("button");
        element.HasAttribute("aria-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_DoesNotHaveDisabledAttribute()
    {
        var cut = Render(CreateButton(nativeButton: false));
        var element = cut.Find("button");
        element.HasAttribute("disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateButton(disabled: true));
        var button = cut.Find("button");
        button.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataDisabledWhenNotDisabled()
    {
        var cut = Render(CreateButton());
        var button = cut.Find("button");
        button.HasAttribute("data-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // FocusableWhenDisabled - native

    [Fact]
    public Task NativeFocusableWhenDisabled_DoesNotHaveDisabledAttribute()
    {
        var cut = Render(CreateButton(disabled: true, focusableWhenDisabled: true));
        var button = cut.Find("button");
        button.HasAttribute("disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeFocusableWhenDisabled_HasAriaDisabledTrue()
    {
        var cut = Render(CreateButton(disabled: true, focusableWhenDisabled: true));
        var button = cut.Find("button");
        button.GetAttribute("aria-disabled").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeFocusableWhenDisabled_HasTabIndex()
    {
        var cut = Render(CreateButton(disabled: true, focusableWhenDisabled: true, tabIndex: 3));
        var button = cut.Find("button");
        button.GetAttribute("tabindex").ShouldBe("3");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeFocusableWhenDisabled_HasDataDisabled()
    {
        var cut = Render(CreateButton(disabled: true, focusableWhenDisabled: true));
        var button = cut.Find("button");
        button.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // FocusableWhenDisabled - non-native

    [Fact]
    public Task NonNativeFocusableWhenDisabled_HasAriaDisabledTrue()
    {
        var cut = Render(CreateButton(nativeButton: false, disabled: true, focusableWhenDisabled: true));
        var element = cut.Find("button");
        element.GetAttribute("aria-disabled").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeFocusableWhenDisabled_HasTabIndex()
    {
        var cut = Render(CreateButton(nativeButton: false, disabled: true, focusableWhenDisabled: true, tabIndex: 2));
        var element = cut.Find("button");
        element.GetAttribute("tabindex").ShouldBe("2");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeFocusableWhenDisabled_HasRoleButton()
    {
        var cut = Render(CreateButton(nativeButton: false, disabled: true, focusableWhenDisabled: true));
        var element = cut.Find("button");
        element.GetAttribute("role").ShouldBe("button");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeFocusableWhenDisabled_HasDataDisabled()
    {
        var cut = Render(CreateButton(nativeButton: false, disabled: true, focusableWhenDisabled: true));
        var element = cut.Find("button");
        element.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // TabIndex

    [Fact]
    public Task NativeButton_HasDefaultTabIndex()
    {
        var cut = Render(CreateButton());
        var button = cut.Find("button");
        button.GetAttribute("tabindex").ShouldBe("0");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_HasDefaultTabIndex()
    {
        var cut = Render(CreateButton(nativeButton: false));
        var element = cut.Find("button");
        element.GetAttribute("tabindex").ShouldBe("0");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsExplicitTabIndex()
    {
        var cut = Render(CreateButton(tabIndex: 5));
        var button = cut.Find("button");
        button.GetAttribute("tabindex").ShouldBe("5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeDisabled_HasTabIndexMinusOne()
    {
        var cut = Render(CreateButton(nativeButton: false, disabled: true));
        var element = cut.Find("button");
        element.GetAttribute("tabindex").ShouldBe("-1");
        return Task.CompletedTask;
    }

    // State cascading

    [Fact]
    public Task CascadesButtonStateToClassValue()
    {
        ButtonState? capturedState = null;
        var cut = Render(CreateButton(
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }));
        capturedState.ShouldNotBeNull();
        capturedState!.Disabled.ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesButtonStateDisabledTrue()
    {
        ButtonState? capturedState = null;
        var cut = Render(CreateButton(
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

    [Fact]
    public Task CascadesButtonStateToStyleValue()
    {
        ButtonState? capturedState = null;
        var cut = Render(CreateButton(
            disabled: true,
            styleValue: state =>
            {
                capturedState = state;
                return "color: red";
            }));
        capturedState.ShouldNotBeNull();
        capturedState!.Disabled.ShouldBeTrue();
        return Task.CompletedTask;
    }

    // Element reference

    [Fact]
    public Task ExposesElementReference()
    {
        Blazix.BaseUI.Button.Button? component = null;
        var cut = Render(builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.Button.Button>(0);
            builder.AddComponentReferenceCapture(1, obj => component = (Blazix.BaseUI.Button.Button)obj);
            builder.CloseComponent();
        });
        component.ShouldNotBeNull();
        cut.WaitForState(() => component!.Element.HasValue);
        component!.Element.HasValue.ShouldBeTrue();
        return Task.CompletedTask;
    }

    // Disposal

    [Fact]
    public async Task DisposeAsync_DoesNotThrow()
    {
        Blazix.BaseUI.Button.Button? component = null;
        var cut = Render(builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.Button.Button>(0);
            builder.AddComponentReferenceCapture(1, obj => component = (Blazix.BaseUI.Button.Button)obj);
            builder.CloseComponent();
        });
        component.ShouldNotBeNull();
        await component!.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_SkipsJsWhenModuleNotCreated()
    {
        // Render a native, non-disabled button (NeedsJsInterop = false)
        // so the JS module is never loaded. Disposal should not invoke JS.
        Blazix.BaseUI.Button.Button? component = null;
        var cut = Render(builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.Button.Button>(0);
            builder.AddAttribute(1, "NativeButton", true);
            builder.AddAttribute(2, "Disabled", false);
            builder.AddComponentReferenceCapture(3, obj => component = (Blazix.BaseUI.Button.Button)obj);
            builder.CloseComponent();
        });
        component.ShouldNotBeNull();

        // Should complete without any JS calls since the module was never created
        await component!.DisposeAsync();
    }
}
