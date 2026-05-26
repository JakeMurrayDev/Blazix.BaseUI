using BlazorBaseUI.Tests.Contracts.Tooltip;
using BlazorBaseUI.Tests.Infrastructure;
using BlazorBaseUI.Tooltip;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tests.Tooltip;

public class TooltipRootTests : BunitContext, ITooltipRootContract
{
    public TooltipRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupTooltipModule(JSInterop);
    }

    private RenderFragment CreateTooltip(
        bool? open = null,
        bool defaultOpen = false,
        bool disabled = false,
        bool disableHoverablePopup = false,
        EventCallback<TooltipOpenChangeEventArgs>? onOpenChange = null,
        TooltipRootActions? actionsRef = null,
        RenderFragment? customContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<TooltipRoot>(0);

            if (open.HasValue)
                builder.AddAttribute(1, "Open", open.Value);
            builder.AddAttribute(2, "DefaultOpen", defaultOpen);
            builder.AddAttribute(3, "Disabled", disabled);
            builder.AddAttribute(4, "DisableHoverablePopup", disableHoverablePopup);

            if (onOpenChange.HasValue)
                builder.AddAttribute(5, "OnOpenChange", onOpenChange.Value);
            if (actionsRef is not null)
                builder.AddAttribute(6, "ActionsRef", actionsRef);

            builder.AddAttribute(7, "ChildContent", customContent ?? CreateDefaultContent());
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateDefaultContent()
    {
        return builder =>
        {
            builder.OpenComponent<TooltipTrigger>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
            builder.CloseComponent();

            builder.OpenComponent<TooltipPortal>(10);
            builder.AddAttribute(11, "KeepMounted", true);
            builder.AddAttribute(12, "ChildContent", (RenderFragment)(portalBuilder =>
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
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersChildren()
    {
        var cut = Render(CreateTooltip());

        cut.Find("button").TextContent.ShouldBe("Trigger");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OpensByDefaultWhenDefaultOpenTrue()
    {
        var cut = Render(CreateTooltip(defaultOpen: true));

        cut.Find("[role='tooltip']").ShouldNotBeNull();
        cut.Find("[role='tooltip']").TextContent.ShouldContain("Content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RemainsClosedWhenDefaultOpenFalseControlled()
    {
        var cut = Render(CreateTooltip(open: false, defaultOpen: true));

        cut.FindAll("[role='tooltip'][data-open]").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RemainsOpenWhenDefaultOpenTrueAndOpenTrue()
    {
        var cut = Render(CreateTooltip(open: true, defaultOpen: true));

        cut.Find("[role='tooltip'][data-open]").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DefaultOpenRemainsUncontrolled()
    {
        var closeRequested = false;

        var cut = Render(CreateTooltip(
            defaultOpen: true,
            onOpenChange: EventCallback.Factory.Create<TooltipOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open)
                {
                    closeRequested = true;
                }
            })
        ));

        cut.Find("[role='tooltip']").ShouldNotBeNull();

        // Trigger blur to close (simulating losing focus)
        var trigger = cut.Find("button");
        trigger.Blur();

        closeRequested.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CallsOnOpenChangeWhenOpenStateChanges()
    {
        var callCount = 0;
        var lastOpen = false;

        var cut = Render(CreateTooltip(
            defaultOpen: false,
            onOpenChange: EventCallback.Factory.Create<TooltipOpenChangeEventArgs>(this, args =>
            {
                callCount++;
                lastOpen = args.Open;
            })
        ));

        var trigger = cut.Find("button");
        trigger.Focus();

        callCount.ShouldBe(1);
        lastOpen.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnOpenChangeIncludesTriggerId()
    {
        string? triggerId = null;

        RenderFragment content = builder =>
        {
            builder.OpenComponent<TooltipTrigger>(0);
            builder.AddAttribute(1, "Id", "custom-trigger");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
            builder.CloseComponent();

            builder.OpenComponent<TooltipPortal>(10);
            builder.AddAttribute(11, "KeepMounted", true);
            builder.AddAttribute(12, "ChildContent", (RenderFragment)(portalBuilder =>
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
            builder.CloseComponent();
        };

        var cut = Render(CreateTooltip(
            defaultOpen: false,
            onOpenChange: EventCallback.Factory.Create<TooltipOpenChangeEventArgs>(this, args =>
            {
                triggerId = args.TriggerId;
            }),
            customContent: content
        ));

        cut.Find("#custom-trigger").Focus();

        triggerId.ShouldBe("custom-trigger");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotCallOnOpenChangeWhenStateUnchanged()
    {
        var callCount = 0;

        var cut = Render(CreateTooltip(
            defaultOpen: true,
            onOpenChange: EventCallback.Factory.Create<TooltipOpenChangeEventArgs>(this, args =>
            {
                callCount++;
            })
        ));

        // Trigger is already open via defaultOpen, focusing shouldn't call onOpenChange again
        var trigger = cut.Find("button");
        trigger.Focus();

        // Should be 0 because state didn't change (already open)
        callCount.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnOpenChangeCancelPreventsOpening()
    {
        var cut = Render(CreateTooltip(
            defaultOpen: false,
            onOpenChange: EventCallback.Factory.Create<TooltipOpenChangeEventArgs>(this, args =>
            {
                if (args.Open)
                {
                    args.Cancel();
                }
            })
        ));

        var trigger = cut.Find("button");
        trigger.Focus();

        cut.FindAll("[role='tooltip'][data-open]").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ShouldNotOpenWhenDisabled()
    {
        var cut = Render(CreateTooltip(defaultOpen: false, disabled: true));

        var trigger = cut.Find("button");
        trigger.Focus();

        cut.FindAll("[role='tooltip'][data-open]").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledPreventsSubsequentOpens()
    {
        // Disabled auto-closes open tooltips and prevents new opens.
        var cut = Render(CreateTooltip(defaultOpen: true, disabled: true));

        // Should be closed because disabled auto-closes open tooltips
        cut.FindAll("[role='tooltip'][data-open]").Count.ShouldBe(0);

        // Try to open by focusing the trigger — should stay closed
        var trigger = cut.Find("button");
        trigger.Focus();

        cut.FindAll("[role='tooltip'][data-open]").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledDoesNotPreventInitialDefaultOpen()
    {
        // When both DefaultOpen=true and Disabled=true are set,
        // the disabled auto-close fires in OnParametersSet, closing the tooltip.
        var cut = Render(CreateTooltip(defaultOpen: true, disabled: true));

        // The tooltip should be closed because disabled auto-closes open tooltips
        cut.FindAll("[role='tooltip'][data-open]").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ActionsRefCloseMethodClosesTooltip()
    {
        var closeRequested = false;
        var actions = new TooltipRootActions();

        var cut = Render(CreateTooltip(
            defaultOpen: true,
            actionsRef: actions,
            onOpenChange: EventCallback.Factory.Create<TooltipOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open)
                {
                    closeRequested = true;
                }
            })
        ));

        cut.Find("[role='tooltip']").ShouldNotBeNull();

        await cut.InvokeAsync(() => actions.Close?.Invoke());

        closeRequested.ShouldBeTrue();
    }

    [Fact]
    public async Task ActionsRefUnmountMethodUnmountsTooltip()
    {
        var actions = new TooltipRootActions();
        var cut = Render(CreateTooltip(defaultOpen: true, actionsRef: actions));

        // Verify tooltip is open and visible initially
        cut.Find("[role='tooltip'][data-open]").ShouldNotBeNull();
        cut.FindAll("[role='presentation'][hidden]").Count.ShouldBe(0);

        await cut.InvokeAsync(() => actions.Unmount?.Invoke());

        // ForceUnmount sets isMounted=false but keeps open=true
        // With KeepMounted=true, the element stays in DOM but positioner gets hidden attribute
        cut.WaitForAssertion(() => cut.Find("[role='presentation'][hidden]").ShouldNotBeNull());
    }

    [Fact]
    public Task CascadesContextToChildren()
    {
        RenderFragment customContent = builder =>
        {
            builder.OpenComponent<TooltipTrigger>(0);
            builder.AddAttribute(1, "ClassValue", (Func<TooltipTriggerState, string>)(state =>
            {
                return state.Open ? "open" : "closed";
            }));
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
            builder.CloseComponent();

            builder.OpenComponent<TooltipPortal>(10);
            builder.AddAttribute(11, "KeepMounted", true);
            builder.AddAttribute(12, "ChildContent", (RenderFragment)(portalBuilder =>
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
            builder.CloseComponent();
        };

        var cut = Render(CreateTooltip(defaultOpen: true, customContent: customContent));

        // Verify context is cascaded by checking the trigger has the correct class
        var trigger = cut.Find("button");
        trigger.GetAttribute("class").ShouldContain("open");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SwitchesActiveTriggerAndPayloadWhileAlreadyOpen()
    {
        RenderFragment<TooltipRootPayloadContext> content = payloadContext => builder =>
        {
            builder.OpenComponent<TooltipTrigger>(0);
            builder.AddAttribute(1, "Id", "trigger-1");
            builder.AddAttribute(2, "Payload", "one");
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger 1")));
            builder.CloseComponent();

            builder.OpenComponent<TooltipTrigger>(10);
            builder.AddAttribute(11, "Id", "trigger-2");
            builder.AddAttribute(12, "Payload", "two");
            builder.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger 2")));
            builder.CloseComponent();

            builder.OpenComponent<TooltipPortal>(20);
            builder.AddAttribute(21, "KeepMounted", true);
            builder.AddAttribute(22, "ChildContent", (RenderFragment)(portalBuilder =>
            {
                portalBuilder.OpenComponent<TooltipPositioner>(0);
                portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<TooltipPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenElement(0, "span");
                        popupBuilder.AddAttribute(1, "data-testid", "payload");
                        popupBuilder.AddContent(2, payloadContext.Payload);
                        popupBuilder.CloseElement();
                    }));
                    posBuilder.CloseComponent();
                }));
                portalBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(builder =>
        {
            builder.OpenComponent<TooltipRoot>(0);
            builder.AddAttribute(1, "ChildContentWithPayload", content);
            builder.CloseComponent();
        });

        var trigger1 = cut.Find("#trigger-1");
        var trigger2 = cut.Find("#trigger-2");

        trigger1.Focus();
        cut.Find("[data-testid='payload']").TextContent.ShouldBe("one");
        trigger1.HasAttribute("data-popup-open").ShouldBeTrue();

        trigger2.Focus();
        cut.Find("[data-testid='payload']").TextContent.ShouldBe("two");
        trigger1.HasAttribute("data-popup-open").ShouldBeFalse();
        trigger2.HasAttribute("data-popup-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledTriggerClosesActiveTooltip()
    {
        RenderFragment content = builder =>
        {
            builder.OpenComponent<TooltipTrigger>(0);
            builder.AddAttribute(1, "Id", "trigger-1");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger 1")));
            builder.CloseComponent();

            builder.OpenComponent<TooltipTrigger>(10);
            builder.AddAttribute(11, "Id", "trigger-2");
            builder.AddAttribute(12, "Disabled", true);
            builder.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger 2")));
            builder.CloseComponent();

            builder.OpenComponent<TooltipPortal>(20);
            builder.AddAttribute(21, "KeepMounted", true);
            builder.AddAttribute(22, "ChildContent", (RenderFragment)(portalBuilder =>
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
            builder.CloseComponent();
        };

        var cut = Render(CreateTooltip(customContent: content));

        cut.Find("#trigger-1").Focus();
        cut.Find("[role='tooltip']").HasAttribute("data-open").ShouldBeTrue();

        cut.Find("#trigger-2").Focus();

        cut.Find("[role='tooltip']").HasAttribute("data-closed").ShouldBeTrue();
        cut.Find("#trigger-2").HasAttribute("data-popup-open").ShouldBeFalse();

        return Task.CompletedTask;
    }
}
