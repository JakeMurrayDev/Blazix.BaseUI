using Blazix.BaseUI.Dialog;
using Blazix.BaseUI.Tests.Contracts.Dialog;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blazix.BaseUI.Tests.Dialog;

public class DialogTitleTests : BunitContext, IDialogTitleContract
{
    public DialogTitleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupDialogModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateDialogWithTitle(
        RenderFragment<RenderProps<DialogTitleState>>? render = null,
        Dictionary<string, object>? titleAttributes = null,
        Func<DialogTitleState, string>? classValue = null,
        Func<DialogTitleState, string>? styleValue = null)
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
                    portalBuilder.AddAttribute(1, "data-testid", "popup");
                    portalBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<DialogTitle>(0);
                        popupBuilder.AddAttribute(1, "data-testid", "title");

                        if (render is not null)
                            popupBuilder.AddAttribute(2, "Render", render);

                        if (titleAttributes is not null)
                        {
                            foreach (var (key, value) in titleAttributes)
                            {
                                popupBuilder.AddAttribute(3, key, value);
                            }
                        }

                        if (classValue is not null)
                            popupBuilder.AddAttribute(4, "ClassValue", classValue);

                        if (styleValue is not null)
                            popupBuilder.AddAttribute(5, "StyleValue", styleValue);

                        popupBuilder.AddAttribute(6, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Title Text")));
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
    public Task RendersAsH2ByDefault()
    {
        var cut = Render(CreateDialogWithTitle());

        var title = cut.Find("[data-testid='title']");
        title.TagName.ShouldBe("H2");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<DialogTitleState>> render = props => builder =>
        {
            builder.OpenElement(0, "h3");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateDialogWithTitle(render: render));

        var title = cut.Find("h3");
        title.TextContent.ShouldBe("Title Text");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateDialogWithTitle(titleAttributes: new Dictionary<string, object>
        {
            { "data-custom", "value" }
        }));

        var title = cut.Find("[data-testid='title']");
        title.GetAttribute("data-custom").ShouldBe("value");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateDialogWithTitle(
            classValue: _ => "title-class"
        ));

        var title = cut.Find("[data-testid='title']");
        title.GetAttribute("class").ShouldContain("title-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateDialogWithTitle(
            styleValue: _ => "font-size: 24px;"
        ));

        var title = cut.Find("[data-testid='title']");
        title.GetAttribute("style").ShouldContain("font-size: 24px");

        return Task.CompletedTask;
    }

    [Fact]
    public Task GeneratesIdForAriaLabelledBy()
    {
        var cut = Render(CreateDialogWithTitle());

        var popup = cut.Find("[data-testid='popup']");
        var title = cut.Find("[data-testid='title']");

        var titleId = title.GetAttribute("id");
        titleId.ShouldNotBeNullOrEmpty();
        popup.GetAttribute("aria-labelledby").ShouldBe(titleId);

        return Task.CompletedTask;
    }
}
