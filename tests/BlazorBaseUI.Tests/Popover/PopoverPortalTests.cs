using BlazorBaseUI.Popover;
using BlazorBaseUI.Tests.Contracts.Popover;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Popover;

public class PopoverPortalTests : BunitContext, IPopoverPortalContract
{
    public PopoverPortalTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPopoverModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreatePortalInPopover(
        bool defaultOpen = false,
        bool keepMounted = false)
    {
        return builder =>
        {
            builder.OpenComponent<PopoverRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<PopoverRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PopoverPortal>(10);
                innerBuilder.AddAttribute(11, "KeepMounted", keepMounted);
                innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(portalBuilder =>
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

    [Fact]
    public Task RendersPortalContainer()
    {
        var cut = Render(CreatePortalInPopover(defaultOpen: true));

        cut.Find("[role='dialog']").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildrenWhenMounted()
    {
        var cut = Render(CreatePortalInPopover(defaultOpen: true));

        cut.Find("[role='dialog']").TextContent.ShouldContain("Popup Content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderChildrenWhenNotMounted()
    {
        var cut = Render(CreatePortalInPopover(defaultOpen: false));

        cut.FindAll("[role='dialog']").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithKeepMounted()
    {
        var cut = Render(CreatePortalInPopover(defaultOpen: false, keepMounted: true));

        var positioner = cut.Find("[role='presentation']");
        positioner.HasAttribute("hidden").ShouldBeTrue();

        return Task.CompletedTask;
    }

}
