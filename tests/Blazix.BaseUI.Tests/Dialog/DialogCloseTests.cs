using Blazix.BaseUI.Dialog;
using Blazix.BaseUI.Tests.Contracts.Dialog;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blazix.BaseUI.Tests.Dialog;

public class DialogCloseTests : BunitContext, IDialogCloseContract
{
    public DialogCloseTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupDialogModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateDialogWithClose(
        RenderFragment<RenderProps<DialogCloseState>>? render = null,
        bool closeDisabled = false,
        bool nativeButton = true,
        Dictionary<string, object>? closeAttributes = null,
        Func<DialogCloseState, string>? classValue = null,
        Func<DialogCloseState, string>? styleValue = null,
        EventCallback<DialogOpenChangeEventArgs>? onOpenChange = null)
    {
        return builder =>
        {
            builder.OpenComponent<DialogRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "Modal", Blazix.BaseUI.Dialog.DialogModalMode.False);

            if (onOpenChange.HasValue)
                builder.AddAttribute(3, "OnOpenChange", onOpenChange.Value);

            builder.AddAttribute(4, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<DialogPortal>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogPopup>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<DialogClose>(0);
                        popupBuilder.AddAttribute(1, "data-testid", "close");

                        if (render is not null)
                            popupBuilder.AddAttribute(2, "Render", render);

                        popupBuilder.AddAttribute(3, "Disabled", closeDisabled);
                        popupBuilder.AddAttribute(4, "NativeButton", nativeButton);

                        if (closeAttributes is not null)
                        {
                            foreach (var (key, value) in closeAttributes)
                            {
                                popupBuilder.AddAttribute(5, key, value);
                            }
                        }

                        if (classValue is not null)
                            popupBuilder.AddAttribute(6, "ClassValue", classValue);

                        if (styleValue is not null)
                            popupBuilder.AddAttribute(7, "StyleValue", styleValue);

                        popupBuilder.AddAttribute(8, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Close")));
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
    public Task RendersAsButtonByDefault()
    {
        var cut = Render(CreateDialogWithClose());

        var closeButton = cut.Find("[data-testid='close']");
        closeButton.TagName.ShouldBe("BUTTON");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<DialogCloseState>> render = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateDialogWithClose(render: render, nativeButton: false));

        var closeButton = cut.Find("span");
        closeButton.TextContent.ShouldBe("Close");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateDialogWithClose(closeAttributes: new Dictionary<string, object>
        {
            { "data-custom", "value" },
            { "aria-label", "close dialog" }
        }));

        var closeButton = cut.Find("[data-testid='close']");
        closeButton.GetAttribute("data-custom").ShouldBe("value");
        closeButton.GetAttribute("aria-label").ShouldBe("close dialog");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateDialogWithClose(
            classValue: state => state.Disabled ? "disabled-class" : "enabled-class"
        ));

        var closeButton = cut.Find("[data-testid='close']");
        closeButton.GetAttribute("class").ShouldContain("enabled-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateDialogWithClose(
            styleValue: _ => "background: green;"
        ));

        var closeButton = cut.Find("[data-testid='close']");
        closeButton.GetAttribute("style").ShouldContain("background: green");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledPreventsClosing()
    {
        var closeRequested = false;

        var cut = Render(CreateDialogWithClose(
            closeDisabled: true,
            onOpenChange: EventCallback.Factory.Create<DialogOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open)
                {
                    closeRequested = true;
                }
            })
        ));

        var closeButton = cut.Find("[data-testid='close']");
        closeButton.HasAttribute("disabled").ShouldBeTrue();
        closeButton.HasAttribute("data-disabled").ShouldBeTrue();

        closeButton.Click();

        closeRequested.ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledCustomElement()
    {
        RenderFragment<RenderProps<DialogCloseState>> render = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateDialogWithClose(
            render: render,
            closeDisabled: true,
            nativeButton: false
        ));

        var closeButton = cut.Find("[data-testid='close']");
        closeButton.HasAttribute("disabled").ShouldBeFalse();
        closeButton.HasAttribute("data-disabled").ShouldBeTrue();
        closeButton.GetAttribute("aria-disabled").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ClosesDialogOnClick()
    {
        var closeRequested = false;

        var cut = Render(CreateDialogWithClose(
            onOpenChange: EventCallback.Factory.Create<DialogOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open)
                {
                    closeRequested = true;
                }
            })
        ));

        cut.Find("[role='dialog']").ShouldNotBeNull();

        var closeButton = cut.Find("[data-testid='close']");
        closeButton.Click();

        closeRequested.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ClosesWithUndefinedOnClick()
    {
        var closeRequested = false;

        // Create dialog with explicit undefined/null onclick (simulated by not providing one)
        RenderFragment content = builder =>
        {
            builder.OpenComponent<DialogRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "Modal", Blazix.BaseUI.Dialog.DialogModalMode.False);
            builder.AddAttribute(3, "OnOpenChange", EventCallback.Factory.Create<DialogOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open)
                {
                    closeRequested = true;
                }
            }));
            builder.AddAttribute(4, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<DialogPortal>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogPopup>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<DialogClose>(0);
                        popupBuilder.AddAttribute(1, "data-testid", "close");
                        // No onclick attribute - simulating onClick={undefined}
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Close")));
                        popupBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(content);

        cut.Find("[role='dialog']").ShouldNotBeNull();

        var closeButton = cut.Find("[data-testid='close']");
        closeButton.Click();

        closeRequested.ShouldBeTrue();

        return Task.CompletedTask;
    }
}
