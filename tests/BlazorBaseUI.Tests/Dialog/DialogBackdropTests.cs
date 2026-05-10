using BlazorBaseUI.Dialog;
using BlazorBaseUI.Tests.Contracts.Dialog;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Dialog;

public class DialogBackdropTests : BunitContext, IDialogBackdropContract
{
    public DialogBackdropTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupDialogModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateDialogWithBackdrop(
        bool open = true,
        BlazorBaseUI.Dialog.DialogModalMode modal = BlazorBaseUI.Dialog.DialogModalMode.True,
        RenderFragment<RenderProps<DialogBackdropState>>? render = null,
        bool forceRender = false,
        Dictionary<string, object>? backdropAttributes = null,
        Func<DialogBackdropState, string>? classValue = null,
        Func<DialogBackdropState, string>? styleValue = null,
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
                    portalBuilder.OpenComponent<DialogBackdrop>(0);
                    portalBuilder.AddAttribute(1, "data-testid", "backdrop");
                    portalBuilder.AddAttribute(2, "ForceRender", forceRender);

                    if (render is not null)
                        portalBuilder.AddAttribute(3, "Render", render);

                    if (backdropAttributes is not null)
                    {
                        foreach (var (key, value) in backdropAttributes)
                        {
                            portalBuilder.AddAttribute(4, key, value);
                        }
                    }

                    if (classValue is not null)
                        portalBuilder.AddAttribute(5, "ClassValue", classValue);

                    if (styleValue is not null)
                        portalBuilder.AddAttribute(6, "StyleValue", styleValue);

                    portalBuilder.CloseComponent();

                    portalBuilder.OpenComponent<DialogPopup>(10);
                    portalBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateNestedDialogWithBackdrop(bool forceRender = false)
    {
        return builder =>
        {
            builder.OpenComponent<DialogRoot>(0);
            builder.AddAttribute(1, "Open", true);
            builder.AddAttribute(2, "Modal", BlazorBaseUI.Dialog.DialogModalMode.True);
            builder.AddAttribute(3, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<DialogPortal>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogBackdrop>(0);
                    portalBuilder.AddAttribute(1, "data-testid", "root-backdrop");
                    portalBuilder.AddAttribute(2, "ForceRender", forceRender);
                    portalBuilder.CloseComponent();

                    portalBuilder.OpenComponent<DialogPopup>(10);
                    portalBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        // Nested dialog
                        popupBuilder.OpenComponent<DialogRoot>(0);
                        popupBuilder.AddAttribute(1, "Open", true);
                        popupBuilder.AddAttribute(2, "Modal", BlazorBaseUI.Dialog.DialogModalMode.True);
                        popupBuilder.AddAttribute(3, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => nestedInnerBuilder =>
                        {
                            nestedInnerBuilder.OpenComponent<DialogPortal>(0);
                            nestedInnerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(nestedPortalBuilder =>
                            {
                                nestedPortalBuilder.OpenComponent<DialogBackdrop>(0);
                                nestedPortalBuilder.AddAttribute(1, "data-testid", "nested-backdrop");
                                nestedPortalBuilder.AddAttribute(2, "ForceRender", forceRender);
                                nestedPortalBuilder.CloseComponent();

                                nestedPortalBuilder.OpenComponent<DialogPopup>(10);
                                nestedPortalBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Nested")));
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
        var cut = Render(CreateDialogWithBackdrop());

        var backdrop = cut.Find("[data-testid='backdrop']");
        backdrop.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<DialogBackdropState>> render = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateDialogWithBackdrop(render: render));

        var backdrop = cut.Find("span");
        backdrop.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateDialogWithBackdrop(backdropAttributes: new Dictionary<string, object>
        {
            { "data-custom", "value" }
        }));

        var backdrop = cut.Find("[data-testid='backdrop']");
        backdrop.GetAttribute("data-custom").ShouldBe("value");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateDialogWithBackdrop(
            classValue: state => state.Open ? "open-class" : "closed-class"
        ));

        var backdrop = cut.Find("[data-testid='backdrop']");
        backdrop.GetAttribute("class").ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateDialogWithBackdrop(
            styleValue: _ => "opacity: 0.5;"
        ));

        var backdrop = cut.Find("[data-testid='backdrop']");
        backdrop.GetAttribute("style").ShouldContain("opacity: 0.5");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRolePresentation()
    {
        var cut = Render(CreateDialogWithBackdrop());

        var backdrop = cut.Find("[data-testid='backdrop']");
        backdrop.GetAttribute("role").ShouldBe("presentation");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWhenModalFalse()
    {
        // Fix 02: Source has no modal guard on backdrop — backdrop renders for non-modal dialogs if included in markup
        var cut = Render(CreateDialogWithBackdrop(modal: BlazorBaseUI.Dialog.DialogModalMode.False));

        cut.FindAll("[data-testid='backdrop']").Count.ShouldBe(1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForceRenderOnlyRootByDefault()
    {
        var cut = Render(CreateNestedDialogWithBackdrop(forceRender: false));

        cut.FindAll("[data-testid='root-backdrop']").Count.ShouldBe(1);
        cut.FindAll("[data-testid='nested-backdrop']").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForceRenderAllWhenTrue()
    {
        var cut = Render(CreateNestedDialogWithBackdrop(forceRender: true));

        cut.FindAll("[data-testid='root-backdrop']").Count.ShouldBe(1);
        cut.FindAll("[data-testid='nested-backdrop']").Count.ShouldBe(1);

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreateDialogWithBackdrop(open: true));

        var backdrop = cut.Find("[data-testid='backdrop']");
        backdrop.HasAttribute("data-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreateDialogWithBackdrop(open: false, keepMounted: true));

        var backdrop = cut.Find("[data-testid='backdrop']");
        backdrop.HasAttribute("data-closed").ShouldBeTrue();

        return Task.CompletedTask;
    }
}
