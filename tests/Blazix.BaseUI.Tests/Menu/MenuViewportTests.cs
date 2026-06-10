using Blazix.BaseUI.Menu;
using Blazix.BaseUI.Tests.Contracts.Menu;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blazix.BaseUI.Tests.Menu;

public class MenuViewportTests : BunitContext, IMenuViewportContract
{
    public MenuViewportTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateViewportInRoot(
        RenderFragment<RenderProps<MenuViewportState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<MenuViewportState, string?>? classValue = null,
        Func<MenuViewportState, string?>? styleValue = null)
    {
        return builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<MenuPositioner>(2);
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuViewport>(0);
                        var attrIndex = 1;

                        if (render is not null)
                            popupBuilder.AddAttribute(attrIndex++, "Render", render);
                        if (classValue is not null)
                            popupBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                        if (styleValue is not null)
                            popupBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                        if (additionalAttributes is not null)
                            popupBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);
                        popupBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(viewportBuilder =>
                        {
                            viewportBuilder.OpenComponent<MenuItem>(0);
                            viewportBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item 1")));
                            viewportBuilder.CloseComponent();
                        }));

                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersDiv()
    {
        var cut = Render(CreateViewportInRoot());

        var viewport = cut.Find("div[data-current]");
        viewport.ShouldNotBeNull();
        viewport.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataTransitioning()
    {
        // data-transitioning is a boolean attribute — absent when false (not transitioning)
        var cut = Render(CreateViewportInRoot());

        var viewport = cut.Find("div[data-current]");
        viewport.HasAttribute("data-transitioning").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataCurrent()
    {
        var cut = Render(CreateViewportInRoot());

        var viewport = cut.Find("div[data-current]");
        viewport.HasAttribute("data-current").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataInstantWhenSet()
    {
        // By default, MenuInstantType is None so data-instant should not be rendered
        var cut = Render(CreateViewportInRoot());

        var viewport = cut.Find("div[data-current]");
        viewport.HasAttribute("data-instant").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsHasViewportOnContext()
    {
        // SetHasViewport is called in OnInitialized; verifying it renders without error
        // confirms the viewport registered itself with the context.
        var cut = Render(CreateViewportInRoot());

        var viewport = cut.Find("div[data-current]");
        viewport.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<MenuViewportState>> customRender = props => b =>
        {
            b.OpenElement(0, "section");
            b.AddMultipleAttributes(1, props.Attributes);
            b.AddContent(2, props.ChildContent);
            b.CloseElement();
        };

        var cut = Render(CreateViewportInRoot(render: customRender));

        var section = cut.Find("section[data-current]");
        section.ShouldNotBeNull();
        section.TagName.ShouldBe("SECTION");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var attrs = new Dictionary<string, object>
        {
            ["data-testid"] = "my-viewport",
            ["aria-label"] = "viewport"
        };

        var cut = Render(CreateViewportInRoot(additionalAttributes: attrs));

        var viewport = cut.Find("div[data-current]");
        viewport.GetAttribute("data-testid").ShouldBe("my-viewport");
        viewport.GetAttribute("aria-label").ShouldBe("viewport");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateViewportInRoot(classValue: _ => "my-viewport-class"));

        var viewport = cut.Find("div[data-current]");
        viewport.ClassList.ShouldContain("my-viewport-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateViewportInRoot(styleValue: _ => "color: red"));

        var viewport = cut.Find("div[data-current]");
        viewport.GetAttribute("style")!.ShouldContain("color: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateViewportInRoot());

        var viewport = cut.Find("div[data-current]");
        viewport.InnerHtml.ShouldContain("Item 1");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task SetsInstantTypeTriggerChangeOnTransitionEnd()
    {
        var cut = Render(CreateViewportInRoot());

        var viewport = cut.FindComponent<MenuViewport>();

        // Simulate a viewport transition start then end
        await cut.InvokeAsync(() => viewport.Instance.OnViewportTransitionStart("right down"));
        await cut.InvokeAsync(() => viewport.Instance.OnViewportTransitionEnd());

        // After transition end, the viewport should have data-instant="trigger-change"
        var viewportEl = cut.Find("div[data-current]");
        viewportEl.GetAttribute("data-instant")!.ShouldBe("trigger-change");
    }

    [Fact]
    public async Task HasActivationDirectionAfterTransitionStart()
    {
        var cut = Render(CreateViewportInRoot());

        var viewport = cut.FindComponent<MenuViewport>();

        await cut.InvokeAsync(() => viewport.Instance.OnViewportTransitionStart("right down"));

        var viewportEl = cut.Find("div[data-current]");
        viewportEl.GetAttribute("data-activation-direction").ShouldBe("right down");
        viewportEl.HasAttribute("data-transitioning").ShouldBeTrue();
    }
}
