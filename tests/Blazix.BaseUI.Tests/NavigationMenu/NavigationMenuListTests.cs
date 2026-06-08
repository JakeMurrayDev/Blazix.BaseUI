namespace Blazix.BaseUI.Tests.NavigationMenu;

public class NavigationMenuListTests : BunitContext, INavigationMenuListContract
{
    public NavigationMenuListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNavigationMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
    }

    private RenderFragment CreateListInRoot(
        Func<NavigationMenuListState, string>? classValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuList>(0);
                var attrIndex = 1;
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                innerBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Items")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersUlByDefault()
    {
        var cut = Render(CreateListInRoot());

        var ul = cut.Find("ul");
        ul.TagName.ShouldBe("UL");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateListInRoot(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "nav-list" } }
        ));

        var ul = cut.Find("ul");
        ul.GetAttribute("data-testid").ShouldBe("nav-list");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateListInRoot(
            classValue: _ => "list-class"
        ));

        var ul = cut.Find("ul");
        ul.GetAttribute("class")!.ShouldContain("list-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasKeyDownHandlerWhenNotNested()
    {
        var cut = Render(CreateListInRoot());

        var ul = cut.Find("ul");
        ul.HasAttribute("blazor:onkeydown").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<NavigationMenuList>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Items"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
