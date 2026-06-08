namespace Blazix.BaseUI.Tests.Menu;

public class MenuRootTests : BunitContext, IMenuRootContract
{
    private readonly BunitJSModuleInterop menuModule;

    public MenuRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        menuModule = JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateMenuRoot(
        bool? open = null,
        bool defaultOpen = false,
        bool disabled = false,
        Blazix.BaseUI.Menu.MenuModalMode modal = Blazix.BaseUI.Menu.MenuModalMode.True,
        MenuOrientation orientation = MenuOrientation.Vertical,
        MenuRootActions? actionsRef = null,
        EventCallback<MenuOpenChangeEventArgs>? onOpenChange = null,
        EventCallback<bool>? onOpenChangeComplete = null,
        bool includeTrigger = true,
        bool includePositioner = true)
    {
        return builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            var attrIndex = 1;

            if (open.HasValue)
                builder.AddAttribute(attrIndex++, "Open", open.Value);
            builder.AddAttribute(attrIndex++, "DefaultOpen", defaultOpen);
            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            builder.AddAttribute(attrIndex++, "Modal", modal);
            builder.AddAttribute(attrIndex++, "Orientation", orientation);
            if (actionsRef is not null)
                builder.AddAttribute(attrIndex++, "ActionsRef", actionsRef);
            if (onOpenChange.HasValue)
                builder.AddAttribute(attrIndex++, "OnOpenChange", onOpenChange.Value);
            if (onOpenChangeComplete.HasValue)
                builder.AddAttribute(attrIndex++, "OnOpenChangeComplete", onOpenChangeComplete.Value);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => CreateChildContent(includeTrigger, includePositioner)));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateChildContent(bool includeTrigger = true, bool includePositioner = true)
    {
        return builder =>
        {
            if (includeTrigger)
            {
                builder.OpenComponent<MenuTrigger>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Open Menu")));
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
    public Task CascadesContextToChildren()
    {
        MenuTriggerState? capturedState = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                innerBuilder.AddAttribute(1, "ClassValue", (Func<MenuTriggerState, string>)(state =>
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
    public Task ControlledModeRespectsOpenParameter()
    {
        var cut = Render(CreateMenuRoot(open: false));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded")!.ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledModeUsesDefaultOpen()
    {
        var cut = Render(CreateMenuRoot(defaultOpen: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task InvokesOnOpenChangeWithReason()
    {
        var invoked = false;
        var receivedOpen = false;
        var receivedReason = MenuOpenChangeReason.None;

        var cut = Render(CreateMenuRoot(
            onOpenChange: EventCallback.Factory.Create<MenuOpenChangeEventArgs>(this, args =>
            {
                invoked = true;
                receivedOpen = args.Open;
                receivedReason = args.Reason;
            })
        ));

        var trigger = cut.Find("button");
        await trigger.TriggerEventAsync("onpointerdown", new Microsoft.AspNetCore.Components.Web.PointerEventArgs());

        invoked.ShouldBeTrue();
        receivedOpen.ShouldBeTrue();
        receivedReason.ShouldBe(MenuOpenChangeReason.TriggerPress);
    }

    [Fact]
    public Task InvokesOnOpenChangeComplete()
    {
        // OnOpenChangeComplete is invoked after transitions complete.
        // In bUnit tests without real JS, we can verify the callback is wired up.
        var cut = Render(CreateMenuRoot(
            defaultOpen: true,
            onOpenChangeComplete: EventCallback.Factory.Create<bool>(this, _ => { })
        ));

        // Menu should be open
        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledStatePreventsTriggerInteraction()
    {
        var cut = Render(CreateMenuRoot(disabled: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsModalModes()
    {
        var cutModal = Render(CreateMenuRoot(modal: Blazix.BaseUI.Menu.MenuModalMode.True));
        var cutNonModal = Render(CreateMenuRoot(modal: Blazix.BaseUI.Menu.MenuModalMode.False));

        // Both should render the trigger
        cutModal.Find("button").ShouldNotBeNull();
        cutNonModal.Find("button").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsOrientations()
    {
        var cutVertical = Render(CreateMenuRoot(orientation: MenuOrientation.Vertical));
        var cutHorizontal = Render(CreateMenuRoot(orientation: MenuOrientation.Horizontal));

        // Both should render the trigger
        cutVertical.Find("button").ShouldNotBeNull();
        cutHorizontal.Find("button").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsDirectionParameter()
    {
        var cut = Render<MenuRoot>(parameters => parameters
            .Add(p => p.Direction, Blazix.BaseUI.Direction.Rtl)
            .Add(p => p.ChildContent, _ => CreateChildContent()));

        cut.Find("button").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsSubmenuDirectionParameter()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuSubmenuRoot>(0);
                innerBuilder.AddAttribute(1, "Direction", Blazix.BaseUI.Direction.Rtl);
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(submenuBuilder =>
                {
                    submenuBuilder.OpenComponent<MenuSubmenuTrigger>(0);
                    submenuBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "More")));
                    submenuBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        cut.Find("[role='menuitem']").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ActionsRefProvidesCloseMethod()
    {
        var actions = new MenuRootActions();

        var cut = Render(CreateMenuRoot(
            defaultOpen: true,
            actionsRef: actions
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded")!.ShouldBe("true");

        actions.Close.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ChildContentReceivesPayloadContext()
    {
        MenuRootPayloadContext? capturedContext = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(ctx =>
            {
                capturedContext = ctx;
                return innerBuilder =>
                {
                    innerBuilder.OpenComponent<MenuTrigger>(0);
                    innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                    innerBuilder.CloseComponent();
                };
            }));
            builder.CloseComponent();
        });

        capturedContext.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DefaultOpenUsesDefaultTriggerIdPayload()
    {
        MenuRootPayloadContext? capturedContext = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "DefaultTriggerId", "trigger-2");
            builder.AddAttribute(3, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(ctx =>
            {
                capturedContext = ctx;
                return innerBuilder =>
                {
                    innerBuilder.OpenComponent<MenuTrigger>(0);
                    innerBuilder.AddAttribute(1, "id", "trigger-1");
                    innerBuilder.AddAttribute(2, "Payload", "one");
                    innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "One")));
                    innerBuilder.CloseComponent();

                    innerBuilder.OpenComponent<MenuTrigger>(10);
                    innerBuilder.AddAttribute(11, "id", "trigger-2");
                    innerBuilder.AddAttribute(12, "Payload", "two");
                    innerBuilder.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Two")));
                    innerBuilder.CloseComponent();
                };
            }));
            builder.CloseComponent();
        });

        cut.WaitForAssertion(() =>
        {
            capturedContext.ShouldNotBeNull();
            capturedContext.Value.Payload.ShouldBe("two");
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledTriggerIdChangeUpdatesPayload()
    {
        MenuRootPayloadContext? capturedContext = null;
        RenderFragment<MenuRootPayloadContext> childContent = ctx =>
        {
            capturedContext = ctx;
            return innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                innerBuilder.AddAttribute(1, "id", "trigger-1");
                innerBuilder.AddAttribute(2, "Payload", "one");
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "One")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<MenuTrigger>(10);
                innerBuilder.AddAttribute(11, "id", "trigger-2");
                innerBuilder.AddAttribute(12, "Payload", "two");
                innerBuilder.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Two")));
                innerBuilder.CloseComponent();
            };
        };

        var cut = Render<MenuRoot>(parameters => parameters
            .Add(p => p.Open, true)
            .Add(p => p.TriggerId, "trigger-1")
            .Add(p => p.ChildContent, childContent));

        cut.WaitForAssertion(() =>
        {
            capturedContext.ShouldNotBeNull();
            capturedContext.Value.Payload.ShouldBe("one");
        });

        cut.Render(parameters => parameters
            .Add(p => p.Open, true)
            .Add(p => p.TriggerId, "trigger-2")
            .Add(p => p.ChildContent, childContent));

        cut.WaitForAssertion(() =>
        {
            capturedContext.ShouldNotBeNull();
            capturedContext.Value.Payload.ShouldBe("two");
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledTriggerIdChangeClearsPayloadWhenTriggerIsMissing()
    {
        MenuRootPayloadContext? capturedContext = null;
        RenderFragment<MenuRootPayloadContext> childContent = ctx =>
        {
            capturedContext = ctx;
            return innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                innerBuilder.AddAttribute(1, "id", "trigger-1");
                innerBuilder.AddAttribute(2, "Payload", "one");
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "One")));
                innerBuilder.CloseComponent();
            };
        };

        var cut = Render<MenuRoot>(parameters => parameters
            .Add(p => p.Open, true)
            .Add(p => p.TriggerId, "trigger-1")
            .Add(p => p.ChildContent, childContent));

        cut.WaitForAssertion(() =>
        {
            capturedContext.ShouldNotBeNull();
            capturedContext.Value.Payload.ShouldBe("one");
        });

        cut.Render(parameters => parameters
            .Add(p => p.Open, true)
            .Add(p => p.TriggerId, "missing-trigger")
            .Add(p => p.ChildContent, childContent));

        cut.WaitForAssertion(() =>
        {
            capturedContext.ShouldNotBeNull();
            capturedContext.Value.Payload.ShouldBeNull();
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task HandleControlledTriggerIdChangeClearsPayloadWhenTriggerPayloadIsNull()
    {
        var handle = new MenuHandle<string?>();
        MenuRootPayloadContext? capturedContext = null;
        RenderFragment<MenuRootPayloadContext> childContent = ctx =>
        {
            capturedContext = ctx;
            return innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTypedTrigger<string?>>(0);
                innerBuilder.AddAttribute(1, "Handle", handle);
                innerBuilder.AddAttribute(2, "id", "trigger-1");
                innerBuilder.AddAttribute(3, "Payload", "one");
                innerBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "One")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<MenuTypedTrigger<string?>>(10);
                innerBuilder.AddAttribute(11, "Handle", handle);
                innerBuilder.AddAttribute(12, "id", "trigger-2");
                innerBuilder.AddAttribute(13, "Payload", (string?)null);
                innerBuilder.AddAttribute(14, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Two")));
                innerBuilder.CloseComponent();
            };
        };

        var cut = Render<MenuRoot>(parameters => parameters
            .Add(p => p.Handle, handle)
            .Add(p => p.Open, true)
            .Add(p => p.TriggerId, "trigger-1")
            .Add(p => p.ChildContent, childContent));

        cut.WaitForAssertion(() =>
        {
            capturedContext.ShouldNotBeNull();
            capturedContext.Value.Payload.ShouldBe("one");
        });

        cut.Render(parameters => parameters
            .Add(p => p.Handle, handle)
            .Add(p => p.Open, true)
            .Add(p => p.TriggerId, "trigger-2")
            .Add(p => p.ChildContent, childContent));

        cut.WaitForAssertion(() =>
        {
            capturedContext.ShouldNotBeNull();
            capturedContext.Value.Payload.ShouldBeNull();
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task HandleDefaultOpenUsesDefaultTriggerIdPayload()
    {
        var handle = new MenuHandle<string>();
        MenuRootPayloadContext? capturedContext = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "Handle", (IMenuHandle)handle);
            builder.AddAttribute(2, "DefaultOpen", true);
            builder.AddAttribute(3, "DefaultTriggerId", "trigger-2");
            builder.AddAttribute(4, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(ctx =>
            {
                capturedContext = ctx;
                return innerBuilder =>
                {
                    innerBuilder.OpenComponent<MenuTypedTrigger<string>>(0);
                    innerBuilder.AddAttribute(1, "Handle", handle);
                    innerBuilder.AddAttribute(2, "id", "trigger-1");
                    innerBuilder.AddAttribute(3, "Payload", "one");
                    innerBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "One")));
                    innerBuilder.CloseComponent();

                    innerBuilder.OpenComponent<MenuTypedTrigger<string>>(10);
                    innerBuilder.AddAttribute(11, "Handle", handle);
                    innerBuilder.AddAttribute(12, "id", "trigger-2");
                    innerBuilder.AddAttribute(13, "Payload", "two");
                    innerBuilder.AddAttribute(14, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Two")));
                    innerBuilder.CloseComponent();
                };
            }));
            builder.CloseComponent();
        });

        cut.WaitForAssertion(() =>
        {
            capturedContext.ShouldNotBeNull();
            capturedContext.Value.Payload.ShouldBe("two");
        });

        handle.ActiveTriggerId.ShouldBe("trigger-2");
        handle.Payload.ShouldBe("two");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task MultipleTriggersPassTheirOwnPayload()
    {
        MenuRootPayloadContext? capturedContext = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(ctx =>
            {
                capturedContext = ctx;
                return innerBuilder =>
                {
                    innerBuilder.OpenComponent<MenuTrigger>(0);
                    innerBuilder.AddAttribute(1, "id", "trigger-1");
                    innerBuilder.AddAttribute(2, "Payload", "one");
                    innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "One")));
                    innerBuilder.CloseComponent();

                    innerBuilder.OpenComponent<MenuTrigger>(10);
                    innerBuilder.AddAttribute(11, "id", "trigger-2");
                    innerBuilder.AddAttribute(12, "Payload", "two");
                    innerBuilder.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Two")));
                    innerBuilder.CloseComponent();
                };
            }));
            builder.CloseComponent();
        });

        var triggers = cut.FindAll("button");
        await triggers[1].TriggerEventAsync("onpointerdown", new Microsoft.AspNetCore.Components.Web.PointerEventArgs());

        cut.WaitForAssertion(() =>
        {
            capturedContext.ShouldNotBeNull();
            capturedContext.Value.Payload.ShouldBe("two");
        });
    }

    [Fact]
    public Task HandleTriggerSwitchUpdatesJavaScriptMenubarContext()
    {
        var handle = new MenuHandle<string>();

        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuBarRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(menuBarBuilder =>
            {
                menuBarBuilder.OpenComponent<MenuTypedTrigger<string>>(0);
                menuBarBuilder.AddAttribute(1, "Handle", handle);
                menuBarBuilder.AddAttribute(2, "id", "first-trigger");
                menuBarBuilder.AddAttribute(3, "Payload", "first");
                menuBarBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "First")));
                menuBarBuilder.CloseComponent();
            }));
            builder.CloseComponent();

            builder.OpenComponent<MenuBarRoot>(10);
            builder.AddAttribute(11, "ChildContent", (RenderFragment)(menuBarBuilder =>
            {
                menuBarBuilder.OpenComponent<MenuTypedTrigger<string>>(0);
                menuBarBuilder.AddAttribute(1, "Handle", handle);
                menuBarBuilder.AddAttribute(2, "id", "second-trigger");
                menuBarBuilder.AddAttribute(3, "Payload", "second");
                menuBarBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Second")));
                menuBarBuilder.CloseComponent();
            }));
            builder.CloseComponent();

            builder.OpenComponent<MenuRoot>(20);
            builder.AddAttribute(21, "Handle", (IMenuHandle)handle);
            builder.AddAttribute(22, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => menuBuilder =>
            {
                menuBuilder.OpenComponent<MenuPositioner>(0);
                menuBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(positionerBuilder =>
                {
                    positionerBuilder.OpenComponent<MenuPopup>(0);
                    positionerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item")));
                        popupBuilder.CloseComponent();
                    }));
                    positionerBuilder.CloseComponent();
                }));
                menuBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        handle.Open("first-trigger");
        cut.Render();

        var initialRootInvocation = menuModule.Invocations.Last(invocation => invocation.Identifier == "initializeRoot");
        var initialMenubarElement = initialRootInvocation.Arguments[5];
        var updateRootCountBefore = menuModule.Invocations.Count(invocation => invocation.Identifier == "updateRoot");

        handle.Open("second-trigger");
        cut.Render();

        cut.WaitForAssertion(() =>
        {
            var updateRootInvocations = menuModule.Invocations
                .Skip(updateRootCountBefore)
                .Where(invocation => invocation.Identifier == "updateRoot")
                .ToList();

            updateRootInvocations.Count.ShouldBeGreaterThan(0);
            var updateRootInvocation = updateRootInvocations[^1];
            updateRootInvocation.Arguments.Count.ShouldBeGreaterThan(7);
            updateRootInvocation.Arguments[6].ShouldNotBe(initialMenubarElement);
            updateRootInvocation.Arguments[7].ShouldBe("menubar");
        });

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ControlledTriggerIdSwitchUpdatesJavaScriptMenubarContext()
    {
        var handle = new MenuHandle<string>();
        RenderFragment<MenuRootPayloadContext> menuContent = _ => menuBuilder =>
        {
            menuBuilder.OpenComponent<MenuPositioner>(0);
            menuBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(positionerBuilder =>
            {
                positionerBuilder.OpenComponent<MenuPopup>(0);
                positionerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                {
                    popupBuilder.OpenComponent<MenuItem>(0);
                    popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item")));
                    popupBuilder.CloseComponent();
                }));
                positionerBuilder.CloseComponent();
            }));
            menuBuilder.CloseComponent();
        };

        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuBarRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(menuBarBuilder =>
            {
                menuBarBuilder.OpenComponent<MenuTypedTrigger<string>>(0);
                menuBarBuilder.AddAttribute(1, "Handle", handle);
                menuBarBuilder.AddAttribute(2, "id", "first-trigger");
                menuBarBuilder.AddAttribute(3, "Payload", "first");
                menuBarBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "First")));
                menuBarBuilder.CloseComponent();
            }));
            builder.CloseComponent();

            builder.OpenComponent<MenuBarRoot>(10);
            builder.AddAttribute(11, "ChildContent", (RenderFragment)(menuBarBuilder =>
            {
                menuBarBuilder.OpenComponent<MenuTypedTrigger<string>>(0);
                menuBarBuilder.AddAttribute(1, "Handle", handle);
                menuBarBuilder.AddAttribute(2, "id", "second-trigger");
                menuBarBuilder.AddAttribute(3, "Payload", "second");
                menuBarBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Second")));
                menuBarBuilder.CloseComponent();
            }));
            builder.CloseComponent();

            builder.OpenComponent<MenuRoot>(20);
            builder.AddAttribute(21, "Handle", (IMenuHandle)handle);
            builder.AddAttribute(22, "Open", true);
            builder.AddAttribute(23, "TriggerId", "first-trigger");
            builder.AddAttribute(24, "ChildContent", menuContent);
            builder.CloseComponent();
        });

        var initialRootInvocation = menuModule.Invocations.Last(invocation => invocation.Identifier == "initializeRoot");
        var initialMenubarElement = initialRootInvocation.Arguments[5];
        var updateRootCountBefore = menuModule.Invocations.Count(invocation => invocation.Identifier == "updateRoot");

        var root = cut.FindComponent<MenuRoot>();
        root.Render(parameters => parameters
            .Add(p => p.Handle, (IMenuHandle)handle)
            .Add(p => p.Open, true)
            .Add(p => p.TriggerId, "second-trigger")
            .Add(p => p.ChildContent, menuContent));

        handle.ActiveTriggerId.ShouldBe("second-trigger");

        var updateRootInvocations = new List<JSRuntimeInvocation>();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            updateRootInvocations = menuModule.Invocations
                .Skip(updateRootCountBefore)
                .Where(invocation => invocation.Identifier == "updateRoot")
                .ToList();

            if (updateRootInvocations.Count > 0)
            {
                break;
            }

            await Task.Delay(25);
        }

        updateRootInvocations.Count.ShouldBeGreaterThan(0);
        var updateRootInvocation = updateRootInvocations[^1];
        updateRootInvocation.Arguments.Count.ShouldBeGreaterThan(7);
        updateRootInvocation.Arguments[6].ShouldNotBe(initialMenubarElement);
        updateRootInvocation.Arguments[7].ShouldBe("menubar");
    }
}
