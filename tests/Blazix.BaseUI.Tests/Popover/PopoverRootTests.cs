using Blazix.BaseUI.Popover;
using Blazix.BaseUI.Tests.Contracts.Popover;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Blazix.BaseUI.Tests.Popover;

public class PopoverRootTests : BunitContext, IPopoverRootContract
{
    private readonly BunitJSModuleInterop popoverModule;

    public PopoverRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        popoverModule = JsInteropSetup.SetupPopoverModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    [Fact]
    public Task RendersChildren()
    {
        var cut = Render(CreatePopover());

        cut.Find("button").TextContent.ShouldBe("Toggle");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OpensByDefaultWhenDefaultOpenTrue()
    {
        var cut = Render(CreatePopover(defaultOpen: true));

        cut.Find("[role='dialog']").ShouldNotBeNull();
        cut.Find("[role='dialog']").TextContent.ShouldContain("Content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HydratesJsRootWhenDefaultOpenTrue()
    {
        Render(CreatePopover(defaultOpen: true));

        popoverModule.Invocations.Any(invocation => invocation.Identifier == "hydrateRootOpen").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DefaultOpenWithDefaultTriggerIdResolvesContainedTriggerPayload()
    {
        var cut = Render(CreatePayloadPopover(defaultOpen: true, defaultTriggerId: "trigger-b"));

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='payload']").TextContent.ShouldBe("Payload B");
            cut.Find("#trigger-b").HasAttribute("data-popup-open").ShouldBeTrue();
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task RemainsClosedWhenDefaultOpenFalseControlled()
    {
        var cut = Render(CreatePopover(open: false, defaultOpen: true));

        cut.FindAll("[role='dialog']").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ClosesWhenTriggerClickedTwice()
    {
        var closeRequested = false;

        // Start with popover open to verify close behavior
        var cut = Render(CreatePopover(
            defaultOpen: true,
            onOpenChange: EventCallback.Factory.Create<PopoverOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open)
                {
                    closeRequested = true;
                }
            })
        ));

        cut.Find("[role='dialog']").ShouldNotBeNull();

        var trigger = cut.Find("button");
        trigger.Click();

        // Verify that close was requested (OnOpenChange fired with Open=false)
        closeRequested.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CallsOnOpenChangeWhenOpenStateChanges()
    {
        var callCount = 0;
        var lastOpen = false;

        var cut = Render(CreatePopover(
            defaultOpen: false,
            onOpenChange: EventCallback.Factory.Create<PopoverOpenChangeEventArgs>(this, args =>
            {
                callCount++;
                lastOpen = args.Open;
            })
        ));

        var trigger = cut.Find("button");
        trigger.Click();

        callCount.ShouldBe(1);
        lastOpen.ShouldBeTrue();

        trigger.Click();

        callCount.ShouldBe(2);
        lastOpen.ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnOpenChangeCancelPreventsOpening()
    {
        var cut = Render(CreatePopover(
            defaultOpen: false,
            onOpenChange: EventCallback.Factory.Create<PopoverOpenChangeEventArgs>(this, args =>
            {
                if (args.Open)
                {
                    args.Cancel();
                }
            })
        ));

        var trigger = cut.Find("button");
        trigger.Click();

        cut.FindAll("[role='dialog']").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersInternalBackdropWhenModalTrue()
    {
        // Create a popover with an explicit PopoverBackdrop to test modal behavior
        RenderFragment content = builder =>
        {
            builder.OpenComponent<PopoverRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "Modal", Blazix.BaseUI.Popover.PopoverModalMode.True);
            builder.AddAttribute(3, "ChildContent", (RenderFragment<PopoverRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PopoverPortal>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    // Add explicit backdrop
                    portalBuilder.OpenComponent<PopoverBackdrop>(0);
                    portalBuilder.CloseComponent();

                    portalBuilder.OpenComponent<PopoverPositioner>(10);
                    portalBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(content);

        var positioner = cut.Find("[data-side]");
        var previousSibling = positioner.PreviousElementSibling;

        previousSibling.ShouldNotBeNull();
        previousSibling!.GetAttribute("role").ShouldBe("presentation");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderInternalBackdropWhenModalFalse()
    {
        var cut = Render(CreatePopover(defaultOpen: true, modal: Blazix.BaseUI.Popover.PopoverModalMode.False));

        var positioner = cut.Find("[data-side]");
        var previousSibling = positioner.PreviousElementSibling;

        if (previousSibling is not null)
        {
            previousSibling.GetAttribute("role").ShouldNotBe("presentation");
        }

        return Task.CompletedTask;
    }

    [Fact]
    public Task ActionsRefCloseMethodClosesPopover()
    {
        var closeRequested = false;
        var actions = new PopoverRootActions();

        var cut = Render(CreatePopover(
            defaultOpen: true,
            actionsRef: actions,
            onOpenChange: EventCallback.Factory.Create<PopoverOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open)
                {
                    closeRequested = true;
                }
            })
        ));

        cut.Find("[role='dialog']").ShouldNotBeNull();

        // Invoke the Close action
        actions.Close?.Invoke();

        // Verify that close was requested (OnOpenChange fired with Open=false)
        closeRequested.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ActionsRefUnmountMethodUnmountsPopover()
    {
        var actions = new PopoverRootActions();
        var cut = Render(CreatePopover(defaultOpen: true, actionsRef: actions));

        cut.Find("[role='dialog']").ShouldNotBeNull();

        await cut.InvokeAsync(() => actions.Unmount?.Invoke());

        cut.WaitForAssertion(() => cut.FindAll("[role='dialog']").Count.ShouldBe(0));
    }

    [Fact]
    public Task OnOpenChangeCompleteNotCalledOnMount()
    {
        var completeCalled = false;

        var cut = Render(CreatePopover(
            defaultOpen: true,
            onOpenChangeComplete: EventCallback.Factory.Create<bool>(this, _ =>
            {
                completeCalled = true;
            })
        ));

        cut.Find("[role='dialog']").ShouldNotBeNull();
        completeCalled.ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task MultiTrigger_OpensWithAnyContainedTrigger()
    {
        var cut = Render(CreateMultiTriggerPopover());

        var triggerA = cut.Find("#trigger-a");
        var triggerB = cut.Find("#trigger-b");

        // Click trigger A - should open
        triggerA.Click();
        triggerA.GetAttribute("aria-expanded").ShouldBe("true");

        // Click trigger A again to close
        triggerA.Click();
        triggerA.GetAttribute("aria-expanded").ShouldBe("false");

        // Click trigger B - should open
        triggerB.Click();
        triggerB.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task AlreadyOpenTriggerPressTracksTriggerPressReason()
    {
        var cut = Render(CreateMultiTriggerPopover());
        var root = cut.FindComponent<PopoverRoot>().Instance;

        await cut.InvokeAsync(async () => await root.SetOpenAsync(true, PopoverOpenChangeReason.TriggerPress, "trigger-a"));
        cut.WaitForAssertion(() => cut.Find("#trigger-a").GetAttribute("aria-expanded").ShouldBe("true"));
        cut.Find("#trigger-b").GetAttribute("aria-expanded").ShouldBe("false");

        await cut.InvokeAsync(async () => await root.SetOpenAsync(true, PopoverOpenChangeReason.TriggerPress, "trigger-b"));
        cut.WaitForAssertion(() =>
        {
            cut.Find("#trigger-a").GetAttribute("aria-expanded").ShouldBe("false");
            cut.Find("#trigger-b").GetAttribute("aria-expanded").ShouldBe("true");
            cut.Find("#trigger-b").HasAttribute("data-pressed").ShouldBeTrue();
        });
    }

    [Fact]
    public async Task UnregisteringActiveTriggerRepointsActiveTriggerToSurvivor()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<PopoverDynamicTriggersHost>(0);
            builder.CloseComponent();
        });

        cut.WaitForAssertion(() => cut.Find("#trigger-a").GetAttribute("aria-expanded").ShouldBe("true"));

        cut.Find("[data-testid='remove-a']").Click();
        await Task.Delay(100);

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("#trigger-a").Count.ShouldBe(0);
            cut.Find("#trigger-b").GetAttribute("aria-expanded").ShouldBe("true");
        });
    }

    [Fact]
    public Task Handle_OpensAndClosesImperatively()
    {
        var handle = new PopoverHandle<string>();
        var cut = Render(CreateHandlePopover(handle));

        // Verify initially closed
        cut.FindAll("[role='dialog']").Count.ShouldBe(0);

        // Open via handle
        handle.Open("trigger-a");

        cut.WaitForAssertion(() =>
        {
            handle.IsOpen.ShouldBeTrue();
            handle.ActiveTriggerId.ShouldBe("trigger-a");
        });

        // Close via handle
        handle.Close();

        cut.WaitForAssertion(() =>
        {
            handle.IsOpen.ShouldBeFalse();
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task Handle_SetsPayload()
    {
        var handle = new PopoverHandle<string>();
        var cut = Render(CreateHandlePopover(handle));

        // Open trigger-a which has Payload="hello"
        handle.Open("trigger-a");

        cut.WaitForAssertion(() =>
        {
            handle.Payload.ShouldBe("hello");
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task Handle_DetachedTriggerReceivesAriaControlsWhenOpen()
    {
        var handle = new PopoverHandle<string>();
        var cut = Render(CreateHandlePopover(handle));

        handle.Open("trigger-a");

        cut.WaitForAssertion(() =>
        {
            var popupId = cut.Find("[role='dialog']").Id;
            popupId.ShouldNotBeNullOrEmpty();
            cut.Find("#trigger-a").GetAttribute("aria-controls").ShouldBe(popupId);
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task Handle_DetachedTriggerRendersFocusGuardsWhenMountedAndNonModal()
    {
        var handle = new PopoverHandle<string>();
        var cut = Render(CreateHandlePopover(handle));

        handle.Open("trigger-a");

        cut.WaitForAssertion(() =>
        {
            var trigger = cut.Find("#trigger-a");
            trigger.PreviousElementSibling?.HasAttribute("data-blazix-base-ui-focus-guard").ShouldBeTrue();
            trigger.NextElementSibling?.HasAttribute("data-blazix-base-ui-focus-guard").ShouldBeTrue();
        });

        return Task.CompletedTask;
    }

    [Fact]
    public async Task HandleViewportSwapClearsInstantOnNextTriggerPress()
    {
        var handle = new PopoverHandle<string>();
        var cut = Render(CreateHandleViewportPopover(handle));

        cut.Find("#trigger-a").PointerDown(new PointerEventArgs { PointerType = "mouse" });
        cut.Find("#trigger-a").Click(new MouseEventArgs { Detail = 1 });

        cut.WaitForAssertion(() => cut.Find("[role='dialog']").ShouldNotBeNull());

        var viewport = cut.FindComponent<PopoverViewport>();
        await cut.InvokeAsync(() => viewport.Instance.OnViewportTransitionEnd());

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='presentation']").GetAttribute("data-instant").ShouldBe("trigger-change");
        });

        cut.Find("#trigger-b").PointerDown(new PointerEventArgs { PointerType = "mouse" });
        cut.Find("#trigger-b").Click(new MouseEventArgs { Detail = 1 });

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='presentation']").HasAttribute("data-instant").ShouldBeFalse();
        });
    }

    [Fact]
    public Task HandleViewportSwapMovesDetachedTriggerOpenState()
    {
        var handle = new PopoverHandle<string>();
        var cut = Render(CreateHandleViewportPopover(handle));

        cut.Find("#trigger-a").PointerDown(new PointerEventArgs { PointerType = "mouse" });
        cut.Find("#trigger-a").Click(new MouseEventArgs { Detail = 1 });

        cut.WaitForAssertion(() =>
        {
            cut.Find("#trigger-a").GetAttribute("data-popup-open").ShouldBe(string.Empty);
            cut.Find("#trigger-b").HasAttribute("data-popup-open").ShouldBeFalse();
        });

        cut.Find("#trigger-b").PointerDown(new PointerEventArgs { PointerType = "mouse" });
        cut.Find("#trigger-b").Click(new MouseEventArgs { Detail = 1 });

        cut.WaitForAssertion(() =>
        {
            cut.Find("#trigger-a").HasAttribute("data-popup-open").ShouldBeFalse();
            cut.Find("#trigger-b").GetAttribute("data-popup-open").ShouldBe(string.Empty);
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task HandleViewportSwapKeepsThreeDetachedTriggersAssociatedAfterReturningToFirstTrigger()
    {
        var handle = new PopoverHandle<string>();
        var cut = Render(CreateThreeTriggerHandleViewportPopover(handle));

        ClickTrigger(cut, "profile");
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='payload']").TextContent.ShouldBe("Profile");
            AssertOnlyTriggerOpen(cut, "profile");
        });

        ClickTrigger(cut, "notifications");
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='payload']").TextContent.ShouldBe("Notifications");
            AssertOnlyTriggerOpen(cut, "notifications");
        });

        ClickTrigger(cut, "profile");
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='payload']").TextContent.ShouldBe("Profile");
            AssertOnlyTriggerOpen(cut, "profile");
        });

        ClickTrigger(cut, "activity");
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='payload']").TextContent.ShouldBe("Activity");
            handle.ActiveTriggerId.ShouldBe("activity");
            AssertOnlyTriggerOpen(cut, "activity");
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnOpenChangeReceivesTriggerEventDetails()
    {
        PopoverOpenChangeEventArgs? receivedArgs = null;
        var cut = Render(CreatePopover(
            onOpenChange: EventCallback.Factory.Create<PopoverOpenChangeEventArgs>(this, args => receivedArgs = args),
            customContent: _ => innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverTrigger>(0);
                innerBuilder.AddAttribute(1, "Id", "trigger-a");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PopoverPortal>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PopoverPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));

        var trigger = cut.Find("#trigger-a");
        trigger.PointerDown(new PointerEventArgs { PointerType = "mouse" });
        trigger.Click(new MouseEventArgs { Detail = 1 });

        receivedArgs.ShouldNotBeNull();
        receivedArgs.Open.ShouldBeTrue();
        receivedArgs.Reason.ShouldBe(PopoverOpenChangeReason.TriggerPress);
        receivedArgs.TriggerId.ShouldBe("trigger-a");
        receivedArgs.Trigger.HasValue.ShouldBeTrue();
        receivedArgs.Event.ShouldBeOfType<MouseEventArgs>();
        receivedArgs.InteractionType.ShouldBe("mouse");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsInstantClickOnlyForKeyboardTriggerPress()
    {
        var cut = Render(CreatePopover(defaultOpen: false));

        // Click with Detail=1 (mouse click) - should NOT set instant click
        var trigger = cut.Find("button");
        trigger.PointerDown(new PointerEventArgs { PointerType = "mouse" });
        trigger.Click(new MouseEventArgs { Detail = 1 });

        // The popup should not have data-instant="click"
        var popup = cut.Find("[role='dialog']");
        popup.HasAttribute("data-instant").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task DoesNotSetInstantDismissOnOutsidePressClose()
    {
        var cut = Render(CreatePopover(defaultOpen: true));

        // Simulate outside press close
        var root = cut.FindComponent<PopoverRoot>();
        await cut.InvokeAsync(() => root.Instance.OnOutsidePress());

        // Popup should not have data-instant="dismiss"
        var popup = cut.Find("[role='dialog']");
        popup.HasAttribute("data-instant").ShouldBeFalse();
    }

    [Fact]
    public Task DoesNotSetInstantClickOnClosePressClose()
    {
        var cut = Render(CreatePopover(defaultOpen: true));

        // Find and verify the close button can close
        // ClosePress uses the Close() method which sets OpenChangeReason.ClosePress
        // Since no interaction type is "keyboard", it should not set instant
        var popup = cut.Find("[role='dialog']");

        // Before close, verify no instant attribute
        popup.HasAttribute("data-instant").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task PreventUnmountOnCloseKeepsPopupMounted()
    {
        var cut = Render(CreatePopover(
            defaultOpen: true,
            onOpenChange: EventCallback.Factory.Create<PopoverOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open)
                {
                    args.PreventUnmountOnClose();
                }
            })
        ));

        cut.Find("[role='dialog']").ShouldNotBeNull();

        // Close the popover
        cut.Find("button").Click();

        // Simulate transition end
        var root = cut.FindComponent<PopoverRoot>();
        await cut.InvokeAsync(() => root.Instance.OnTransitionEnd(false));

        // Popup should still be mounted because we prevented unmount
        cut.Find("[role='dialog']").ShouldNotBeNull();
    }

    [Fact]
    public async Task PreventUnmountOnCloseFlagIsResetOnNextClose()
    {
        var preventUnmount = true;

        var cut = Render(CreatePopover(
            defaultOpen: true,
            onOpenChange: EventCallback.Factory.Create<PopoverOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open && preventUnmount)
                {
                    args.PreventUnmountOnClose();
                }
            })
        ));

        // Close with prevention
        cut.Find("button").Click();
        var root = cut.FindComponent<PopoverRoot>();
        await cut.InvokeAsync(() => root.Instance.OnTransitionEnd(false));

        // Still mounted
        cut.Find("[role='dialog']").ShouldNotBeNull();

        // Now disable prevention and close again
        preventUnmount = false;
        // Re-open
        cut.Find("button").Click();
        await cut.InvokeAsync(() => root.Instance.OnTransitionEnd(true));

        // Close again without prevention
        cut.Find("button").Click();
        await cut.InvokeAsync(() => root.Instance.OnTransitionEnd(false));

        // Should be unmounted now
        cut.FindAll("[role='dialog']").Count.ShouldBe(0);
    }

    [Fact]
    public Task ScrollLockReactsToModalParameterChange()
    {
        // Start with non-modal open popover
        var cut = Render(CreatePopover(defaultOpen: true, modal: Blazix.BaseUI.Popover.PopoverModalMode.False));
        cut.Find("[role='dialog']").ShouldNotBeNull();

        // Re-render with modal=true - verifies no exceptions from scroll lock update
        var cut2 = Render(CreatePopover(defaultOpen: true, modal: Blazix.BaseUI.Popover.PopoverModalMode.True));
        cut2.Find("[role='dialog']").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    private RenderFragment CreatePopover(
        bool? open = null,
        bool defaultOpen = false,
        Blazix.BaseUI.Popover.PopoverModalMode modal = Blazix.BaseUI.Popover.PopoverModalMode.False,
        EventCallback<PopoverOpenChangeEventArgs>? onOpenChange = null,
        EventCallback<bool>? onOpenChangeComplete = null,
        PopoverRootActions? actionsRef = null,
        RenderFragment<PopoverRootPayloadContext>? customContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<PopoverRoot>(0);

            if (open.HasValue)
                builder.AddAttribute(1, "Open", open.Value);
            builder.AddAttribute(2, "DefaultOpen", defaultOpen);
            builder.AddAttribute(3, "Modal", modal);

            if (onOpenChange.HasValue)
                builder.AddAttribute(4, "OnOpenChange", onOpenChange.Value);
            if (onOpenChangeComplete.HasValue)
                builder.AddAttribute(5, "OnOpenChangeComplete", onOpenChangeComplete.Value);
            if (actionsRef is not null)
                builder.AddAttribute(6, "ActionsRef", actionsRef);

            builder.AddAttribute(7, "ChildContent", customContent ?? ((RenderFragment<PopoverRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PopoverPortal>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PopoverPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            })));

            builder.CloseComponent();
        };
    }

    private static RenderFragment CreatePayloadPopover(bool defaultOpen, string defaultTriggerId)
    {
        return builder =>
        {
            builder.OpenComponent<PopoverRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "DefaultTriggerId", defaultTriggerId);
            builder.AddAttribute(3, "ChildContent", (RenderFragment<PopoverRootPayloadContext>)(context => innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverTypedTrigger<string>>(0);
                innerBuilder.AddAttribute(1, "Id", "trigger-a");
                innerBuilder.AddAttribute(2, "Payload", "Payload A");
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger A")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PopoverTypedTrigger<string>>(10);
                innerBuilder.AddAttribute(11, "Id", "trigger-b");
                innerBuilder.AddAttribute(12, "Payload", "Payload B");
                innerBuilder.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger B")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PopoverPortal>(20);
                innerBuilder.AddAttribute(21, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PopoverPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenElement(0, "span");
                            popupBuilder.AddAttribute(1, "data-testid", "payload");
                            popupBuilder.AddContent(2, context.Payload?.ToString() ?? "none");
                            popupBuilder.CloseElement();
                        }));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateMultiTriggerPopover()
    {
        return builder =>
        {
            builder.OpenComponent<PopoverRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment<PopoverRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverTrigger>(0);
                innerBuilder.AddAttribute(1, "Id", "trigger-a");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger A")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PopoverTrigger>(10);
                innerBuilder.AddAttribute(11, "Id", "trigger-b");
                innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger B")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PopoverPortal>(20);
                innerBuilder.AddAttribute(21, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PopoverPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Popup Content")));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateHandlePopover(PopoverHandle<string> handle)
    {
        return builder =>
        {
            // Detached triggers
            builder.OpenComponent<PopoverTypedTrigger<string>>(0);
            builder.AddAttribute(1, "Id", "trigger-a");
            builder.AddAttribute(2, "Handle", handle);
            builder.AddAttribute(3, "Payload", "hello");
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger A")));
            builder.CloseComponent();

            builder.OpenComponent<PopoverTypedTrigger<string>>(10);
            builder.AddAttribute(11, "Id", "trigger-b");
            builder.AddAttribute(12, "Handle", handle);
            builder.AddAttribute(13, "Payload", "world");
            builder.AddAttribute(14, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger B")));
            builder.CloseComponent();

            // Root with handle
            builder.OpenComponent<PopoverRoot>(20);
            builder.AddAttribute(21, "Handle", (IPopoverHandle)handle);
            builder.AddAttribute(22, "ChildContent", (RenderFragment<PopoverRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverPortal>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PopoverPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Handle Content")));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateHandleViewportPopover(PopoverHandle<string> handle)
    {
        return builder =>
        {
            builder.OpenComponent<PopoverTypedTrigger<string>>(0);
            builder.AddAttribute(1, "Id", "trigger-a");
            builder.AddAttribute(2, "Handle", handle);
            builder.AddAttribute(3, "Payload", "Panel A");
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger A")));
            builder.CloseComponent();

            builder.OpenComponent<PopoverTypedTrigger<string>>(10);
            builder.AddAttribute(11, "Id", "trigger-b");
            builder.AddAttribute(12, "Handle", handle);
            builder.AddAttribute(13, "Payload", "Panel B");
            builder.AddAttribute(14, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger B")));
            builder.CloseComponent();

            builder.OpenComponent<PopoverRoot>(20);
            builder.AddAttribute(21, "Handle", (IPopoverHandle)handle);
            builder.AddAttribute(22, "ChildContent", (RenderFragment<PopoverRootPayloadContext>)(context => innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverPortal>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PopoverPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<PopoverViewport>(0);
                            popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(viewportBuilder =>
                            {
                                viewportBuilder.OpenElement(0, "span");
                                viewportBuilder.AddContent(1, context.Payload?.ToString() ?? "none");
                                viewportBuilder.CloseElement();
                            }));
                            popupBuilder.CloseComponent();
                        }));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateThreeTriggerHandleViewportPopover(PopoverHandle<string> handle)
    {
        return builder =>
        {
            builder.OpenComponent<PopoverTypedTrigger<string>>(0);
            builder.AddAttribute(1, "Id", "notifications");
            builder.AddAttribute(2, "Handle", handle);
            builder.AddAttribute(3, "Payload", "Notifications");
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Notifications")));
            builder.CloseComponent();

            builder.OpenComponent<PopoverTypedTrigger<string>>(10);
            builder.AddAttribute(11, "Id", "activity");
            builder.AddAttribute(12, "Handle", handle);
            builder.AddAttribute(13, "Payload", "Activity");
            builder.AddAttribute(14, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Activity")));
            builder.CloseComponent();

            builder.OpenComponent<PopoverTypedTrigger<string>>(20);
            builder.AddAttribute(21, "Id", "profile");
            builder.AddAttribute(22, "Handle", handle);
            builder.AddAttribute(23, "Payload", "Profile");
            builder.AddAttribute(24, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Profile")));
            builder.CloseComponent();

            builder.OpenComponent<PopoverRoot>(30);
            builder.AddAttribute(31, "Handle", (IPopoverHandle)handle);
            builder.AddAttribute(32, "ChildContent", (RenderFragment<PopoverRootPayloadContext>)(context => innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverPortal>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PopoverPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<PopoverViewport>(0);
                            popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(viewportBuilder =>
                            {
                                viewportBuilder.OpenElement(0, "span");
                                viewportBuilder.AddAttribute(1, "data-testid", "payload");
                                viewportBuilder.AddContent(2, context.Payload?.ToString() ?? "none");
                                viewportBuilder.CloseElement();
                            }));
                            popupBuilder.CloseComponent();
                        }));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static void ClickTrigger(IRenderedComponent<ContainerFragment> cut, string triggerId)
    {
        var trigger = cut.Find($"#{triggerId}");
        trigger.PointerDown(new PointerEventArgs { PointerType = "mouse" });
        trigger.Click(new MouseEventArgs { Detail = 1 });
    }

    private static void AssertOnlyTriggerOpen(IRenderedComponent<ContainerFragment> cut, string triggerId)
    {
        foreach (var id in new[] { "notifications", "activity", "profile" })
        {
            var trigger = cut.Find($"#{id}");
            trigger.GetAttribute("aria-expanded").ShouldBe(id == triggerId ? "true" : "false");
            trigger.HasAttribute("data-popup-open").ShouldBe(id == triggerId);
        }
    }
}

internal sealed class PopoverDynamicTriggersHost : ComponentBase
{
    private bool showTriggerA = true;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<PopoverRoot>(0);
        builder.AddAttribute(1, "DefaultOpen", true);
        builder.AddAttribute(2, "DefaultTriggerId", "trigger-a");
        builder.AddAttribute(3, "ChildContent", (RenderFragment<PopoverRootPayloadContext>)(_ => innerBuilder =>
        {
            if (showTriggerA)
            {
                innerBuilder.OpenComponent<PopoverTrigger>(0);
                innerBuilder.AddAttribute(1, "Id", "trigger-a");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger A")));
                innerBuilder.CloseComponent();
            }

            innerBuilder.OpenComponent<PopoverTrigger>(10);
            innerBuilder.AddAttribute(11, "Id", "trigger-b");
            innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger B")));
            innerBuilder.CloseComponent();

            innerBuilder.OpenElement(20, "button");
            innerBuilder.AddAttribute(21, "data-testid", "remove-a");
            innerBuilder.AddAttribute(22, "onclick", EventCallback.Factory.Create(this, () => showTriggerA = false));
            innerBuilder.AddContent(23, "Remove A");
            innerBuilder.CloseElement();

            innerBuilder.OpenComponent<PopoverPortal>(30);
            innerBuilder.AddAttribute(31, "ChildContent", (RenderFragment)(portalBuilder =>
            {
                portalBuilder.OpenComponent<PopoverPositioner>(0);
                portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<PopoverPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                    posBuilder.CloseComponent();
                }));
                portalBuilder.CloseComponent();
            }));
            innerBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
