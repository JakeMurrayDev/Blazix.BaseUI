using Blazix.BaseUI.Dialog;
using Blazix.BaseUI.Tests.Contracts.Dialog;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blazix.BaseUI.Tests.Dialog;

public class DialogViewportTests : BunitContext, IDialogViewportContract
{
    public DialogViewportTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupDialogModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateDialogWithViewport(
        bool open = true,
        RenderFragment<RenderProps<DialogViewportState>>? render = null,
        bool keepMounted = false,
        Dictionary<string, object>? viewportAttributes = null,
        Func<DialogViewportState, string>? classValue = null,
        Func<DialogViewportState, string>? styleValue = null)
    {
        return builder =>
        {
            builder.OpenComponent<DialogRoot>(0);
            builder.AddAttribute(1, "Open", open);
            builder.AddAttribute(2, "Modal", Blazix.BaseUI.Dialog.DialogModalMode.False);
            builder.AddAttribute(3, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<DialogPortal>(0);
                innerBuilder.AddAttribute(1, "KeepMounted", keepMounted);
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogViewport>(0);
                    portalBuilder.AddAttribute(1, "data-testid", "viewport");

                    if (render is not null)
                        portalBuilder.AddAttribute(2, "Render", render);

                    if (viewportAttributes is not null)
                    {
                        foreach (var (key, value) in viewportAttributes)
                        {
                            portalBuilder.AddAttribute(3, key, value);
                        }
                    }

                    if (classValue is not null)
                        portalBuilder.AddAttribute(4, "ClassValue", classValue);

                    if (styleValue is not null)
                        portalBuilder.AddAttribute(5, "StyleValue", styleValue);

                    portalBuilder.AddAttribute(6, "ChildContent", (RenderFragment)(viewportBuilder =>
                    {
                        viewportBuilder.OpenComponent<DialogPopup>(0);
                        viewportBuilder.AddAttribute(1, "data-testid", "popup");
                        viewportBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                        viewportBuilder.CloseComponent();
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
        var cut = Render(CreateDialogWithViewport());

        var viewport = cut.Find("[data-testid='viewport']");
        viewport.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<DialogViewportState>> render = props => builder =>
        {
            builder.OpenElement(0, "section");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateDialogWithViewport(render: render));

        var viewport = cut.Find("section");
        viewport.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateDialogWithViewport(viewportAttributes: new Dictionary<string, object>
        {
            { "data-custom", "value" }
        }));

        var viewport = cut.Find("[data-testid='viewport']");
        viewport.GetAttribute("data-custom").ShouldBe("value");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateDialogWithViewport(
            classValue: state => state.Open ? "open-viewport" : "closed-viewport"
        ));

        var viewport = cut.Find("[data-testid='viewport']");
        viewport.GetAttribute("class").ShouldContain("open-viewport");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateDialogWithViewport(
            styleValue: _ => "overflow: auto;"
        ));

        var viewport = cut.Find("[data-testid='viewport']");
        viewport.GetAttribute("style").ShouldContain("overflow: auto");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRolePresentation()
    {
        var cut = Render(CreateDialogWithViewport());

        var viewport = cut.Find("[data-testid='viewport']");
        viewport.GetAttribute("role").ShouldBe("presentation");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersOnlyWhenMounted()
    {
        var cut = Render(CreateDialogWithViewport(open: false, keepMounted: false));

        cut.FindAll("[data-testid='viewport']").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task StaysMountedWithKeepMounted()
    {
        var cut = Render(CreateDialogWithViewport(open: false, keepMounted: true));

        var viewport = cut.Find("[data-testid='viewport']");
        viewport.ShouldNotBeNull();

        return Task.CompletedTask;
    }
}
