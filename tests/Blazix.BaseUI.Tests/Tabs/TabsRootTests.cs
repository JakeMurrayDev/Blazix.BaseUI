namespace Blazix.BaseUI.Tests.Tabs;

public class TabsRootTests : BunitContext, ITabsRootContract
{
    public TabsRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupTabsModule(JSInterop);
    }

    private RenderFragment CreateTabsRoot(
        string? defaultValue = null,
        string? value = null,
        Orientation orientation = Orientation.Horizontal,
        EventCallback<string?>? valueChanged = null,
        Action<TabsValueChangeEventArgs<string>>? onValueChange = null,
        RenderFragment<RenderProps<TabsRootState>>? render = null,
        Func<TabsRootState, string>? classValue = null,
        Func<TabsRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<TabsRoot<string>>(0);
            var seq = 1;
            if (defaultValue is not null)
                builder.AddAttribute(seq++, "DefaultValue", defaultValue);
            if (value is not null)
                builder.AddAttribute(seq++, "Value", value);
            builder.AddAttribute(seq++, "Orientation", orientation);
            if (valueChanged.HasValue)
                builder.AddAttribute(seq++, "ValueChanged", valueChanged.Value);
            if (onValueChange is not null)
                builder.AddAttribute(seq++, "OnValueChange", EventCallback.Factory.Create(this, onValueChange));
            if (render is not null)
                builder.AddAttribute(seq++, "Render", render);
            if (classValue is not null)
                builder.AddAttribute(seq++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(seq++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(seq++, "AdditionalAttributes", additionalAttributes);
            if (childContent is not null)
                builder.AddAttribute(seq++, "ChildContent", childContent);
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateFullTabs(
        string? defaultValue = "tab1",
        string? value = null,
        Orientation orientation = Orientation.Horizontal,
        bool tab1Disabled = false,
        bool tab2Disabled = false,
        bool tab3Disabled = false,
        bool keepMounted = false,
        EventCallback<string?>? valueChanged = null,
        Action<TabsValueChangeEventArgs<string>>? onValueChange = null)
    {
        return CreateTabsRoot(
            defaultValue: defaultValue,
            value: value,
            orientation: orientation,
            valueChanged: valueChanged,
            onValueChange: onValueChange,
            childContent: listBuilder =>
            {
                // TabsList
                listBuilder.OpenComponent<TabsList<string>>(0);
                listBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(tabBuilder =>
                {
                    tabBuilder.OpenComponent<TabsTab<string>>(0);
                    tabBuilder.AddAttribute(1, "Value", "tab1");
                    tabBuilder.AddAttribute(2, "Disabled", tab1Disabled);
                    tabBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Tab 1")));
                    tabBuilder.CloseComponent();

                    tabBuilder.OpenComponent<TabsTab<string>>(10);
                    tabBuilder.AddAttribute(11, "Value", "tab2");
                    tabBuilder.AddAttribute(12, "Disabled", tab2Disabled);
                    tabBuilder.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Tab 2")));
                    tabBuilder.CloseComponent();

                    tabBuilder.OpenComponent<TabsTab<string>>(20);
                    tabBuilder.AddAttribute(21, "Value", "tab3");
                    tabBuilder.AddAttribute(22, "Disabled", tab3Disabled);
                    tabBuilder.AddAttribute(23, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Tab 3")));
                    tabBuilder.CloseComponent();
                }));
                listBuilder.CloseComponent();

                // Panels
                listBuilder.OpenComponent<TabsPanel<string>>(100);
                listBuilder.AddAttribute(101, "Value", "tab1");
                listBuilder.AddAttribute(102, "KeepMounted", keepMounted);
                listBuilder.AddAttribute(103, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Panel 1 content")));
                listBuilder.CloseComponent();

                listBuilder.OpenComponent<TabsPanel<string>>(110);
                listBuilder.AddAttribute(111, "Value", "tab2");
                listBuilder.AddAttribute(112, "KeepMounted", keepMounted);
                listBuilder.AddAttribute(113, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Panel 2 content")));
                listBuilder.CloseComponent();

                listBuilder.OpenComponent<TabsPanel<string>>(120);
                listBuilder.AddAttribute(121, "Value", "tab3");
                listBuilder.AddAttribute(122, "KeepMounted", keepMounted);
                listBuilder.AddAttribute(123, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Panel 3 content")));
                listBuilder.CloseComponent();
            });
    }

    // Rendering

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateTabsRoot());
        var element = cut.Find("div");
        element.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateTabsRoot(
            render: ctx => builder =>
            {
                builder.OpenElement(0, "section");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }));
        var element = cut.Find("section");
        element.TagName.ShouldBe("SECTION");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateTabsRoot(
            childContent: b => b.AddContent(0, "Hello Tabs")));
        cut.Markup.ShouldContain("Hello Tabs");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateTabsRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-custom", "value" },
                { "aria-label", "My Tabs" }
            }));
        var element = cut.Find("div");
        element.GetAttribute("data-custom").ShouldBe("value");
        element.GetAttribute("aria-label").ShouldBe("My Tabs");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateTabsRoot(classValue: _ => "custom-class"));
        var element = cut.Find("div");
        element.GetAttribute("class").ShouldContain("custom-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateTabsRoot(styleValue: _ => "gap: 8px"));
        var element = cut.Find("div");
        element.GetAttribute("style").ShouldContain("gap: 8px");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateTabsRoot(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }));
        var element = cut.Find("div");
        var classAttr = element.GetAttribute("class");
        classAttr.ShouldContain("static-class");
        classAttr.ShouldContain("dynamic-class");
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataActivationDirectionNoneByDefault()
    {
        var cut = Render(CreateTabsRoot());
        var element = cut.Find("div");
        element.GetAttribute("data-activation-direction").ShouldBe("none");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientationHorizontalByDefault()
    {
        var cut = Render(CreateTabsRoot());
        var element = cut.Find("div");
        element.GetAttribute("data-orientation").ShouldBe("horizontal");
        return Task.CompletedTask;
    }

    // State

    [Fact]
    public Task ClassValueReceivesTabsRootState()
    {
        TabsRootState? capturedState = null;
        var cut = Render(CreateTabsRoot(
            classValue: state =>
            {
                capturedState = state;
                return "test";
            }));

        capturedState.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ClassValueReceivesOrientationVertical()
    {
        TabsRootState? capturedState = null;
        var cut = Render(CreateTabsRoot(
            orientation: Orientation.Vertical,
            classValue: state =>
            {
                capturedState = state;
                return "test";
            }));

        capturedState.ShouldNotBeNull();
        capturedState!.Orientation.ShouldBe(Orientation.Vertical);
        return Task.CompletedTask;
    }

    // Element reference

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreateTabsRoot());
        var component = cut.FindComponent<TabsRoot<string>>();
        component.Instance.Element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // Validation

    [Fact]
    public Task AcceptsNullChildren()
    {
        var cut = Render(CreateTabsRoot(childContent: null));
        cut.Markup.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // Value management

    [Fact]
    public Task SetsDefaultValueOnInit()
    {
        var cut = Render(CreateFullTabs(defaultValue: "tab2"));
        var tabs = cut.FindAll("[role='tab']");

        tabs[0].GetAttribute("aria-selected").ShouldBe("false");
        tabs[1].GetAttribute("aria-selected").ShouldBe("true");
        tabs[2].GetAttribute("aria-selected").ShouldBe("false");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsControlledValue()
    {
        var currentValue = "tab2";
        var cut = Render(CreateFullTabs(
            defaultValue: null,
            value: currentValue,
            valueChanged: EventCallback.Factory.Create<string?>(this, v => currentValue = v)));

        var tabs = cut.FindAll("[role='tab']");
        tabs[1].GetAttribute("aria-selected").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task FiresValueChangedCallback()
    {
        string? receivedValue = null;
        var cut = Render(CreateFullTabs(
            defaultValue: "tab1",
            valueChanged: EventCallback.Factory.Create<string?>(this, v => receivedValue = v)));

        var tabs = cut.FindAll("[role='tab']");
        tabs[1].Click();

        receivedValue.ShouldBe("tab2");
        return Task.CompletedTask;
    }

    [Fact]
    public Task FiresOnValueChangeWithCancellation()
    {
        TabsValueChangeEventArgs<string>? receivedArgs = null;
        // Use defaultValue: null so no tab is initially selected.
        // This avoids DetectActivationDirectionAsync calling getTabPosition JS
        // (which returns null in bUnit since TabPositionResult is private).
        var cut = Render(CreateFullTabs(
            defaultValue: null,
            onValueChange: args =>
            {
                receivedArgs = args;
                args.Cancel();
            }));

        var tabs = cut.FindAll("[role='tab']");
        tabs[1].Click();

        receivedArgs.ShouldNotBeNull();
        receivedArgs!.Value.ShouldBe("tab2");
        receivedArgs.IsCanceled.ShouldBeTrue();

        // Value should not have changed - tab1 was auto-selected and should remain
        tabs = cut.FindAll("[role='tab']");
        tabs[0].GetAttribute("aria-selected").ShouldBe("true");
        tabs[1].GetAttribute("aria-selected").ShouldBe("false");
        tabs[2].GetAttribute("aria-selected").ShouldBe("false");
        return Task.CompletedTask;
    }

    [Fact]
    public Task FiresOnValueChangeWithInitialReasonForImplicitAutoSelection()
    {
        TabsValueChangeEventArgs<string>? receivedArgs = null;
        var cut = Render(CreateFullTabs(
            defaultValue: null,
            onValueChange: args => receivedArgs = args));

        cut.WaitForAssertion(() =>
        {
            receivedArgs.ShouldNotBeNull();
            receivedArgs!.Value.ShouldBe("tab1");
            receivedArgs.Reason.ShouldBe(TabsValueChangeReason.Initial);
            receivedArgs.ActivationDirection.ShouldBe(ActivationDirection.None);
        });

        var tabs = cut.FindAll("[role='tab']");
        tabs[0].GetAttribute("aria-selected").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AutomaticInitialSelectionCannotBeCanceled()
    {
        TabsValueChangeEventArgs<string>? receivedArgs = null;
        var cut = Render(CreateFullTabs(
            defaultValue: null,
            tab1Disabled: true,
            onValueChange: args =>
            {
                receivedArgs = args;
                args.Cancel();
            }));

        cut.WaitForAssertion(() =>
        {
            receivedArgs.ShouldNotBeNull();
            receivedArgs!.Value.ShouldBe("tab2");
            receivedArgs.Reason.ShouldBe(TabsValueChangeReason.Initial);
        });

        var tabs = cut.FindAll("[role='tab']");
        tabs[0].GetAttribute("aria-selected").ShouldBe("false");
        tabs[1].GetAttribute("aria-selected").ShouldBe("true");
        tabs[2].GetAttribute("aria-selected").ShouldBe("false");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ExplicitNullDefaultValueDoesNotAutoSelect()
    {
        TabsValueChangeEventArgs<string>? receivedArgs = null;
        var cut = Render(builder =>
        {
            builder.OpenComponent<TabsRoot<string>>(0);
            builder.AddAttribute(1, "DefaultValue", (string?)null);
            builder.AddAttribute(2, "OnValueChange", EventCallback.Factory.Create<TabsValueChangeEventArgs<string>>(this, args => receivedArgs = args));
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(rootBuilder =>
            {
                rootBuilder.OpenComponent<TabsList<string>>(0);
                rootBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(tabBuilder =>
                {
                    tabBuilder.OpenComponent<TabsTab<string>>(0);
                    tabBuilder.AddAttribute(1, "Value", "tab1");
                    tabBuilder.CloseComponent();

                    tabBuilder.OpenComponent<TabsTab<string>>(10);
                    tabBuilder.AddAttribute(11, "Value", "tab2");
                    tabBuilder.CloseComponent();
                }));
                rootBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        receivedArgs.ShouldBeNull();
        var tabs = cut.FindAll("[role='tab']");
        tabs[0].GetAttribute("aria-selected").ShouldBe("false");
        tabs[1].GetAttribute("aria-selected").ShouldBe("false");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ValueParameterControlsSelectionWithoutValueChanged()
    {
        var cut = Render(CreateFullTabs(
            defaultValue: null,
            value: "tab1",
            tab1Disabled: true));

        var tabs = cut.FindAll("[role='tab']");
        tabs[0].GetAttribute("aria-selected").ShouldBe("true");
        tabs[1].GetAttribute("aria-selected").ShouldBe("false");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SelectedTabDisabledAfterInitialValidityFallsBackWithDisabledReason()
    {
        var disableFirst = false;
        TabsValueChangeEventArgs<string>? receivedArgs = null;

        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<TabsRoot<string>>(0);
            builder.AddAttribute(1, "DefaultValue", "tab1");
            builder.AddAttribute(2, "OnValueChange", EventCallback.Factory.Create<TabsValueChangeEventArgs<string>>(this, args => receivedArgs = args));
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(rootBuilder =>
            {
                rootBuilder.OpenComponent<TabsList<string>>(0);
                rootBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(tabBuilder =>
                {
                    tabBuilder.OpenComponent<TabsTab<string>>(0);
                    tabBuilder.AddAttribute(1, "Value", "tab1");
                    tabBuilder.AddAttribute(2, "Disabled", disableFirst);
                    tabBuilder.CloseComponent();

                    tabBuilder.OpenComponent<TabsTab<string>>(10);
                    tabBuilder.AddAttribute(11, "Value", "tab2");
                    tabBuilder.CloseComponent();
                }));
                rootBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(fragment);
        receivedArgs.ShouldBeNull();

        disableFirst = true;
        cut.FindComponent<TabsRoot<string>>().Render();

        cut.WaitForAssertion(() =>
        {
            receivedArgs.ShouldNotBeNull();
            receivedArgs!.Value.ShouldBe("tab2");
            receivedArgs.Reason.ShouldBe(TabsValueChangeReason.Disabled);
            receivedArgs.ActivationDirection.ShouldBe(ActivationDirection.None);

            var tabs = cut.FindAll("[role='tab']");
            tabs[0].GetAttribute("aria-selected").ShouldBe("false");
            tabs[1].GetAttribute("aria-selected").ShouldBe("true");
        });

        return Task.CompletedTask;
    }

    // Composite tests

    [Fact]
    public Task SetsAriaControlsOnTabsToCorrespondingPanelId()
    {
        var cut = Render(CreateFullTabs(defaultValue: "tab1", keepMounted: true));
        var tabs = cut.FindAll("[role='tab']");
        var panels = cut.FindAll("[role='tabpanel']");

        for (var i = 0; i < tabs.Count; i++)
        {
            var ariaControls = tabs[i].GetAttribute("aria-controls");
            var panelId = panels[i].GetAttribute("id");
            ariaControls.ShouldBe(panelId);
        }
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaLabelledbyOnPanelsToCorrespondingTabId()
    {
        var cut = Render(CreateFullTabs(defaultValue: "tab1", keepMounted: true));

        // Tab IDs are registered in OnAfterRenderAsync, so panels don't have
        // aria-labelledby on first render. Force a re-render to pick up the IDs.
        cut.FindComponent<TabsRoot<string>>().Render();

        var tabs = cut.FindAll("[role='tab']");
        var panels = cut.FindAll("[role='tabpanel']");

        for (var i = 0; i < tabs.Count; i++)
        {
            var tabId = tabs[i].GetAttribute("id");
            var ariaLabelledby = panels[i].GetAttribute("aria-labelledby");
            ariaLabelledby.ShouldBe(tabId);
        }
        return Task.CompletedTask;
    }

    [Fact]
    public Task PutsSelectedChildInTabOrder()
    {
        var cut = Render(CreateFullTabs(defaultValue: "tab2"));
        var tabs = cut.FindAll("[role='tab']");

        tabs[0].GetAttribute("tabindex").ShouldBe("-1");
        tabs[1].GetAttribute("tabindex").ShouldBe("0");
        tabs[2].GetAttribute("tabindex").ShouldBe("-1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ShowsOnlyActivePanelContent()
    {
        var cut = Render(CreateFullTabs(defaultValue: "tab1"));
        cut.Markup.ShouldContain("Panel 1 content");
        cut.Markup.ShouldNotContain("Panel 2 content");
        cut.Markup.ShouldNotContain("Panel 3 content");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledTabAutoSelectsFirstEnabled()
    {
        var cut = Render(CreateFullTabs(defaultValue: null, tab1Disabled: true));
        var tabs = cut.FindAll("[role='tab']");

        // No default value and first tab disabled - auto-selects first enabled (tab2)
        tabs[0].GetAttribute("aria-selected").ShouldBe("false");
        tabs[1].GetAttribute("aria-selected").ShouldBe("true");
        tabs[2].GetAttribute("aria-selected").ShouldBe("false");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledTabAutoSelectsThirdWhenFirstTwoDisabled()
    {
        var cut = Render(CreateFullTabs(defaultValue: null, tab1Disabled: true, tab2Disabled: true));
        var tabs = cut.FindAll("[role='tab']");

        // No default value, first two disabled - auto-selects first enabled (tab3)
        tabs[0].GetAttribute("aria-selected").ShouldBe("false");
        tabs[1].GetAttribute("aria-selected").ShouldBe("false");
        tabs[2].GetAttribute("aria-selected").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HonorsExplicitDefaultValueOnDisabledTab()
    {
        var cut = Render(CreateFullTabs(defaultValue: "tab1", tab1Disabled: true));
        var tabs = cut.FindAll("[role='tab']");

        // Explicit defaultValue should be honored even if disabled
        tabs[0].GetAttribute("aria-selected").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HonorsExplicitValueOnDisabledTab()
    {
        var currentValue = "tab1";
        var cut = Render(CreateFullTabs(
            defaultValue: null,
            value: currentValue,
            tab1Disabled: true,
            valueChanged: EventCallback.Factory.Create<string?>(this, v => currentValue = v)));

        var tabs = cut.FindAll("[role='tab']");
        tabs[0].GetAttribute("aria-selected").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NoTabSelectedWhenAllDisabled()
    {
        var cut = Render(CreateFullTabs(
            defaultValue: null,
            tab1Disabled: true,
            tab2Disabled: true,
            tab3Disabled: true));

        var tabs = cut.FindAll("[role='tab']");
        tabs[0].GetAttribute("aria-selected").ShouldBe("false");
        tabs[1].GetAttribute("aria-selected").ShouldBe("false");
        tabs[2].GetAttribute("aria-selected").ShouldBe("false");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SyncsAriaControlsWithKeepMountedFalse()
    {
        var cut = Render(CreateFullTabs(defaultValue: "tab1", keepMounted: false));
        var activeTab = cut.Find("[role='tab'][aria-selected='true']");
        var ariaControls = activeTab.GetAttribute("aria-controls");

        // The active panel should be in the DOM
        var panel = cut.Find("[role='tabpanel']");
        panel.GetAttribute("id").ShouldBe(ariaControls);
        return Task.CompletedTask;
    }
}
