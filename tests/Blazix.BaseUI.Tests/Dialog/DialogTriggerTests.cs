using Blazix.BaseUI.Dialog;
using Blazix.BaseUI.Tests.Contracts.Dialog;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blazix.BaseUI.Tests.Dialog;

public class DialogTriggerTests : BunitContext, IDialogTriggerContract
{
    public DialogTriggerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupDialogModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateDialogWithTrigger(
        bool open = false,
        RenderFragment<RenderProps<DialogTriggerState>>? render = null,
        bool triggerDisabled = false,
        bool nativeButton = true,
        Dictionary<string, object>? triggerAttributes = null,
        Func<DialogTriggerState, string>? classValue = null,
        Func<DialogTriggerState, string>? styleValue = null)
    {
        return builder =>
        {
            builder.OpenComponent<DialogRoot>(0);
            builder.AddAttribute(1, "Open", open);
            builder.AddAttribute(2, "Modal", Blazix.BaseUI.Dialog.DialogModalMode.False);
            builder.AddAttribute(3, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<DialogTrigger>(0);
                innerBuilder.AddAttribute(1, "data-testid", "trigger");

                if (render is not null)
                    innerBuilder.AddAttribute(2, "Render", render);

                innerBuilder.AddAttribute(3, "Disabled", triggerDisabled);
                innerBuilder.AddAttribute(4, "NativeButton", nativeButton);

                if (triggerAttributes is not null)
                {
                    foreach (var (key, value) in triggerAttributes)
                    {
                        innerBuilder.AddAttribute(5, key, value);
                    }
                }

                if (classValue is not null)
                    innerBuilder.AddAttribute(6, "ClassValue", classValue);

                if (styleValue is not null)
                    innerBuilder.AddAttribute(7, "StyleValue", styleValue);

                innerBuilder.AddAttribute(8, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Open")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<DialogPortal>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogPopup>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
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
        var cut = Render(CreateDialogWithTrigger());

        var trigger = cut.Find("[data-testid='trigger']");
        trigger.TagName.ShouldBe("BUTTON");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<DialogTriggerState>> render = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateDialogWithTrigger(render: render, nativeButton: false));

        var trigger = cut.Find("span");
        trigger.TextContent.ShouldBe("Open");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateDialogWithTrigger(triggerAttributes: new Dictionary<string, object>
        {
            { "data-custom", "value" },
            { "aria-label", "open dialog" }
        }));

        var trigger = cut.Find("[data-testid='trigger']");
        trigger.GetAttribute("data-custom").ShouldBe("value");
        trigger.GetAttribute("aria-label").ShouldBe("open dialog");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateDialogWithTrigger(
            classValue: state => state.Disabled ? "disabled-class" : "enabled-class"
        ));

        var trigger = cut.Find("[data-testid='trigger']");
        trigger.GetAttribute("class").ShouldContain("enabled-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateDialogWithTrigger(
            styleValue: _ => "background: blue;"
        ));

        var trigger = cut.Find("[data-testid='trigger']");
        trigger.GetAttribute("style").ShouldContain("background: blue");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledPreventsOpening()
    {
        var openRequested = false;

        RenderFragment content = builder =>
        {
            builder.OpenComponent<DialogRoot>(0);
            builder.AddAttribute(1, "Modal", Blazix.BaseUI.Dialog.DialogModalMode.False);
            builder.AddAttribute(2, "OnOpenChange", EventCallback.Factory.Create<DialogOpenChangeEventArgs>(this, args =>
            {
                openRequested = args.Open;
            }));
            builder.AddAttribute(3, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<DialogTrigger>(0);
                innerBuilder.AddAttribute(1, "Disabled", true);
                innerBuilder.AddAttribute(2, "data-testid", "trigger");
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Open")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<DialogPortal>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogPopup>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(content);

        var trigger = cut.Find("[data-testid='trigger']");
        trigger.HasAttribute("disabled").ShouldBeTrue();
        trigger.HasAttribute("data-disabled").ShouldBeTrue();

        trigger.Click();

        openRequested.ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledCustomElement()
    {
        RenderFragment<RenderProps<DialogTriggerState>> render = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateDialogWithTrigger(
            render: render,
            triggerDisabled: true,
            nativeButton: false
        ));

        var trigger = cut.Find("[data-testid='trigger']");
        trigger.HasAttribute("disabled").ShouldBeFalse();
        trigger.HasAttribute("data-disabled").ShouldBeTrue();
        trigger.GetAttribute("aria-disabled").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaHasPopupDialog()
    {
        var cut = Render(CreateDialogWithTrigger());

        var trigger = cut.Find("[data-testid='trigger']");
        trigger.GetAttribute("aria-haspopup").ShouldBe("dialog");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedFalseWhenClosed()
    {
        var cut = Render(CreateDialogWithTrigger(open: false));

        var trigger = cut.Find("[data-testid='trigger']");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedTrueWhenOpen()
    {
        var cut = Render(CreateDialogWithTrigger(open: true));

        var trigger = cut.Find("[data-testid='trigger']");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DetachedTriggerSetsAriaControlsWhenItOpensHandledDialog()
    {
        var handle = new DialogHandle<object?>();

        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<DialogTrigger>(0);
            builder.AddAttribute(1, "Handle", handle);
            builder.AddAttribute(2, "Id", "detached-trigger");
            builder.AddAttribute(3, "data-testid", "detached-trigger");
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(triggerBuilder => triggerBuilder.AddContent(0, "Open")));
            builder.CloseComponent();

            builder.OpenComponent<DialogRoot>(10);
            builder.AddAttribute(11, "Handle", (IDialogHandle)handle);
            builder.AddAttribute(12, "Modal", Blazix.BaseUI.Dialog.DialogModalMode.False);
            builder.AddAttribute(13, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => rootBuilder =>
            {
                rootBuilder.OpenComponent<DialogPortal>(0);
                rootBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogPopup>(0);
                    portalBuilder.AddAttribute(1, "data-testid", "dialog-popup");
                    portalBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(popupBuilder => popupBuilder.AddContent(0, "Content")));
                    portalBuilder.CloseComponent();
                }));
                rootBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(fragment);

        cut.Find("[data-testid='detached-trigger']").Click();

        cut.WaitForAssertion(() =>
        {
            var trigger = cut.Find("[data-testid='detached-trigger']");
            var popup = cut.Find("[data-testid='dialog-popup']");
            var popupId = popup.GetAttribute("id");

            popupId.ShouldNotBeNullOrEmpty();
            trigger.GetAttribute("aria-expanded").ShouldBe("true");
            trigger.GetAttribute("aria-controls").ShouldBe(popupId);
        });

        return Task.CompletedTask;
    }
}
