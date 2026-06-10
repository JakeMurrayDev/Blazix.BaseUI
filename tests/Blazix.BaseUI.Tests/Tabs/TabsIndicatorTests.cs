namespace Blazix.BaseUI.Tests.Tabs;

public class TabsIndicatorTests : BunitContext, ITabsIndicatorContract
{
    public TabsIndicatorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupTabsModule(JSInterop);
    }

    private RenderFragment CreateIndicatorInRoot(
        string? defaultValue = "tab1",
        Orientation orientation = Orientation.Horizontal,
        RenderFragment<RenderProps<TabsIndicatorState>>? render = null,
        Func<TabsIndicatorState, string>? classValue = null,
        Func<TabsIndicatorState, string>? styleValue = null,
        bool renderBeforeHydration = false,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment? childContent = null)
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
                    listInner.AddAttribute(1, "Value", "tab1");
                    listInner.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Tab 1")));
                    listInner.CloseComponent();

                    listInner.OpenComponent<TabsIndicator<string>>(10);
                    var seq = 11;
                    if (render is not null)
                        listInner.AddAttribute(seq++, "Render", render);
                    if (classValue is not null)
                        listInner.AddAttribute(seq++, "ClassValue", classValue);
                    if (styleValue is not null)
                        listInner.AddAttribute(seq++, "StyleValue", styleValue);
                    if (renderBeforeHydration)
                        listInner.AddAttribute(seq++, "RenderBeforeHydration", true);
                    if (additionalAttributes is not null)
                        listInner.AddAttribute(seq++, "AdditionalAttributes", additionalAttributes);
                    if (childContent is not null)
                        listInner.AddAttribute(seq++, "ChildContent", childContent);
                    listInner.CloseComponent();
                }));
                rootInner.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsSpanByDefault()
    {
        var cut = Render(CreateIndicatorInRoot());
        var element = cut.Find("[role='presentation']");
        element.TagName.ShouldBe("SPAN");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateIndicatorInRoot(
            render: ctx => builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }));
        var element = cut.Find("[role='presentation']");
        element.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateIndicatorInRoot(
            childContent: b => b.AddContent(0, "Indicator")));
        cut.Markup.ShouldContain("Indicator");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateIndicatorInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-custom", "value" }
            }));
        var element = cut.Find("[role='presentation']");
        element.GetAttribute("data-custom").ShouldBe("value");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateIndicatorInRoot(classValue: _ => "indicator-class"));
        var element = cut.Find("[role='presentation']");
        element.GetAttribute("class").ShouldContain("indicator-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateIndicatorInRoot(styleValue: _ => "background: blue"));
        var element = cut.Find("[role='presentation']");
        element.GetAttribute("style").ShouldContain("background: blue");
        return Task.CompletedTask;
    }

    // ARIA

    [Fact]
    public Task HasRolePresentation()
    {
        var cut = Render(CreateIndicatorInRoot());
        var element = cut.Find("[role='presentation']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // Visibility

    [Fact]
    public Task DoesNotRenderWhenValueIsNull()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<TabsRoot<string>>(0);
            builder.AddAttribute(1, "DefaultValue", (string?)null);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(rootInner =>
            {
                rootInner.OpenComponent<TabsList<string>>(0);
                rootInner.AddAttribute(1, "ChildContent", (RenderFragment)(listInner =>
                {
                    listInner.OpenComponent<TabsTab<string>>(0);
                    listInner.AddAttribute(1, "Value", "tab1");
                    listInner.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Tab 1")));
                    listInner.CloseComponent();

                    listInner.OpenComponent<TabsIndicator<string>>(10);
                    listInner.AddAttribute(11, "data-testid", "indicator");
                    listInner.CloseComponent();
                }));
                rootInner.CloseComponent();
            }));
            builder.CloseComponent();
        });
        var elements = cut.FindAll("[role='presentation']");
        elements.Count.ShouldBe(0);
        return Task.CompletedTask;
    }

    [Fact]
    public Task RenderBeforeHydrationEmitsScript()
    {
        var cut = Render(CreateIndicatorInRoot(renderBeforeHydration: true));
        var scripts = cut.FindAll("script");
        scripts.Count.ShouldBe(1);
        scripts[0].TextContent.ShouldContain("--active-tab-");
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataActivationDirection()
    {
        var cut = Render(CreateIndicatorInRoot());
        var element = cut.Find("[role='presentation']");
        element.GetAttribute("data-activation-direction").ShouldBe("none");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientation()
    {
        var cut = Render(CreateIndicatorInRoot());
        var element = cut.Find("[role='presentation']");
        element.GetAttribute("data-orientation").ShouldBe("horizontal");
        return Task.CompletedTask;
    }

    // Element reference

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreateIndicatorInRoot());
        var component = cut.FindComponent<TabsIndicator<string>>();
        component.Instance.Element.ShouldNotBeNull();
        return Task.CompletedTask;
    }
}
