namespace Blazix.BaseUI.Tests.NavigationMenu;

public class NavigationMenuViewportTests : BunitContext, INavigationMenuViewportContract
{
    public NavigationMenuViewportTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNavigationMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
    }

    private RenderFragment CreateViewportInRoot(
        Func<NavigationMenuViewportState, string>? classValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuViewport>(0);
                var attrIndex = 1;
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                innerBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Viewport content")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateViewportWithContext(bool viewportInert, bool insidePositioner)
    {
        var rootContext = new NavigationMenuRootContext
        {
            RootId = "test-root",
            ViewportId = "test-viewport-id",
            ViewportTargetId = "test-viewport-target-id",
            ViewportInert = viewportInert,
            GetValue = () => null,
            GetMounted = () => true,
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
            DisposeContentElement = _ => { },
            EmitClose = _ => { },
            SetViewportInert = _ => { },
            EmitLinkCloseAsync = () => Task.CompletedTask,
        };

        var positionerContext = new NavigationMenuPositionerContext
        {
            Side = Side.Bottom,
            Align = Align.Center,
            GetArrowElement = () => null,
            SetArrowElement = _ => { },
        };

        RenderFragment viewportFragment = viewportBuilder =>
        {
            viewportBuilder.OpenComponent<NavigationMenuViewport>(0);
            viewportBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Viewport content")));
            viewportBuilder.CloseComponent();
        };

        return builder =>
        {
            builder.OpenComponent<CascadingValue<NavigationMenuRootContext>>(0);
            builder.AddAttribute(1, "Value", rootContext);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(inner =>
            {
                if (!insidePositioner)
                {
                    viewportFragment(inner);
                    return;
                }

                inner.OpenComponent<CascadingValue<NavigationMenuPositionerContext>>(0);
                inner.AddAttribute(1, "Value", positionerContext);
                inner.AddAttribute(2, "ChildContent", viewportFragment);
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersDivByDefault()
    {
        var cut = Render(CreateViewportInRoot());

        var divs = cut.FindAll("div");
        divs.ShouldNotBeEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateViewportInRoot(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "viewport" } }
        ));

        var div = cut.Find("div[data-testid='viewport']");
        div.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasGeneratedId()
    {
        var cut = Render(CreateViewportInRoot());

        var div = cut.Find("div[id]");
        div.GetAttribute("id").ShouldNotBeNullOrEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasBlurHandlerWired()
    {
        var cut = Render(CreateViewportInRoot());

        var div = cut.Find("div[id]");
        div.HasAttribute("blazor:onblur").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersViewportTargetAndFocusGuards()
    {
        var cut = Render(CreateViewportInRoot());

        cut.Find("div[data-blazix-base-ui-navigation-menu-viewport-target]");
        cut.FindAll("span[data-blazix-base-ui-focus-guard]").Count.ShouldBe(2);

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateViewportInRoot(
            classValue: _ => "viewport-class"
        ));

        var div = cut.Find("div");
        div.GetAttribute("class")!.ShouldContain("viewport-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<NavigationMenuViewport>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Viewport"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task IsInertWhenViewportInertWithoutPositioner()
    {
        var cut = Render(CreateViewportWithContext(viewportInert: true, insidePositioner: false));

        cut.Find("div[id]").HasAttribute("inert").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotApplyInertWhenInsidePositioner()
    {
        var cut = Render(CreateViewportWithContext(viewportInert: true, insidePositioner: true));

        cut.Find("div[id]").HasAttribute("inert").ShouldBeFalse();

        return Task.CompletedTask;
    }
}
