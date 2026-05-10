namespace BlazorBaseUI.Tests.NavigationMenu;

public class NavigationMenuPositionerTests : BunitContext, INavigationMenuPositionerContract
{
    public NavigationMenuPositionerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNavigationMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
    }

    private RenderFragment CreatePositionerInRoot(
        Func<NavigationMenuPositionerState, string>? classValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuPositioner>(0);
                var attrIndex = 1;
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                innerBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Positioner content")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersDivByDefault()
    {
        var cut = Render(CreatePositionerInRoot());

        var div = cut.Find("div[role='presentation']");
        div.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreatePositionerInRoot(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "positioner" } }
        ));

        var div = cut.Find("div[data-testid='positioner']");
        div.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRolePresentation()
    {
        var cut = Render(CreatePositionerInRoot());

        var div = cut.Find("div[role='presentation']");
        div.GetAttribute("role").ShouldBe("presentation");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataSide()
    {
        var cut = Render(CreatePositionerInRoot());

        var div = cut.Find("div[role='presentation']");
        div.GetAttribute("data-side").ShouldBe("bottom");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataAlign()
    {
        var cut = Render(CreatePositionerInRoot());

        var div = cut.Find("div[role='presentation']");
        div.GetAttribute("data-align").ShouldBe("center");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task DataSideAndAlignReflectComputedPlacement()
    {
        var cut = Render(CreatePositionerInRoot());
        var positioner = cut.FindComponent<NavigationMenuPositioner>();

        await positioner.InvokeAsync(() => positioner.Instance.OnPositionUpdated("top", "end", false, false));

        var div = cut.Find("div[role='presentation']");
        div.GetAttribute("data-side").ShouldBe("top");
        div.GetAttribute("data-align").ShouldBe("end");

    }

    [Fact]
    public Task IsInertWhenClosed()
    {
        var cut = Render(CreatePositionerInRoot());

        var div = cut.Find("div[role='presentation']");
        div.HasAttribute("inert").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreatePositionerInRoot(
            classValue: _ => "positioner-class"
        ));

        var div = cut.Find("div[role='presentation']");
        div.GetAttribute("class")!.ShouldContain("positioner-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<NavigationMenuPositioner>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Positioner"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
