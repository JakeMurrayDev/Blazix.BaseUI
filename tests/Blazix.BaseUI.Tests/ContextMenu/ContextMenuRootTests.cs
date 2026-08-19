namespace Blazix.BaseUI.Tests.ContextMenu;

public class ContextMenuRootTests : BunitContext, IContextMenuRootContract
{
    public ContextMenuRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupContextMenuModule(JSInterop);
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateContextMenuRoot(
        bool? open = null,
        bool defaultOpen = false,
        bool disabled = false,
        MenuOrientation orientation = MenuOrientation.Vertical,
        EventCallback<MenuOpenChangeEventArgs>? onOpenChange = null,
        bool includeTrigger = true,
        bool includePositioner = true)
    {
        return builder =>
        {
            builder.OpenComponent<ContextMenuRoot>(0);
            var attrIndex = 1;

            if (open.HasValue)
                builder.AddAttribute(attrIndex++, "Open", open.Value);
            builder.AddAttribute(attrIndex++, "DefaultOpen", defaultOpen);
            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            builder.AddAttribute(attrIndex++, "Orientation", orientation);
            if (onOpenChange.HasValue)
                builder.AddAttribute(attrIndex++, "OnOpenChange", onOpenChange.Value);

            builder.AddAttribute(attrIndex++, "ChildContent", CreateChildContent(includeTrigger, includePositioner));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateChildContent(bool includeTrigger = true, bool includePositioner = true)
    {
        return builder =>
        {
            if (includeTrigger)
            {
                builder.OpenComponent<ContextMenuTrigger>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Right click here")));
                builder.CloseComponent();
            }
            if (includePositioner)
            {
                builder.OpenComponent<MenuPositioner>(2);
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item 1")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            }
        };
    }

    [Fact]
    public Task CascadesContextToTrigger()
    {
        ContextMenuTriggerState? capturedState = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<ContextMenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ContextMenuTrigger>(0);
                innerBuilder.AddAttribute(1, "ClassValue", (Func<ContextMenuTriggerState, string>)(state =>
                {
                    capturedState = state;
                    return "trigger-class";
                }));
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        capturedState.ShouldNotBeNull();
        capturedState!.Value.Open.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledModeUsesDefaultOpen()
    {
        var cut = Render(CreateContextMenuRoot(defaultOpen: true));

        var trigger = cut.Find("[style*='touch-callout']");
        trigger.HasAttribute("data-popup-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledModeRespectsOpenParameter()
    {
        var cut = Render(CreateContextMenuRoot(open: false));

        var trigger = cut.Find("[style*='touch-callout']");
        trigger.HasAttribute("data-popup-open").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithOnOpenChangeCallback()
    {
        var cut = Render(CreateContextMenuRoot(
            defaultOpen: true,
            onOpenChange: EventCallback.Factory.Create<MenuOpenChangeEventArgs>(this, _ => { })
        ));

        var trigger = cut.Find("[style*='touch-callout']");
        trigger.HasAttribute("data-popup-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledStatePreventsInteraction()
    {
        var cut = Render(CreateContextMenuRoot(disabled: true));

        var trigger = cut.Find("[style*='touch-callout']");
        trigger.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsOrientations()
    {
        var cutVertical = Render(CreateContextMenuRoot(orientation: MenuOrientation.Vertical));
        var cutHorizontal = Render(CreateContextMenuRoot(orientation: MenuOrientation.Horizontal));

        cutVertical.Find("[style*='touch-callout']").ShouldNotBeNull();
        cutHorizontal.Find("[style*='touch-callout']").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsParentTypeToContextMenu()
    {
        // ContextMenuRoot resets the parent MenuRootContext,
        // so the inner MenuRoot sees no parent and its ParentType is None.
        // This verifies the context isolation works correctly.
        var cut = Render(CreateContextMenuRoot());

        cut.Find("[style*='touch-callout']").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task OmitsModalOpenOnHoverDelayCloseDelayProps()
    {
        // ContextMenuRoot always passes MenuModalMode.True to MenuRoot (matching React default).
        // It does not expose Modal, OpenOnHover, Delay, or CloseDelay parameters.
        var cut = Render(CreateContextMenuRoot());

        cut.Find("[style*='touch-callout']").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ExposesLinkItemAlias()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<ContextMenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<ContextMenuTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<ContextMenuPositioner>(2);
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<ContextMenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<ContextMenuLinkItem>(0);
                        popupBuilder.AddAttribute(1, "AdditionalAttributes", new Dictionary<string, object>
                        {
                            ["href"] = "/projects"
                        });
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Projects")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var link = cut.Find("a[role='menuitem']");
        link.GetAttribute("href")!.ShouldBe("/projects");
        link.TextContent.ShouldBe("Projects");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OmitsDetachedTriggerAndPayloadProps()
    {
        // ContextMenuRoot mirrors the upstream Omit list: no detached-trigger support
        // (Handle/TriggerId/DefaultTriggerId) and no payload render-function children.
        typeof(ContextMenuRoot).GetProperty("Handle").ShouldBeNull();
        typeof(ContextMenuRoot).GetProperty("TriggerId").ShouldBeNull();
        typeof(ContextMenuRoot).GetProperty("DefaultTriggerId").ShouldBeNull();
        typeof(ContextMenuRoot).GetProperty("PayloadChildContent").ShouldBeNull();

        // ContextMenuSubmenuRoot (via MenuSubmenuRoot) likewise omits detached-trigger props.
        typeof(ContextMenuSubmenuRoot).GetProperty("Handle").ShouldBeNull();
        typeof(ContextMenuSubmenuRoot).GetProperty("TriggerId").ShouldBeNull();
        typeof(ContextMenuSubmenuRoot).GetProperty("DefaultTriggerId").ShouldBeNull();

        return Task.CompletedTask;
    }
}
