using Blazix.BaseUI.Popover;
using Blazix.BaseUI.Tests.Contracts.Popover;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Blazix.BaseUI.Tests.Popover;

public class PopoverCloseTests : BunitContext, IPopoverCloseContract
{
    public PopoverCloseTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPopoverModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateCloseInPopover(
        bool defaultOpen = true,
        RenderFragment<RenderProps<PopoverCloseState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<PopoverCloseState, string>? classValue = null,
        Func<PopoverCloseState, string>? styleValue = null)
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
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PopoverPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.AddContent(0, "Content");

                            popupBuilder.OpenComponent<PopoverClose>(10);
                            var attrIndex = 11;

                            if (render is not null)
                                popupBuilder.AddAttribute(attrIndex++, "Render", render);
                            if (classValue is not null)
                                popupBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                            if (styleValue is not null)
                                popupBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                            if (additionalAttributes is not null)
                                popupBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);
                            popupBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Close")));

                            popupBuilder.CloseComponent();
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
    public Task RendersAsButtonByDefault()
    {
        var cut = Render(CreateCloseInPopover());

        var dialog = cut.Find("[role='dialog']");
        var closeButton = dialog.QuerySelector("button[type='button']");
        closeButton.ShouldNotBeNull();
        closeButton!.TagName.ShouldBe("BUTTON");
        closeButton.TextContent.ShouldBe("Close");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<PopoverCloseState>> render = props => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateCloseInPopover(render: render));

        var closeButton = cut.Find("div[type='button']");
        closeButton.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateCloseInPopover(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "close" },
                { "aria-label", "Close popover" }
            }
        ));

        var closeButton = cut.Find("[data-testid='close']");
        closeButton.GetAttribute("aria-label").ShouldBe("Close popover");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTypeButtonAttribute()
    {
        var cut = Render(CreateCloseInPopover());

        var closeButton = cut.Find("button");
        closeButton.GetAttribute("type").ShouldBe("button");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ClosesPopoverOnClick()
    {
        var closeRequested = false;

        // Create custom popover with OnOpenChange callback to verify close was requested
        RenderFragment content = builder =>
        {
            builder.OpenComponent<PopoverRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "OnOpenChange", EventCallback.Factory.Create<PopoverOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open)
                {
                    closeRequested = true;
                }
            }));
            builder.AddAttribute(3, "ChildContent", (RenderFragment<PopoverRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PopoverPortal>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PopoverPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.AddContent(0, "Content");

                            popupBuilder.OpenComponent<PopoverClose>(10);
                            popupBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Close")));
                            popupBuilder.CloseComponent();
                        }));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(content);

        cut.Find("[role='dialog']").ShouldNotBeNull();

        var dialog = cut.Find("[role='dialog']");
        var closeButton = dialog.QuerySelector("button[type='button']");
        closeButton.ShouldNotBeNull();

        closeButton!.Click();

        // Verify that close was requested (OnOpenChange fired with Open=false)
        closeRequested.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ClosePressPreservesActiveTriggerDetails()
    {
        PopoverOpenChangeEventArgs? receivedArgs = null;

        RenderFragment content = builder =>
        {
            builder.OpenComponent<PopoverRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "DefaultTriggerId", "trigger-a");
            builder.AddAttribute(3, "OnOpenChange", EventCallback.Factory.Create<PopoverOpenChangeEventArgs>(this, args => receivedArgs = args));
            builder.AddAttribute(4, "ChildContent", (RenderFragment<PopoverRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverTrigger>(0);
                innerBuilder.AddAttribute(1, "Id", "trigger-a");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PopoverPortal>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PopoverPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<PopoverClose>(0);
                            popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Close")));
                            popupBuilder.CloseComponent();
                        }));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(content);
        var closeButton = cut.Find("[role='dialog'] button[type='button']");
        closeButton.PointerDown(new PointerEventArgs { PointerType = "mouse" });
        closeButton.Click(new MouseEventArgs { Detail = 1 });

        receivedArgs.ShouldNotBeNull();
        receivedArgs.Open.ShouldBeFalse();
        receivedArgs.Reason.ShouldBe(PopoverOpenChangeReason.ClosePress);
        receivedArgs.TriggerId.ShouldBe("trigger-a");
        receivedArgs.Trigger.HasValue.ShouldBeTrue();
        receivedArgs.Event.ShouldBeOfType<MouseEventArgs>();
        receivedArgs.InteractionType.ShouldBe("mouse");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreateCloseInPopover(
            classValue: _ => "open-class"
        ));

        var dialog = cut.Find("[role='dialog']");
        var closeButton = dialog.QuerySelector("button[type='button']");
        closeButton.ShouldNotBeNull();
        closeButton!.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreateCloseInPopover(
            styleValue: _ => "color: red"
        ));

        var dialog = cut.Find("[role='dialog']");
        var closeButton = dialog.QuerySelector("button[type='button']");
        closeButton.ShouldNotBeNull();
        closeButton!.GetAttribute("style")!.ShouldContain("color: red");

        return Task.CompletedTask;
    }

}
