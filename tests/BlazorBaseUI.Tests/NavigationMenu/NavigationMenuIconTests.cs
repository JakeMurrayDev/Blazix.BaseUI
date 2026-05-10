namespace BlazorBaseUI.Tests.NavigationMenu;

public class NavigationMenuIconTests : BunitContext, INavigationMenuIconContract
{
    public NavigationMenuIconTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNavigationMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
    }

    private RenderFragment CreateIconInRoot(
        string? defaultValue = null,
        Func<NavigationMenuIconState, string>? classValue = null)
    {
        return builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            var rootAttr = 1;
            if (defaultValue is not null)
                builder.AddAttribute(rootAttr++, "DefaultValue", defaultValue);
            builder.AddAttribute(rootAttr++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuItem>(0);
                innerBuilder.AddAttribute(1, "Value", "item1");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<NavigationMenuIcon>(0);
                    var attrIndex = 1;
                    if (classValue is not null)
                        itemBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                    itemBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersSpanByDefault()
    {
        var cut = Render(CreateIconInRoot());

        var span = cut.Find("span");
        span.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDefaultArrowContent()
    {
        var cut = Render(CreateIconInRoot());

        var span = cut.Find("span");
        span.TextContent.ShouldContain("\u25bc");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaHidden()
    {
        var cut = Render(CreateIconInRoot());

        var span = cut.Find("span");
        span.GetAttribute("aria-hidden").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task NoDataPopupOpenWhenClosed()
    {
        var cut = Render(CreateIconInRoot());

        var span = cut.Find("span");
        span.HasAttribute("data-popup-open").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataPopupOpenWhenOpen()
    {
        var cut = Render(CreateIconInRoot(defaultValue: "item1"));

        var span = cut.Find("span");
        span.HasAttribute("data-popup-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateIconInRoot(
            classValue: state => state.Open ? "open-icon" : "closed-icon"
        ));

        var span = cut.Find("span");
        span.GetAttribute("class")!.ShouldContain("closed-icon");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<NavigationMenuIcon>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Icon"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
