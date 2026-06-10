namespace Blazix.BaseUI.Tests.Menu;

public class MenuItemTests : BunitContext, IMenuItemContract
{
    public MenuItemTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateMenuItemInRoot(
        bool defaultOpen = true,
        bool itemDisabled = false,
        bool closeOnClick = true,
        RenderFragment<RenderProps<MenuItemState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        bool nativeButton = false)
    {
        return builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<MenuPositioner>(2);
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        var attrIndex = 1;

                        if (itemDisabled)
                            popupBuilder.AddAttribute(attrIndex++, "Disabled", true);
                        if (nativeButton)
                            popupBuilder.AddAttribute(attrIndex++, "NativeButton", true);
                        popupBuilder.AddAttribute(attrIndex++, "CloseOnClick", closeOnClick);
                        if (render is not null)
                            popupBuilder.AddAttribute(attrIndex++, "Render", render);
                        if (additionalAttributes is not null)
                            popupBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                        popupBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item 1")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateMenuItemInRoot());

        var item = cut.Find("[role='menuitem']");
        item.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<MenuItemState>> renderAsSpan = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback!);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateMenuItemInRoot(render: renderAsSpan));

        var item = cut.Find("span[role='menuitem']");
        item.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsNativeButtonMode()
    {
        var cut = Render(CreateMenuItemInRoot(nativeButton: true));

        var item = cut.Find("button[role='menuitem']");
        item.GetAttribute("type")!.ShouldBe("button");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ResolvesIdFromAdditionalAttributes()
    {
        var cut = Render(CreateMenuItemInRoot(
            additionalAttributes: new Dictionary<string, object> { ["id"] = "item-from-attrs" }));

        var item = cut.Find("[role='menuitem']");
        item.GetAttribute("id")!.ShouldBe("item-from-attrs");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateMenuItemInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "my-item" },
                { "aria-label", "Custom label" }
            }
        ));

        var item = cut.Find("[role='menuitem']");
        item.GetAttribute("data-testid")!.ShouldBe("my-item");
        item.GetAttribute("aria-label")!.ShouldBe("Custom label");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<MenuPositioner>(2);
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        popupBuilder.AddAttribute(1, "ClassValue", (Func<MenuItemState, string?>)(s => s.Disabled ? "disabled-class" : "enabled-class"));
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var item = cut.Find("[role='menuitem']");
        item.ClassList.ShouldContain("enabled-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<MenuPositioner>(2);
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        popupBuilder.AddAttribute(1, "StyleValue", (Func<MenuItemState, string?>)(_ => "color: red"));
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var item = cut.Find("[role='menuitem']");
        item.GetAttribute("style")!.ShouldContain("color: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRoleMenuitem()
    {
        var cut = Render(CreateMenuItemInRoot());

        var item = cut.Find("[role='menuitem']");
        item.GetAttribute("role")!.ShouldBe("menuitem");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabindexMinusOneByDefault()
    {
        var cut = Render(CreateMenuItemInRoot());

        var item = cut.Find("[role='menuitem']");
        item.GetAttribute("tabindex")!.ShouldBe("-1");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateMenuItemInRoot(itemDisabled: true));

        var item = cut.Find("[role='menuitem']");
        item.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaDisabledWhenDisabled()
    {
        var cut = Render(CreateMenuItemInRoot(itemDisabled: true));

        var item = cut.Find("[role='menuitem']");
        item.GetAttribute("aria-disabled")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnClickHandler()
    {
        var clicked = false;

        var cut = Render(CreateMenuItemInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, _ => clicked = true) }
            }
        ));

        var item = cut.Find("[role='menuitem']");
        item.Click();

        clicked.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ClosesMenuOnClickByDefault()
    {
        var closeEmitted = false;

        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "OnOpenChange", EventCallback.Factory.Create<MenuOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open && args.Reason == MenuOpenChangeReason.ItemPress)
                {
                    closeEmitted = true;
                }
            }));
            builder.AddAttribute(3, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<MenuPositioner>(2);
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        popupBuilder.AddAttribute(1, "CloseOnClick", true);
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item 1")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var item = cut.Find("[role='menuitem']");
        item.Click();

        // Verify that close was emitted with ItemPress reason
        closeEmitted.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotCloseWhenCloseOnClickFalse()
    {
        var cut = Render(CreateMenuItemInRoot(closeOnClick: false));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded")!.ShouldBe("true");

        var item = cut.Find("[role='menuitem']");
        item.Click();

        // Menu should remain open
        trigger.GetAttribute("aria-expanded")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotActivateWhenDisabled()
    {
        var clicked = false;

        var cut = Render(CreateMenuItemInRoot(
            itemDisabled: true,
            additionalAttributes: new Dictionary<string, object>
            {
                { "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, _ => clicked = true) }
            }
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded")!.ShouldBe("true");

        var item = cut.Find("[role='menuitem']");
        item.Click();

        // Menu should remain open (no close action for disabled items)
        trigger.GetAttribute("aria-expanded")!.ShouldBe("true");
        // User onclick should not be invoked when item logic is blocked
        // Note: The EventUtilities.InvokeOnClickAsync is called after the disabled check returns
        clicked.ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataHighlightedOnMouseEnter()
    {
        var cut = Render(CreateMenuItemInRoot());

        var item = cut.Find("[role='menuitem']");
        item.HasAttribute("data-highlighted").ShouldBeFalse();

        item.MouseEnter();

        item.HasAttribute("data-highlighted").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsLabelAsDataAttribute()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<MenuPositioner>(2);
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        popupBuilder.AddAttribute(1, "Label", "custom-label");
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var item = cut.Find("[role='menuitem']");
        item.GetAttribute("data-label")!.ShouldBe("custom-label");

        return Task.CompletedTask;
    }
}
