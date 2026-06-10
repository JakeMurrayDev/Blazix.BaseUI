using Blazix.BaseUI.AlertDialog;
using Blazix.BaseUI.Dialog;
using Blazix.BaseUI.Tests.Contracts.AlertDialog;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blazix.BaseUI.Tests.AlertDialog;

public class AlertDialogRootTests : BunitContext, IAlertDialogRootContract
{
    public AlertDialogRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupDialogModule(JSInterop);
    }

    private RenderFragment CreateAlertDialog(
        bool? open = null,
        bool defaultOpen = false,
        EventCallback<DialogOpenChangeEventArgs>? onOpenChange = null)
    {
        return builder =>
        {
            builder.OpenComponent<AlertDialogRoot>(0);

            if (open.HasValue)
                builder.AddAttribute(1, "Open", open.Value);
            builder.AddAttribute(2, "DefaultOpen", defaultOpen);

            if (onOpenChange.HasValue)
                builder.AddAttribute(3, "OnOpenChange", onOpenChange.Value);

            builder.AddAttribute(4, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<AlertDialogTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Open")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<AlertDialogPortal>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<AlertDialogPopup>(10);
                    portalBuilder.AddAttribute(11, "data-testid", "alert-dialog-popup");
                    portalBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.AddContent(0, "Alert content");

                        popupBuilder.OpenComponent<AlertDialogClose>(10);
                        popupBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Close")));
                        popupBuilder.CloseComponent();
                    }));
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
        var cut = Render(CreateAlertDialog());

        cut.Find("button").TextContent.ShouldBe("Open");

        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesAlertDialogRole()
    {
        var cut = Render(CreateAlertDialog(defaultOpen: true));

        cut.Find("[role='alertdialog']").ShouldNotBeNull();
        cut.Find("[role='alertdialog']").TextContent.ShouldContain("Alert content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task IsAlwaysModal()
    {
        var cut = Render(CreateAlertDialog(defaultOpen: true));

        // AlertDialogRoot wraps DialogRoot with hardcoded Modal=True.
        // Verify the inner DialogRoot component received the correct Modal parameter.
        var dialogRoot = cut.FindComponent<DialogRoot>();
        dialogRoot.Instance.Modal.ShouldBe(Blazix.BaseUI.Dialog.DialogModalMode.True);

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotDismissOnOutsidePress()
    {
        var cut = Render(CreateAlertDialog(defaultOpen: true));

        // Verify AlertDialogRoot forwards DisablePointerDismissal=true to the underlying DialogRoot
        var dialogRoot = cut.FindComponent<DialogRoot>();
        dialogRoot.Instance.DisablePointerDismissal.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotExposeModalParameter()
    {
        var properties = typeof(AlertDialogRoot).GetProperties();
        var modalProp = properties.FirstOrDefault(p => p.Name == "Modal");
        modalProp.ShouldBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotExposeDisablePointerDismissalParameter()
    {
        var properties = typeof(AlertDialogRoot).GetProperties();
        var dismissProp = properties.FirstOrDefault(p => p.Name == "DisablePointerDismissal");
        dismissProp.ShouldBeNull();

        return Task.CompletedTask;
    }
}
