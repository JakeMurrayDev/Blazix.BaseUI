using BlazorBaseUI;

namespace BlazorBaseUI.Tests.MenuBar;

public class MenuBarRootTests : BunitContext, IMenuBarRootContract
{
    public MenuBarRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuBarModule(JSInterop);
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateMenuBarRoot(
        bool disabled = false,
        bool loopFocus = true,
        bool modal = true,
        Orientation orientation = Orientation.Horizontal,
        RenderFragment<RenderProps<MenuBarRootState>>? render = null,
        Func<MenuBarRootState, string>? classValue = null,
        Func<MenuBarRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        bool includeMenus = true)
    {
        return builder =>
        {
            builder.OpenComponent<MenuBarRoot>(0);
            var attrIndex = 1;

            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            builder.AddAttribute(attrIndex++, "LoopFocus", loopFocus);
            builder.AddAttribute(attrIndex++, "Modal", modal);
            builder.AddAttribute(attrIndex++, "Orientation", orientation);
            if (render is not null)
                builder.AddAttribute(attrIndex++, "Render", render);
            if (classValue is not null)
                builder.AddAttribute(attrIndex++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(attrIndex++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);

            if (includeMenus)
            {
                builder.AddAttribute(attrIndex++, "ChildContent", CreateChildContent());
            }
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateChildContent()
    {
        return builder =>
        {
            // First menu
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => menuBuilder =>
            {
                menuBuilder.OpenComponent<MenuTrigger>(0);
                menuBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "File")));
                menuBuilder.CloseComponent();

                menuBuilder.OpenComponent<MenuPositioner>(2);
                menuBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "New")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                menuBuilder.CloseComponent();
            }));
            builder.CloseComponent();

            // Second menu
            builder.OpenComponent<MenuRoot>(10);
            builder.AddAttribute(11, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => menuBuilder =>
            {
                menuBuilder.OpenComponent<MenuTrigger>(0);
                menuBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Edit")));
                menuBuilder.CloseComponent();

                menuBuilder.OpenComponent<MenuPositioner>(2);
                menuBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Undo")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                menuBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateMenuBarRoot(includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<MenuBarRootState>> renderAsNav = props => builder =>
        {
            builder.OpenElement(0, "nav");
            builder.AddMultipleAttributes(1, props.Attributes);
            builder.AddContent(2, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateMenuBarRoot(render: renderAsNav, includeMenus: false));

        var menubar = cut.Find("nav[role='menubar']");
        menubar.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRoleMenubar()
    {
        var cut = Render(CreateMenuBarRoot(includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.GetAttribute("role")!.ShouldBe("menubar");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaOrientationHorizontalByDefault()
    {
        var cut = Render(CreateMenuBarRoot(orientation: Orientation.Horizontal, includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.GetAttribute("aria-orientation")!.ShouldBe("horizontal");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaOrientationVerticalWhenSet()
    {
        var cut = Render(CreateMenuBarRoot(orientation: Orientation.Vertical, includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.GetAttribute("aria-orientation")!.ShouldBe("vertical");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientationAttribute()
    {
        var cut = Render(CreateMenuBarRoot(orientation: Orientation.Horizontal, includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.GetAttribute("data-orientation")!.ShouldBe("horizontal");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotSetDataDisabledOnRootWhenDisabled()
    {
        var cut = Render(CreateMenuBarRoot(disabled: true, includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.HasAttribute("data-disabled").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var attrs = new Dictionary<string, object> { ["data-testid"] = "my-menubar" };
        var cut = Render(CreateMenuBarRoot(additionalAttributes: attrs, includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.GetAttribute("data-testid")!.ShouldBe("my-menubar");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsProvidedId()
    {
        var attrs = new Dictionary<string, object> { ["id"] = "custom-menubar" };
        var cut = Render(CreateMenuBarRoot(additionalAttributes: attrs, includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.GetAttribute("id")!.ShouldBe("custom-menubar");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasGeneratedId()
    {
        var cut = Render(CreateMenuBarRoot(includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.GetAttribute("id").ShouldNotBeNullOrWhiteSpace();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        Func<MenuBarRootState, string> classValue = state => "custom-class";
        var cut = Render(CreateMenuBarRoot(classValue: classValue, includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.GetAttribute("class")!.ShouldContain("custom-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        Func<MenuBarRootState, string> styleValue = state => "color: red";
        var cut = Render(CreateMenuBarRoot(styleValue: styleValue, includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.GetAttribute("style")!.ShouldContain("color: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesContextToChildren()
    {
        var cut = Render(CreateMenuBarRoot());

        // MenuBarRoot should cascade context to child menus
        // Child menus should render their triggers
        var triggers = cut.FindAll("button[aria-haspopup='menu']");
        triggers.Count.ShouldBe(2);

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataModalWhenModal()
    {
        var cut = Render(CreateMenuBarRoot());
        var menubar = cut.Find("[role='menubar']");
        menubar.HasAttribute("data-modal").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotSetDataModalWhenModalFalse()
    {
        var cut = Render(CreateMenuBarRoot(modal: false, includeMenus: false));
        var menubar = cut.Find("[role='menubar']");
        menubar.HasAttribute("data-modal").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task TracksHasSubmenuOpenState()
    {
        // This test verifies that the MenuBarRoot tracks has-submenu-open state
        // In bUnit, the state tracking works through the context, but the
        // actual data attribute update requires full rendering cycles.
        // This test verifies the initial state and that the triggers render correctly.
        var cut = Render(CreateMenuBarRoot());

        var menubar = cut.Find("[role='menubar']");

        // Initially no submenu is open
        menubar.HasAttribute("data-has-submenu-open").ShouldBeFalse();

        // Pointerdown on trigger to open menu (must use TriggerEventAsync to properly await the async handler chain)
        var trigger = cut.Find("button[aria-haspopup='menu']");
        await trigger.TriggerEventAsync("onpointerdown", new Microsoft.AspNetCore.Components.Web.PointerEventArgs());

        // Force re-render since the open state propagates through a fixed cascading context
        cut.FindComponent<MenuTrigger>().Render();

        // Verify the menu opened (the trigger's aria-expanded should change)
        trigger = cut.Find("button[aria-haspopup='menu']");
        trigger.GetAttribute("aria-expanded")!.ShouldBe("true");

        // Note: The data-has-submenu-open attribute update happens through
        // MenuBarContext.SetHasSubmenuOpen which is called from MenuRoot.SetOpenAsync.
        // Full integration of this behavior is better tested via Playwright E2E tests.
    }
}
