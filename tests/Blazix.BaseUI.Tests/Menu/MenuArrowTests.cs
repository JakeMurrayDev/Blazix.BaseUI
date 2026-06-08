namespace Blazix.BaseUI.Tests.Menu;

public class MenuArrowTests : BunitContext, IMenuArrowContract
{
    public MenuArrowTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateArrowInMenu(
        bool defaultOpen = true,
        RenderFragment<RenderProps<MenuArrowState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<MenuArrowState, string>? classValue = null,
        Func<MenuArrowState, string>? styleValue = null)
    {
        return builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment<MenuRootPayloadContext>)(_ => innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<MenuPortal>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<MenuPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<MenuPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<MenuArrow>(0);
                            var attrIndex = 1;

                            if (render is not null)
                                popupBuilder.AddAttribute(attrIndex++, "Render", render);
                            if (classValue is not null)
                                popupBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                            if (styleValue is not null)
                                popupBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                            if (additionalAttributes is not null)
                                popupBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);

                            popupBuilder.CloseComponent();
                        }));
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
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateArrowInMenu());

        var arrow = cut.Find("div[aria-hidden='true']");
        arrow.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<MenuArrowState>> render = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback!);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateArrowInMenu(render: render));

        var arrow = cut.Find("span[aria-hidden='true']");
        arrow.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateArrowInMenu(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "arrow" },
                { "aria-label", "Arrow" }
            }
        ));

        var arrow = cut.Find("[data-testid='arrow']");
        arrow.GetAttribute("aria-label")!.ShouldBe("Arrow");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreateArrowInMenu(
            classValue: state => state.Open ? "open-class" : "closed-class"
        ));

        var arrow = cut.Find("div[aria-hidden='true']");
        arrow.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreateArrowInMenu(
            styleValue: _ => "color: red"
        ));

        var arrow = cut.Find("div[aria-hidden='true']");
        arrow.GetAttribute("style")!.ShouldContain("color: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataSideAttribute()
    {
        var cut = Render(CreateArrowInMenu());

        var arrow = cut.Find("div[aria-hidden='true']");
        arrow.HasAttribute("data-side").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataAlignAttribute()
    {
        var cut = Render(CreateArrowInMenu());

        var arrow = cut.Find("div[aria-hidden='true']");
        arrow.HasAttribute("data-align").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaHidden()
    {
        var cut = Render(CreateArrowInMenu());

        var arrow = cut.Find("div[aria-hidden='true']");
        arrow.GetAttribute("aria-hidden")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreateArrowInMenu(defaultOpen: true));

        var arrow = cut.Find("div[aria-hidden='true']");
        arrow.HasAttribute("data-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<MenuArrow>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
