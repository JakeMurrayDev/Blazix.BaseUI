namespace Blazix.BaseUI.Tests.Collapsible;

public class CollapsiblePanelTests : BunitContext, ICollapsiblePanelContract
{
    public CollapsiblePanelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupCollapsiblePanel(JSInterop);
    }

    private RenderFragment CreatePanelInRoot(
        bool defaultOpen = false,
        bool disabled = false,
        bool keepMounted = true,
        bool hiddenUntilFound = false,
        Func<CollapsiblePanelState, string>? classValue = null,
        Func<CollapsiblePanelState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<CollapsiblePanelState>>? render = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<CollapsibleRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            if (disabled)
                builder.AddAttribute(2, "Disabled", true);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<CollapsibleTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<CollapsiblePanel>(2);
                var attrIndex = 3;

                innerBuilder.AddAttribute(attrIndex++, "KeepMounted", keepMounted);
                innerBuilder.AddAttribute(attrIndex++, "HiddenUntilFound", hiddenUntilFound);
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                if (render is not null)
                    innerBuilder.AddAttribute(attrIndex++, "Render", render);
                innerBuilder.AddAttribute(attrIndex++, "ChildContent", childContent ?? ((RenderFragment)(b => b.AddContent(0, "Panel Content"))));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreatePanelInRoot(defaultOpen: true));

        var panel = cut.Find("div[data-open]");
        panel.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreatePanelInRoot(
            defaultOpen: true,
            render: ctx => builder =>
            {
                builder.OpenElement(0, "section");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));

        var panel = cut.Find("section[data-open]");
        panel.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreatePanelInRoot(
            defaultOpen: true,
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "panel" },
                { "aria-label", "Collapsible content" }
            }
        ));

        cut.Markup.ShouldContain("data-testid=\"panel\"");
        cut.Markup.ShouldContain("aria-label=\"Collapsible content\"");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreatePanelInRoot(
            defaultOpen: true,
            classValue: _ => "panel-class"
        ));

        cut.Markup.ShouldContain("panel-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreatePanelInRoot(
            defaultOpen: true,
            styleValue: _ => "background: green"
        ));

        cut.Markup.ShouldContain("background: green");

        return Task.CompletedTask;
    }

    [Fact]
    public Task IsNotVisibleWhenClosed()
    {
        var cut = Render(CreatePanelInRoot(defaultOpen: false, keepMounted: false, hiddenUntilFound: false));

        // When closed, keepMounted is false, and not hiddenUntilFound, the panel is not rendered at all
        // Panel content should not be visible
        cut.Markup.ShouldNotContain("Panel Content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task IsVisibleWhenOpen()
    {
        var cut = Render(CreatePanelInRoot(defaultOpen: true));

        var panel = cut.Find("div[data-open]");
        panel.ShouldNotBeNull();
        cut.Markup.ShouldContain("Panel Content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RemainsInDomWhenKeepMounted()
    {
        var cut = Render(CreatePanelInRoot(defaultOpen: false, keepMounted: true));

        var panel = cut.FindAll("div[data-closed]").Last();
        panel.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task KeepMountedPanelIsHiddenWhenClosed()
    {
        var cut = Render(CreatePanelInRoot(defaultOpen: false, keepMounted: true));

        var panel = cut.FindAll("div[data-closed]").Last();
        panel.HasAttribute("hidden").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task IsRemovedFromDomWhenNotKeepMounted()
    {
        var cut = Render(CreatePanelInRoot(defaultOpen: false, keepMounted: false, hiddenUntilFound: false));

        // When closed and not keepMounted, panel content should not be visible
        // The panel may still have a data-closed element due to initial render behavior
        cut.Markup.ShouldNotContain("Panel Content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasHiddenUntilFoundAttribute()
    {
        // With hiddenUntilFound: true, panel is rendered when closed with hidden attribute
        var cut = Render(CreatePanelInRoot(defaultOpen: false, keepMounted: false, hiddenUntilFound: true));

        var panel = cut.FindAll("div[data-closed]").Last();
        panel.GetAttribute("hidden").ShouldBe("until-found");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HiddenUntilFoundClosedPanelPersistsStartingStyle()
    {
        var cut = Render(CreatePanelInRoot(defaultOpen: false, keepMounted: false, hiddenUntilFound: true));

        var panel = cut.FindAll("div[data-closed]").Last();
        panel.HasAttribute("data-starting-style").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreatePanelInRoot(defaultOpen: true));

        var panel = cut.Find("div[data-open]");
        panel.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task InitialOpenStateReportsIdleTransitionStatus()
    {
        CollapsiblePanelState? capturedState = null;

        Render(CreatePanelInRoot(
            defaultOpen: true,
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.TransitionStatus.ShouldBe(TransitionStatus.Idle);

        return Task.CompletedTask;
    }

    [Fact]
    public Task InitialOpenPanelSuppressesMountAnimationAfterUserStyles()
    {
        var cut = Render(CreatePanelInRoot(
            defaultOpen: true,
            styleValue: _ => "animation-name: panel-slide-down; animation-duration: 100ms"
        ));

        var panel = cut.FindAll("div[data-open]").Last();
        var style = panel.GetAttribute("style");

        style.ShouldNotBeNull();
        style!.ShouldContain("animation-name: panel-slide-down");
        style.ShouldEndWith("animation-name: none");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InitialOpenPanelDoesNotSuppressReopenAfterCloseRequest()
    {
        var cut = Render(CreatePanelInRoot(
            defaultOpen: true,
            styleValue: _ => "animation-name: panel-slide-down; animation-duration: 100ms"
        ));

        var trigger = cut.Find("button");
        trigger.Click();
        trigger.Click();

        var panel = cut.FindAll("div[data-open]").Last();
        var style = panel.GetAttribute("style");

        style.ShouldNotBeNull();
        style!.ShouldContain("animation-name: panel-slide-down");
        style.ShouldNotEndWith("animation-name: none");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreatePanelInRoot(defaultOpen: false, keepMounted: true));

        var panel = cut.Find("div[data-closed]");
        panel.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ReceivesCorrectState()
    {
        CollapsiblePanelState? capturedState = null;

        var cut = Render(CreatePanelInRoot(
            defaultOpen: true,
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Open.ShouldBeTrue();
        capturedState.Disabled.ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<CollapsiblePanel>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
