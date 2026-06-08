namespace Blazix.BaseUI.Tests.Menu;

public class MenuTriggerTests : BunitContext, IMenuTriggerContract
{
    public MenuTriggerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateTriggerInRoot(
        bool defaultOpen = false,
        bool disabled = false,
        bool triggerDisabled = false,
        Func<MenuTriggerState, string>? classValue = null,
        Func<MenuTriggerState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<MenuTriggerState>>? render = null,
        bool includePositioner = true,
        bool nativeButton = true)
    {
        return builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            if (disabled)
                builder.AddAttribute(2, "Disabled", true);
            builder.AddAttribute(3, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                var attrIndex = 1;

                if (triggerDisabled)
                    innerBuilder.AddAttribute(attrIndex++, "Disabled", true);
                if (!nativeButton)
                    innerBuilder.AddAttribute(attrIndex++, "NativeButton", false);
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                if (render is not null)
                    innerBuilder.AddAttribute(attrIndex++, "Render", render);
                innerBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                if (includePositioner)
                {
                    innerBuilder.OpenComponent<MenuPositioner>(10);
                    innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<MenuPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<MenuItem>(0);
                            popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item")));
                            popupBuilder.CloseComponent();
                        }));
                        posBuilder.CloseComponent();
                    }));
                    innerBuilder.CloseComponent();
                }
            }));
            builder.CloseComponent();
        };
    }

    private static MenuBarRootContext CreateMenuBarContext()
    {
        return new MenuBarRootContext
        {
            RegisterItem = _ => { },
            UnregisterItem = _ => { },
            SetHasSubmenuOpen = (_, _) => { },
            GetHasSubmenuOpen = () => false,
            GetElement = () => null
        };
    }

    [Fact]
    public Task RendersAsButtonByDefault()
    {
        var cut = Render(CreateTriggerInRoot());

        var trigger = cut.Find("button");
        trigger.TagName.ShouldBe("BUTTON");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<MenuTriggerState>> renderAsDiv = props => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback!);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateTriggerInRoot(render: renderAsDiv));

        var trigger = cut.Find("div[aria-expanded]");
        trigger.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateTriggerInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "trigger" },
                { "aria-label", "Open menu" }
            }
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("data-testid")!.ShouldBe("trigger");
        trigger.GetAttribute("aria-label")!.ShouldBe("Open menu");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaHaspopupMenu()
    {
        var cut = Render(CreateTriggerInRoot());

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-haspopup")!.ShouldBe("menu");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedFalseWhenClosed()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded")!.ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedTrueWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataPopupOpenWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-popup-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDisabledWhenDisabled()
    {
        var cut = Render(CreateTriggerInRoot(disabled: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("disabled").ShouldBeTrue();
        trigger.GetAttribute("aria-disabled")!.ShouldBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreateTriggerInRoot(
            defaultOpen: true,
            classValue: state => state.Open ? "open-class" : "closed-class"
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreateTriggerInRoot(
            styleValue: _ => "color: red"
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("style")!.ShouldContain("color: red");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ToggleMenuOnPointerDown()
    {
        bool userHandlerCalled = false;
        var cut = Render(CreateTriggerInRoot(defaultOpen: false,
            additionalAttributes: new Dictionary<string, object>
            {
                { "onclick", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(
                    new object(), e => { userHandlerCalled = true; return Task.CompletedTask; }) }
            }));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded")!.ShouldBe("false");

        await trigger.TriggerEventAsync("onpointerdown", new Microsoft.AspNetCore.Components.Web.PointerEventArgs());
        cut.FindComponent<MenuTrigger>().Render();

        trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded")!.ShouldBe("true");

        // User click handler still fires on the subsequent click
        await trigger.TriggerEventAsync("onclick", new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        userHandlerCalled.ShouldBeTrue();
    }

    [Fact]
    public Task DoesNotToggleWhenDisabled()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false, disabled: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded")!.ShouldBe("false");

        trigger.Click();

        trigger.GetAttribute("aria-expanded")!.ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        // MenuTrigger renders nothing when context is missing
        var cut = Render<MenuTrigger>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Toggle"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CloseDelayDefaultsToZero()
    {
        var cut = Render(CreateTriggerInRoot());

        var triggerComponent = cut.FindComponent<MenuTrigger>();
        triggerComponent.Instance.CloseDelay.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task HandleBasedTriggerRegistersOnRender()
    {
        var handle = new MenuHandle();

        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "Handle", (IMenuHandle)handle);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuPositioner>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();

            builder.OpenComponent<MenuTrigger>(10);
            builder.AddAttribute(11, "Handle", (IMenuHandle)handle);
            builder.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "External Trigger")));
            builder.CloseComponent();
        });

        // The trigger should render and have aria-haspopup
        var buttons = cut.FindAll("button[aria-haspopup='menu']");
        buttons.Count.ShouldBeGreaterThan(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsNonNativeButtonMode()
    {
        var cut = Render(CreateTriggerInRoot(nativeButton: false));

        var trigger = cut.Find("div[role='button'][aria-haspopup='menu']");
        trigger.HasAttribute("type").ShouldBeFalse();
        trigger.GetAttribute("tabindex")!.ShouldBe("0");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HandleOpenUsesExplicitTriggerIdAndPayload()
    {
        var handle = new MenuHandle();
        MenuRootPayloadContext? capturedPayloadContext = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuTrigger>(0);
            builder.AddAttribute(1, "Handle", (IMenuHandle)handle);
            builder.AddAttribute(2, "id", "detached-settings");
            builder.AddAttribute(3, "Payload", "Settings payload");
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Settings")));
            builder.CloseComponent();

            builder.OpenComponent<MenuRoot>(10);
            builder.AddAttribute(11, "Handle", (IMenuHandle)handle);
            builder.AddAttribute(12, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(ctx =>
            {
                capturedPayloadContext = ctx;
                return innerBuilder =>
                {
                    innerBuilder.OpenComponent<MenuPositioner>(0);
                    innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<MenuPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<MenuItem>(0);
                            popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, ctx.Payload?.ToString())));
                            popupBuilder.CloseComponent();
                        }));
                        posBuilder.CloseComponent();
                    }));
                    innerBuilder.CloseComponent();
                };
            }));
            builder.CloseComponent();
        });

        var trigger = cut.Find("button#detached-settings");
        trigger.GetAttribute("aria-expanded")!.ShouldBe("false");

        handle.Open("detached-settings");
        cut.Render();

        handle.IsOpen.ShouldBeTrue();
        handle.ActiveTriggerId.ShouldBe("detached-settings");
        handle.Payload.ShouldBe("Settings payload");

        trigger = cut.Find("button#detached-settings");
        trigger.GetAttribute("aria-expanded")!.ShouldBe("true");
        cut.WaitForAssertion(() =>
        {
            capturedPayloadContext.ShouldNotBeNull();
            capturedPayloadContext.Value.Payload.ShouldBe("Settings payload");
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task HandleBasedTriggerReregistersWhenIdChanges()
    {
        var handle = new MenuHandle();

        var cut = Render<MenuTrigger>(parameters => parameters
            .Add(p => p.Handle, (IMenuHandle)handle)
            .Add(p => p.Payload, "Payload A")
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> { { "id", "detached-a" } })
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "External Trigger")));

        handle.Open("detached-a");
        handle.Payload.ShouldBe("Payload A");

        cut.Render(parameters => parameters
            .Add(p => p.Handle, (IMenuHandle)handle)
            .Add(p => p.Payload, "Payload B")
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> { { "id", "detached-b" } })
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "External Trigger")));

        Should.Throw<InvalidOperationException>(() => handle.Open("detached-a"));
        handle.Open("detached-b");
        handle.Payload.ShouldBe("Payload B");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HandleBasedTriggerReregistersMenubarContextWhenCascadeChanges()
    {
        var handle = new MenuHandle();
        var firstContext = CreateMenuBarContext();
        var secondContext = CreateMenuBarContext();

        RenderFragment childContent = childBuilder =>
        {
            childBuilder.OpenComponent<MenuTrigger>(0);
            childBuilder.AddAttribute(1, "Handle", (IMenuHandle)handle);
            childBuilder.AddAttribute(2, "id", "detached-trigger");
            childBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "External Trigger")));
            childBuilder.CloseComponent();
        };

        var cut = Render<CascadingValue<MenuBarRootContext>>(parameters => parameters
            .Add(p => p.Value, firstContext)
            .Add(p => p.ChildContent, childContent));

        handle.MenubarContext.ShouldBeSameAs(firstContext);

        cut.Render(parameters => parameters
            .Add(p => p.Value, secondContext)
            .Add(p => p.ChildContent, childContent));

        handle.MenubarContext.ShouldBeSameAs(secondContext);

        return Task.CompletedTask;
    }

    [Fact]
    public Task TypedHandleOpenUsesExplicitTriggerIdAndPayload()
    {
        var handle = new MenuHandle<string>();
        MenuRootPayloadContext? capturedPayloadContext = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuTypedTrigger<string>>(0);
            builder.AddAttribute(1, "Handle", handle);
            builder.AddAttribute(2, "id", "detached-profile");
            builder.AddAttribute(3, "Payload", "Profile payload");
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Profile")));
            builder.CloseComponent();

            builder.OpenComponent<MenuRoot>(10);
            builder.AddAttribute(11, "Handle", (IMenuHandle)handle);
            builder.AddAttribute(12, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(ctx =>
            {
                capturedPayloadContext = ctx;
                return innerBuilder =>
                {
                    innerBuilder.OpenComponent<MenuPositioner>(0);
                    innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<MenuPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<MenuItem>(0);
                            popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, ctx.Payload?.ToString())));
                            popupBuilder.CloseComponent();
                        }));
                        posBuilder.CloseComponent();
                    }));
                    innerBuilder.CloseComponent();
                };
            }));
            builder.CloseComponent();
        });

        var trigger = cut.Find("button#detached-profile");
        trigger.GetAttribute("aria-expanded")!.ShouldBe("false");

        handle.Open("detached-profile");
        cut.Render();

        handle.IsOpen.ShouldBeTrue();
        handle.ActiveTriggerId.ShouldBe("detached-profile");
        handle.Payload.ShouldBe("Profile payload");

        trigger = cut.Find("button#detached-profile");
        trigger.GetAttribute("aria-expanded")!.ShouldBe("true");
        cut.WaitForAssertion(() =>
        {
            capturedPayloadContext.ShouldNotBeNull();
            capturedPayloadContext.Value.Payload.ShouldBe("Profile payload");
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task TypedHandleBasedTriggerReregistersWhenIdChanges()
    {
        var handle = new MenuHandle<string>();

        var cut = Render<MenuTypedTrigger<string>>(parameters => parameters
            .Add(p => p.Handle, handle)
            .Add(p => p.Payload, "Payload A")
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> { { "id", "detached-a" } })
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "External Trigger")));

        handle.Open("detached-a");
        handle.Payload.ShouldBe("Payload A");

        cut.Render(parameters => parameters
            .Add(p => p.Handle, handle)
            .Add(p => p.Payload, "Payload B")
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> { { "id", "detached-b" } })
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "External Trigger")));

        Should.Throw<InvalidOperationException>(() => handle.Open("detached-a"));
        handle.Open("detached-b");
        handle.Payload.ShouldBe("Payload B");

        return Task.CompletedTask;
    }

    [Fact]
    public Task TypedHandleBasedTriggerReregistersMenubarContextWhenCascadeChanges()
    {
        var handle = new MenuHandle<string>();
        var firstContext = CreateMenuBarContext();
        var secondContext = CreateMenuBarContext();

        RenderFragment childContent = childBuilder =>
        {
            childBuilder.OpenComponent<MenuTypedTrigger<string>>(0);
            childBuilder.AddAttribute(1, "Handle", handle);
            childBuilder.AddAttribute(2, "id", "detached-trigger");
            childBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "External Trigger")));
            childBuilder.CloseComponent();
        };

        var cut = Render<CascadingValue<MenuBarRootContext>>(parameters => parameters
            .Add(p => p.Value, firstContext)
            .Add(p => p.ChildContent, childContent));

        handle.MenubarContext.ShouldBeSameAs(firstContext);

        cut.Render(parameters => parameters
            .Add(p => p.Value, secondContext)
            .Add(p => p.ChildContent, childContent));

        handle.MenubarContext.ShouldBeSameAs(secondContext);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task HandleBasedTriggerUnregistersOnDispose()
    {
        var handle = new MenuHandle();

        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "Handle", (IMenuHandle)handle);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuPositioner>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();

            builder.OpenComponent<MenuTrigger>(10);
            builder.AddAttribute(11, "Handle", (IMenuHandle)handle);
            builder.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "External Trigger")));
            builder.CloseComponent();
        });

        // Disposing should not throw
        await cut.FindComponent<MenuTrigger>().Instance.DisposeAsync();
    }
}
