using BlazorBaseUI.Tests.Contracts.Tooltip;
using BlazorBaseUI.Tests.Infrastructure;
using BlazorBaseUI.Tooltip;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tests.Tooltip;

public class TooltipPortalTests : BunitContext, ITooltipPortalContract
{
    public TooltipPortalTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupTooltipModule(JSInterop);
    }

    private RenderFragment CreatePortalInRoot(
        bool defaultOpen = false,
        bool keepMounted = false,
        RenderFragment<RenderProps<object>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<TooltipRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<TooltipTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<TooltipPortal>(10);
                innerBuilder.AddAttribute(11, "KeepMounted", keepMounted);
                if (render is not null)
                {
                    innerBuilder.AddAttribute(12, "Render", render);
                }
                if (additionalAttributes is not null)
                {
                    foreach (var kvp in additionalAttributes)
                    {
                        innerBuilder.AddAttribute(13, kvp.Key, kvp.Value);
                    }
                }
                innerBuilder.AddAttribute(14, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<TooltipPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<TooltipPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
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
    public Task RendersChildrenWhenMounted()
    {
        var cut = Render(CreatePortalInRoot(defaultOpen: true, keepMounted: false));

        cut.Find("[role='tooltip']").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderChildrenWhenNotMounted()
    {
        var cut = Render(CreatePortalInRoot(defaultOpen: false, keepMounted: false));

        cut.FindAll("[role='tooltip']").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildrenWhenKeepMounted()
    {
        var cut = Render(CreatePortalInRoot(defaultOpen: false, keepMounted: true));

        // Portal content should be rendered even when closed
        cut.Find("[role='tooltip']").ShouldNotBeNull();
        cut.Find("[role='tooltip']").HasAttribute("data-closed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesPortalContext()
    {
        // The portal context with keepMounted should be available to children
        var cut = Render(CreatePortalInRoot(defaultOpen: false, keepMounted: true));

        // Content is rendered because keepMounted is true
        cut.Find("[role='presentation']").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        Should.Throw<InvalidOperationException>(() =>
            Render<TooltipPortal>(parameters => parameters
                .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
            )
        );

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreatePortalInRoot(
            defaultOpen: true,
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "portal" },
                { "aria-label", "Portal" }
            }
        ));

        var portal = cut.Find("[data-testid='portal']");
        portal.GetAttribute("aria-label").ShouldBe("Portal");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<object>> customRender = context => builder =>
        {
            builder.OpenElement(0, "section");
            foreach (var attr in context.Attributes)
            {
                builder.AddAttribute(1, attr.Key, attr.Value);
            }
            if (context.ChildContent is not null)
            {
                builder.AddContent(2, context.ChildContent);
            }
            builder.CloseElement();
        };

        var cut = Render(CreatePortalInRoot(
            defaultOpen: true,
            render: customRender
        ));

        var section = cut.Find("section[data-blazor-base-ui-portal]");
        section.TagName.ShouldBe("SECTION");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreatePortalInRoot(defaultOpen: true));

        var portal = cut.FindComponent<TooltipPortal>();
        portal.Instance.Element.ShouldNotBeNull();

        return Task.CompletedTask;
    }
}
