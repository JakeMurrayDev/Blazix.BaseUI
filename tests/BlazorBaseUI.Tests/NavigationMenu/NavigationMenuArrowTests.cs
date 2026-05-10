namespace BlazorBaseUI.Tests.NavigationMenu;

public class NavigationMenuArrowTests : BunitContext, INavigationMenuArrowContract
{
    public NavigationMenuArrowTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNavigationMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
    }

    private RenderFragment CreateArrowInRoot(
        string? defaultValue = null,
        Func<NavigationMenuArrowState, string>? classValue = null,
        Func<NavigationMenuArrowState, string?>? styleValue = null,
        RenderFragment<RenderProps<NavigationMenuArrowState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            var rootAttrIndex = 1;
            if (defaultValue is not null)
                builder.AddAttribute(rootAttrIndex++, "DefaultValue", defaultValue);
            builder.AddAttribute(rootAttrIndex, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuPositioner>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<NavigationMenuArrow>(0);
                    var attrIndex = 1;
                    if (classValue is not null)
                        posBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                    if (styleValue is not null)
                        posBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                    if (render is not null)
                        posBuilder.AddAttribute(attrIndex++, "Render", render);
                    if (additionalAttributes is not null)
                        posBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersDivByDefault()
    {
        var cut = Render(CreateArrowInRoot());

        var div = cut.Find("div[aria-hidden='true']");
        div.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateArrowInRoot(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "arrow" } }
        ));

        var div = cut.Find("div[data-testid='arrow']");
        div.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaHidden()
    {
        var cut = Render(CreateArrowInRoot());

        var div = cut.Find("div[aria-hidden='true']");
        div.GetAttribute("aria-hidden").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataSide()
    {
        var cut = Render(CreateArrowInRoot());

        var div = cut.Find("div[aria-hidden='true']");
        div.GetAttribute("data-side").ShouldBe("bottom");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataAlign()
    {
        var cut = Render(CreateArrowInRoot());

        var div = cut.Find("div[aria-hidden='true']");
        div.GetAttribute("data-align").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateArrowInRoot(
            classValue: _ => "arrow-class"
        ));

        var div = cut.Find("div[aria-hidden='true']");
        div.GetAttribute("class")!.ShouldContain("arrow-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateArrowInRoot(
            styleValue: _ => "color: red"
        ));

        var div = cut.Find("div[aria-hidden='true']");
        div.GetAttribute("style")!.ShouldContain("color: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<NavigationMenuArrowState>> renderAsSpan = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            builder.AddContent(2, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateArrowInRoot(render: renderAsSpan));

        var span = cut.Find("span[aria-hidden='true']");
        span.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreateArrowInRoot(defaultValue: "item1"));

        var div = cut.Find("div[aria-hidden='true']");
        div.HasAttribute("data-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreateArrowInRoot());

        var div = cut.Find("div[aria-hidden='true']");
        div.HasAttribute("data-closed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataUncenteredWhenUncentered()
    {
        var cut = Render(CreateArrowInRoot(defaultValue: "item1"));

        var positioner = cut.FindComponent<NavigationMenuPositioner>();
        positioner.InvokeAsync(() => positioner.Instance.OnPositionUpdated("bottom", "center", false, true));

        var arrow = cut.FindComponent<NavigationMenuArrow>();
        arrow.Render();

        var div = cut.Find("div[aria-hidden='true']");
        div.HasAttribute("data-uncentered").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<NavigationMenuArrow>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Arrow"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
