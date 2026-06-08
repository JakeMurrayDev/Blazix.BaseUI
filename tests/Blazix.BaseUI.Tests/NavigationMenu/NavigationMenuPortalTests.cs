namespace Blazix.BaseUI.Tests.NavigationMenu;

public class NavigationMenuPortalTests : BunitContext, INavigationMenuPortalContract
{
    public NavigationMenuPortalTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNavigationMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
    }

    [Fact]
    public Task RendersWhenMounted()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "DefaultValue", "item1");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuItem>(0);
                innerBuilder.AddAttribute(1, "Value", "item1");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<NavigationMenuTrigger>(0);
                    itemBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                    itemBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<NavigationMenuPortal>(4);
                innerBuilder.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Portal content")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        cut.Markup.ShouldContain("Portal content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderWhenNotMounted()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuPortal>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Portal content")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        cut.Markup.ShouldNotContain("Portal content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWhenKeepMounted()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuPortal>(0);
                innerBuilder.AddAttribute(1, "KeepMounted", true);
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Portal content")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        cut.Markup.ShouldContain("Portal content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesPortalContext()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuPortal>(0);
                innerBuilder.AddAttribute(1, "KeepMounted", true);
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<NavigationMenuPositioner>(0);
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        // Positioner renders inside the portal, proving context cascades through
        cut.Find("[role='presentation']").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresRootContext()
    {
        Should.Throw<InvalidOperationException>(() =>
            Render<NavigationMenuPortal>(parameters => parameters
                .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
            )
        );

        return Task.CompletedTask;
    }
}
