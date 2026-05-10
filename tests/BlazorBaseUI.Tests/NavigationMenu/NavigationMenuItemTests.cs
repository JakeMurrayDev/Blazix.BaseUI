namespace BlazorBaseUI.Tests.NavigationMenu;

public class NavigationMenuItemTests : BunitContext, INavigationMenuItemContract
{
    public NavigationMenuItemTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNavigationMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
    }

    private RenderFragment CreateItemInRoot(
        string itemValue = "test-item",
        string? defaultValue = null,
        Func<NavigationMenuItemState, string>? classValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
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
                var attrIndex = 1;
                innerBuilder.AddAttribute(attrIndex++, "Value", itemValue);
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                innerBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item content")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersLiByDefault()
    {
        var cut = Render(CreateItemInRoot());

        var li = cut.Find("li");
        li.TagName.ShouldBe("LI");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateItemInRoot(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "nav-item" } }
        ));

        var li = cut.Find("li");
        li.GetAttribute("data-testid").ShouldBe("nav-item");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateItemInRoot(
            classValue: _ => "item-class"
        ));

        var li = cut.Find("li");
        li.GetAttribute("class")!.ShouldContain("item-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<NavigationMenuItem>(parameters => parameters
            .Add(p => p.Value, "test")
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Item"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
