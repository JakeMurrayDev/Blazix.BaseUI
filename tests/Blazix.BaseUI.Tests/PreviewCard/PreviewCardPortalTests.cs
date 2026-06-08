using Blazix.BaseUI.Tests.Contracts.PreviewCard;
using Blazix.BaseUI.Tests.Infrastructure;
using Blazix.BaseUI.PreviewCard;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Tests.PreviewCard;

public class PreviewCardPortalTests : BunitContext, IPreviewCardPortalContract
{
    public PreviewCardPortalTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPreviewCardModule(JSInterop);
    }

    private RenderFragment CreatePortalInRoot(
        bool defaultOpen = false,
        bool keepMounted = false)
    {
        return builder =>
        {
            builder.OpenComponent<PreviewCardRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<PreviewCardTrigger>(0);
                innerBuilder.AddAttribute(1, "Delay", 0);
                innerBuilder.AddAttribute(2, "CloseDelay", 0);
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PreviewCardPortal>(10);
                innerBuilder.AddAttribute(11, "KeepMounted", keepMounted);
                innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PreviewCardPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PreviewCardPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
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
    public Task RendersChildrenWhenMounted()
    {
        var cut = Render(CreatePortalInRoot(defaultOpen: true, keepMounted: false));

        cut.Find("div[data-side][id]").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderChildrenWhenNotMounted()
    {
        var cut = Render(CreatePortalInRoot(defaultOpen: false, keepMounted: false));

        cut.FindAll("div[data-side][id]").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildrenWhenKeepMounted()
    {
        var cut = Render(CreatePortalInRoot(defaultOpen: false, keepMounted: true));

        // Portal content should be rendered even when closed
        var popup = cut.Find("div[data-side][id]");
        popup.ShouldNotBeNull();
        popup.HasAttribute("data-closed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesPortalContext()
    {
        // The portal context with keepMounted should be available to children
        var cut = Render(CreatePortalInRoot(defaultOpen: false, keepMounted: true));

        // Content is rendered because keepMounted is true
        cut.Find("[role='presentation']").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<PreviewCardPortal>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
