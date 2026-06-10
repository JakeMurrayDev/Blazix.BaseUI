namespace Blazix.BaseUI.Tests.Tabs;

public class TabsTabTests : BunitContext, ITabsTabContract
{
    public TabsTabTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupTabsModule(JSInterop);
    }

    private RenderFragment CreateTabInRoot(
        string tabValue = "tab1",
        string? defaultValue = "tab1",
        bool disabled = false,
        bool nativeButton = true,
        Orientation orientation = Orientation.Horizontal,
        RenderFragment<RenderProps<TabsTabState>>? render = null,
        Func<TabsTabState, string>? classValue = null,
        Func<TabsTabState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment? childContent = null,
        bool includePanel = false,
        bool keepMounted = false)
    {
        return builder =>
        {
            builder.OpenComponent<TabsRoot<string>>(0);
            if (defaultValue is not null)
                builder.AddAttribute(1, "DefaultValue", defaultValue);
            builder.AddAttribute(2, "Orientation", orientation);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(rootInner =>
            {
                rootInner.OpenComponent<TabsList<string>>(0);
                rootInner.AddAttribute(1, "ChildContent", (RenderFragment)(listInner =>
                {
                    listInner.OpenComponent<TabsTab<string>>(0);
                    var seq = 1;
                    listInner.AddAttribute(seq++, "Value", tabValue);
                    listInner.AddAttribute(seq++, "Disabled", disabled);
                    listInner.AddAttribute(seq++, "NativeButton", nativeButton);
                    if (render is not null)
                        listInner.AddAttribute(seq++, "Render", render);
                    if (classValue is not null)
                        listInner.AddAttribute(seq++, "ClassValue", classValue);
                    if (styleValue is not null)
                        listInner.AddAttribute(seq++, "StyleValue", styleValue);
                    if (additionalAttributes is not null)
                        listInner.AddAttribute(seq++, "AdditionalAttributes", additionalAttributes);
                    if (childContent is not null)
                        listInner.AddAttribute(seq++, "ChildContent", childContent);
                    listInner.CloseComponent();
                }));
                rootInner.CloseComponent();

                if (includePanel)
                {
                    rootInner.OpenComponent<TabsPanel<string>>(100);
                    rootInner.AddAttribute(101, "Value", tabValue);
                    rootInner.AddAttribute(102, "KeepMounted", keepMounted);
                    rootInner.AddAttribute(103, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Panel content")));
                    rootInner.CloseComponent();
                }
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateMultipleTabsInRoot(
        string? defaultValue = "tab1",
        bool tab1Disabled = false,
        bool tab2Disabled = false,
        string? value = null,
        EventCallback<string?>? valueChanged = null,
        Action<TabsValueChangeEventArgs<string>>? onValueChange = null,
        Orientation orientation = Orientation.Horizontal,
        bool keepMounted = true)
    {
        return builder =>
        {
            builder.OpenComponent<TabsRoot<string>>(0);
            var seq = 1;
            if (defaultValue is not null)
                builder.AddAttribute(seq++, "DefaultValue", defaultValue);
            if (value is not null)
                builder.AddAttribute(seq++, "Value", value);
            if (valueChanged.HasValue)
                builder.AddAttribute(seq++, "ValueChanged", valueChanged.Value);
            if (onValueChange is not null)
                builder.AddAttribute(seq++, "OnValueChange", EventCallback.Factory.Create(this, onValueChange));
            builder.AddAttribute(seq++, "Orientation", orientation);
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(rootInner =>
            {
                rootInner.OpenComponent<TabsList<string>>(0);
                rootInner.AddAttribute(1, "ChildContent", (RenderFragment)(listInner =>
                {
                    listInner.OpenComponent<TabsTab<string>>(0);
                    listInner.AddAttribute(1, "Value", "tab1");
                    listInner.AddAttribute(2, "Disabled", tab1Disabled);
                    listInner.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Tab 1")));
                    listInner.CloseComponent();

                    listInner.OpenComponent<TabsTab<string>>(10);
                    listInner.AddAttribute(11, "Value", "tab2");
                    listInner.AddAttribute(12, "Disabled", tab2Disabled);
                    listInner.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Tab 2")));
                    listInner.CloseComponent();
                }));
                rootInner.CloseComponent();

                rootInner.OpenComponent<TabsPanel<string>>(100);
                rootInner.AddAttribute(101, "Value", "tab1");
                rootInner.AddAttribute(102, "KeepMounted", keepMounted);
                rootInner.AddAttribute(103, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Panel 1")));
                rootInner.CloseComponent();

                rootInner.OpenComponent<TabsPanel<string>>(110);
                rootInner.AddAttribute(111, "Value", "tab2");
                rootInner.AddAttribute(112, "KeepMounted", keepMounted);
                rootInner.AddAttribute(113, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Panel 2")));
                rootInner.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsButtonByDefault()
    {
        var cut = Render(CreateTabInRoot());
        var element = cut.Find("[role='tab']");
        element.TagName.ShouldBe("BUTTON");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateTabInRoot(
            render: ctx => builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }));
        var element = cut.Find("[role='tab']");
        element.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateTabInRoot(
            childContent: b => b.AddContent(0, "My Tab")));
        cut.Markup.ShouldContain("My Tab");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateTabInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-custom", "value" },
                { "aria-label", "My Tab" }
            }));
        var element = cut.Find("[role='tab']");
        element.GetAttribute("data-custom").ShouldBe("value");
        element.GetAttribute("aria-label").ShouldBe("My Tab");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateTabInRoot(classValue: _ => "custom-class"));
        var element = cut.Find("[role='tab']");
        element.GetAttribute("class").ShouldContain("custom-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateTabInRoot(styleValue: _ => "color: red"));
        var element = cut.Find("[role='tab']");
        element.GetAttribute("style").ShouldContain("color: red");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateTabInRoot(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }));
        var element = cut.Find("[role='tab']");
        var classAttr = element.GetAttribute("class");
        classAttr.ShouldContain("static-class");
        classAttr.ShouldContain("dynamic-class");
        return Task.CompletedTask;
    }

    // ARIA

    [Fact]
    public Task HasRoleTab()
    {
        var cut = Render(CreateTabInRoot());
        var element = cut.Find("[role='tab']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTypeButton()
    {
        var cut = Render(CreateTabInRoot(nativeButton: true));
        var element = cut.Find("[role='tab']");
        element.GetAttribute("type").ShouldBe("button");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaSelectedTrueWhenActive()
    {
        var cut = Render(CreateTabInRoot(tabValue: "tab1", defaultValue: "tab1"));
        var element = cut.Find("[role='tab']");
        element.GetAttribute("aria-selected").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaSelectedFalseWhenInactive()
    {
        var cut = Render(CreateTabInRoot(tabValue: "tab1", defaultValue: "tab2"));
        var element = cut.Find("[role='tab']");
        element.GetAttribute("aria-selected").ShouldBe("false");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabIndex0WhenActive()
    {
        var cut = Render(CreateTabInRoot(tabValue: "tab1", defaultValue: "tab1"));
        var element = cut.Find("[role='tab']");
        element.GetAttribute("tabindex").ShouldBe("0");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabIndexMinus1WhenInactive()
    {
        var cut = Render(CreateMultipleTabsInRoot(defaultValue: "tab2"));
        var tabs = cut.FindAll("[role='tab']");
        tabs[0].GetAttribute("tabindex").ShouldBe("-1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaControlsPointingToPanel()
    {
        var cut = Render(CreateTabInRoot(tabValue: "tab1", defaultValue: "tab1", includePanel: true, keepMounted: true));
        var tab = cut.Find("[role='tab']");
        var panel = cut.Find("[role='tabpanel']");
        tab.GetAttribute("aria-controls").ShouldBe(panel.GetAttribute("id"));
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataOrientationHorizontal()
    {
        var cut = Render(CreateTabInRoot(orientation: Orientation.Horizontal));
        var element = cut.Find("[role='tab']");
        element.GetAttribute("data-orientation").ShouldBe("horizontal");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataActiveWhenActive()
    {
        var cut = Render(CreateTabInRoot(tabValue: "tab1", defaultValue: "tab1"));
        var element = cut.Find("[role='tab']");
        element.HasAttribute("data-active").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataActiveWhenInactive()
    {
        var cut = Render(CreateTabInRoot(tabValue: "tab1", defaultValue: "tab2"));
        var element = cut.Find("[role='tab']");
        element.HasAttribute("data-active").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateTabInRoot(disabled: true));
        var element = cut.Find("[role='tab']");
        element.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataDisabledWhenNotDisabled()
    {
        var cut = Render(CreateTabInRoot(disabled: false));
        var element = cut.Find("[role='tab']");
        element.HasAttribute("data-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // Disabled behavior

    [Fact]
    public Task HasAriaDisabledWhenDisabled()
    {
        var cut = Render(CreateTabInRoot(disabled: true));
        var element = cut.Find("[role='tab']");
        element.GetAttribute("aria-disabled").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledActiveTabDoesNotEnterTabOrder()
    {
        var cut = Render(CreateMultipleTabsInRoot(
            defaultValue: null,
            value: "tab1",
            tab1Disabled: true));

        var tabs = cut.FindAll("[role='tab']");
        tabs[0].GetAttribute("aria-selected").ShouldBe("true");
        tabs[0].GetAttribute("tabindex").ShouldBe("-1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RightClickDoesNotActivateTab()
    {
        TabsValueChangeEventArgs<string>? receivedArgs = null;
        var cut = Render(CreateMultipleTabsInRoot(
            defaultValue: "tab1",
            onValueChange: args => receivedArgs = args));

        var tabs = cut.FindAll("[role='tab']");
        tabs[1].Click(new MouseEventArgs { Button = 2 });

        receivedArgs.ShouldBeNull();
        tabs = cut.FindAll("[role='tab']");
        tabs[0].GetAttribute("aria-selected").ShouldBe("true");
        tabs[1].GetAttribute("aria-selected").ShouldBe("false");
        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesClickHandlerWhenTabIsAlreadyActive()
    {
        var clicked = false;
        var cut = Render(CreateTabInRoot(
            tabValue: "tab1",
            defaultValue: "tab1",
            additionalAttributes: new Dictionary<string, object>
            {
                { "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked = true) }
            }));

        cut.Find("[role='tab']").Click();

        clicked.ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesFocusHandlerWhenTabIsDisabled()
    {
        var focused = false;
        var cut = Render(CreateTabInRoot(
            disabled: true,
            additionalAttributes: new Dictionary<string, object>
            {
                { "onfocus", EventCallback.Factory.Create<FocusEventArgs>(this, () => focused = true) }
            }));

        cut.Find("[role='tab']").Focus();

        focused.ShouldBeTrue();
        return Task.CompletedTask;
    }

    // State

    [Fact]
    public Task ClassValueReceivesTabsTabState()
    {
        TabsTabState? capturedState = null;
        var cut = Render(CreateTabInRoot(
            classValue: state =>
            {
                capturedState = state;
                return "test";
            }));

        capturedState.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ClassValueReceivesActiveTrue()
    {
        TabsTabState? capturedState = null;
        var cut = Render(CreateTabInRoot(
            tabValue: "tab1",
            defaultValue: "tab1",
            classValue: state =>
            {
                capturedState = state;
                return "test";
            }));

        capturedState.ShouldNotBeNull();
        capturedState!.Active.ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ClassValueReceivesDisabledTrue()
    {
        TabsTabState? capturedState = null;
        var cut = Render(CreateTabInRoot(
            disabled: true,
            classValue: state =>
            {
                capturedState = state;
                return "test";
            }));

        capturedState.ShouldNotBeNull();
        capturedState!.Disabled.ShouldBeTrue();
        return Task.CompletedTask;
    }

    // Element reference

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreateTabInRoot());
        var component = cut.FindComponent<TabsTab<string>>();
        component.Instance.Element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // Validation

    [Fact]
    public Task ThrowsWhenNotInTabsRoot()
    {
        Should.Throw<InvalidOperationException>(() =>
        {
            Render(builder =>
            {
                builder.OpenComponent<TabsTab<string>>(0);
                builder.AddAttribute(1, "Value", "tab1");
                builder.CloseComponent();
            });
        });

        return Task.CompletedTask;
    }
}
