namespace BlazorBaseUI.Tests.Tabs;

public class TabsPanelTests : BunitContext, ITabsPanelContract
{
    private readonly BunitJSModuleInterop module;

    public TabsPanelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        module = JsInteropSetup.SetupTabsModule(JSInterop);
    }

    private RenderFragment CreatePanelInRoot(
        string panelValue = "tab1",
        string? defaultValue = "tab1",
        bool keepMounted = false,
        Orientation orientation = Orientation.Horizontal,
        RenderFragment<RenderProps<TabsPanelState>>? render = null,
        Func<TabsPanelState, string>? classValue = null,
        Func<TabsPanelState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment? childContent = null,
        bool includeTab = true)
    {
        return builder =>
        {
            builder.OpenComponent<TabsRoot<string>>(0);
            if (defaultValue is not null)
                builder.AddAttribute(1, "DefaultValue", defaultValue);
            builder.AddAttribute(2, "Orientation", orientation);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(rootInner =>
            {
                if (includeTab)
                {
                    rootInner.OpenComponent<TabsList<string>>(0);
                    rootInner.AddAttribute(1, "ChildContent", (RenderFragment)(listInner =>
                    {
                        listInner.OpenComponent<TabsTab<string>>(0);
                        listInner.AddAttribute(1, "Value", panelValue);
                        listInner.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Tab")));
                        listInner.CloseComponent();
                    }));
                    rootInner.CloseComponent();
                }

                rootInner.OpenComponent<TabsPanel<string>>(100);
                var seq = 101;
                rootInner.AddAttribute(seq++, "Value", panelValue);
                rootInner.AddAttribute(seq++, "KeepMounted", keepMounted);
                if (render is not null)
                    rootInner.AddAttribute(seq++, "Render", render);
                if (classValue is not null)
                    rootInner.AddAttribute(seq++, "ClassValue", classValue);
                if (styleValue is not null)
                    rootInner.AddAttribute(seq++, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    rootInner.AddAttribute(seq++, "AdditionalAttributes", additionalAttributes);
                if (childContent is not null)
                    rootInner.AddAttribute(seq++, "ChildContent", childContent);
                rootInner.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateMultiplePanelsInRoot(
        string? defaultValue = "tab1",
        bool keepMounted = false)
    {
        return builder =>
        {
            builder.OpenComponent<TabsRoot<string>>(0);
            if (defaultValue is not null)
                builder.AddAttribute(1, "DefaultValue", defaultValue);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(rootInner =>
            {
                rootInner.OpenComponent<TabsList<string>>(0);
                rootInner.AddAttribute(1, "ChildContent", (RenderFragment)(listInner =>
                {
                    listInner.OpenComponent<TabsTab<string>>(0);
                    listInner.AddAttribute(1, "Value", "tab1");
                    listInner.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Tab 1")));
                    listInner.CloseComponent();

                    listInner.OpenComponent<TabsTab<string>>(10);
                    listInner.AddAttribute(11, "Value", "tab2");
                    listInner.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Tab 2")));
                    listInner.CloseComponent();
                }));
                rootInner.CloseComponent();

                rootInner.OpenComponent<TabsPanel<string>>(100);
                rootInner.AddAttribute(101, "Value", "tab1");
                rootInner.AddAttribute(102, "KeepMounted", keepMounted);
                rootInner.AddAttribute(103, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Panel 1 content")));
                rootInner.CloseComponent();

                rootInner.OpenComponent<TabsPanel<string>>(110);
                rootInner.AddAttribute(111, "Value", "tab2");
                rootInner.AddAttribute(112, "KeepMounted", keepMounted);
                rootInner.AddAttribute(113, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Panel 2 content")));
                rootInner.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreatePanelInRoot());
        var element = cut.Find("[role='tabpanel']");
        element.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreatePanelInRoot(
            render: ctx => builder =>
            {
                builder.OpenElement(0, "section");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }));
        var element = cut.Find("[role='tabpanel']");
        element.TagName.ShouldBe("SECTION");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreatePanelInRoot(
            childContent: b => b.AddContent(0, "Panel Text")));
        cut.Markup.ShouldContain("Panel Text");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreatePanelInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-custom", "value" },
                { "aria-label", "My Panel" }
            }));
        var element = cut.Find("[role='tabpanel']");
        element.GetAttribute("data-custom").ShouldBe("value");
        element.GetAttribute("aria-label").ShouldBe("My Panel");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreatePanelInRoot(classValue: _ => "custom-class"));
        var element = cut.Find("[role='tabpanel']");
        element.GetAttribute("class").ShouldContain("custom-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreatePanelInRoot(styleValue: _ => "padding: 16px"));
        var element = cut.Find("[role='tabpanel']");
        element.GetAttribute("style").ShouldContain("padding: 16px");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreatePanelInRoot(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }));
        var element = cut.Find("[role='tabpanel']");
        var classAttr = element.GetAttribute("class");
        classAttr.ShouldContain("static-class");
        classAttr.ShouldContain("dynamic-class");
        return Task.CompletedTask;
    }

    // ARIA

    [Fact]
    public Task HasRoleTabpanel()
    {
        var cut = Render(CreatePanelInRoot());
        var element = cut.Find("[role='tabpanel']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabIndex0WhenVisible()
    {
        var cut = Render(CreatePanelInRoot(panelValue: "tab1", defaultValue: "tab1"));
        var element = cut.Find("[role='tabpanel']");
        element.GetAttribute("tabindex").ShouldBe("0");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabIndexMinus1WhenHidden()
    {
        var cut = Render(CreatePanelInRoot(panelValue: "tab1", defaultValue: "tab2", keepMounted: true));
        var panels = cut.FindAll("[role='tabpanel']");
        var hiddenPanel = panels.First(p => p.HasAttribute("hidden"));
        hiddenPanel.GetAttribute("tabindex").ShouldBe("-1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaLabelledbyPointingToTab()
    {
        var cut = Render(CreateMultiplePanelsInRoot(defaultValue: "tab1"));

        // Tab IDs are registered in OnAfterRenderAsync, so panels don't have
        // aria-labelledby on first render. Force a re-render to pick up the IDs.
        cut.FindComponent<TabsRoot<string>>().Render();

        var tab = cut.Find("[role='tab']");
        var panel = cut.Find("[role='tabpanel']");
        panel.GetAttribute("aria-labelledby").ShouldBe(tab.GetAttribute("id"));
        return Task.CompletedTask;
    }

    // Visibility

    [Fact]
    public Task HasHiddenWhenNotActive()
    {
        var cut = Render(CreatePanelInRoot(panelValue: "tab1", defaultValue: "tab2", keepMounted: true));
        var panels = cut.FindAll("[role='tabpanel']");
        var inactivePanel = panels.First(p => p.HasAttribute("hidden"));
        inactivePanel.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveHiddenWhenActive()
    {
        var cut = Render(CreatePanelInRoot(panelValue: "tab1", defaultValue: "tab1"));
        var element = cut.Find("[role='tabpanel']");
        element.HasAttribute("hidden").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderWhenNotActiveAndKeepMountedFalse()
    {
        var cut = Render(CreateMultiplePanelsInRoot(defaultValue: "tab1", keepMounted: false));
        // Only the active panel should be in the DOM
        var panels = cut.FindAll("[role='tabpanel']");
        panels.Count.ShouldBe(1);
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersHiddenWhenNotActiveAndKeepMountedTrue()
    {
        var cut = Render(CreateMultiplePanelsInRoot(defaultValue: "tab1", keepMounted: true));
        var panels = cut.FindAll("[role='tabpanel']");
        panels.Count.ShouldBe(2);

        // Active panel should not be hidden
        panels[0].HasAttribute("hidden").ShouldBeFalse();
        // Inactive panel should be hidden
        panels[1].HasAttribute("hidden").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataOrientationHorizontal()
    {
        var cut = Render(CreatePanelInRoot(orientation: Orientation.Horizontal));
        var element = cut.Find("[role='tabpanel']");
        element.GetAttribute("data-orientation").ShouldBe("horizontal");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataActivationDirection()
    {
        var cut = Render(CreatePanelInRoot());
        var element = cut.Find("[role='tabpanel']");
        element.GetAttribute("data-activation-direction").ShouldBe("none");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataIndex()
    {
        var cut = Render(CreateMultiplePanelsInRoot(defaultValue: "tab1", keepMounted: true));
        var panels = cut.FindAll("[role='tabpanel']");
        panels[0].GetAttribute("data-index").ShouldBe("0");
        panels[1].GetAttribute("data-index").ShouldBe("1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataHiddenWhenNotActive()
    {
        var cut = Render(CreatePanelInRoot(panelValue: "tab1", defaultValue: "tab2", keepMounted: true));
        var panels = cut.FindAll("[role='tabpanel']");
        var hiddenPanel = panels.First(p => p.HasAttribute("hidden"));
        hiddenPanel.HasAttribute("data-hidden").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataHiddenWhenActive()
    {
        var cut = Render(CreatePanelInRoot(panelValue: "tab1", defaultValue: "tab1"));
        var element = cut.Find("[role='tabpanel']");
        element.HasAttribute("data-hidden").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasInertWhenHiddenAndKeptMounted()
    {
        var cut = Render(CreatePanelInRoot(panelValue: "tab1", defaultValue: "tab2", keepMounted: true));
        var panel = cut.Find("[role='tabpanel']");
        panel.HasAttribute("inert").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ClassValueReceivesTransitionStatus()
    {
        TabsPanelState? capturedState = null;
        var cut = Render(CreatePanelInRoot(
            classValue: state =>
            {
                capturedState = state;
                return "test";
            }));

        capturedState.ShouldNotBeNull();
        capturedState!.TransitionStatus.ShouldBe(TransitionStatus.Undefined);
        return Task.CompletedTask;
    }

    [Fact]
    public Task AddsStartingStyleWhenPanelOpens()
    {
        var cut = Render(CreateMultiplePanelsInRoot(defaultValue: "tab1", keepMounted: false));

        cut.FindAll("[role='tab']")[1].Click();

        var openingPanel = cut.FindAll("[role='tabpanel']")
            .Single(panel => panel.TextContent.Contains("Panel 2 content", StringComparison.Ordinal));
        openingPanel.HasAttribute("data-starting-style").ShouldBeTrue();
        openingPanel.HasAttribute("hidden").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task KeepsExitingPanelVisibleUntilTransitionCompletes()
    {
        var cut = Render(CreateMultiplePanelsInRoot(defaultValue: "tab1", keepMounted: false));

        cut.FindAll("[role='tab']")[1].Click();

        var exitingPanel = cut.FindAll("[role='tabpanel']")
            .Single(panel => panel.TextContent.Contains("Panel 1 content", StringComparison.Ordinal));
        exitingPanel.HasAttribute("data-ending-style").ShouldBeTrue();
        exitingPanel.HasAttribute("hidden").ShouldBeFalse();
        exitingPanel.HasAttribute("data-hidden").ShouldBeFalse();
        exitingPanel.GetAttribute("tabindex").ShouldBe("-1");
        exitingPanel.HasAttribute("inert").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DisposeAsync_DisposesImportedModuleAfterPanelElementUnmounts()
    {
        var cut = Render(CreateMultiplePanelsInRoot(defaultValue: "tab1", keepMounted: false));

        cut.FindAll("[role='tab']")[1].Click();

        module.Invocations
            .Any(invocation => invocation.Identifier == "startPanelTransition")
            .ShouldBeTrue();

        var exitingPanel = cut.FindComponents<TabsPanel<string>>()
            .Single(panel => panel.Instance.Value == "tab1");

        await exitingPanel.InvokeAsync(() => exitingPanel.Instance.OnPanelTransitionEnded());

        var renderElementField = typeof(TabsPanel<string>).GetField(
            "renderElementReference",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        renderElementField.ShouldNotBeNull();
        renderElementField.SetValue(exitingPanel.Instance, null);

        exitingPanel.Instance.Element.HasValue.ShouldBeFalse();

        await exitingPanel.InvokeAsync(async () => await exitingPanel.Instance.DisposeAsync());

        module.Invocations
            .Any(invocation => invocation.Identifier == "disposePanel")
            .ShouldBeTrue();
    }

    // State

    [Fact]
    public Task ClassValueReceivesTabsPanelState()
    {
        TabsPanelState? capturedState = null;
        var cut = Render(CreatePanelInRoot(
            classValue: state =>
            {
                capturedState = state;
                return "test";
            }));

        capturedState.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ClassValueReceivesHiddenTrue()
    {
        TabsPanelState? capturedState = null;
        var cut = Render(CreatePanelInRoot(
            panelValue: "tab1",
            defaultValue: "tab2",
            keepMounted: true,
            classValue: state =>
            {
                capturedState = state;
                return "test";
            }));

        capturedState.ShouldNotBeNull();
        capturedState!.Hidden.ShouldBeTrue();
        return Task.CompletedTask;
    }

    // Element reference

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreatePanelInRoot());
        var component = cut.FindComponent<TabsPanel<string>>();
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
                builder.OpenComponent<TabsPanel<string>>(0);
                builder.AddAttribute(1, "Value", "tab1");
                builder.CloseComponent();
            });
        });

        return Task.CompletedTask;
    }
}
