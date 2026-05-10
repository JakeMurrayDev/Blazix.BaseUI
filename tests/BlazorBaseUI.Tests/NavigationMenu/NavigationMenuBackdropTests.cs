namespace BlazorBaseUI.Tests.NavigationMenu;

public class NavigationMenuBackdropTests : BunitContext, INavigationMenuBackdropContract
{
    public NavigationMenuBackdropTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNavigationMenuModule(JSInterop);
        JsInteropSetup.SetupFloatingTreeModule(JSInterop);
    }

    private RenderFragment CreateBackdropInRoot(
        Func<NavigationMenuBackdropState, string>? classValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuBackdrop>(0);
                var attrIndex = 1;
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateBackdropWithContext(
        string? value = null,
        bool mounted = false,
        TransitionStatus transitionStatus = TransitionStatus.Undefined,
        Func<NavigationMenuBackdropState, string?>? classValue = null,
        Func<NavigationMenuBackdropState, string?>? styleValue = null,
        RenderFragment<RenderProps<NavigationMenuBackdropState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            var context = new NavigationMenuRootContext
            {
                Value = value,
                Mounted = mounted,
                TransitionStatus = transitionStatus,
                GetValue = () => value,
                GetMounted = () => mounted,
                SetValueAsync = (_, _) => Task.CompletedTask,
                SetTriggerElement = (_, _) => { },
                GetTriggerElement = _ => null,
                SetPopupElement = _ => { },
                SetPositionerElement = _ => { },
                SetViewportElement = _ => { },
                SetViewportTargetElement = _ => { },
                RegisterItem = _ => { },
                UnregisterItem = _ => { },
                SetContentElement = (_, _) => { },
                EmitClose = _ => { },
                SetViewportInert = _ => { },
                SetPrevTriggerElement = _ => { },
                GetPrevTriggerElement = () => null,
                SetListElement = _ => { },
                UnmountAsync = () => Task.CompletedTask,
                RegisterContentCallback = (_, _) => { },
                UnregisterContentCallback = _ => { },
            };

            builder.OpenComponent<CascadingValue<NavigationMenuRootContext>>(0);
            builder.AddAttribute(1, "Value", context);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuBackdrop>(0);
                var attrIndex = 1;
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                if (render is not null)
                    innerBuilder.AddAttribute(attrIndex++, "Render", render);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersDivByDefault()
    {
        var cut = Render(CreateBackdropInRoot());

        var div = cut.Find("div[role='presentation']");
        div.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateBackdropInRoot(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "backdrop" } }
        ));

        var div = cut.Find("div[data-testid='backdrop']");
        div.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRolePresentation()
    {
        var cut = Render(CreateBackdropInRoot());

        var div = cut.Find("div[role='presentation']");
        div.GetAttribute("role").ShouldBe("presentation");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreateBackdropInRoot());

        var div = cut.Find("div[role='presentation']");
        div.HasAttribute("data-closed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreateBackdropWithContext(value: "item1", mounted: true));

        var div = cut.Find("div[role='presentation']");
        div.HasAttribute("data-open").ShouldBeTrue();
        div.HasAttribute("data-closed").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task IsHiddenWhenNotMounted()
    {
        var cut = Render(CreateBackdropWithContext(mounted: false));

        var div = cut.Find("div[role='presentation']");
        div.HasAttribute("hidden").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task IsNotHiddenWhenMounted()
    {
        var cut = Render(CreateBackdropWithContext(value: "item1", mounted: true));

        var div = cut.Find("div[role='presentation']");
        div.HasAttribute("hidden").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasUserSelectNoneStyle()
    {
        var cut = Render(CreateBackdropWithContext());

        var div = cut.Find("div[role='presentation']");
        div.GetAttribute("style").ShouldContain("user-select: none");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataStartingStyleDuringTransition()
    {
        var cut = Render(CreateBackdropWithContext(
            value: "item1",
            mounted: true,
            transitionStatus: TransitionStatus.Starting));

        var div = cut.Find("div[role='presentation']");
        div.HasAttribute("data-starting-style").ShouldBeTrue();
        div.HasAttribute("data-ending-style").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataEndingStyleDuringTransition()
    {
        var cut = Render(CreateBackdropWithContext(
            mounted: true,
            transitionStatus: TransitionStatus.Ending));

        var div = cut.Find("div[role='presentation']");
        div.HasAttribute("data-ending-style").ShouldBeTrue();
        div.HasAttribute("data-starting-style").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateBackdropInRoot(
            classValue: _ => "backdrop-class"
        ));

        var div = cut.Find("div[role='presentation']");
        div.GetAttribute("class")!.ShouldContain("backdrop-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateBackdropWithContext(
            styleValue: _ => "opacity: 0.5"));

        var div = cut.Find("div[role='presentation']");
        var style = div.GetAttribute("style")!;
        style.ShouldContain("opacity: 0.5");
        style.ShouldContain("user-select: none");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<NavigationMenuBackdropState>> render = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateBackdropWithContext(render: render));

        var span = cut.Find("span[role='presentation']");
        span.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<NavigationMenuBackdrop>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Backdrop"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
