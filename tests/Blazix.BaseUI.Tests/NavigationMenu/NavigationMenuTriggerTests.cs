namespace Blazix.BaseUI.Tests.NavigationMenu;

public class NavigationMenuTriggerTests : BunitContext, INavigationMenuTriggerContract
{
    public NavigationMenuTriggerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNavigationMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
    }

    private RenderFragment CreateTriggerInRoot(
        string? defaultValue = null,
        bool triggerDisabled = false,
        bool includePopup = false,
        Func<NavigationMenuTriggerState, string>? classValue = null,
        Func<NavigationMenuTriggerState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            var rootAttr = 1;
            if (defaultValue is not null)
                builder.AddAttribute(rootAttr++, "DefaultValue", defaultValue);
            builder.AddAttribute(rootAttr++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuItem>(0);
                innerBuilder.AddAttribute(1, "Value", "item1");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<NavigationMenuTrigger>(0);
                    var attrIndex = 1;
                    if (triggerDisabled)
                        itemBuilder.AddAttribute(attrIndex++, "Disabled", true);
                    if (classValue is not null)
                        itemBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                    if (styleValue is not null)
                        itemBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                    if (additionalAttributes is not null)
                        itemBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                    itemBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                    itemBuilder.CloseComponent();

                    itemBuilder.OpenComponent<NavigationMenuContent>(10);
                    itemBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                    itemBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();

                if (includePopup)
                {
                    innerBuilder.OpenComponent<NavigationMenuPositioner>(4);
                    innerBuilder.AddAttribute(5, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<NavigationMenuPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Popup")));
                        posBuilder.CloseComponent();
                    }));
                    innerBuilder.CloseComponent();
                }
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersButtonByDefault()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        button.TagName.ShouldBe("BUTTON");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateTriggerInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "trigger" },
                { "aria-label", "Open nav" }
            }
        ));

        var button = cut.Find("button");
        button.GetAttribute("data-testid").ShouldBe("trigger");
        button.GetAttribute("aria-label").ShouldBe("Open nav");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedFalse()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        button.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedTrue()
    {
        var cut = Render(CreateTriggerInRoot(defaultValue: "item1"));

        var button = cut.Find("button");
        button.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task NoAriaControlsWhenClosed()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        button.HasAttribute("aria-controls").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaControlsWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultValue: "item1", includePopup: true));

        var button = cut.Find("button");
        var ariaControls = button.GetAttribute("aria-controls");
        ariaControls.ShouldNotBeNullOrEmpty();

        // aria-controls should reference the popup element's auto-generated id
        var popup = cut.Find($"nav[id='{ariaControls}']");
        popup.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabIndex()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        button.GetAttribute("tabindex").ShouldBe("0");

        return Task.CompletedTask;
    }

    [Fact]
    public Task NoDataPopupOpenWhenClosed()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        button.HasAttribute("data-popup-open").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataPopupOpenWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultValue: "item1"));

        var button = cut.Find("button");
        button.HasAttribute("data-popup-open").ShouldBeTrue();
        button.HasAttribute("data-pressed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateTriggerInRoot(
            defaultValue: "item1",
            classValue: state => state.Open ? "open-class" : "closed-class"
        ));

        var button = cut.Find("button");
        button.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ToggleOnClick()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        button.GetAttribute("aria-expanded").ShouldBe("false");

        await button.TriggerEventAsync("onclick", new MouseEventArgs());
        cut.FindComponent<NavigationMenuTrigger>().Render();

        button = cut.Find("button");
        button.GetAttribute("aria-expanded").ShouldBe("true");
    }

    [Fact]
    public Task DoesNotToggleWhenDisabled()
    {
        var cut = Render(CreateTriggerInRoot(triggerDisabled: true));

        var button = cut.Find("button");
        button.GetAttribute("aria-expanded").ShouldBe("false");

        button.Click();

        button.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<NavigationMenuTrigger>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Trigger"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesAriaDisabledWhenDisabled()
    {
        var cut = Render(CreateTriggerInRoot(triggerDisabled: true));

        var button = cut.Find("button");
        button.GetAttribute("aria-disabled").ShouldBe("true");
        button.HasAttribute("disabled").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RemainsInTabOrderWhenDisabled()
    {
        var cut = Render(CreateTriggerInRoot(triggerDisabled: true));

        var button = cut.Find("button");
        button.GetAttribute("tabindex").ShouldBe("0");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasKeyDownHandlerWired()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        // bUnit registers event handlers as blazor:onkeydown attributes
        button.HasAttribute("blazor:onkeydown").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasFocusHandlerWired()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        button.HasAttribute("blazor:onfocus").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasPointerDownHandlerWired()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        button.HasAttribute("blazor:onpointerdown").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersFocusGuardsWhenActive()
    {
        var cut = Render(CreateTriggerInRoot(defaultValue: "item1"));

        var guards = cut.FindAll("[data-blazix-base-ui-focus-guard]");
        guards.Count.ShouldBe(2);

        return Task.CompletedTask;
    }

    [Fact]
    public Task NoFocusGuardsWhenInactive()
    {
        var cut = Render(CreateTriggerInRoot());

        var guards = cut.FindAll("[data-blazix-base-ui-focus-guard]");
        guards.Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersAriaOwnsWhenActive()
    {
        var cut = Render(CreateTriggerInRoot(defaultValue: "item1"));

        var ariaOwnsSpan = cut.FindAll("[aria-owns]");
        ariaOwnsSpan.Count.ShouldBe(1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task NoAriaOwnsWhenInactive()
    {
        var cut = Render(CreateTriggerInRoot());

        var ariaOwnsSpan = cut.FindAll("[aria-owns]");
        ariaOwnsSpan.Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateTriggerInRoot(
            defaultValue: "item1",
            styleValue: state => state.Open ? "color:red" : "color:blue"
        ));

        var button = cut.Find("button");
        button.GetAttribute("style")!.ShouldContain("color:red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasBlurHandlerWired()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        button.HasAttribute("blazor:onblur").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasMouseMoveHandlerWired()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        button.HasAttribute("blazor:onmousemove").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task FocusGuardsRenderedAfterTrigger()
    {
        var cut = Render(CreateTriggerInRoot(defaultValue: "item1"));

        // The button should appear before the focus guards in the DOM
        var markup = cut.Markup;
        var buttonIndex = markup.IndexOf("<button");
        var guardIndex = markup.IndexOf("data-blazix-base-ui-focus-guard");
        guardIndex.ShouldBeGreaterThan(buttonIndex);

        return Task.CompletedTask;
    }
}
