using BlazorBaseUI.Dialog;
using BlazorBaseUI.Tests.Contracts.Dialog;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Dialog;

public class DialogPortalTests : BunitContext, IDialogPortalContract
{
    public DialogPortalTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupDialogModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateDialogWithPortal(
        bool open = true,
        bool keepMounted = false)
    {
        return builder =>
        {
            builder.OpenComponent<DialogRoot>(0);
            builder.AddAttribute(1, "Open", open);
            builder.AddAttribute(2, "Modal", BlazorBaseUI.Dialog.DialogModalMode.False);
            builder.AddAttribute(3, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<DialogPortal>(0);
                innerBuilder.AddAttribute(1, "KeepMounted", keepMounted);
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogPopup>(0);
                    portalBuilder.AddAttribute(1, "data-testid", "popup");
                    portalBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersChildrenWhenOpen()
    {
        var cut = Render(CreateDialogWithPortal(open: true));

        cut.Find("[data-testid='popup']").ShouldNotBeNull();
        cut.Find("[data-testid='popup']").TextContent.ShouldContain("Content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderWhenClosed()
    {
        var cut = Render(CreateDialogWithPortal(open: false, keepMounted: false));

        cut.FindAll("[data-testid='popup']").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task KeepMountedTrue_StaysMounted()
    {
        var cut = Render(CreateDialogWithPortal(open: false, keepMounted: true));

        var popup = cut.Find("[data-testid='popup']");
        popup.ShouldNotBeNull();
        popup.HasAttribute("hidden").ShouldBeTrue();

        return Task.CompletedTask;
    }
}
