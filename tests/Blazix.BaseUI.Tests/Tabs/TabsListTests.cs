namespace Blazix.BaseUI.Tests.Tabs;

public class TabsListTests : BunitContext, ITabsListContract
{
    public TabsListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupTabsModule(JSInterop);
    }

    private RenderFragment CreateTabsList(
        Orientation orientation = Orientation.Horizontal,
        RenderFragment<RenderProps<TabsRootState>>? render = null,
        Func<TabsRootState, string>? classValue = null,
        Func<TabsRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<TabsRoot<string>>(0);
            builder.AddAttribute(1, "Orientation", orientation);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<TabsList<string>>(0);
                var seq = 1;
                if (render is not null)
                    innerBuilder.AddAttribute(seq++, "Render", render);
                if (classValue is not null)
                    innerBuilder.AddAttribute(seq++, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(seq++, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(seq++, "AdditionalAttributes", additionalAttributes);
                if (childContent is not null)
                    innerBuilder.AddAttribute(seq++, "ChildContent", childContent);
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateTabsList());
        var element = cut.Find("[role='tablist']");
        element.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateTabsList(
            render: ctx => builder =>
            {
                builder.OpenElement(0, "nav");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }));
        var element = cut.Find("[role='tablist']");
        element.TagName.ShouldBe("NAV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateTabsList(
            childContent: b => b.AddContent(0, "Tab Content")));
        cut.Markup.ShouldContain("Tab Content");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateTabsList(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-custom", "value" },
                { "aria-label", "My Tabs" }
            }));
        var element = cut.Find("[role='tablist']");
        element.GetAttribute("data-custom").ShouldBe("value");
        element.GetAttribute("aria-label").ShouldBe("My Tabs");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateTabsList(classValue: _ => "custom-class"));
        var element = cut.Find("[role='tablist']");
        element.GetAttribute("class").ShouldContain("custom-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateTabsList(styleValue: _ => "gap: 8px"));
        var element = cut.Find("[role='tablist']");
        element.GetAttribute("style").ShouldContain("gap: 8px");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateTabsList(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }));
        var element = cut.Find("[role='tablist']");
        var classAttr = element.GetAttribute("class");
        classAttr.ShouldContain("static-class");
        classAttr.ShouldContain("dynamic-class");
        return Task.CompletedTask;
    }

    // ARIA

    [Fact]
    public Task HasRoleTablist()
    {
        var cut = Render(CreateTabsList());
        var element = cut.Find("[role='tablist']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveAriaOrientationWhenHorizontal()
    {
        var cut = Render(CreateTabsList());
        var element = cut.Find("[role='tablist']");
        element.HasAttribute("aria-orientation").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaOrientationVerticalWhenVertical()
    {
        var cut = Render(CreateTabsList(orientation: Orientation.Vertical));
        var element = cut.Find("[role='tablist']");
        element.GetAttribute("aria-orientation").ShouldBe("vertical");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CanBeNamedViaAriaLabel()
    {
        var cut = Render(CreateTabsList(
            additionalAttributes: new Dictionary<string, object>
            {
                { "aria-label", "Navigation Tabs" }
            }));
        var element = cut.Find("[role='tablist']");
        element.GetAttribute("aria-label").ShouldBe("Navigation Tabs");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CanBeNamedViaAriaLabelledby()
    {
        var cut = Render(CreateTabsList(
            additionalAttributes: new Dictionary<string, object>
            {
                { "aria-labelledby", "tabs-heading" }
            }));
        var element = cut.Find("[role='tablist']");
        element.GetAttribute("aria-labelledby").ShouldBe("tabs-heading");
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataOrientationHorizontal()
    {
        var cut = Render(CreateTabsList());
        var element = cut.Find("[role='tablist']");
        element.GetAttribute("data-orientation").ShouldBe("horizontal");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataActivationDirection()
    {
        var cut = Render(CreateTabsList());
        var element = cut.Find("[role='tablist']");
        element.GetAttribute("data-activation-direction").ShouldBe("none");
        return Task.CompletedTask;
    }

    // Element reference

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreateTabsList());
        var component = cut.FindComponent<TabsList<string>>();
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
                builder.OpenComponent<TabsList<string>>(0);
                builder.CloseComponent();
            });
        });

        return Task.CompletedTask;
    }
}
