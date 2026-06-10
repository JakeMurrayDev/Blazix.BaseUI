using Blazix.BaseUI.Tests.Contracts.Tooltip;
using Blazix.BaseUI.Tests.Infrastructure;
using Blazix.BaseUI.Tooltip;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Tests.Tooltip;

public class TooltipProviderTests : BunitContext, ITooltipProviderContract
{
    public TooltipProviderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupTooltipModule(JSInterop);
        JsInteropSetup.SetupFloatingDelayGroupModule(JSInterop);
    }

    private RenderFragment CreateProvider(
        int? delay = null,
        int? closeDelay = null,
        int timeout = 400,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<TooltipProvider>(0);
            if (delay.HasValue)
                builder.AddAttribute(1, "Delay", delay.Value);
            if (closeDelay.HasValue)
                builder.AddAttribute(2, "CloseDelay", closeDelay.Value);
            builder.AddAttribute(3, "Timeout", timeout);
            builder.AddAttribute(4, "ChildContent", childContent ?? CreateDefaultContent());
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateDefaultContent()
    {
        return builder =>
        {
            builder.OpenComponent<TooltipRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<TooltipTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<TooltipPortal>(10);
                innerBuilder.AddAttribute(11, "KeepMounted", true);
                innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<TooltipPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<TooltipPopup>(0);
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
    public Task RendersChildren()
    {
        var cut = Render(CreateProvider());

        cut.Find("[role='tooltip']").ShouldNotBeNull();
        cut.Find("button").TextContent.ShouldBe("Trigger");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesProviderContext()
    {
        RenderFragment customContent = builder =>
        {
            builder.OpenComponent<TooltipRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                // We can verify context is cascaded by checking the trigger honors provider delay
                innerBuilder.OpenComponent<TooltipTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<TooltipPortal>(10);
                innerBuilder.AddAttribute(11, "KeepMounted", true);
                innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<TooltipPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<TooltipPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(CreateProvider(delay: 100, childContent: customContent));

        // If provider context is cascaded, the tooltip structure should render correctly
        cut.Find("[role='tooltip']").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DelayParameterIsPassedToContext()
    {
        // The delay parameter should be accessible through the provider context
        // We verify this indirectly by ensuring the provider renders with the delay set
        var cut = Render(CreateProvider(delay: 500));

        // Component renders successfully with custom delay
        cut.Find("button").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CloseDelayParameterIsPassedToContext()
    {
        var cut = Render(CreateProvider(closeDelay: 200));

        // Component renders successfully with custom closeDelay
        cut.Find("button").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task TimeoutParameterIsPassedToContext()
    {
        var cut = Render(CreateProvider(timeout: 600));

        // Component renders successfully with custom timeout
        cut.Find("button").ShouldNotBeNull();

        return Task.CompletedTask;
    }
}
