namespace Blazix.BaseUI.Tests.Menu;

public class MenuBackdropTests : BunitContext, IMenuBackdropContract
{
    public MenuBackdropTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateBackdropInMenu(
        bool defaultOpen = true,
        RenderFragment<RenderProps<MenuBackdropState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<MenuBackdropState, string>? classValue = null,
        Func<MenuBackdropState, string>? styleValue = null)
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
                    portalBuilder.OpenComponent<MenuBackdrop>(0);
                    var attrIndex = 1;

                    if (render is not null)
                        portalBuilder.AddAttribute(attrIndex++, "Render", render);
                    if (classValue is not null)
                        portalBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                    if (styleValue is not null)
                        portalBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                    if (additionalAttributes is not null)
                        portalBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);

                    portalBuilder.CloseComponent();

                    portalBuilder.OpenComponent<MenuPositioner>(10);
                    portalBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<MenuPopup>(0);
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
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateBackdropInMenu());

        var backdrop = cut.Find("div[role='presentation']");
        backdrop.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<MenuBackdropState>> render = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback!);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateBackdropInMenu(render: render));

        var backdrop = cut.Find("span[role='presentation']");
        backdrop.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateBackdropInMenu(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "backdrop" },
                { "aria-label", "Backdrop" }
            }
        ));

        var backdrop = cut.Find("[data-testid='backdrop']");
        backdrop.GetAttribute("aria-label")!.ShouldBe("Backdrop");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreateBackdropInMenu(
            classValue: state => state.Open ? "open-class" : "closed-class"
        ));

        var backdrop = cut.Find("div[role='presentation']");
        backdrop.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreateBackdropInMenu(
            styleValue: _ => "background: black"
        ));

        var backdrop = cut.Find("div[role='presentation']");
        backdrop.GetAttribute("style")!.ShouldContain("background: black");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreateBackdropInMenu(defaultOpen: true));

        var backdrop = cut.Find("div[role='presentation']");
        backdrop.HasAttribute("data-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task HasPointerEventsNoneWhenHoverOpened()
    {
        var cut = Render(CreateBackdropInMenu(defaultOpen: false));

        var root = cut.FindComponent<MenuRoot>();
        await root.InvokeAsync(() => root.Instance.OnHoverOpen());

        var backdrop = cut.Find("div[role='presentation']");
        backdrop.GetAttribute("style")!.ShouldContain("pointer-events: none");
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<MenuBackdrop>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
