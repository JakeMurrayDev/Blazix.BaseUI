namespace BlazorBaseUI.Tests.Separator;

public class SeparatorTests : BunitContext, ISeparatorContract
{
    public SeparatorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateSeparator(
        Orientation orientation = Orientation.Horizontal,
        Func<SeparatorState, string>? classValue = null,
        Func<SeparatorState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<BlazorBaseUI.Separator.Separator>(0);
            var seq = 1;
            builder.AddAttribute(seq++, "Orientation", orientation);
            if (classValue is not null)
                builder.AddAttribute(seq++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(seq++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(seq++, "AdditionalAttributes", additionalAttributes);
            if (childContent is not null)
                builder.AddAttribute(seq++, "ChildContent", childContent);
            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateSeparator());
        var element = cut.Find("[role='separator']");
        element.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithSeparatorRole()
    {
        var cut = Render(CreateSeparator());
        var element = cut.Find("[role='separator']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRenderFragment()
    {
        var fragment = (RenderFragment)(builder =>
        {
            builder.OpenComponent<BlazorBaseUI.Separator.Separator>(0);
            builder.AddAttribute(1, "Render", (RenderFragment<RenderProps<SeparatorState>>)(props => b =>
            {
                b.OpenElement(0, "hr");
                b.AddMultipleAttributes(1, props.Attributes);
                b.AddContent(2, props.ChildContent);
                b.CloseElement();
            }));
            builder.CloseComponent();
        });

        var cut = Render(fragment);
        cut.Find("hr[role='separator']").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateSeparator(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-custom", "value" }
            }));
        var element = cut.Find("[role='separator']");
        element.GetAttribute("data-custom")!.ShouldBe("value");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateSeparator(
            classValue: _ => "sep-class"));
        var element = cut.Find("[role='separator']");
        element.GetAttribute("class")!.ShouldContain("sep-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateSeparator(
            styleValue: _ => "margin: 0 8px"));
        var element = cut.Find("[role='separator']");
        element.GetAttribute("style")!.ShouldContain("margin: 0 8px");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateSeparator(
            childContent: b => b.AddContent(0, "Sep Content")));
        cut.Markup.ShouldContain("Sep Content");
        return Task.CompletedTask;
    }

    // Orientation

    [Fact]
    public Task DefaultsToHorizontalOrientation()
    {
        var cut = Render(CreateSeparator());
        var element = cut.Find("[role='separator']");
        element.GetAttribute("aria-orientation")!.ShouldBe("horizontal");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsVerticalOrientation()
    {
        var cut = Render(CreateSeparator(orientation: Orientation.Vertical));
        var element = cut.Find("[role='separator']");
        element.GetAttribute("aria-orientation")!.ShouldBe("vertical");
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task SetsDataOrientationHorizontal()
    {
        var cut = Render(CreateSeparator());
        var element = cut.Find("[role='separator']");
        element.GetAttribute("data-orientation")!.ShouldBe("horizontal");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsDataOrientationVertical()
    {
        var cut = Render(CreateSeparator(orientation: Orientation.Vertical));
        var element = cut.Find("[role='separator']");
        element.GetAttribute("data-orientation")!.ShouldBe("vertical");
        return Task.CompletedTask;
    }
}
