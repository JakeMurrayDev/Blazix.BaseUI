namespace Blazix.BaseUI.Tests.NavigationMenu;

public class NavigationMenuPopupTests : BunitContext, INavigationMenuPopupContract
{
    public NavigationMenuPopupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNavigationMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
    }

    private RenderFragment CreatePopupInRoot(
        string? defaultValue = null,
        Func<NavigationMenuPopupState, string>? classValue = null,
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
                    itemBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                    itemBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<NavigationMenuPositioner>(4);
                innerBuilder.AddAttribute(5, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<NavigationMenuPopup>(0);
                    var attrIndex = 1;
                    if (classValue is not null)
                        posBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                    if (additionalAttributes is not null)
                        posBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                    posBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Popup content")));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreatePopupWithContext(
        string? value = null,
        bool mounted = false,
        TransitionStatus transitionStatus = TransitionStatus.Undefined,
        string direction = "ltr",
        Side side = Side.Bottom,
        Align align = Align.Center,
        bool anchorHidden = false,
        Func<NavigationMenuPopupState, string?>? classValue = null,
        Func<NavigationMenuPopupState, string?>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        var rootContext = new NavigationMenuRootContext
        {
            RootId = "test-root",
            Value = value,
            Mounted = mounted,
            Direction = direction,
            TransitionStatus = transitionStatus,
            PopupId = "test-popup-id",
            ViewportId = "test-viewport-id",
            GetValue = () => value,
            GetMounted = () => mounted,
            SetValueAsync = (_, _) => Task.CompletedTask,
            SetTriggerElement = (_, _) => { },
            GetTriggerElement = _ => null,
            SetPopupElement = _ => { },
            SetPositionerElement = _ => { },
            SetViewportElement = _ => { },
            SetViewportTargetElement = _ => { },
            RegisterItem = _ => { },
            UnregisterItem = _ => { },
            SetContentElement = (_, _) => { },
            EmitClose = _ => { },
            SetViewportInert = _ => { },
            SetPrevTriggerElement = _ => { },
            GetPrevTriggerElement = () => null,
            SetListElement = _ => { },
            UnmountAsync = () => Task.CompletedTask,
            RegisterContentCallback = (_, _) => { },
            UnregisterContentCallback = _ => { },
        };

        var positionerContext = new NavigationMenuPositionerContext
        {
            Side = side,
            Align = align,
            AnchorHidden = anchorHidden,
            ArrowUncentered = false,
            GetArrowElement = () => null,
            SetArrowElement = _ => { },
        };

        return builder =>
        {
            builder.OpenComponent<CascadingValue<NavigationMenuRootContext>>(0);
            builder.AddAttribute(1, "Value", rootContext);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<CascadingValue<NavigationMenuPositionerContext>>(0);
                inner.AddAttribute(1, "Value", positionerContext);
                inner.AddAttribute(2, "ChildContent", (RenderFragment)(popupBuilder =>
                {
                    popupBuilder.OpenComponent<NavigationMenuPopup>(0);
                    var attrIndex = 1;
                    if (classValue is not null)
                        popupBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                    if (styleValue is not null)
                        popupBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                    if (additionalAttributes is not null)
                        popupBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                    popupBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Popup content")));
                    popupBuilder.CloseComponent();
                }));
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersNavByDefault()
    {
        var cut = Render(CreatePopupInRoot());

        // The root is also a nav, the popup is also a nav
        var navs = cut.FindAll("nav");
        navs.Count.ShouldBeGreaterThan(1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreatePopupInRoot(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "popup" } }
        ));

        var popup = cut.Find("nav[data-testid='popup']");
        popup.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAutoGeneratedId()
    {
        var cut = Render(CreatePopupInRoot());

        var popup = cut.Find("nav[tabindex='-1']");
        var id = popup.GetAttribute("id");
        id.ShouldNotBeNullOrEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataSide()
    {
        var cut = Render(CreatePopupInRoot());

        var popup = cut.Find("nav[tabindex='-1']");
        popup.GetAttribute("data-side").ShouldBe("bottom");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataAlign()
    {
        var cut = Render(CreatePopupInRoot());

        var popup = cut.Find("nav[tabindex='-1']");
        popup.GetAttribute("data-align").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabIndexMinusOne()
    {
        var cut = Render(CreatePopupInRoot());

        var popup = cut.Find("nav[tabindex='-1']");
        popup.GetAttribute("tabindex").ShouldBe("-1");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreatePopupInRoot(
            classValue: _ => "popup-class"
        ));

        var popup = cut.Find("nav[tabindex='-1']");
        popup.GetAttribute("class")!.ShouldContain("popup-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<NavigationMenuPopup>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Popup"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreatePopupWithContext(value: "item1", mounted: true));

        var popup = cut.Find("nav[tabindex='-1']");
        popup.HasAttribute("data-open").ShouldBeTrue();
        popup.HasAttribute("data-closed").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreatePopupWithContext());

        var popup = cut.Find("nav[tabindex='-1']");
        popup.HasAttribute("data-closed").ShouldBeTrue();
        popup.HasAttribute("data-open").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataStartingStyleDuringTransition()
    {
        var cut = Render(CreatePopupWithContext(
            value: "item1",
            mounted: true,
            transitionStatus: TransitionStatus.Starting));

        var popup = cut.Find("nav[tabindex='-1']");
        popup.HasAttribute("data-starting-style").ShouldBeTrue();
        popup.HasAttribute("data-ending-style").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataEndingStyleDuringTransition()
    {
        var cut = Render(CreatePopupWithContext(
            mounted: true,
            transitionStatus: TransitionStatus.Ending));

        var popup = cut.Find("nav[tabindex='-1']");
        popup.HasAttribute("data-ending-style").ShouldBeTrue();
        popup.HasAttribute("data-starting-style").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataAnchorHiddenWhenAnchorHidden()
    {
        var cut = Render(CreatePopupWithContext(
            value: "item1",
            mounted: true,
            anchorHidden: true));

        var popup = cut.Find("nav[tabindex='-1']");
        popup.HasAttribute("data-anchor-hidden").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesOriginSideStyleForTopSide()
    {
        var cut = Render(CreatePopupWithContext(
            value: "item1",
            mounted: true,
            side: Side.Top));

        var popup = cut.Find("nav[tabindex='-1']");
        var style = popup.GetAttribute("style");
        style.ShouldContain("position: absolute");
        style.ShouldContain("bottom: 0");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesRtlOriginSideStyleForInlineEnd()
    {
        var cut = Render(CreatePopupWithContext(
            value: "item1",
            mounted: true,
            direction: "rtl",
            side: Side.InlineEnd));

        var popup = cut.Find("nav[tabindex='-1']");
        var style = popup.GetAttribute("style");
        style.ShouldContain("position: absolute");
        style.ShouldContain("right: 0");

        return Task.CompletedTask;
    }
}
