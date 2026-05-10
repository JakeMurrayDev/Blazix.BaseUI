namespace BlazorBaseUI.Tests.NavigationMenu;

public class NavigationMenuContentTests : BunitContext, INavigationMenuContentContract
{
    public NavigationMenuContentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNavigationMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
    }

    private RenderFragment CreateContentInRoot(
        string? defaultValue = null,
        bool keepMounted = false,
        Func<NavigationMenuContentState, string>? classValue = null,
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
                innerBuilder.AddAttribute(1, "Value", "item1");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<NavigationMenuTrigger>(0);
                    itemBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                    itemBuilder.CloseComponent();

                    itemBuilder.OpenComponent<NavigationMenuContent>(2);
                    var attrIndex = 3;
                    if (keepMounted)
                        itemBuilder.AddAttribute(attrIndex++, "KeepMounted", true);
                    if (classValue is not null)
                        itemBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                    if (additionalAttributes is not null)
                        itemBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                    itemBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
                    itemBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersDivByDefault()
    {
        var cut = Render(CreateContentInRoot(defaultValue: "item1"));

        var content = cut.Find("div[data-open]");
        content.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateContentInRoot(
            defaultValue: "item1",
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "content" } }
        ));

        var content = cut.Find("div[data-testid='content']");
        content.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenActive()
    {
        var cut = Render(CreateContentInRoot(defaultValue: "item1"));

        var content = cut.Find("div[data-open]");
        content.HasAttribute("data-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenInactiveWithKeepMounted()
    {
        var cut = Render(CreateContentInRoot(keepMounted: true));

        var content = cut.Find("div[data-closed]");
        content.HasAttribute("data-closed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderWhenInactiveWithoutKeepMounted()
    {
        var cut = Render(CreateContentInRoot());

        cut.FindAll("div[data-closed]").ShouldBeEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateContentInRoot(
            defaultValue: "item1",
            classValue: state => state.Open ? "active-class" : "inactive-class"
        ));

        var content = cut.Find("div[data-open]");
        content.GetAttribute("class")!.ShouldContain("active-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<NavigationMenuContent>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
