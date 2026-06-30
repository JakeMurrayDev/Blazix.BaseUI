using Blazix.BaseUI.Dialog;
using Blazix.BaseUI.Tests.Contracts.Dialog;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blazix.BaseUI.Tests.Dialog;

public class DialogRootTests : BunitContext, IDialogRootContract
{
    public DialogRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupDialogModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateDialog(
        bool? open = null,
        bool defaultOpen = false,
        Blazix.BaseUI.Dialog.DialogModalMode modal = Blazix.BaseUI.Dialog.DialogModalMode.True,
        EventCallback<DialogOpenChangeEventArgs>? onOpenChange = null,
        DialogRootActions? actionsRef = null,
        RenderFragment? customPopupContent = null,
        bool includeTitle = false,
        bool includeDescription = false,
        bool includeBackdrop = false,
        FocusTarget? finalFocus = null)
    {
        return builder =>
        {
            builder.OpenComponent<DialogRoot>(0);

            if (open.HasValue)
                builder.AddAttribute(1, "Open", open.Value);
            builder.AddAttribute(2, "DefaultOpen", defaultOpen);
            builder.AddAttribute(3, "Modal", modal);

            if (onOpenChange.HasValue)
                builder.AddAttribute(4, "OnOpenChange", onOpenChange.Value);
            if (actionsRef is not null)
                builder.AddAttribute(5, "ActionsRef", actionsRef);

            builder.AddAttribute(6, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<DialogTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Open")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<DialogPortal>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    if (includeBackdrop)
                    {
                        portalBuilder.OpenComponent<DialogBackdrop>(0);
                        portalBuilder.AddAttribute(1, "data-testid", "backdrop");
                        portalBuilder.CloseComponent();
                    }

                    portalBuilder.OpenComponent<DialogPopup>(10);
                    portalBuilder.AddAttribute(11, "data-testid", "dialog-popup");
                    portalBuilder.AddAttribute(12, "ChildContent", customPopupContent ?? ((RenderFragment)(popupBuilder =>
                    {
                        if (includeTitle)
                        {
                            popupBuilder.OpenComponent<DialogTitle>(0);
                            popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "title text")));
                            popupBuilder.CloseComponent();
                        }

                        if (includeDescription)
                        {
                            popupBuilder.OpenComponent<DialogDescription>(10);
                            popupBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(b => b.AddContent(0, "description text")));
                            popupBuilder.CloseComponent();
                        }

                        popupBuilder.AddContent(20, "Dialog content");

                        popupBuilder.OpenComponent<DialogClose>(30);
                        popupBuilder.AddAttribute(31, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Close")));
                        popupBuilder.CloseComponent();
                    })));

                    if (finalFocus is not null)
                    {
                        portalBuilder.AddAttribute(13, "FinalFocus", finalFocus);
                    }

                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersChildren()
    {
        var cut = Render(CreateDialog());

        cut.Find("button").TextContent.ShouldBe("Open");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OpensByDefaultWhenDefaultOpenTrue()
    {
        var cut = Render(CreateDialog(defaultOpen: true));

        cut.Find("[role='dialog']").ShouldNotBeNull();
        cut.Find("[role='dialog']").TextContent.ShouldContain("Dialog content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RemainsClosedWhenControlledOpenFalse()
    {
        var cut = Render(CreateDialog(open: false, defaultOpen: true));

        cut.FindAll("[role='dialog']").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaLabelledByFromTitle()
    {
        var cut = Render(CreateDialog(defaultOpen: true, modal: Blazix.BaseUI.Dialog.DialogModalMode.False, includeTitle: true));

        var popup = cut.Find("[role='dialog']");
        var title = cut.Find("h2");

        popup.GetAttribute("aria-labelledby").ShouldBe(title.GetAttribute("id"));

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaDescribedByFromDescription()
    {
        var cut = Render(CreateDialog(defaultOpen: true, modal: Blazix.BaseUI.Dialog.DialogModalMode.False, includeDescription: true));

        var popup = cut.Find("[role='dialog']");
        var description = cut.Find("p");

        popup.GetAttribute("aria-describedby").ShouldBe(description.GetAttribute("id"));

        return Task.CompletedTask;
    }

    [Fact]
    public Task CallsOnOpenChangeWhenOpenStateChanges()
    {
        var callCount = 0;
        var lastOpen = false;

        var cut = Render(CreateDialog(
            defaultOpen: false,
            onOpenChange: EventCallback.Factory.Create<DialogOpenChangeEventArgs>(this, args =>
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
    public Task OnOpenChangeReasonTriggerPress()
    {
        Blazix.BaseUI.Dialog.DialogOpenChangeReason? capturedReason = null;

        var cut = Render(CreateDialog(
            defaultOpen: false,
            onOpenChange: EventCallback.Factory.Create<DialogOpenChangeEventArgs>(this, args =>
            {
                capturedReason = args.Reason;
            })
        ));

        var trigger = cut.Find("button");
        trigger.Click();

        capturedReason.ShouldBe(Blazix.BaseUI.Dialog.DialogOpenChangeReason.TriggerPress);

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnOpenChangeIncludesTriggerAssociation()
    {
        DialogOpenChangeEventArgs? captured = null;

        var cut = Render(CreateDialog(
            defaultOpen: false,
            onOpenChange: EventCallback.Factory.Create<DialogOpenChangeEventArgs>(this, args => captured = args)
        ));

        var trigger = cut.Find("button");
        var triggerId = trigger.GetAttribute("id");
        trigger.Click();

        captured.ShouldNotBeNull();
        // React surfaces the owning trigger on the change event details; the Blazor event args must
        // expose the same association (trigger id + element).
        captured!.TriggerId.ShouldBe(triggerId);
        captured.Trigger.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task FinalFocusCallbackReceivesCloseInteractionType()
    {
        string? receivedCloseType = null;

        var cut = Render(CreateDialog(
            defaultOpen: true,
            finalFocus: new FocusTarget.Callback(type =>
            {
                receivedCloseType = type;
                return null;
            })
        ));

        // The callback must observe the close interaction type, not the open interaction type.
        // Closing via the close button is a close-press, which resolves to a pointer interaction.
        var closeButton = cut.FindAll("button").First(b => b.TextContent.Contains("Close"));
        closeButton.Click();

        receivedCloseType.ShouldBe("mouse");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnOpenChangeReasonClosePress()
    {
        Blazix.BaseUI.Dialog.DialogOpenChangeReason? capturedReason = null;

        var cut = Render(CreateDialog(
            defaultOpen: true,
            onOpenChange: EventCallback.Factory.Create<DialogOpenChangeEventArgs>(this, args =>
            {
                capturedReason = args.Reason;
            })
        ));

        var closeButton = cut.Find("[data-testid='dialog-popup'] button");
        closeButton.Click();

        capturedReason.ShouldBe(Blazix.BaseUI.Dialog.DialogOpenChangeReason.ClosePress);

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnOpenChangeCancelPreventsOpening()
    {
        var cut = Render(CreateDialog(
            defaultOpen: false,
            onOpenChange: EventCallback.Factory.Create<DialogOpenChangeEventArgs>(this, args =>
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
        var cut = Render(CreateDialog(defaultOpen: true, modal: Blazix.BaseUI.Dialog.DialogModalMode.True, includeBackdrop: true));

        var backdrop = cut.Find("[data-testid='backdrop']");
        backdrop.ShouldNotBeNull();
        backdrop.GetAttribute("role").ShouldBe("presentation");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersInternalBackdropWhenModalFalse()
    {
        // Fix 02: Source has no modal guard on backdrop — backdrop renders for non-modal dialogs if included in markup
        var cut = Render(CreateDialog(defaultOpen: true, modal: Blazix.BaseUI.Dialog.DialogModalMode.False, includeBackdrop: true));

        cut.FindAll("[data-testid='backdrop']").Count.ShouldBe(1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ActionsRefCloseMethodClosesDialog()
    {
        var closeRequested = false;
        var actions = new DialogRootActions();

        var cut = Render(CreateDialog(
            defaultOpen: true,
            actionsRef: actions,
            onOpenChange: EventCallback.Factory.Create<DialogOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open)
                {
                    closeRequested = true;
                }
            })
        ));

        cut.Find("[role='dialog']").ShouldNotBeNull();

        actions.Close?.Invoke();

        closeRequested.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ActionsRefUnmountMethodUnmountsDialog()
    {
        var actions = new DialogRootActions();
        var cut = Render(CreateDialog(defaultOpen: true, actionsRef: actions));

        cut.Find("[role='dialog']").ShouldNotBeNull();

        await cut.InvokeAsync(() => actions.Unmount?.Invoke());

        cut.WaitForAssertion(() => cut.FindAll("[role='dialog']").Count.ShouldBe(0));
    }

    [Fact]
    public async Task HandleOpenWithPayloadOpensDialogWithPayload()
    {
        var handle = new DialogHandle<string>();
        object? capturedPayload = null;

        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<DialogRoot>(0);
            builder.AddAttribute(1, "Handle", (IDialogHandle)handle);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(ctx => b =>
            {
                capturedPayload = ctx.Payload;

                b.OpenComponent<DialogPortal>(0);
                b.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogPopup>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.AddContent(0, $"Payload: {ctx.Payload}");
                    }));
                    portalBuilder.CloseComponent();
                }));
                b.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(fragment);

        cut.FindAll("[role='dialog']").Count.ShouldBe(0);

        await cut.InvokeAsync(() => handle.OpenWithPayload("test-payload"));

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='dialog']").ShouldNotBeNull();
            capturedPayload.ShouldBe("test-payload");
        });
    }

    [Fact]
    public async Task HandleOpenAllowsMissingTriggerIdAndOpensWithoutAssociation()
    {
        var handle = new DialogHandle<string>();

        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<DialogRoot>(0);
            builder.AddAttribute(1, "Handle", (IDialogHandle)handle);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => b =>
            {
                b.OpenComponent<DialogPortal>(0);
                b.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogPopup>(0);
                    portalBuilder.AddAttribute(1, "data-testid", "dialog-popup");
                    portalBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.AddContent(0, "Dialog content");
                    }));
                    portalBuilder.CloseComponent();
                }));
                b.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(fragment);

        await cut.InvokeAsync(() => handle.Open("missing-trigger"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='dialog-popup']").ShouldNotBeNull());
        handle.ActiveTriggerId.ShouldBe("missing-trigger");
    }

    [Fact]
    public async Task HandleOpenNullOpensWithoutTriggerAssociation()
    {
        var handle = new DialogHandle<string>();

        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<DialogRoot>(0);
            builder.AddAttribute(1, "Handle", (IDialogHandle)handle);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => b =>
            {
                b.OpenComponent<DialogPortal>(0);
                b.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogPopup>(0);
                    portalBuilder.AddAttribute(1, "data-testid", "dialog-popup");
                    portalBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.AddContent(0, "Dialog content");
                    }));
                    portalBuilder.CloseComponent();
                }));
                b.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(fragment);

        await cut.InvokeAsync(() => handle.Open(null));

        cut.WaitForAssertion(() => cut.Find("[data-testid='dialog-popup']").ShouldNotBeNull());
        handle.ActiveTriggerId.ShouldBeNull();
    }

    [Fact]
    public async Task HandleOpenWithPayloadUpdatesPayloadWhileAlreadyOpen()
    {
        var handle = new DialogHandle<string>();

        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<DialogRoot>(0);
            builder.AddAttribute(1, "Handle", (IDialogHandle)handle);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(ctx => b =>
            {
                b.OpenComponent<DialogPortal>(0);
                b.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogPopup>(0);
                    portalBuilder.AddAttribute(1, "data-testid", "dialog-popup");
                    portalBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenElement(0, "span");
                        popupBuilder.AddAttribute(1, "data-testid", "payload");
                        popupBuilder.AddContent(2, ctx.Payload);
                        popupBuilder.CloseElement();
                    }));
                    portalBuilder.CloseComponent();
                }));
                b.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(fragment);

        await cut.InvokeAsync(() => handle.OpenWithPayload("first"));
        cut.WaitForAssertion(() => cut.Find("[data-testid='payload']").TextContent.ShouldBe("first"));

        await cut.InvokeAsync(() => handle.OpenWithPayload("second"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='payload']").TextContent.ShouldBe("second"));
    }

    [Fact]
    public Task ClickingDifferentTriggerWhileOpenSwitchesActivePayloadWithoutClosing()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<DialogRoot>(0);
            builder.AddAttribute(1, "Modal", Blazix.BaseUI.Dialog.DialogModalMode.False);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(ctx => b =>
            {
                b.OpenComponent<DialogTypedTrigger<int>>(0);
                b.AddAttribute(1, "Id", "trigger-1");
                b.AddAttribute(2, "Payload", 1);
                b.AddAttribute(3, "data-testid", "trigger-1");
                b.AddAttribute(4, "ChildContent", (RenderFragment)(triggerBuilder => triggerBuilder.AddContent(0, "One")));
                b.CloseComponent();

                b.OpenComponent<DialogTypedTrigger<int>>(10);
                b.AddAttribute(11, "Id", "trigger-2");
                b.AddAttribute(12, "Payload", 2);
                b.AddAttribute(13, "data-testid", "trigger-2");
                b.AddAttribute(14, "ChildContent", (RenderFragment)(triggerBuilder => triggerBuilder.AddContent(0, "Two")));
                b.CloseComponent();

                b.OpenComponent<DialogPortal>(20);
                b.AddAttribute(21, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogPopup>(0);
                    portalBuilder.AddAttribute(1, "data-testid", "dialog-popup");
                    portalBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenElement(0, "span");
                        popupBuilder.AddAttribute(1, "data-testid", "payload");
                        popupBuilder.AddContent(2, ctx.Payload);
                        popupBuilder.CloseElement();
                    }));
                    portalBuilder.CloseComponent();
                }));
                b.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(fragment);

        cut.Find("[data-testid='trigger-1']").Click();
        cut.Find("[data-testid='payload']").TextContent.ShouldBe("1");

        var popupId = cut.Find("[data-testid='dialog-popup']").GetAttribute("id");
        popupId.ShouldNotBeNullOrEmpty();

        cut.Find("[data-testid='trigger-2']").Click();

        cut.Find("[data-testid='payload']").TextContent.ShouldBe("2");
        cut.Find("[data-testid='dialog-popup']").GetAttribute("id").ShouldBe(popupId);
        cut.Find("[data-testid='trigger-1']").GetAttribute("aria-expanded").ShouldBe("false");
        cut.Find("[data-testid='trigger-2']").GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledTriggerIdSelectsPayloadOnInitialOpen()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<DialogRoot>(0);
            builder.AddAttribute(1, "Open", true);
            builder.AddAttribute(2, "TriggerId", "trigger-2");
            builder.AddAttribute(3, "Modal", Blazix.BaseUI.Dialog.DialogModalMode.False);
            builder.AddAttribute(4, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(ctx => b =>
            {
                b.OpenComponent<DialogTypedTrigger<int>>(0);
                b.AddAttribute(1, "Id", "trigger-1");
                b.AddAttribute(2, "Payload", 1);
                b.AddAttribute(3, "ChildContent", (RenderFragment)(triggerBuilder => triggerBuilder.AddContent(0, "One")));
                b.CloseComponent();

                b.OpenComponent<DialogTypedTrigger<int>>(10);
                b.AddAttribute(11, "Id", "trigger-2");
                b.AddAttribute(12, "Payload", 2);
                b.AddAttribute(13, "ChildContent", (RenderFragment)(triggerBuilder => triggerBuilder.AddContent(0, "Two")));
                b.CloseComponent();

                b.OpenComponent<DialogPortal>(20);
                b.AddAttribute(21, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogPopup>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenElement(0, "span");
                        popupBuilder.AddAttribute(1, "data-testid", "payload");
                        popupBuilder.AddContent(2, ctx.Payload);
                        popupBuilder.CloseElement();
                    }));
                    portalBuilder.CloseComponent();
                }));
                b.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(fragment);

        cut.Find("[data-testid='payload']").TextContent.ShouldBe("2");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task OnFocusOutClosesNonModalDialog()
    {
        Blazix.BaseUI.Dialog.DialogOpenChangeReason? capturedReason = null;

        var cut = Render(CreateDialog(
            defaultOpen: true,
            modal: Blazix.BaseUI.Dialog.DialogModalMode.False,
            onOpenChange: EventCallback.Factory.Create<DialogOpenChangeEventArgs>(this, args =>
            {
                capturedReason = args.Reason;
            })
        ));

        cut.Find("[role='dialog']").ShouldNotBeNull();

        // Simulate the JS calling OnFocusOut via JSInvokable
        var rootComponent = cut.FindComponent<DialogRoot>();
        await cut.InvokeAsync(() => rootComponent.Instance.OnFocusOut());

        capturedReason.ShouldBe(Blazix.BaseUI.Dialog.DialogOpenChangeReason.FocusOut);
    }

}
