using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Blazix.BaseUI.Separator;

namespace Blazix.BaseUI.Tests.Toolbar;

public class ToolbarSeparatorTests : BunitContext, IToolbarSeparatorContract
{
    public ToolbarSeparatorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupToolbarModule(JSInterop);
        Services.AddLogging();
    }

    private RenderFragment CreateToolbarSeparatorInRoot(
        Orientation rootOrientation = Orientation.Horizontal,
        Func<SeparatorState, string>? classValue = null,
        Func<SeparatorState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<ToolbarRoot>(0);
            builder.AddAttribute(1, "Orientation", rootOrientation);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<ToolbarSeparator>(0);
                var seq = 1;
                if (classValue is not null)
                    inner.AddAttribute(seq++, "ClassValue", classValue);
                if (styleValue is not null)
                    inner.AddAttribute(seq++, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    inner.AddAttribute(seq++, "AdditionalAttributes", additionalAttributes);
                if (childContent is not null)
                    inner.AddAttribute(seq++, "ChildContent", childContent);
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsSeparatorComponent()
    {
        var cut = Render(CreateToolbarSeparatorInRoot());
        var element = cut.Find("[role='separator']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRenderFragment()
    {
        var fragment = (RenderFragment)(builder =>
        {
            builder.OpenComponent<ToolbarRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<ToolbarSeparator>(0);
                inner.AddAttribute(1, "Render", (RenderFragment<RenderProps<SeparatorState>>)(props => b =>
                {
                    b.OpenElement(0, "hr");
                    b.AddMultipleAttributes(1, props.Attributes);
                    b.AddContent(2, props.ChildContent);
                    b.CloseElement();
                }));
                inner.CloseComponent();
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
        var cut = Render(CreateToolbarSeparatorInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-custom", "value" }
            }));
        var element = cut.Find("[role='separator']");
        element.GetAttribute("data-custom").ShouldBe("value");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateToolbarSeparatorInRoot(
            classValue: _ => "sep-class"));
        var element = cut.Find("[role='separator']");
        element.GetAttribute("class").ShouldContain("sep-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateToolbarSeparatorInRoot(
            childContent: b => b.AddContent(0, "Sep Content")));
        cut.Markup.ShouldContain("Sep Content");
        return Task.CompletedTask;
    }

    // Orientation inversion

    [Fact]
    public Task InvertsOrientationFromHorizontalToVertical()
    {
        var cut = Render(CreateToolbarSeparatorInRoot(rootOrientation: Orientation.Horizontal));
        var element = cut.Find("[role='separator']");
        element.GetAttribute("aria-orientation").ShouldBe("vertical");
        element.GetAttribute("data-orientation").ShouldBe("vertical");
        return Task.CompletedTask;
    }

    [Fact]
    public Task InvertsOrientationFromVerticalToHorizontal()
    {
        var cut = Render(CreateToolbarSeparatorInRoot(rootOrientation: Orientation.Vertical));
        var element = cut.Find("[role='separator']");
        element.GetAttribute("aria-orientation").ShouldBe("horizontal");
        element.GetAttribute("data-orientation").ShouldBe("horizontal");
        return Task.CompletedTask;
    }

    // Validation

    [Fact]
    public Task ThrowsWhenNotInsideToolbarRoot()
    {
        Should.Throw<InvalidOperationException>(() =>
        {
            Render(builder =>
            {
                builder.OpenComponent<ToolbarSeparator>(0);
                builder.CloseComponent();
            });
        });

        return Task.CompletedTask;
    }

    // Style

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateToolbarSeparatorInRoot(
            styleValue: _ => "margin: 0 8px"));
        var element = cut.Find("[role='separator']");
        element.GetAttribute("style").ShouldContain("margin: 0 8px");
        return Task.CompletedTask;
    }
}
