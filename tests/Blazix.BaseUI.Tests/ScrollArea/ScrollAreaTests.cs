namespace Blazix.BaseUI.Tests.ScrollArea;

public class ScrollAreaTests : BunitContext, IScrollAreaContract
{
    private readonly BunitJSModuleInterop module;

    public ScrollAreaTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        module = JsInteropSetup.SetupScrollAreaModule(JSInterop);
    }

    private IRenderedComponent<ScrollAreaRoot> RenderScrollArea(
        bool keepMounted = true,
        Func<ScrollAreaRootState, string?>? classValue = null,
        Func<ScrollAreaRootState, string?>? styleValue = null,
        RenderFragment<RenderProps<ScrollAreaRootState>>? render = null)
    {
        return Render<ScrollAreaRoot>(parameters =>
        {
            parameters.AddUnmatched("data-testid", "root");

            if (classValue is not null)
                parameters.Add(p => p.ClassValue, classValue);
            if (styleValue is not null)
                parameters.Add(p => p.StyleValue, styleValue);
            if (render is not null)
                parameters.Add(p => p.Render, render);

            parameters.Add(p => p.ChildContent, CreateScrollAreaContent(keepMounted));
        });
    }

    private static RenderFragment CreateScrollAreaContent(bool keepMounted = true)
    {
        return builder =>
        {
            builder.OpenComponent<ScrollAreaViewport>(0);
            builder.AddAttribute(1, "data-testid", "viewport");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(viewportBuilder =>
            {
                viewportBuilder.OpenComponent<ScrollAreaContent>(0);
                viewportBuilder.AddAttribute(1, "data-testid", "content");
                viewportBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(contentBuilder =>
                {
                    contentBuilder.AddMarkupContent(0, "<div>Scrollable content</div>");
                }));
                viewportBuilder.CloseComponent();
            }));
            builder.CloseComponent();

            builder.OpenComponent<ScrollAreaScrollbar>(3);
            builder.AddAttribute(4, "data-testid", "vertical-scrollbar");
            builder.AddAttribute(5, "KeepMounted", keepMounted);
            builder.AddAttribute(6, "ChildContent", (RenderFragment)(scrollbarBuilder =>
            {
                scrollbarBuilder.OpenComponent<ScrollAreaThumb>(0);
                scrollbarBuilder.AddAttribute(1, "data-testid", "vertical-thumb");
                scrollbarBuilder.CloseComponent();
            }));
            builder.CloseComponent();

            builder.OpenComponent<ScrollAreaScrollbar>(7);
            builder.AddAttribute(8, "data-testid", "horizontal-scrollbar");
            builder.AddAttribute(9, "Orientation", Orientation.Horizontal);
            builder.AddAttribute(10, "KeepMounted", keepMounted);
            builder.AddAttribute(11, "ChildContent", (RenderFragment)(scrollbarBuilder =>
            {
                scrollbarBuilder.OpenComponent<ScrollAreaThumb>(0);
                scrollbarBuilder.AddAttribute(1, "data-testid", "horizontal-thumb");
                scrollbarBuilder.CloseComponent();
            }));
            builder.CloseComponent();

            builder.OpenComponent<ScrollAreaCorner>(12);
            builder.AddAttribute(13, "data-testid", "corner");
            builder.CloseComponent();
        };
    }

    private static Task ApplyMeasuredStateAsync(
        IRenderedComponent<ScrollAreaRoot> cut,
        bool scrollingX = false,
        bool scrollingY = false,
        bool hovering = false,
        bool hiddenX = false,
        bool hiddenY = false,
        bool hiddenCorner = false,
        bool overflowXStart = false,
        bool overflowXEnd = true,
        bool overflowYStart = false,
        bool overflowYEnd = true,
        double cornerWidth = 11,
        double cornerHeight = 13,
        double thumbWidth = 40,
        double thumbHeight = 44)
    {
        return cut.InvokeAsync(() => cut.Instance.OnScrollAreaStateChanged(
            scrollingX,
            scrollingY,
            hovering,
            true,
            hiddenX,
            hiddenY,
            hiddenCorner,
            overflowXStart,
            overflowXEnd,
            overflowYStart,
            overflowYEnd,
            cornerWidth,
            cornerHeight,
            thumbWidth,
            thumbHeight));
    }

    [Fact]
    public Task RootRendersAsDivByDefault()
    {
        var cut = RenderScrollArea();
        var root = cut.Find("[data-testid='root']");

        root.TagName.ShouldBe("DIV");
        root.GetAttribute("role").ShouldBe("presentation");
        root.GetAttribute("style").ShouldContain("position: relative");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RootRendersWithCustomRender()
    {
        RenderFragment<RenderProps<ScrollAreaRootState>> render = props => builder =>
        {
            builder.OpenElement(0, "section");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = RenderScrollArea(render: render);

        cut.Find("section[data-testid='root']").GetAttribute("role").ShouldBe("presentation");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RootForwardsAdditionalAttributes()
    {
        var cut = RenderScrollArea();
        var root = cut.Find("[data-testid='root']");

        root.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RootAppliesClassValue()
    {
        var cut = RenderScrollArea(classValue: _ => "scroll-root");
        var root = cut.Find("[data-testid='root']");

        root.GetAttribute("class").ShouldContain("scroll-root");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RootAppliesStyleValue()
    {
        var cut = RenderScrollArea(styleValue: _ => "width: 200px");
        var root = cut.Find("[data-testid='root']");

        root.GetAttribute("style").ShouldContain("width: 200px");
        return Task.CompletedTask;
    }

    [Fact]
    public async Task RootAppliesOverflowStateAttributes()
    {
        var cut = RenderScrollArea();

        await ApplyMeasuredStateAsync(cut, overflowXStart: true, overflowYStart: true);

        var root = cut.Find("[data-testid='root']");
        root.HasAttribute("data-has-overflow-x").ShouldBeTrue();
        root.HasAttribute("data-has-overflow-y").ShouldBeTrue();
        root.HasAttribute("data-overflow-x-start").ShouldBeTrue();
        root.HasAttribute("data-overflow-x-end").ShouldBeTrue();
        root.HasAttribute("data-overflow-y-start").ShouldBeTrue();
        root.HasAttribute("data-overflow-y-end").ShouldBeTrue();
    }

    [Fact]
    public Task ViewportRendersPresentationRegion()
    {
        var cut = RenderScrollArea();
        var viewport = cut.Find("[data-testid='viewport']");

        viewport.GetAttribute("role").ShouldBe("presentation");
        viewport.GetAttribute("data-id").ShouldEndWith("-viewport");
        viewport.GetAttribute("style").ShouldContain("overflow: scroll");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ViewportCombinesDisableScrollbarClass()
    {
        var cut = RenderScrollArea();
        var viewport = cut.Find("[data-testid='viewport']");

        viewport.GetAttribute("class").ShouldContain("base-ui-disable-scrollbar");
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ViewportUsesFocusableTabIndexWhenScrollable()
    {
        var cut = RenderScrollArea();

        cut.Find("[data-testid='viewport']").GetAttribute("tabindex").ShouldBe("-1");

        await ApplyMeasuredStateAsync(cut);

        cut.Find("[data-testid='viewport']").GetAttribute("tabindex").ShouldBe("0");
    }

    [Fact]
    public Task ContentRendersPresentationWrapper()
    {
        var cut = RenderScrollArea();
        var content = cut.Find("[data-testid='content']");

        content.GetAttribute("role").ShouldBe("presentation");
        content.GetAttribute("style").ShouldContain("min-width: fit-content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ScrollbarDefaultsToVerticalOrientation()
    {
        var cut = RenderScrollArea();
        var scrollbar = cut.Find("[data-testid='vertical-scrollbar']");

        scrollbar.GetAttribute("data-orientation").ShouldBe("vertical");
        scrollbar.GetAttribute("style").ShouldContain("--scroll-area-thumb-height");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ScrollbarSupportsHorizontalOrientation()
    {
        var cut = RenderScrollArea();
        var scrollbar = cut.Find("[data-testid='horizontal-scrollbar']");

        scrollbar.GetAttribute("data-orientation").ShouldBe("horizontal");
        scrollbar.GetAttribute("style").ShouldContain("--scroll-area-thumb-width");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ScrollbarHonorsKeepMounted()
    {
        var cut = RenderScrollArea(keepMounted: true);

        cut.Find("[data-testid='vertical-scrollbar']").ShouldNotBeNull();
        cut.Find("[data-testid='horizontal-scrollbar']").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ScrollbarRendersWhenOverflowAppears()
    {
        var cut = RenderScrollArea(keepMounted: false);

        cut.FindAll("[data-testid='vertical-scrollbar']").ShouldBeEmpty();

        await ApplyMeasuredStateAsync(cut);

        cut.Find("[data-testid='vertical-scrollbar']").ShouldNotBeNull();
        cut.Find("[data-testid='horizontal-scrollbar']").ShouldNotBeNull();
    }

    [Fact]
    public async Task ScrollbarAppliesAxisScrollingAttribute()
    {
        var cut = RenderScrollArea();

        await ApplyMeasuredStateAsync(cut, scrollingY: true);

        cut.Find("[data-testid='vertical-scrollbar']").HasAttribute("data-scrolling").ShouldBeTrue();
        cut.Find("[data-testid='horizontal-scrollbar']").HasAttribute("data-scrolling").ShouldBeFalse();

        await ApplyMeasuredStateAsync(cut, scrollingX: true);

        cut.Find("[data-testid='vertical-scrollbar']").HasAttribute("data-scrolling").ShouldBeFalse();
        cut.Find("[data-testid='horizontal-scrollbar']").HasAttribute("data-scrolling").ShouldBeTrue();
    }

    [Fact]
    public async Task ThumbReceivesOrientationAndMeasuredStyles()
    {
        var cut = RenderScrollArea();

        cut.Find("[data-testid='vertical-thumb']").GetAttribute("style").ShouldContain("visibility: hidden");

        await ApplyMeasuredStateAsync(cut);

        var verticalThumb = cut.Find("[data-testid='vertical-thumb']");
        var horizontalThumb = cut.Find("[data-testid='horizontal-thumb']");

        verticalThumb.GetAttribute("data-orientation").ShouldBe("vertical");
        verticalThumb.GetAttribute("style").ShouldContain("height: var(--scroll-area-thumb-height)");
        verticalThumb.GetAttribute("style").ShouldNotContain("visibility: hidden");
        horizontalThumb.GetAttribute("data-orientation").ShouldBe("horizontal");
        horizontalThumb.GetAttribute("style").ShouldContain("width: var(--scroll-area-thumb-width)");
    }

    [Fact]
    public async Task CornerRendersOnlyWhenBothScrollbarsAreVisible()
    {
        var cut = RenderScrollArea();

        cut.FindAll("[data-testid='corner']").ShouldBeEmpty();

        await ApplyMeasuredStateAsync(cut);

        var corner = cut.Find("[data-testid='corner']");
        corner.GetAttribute("style").ShouldContain("width: 11px");
        corner.GetAttribute("style").ShouldContain("height: 13px");
    }

    [Fact]
    public Task DescendantsRequireScrollAreaContext()
    {
        Render<ScrollAreaViewport>().Markup.ShouldBeEmpty();
        Render<ScrollAreaContent>().Markup.ShouldBeEmpty();
        Render<ScrollAreaScrollbar>().Markup.ShouldBeEmpty();
        Render<ScrollAreaThumb>().Markup.ShouldBeEmpty();
        Render<ScrollAreaCorner>().Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task DescendantRegistrationsAreIdempotentAcrossRootStateRerenders()
    {
        var cut = RenderScrollArea();
        await ApplyMeasuredStateAsync(cut);

        cut.WaitForAssertion(() =>
        {
            CountModuleInvocations("registerViewport").ShouldBe(1);
            CountModuleInvocations("registerContent").ShouldBe(1);
            CountModuleInvocations("registerScrollbar").ShouldBe(2);
            CountModuleInvocations("registerThumb").ShouldBe(2);
            CountModuleInvocations("registerCorner").ShouldBe(1);
        });

        await ApplyMeasuredStateAsync(cut, scrollingY: true);

        cut.WaitForAssertion(() =>
        {
            CountModuleInvocations("registerViewport").ShouldBe(1);
            CountModuleInvocations("registerContent").ShouldBe(1);
            CountModuleInvocations("registerScrollbar").ShouldBe(2);
            CountModuleInvocations("registerThumb").ShouldBe(2);
            CountModuleInvocations("registerCorner").ShouldBe(1);
        });
    }

    private int CountModuleInvocations(string identifier) =>
        module.Invocations.Count(invocation => invocation.Identifier == identifier);
}
