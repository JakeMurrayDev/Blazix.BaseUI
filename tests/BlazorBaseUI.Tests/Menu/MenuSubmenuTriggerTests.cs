namespace BlazorBaseUI.Tests.Menu;

public class MenuSubmenuTriggerTests : BunitContext, IMenuSubmenuTriggerContract
{
    public MenuSubmenuTriggerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateSubmenuTriggerInRoot(
        bool parentDefaultOpen = true,
        bool submenuDefaultOpen = false,
        bool triggerDisabled = false,
        bool openOnHover = true,
        RenderFragment<RenderProps<MenuSubmenuTriggerState>>? render = null,
        bool nativeButton = false)
    {
        return builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", parentDefaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Main Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<MenuPositioner>(2);
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item 1")));
                        popupBuilder.CloseComponent();

                        popupBuilder.OpenComponent<MenuSubmenuRoot>(2);
                        popupBuilder.AddAttribute(3, "DefaultOpen", submenuDefaultOpen);
                        popupBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(submenuBuilder =>
                        {
                            submenuBuilder.OpenComponent<MenuSubmenuTrigger>(0);
                            var attrIndex = 1;

                            if (triggerDisabled)
                                submenuBuilder.AddAttribute(attrIndex++, "Disabled", true);
                            if (nativeButton)
                                submenuBuilder.AddAttribute(attrIndex++, "NativeButton", true);
                            submenuBuilder.AddAttribute(attrIndex++, "OpenOnHover", openOnHover);
                            if (render is not null)
                                submenuBuilder.AddAttribute(attrIndex++, "Render", render);
                            submenuBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Submenu")));
                            submenuBuilder.CloseComponent();

                            submenuBuilder.OpenComponent<MenuPositioner>(10);
                            submenuBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(subPosBuilder =>
                            {
                                subPosBuilder.OpenComponent<MenuPopup>(0);
                                subPosBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(subPopupBuilder =>
                                {
                                    subPopupBuilder.OpenComponent<MenuItem>(0);
                                    subPopupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Sub Item 1")));
                                    subPopupBuilder.CloseComponent();
                                }));
                                subPosBuilder.CloseComponent();
                            }));
                            submenuBuilder.CloseComponent();
                        }));
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
        var cut = Render(CreateSubmenuTriggerInRoot());

        // Find submenu trigger by looking for element with both role=menuitem and aria-haspopup
        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<MenuSubmenuTriggerState>> renderAsSpan = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback!);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateSubmenuTriggerInRoot(render: renderAsSpan));

        var submenuTrigger = cut.Find("span[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsNativeButtonMode()
    {
        var cut = Render(CreateSubmenuTriggerInRoot(nativeButton: true));

        var submenuTrigger = cut.Find("button[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.GetAttribute("type")!.ShouldBe("button");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaHaspopupMenu()
    {
        var cut = Render(CreateSubmenuTriggerInRoot());

        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup]");
        submenuTrigger.GetAttribute("aria-haspopup")!.ShouldBe("menu");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedFalseWhenClosed()
    {
        var cut = Render(CreateSubmenuTriggerInRoot(submenuDefaultOpen: false));

        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.GetAttribute("aria-expanded")!.ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedTrueWhenOpen()
    {
        var cut = Render(CreateSubmenuTriggerInRoot(submenuDefaultOpen: true));

        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.GetAttribute("aria-expanded")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataPopupOpenWhenOpen()
    {
        var cut = Render(CreateSubmenuTriggerInRoot(submenuDefaultOpen: true));

        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.HasAttribute("data-popup-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataPopupOpenWhenClosed()
    {
        var cut = Render(CreateSubmenuTriggerInRoot(submenuDefaultOpen: false));

        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.HasAttribute("data-popup-open").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateSubmenuTriggerInRoot(triggerDisabled: true));

        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaDisabledWhenDisabled()
    {
        var cut = Render(CreateSubmenuTriggerInRoot(triggerDisabled: true));

        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.GetAttribute("aria-disabled").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresSubmenuContext()
    {
        // MenuSubmenuTrigger throws when not inside a MenuSubmenuRoot
        Should.Throw<InvalidOperationException>(() =>
        {
            Render(builder =>
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
                            // MenuSubmenuTrigger without MenuSubmenuRoot
                            popupBuilder.OpenComponent<MenuSubmenuTrigger>(0);
                            popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Invalid Submenu")));
                            popupBuilder.CloseComponent();
                        }));
                        posBuilder.CloseComponent();
                    }));
                    innerBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            });
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task CloseDelayDefaultsToZero()
    {
        var cut = Render(CreateSubmenuTriggerInRoot());

        var triggerComponent = cut.FindComponent<MenuSubmenuTrigger>();
        triggerComponent.Instance.CloseDelay.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task HighlightsOnMouseEnter()
    {
        var cut = Render(CreateSubmenuTriggerInRoot());

        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.MouseEnter();

        submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.HasAttribute("data-highlighted").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotToggleOnClickWhenOpenOnHover()
    {
        var cut = Render(CreateSubmenuTriggerInRoot(submenuDefaultOpen: false, openOnHover: true));

        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.Click();

        submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.GetAttribute("aria-expanded")!.ShouldBe("false");

        return Task.CompletedTask;
    }
}
