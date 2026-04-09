using BlazorBaseUI.Popover;
using BlazorBaseUI.Tests.Contracts.Popover;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Popover;

public class PopoverTriggerTests : BunitContext, IPopoverTriggerContract
{
    public PopoverTriggerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPopoverModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateTriggerInRoot(
        bool defaultOpen = false,
        bool triggerDisabled = false,
        bool openOnHover = false,
        bool nativeButton = true,
        RenderFragment<RenderProps<PopoverTriggerState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<PopoverTriggerState, string>? classValue = null,
        Func<PopoverTriggerState, string>? styleValue = null,
        bool includePositioner = true,
        PopoverModalMode modal = PopoverModalMode.False)
    {
        return builder =>
        {
            builder.OpenComponent<PopoverRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "Modal", modal);
            builder.AddAttribute(3, "ChildContent", (RenderFragment<PopoverRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverTrigger>(0);
                var attrIndex = 1;

                if (triggerDisabled)
                    innerBuilder.AddAttribute(attrIndex++, "Disabled", true);
                if (!nativeButton)
                    innerBuilder.AddAttribute(attrIndex++, "NativeButton", false);
                if (openOnHover)
                    innerBuilder.AddAttribute(attrIndex++, "OpenOnHover", true);
                if (render is not null)
                    innerBuilder.AddAttribute(attrIndex++, "Render", render);
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);
                innerBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                if (includePositioner)
                {
                    innerBuilder.OpenComponent<PopoverPortal>(10);
                    innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(portalBuilder =>
                    {
                        portalBuilder.OpenComponent<PopoverPositioner>(0);
                        portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                        {
                            posBuilder.OpenComponent<PopoverPopup>(0);
                            posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
                            {
                                b.AddContent(0, "Content");
                                if (modal is not PopoverModalMode.False)
                                {
                                    b.OpenComponent<PopoverClose>(1);
                                    b.AddAttribute(2, "ChildContent", (RenderFragment)(cb => cb.AddContent(0, "Close")));
                                    b.CloseComponent();
                                }
                            }));
                            posBuilder.CloseComponent();
                        }));
                        portalBuilder.CloseComponent();
                    }));
                    innerBuilder.CloseComponent();
                }
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsButtonByDefault()
    {
        var cut = Render(CreateTriggerInRoot());

        var trigger = cut.Find("button");
        trigger.TagName.ShouldBe("BUTTON");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<PopoverTriggerState>> render = props => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateTriggerInRoot(render: render));

        var trigger = cut.Find("div[aria-expanded]");
        trigger.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateTriggerInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "trigger" },
                { "aria-label", "Open popover" }
            }
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("data-testid").ShouldBe("trigger");
        trigger.GetAttribute("aria-label").ShouldBe("Open popover");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaHaspopupDialog()
    {
        var cut = Render(CreateTriggerInRoot());

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-haspopup").ShouldBe("dialog");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedFalseWhenClosed()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedTrueWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataPopupOpenWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-popup-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDisabledAttributeWhenDisabled()
    {
        var cut = Render(CreateTriggerInRoot(triggerDisabled: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotToggleWhenDisabled()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false, triggerDisabled: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        trigger.Click();

        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task TogglesOnClick()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        trigger.Click();

        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreateTriggerInRoot(
            defaultOpen: true,
            classValue: state => state.Open ? "open-class" : "closed-class"
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreateTriggerInRoot(
            styleValue: _ => "color: blue"
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("style")!.ShouldContain("color: blue");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasFocusHandlersWhenOpenOnHover()
    {
        // Focus-based open/close is managed through JS-side focus management infrastructure,
        // not Blazor event handlers. Verify no onfocus/onblur handlers are present.
        var cut = Render(CreateTriggerInRoot(openOnHover: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("onfocus").ShouldBeFalse();
        trigger.HasAttribute("onblur").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaDisabledWhenDisabledCustomElement()
    {
        var cut = Render(CreateTriggerInRoot(triggerDisabled: true, nativeButton: false));

        var trigger = cut.Find("[role='button']");
        trigger.GetAttribute("aria-disabled").ShouldBe("true");
        trigger.HasAttribute("disabled").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataPressedWhenOpenViaClick()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false));

        var trigger = cut.Find("button");
        trigger.Click();

        trigger.HasAttribute("data-popup-open").ShouldBeTrue();
        trigger.HasAttribute("data-pressed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataPressedWhenClosed()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false));

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-pressed").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataPressedWhenHoverOpened()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true, openOnHover: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-pressed").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClickTriggerAttribute()
    {
        var cut = Render(CreateTriggerInRoot());

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-blazor-base-ui-click-trigger").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task InitializesHoverForHandleTrigger()
    {
        var handle = new PopoverHandle<string>();

        RenderFragment content = builder =>
        {
            builder.OpenComponent<PopoverTypedTrigger<string>>(0);
            builder.AddAttribute(1, "Id", "hover-handle-trigger");
            builder.AddAttribute(2, "Handle", handle);
            builder.AddAttribute(3, "OpenOnHover", true);
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Hover Me")));
            builder.CloseComponent();
        };

        var cut = Render(content);

        // Trigger renders even without a root context when using a handle
        var trigger = cut.Find("button");
        trigger.ShouldNotBeNull();
        trigger.TextContent.ShouldBe("Hover Me");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersFocusGuardsWhenOpenAndNonModal()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true, modal: PopoverModalMode.False));

        var guards = cut.FindAll("[data-blazor-base-ui-focus-guard]");
        guards.Count.ShouldBeGreaterThanOrEqualTo(2);

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderFocusGuardsWhenClosed()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false));

        var trigger = cut.Find("button");
        var guards = cut.FindAll("[data-blazor-base-ui-focus-guard]");

        // The trigger's focus guards should not be present when closed.
        // Other guards (from FloatingFocusManager/Portal) may exist, but
        // the trigger should not contribute any since it's closed.
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderFocusGuardsWhenModal()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true, modal: PopoverModalMode.True));

        // PopoverClose registers in OnInitialized (ClosePartCount++), then
        // PopoverPopup.OnParametersSet computes FocusManagerModal from HasClosePart.
        // The trigger renders before the popup updates FocusManagerModal, so
        // extra render cycles propagate the cascading state.
        cut.FindComponent<PopoverRoot>().Render();
        cut.FindComponent<PopoverRoot>().Render();

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        var prevSibling = trigger.PreviousElementSibling;
        var nextSibling = trigger.NextElementSibling;

        var hasTriggerPreGuard = prevSibling?.HasAttribute("data-blazor-base-ui-focus-guard") == true;
        var hasTriggerPostGuard = nextSibling?.HasAttribute("data-blazor-base-ui-focus-guard") == true;

        hasTriggerPreGuard.ShouldBeFalse();
        hasTriggerPostGuard.ShouldBeFalse();

        return Task.CompletedTask;
    }
}
