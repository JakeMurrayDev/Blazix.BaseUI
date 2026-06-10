using Blazix.BaseUI.Dialog;
using Blazix.BaseUI.Tests.Contracts.Dialog;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blazix.BaseUI.Tests.Dialog;

public class DialogPopupTests : BunitContext, IDialogPopupContract
{
    public DialogPopupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupDialogModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateDialogWithPopup(
        bool open = true,
        Blazix.BaseUI.Dialog.DialogModalMode modal = Blazix.BaseUI.Dialog.DialogModalMode.False,
        RenderFragment<RenderProps<DialogPopupState>>? render = null,
        Dictionary<string, object>? popupAttributes = null,
        Func<DialogPopupState, string>? classValue = null,
        Func<DialogPopupState, string>? styleValue = null,
        bool keepMounted = false)
    {
        return builder =>
        {
            builder.OpenComponent<DialogRoot>(0);
            builder.AddAttribute(1, "Open", open);
            builder.AddAttribute(2, "Modal", modal);
            builder.AddAttribute(3, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<DialogPortal>(0);
                innerBuilder.AddAttribute(1, "KeepMounted", keepMounted);
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogPopup>(0);
                    portalBuilder.AddAttribute(1, "data-testid", "dialog-popup");

                    if (render is not null)
                        portalBuilder.AddAttribute(2, "Render", render);

                    if (popupAttributes is not null)
                    {
                        foreach (var (key, value) in popupAttributes)
                        {
                            portalBuilder.AddAttribute(3, key, value);
                        }
                    }

                    if (classValue is not null)
                        portalBuilder.AddAttribute(4, "ClassValue", classValue);

                    if (styleValue is not null)
                        portalBuilder.AddAttribute(5, "StyleValue", styleValue);

                    portalBuilder.AddAttribute(6, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateNestedDialog()
    {
        return builder =>
        {
            builder.OpenComponent<DialogRoot>(0);
            builder.AddAttribute(1, "Open", true);
            builder.AddAttribute(2, "Modal", Blazix.BaseUI.Dialog.DialogModalMode.False);
            builder.AddAttribute(3, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<DialogPortal>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogPopup>(0);
                    portalBuilder.AddAttribute(1, "data-testid", "parent-popup");
                    portalBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        // Nested dialog
                        popupBuilder.OpenComponent<DialogRoot>(0);
                        popupBuilder.AddAttribute(1, "Open", true);
                        popupBuilder.AddAttribute(2, "Modal", Blazix.BaseUI.Dialog.DialogModalMode.False);
                        popupBuilder.AddAttribute(3, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => nestedInnerBuilder =>
                        {
                            nestedInnerBuilder.OpenComponent<DialogPortal>(0);
                            nestedInnerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(nestedPortalBuilder =>
                            {
                                nestedPortalBuilder.OpenComponent<DialogPopup>(0);
                                nestedPortalBuilder.AddAttribute(1, "data-testid", "nested-popup");
                                nestedPortalBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Nested")));
                                nestedPortalBuilder.CloseComponent();
                            }));
                            nestedInnerBuilder.CloseComponent();
                        }));
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
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateDialogWithPopup());

        var popup = cut.Find("[data-testid='dialog-popup']");
        popup.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<DialogPopupState>> render = props => builder =>
        {
            builder.OpenElement(0, "section");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateDialogWithPopup(render: render));

        var popup = cut.Find("section");
        popup.TextContent.ShouldBe("Content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateDialogWithPopup(popupAttributes: new Dictionary<string, object>
        {
            { "data-custom", "value" },
            { "aria-label", "test label" }
        }));

        var popup = cut.Find("[data-testid='dialog-popup']");
        popup.GetAttribute("data-custom").ShouldBe("value");
        popup.GetAttribute("aria-label").ShouldBe("test label");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateDialogWithPopup(
            classValue: state => state.Open ? "open-class" : "closed-class"
        ));

        var popup = cut.Find("[data-testid='dialog-popup']");
        popup.GetAttribute("class").ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateDialogWithPopup(
            styleValue: _ => "background: red;"
        ));

        var popup = cut.Find("[data-testid='dialog-popup']");
        popup.GetAttribute("style").ShouldContain("background: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task KeepMountedTrue_DialogStaysMounted()
    {
        var cut = Render(CreateDialogWithPopup(open: false, keepMounted: true));

        var popup = cut.Find("[role='dialog']");
        popup.ShouldNotBeNull();
        popup.HasAttribute("hidden").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task KeepMountedFalse_DialogUnmounts()
    {
        var cut = Render(CreateDialogWithPopup(open: false, keepMounted: false));

        cut.FindAll("[role='dialog']").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRoleDialog()
    {
        var cut = Render(CreateDialogWithPopup());

        var popup = cut.Find("[data-testid='dialog-popup']");
        popup.GetAttribute("role").ShouldBe("dialog");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaModalTrueWhenModal()
    {
        var cut = Render(CreateDialogWithPopup(modal: Blazix.BaseUI.Dialog.DialogModalMode.True));

        var popup = cut.Find("[data-testid='dialog-popup']");
        popup.GetAttribute("aria-modal").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreateDialogWithPopup(open: true));

        var popup = cut.Find("[data-testid='dialog-popup']");
        popup.HasAttribute("data-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreateDialogWithPopup(open: false, keepMounted: true));

        var popup = cut.Find("[data-testid='dialog-popup']");
        popup.HasAttribute("data-closed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataNestedWhenNested()
    {
        var cut = Render(CreateNestedDialog());

        var nestedPopup = cut.Find("[data-testid='nested-popup']");
        nestedPopup.HasAttribute("data-nested").ShouldBeTrue();

        var parentPopup = cut.Find("[data-testid='parent-popup']");
        parentPopup.HasAttribute("data-nested").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabIndexNegativeOne()
    {
        var cut = Render(CreateDialogWithPopup());

        var popup = cut.Find("[data-testid='dialog-popup']");
        popup.GetAttribute("tabindex").ShouldBe("-1");

        return Task.CompletedTask;
    }
}
