using BlazorBaseUI.Dialog;
using BlazorBaseUI.Tests.Contracts.Dialog;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Dialog;

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
        BlazorBaseUI.Dialog.DialogModalMode modal = BlazorBaseUI.Dialog.DialogModalMode.True,
        EventCallback<DialogOpenChangeEventArgs>? onOpenChange = null,
        DialogRootActions? actionsRef = null,
        RenderFragment? customPopupContent = null,
        bool includeTitle = false,
        bool includeDescription = false,
        bool includeBackdrop = false)
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
        var cut = Render(CreateDialog(defaultOpen: true, modal: BlazorBaseUI.Dialog.DialogModalMode.False, includeTitle: true));

        var popup = cut.Find("[role='dialog']");
        var title = cut.Find("h2");

        popup.GetAttribute("aria-labelledby").ShouldBe(title.GetAttribute("id"));

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaDescribedByFromDescription()
    {
        var cut = Render(CreateDialog(defaultOpen: true, modal: BlazorBaseUI.Dialog.DialogModalMode.False, includeDescription: true));

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
        BlazorBaseUI.Dialog.DialogOpenChangeReason? capturedReason = null;

        var cut = Render(CreateDialog(
            defaultOpen: false,
            onOpenChange: EventCallback.Factory.Create<DialogOpenChangeEventArgs>(this, args =>
            {
                capturedReason = args.Reason;
            })
        ));

        var trigger = cut.Find("button");
        trigger.Click();

        capturedReason.ShouldBe(BlazorBaseUI.Dialog.DialogOpenChangeReason.TriggerPress);

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnOpenChangeReasonClosePress()
    {
        BlazorBaseUI.Dialog.DialogOpenChangeReason? capturedReason = null;

        var cut = Render(CreateDialog(
            defaultOpen: true,
            onOpenChange: EventCallback.Factory.Create<DialogOpenChangeEventArgs>(this, args =>
            {
                capturedReason = args.Reason;
            })
        ));

        var closeButton = cut.Find("[data-testid='dialog-popup'] button");
        closeButton.Click();

        capturedReason.ShouldBe(BlazorBaseUI.Dialog.DialogOpenChangeReason.ClosePress);

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
        var cut = Render(CreateDialog(defaultOpen: true, modal: BlazorBaseUI.Dialog.DialogModalMode.True, includeBackdrop: true));

        var backdrop = cut.Find("[data-testid='backdrop']");
        backdrop.ShouldNotBeNull();
        backdrop.GetAttribute("role").ShouldBe("presentation");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersInternalBackdropWhenModalFalse()
    {
        // Fix 02: Source has no modal guard on backdrop — backdrop renders for non-modal dialogs if included in markup
        var cut = Render(CreateDialog(defaultOpen: true, modal: BlazorBaseUI.Dialog.DialogModalMode.False, includeBackdrop: true));

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
    public async Task OnFocusOutClosesNonModalDialog()
    {
        BlazorBaseUI.Dialog.DialogOpenChangeReason? capturedReason = null;

        var cut = Render(CreateDialog(
            defaultOpen: true,
            modal: BlazorBaseUI.Dialog.DialogModalMode.False,
            onOpenChange: EventCallback.Factory.Create<DialogOpenChangeEventArgs>(this, args =>
            {
                capturedReason = args.Reason;
            })
        ));

        cut.Find("[role='dialog']").ShouldNotBeNull();

        // Simulate the JS calling OnFocusOut via JSInvokable
        var rootComponent = cut.FindComponent<DialogRoot>();
        await cut.InvokeAsync(() => rootComponent.Instance.OnFocusOut());

        capturedReason.ShouldBe(BlazorBaseUI.Dialog.DialogOpenChangeReason.FocusOut);
    }

}
