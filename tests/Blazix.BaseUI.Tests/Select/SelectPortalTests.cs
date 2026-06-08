namespace Blazix.BaseUI.Tests.Select;

public class SelectPortalTests : BunitContext, ISelectPortalContract
{
    public SelectPortalTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSelectModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateSelectWithPortal(
        bool defaultOpen = false,
        bool keepMounted = false,
        RenderFragment<RenderProps<object>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Action<SelectPortalContext?>? capturePortalContext = null,
        Action<ISelectRootContext?>? captureRootContext = null,
        string? container = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                if (captureRootContext is not null)
                {
                    innerBuilder.OpenComponent<CascadingValueCapture<ISelectRootContext>>(0);
                    innerBuilder.AddAttribute(
                        1,
                        "OnCaptured",
                        EventCallback.Factory.Create<ISelectRootContext?>(this, captureRootContext));
                    innerBuilder.CloseComponent();
                }

                innerBuilder.OpenComponent<SelectTrigger>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPortal>(20);
                innerBuilder.AddAttribute(21, "KeepMounted", keepMounted);
                if (container is not null)
                {
                    innerBuilder.AddAttribute(22, "Container", container);
                }
                if (render is not null)
                {
                    innerBuilder.AddAttribute(23, "Render", render);
                }
                if (additionalAttributes is not null)
                {
                    foreach (var kvp in additionalAttributes)
                    {
                        innerBuilder.AddAttribute(24, kvp.Key, kvp.Value);
                    }
                }
                innerBuilder.AddAttribute(25, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    if (capturePortalContext is not null)
                    {
                        portalBuilder.OpenComponent<CascadingValueCapture<SelectPortalContext>>(0);
                        portalBuilder.AddAttribute(
                            1,
                            "OnCaptured",
                            EventCallback.Factory.Create<SelectPortalContext?>(this, capturePortalContext));
                        portalBuilder.CloseComponent();
                    }

                    portalBuilder.AddMarkupContent(10, "<span data-testid=\"portal-child\">Content</span>");
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersChildrenWhenMounted()
    {
        var cut = Render(CreateSelectWithPortal(defaultOpen: true));

        cut.Find("[data-testid='portal-child']").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderChildrenWhenNotMounted()
    {
        var cut = Render(CreateSelectWithPortal(defaultOpen: false, keepMounted: false));

        cut.FindAll("[data-testid='portal-child']").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildrenWhenKeepMounted()
    {
        var cut = Render(CreateSelectWithPortal(defaultOpen: false, keepMounted: true));

        cut.Find("[data-testid='portal-child']").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildrenWhenForceMounted()
    {
        ISelectRootContext? rootContext = null;
        var cut = Render(CreateSelectWithPortal(
            defaultOpen: false,
            keepMounted: false,
            captureRootContext: ctx => rootContext = ctx));

        cut.FindAll("[data-testid='portal-child']").Count.ShouldBe(0);

        rootContext.ShouldNotBeNull();
        rootContext!.ForceMount = true;
        cut.FindComponent<SelectPortal>().Render();

        cut.Find("[data-testid='portal-child']").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesPortalContext()
    {
        SelectPortalContext? captured = null;
        var cut = Render(CreateSelectWithPortal(
            defaultOpen: true,
            keepMounted: true,
            capturePortalContext: ctx => captured = ctx));

        captured.ShouldNotBeNull();
        captured!.KeepMounted.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        Should.Throw<InvalidOperationException>(() =>
            Render<SelectPortal>(parameters => parameters
                .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
            )
        );

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateSelectWithPortal(
            defaultOpen: true,
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "portal" },
                { "aria-label", "Portal" }
            }));

        var portal = cut.Find("[data-testid='portal']");
        portal.GetAttribute("aria-label").ShouldBe("Portal");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<object>> customRender = context => builder =>
        {
            builder.OpenElement(0, "section");
            foreach (var attr in context.Attributes)
            {
                builder.AddAttribute(1, attr.Key, attr.Value);
            }
            if (context.ChildContent is not null)
            {
                builder.AddContent(2, context.ChildContent);
            }
            builder.CloseElement();
        };

        var cut = Render(CreateSelectWithPortal(
            defaultOpen: true,
            render: customRender));

        var section = cut.Find("section[data-blazix-base-ui-portal]");
        section.TagName.ShouldBe("SECTION");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreateSelectWithPortal(defaultOpen: true));

        var portal = cut.FindComponent<SelectPortal>();
        portal.Instance.Element.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DefaultContainerIsBody()
    {
        var cut = Render(CreateSelectWithPortal(defaultOpen: true));

        var portal = cut.FindComponent<SelectPortal>();
        portal.Instance.Container.ShouldBe("body");
        cut.Find("[data-blazix-base-ui-portal]").ShouldNotBeNull();

        return Task.CompletedTask;
    }
}
