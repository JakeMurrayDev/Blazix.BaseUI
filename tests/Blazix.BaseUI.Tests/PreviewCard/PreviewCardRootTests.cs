using Blazix.BaseUI.Tests.Contracts.PreviewCard;
using Blazix.BaseUI.Tests.Infrastructure;
using Blazix.BaseUI.PreviewCard;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Tests.PreviewCard;

public class PreviewCardRootTests : BunitContext, IPreviewCardRootContract
{
    public PreviewCardRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPreviewCardModule(JSInterop);
    }

    private RenderFragment CreatePreviewCard(
        bool? open = null,
        bool defaultOpen = false,
        int delay = 0,
        int closeDelay = 0,
        EventCallback<PreviewCardOpenChangeEventArgs>? onOpenChange = null,
        PreviewCardRootActions? actionsRef = null,
        RenderFragment? customContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<PreviewCardRoot>(0);

            if (open.HasValue)
                builder.AddAttribute(1, "Open", open.Value);
            builder.AddAttribute(2, "DefaultOpen", defaultOpen);

            if (onOpenChange.HasValue)
                builder.AddAttribute(5, "OnOpenChange", onOpenChange.Value);
            if (actionsRef is not null)
                builder.AddAttribute(6, "ActionsRef", actionsRef);

            builder.AddAttribute(7, "ChildContent", customContent ?? CreateDefaultContent(delay, closeDelay));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateDefaultContent(int delay = 0, int closeDelay = 0)
    {
        return builder =>
        {
            builder.OpenComponent<PreviewCardTrigger>(0);
            builder.AddAttribute(1, "Delay", delay);
            builder.AddAttribute(2, "CloseDelay", closeDelay);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
            builder.CloseComponent();

            builder.OpenComponent<PreviewCardPortal>(10);
            builder.AddAttribute(11, "KeepMounted", true);
            builder.AddAttribute(12, "ChildContent", (RenderFragment)(portalBuilder =>
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
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreatePayloadSwitchingPreviewCard()
    {
        return builder =>
        {
            builder.OpenComponent<PreviewCardRoot>(0);
            builder.AddAttribute(1, "ChildContentWithPayload", (RenderFragment<PreviewCardRootPayloadContext>)(payloadContext => innerBuilder =>
            {
                innerBuilder.OpenComponent<PreviewCardTrigger>(0);
                innerBuilder.AddAttribute(1, "Id", "trigger-one");
                innerBuilder.AddAttribute(2, "Delay", 0);
                innerBuilder.AddAttribute(3, "CloseDelay", 0);
                innerBuilder.AddAttribute(4, "Payload", "one");
                innerBuilder.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger one")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PreviewCardTrigger>(10);
                innerBuilder.AddAttribute(11, "Id", "trigger-two");
                innerBuilder.AddAttribute(12, "Delay", 0);
                innerBuilder.AddAttribute(13, "CloseDelay", 0);
                innerBuilder.AddAttribute(14, "Payload", "two");
                innerBuilder.AddAttribute(15, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger two")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PreviewCardPortal>(20);
                innerBuilder.AddAttribute(21, "KeepMounted", true);
                innerBuilder.AddAttribute(22, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PreviewCardPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PreviewCardPopup>(0);
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
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersChildren()
    {
        var cut = Render(CreatePreviewCard());

        cut.Find("a").TextContent.ShouldBe("Trigger");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledOpenPropShowsPopup()
    {
        var cut = Render(CreatePreviewCard(open: true));

        cut.Find("[data-open]").ShouldNotBeNull();
        cut.Find("[data-open]").TextContent.ShouldContain("Content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OpensByDefaultWhenDefaultOpenTrue()
    {
        var cut = Render(CreatePreviewCard(defaultOpen: true));

        var popup = cut.Find("div[data-open]");
        popup.ShouldNotBeNull();
        popup.TextContent.ShouldContain("Content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RemainsClosedWhenDefaultOpenFalseControlled()
    {
        var cut = Render(CreatePreviewCard(open: false, defaultOpen: true));

        // The popup should not have data-open (it should have data-closed)
        cut.FindAll("div[data-side][data-open]").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RemainsOpenWhenDefaultOpenTrueAndOpenTrue()
    {
        var cut = Render(CreatePreviewCard(open: true, defaultOpen: true));

        cut.Find("div[data-side][data-open]").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DefaultOpenRemainsUncontrolled()
    {
        var closeRequested = false;

        var cut = Render(CreatePreviewCard(
            defaultOpen: true,
            onOpenChange: EventCallback.Factory.Create<PreviewCardOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open)
                {
                    closeRequested = true;
                }
            })
        ));

        var popup = cut.Find("div[data-open]");
        popup.ShouldNotBeNull();

        // Trigger blur to close
        var trigger = cut.Find("a");
        trigger.Blur();

        closeRequested.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task OpensOnTriggerHover()
    {
        var cut = Render(CreatePreviewCard());

        var root = cut.FindComponent<PreviewCardRoot>();
        await cut.InvokeAsync(() => root.Instance.OnHoverOpen(null));

        cut.Find("div[data-side][data-open]").ShouldNotBeNull();
    }

    [Fact]
    public async Task ClosesOnTriggerUnhover()
    {
        var callCount = 0;
        var cut = Render(CreatePreviewCard(
            defaultOpen: true,
            onOpenChange: EventCallback.Factory.Create<PreviewCardOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open) callCount++;
            })
        ));

        var root = cut.FindComponent<PreviewCardRoot>();
        await cut.InvokeAsync(() => root.Instance.OnHoverClose(null));
        callCount.ShouldBe(1);
    }

    [Fact]
    public Task OpensOnTriggerFocus()
    {
        var cut = Render(CreatePreviewCard());

        var trigger = cut.Find("a");
        trigger.Focus();

        cut.Find("div[data-side][data-open]").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task SwitchesActiveTriggerWhileOpenAndUpdatesPayload()
    {
        var cut = Render(CreatePayloadSwitchingPreviewCard());
        var root = cut.FindComponent<PreviewCardRoot>();

        await cut.InvokeAsync(() => root.Instance.SetOpenAsync(true, PreviewCardOpenChangeReason.TriggerFocus, "trigger-one"));

        cut.Find("[data-testid='payload']").TextContent.ShouldBe("one");

        await cut.InvokeAsync(() => root.Instance.SetOpenAsync(true, PreviewCardOpenChangeReason.TriggerFocus, "trigger-two"));

        cut.Find("[data-testid='payload']").TextContent.ShouldBe("two");
    }

    [Fact]
    public Task ClosesOnTriggerBlur()
    {
        var closeRequested = false;
        var cut = Render(CreatePreviewCard(
            defaultOpen: true,
            onOpenChange: EventCallback.Factory.Create<PreviewCardOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open) closeRequested = true;
            })
        ));

        cut.Find("a").Blur();
        closeRequested.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CallsOnOpenChangeWhenOpenStateChanges()
    {
        var callCount = 0;
        var lastOpen = false;

        var cut = Render(CreatePreviewCard(
            defaultOpen: false,
            onOpenChange: EventCallback.Factory.Create<PreviewCardOpenChangeEventArgs>(this, args =>
            {
                callCount++;
                lastOpen = args.Open;
            })
        ));

        var trigger = cut.Find("a");
        trigger.Focus();

        callCount.ShouldBe(1);
        lastOpen.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotCallOnOpenChangeWhenStateUnchanged()
    {
        var callCount = 0;

        var cut = Render(CreatePreviewCard(
            defaultOpen: true,
            onOpenChange: EventCallback.Factory.Create<PreviewCardOpenChangeEventArgs>(this, args =>
            {
                callCount++;
            })
        ));

        // Trigger is already open via defaultOpen, focusing shouldn't call onOpenChange again
        var trigger = cut.Find("a");
        trigger.Focus();

        // Should be 0 because state didn't change (already open)
        callCount.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnOpenChangeCancelPreventsOpening()
    {
        var cut = Render(CreatePreviewCard(
            defaultOpen: false,
            onOpenChange: EventCallback.Factory.Create<PreviewCardOpenChangeEventArgs>(this, args =>
            {
                if (args.Open)
                {
                    args.Cancel();
                }
            })
        ));

        var trigger = cut.Find("a");
        trigger.Focus();

        cut.FindAll("div[data-side][data-open]").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ActionsRefCloseMethodClosesPreviewCard()
    {
        var closeRequested = false;
        var actions = new PreviewCardRootActions();

        var cut = Render(CreatePreviewCard(
            defaultOpen: true,
            actionsRef: actions,
            onOpenChange: EventCallback.Factory.Create<PreviewCardOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open)
                {
                    closeRequested = true;
                }
            })
        ));

        var popup = cut.Find("div[data-open]");
        popup.ShouldNotBeNull();

        await cut.InvokeAsync(() => actions.Close?.Invoke());

        closeRequested.ShouldBeTrue();
    }

    [Fact]
    public async Task ActionsRefUnmountMethodUnmountsPreviewCard()
    {
        var actions = new PreviewCardRootActions();
        var cut = Render(CreatePreviewCard(defaultOpen: true, actionsRef: actions));

        // Verify preview card is open and visible initially
        cut.Find("div[data-side][data-open]").ShouldNotBeNull();
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
            builder.OpenComponent<PreviewCardTrigger>(0);
            builder.AddAttribute(1, "Delay", 0);
            builder.AddAttribute(2, "CloseDelay", 0);
            builder.AddAttribute(3, "ClassValue", (Func<PreviewCardTriggerState, string>)(state =>
            {
                return state.Open ? "open" : "closed";
            }));
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
            builder.CloseComponent();

            builder.OpenComponent<PreviewCardPortal>(10);
            builder.AddAttribute(11, "KeepMounted", true);
            builder.AddAttribute(12, "ChildContent", (RenderFragment)(portalBuilder =>
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
            builder.CloseComponent();
        };

        var cut = Render(CreatePreviewCard(defaultOpen: true, customContent: customContent));

        // Verify context is cascaded by checking the trigger has the correct class
        var trigger = cut.Find("a");
        trigger.GetAttribute("class").ShouldContain("open");

        return Task.CompletedTask;
    }
}
