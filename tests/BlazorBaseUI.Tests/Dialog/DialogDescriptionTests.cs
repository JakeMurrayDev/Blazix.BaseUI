using BlazorBaseUI.Dialog;
using BlazorBaseUI.Tests.Contracts.Dialog;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Dialog;

public class DialogDescriptionTests : BunitContext, IDialogDescriptionContract
{
    public DialogDescriptionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupDialogModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateDialogWithDescription(
        RenderFragment<RenderProps<DialogDescriptionState>>? render = null,
        Dictionary<string, object>? descriptionAttributes = null,
        Func<DialogDescriptionState, string>? classValue = null,
        Func<DialogDescriptionState, string>? styleValue = null)
    {
        return builder =>
        {
            builder.OpenComponent<DialogRoot>(0);
            builder.AddAttribute(1, "Open", true);
            builder.AddAttribute(2, "Modal", BlazorBaseUI.Dialog.DialogModalMode.False);
            builder.AddAttribute(3, "ChildContent", (RenderFragment<DialogRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<DialogPortal>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<DialogPopup>(0);
                    portalBuilder.AddAttribute(1, "data-testid", "popup");
                    portalBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<DialogDescription>(0);
                        popupBuilder.AddAttribute(1, "data-testid", "description");

                        if (render is not null)
                            popupBuilder.AddAttribute(2, "Render", render);

                        if (descriptionAttributes is not null)
                        {
                            foreach (var (key, value) in descriptionAttributes)
                            {
                                popupBuilder.AddAttribute(3, key, value);
                            }
                        }

                        if (classValue is not null)
                            popupBuilder.AddAttribute(4, "ClassValue", classValue);

                        if (styleValue is not null)
                            popupBuilder.AddAttribute(5, "StyleValue", styleValue);

                        popupBuilder.AddAttribute(6, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Description Text")));
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
    public Task RendersAsParagraphByDefault()
    {
        var cut = Render(CreateDialogWithDescription());

        var description = cut.Find("[data-testid='description']");
        description.TagName.ShouldBe("P");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<DialogDescriptionState>> render = props => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateDialogWithDescription(render: render));

        var description = cut.Find("div");
        description.TextContent.ShouldBe("Description Text");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateDialogWithDescription(descriptionAttributes: new Dictionary<string, object>
        {
            { "data-custom", "value" }
        }));

        var description = cut.Find("[data-testid='description']");
        description.GetAttribute("data-custom").ShouldBe("value");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateDialogWithDescription(
            classValue: _ => "description-class"
        ));

        var description = cut.Find("[data-testid='description']");
        description.GetAttribute("class").ShouldContain("description-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateDialogWithDescription(
            styleValue: _ => "color: gray;"
        ));

        var description = cut.Find("[data-testid='description']");
        description.GetAttribute("style").ShouldContain("color: gray");

        return Task.CompletedTask;
    }

    [Fact]
    public Task GeneratesIdForAriaDescribedBy()
    {
        var cut = Render(CreateDialogWithDescription());

        var popup = cut.Find("[data-testid='popup']");
        var description = cut.Find("[data-testid='description']");

        var descriptionId = description.GetAttribute("id");
        descriptionId.ShouldNotBeNullOrEmpty();
        popup.GetAttribute("aria-describedby").ShouldBe(descriptionId);

        return Task.CompletedTask;
    }
}
